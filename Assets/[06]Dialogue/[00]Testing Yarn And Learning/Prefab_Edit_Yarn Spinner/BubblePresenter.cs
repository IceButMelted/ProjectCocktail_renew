/*
 * BubblePresenter.cs
 * Based on LinePresenter.cs from YarnSpinner-Unity (current branch)
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
 *           └── Tail
 *
 * Inspector: assign BubbleContainer RectTransform to "Bubble Rect".
 *
 * ── MODES ────────────────────────────────────────────────────────────────────
 *
 *  MODE A  useWorldTargetAlignment = false
 *    BubbleContainer stays in place (anchoredPosition unchanged).
 *    Only the alignment label (Left/Center/Right) is resolved from
 *    the character name — used to pick which side the tail sits on.
 *
 *  MODE B  useWorldTargetAlignment = true
 *    BubbleContainer is moved so it floats ABOVE the speaker's
 *    world-space Transform, converted to Canvas local space.
 *    Alignment (Left/Center/Right) is derived from the target's
 *    screen X position and controls where the tail base sits.
 *    BubbleContainer anchor/pivot are NEVER changed at runtime.
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

        public enum BubbleAlignment { Left, Center, Right }

        // ─────────────────────────────────────────────────────────────────────
        // Inspector
        // ─────────────────────────────────────────────────────────────────────

        [Header("Bubble References")]
        [Tooltip("BubbleContainer RectTransform (middle/center, anchor 0.5,0.5).\n" +
                 "In Mode B its anchoredPosition is moved above the world target.\n" +
                 "Its anchor and pivot are never changed at runtime.")]
        [SerializeField] private RectTransform bubbleRect;

        [Tooltip("TMP_Text for the dialogue line body.")]
        [SerializeField] private TMP_Text lineText;

        [Tooltip("(Optional) TMP_Text for the character name label.")]
        [SerializeField] private TMP_Text characterNameText;

        [Tooltip("Root GameObject shown/hidden per line (Bubble Presenter itself, " +
                 "or BubbleContainer — whichever you prefer to toggle).")]
        [SerializeField] private GameObject bubbleContainer;

        [Header("Tail / Pointer")]
        [Tooltip("RectTransform of the Tail image (child of BubbleContainer).\n" +
                 "Sprite should point DOWNWARD naturally.\n" +
                 "Set its pivot to (0.5, 1) — top-centre.")]
        [SerializeField] private RectTransform tailImage;

        [Tooltip("Horizontal inset from the bubble left/right edge for the tail base (canvas px).")]
        [SerializeField] private float tailEdgeInset = 24f;

        [Header("Typewriter")]
        [SerializeField] private bool useTypewriterEffect = true;
        [SerializeField] private float typewriterSpeed = 60f;

        [Header("Input Handler")]
        [Tooltip("Auto-found in children if left empty.")]
        [SerializeField] private BubblePresenterButtonHandler buttonHandler;

        [Header("Alignment Mode")]
        [Tooltip("OFF → character-name rules, bubble stays put (Mode A).\n" +
                 "ON  → bubble moves above the speaker's 3-D Transform (Mode B).")]
        [SerializeField] private bool useWorldTargetAlignment = false;

        [Tooltip("Canvas-pixel gap between the bubble bottom and the projected target point.")]
        [SerializeField] private float bubbleAboveTargetOffset = 20f;

        [Tooltip("Fallback when the character name is not in the table (Mode A) " +
                 "or no world target is found (Mode B).")]
        [SerializeField] private BubbleAlignment defaultAlignment = BubbleAlignment.Center;

        [Header("World Target Mapping  (Mode B only)")]
        [Tooltip("One entry per speaking character. Name must match .yarn exactly.")]
        [SerializeField] private List<CharacterTarget> characterTargets = new();

        [Tooltip("Half-width of the \'center\' zone as a fraction of screen width.\n" +
                 "0.10 = target between 40 %–60 % of screen width → Center alignment.")]
        [Range(0.01f, 0.49f)]
        [SerializeField] private float centerZoneFraction = 0.10f;

        [Header("Bubble Horizontal Shift  (Mode B)")]
        [Tooltip("When the target is in the LEFT zone (0 %–leftShiftThreshold %), " +
                 "the bubble shifts RIGHT by this many screen pixels " +
                 "so it doesn\'t overlap the edge.")]
        [SerializeField] private float edgeShiftAmount = 80f;

        [Tooltip("Normalised screen X below which the bubble shifts RIGHT. Default 0.30 = 30 %.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float leftEdgeThreshold = 0.30f;

        [Tooltip("Normalised screen X above which the bubble shifts LEFT. Default 0.70 = 70 %.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float rightEdgeThreshold = 0.70f;

        [Header("Fallback Position  (Mode B, no target found)")]
        [Tooltip("Normalised screen position (0–1) used when no world target exists.\n" +
                 "(0.5, 0.5) = screen centre.  Ignored in Mode A.")]
        [SerializeField] private Vector2 fallbackScreenPointNorm = new Vector2(0.5f, 0.5f);

        [Tooltip("(Optional) Assign a Transform whose screen position is used as the " +
                 "fallback instead of fallbackScreenPointNorm.\n" +
                 "Leave empty to use the normalised value above.")]
        [SerializeField] private Transform fallbackTargetTransform;

        // ─────────────────────────────────────────────────────────────────────
        // Runtime
        // ─────────────────────────────────────────────────────────────────────

        private Dictionary<string, Transform> _targetLookup = new();
        private Canvas   _canvas;
        private Camera   _renderCam;

        // ─────────────────────────────────────────────────────────────────────
        // Unity
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            SetBubbleVisible(false);
            SetTailVisible(false);

            if (buttonHandler == null)
                buttonHandler = GetComponentInChildren<BubblePresenterButtonHandler>();

            _targetLookup.Clear();
            foreach (var e in characterTargets)
                if (!string.IsNullOrEmpty(e.characterName) && e.worldTarget != null)
                    _targetLookup[e.characterName] = e.worldTarget;

            _canvas    = GetComponentInParent<Canvas>();
            _renderCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                         ? _canvas.worldCamera
                         : Camera.main;
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

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            buttonHandler?.OnLineBegin();

            string characterName = line.CharacterName ?? string.Empty;

            // ── Resolve alignment & position ──────────────────────────────
            BubbleAlignment alignment;

            if (useWorldTargetAlignment)
            {
                bool foundTarget = _targetLookup.TryGetValue(characterName, out Transform worldTarget)
                                   && worldTarget != null;

                Vector2 screenPos;

                if (foundTarget)
                {
                    // ── MODE B — target found ─────────────────────────────
                    screenPos = WorldToScreenPoint(worldTarget.position);
                }
                else
                {
                    // ── MODE B — no target → use fallback position ────────
                    if (fallbackTargetTransform != null)
                    {
                        screenPos = WorldToScreenPoint(fallbackTargetTransform.position);
                    }
                    else
                    {
                        // Normalised → pixel
                        screenPos = new Vector2(
                            fallbackScreenPointNorm.x * Screen.width,
                            fallbackScreenPointNorm.y * Screen.height);
                    }
                }

                // Derive tail alignment from screen X
                alignment = ScreenXToAlignment(screenPos.x);

                // Compute horizontal shift so bubble avoids screen edges
                float shiftX = ComputeEdgeShift(screenPos.x);

                if (bubbleRect != null)
                {
                    bubbleRect.position = new Vector3(
                        screenPos.x + shiftX,
                        screenPos.y + bubbleAboveTargetOffset,
                        0f
                    );
                }

                PositionTail(alignment);
                SetTailVisible(true);
            }
            else
            {
                // ── MODE A ────────────────────────────────────────────────
                alignment = ResolveNameAlignment(characterName);
                SetTailVisible(false);
            }

            // ── Character name label ──────────────────────────────────────
            if (characterNameText != null)
                characterNameText.text = characterName;

            // ── Show bubble ───────────────────────────────────────────────
            string lineBody = line.TextWithoutCharacterName.Text;
            SetBubbleVisible(true);

            // ── Typewriter ────────────────────────────────────────────────
            if (useTypewriterEffect && lineText != null)
            {
                lineText.text = lineBody;
                lineText.maxVisibleCharacters = 0;
                int total = lineBody.Length;

                for (int i = 0; i <= total; i++)
                {
                    if (token.IsNextLineRequested)
                    {
                        lineText.maxVisibleCharacters = total;
                        Dismiss(); return;
                    }

                    if (token.IsHurryUpRequested ||
                        (buttonHandler != null && buttonHandler.IsHurryUpRequested))
                        break;

                    lineText.maxVisibleCharacters = i;
                    float delay = typewriterSpeed > 0f ? 1f / typewriterSpeed : 0f;
                    await YarnTask.Delay(TimeSpan.FromSeconds(delay), token.HurryUpToken)
                                  .SuppressCancellationThrow();
                }
                lineText.maxVisibleCharacters = total;
            }
            else if (lineText != null)
            {
                lineText.maxVisibleCharacters = int.MaxValue;
                lineText.text = lineBody;
            }

            // ── Wait for input ────────────────────────────────────────────
            buttonHandler?.OnTypewriterComplete();

            while (!token.IsNextLineRequested)
            {
                if (buttonHandler != null && buttonHandler.IsAdvanceRequested) break;
                await YarnTask.Delay(TimeSpan.FromSeconds(0), token.NextLineToken)
                              .SuppressCancellationThrow();
            }

            Dismiss();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Tail
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Places the tail at the bottom of BubbleContainer.
        /// Tail pivot = (0.5, 1) means its top-centre anchors to the position,
        /// and it hangs downward from there.
        ///
        ///   Left   → tail base near bottom-left  corner (inset by tailEdgeInset)
        ///   Center → tail base at bottom-centre
        ///   Right  → tail base near bottom-right corner (inset by tailEdgeInset)
        ///
        /// The tail is a child of BubbleContainer, so these are local coordinates.
        /// anchorMin/Max (0.5, 0) = horizontally centred, vertically at bottom of parent.
        /// </summary>
        private void PositionTail(BubbleAlignment alignment)
        {
            if (tailImage == null || bubbleRect == null) return;

            // Anchor to bottom of BubbleContainer, pivot top-centre
            tailImage.anchorMin = new Vector2(0.5f, 0f);
            tailImage.anchorMax = new Vector2(0.5f, 0f);
            tailImage.pivot     = new Vector2(0.5f, 1f);

            float halfW = bubbleRect.rect.width * 0.5f;

            float localX = alignment switch
            {
                BubbleAlignment.Left   => -halfW + tailEdgeInset,
                BubbleAlignment.Center =>  0f,
                BubbleAlignment.Right  =>  halfW - tailEdgeInset,
                _                      =>  0f
            };

            // Y = 0 → sits exactly on the bottom edge of BubbleContainer
            tailImage.anchoredPosition = new Vector2(localX, 0f);
            tailImage.localRotation    = Quaternion.identity; // sprite points down naturally
        }

        // ─────────────────────────────────────────────────────────────────────
        // Alignment helpers
        // ─────────────────────────────────────────────────────────────────────

        private BubbleAlignment ResolveNameAlignment(string name) => name switch
        {
            "Player"   => BubbleAlignment.Left,
            "Narrator" => BubbleAlignment.Center,
            "NPC"      => BubbleAlignment.Right,
            _          => defaultAlignment
        };

        private BubbleAlignment ScreenXToAlignment(float screenX)
        {
            float nx = screenX / Screen.width;
            if (nx < 0.5f - centerZoneFraction) return BubbleAlignment.Left;
            if (nx > 0.5f + centerZoneFraction) return BubbleAlignment.Right;
            return BubbleAlignment.Center;
        }

        /// <summary>
        /// Returns a horizontal pixel offset to shift the bubble away from
        /// the screen edge so it stays readable.
        ///
        ///   target in  0 % – leftEdgeThreshold  (default 30 %) → shift RIGHT (+edgeShiftAmount)
        ///   target in  rightEdgeThreshold – 100 % (default 70 %) → shift LEFT  (-edgeShiftAmount)
        ///   target in  30 % – 70 %  → no shift (0)
        /// </summary>
        private float ComputeEdgeShift(float screenX)
        {
            float nx = screenX / Screen.width;
            if (nx < leftEdgeThreshold)  return +edgeShiftAmount;   // near left  → push right
            if (nx > rightEdgeThreshold) return -edgeShiftAmount;   // near right → push left
            return 0f;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Coordinate helpers
        // ─────────────────────────────────────────────────────────────────────

        private Vector2 WorldToScreenPoint(Vector3 worldPos) =>
            _renderCam != null
                ? (Vector2)_renderCam.WorldToScreenPoint(worldPos)
                : Vector2.zero;

        // ─────────────────────────────────────────────────────────────────────
        // Visibility helpers
        // ─────────────────────────────────────────────────────────────────────

        private void SetBubbleVisible(bool v)
        {
            if (bubbleContainer != null) bubbleContainer.SetActive(v);
        }

        private void SetTailVisible(bool v)
        {
            if (tailImage != null) tailImage.gameObject.SetActive(v);
        }

        private void Dismiss()
        {
            buttonHandler?.OnLineDismiss();
            SetBubbleVisible(false);
            SetTailVisible(false);
        }
    }
}
