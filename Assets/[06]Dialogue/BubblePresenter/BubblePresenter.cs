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
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Yarn.Unity;

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

        // ─────────────────────────────────────────────────────────────────────
        // Inspector
        // ─────────────────────────────────────────────────────────────────────

        [Header("Bubble References")]
        [SerializeField] private RectTransform bubbleRect;
        [Tooltip("Background GameObject to calculate Size")]
        [SerializeField] private RectTransform backGroundText;
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private TMP_Text characterNameText;
        private string _lastCharacterName;
        [SerializeField] private GameObject bubbleContainer;

        [Header("Fade")]
        [SerializeField] private CanvasGroup bubbleCanvasGroup;
        [SerializeField] private bool useFadeEffect = true;
        [SerializeField] private float fadeUpDuration = 0.25f;
        [SerializeField] private float fadeDownDuration = 0.1f;

        [Header("Tail / Pointer")]
        [SerializeField] private RectTransform tailImage;

        [Header("Typewriter")]
        [SerializeField] private bool useTypewriterEffect = true;
        [SerializeField] private float typewriterSpeed = 60f;

        /// <summary>
        /// The typewriter used to display this line's text, implementing the
        /// same <see cref="IAsyncTypewriter"/> interface that the built-in
        /// LinePresenter uses (LetterTypewriter / InstantTypewriter / your
        /// own custom typewriter component all conform to it).
        /// Exposing this is what lets a LineAdvancer-aware button handler
        /// (or anything else) know "is the line fully shown yet" the same
        /// way LinePresenterButtonHandler does for LinePresenter — by
        /// checking IAsyncTypewriter completion rather than BubblePresenter
        /// having to track typing progress itself.
        /// </summary>
        public IAsyncTypewriter Typewriter { get; private set; }

        [Header("Input Handler")]
        [Tooltip("Auto-found in children if left empty.")]
        [SerializeField] private BubblePresenterButtonHandler buttonHandler;
        [SerializeField] private bool useLineAdvancer = true;
        private LineAdvancer _activeLineAdvancer;

        [SerializeField] private LineAdvancer lineAdvancer;

        [Header("World Target Mapping")]
        [SerializeField] private List<CharacterTarget> characterTargets = new();
        [SerializeField] private float bubbleAboveTargetOffset = 20f;
        [SerializeField] private float bubbleBelowTargetOffset = 20f;

        [Header("Screen Boundary Clamping")]
        [SerializeField] private float screenBorderPadding = 50f;

        [Header("Fallback Canvas Anchor  (no target found)")]
        [SerializeField] private RectTransform fallbackCanvasAnchor;
        [SerializeField] private float fallbackScreenBorderPadding = 50f;


        private bool _targetFound;
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

            // Initialise alpha so first fade-in starts from zero
            if (bubbleCanvasGroup != null)
                bubbleCanvasGroup.alpha = 0f;

            if (buttonHandler == null)
                buttonHandler = GetComponentInChildren<BubblePresenterButtonHandler>();

            // Build the IAsyncTypewriter that will run this presenter's
            // typewriter effect. This mirrors LinePresenter's "Letter By
            // Letter" typewriter mode (see LetterTypewriter in YarnSpinner-
            // Unity). Speed of 0 / disabled means deliver everything
            // instantly via CharactersPerSecond = 0f.
            Typewriter = new LetterTypewriter
            {
                TextElement = lineText,
                CharactersPerSecond = useTypewriterEffect ? typewriterSpeed : 0f,
            };

            ApplyLineAdvancerState();

            buttonHandler?.SetLineAdvancer(lineAdvancer);

            if (lineAdvancer != null && !GetSeparateHurryUpAndAdvanceControls(lineAdvancer))
                Typewriter.ActionMarkupHandlers.Add(lineAdvancer);

            buttonHandler?.SetLineAdvancer(lineAdvancer);

            if (lineAdvancer != null && !GetSeparateHurryUpAndAdvanceControls(lineAdvancer))
                Typewriter.ActionMarkupHandlers.Add(lineAdvancer);

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

        private void LateUpdate()
        {
            if (_lastTargetPos != null)
            {
                Vector2 targetScreen = WorldToScreenPoint(_lastTargetPos.position);
                bool targetIsLowerHalf = IsTargetLowerHalfScreen(targetScreen);
                //float shiftY = backGroundText != null
                //    ? bubbleAboveTargetOffset + backGroundText.rect.height * 0.5f
                //    : bubbleAboveTargetOffset;

                if (bubbleRect != null)
                {
                    //Vector2 clamped = ClampBubbleToScreen(targetScreen.x, targetScreen.y + shiftY, screenBorderPadding);
                    Vector2 clamped = ClampBubbleToScreen(targetScreen.x, targetScreen.y, screenBorderPadding);
                    bubbleRect.position = new Vector3(clamped.x, clamped.y, 0f);
                    //bubbleRect.position = new Vector3(targetScreen.x, targetScreen.y, 0f);

                    //Decide to shift bubble above target if target is in lower half of screen, to reduce chance of tail being off-screen
                    if (targetIsLowerHalf)
                        bubbleRect.position += new Vector3(0f, bubbleAboveTargetOffset, 0f);
                    else
                        bubbleRect.position -= new Vector3(0f, bubbleBelowTargetOffset, 0f);

                    // Only show tail when target is actually visible on screen
                    if (IsTargetOnScreen(targetScreen))
                    {
                        RotateTailForDirection(bubbleRect.position, targetScreen);
                        SetTailVisible(false);
                    }
                    else
                        SetTailVisible(false);
                }
            }
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

        /// <summary>
        /// Requests that the current line be hurried up (skip the typewriter
        /// straight to full text). Forwards to the assigned LineAdvancer, which
        /// in turn tells the DialogueRunner; this presenter's RunLineAsync loop
        /// observes that via LineCancellationToken.IsHurryUpRequested.
        /// </summary>
        public void RequestHurryUpLine() => lineAdvancer?.RequestLineHurryUp();

        /// <summary>
        /// Requests that dialogue advance to the next line. Forwards to the
        /// assigned LineAdvancer, which in turn tells the DialogueRunner; this
        /// presenter's RunLineAsync loop observes that via
        /// LineCancellationToken.IsNextContentRequested.
        /// </summary>
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


            // init vairables Zone
            //check for last character name to use as indicator for bubble animation
            bool IsNewCharacter = _lastCharacterName != line.CharacterName;

            string currentCharacterName = line.CharacterName ?? string.Empty;
            if (_lastCharacterName != currentCharacterName)
                _lastCharacterName = currentCharacterName;

            //Reset Line Text Preventing Ghost Text
            lineText.text = string.Empty;


            // ── Resolve position & tail ───────────────────────────────────────
            bool foundTarget = _targetLookup.TryGetValue(currentCharacterName, out Transform worldTarget)
                                && worldTarget != null;

            if (foundTarget)
                _lastTargetPos = worldTarget;
            else
                _lastTargetPos = null;

            //Resize bubble to fit text [Here]

            //Place bubble above target, or fallback anchor if no target found
            if (foundTarget)
            {
                Vector2 targetScreen = WorldToScreenPoint(worldTarget.position);
                bool targetIsLowerHalf = IsTargetLowerHalfScreen(targetScreen);
                //float shiftY = backGroundText != null
                //    ? bubbleAboveTargetOffset + backGroundText.rect.height * 0.5f
                //    : bubbleAboveTargetOffset;

                if (bubbleRect != null)
                {
                    //Vector2 clamped = ClampBubbleToScreen(targetScreen.x, targetScreen.y + shiftY, screenBorderPadding);
                    Vector2 clamped = ClampBubbleToScreen(targetScreen.x, targetScreen.y, screenBorderPadding);
                    bubbleRect.position = new Vector3(clamped.x, clamped.y, 0f);
                    //bubbleRect.position = new Vector3(targetScreen.x, targetScreen.y, 0f);

                    //Decide to shift bubble above target if target is in lower half of screen, to reduce chance of tail being off-screen
                    if (targetIsLowerHalf)
                        bubbleRect.position += new Vector3(0f, bubbleAboveTargetOffset, 0f);
                    else
                        bubbleRect.position -= new Vector3(0f, bubbleBelowTargetOffset, 0f);

                    // Only show tail when target is actually visible on screen
                    if (IsTargetOnScreen(targetScreen))
                    {
                        RotateTailForDirection(bubbleRect.position, targetScreen);
                        SetTailVisible(false);
                    }
                    else
                        SetTailVisible(false);
                }
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
            // Mirrors LinePresenter.RunLineAsync in YarnSpinner-Unity: hand the
            // line off to an IAsyncTypewriter and await it, instead of hand-
            // rolling a char-by-char loop. token.HurryUpToken is the same
            // CancellationToken that LineAdvancer.RequestLineHurryUp() (and
            // therefore BubblePresenterButtonHandler's click-while-typing
            // case) cancels — so RunTypewriter below returns the moment a
            // hurry-up is requested from ANY source (button, keyboard,
            // gamepad), and just falls through to showing full text.
            if (Typewriter != null && lineText != null)
            {
                Typewriter.PrepareForContent(line.TextWithoutCharacterName);

                if (useTypewriterEffect)
                {
                    await Typewriter
                        .RunTypewriter(line.TextWithoutCharacterName, token.HurryUpToken)
                        .SuppressCancellationThrow();
                }

                // Whether we finished normally or were hurried up, make sure
                // every character is visible before we move on.
                lineText.maxVisibleCharacters = int.MaxValue;
            }
            else if (lineText != null)
            {
                lineText.maxVisibleCharacters = int.MaxValue;
                lineText.text = lineBody;
            }

            // If the player already requested "next" while the typewriter
            // was running (e.g. a fast double-tap), skip straight out.
            // Dismiss() below already calls buttonHandler.OnLineDismiss().
            if (token.IsNextContentRequested)
            {
                Dismiss();
                return;
            }

            // ── Wait for input ────────────────────────────────────────────────
            // The typewriter has finished (or was hurried up to completion) —
            // this is the "is the line fully shown yet" moment that
            // BubblePresenterButtonHandler.IsWaitingForInput tracks, and that
            // a LineAdvancer with "Separate Hurry Up And Advance Controls"
            // needs in order to choose RequestLineHurryUp() vs RequestNextLine().
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

            if (IsNewCharacter)
                Dismiss();
        }

        // ─────────────────────────────────────────────────────────────────────
        // helpler
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns true if the screen-pixel point is within screen bounds.</summary>
        private bool IsTargetOnScreen(Vector2 screenPos) =>
            screenPos.x >= 0 && screenPos.x <= Screen.width &&
            screenPos.y >= 0 && screenPos.y <= Screen.height;

        /// <summary>Returns true if screenPos is on the right half of the screen.</summary>
        private bool IsTargetOnRightSide(Vector2 screenPos) =>
            screenPos.x > Screen.width * 0.5f;

        /// <summary>
        /// Snaps the direction from bubbleScreenPos → targetScreenPos to one of 8
        /// compass directions, then positions and rotates the tail accordingly.
        /// Tail sprite default orientation = pointing DOWN (rotation identity).
        /// </summary>
        private void RotateTailForDirection(Vector2 bubbleScreenPos, Vector2 targetScreenPos)
        {
            if (tailImage == null || bubbleRect == null) return;

            // Direction from bubble centre to target
            Vector2 dir = targetScreenPos - bubbleScreenPos;

            // Snap angle to nearest 45° step
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;  // standard math angle
            float snapped = Mathf.Round(angle / 45f) * 45f;             // −180 … +180, multiples of 45

            // Map snapped angle → TailDirection
            TailDirection td = snapped switch
            {
                -90f => TailDirection.BottomCenter,
                -45f => TailDirection.BottomRight,
                0f => TailDirection.RightCenter,
                45f => TailDirection.TopRight,
                90f => TailDirection.TopCenter,
                135f => TailDirection.TopLeft,
                // ±180° both map to left
                float a when Mathf.Abs(a) == 180f => TailDirection.LeftCenter,
                -135f => TailDirection.BottomLeft,
                _ => TailDirection.BottomCenter
            };

            // Place tail at the matching edge/corner of the bubble (anchor = bubble centre)
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

            // Rotate tail so its tip points toward the target.
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
        private Boolean IsTargetLowerHalfScreen(Vector2 screenPos) =>
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
        /// is used here purely to read that flag at Awake() time.
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


        /// ─────────────────────────────────────────────────────────────────────
        /// Helpes for line advancer
        // / ─────────────────────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────
        // Runtime LineAdvancer enable/disable
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Enables this presenter's use of a LineAdvancer at runtime. If no
        /// LineAdvancer is assigned in the Inspector, one will be auto-found in
        /// children/parent, same as at startup.
        /// </summary>
        public void EnableLineAdvancer() => SetLineAdvancerEnabled(true);

        /// <summary>
        /// Disables this presenter's use of a LineAdvancer at runtime. The
        /// assigned LineAdvancer reference is kept (not cleared) so it can be
        /// re-enabled later; it is simply unhooked from the typewriter and the
        /// button handler, and RequestHurryUpLine()/RequestNextDialogueLine()
        /// become no-ops until re-enabled.
        /// </summary>
        public void DisableLineAdvancer() => SetLineAdvancerEnabled(false);

        /// <summary>
        /// Sets whether this presenter uses a LineAdvancer at runtime.
        /// Safe to call at any time, including mid-line.
        /// </summary>
        public void SetLineAdvancerEnabled(bool enabled)
        {
            useLineAdvancer = enabled;
            ApplyLineAdvancerState();
        }

        /// <summary>Current runtime enabled state.</summary>
        public bool IsLineAdvancerEnabled => useLineAdvancer;

        /// <summary>
        /// Resolves, wires, or unwires the LineAdvancer based on the current
        /// value of useLineAdvancer. Called from Awake() and whenever
        /// SetLineAdvancerEnabled() is used at runtime.
        /// </summary>
        private void ApplyLineAdvancerState()
        {
            // Unhook whatever was previously active first, to avoid double-adding
            // or leaving a stale handler on the typewriter.
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

            if (_activeLineAdvancer != null && !GetSeparateHurryUpAndAdvanceControls(_activeLineAdvancer))
                Typewriter?.ActionMarkupHandlers.Add(_activeLineAdvancer);
        }
    }
}