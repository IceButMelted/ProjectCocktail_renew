/*
 * BubblePresenter.cs  (Mode B only — world-target alignment)
 *
 * Scene hierarchy expected:
 *   Canvas
 *   └── Bubble Presenter          (Stretch/Stretch, anchors 0,0 → 1,1)
 *       └── BubbleContainer       (middle/center, anchor 0.5,0.5 pivot 0.5,0.5)
 *           ├── Background
 *           ├── Character Name
 *           ├── Text
 *           ├── Button Container
 *           │   └── Continue Button
 *           └── Tail              (default sprite points DOWN)
 *
 * ── Yarn Spinner Plus ───────────────────────────────────────────────────
 * No code changes needed. Plus uses the same runtime API (DialogueRunner,
 * DialoguePresenterBase, LocalizedLine) as the free package — it only adds
 * editor-side tooling (visual graph, saliency, etc). Just make sure the
 * free package is removed from the project before importing Plus.
 *
 * ── Text Animator ───────────────────────────────────────────────────────
 * 1. On the `lineText` GameObject: add `TextAnimator_TMP` + Yarn Spinner's
 *    `Text Animator Yarn Typewriter` component (ships with the Text
 *    Animator add-on package).
 * 2. Set Typewriter → Style to "Custom" and drag that component into
 *    `customTypewriter` below. Switch back to Instant/ByLetter/ByWord to
 *    fall back to Yarn Spinner's built-in typewriters.
 * 3. Only ONE `TextAnimatorYarnTypewriter` may exist per DialogueRunner —
 *    it registers a shared "speed" markup processor on the line provider.
 *    Two instances (e.g. this one + the sample dialogue prefab's) throws
 *    `InvalidOperationException: marker processor already registered`.
 * 4. Text Animator tags use Yarn's [square brackets]: [shake]hi[/shake].
 *    For mid-line pauses use [waitfor=0.3], not <<wait>> (that's a
 *    standalone Yarn command, can't be embedded inside a line's text).
 *    Make sure "waitfor" is in the typewriter's actionTags list.
 * 5. Text Animator tags + inline [action] markup can't combine in the same
 *    line (Yarn Spinner limitation) — use Text Animator's own event system
 *    for in-line triggers instead.
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Yarn.Unity;
using Yarn.Unity.Attributes;

namespace YarnSpinner.Custom
{
    public class BubblePresenter : DialoguePresenterBase
    {
        // ─────────────────────────────────────────────────────────────────────
        // Serialized types
        // ─────────────────────────────────────────────────────────────────────

        [Serializable]
        public class CharacterTarget
        {
            [Tooltip("Exact character name used in the .yarn script.")]
            public string characterName;
            [Tooltip("World-space Transform to track (e.g. character head).")]
            public Transform worldTarget;
        }

        // 8 compass directions from bubble → target
        public enum TailDirection
        {
            BottomCenter, BottomLeft, BottomRight,
            LeftCenter, RightCenter,
            TopCenter, TopLeft, TopRight
        }

        // Mirrors LinePresenter.TypewriterType — "Custom" is the slot Text Animator's
        // TextAnimatorYarnTypewriter plugs into via customTypewriter below.
        internal enum TypewriterType
        {
            Instant, ByLetter, ByWord, Custom,
        }

        // ─────────────────────────────────────────────────────────────────────
        // Inspector
        // ─────────────────────────────────────────────────────────────────────

        [Group("Bubble References")]
        [MustNotBeNull]
        [SerializeField] private RectTransform bubbleRect;

        [Group("Bubble References")]
        [Tooltip("Background GameObject to calculate Size")]
        [SerializeField] private RectTransform backGroundText;

        [Group("Bubble References")]
        [MustNotBeNull]
        [SerializeField] private TMP_Text lineText;

        [Group("Bubble References")]
        [SerializeField] private TMP_Text characterNameText;
        private string _lastCharacterName;

        [Group("Bubble References")]
        [MustNotBeNull]
        [SerializeField] private GameObject bubbleContainer;

        [Group("Fade")]
        [Label("Fade UI")]
        [SerializeField] private bool useFadeEffect = true;

        [Group("Fade")]
        [SerializeField] private CanvasGroup bubbleCanvasGroup;

        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        [SerializeField] private float fadeUpDuration = 0.25f;

        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        [SerializeField] private float fadeDownDuration = 0.1f;

        [Group("Tail / Pointer")]
        [SerializeField] private RectTransform tailImage;

        // ── Typewriter ──────────────────────────────────────────────────────
        // Same shape as LinePresenter's typewriter block: pick a style, and
        // for Custom, drag in anything that implements IAsyncTypewriter —
        // including Text Animator's Text Animator Yarn Typewriter component.

        [Group("Typewriter")]
        [SerializeField] internal TypewriterType typewriterStyle = TypewriterType.ByLetter;

        [Group("Typewriter")]
        [ShowIf(nameof(typewriterStyle), TypewriterType.ByLetter)]
        [Label("Letters per Second")]
        [Min(0)]
        [SerializeField] private int lettersPerSecond = 60;

        [Group("Typewriter")]
        [ShowIf(nameof(typewriterStyle), TypewriterType.ByWord)]
        [Label("Words per Second")]
        [Min(0)]
        [SerializeField] private int wordsPerSecond = 10;

        [Group("Typewriter")]
        [ShowIf(nameof(typewriterStyle), TypewriterType.Custom)]
        [Tooltip("Assign a component implementing IAsyncTypewriter, e.g. Text Animator's Text Animator Yarn Typewriter.")]
        [SerializeField] private InterfaceContainer<IAsyncTypewriter> customTypewriter;

        /// <summary>
        /// The typewriter used to display this line's text (same IAsyncTypewriter
        /// interface LinePresenter uses), so a LineAdvancer/button handler can
        /// check "is the line fully shown yet" the same way.
        /// </summary>
        public IAsyncTypewriter Typewriter { get; private set; }

        [Group("Input Handler")]
        [Tooltip("Auto-found in children if left empty.")]
        [SerializeField] private BubblePresenterButtonHandler buttonHandler;

        [Group("Input Handler")]
        [SerializeField] private bool useLineAdvancer = true;
        private LineAdvancer _activeLineAdvancer;

        [Group("Input Handler")]
        [ShowIf(nameof(useLineAdvancer))]
        [SerializeField] private LineAdvancer lineAdvancer;

        [Group("World Target Mapping")]
        [SerializeField] private List<CharacterTarget> characterTargets = new();

        [Group("World Target Mapping")]
        [SerializeField] private float bubbleAboveTargetOffset = 20f;

        [Group("World Target Mapping")]
        [SerializeField] private float bubbleBelowTargetOffset = 20f;

        [Group("Screen Boundary Clamping")]
        [SerializeField] private float screenBorderPadding = 50f;

        [Group("Fallback Canvas Anchor")]
        [Label("Anchor (no target found)")]
        [SerializeField] private RectTransform fallbackCanvasAnchor;

        [Group("Fallback Canvas Anchor")]
        [SerializeField] private float fallbackScreenBorderPadding = 50f;

        private Transform _lastTargetPos;

        // ─────────────────────────────────────────────────────────────────────
        // Runtime
        // ─────────────────────────────────────────────────────────────────────

        private Dictionary<string, Transform> _targetLookup = new();
        private Canvas _canvas;
        private Camera _renderCam;

        // ─────────────────────────────────────────────────────────────────────
        // Unity
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            SetBubbleVisible(false);
            SetTailVisible(false);

            if (bubbleCanvasGroup != null)
                bubbleCanvasGroup.alpha = 0f; // start transparent so first fade-in reads correctly

            if (buttonHandler == null)
                buttonHandler = GetComponentInChildren<BubblePresenterButtonHandler>();

            // Mirrors LinePresenter.Awake()'s typewriter switch. Text Animator's
            // TextAnimatorYarnTypewriter implements IAsyncTypewriter, so it plugs
            // into the Custom slot exactly like any other custom typewriter would.
            switch (typewriterStyle)
            {
                case TypewriterType.Instant:
                    Typewriter = new InstantTypewriter { TextElement = lineText };
                    break;

                case TypewriterType.ByLetter:
                    Typewriter = new LetterTypewriter { TextElement = lineText, CharactersPerSecond = lettersPerSecond };
                    break;

                case TypewriterType.ByWord:
                    Typewriter = new WordTypewriter { TextElement = lineText, WordsPerSecond = wordsPerSecond };
                    break;

                case TypewriterType.Custom:
                    Typewriter = customTypewriter?.Interface;
                    if (Typewriter == null)
                        Debug.LogWarning($"{nameof(BubblePresenter)}: typewriter style is Custom but no typewriter is assigned.");
                    else
                        Typewriter.TextElement = lineText;
                    break;
            }

            // Wires buttonHandler + typewriter to the LineAdvancer (or unwires if disabled).
            ApplyLineAdvancerState();

            _targetLookup.Clear();
            foreach (var e in characterTargets)
                if (!string.IsNullOrEmpty(e.characterName) && e.worldTarget != null)
                    _targetLookup[e.characterName] = e.worldTarget;

            _canvas = GetComponentInParent<Canvas>();
            _renderCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                         ? _canvas.worldCamera
                         : Camera.main;
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(SceneLoaderBridge.ChapterNodeName))
            {
                DialogueRunner runner = FindFirstObjectByType<DialogueRunner>();
                if (runner != null)
                {
                    SceneLoaderBridge.DialogueRootNode = SceneLoaderBridge.ChapterNodeName;
                    SceneLoaderBridge.SessionOptionChoices.Clear();
                    runner.StartDialogue(SceneLoaderBridge.ChapterNodeName);
                }
            }
        }

        // Keeps the bubble glued to a moving target every frame while a line is showing.
        private void LateUpdate()
        {
            if (_lastTargetPos != null)
                PositionBubbleAtTarget(_lastTargetPos);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DialoguePresenterBase
        // ─────────────────────────────────────────────────────────────────────

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

        public override YarnTask OnDialogueCompleteAsync()
        {
            Dismiss();
            return YarnTask.CompletedTask;
        }

        /// <summary>Skips the typewriter straight to full text via the assigned LineAdvancer.</summary>
        public void RequestHurryUpLine() => lineAdvancer?.RequestLineHurryUp();

        /// <summary>Advances to the next line via the assigned LineAdvancer.</summary>
        public void RequestNextDialogueLine() => lineAdvancer?.RequestNextLine();

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            // ── SAVE/LOAD ────────────────────────────────────────────────────
            SceneLoaderBridge.CurrentLineId = line.TextID;

            if (line.Metadata != null)
                foreach (var tag in line.Metadata)
                    if (tag == "save_checkpoint") { SceneLoaderBridge.CheckpointLineId = line.TextID; break; }

            if (SceneLoaderBridge.IsSilentReplay)
            {
                if (line.TextID == SceneLoaderBridge.TargetLineId)
                {
                    SceneLoaderBridge.IsSilentReplay = false;
                    SceneLoaderBridge.TargetLineId = null;
                }
                else return;
            }
            // ── END SAVE/LOAD ─────────────────────────────────────────────────

            buttonHandler?.OnLineBegin();

            string currentCharacterName = line.CharacterName ?? string.Empty;
            bool isNewCharacter = _lastCharacterName != currentCharacterName;
            _lastCharacterName = currentCharacterName;

            lineText.text = string.Empty; // prevent ghost text from the previous line

            // ── Resolve position & tail ───────────────────────────────────────
            bool foundTarget = _targetLookup.TryGetValue(currentCharacterName, out Transform worldTarget)
                                && worldTarget != null;

            _lastTargetPos = foundTarget ? worldTarget : null;

            // TODO: resize bubble to fit text

            if (foundTarget)
            {
                PositionBubbleAtTarget(worldTarget);
            }
            else if (fallbackCanvasAnchor != null)
            {
                Vector2 fallbackScreen = RectTransformToScreenPoint(fallbackCanvasAnchor);
                if (bubbleRect != null)
                {
                    Vector2 clamped = ClampBubbleToScreen(fallbackScreen.x, fallbackScreen.y, fallbackScreenBorderPadding);
                    bubbleRect.position = new Vector3(clamped.x, clamped.y, 0f);
                }
                SetTailVisible(false);
            }
            else
            {
                SetTailVisible(false);
            }

            // ── Character name label ──────────────────────────────────────────
            if (characterNameText != null)
                characterNameText.text = currentCharacterName;

            // ── Show bubble ───────────────────────────────────────────────────
            string lineBody = line.TextWithoutCharacterName.Text;
            SetBubbleVisible(true);

            if (bubbleCanvasGroup != null)
            {
                if (useFadeEffect)
                    await Effects.FadeAlphaAsync(bubbleCanvasGroup, 0f, 1f, fadeUpDuration, token.HurryUpToken);
                else
                    bubbleCanvasGroup.alpha = 1f;
            }

            // ── Typewriter ────────────────────────────────────────────────────
            // token.HurryUpToken is the same token LineAdvancer.RequestLineHurryUp()
            // cancels, so this returns immediately on hurry-up from any source.
            if (Typewriter != null && lineText != null)
            {
                Typewriter.PrepareForContent(line.TextWithoutCharacterName);

                await Typewriter
                    .RunTypewriter(line.TextWithoutCharacterName, token.HurryUpToken)
                    .SuppressCancellationThrow();

                lineText.maxVisibleCharacters = int.MaxValue; // ensure full text visible either way
            }
            else if (lineText != null)
            {
                lineText.maxVisibleCharacters = int.MaxValue;
                lineText.text = lineBody;
            }

            // Player already requested "next" mid-typewriter (e.g. fast double-tap) — bail out.
            if (token.IsNextContentRequested)
            {
                Dismiss();
                return;
            }

            // ── Wait for input ────────────────────────────────────────────────
            buttonHandler?.OnTypewriterComplete();

            while (!token.IsNextContentRequested)
            {
                if (buttonHandler != null && buttonHandler.IsAdvanceRequested) break;
                await YarnTask.Delay(TimeSpan.FromSeconds(0), token.NextContentToken)
                              .SuppressCancellationThrow();
            }

            if (bubbleCanvasGroup != null)
            {
                if (useFadeEffect)
                    await Effects.FadeAlphaAsync(bubbleCanvasGroup, 1f, 0f, fadeDownDuration, token.HurryUpToken)
                                 .SuppressCancellationThrow();
                else
                    bubbleCanvasGroup.alpha = 0f;
            }

            if (isNewCharacter)
                Dismiss();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Positioning
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Clamps the bubble to screen bounds around the target, nudges it above/below
        /// depending on which half of the screen the target is on, and updates the tail.
        /// Shared by LateUpdate (continuous tracking) and RunLineAsync (initial placement).
        /// </summary>
        private void PositionBubbleAtTarget(Transform target)
        {
            if (bubbleRect == null) return;

            Vector2 targetScreen = WorldToScreenPoint(target.position);
            Vector2 clamped = ClampBubbleToScreen(targetScreen.x, targetScreen.y, screenBorderPadding);
            bubbleRect.position = new Vector3(clamped.x, clamped.y, 0f);

            bool targetIsLowerHalf = IsTargetLowerHalfScreen(targetScreen);
            bubbleRect.position += new Vector3(0f, targetIsLowerHalf ? bubbleAboveTargetOffset : -bubbleBelowTargetOffset, 0f);

            if (IsTargetOnScreen(targetScreen))
                RotateTailForDirection(bubbleRect.position, targetScreen);

            // ponytail: tail sprite is forced hidden for now regardless of on/off-screen target;
            // flip this to IsTargetOnScreen(targetScreen) once tail art/behaviour is finalized.
            SetTailVisible(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        // helper
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns true if the screen-pixel point is within screen bounds.</summary>
        private bool IsTargetOnScreen(Vector2 screenPos) =>
            screenPos.x >= 0 && screenPos.x <= Screen.width &&
            screenPos.y >= 0 && screenPos.y <= Screen.height;

        /// <summary>
        /// Snaps the direction from bubbleScreenPos → targetScreenPos to one of 8
        /// compass directions, then positions and rotates the tail accordingly.
        /// Tail sprite default orientation = pointing DOWN (rotation identity).
        /// </summary>
        private void RotateTailForDirection(Vector2 bubbleScreenPos, Vector2 targetScreenPos)
        {
            if (tailImage == null || bubbleRect == null) return;

            Vector2 dir = targetScreenPos - bubbleScreenPos;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float snapped = Mathf.Round(angle / 45f) * 45f; // −180 … +180, multiples of 45

            TailDirection td = snapped switch
            {
                -90f => TailDirection.BottomCenter,
                -45f => TailDirection.BottomRight,
                0f => TailDirection.RightCenter,
                45f => TailDirection.TopRight,
                90f => TailDirection.TopCenter,
                135f => TailDirection.TopLeft,
                float a when Mathf.Abs(a) == 180f => TailDirection.LeftCenter,
                -135f => TailDirection.BottomLeft,
                _ => TailDirection.BottomCenter
            };

            float hw = bubbleRect.rect.width * 0.5f;
            float hh = bubbleRect.rect.height * 0.5f;

            tailImage.anchorMin = new Vector2(0.5f, 0.5f);
            tailImage.anchorMax = new Vector2(0.5f, 0.5f);
            tailImage.pivot = new Vector2(0.5f, 0.5f);

            tailImage.anchoredPosition = td switch
            {
                TailDirection.BottomCenter => new Vector2(0f, -hh),
                TailDirection.BottomLeft => new Vector2(-hw, -hh),
                TailDirection.BottomRight => new Vector2(hw, -hh),
                TailDirection.LeftCenter => new Vector2(-hw, 0f),
                TailDirection.RightCenter => new Vector2(hw, 0f),
                TailDirection.TopCenter => new Vector2(0f, hh),
                TailDirection.TopLeft => new Vector2(-hw, hh),
                TailDirection.TopRight => new Vector2(hw, hh),
                _ => Vector2.zero
            };

            // Default tail points down = −90° in math. Δ = snapped − (−90°) = snapped + 90°
            tailImage.localRotation = Quaternion.Euler(0f, 0f, snapped + 90f);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Screen / coordinate helpers
        // ─────────────────────────────────────────────────────────────────────

        private Vector2 ClampBubbleToScreen(float x, float y, float padding)
        {
            RectTransform sizeRef = backGroundText != null ? backGroundText : bubbleRect;
            if (sizeRef == null) return new Vector2(x, y);

            float halfW = sizeRef.rect.width * 0.5f;
            float halfH = sizeRef.rect.height * 0.5f;

            x = Mathf.Clamp(x, padding + halfW, Screen.width - padding - halfW);
            y = Mathf.Clamp(y, padding + halfH, Screen.height - padding - halfH);

            return new Vector2(x, y);
        }

        private Vector2 WorldToScreenPoint(Vector3 worldPos) =>
            _renderCam != null
                ? (Vector2)_renderCam.WorldToScreenPoint(worldPos)
                : Vector2.zero;

        private bool IsTargetLowerHalfScreen(Vector2 screenPos) =>
            screenPos.y < Screen.height * 0.5f;

        private Vector2 RectTransformToScreenPoint(RectTransform rt)
        {
            if (rt == null) return Vector2.zero;

            if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return new Vector2(rt.position.x, rt.position.y);

            var cam = _renderCam != null ? _renderCam : Camera.main;
            return cam != null
                ? (Vector2)cam.WorldToScreenPoint(rt.position)
                : new Vector2(rt.position.x, rt.position.y);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Visibility helpers
        // ─────────────────────────────────────────────────────────────────────

        private void SetBubbleVisible(bool v) { if (bubbleContainer != null) bubbleContainer.SetActive(v); }
        private void SetTailVisible(bool v) { if (tailImage != null) tailImage.gameObject.SetActive(v); }

        private void Dismiss()
        {
            Typewriter?.ContentWillDismiss();

            if (bubbleCanvasGroup != null)
                bubbleCanvasGroup.alpha = 0f;

            buttonHandler?.OnLineDismiss();
            SetBubbleVisible(false);
            SetTailVisible(false);

            Typewriter?.ContentDidDismiss();
        }

        /// <summary>
        /// LineAdvancer's `separateHurryUpAndAdvanceControls` field is private with
        /// no public accessor, and LineAdvancer.cs cannot be modified. Reflection
        /// is used here purely to read that flag.
        /// </summary>
        private static bool GetSeparateHurryUpAndAdvanceControls(LineAdvancer advancer)
        {
            var field = typeof(LineAdvancer).GetField(
                "separateHurryUpAndAdvanceControls",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field == null)
            {
                Debug.LogWarning($"{nameof(BubblePresenter)}: could not find LineAdvancer's " +
                    $"'separateHurryUpAndAdvanceControls' field via reflection; assuming false.");
                return false;
            }

            return (bool)field.GetValue(advancer);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Runtime LineAdvancer enable/disable
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Enables this presenter's use of a LineAdvancer at runtime.</summary>
        public void EnableLineAdvancer() => SetLineAdvancerEnabled(true);

        /// <summary>Disables this presenter's use of a LineAdvancer at runtime (reference kept, just unhooked).</summary>
        public void DisableLineAdvancer() => SetLineAdvancerEnabled(false);

        /// <summary>Sets whether this presenter uses a LineAdvancer. Safe to call any time, including mid-line.</summary>
        public void SetLineAdvancerEnabled(bool enabled)
        {
            useLineAdvancer = enabled;
            ApplyLineAdvancerState();
        }

        /// <summary>Current runtime enabled state.</summary>
        public bool IsLineAdvancerEnabled => useLineAdvancer;

        /// <summary>
        /// Resolves, wires, or unwires the LineAdvancer based on useLineAdvancer.
        /// Called from Awake() and whenever SetLineAdvancerEnabled() runs.
        /// </summary>
        private void ApplyLineAdvancerState()
        {
            // Unhook previous handler first to avoid double-adding or stale references.
            if (_activeLineAdvancer != null)
            {
                Typewriter?.ActionMarkupHandlers.Remove(_activeLineAdvancer);
                _activeLineAdvancer = null;
            }

            if (!useLineAdvancer)
            {
                buttonHandler?.SetLineAdvancer(null);
                return;
            }

            // Auto-find only if nothing was assigned in the Inspector.
            if (lineAdvancer == null)
                lineAdvancer = GetComponentInChildren<LineAdvancer>();
            if (lineAdvancer == null)
                lineAdvancer = GetComponentInParent<LineAdvancer>();

            _activeLineAdvancer = lineAdvancer;
            buttonHandler?.SetLineAdvancer(_activeLineAdvancer);

            // If using Text Animator as the Custom typewriter: its actionTags list must
            // include "pause" (default does) or line-progression handlers won't fire mid-line.
            if (_activeLineAdvancer != null && !GetSeparateHurryUpAndAdvanceControls(_activeLineAdvancer))
                Typewriter?.ActionMarkupHandlers.Add(_activeLineAdvancer);
        }
    }
}