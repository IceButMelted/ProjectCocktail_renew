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
 *    • TARGET FOUND → BubbleContainer moves above the speaker's world Transform.
 *    • TARGET NOT FOUND → BubbleContainer moves to the screen position of
 *      fallbackCanvasAnchor (a plain RectTransform you place anywhere on the
 *      Canvas in the Editor). Alignment is still derived from its screen X.
 *
 * ── EDGE SHIFT ───────────────────────────────────────────────────────────────
 *  Base shift  = bubbleRect.rect.width * 0.5 * sideShiftMultiplier
 *  Total shift = base shift + sideShiftExtraPixels
 *  Both values are tunable in the Inspector under "Bubble Edge Shift".
 *
 * ── SCREEN CLAMPING ──────────────────────────────────────────────────────────
 *  After positioning, the bubble is clamped so no edge exits the screen.
 *  A configurable screenBorderPadding (default 50 px) keeps it inset.
 *
 * ── FALLBACK CANVAS ANCHOR SETUP ─────────────────────────────────────────────
 *  1. In your Canvas, create an empty GameObject (e.g. "BubbleFallbackAnchor").
 *  2. Position its RectTransform where you want the default bubble to appear
 *     (e.g. centre-bottom of the screen for a narrator).
 *  3. Assign it to the "Fallback Canvas Anchor" field in the Inspector.
 *  Alignment (Left/Center/Right) is derived from its screen X just like a
 *  world target, and edge-shift logic applies normally.
 *
 * ── TAIL SETUP ───────────────────────────────────────────────────────────────
 *  • Sprite should point DOWNWARD naturally.
 *  • Set Tail RectTransform pivot to (0.5, 1) — top-centre.
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
                 "In Mode B its position is moved above the world target.\n" +
                 "Its anchor and pivot are never changed at runtime.")]
        [SerializeField] private RectTransform bubbleRect;
        [Tooltip("Background GameObject to calculate Size")]
        [SerializeField] private RectTransform backGroundText;

        [Tooltip("TMP_Text for the dialogue line body.")]
        [SerializeField] private TMP_Text lineText;

        [Tooltip("(Optional) TMP_Text for the character name label.")]
        [SerializeField] private TMP_Text characterNameText;

        [Tooltip("Root GameObject shown/hidden per line.")]
        [SerializeField] private GameObject bubbleContainer;


        [Header("Tail / Pointer")]
        [Tooltip("RectTransform of the Tail image (child of BubbleContainer).\n" +
                 "Sprite should point DOWNWARD naturally.\n" +
                 "Set its pivot to (0.5, 1) — top-centre.")]
        [SerializeField] private RectTransform tailImage;

        [Tooltip("Horizontal inset from the bubble left/right edge for the tail base (canvas px).\n" +
                 "e.g. 24 = tail base sits 24 px inside the corner.")]
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

        [Tooltip("Fallback alignment used in Mode A when the character name is not in any rule.")]
        [SerializeField] private BubbleAlignment defaultAlignment = BubbleAlignment.Center;

        [Header("World Target Mapping  (Mode B — target found)")]
        [Tooltip("One entry per speaking character. Name must match .yarn exactly.")]
        [SerializeField] private List<CharacterTarget> characterTargets = new();

        [Tooltip("Half-width of the 'center' zone as a fraction of screen width.\n" +
                 "0.10 = target between 40%–60% of screen width → Center alignment.")]
        [Range(0.01f, 0.49f)]
        [SerializeField] private float centerZoneFraction = 0.10f;

        [Header("Bubble Edge Shift  (Mode B)")]
        [Tooltip("Multiplier applied to (bubbleRect.width * 0.5) to get the base shift amount.\n" +
                 "1.0 = shift by exactly half the bubble width (default).\n" +
                 "Increase to shift more, decrease to shift less.")]
        [Range(0f, 3f)]
        [SerializeField] private float sideShiftMultiplier = 1.0f;

        [Tooltip("Extra flat pixel offset added on top of the multiplied shift.\n" +
                 "Use this to fine-tune without changing the multiplier.")]
        [SerializeField] private float sideShiftExtraPixels = 0f;

        [Tooltip("Normalised screen X BELOW which the bubble shifts RIGHT.\nDefault 0.30 = 30%.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float leftEdgeThreshold = 0.30f;

        [Tooltip("Normalised screen X ABOVE which the bubble shifts LEFT.\nDefault 0.70 = 70%.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float rightEdgeThreshold = 0.70f;

        [Header("Screen Boundary Clamping  (Mode B)")]
        [Tooltip("Minimum distance in screen pixels between any bubble edge and the screen border.\n" +
                 "If the bubble would go outside this margin it is pushed inward.\n" +
                 "Default 50 px.")]
        [SerializeField] private float screenBorderPadding = 50f;

        [Header("Fallback Canvas Anchor  (Mode B — no target found)")]
        [Tooltip("A plain empty RectTransform placed anywhere on the Canvas.\n\n" +
                 "When Mode B is active but no world target exists for the\n" +
                 "speaking character, the bubble is positioned above this\n" +
                 "UI anchor instead of a world Transform.\n\n" +
                 "Setup:\n" +
                 "  1. Create an empty GameObject under your Canvas.\n" +
                 "  2. Set its RectTransform position to where you want\n" +
                 "     the default bubble to appear (e.g. centre-bottom).\n" +
                 "  3. Assign it here.\n\n" +
                 "Alignment (Left/Center/Right) is derived from its screen X,\n" +
                 "and normal edge-shift logic applies.\n\n" +
                 "Leave empty → bubble is not repositioned when no target\n" +
                 "is found (stays at its last known position).")]
        [SerializeField] private RectTransform fallbackCanvasAnchor;

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

            if (buttonHandler == null)
                buttonHandler = GetComponentInChildren<BubblePresenterButtonHandler>();

            _targetLookup.Clear();
            foreach (var e in characterTargets)
                if (!string.IsNullOrEmpty(e.characterName) && e.worldTarget != null)
                    _targetLookup[e.characterName] = e.worldTarget;

            _canvas = GetComponentInParent<Canvas>();
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
                bool shouldReposition;

                if (foundTarget)
                {
                    // ── MODE B — world target found ───────────────────────
                    screenPos = WorldToScreenPoint(worldTarget.position);
                    shouldReposition = true;
                }
                else if (fallbackCanvasAnchor != null)
                {
                    // ── MODE B — no world target → use canvas ref anchor ──
                    //
                    // The fallback anchor is a RectTransform on the Canvas.
                    // RectTransformToScreenPoint converts its position to
                    // screen-pixel coordinates using the same pipeline as
                    // WorldToScreenPoint, so everything downstream is identical.
                    screenPos = RectTransformToScreenPoint(fallbackCanvasAnchor);
                    shouldReposition = true;
                }
                else
                {
                    // ── MODE B — no target AND no fallback anchor ─────────
                    // Skip repositioning; keep bubble at its current position.
                    screenPos = Vector2.zero; // unused
                    shouldReposition = false;
                }

                if (shouldReposition)
                {
                    // Alignment from screen X
                    alignment = ScreenXToAlignment(screenPos.x);

                    // Side shift: (halfWidth * multiplier) + extra pixels
                    float halfBubbleW = bubbleRect != null ? bubbleRect.rect.width * 0.5f : 0f;
                    float edgeShiftAmount = halfBubbleW * sideShiftMultiplier + sideShiftExtraPixels;
                    float shiftX = ComputeEdgeShift(screenPos.x, edgeShiftAmount);

                    float shiftY;
                    if (backGroundText != null)
                        shiftY = bubbleAboveTargetOffset + backGroundText.rect.height * 0.5f;
                    else
                    {
                        shiftY = bubbleAboveTargetOffset;
                        Debug.LogWarning("[BubblePresenter] BackGroundText reference is missing. Using bubbleAboveTargetOffset only.");
                    }

                    if (bubbleRect != null)
                    {
                        float rawX = foundTarget ? screenPos.x + shiftX : screenPos.x;
                        float rawY = screenPos.y + shiftY;

                        // Clamp so no part of the bubble exits the screen
                        Vector2 clamped = ClampBubbleToScreen(rawX, rawY);

                        bubbleRect.position = new Vector3(clamped.x, clamped.y, 0f);
                    }

                    PositionTail(alignment);

                    if (foundTarget)
                        SetTailVisible(true);
                    else
                        SetTailVisible(false);
                }
                else
                {
                    alignment = defaultAlignment;
                    // Tail stays in its last valid state (hidden from previous Dismiss).
                    SetTailVisible(false);
                }
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
                    if (token.IsNextContentRequested)
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

            while (!token.IsNextContentRequested)
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
        /// Tail pivot = (0.5, 1): top-centre anchors to the position and hangs down.
        ///   Left   → near bottom-left  corner (inset by tailEdgeInset)
        ///   Center → bottom-centre
        ///   Right  → near bottom-right corner (inset by tailEdgeInset)
        /// </summary>
        private void PositionTail(BubbleAlignment alignment)
        {
            if (tailImage == null || bubbleRect == null) return;

            tailImage.anchorMin = new Vector2(0.5f, 0f);
            tailImage.anchorMax = new Vector2(0.5f, 0f);
            tailImage.pivot = new Vector2(0.5f, 1f);

            float halfW = bubbleRect.rect.width * 0.5f;
            float halfTailW = tailImage.rect.width * 0.5f;
            float halfTailH = tailImage.rect.height * 0.5f;

            float localX = alignment switch
            {
                //BubbleAlignment.Left => -halfW + tailEdgeInset,
                //BubbleAlignment.Left => -halfW,
                BubbleAlignment.Left => -halfW + halfTailW,
                BubbleAlignment.Center => 0f,
                //BubbleAlignment.Right => halfW - tailEdgeInset,
                //BubbleAlignment.Right => halfW,
                BubbleAlignment.Right => halfW - halfTailW,
                _ => 0f
            };

            //tailImage.anchoredPosition = new Vector2(localX, 0f);
            tailImage.anchoredPosition = new Vector2(localX, halfTailH);
            tailImage.localRotation = Quaternion.identity;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Alignment helpers
        // ─────────────────────────────────────────────────────────────────────

        private BubbleAlignment ResolveNameAlignment(string name) => name switch
        {
            "Player" => BubbleAlignment.Left,
            "Narrator" => BubbleAlignment.Center,
            "NPC" => BubbleAlignment.Right,
            _ => defaultAlignment
        };

        private BubbleAlignment ScreenXToAlignment(float screenX)
        {
            float nx = screenX / Screen.width;
            if (nx < 0.5f - centerZoneFraction) return BubbleAlignment.Left;
            if (nx > 0.5f + centerZoneFraction) return BubbleAlignment.Right;
            return BubbleAlignment.Center;
        }

        /// <summary>
        /// Returns a horizontal pixel offset to keep the bubble away from screen edges.
        ///   Left zone  (nx &lt; leftEdgeThreshold)  → shift RIGHT  (+edgeShiftAmount)
        ///   Right zone (nx &gt; rightEdgeThreshold) → shift LEFT   (-edgeShiftAmount)
        ///   Centre                                  → no shift    (0)
        ///
        /// edgeShiftAmount = (bubbleRect.width * 0.5 * sideShiftMultiplier) + sideShiftExtraPixels
        /// </summary>
        private float ComputeEdgeShift(float screenX, float edgeShiftAmount)
        {
            float nx = screenX / Screen.width;
            if (nx < leftEdgeThreshold) return +edgeShiftAmount;
            if (nx > rightEdgeThreshold) return -edgeShiftAmount;
            return 0f;
        }

        /// <summary>
        /// Clamps the bubble's screen-space position so that no edge of bubbleRect
        /// goes outside the screen minus screenBorderPadding.
        ///
        /// bubbleRect.position is the CENTRE of the rect (pivot 0.5, 0.5).
        /// So half-width and half-height are subtracted/added to find edges.
        ///
        /// Uses backGroundText.rect for size when available (more accurate than
        /// bubbleRect which may include invisible padding), falls back to bubbleRect.
        /// </summary>
        private Vector2 ClampBubbleToScreen(float x, float y)
        {
            // Use the visual background size for accurate edge calculation
            RectTransform sizeRef = backGroundText != null ? backGroundText : bubbleRect;
            if (sizeRef == null) return new Vector2(x, y);

            float halfW = sizeRef.rect.width * 0.5f;
            float halfH = sizeRef.rect.height * 0.5f;

            float pad = screenBorderPadding;

            // Horizontal clamp
            float minX = pad + halfW;
            float maxX = Screen.width - pad - halfW;
            x = Mathf.Clamp(x, minX, maxX);

            // Vertical clamp
            float minY = pad + halfH;
            float maxY = Screen.height - pad - halfH;
            y = Mathf.Clamp(y, minY, maxY);

            return new Vector2(x, y);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Coordinate helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>World → screen-pixel position via the canvas render camera.</summary>
        private Vector2 WorldToScreenPoint(Vector3 worldPos) =>
            _renderCam != null
                ? (Vector2)_renderCam.WorldToScreenPoint(worldPos)
                : Vector2.zero;

        /// <summary>
        /// Converts a Canvas RectTransform's position to screen-pixel coordinates.
        ///
        /// ScreenSpaceOverlay  → rt.position IS already in screen pixels.
        /// Camera / WorldSpace → project rt.position through the render camera.
        ///
        /// This makes fallbackCanvasAnchor use the exact same positioning pipeline
        /// as a world-space character target, regardless of canvas render mode.
        /// </summary>
        private Vector2 RectTransformToScreenPoint(RectTransform rt)
        {
            if (rt == null) return Vector2.zero;

            if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // position is already in screen-pixel space
                return new Vector2(rt.position.x, rt.position.y);
            }

            // Camera-space or World-space canvas: project through the render camera
            var cam = _renderCam != null ? _renderCam : Camera.main;
            return cam != null
                ? (Vector2)cam.WorldToScreenPoint(rt.position)
                : new Vector2(rt.position.x, rt.position.y);
        }

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