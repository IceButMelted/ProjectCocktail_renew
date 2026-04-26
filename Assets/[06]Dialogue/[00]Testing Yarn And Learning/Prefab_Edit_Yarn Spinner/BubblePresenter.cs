/*
 * BubblePresenter.cs
 * Based on LinePresenter.cs from YarnSpinner-Unity (current branch)
 *
 * The "Bubble Presenter" GameObject itself does NOT move.
 * Instead, a separate child RectTransform ("bubbleContentRect") is shifted
 * left / center / right by changing its anchorMin/Max and pivot, then
 * resetting anchoredPosition to 0 so it snaps flush.
 *
 * Assign in Inspector:
 *   bubbleRect        → the child RectTransform to reposition (e.g. Background,
 *                        or a "BubbleContent" wrapper that holds Background + Text)
 *   NOT the root Bubble Presenter RectTransform itself.
 *
 * Character alignment:
 *   "Player"   → LEFT   (anchor/pivot X = 0)
 *   "Narrator" → CENTER (anchor/pivot X = 0.5)
 *   "NPC"      → RIGHT  (anchor/pivot X = 1)
 */

using TMPro;
using UnityEngine;
using Yarn.Unity;

namespace YarnSpinner.Custom
{
    public class BubblePresenter : DialoguePresenterBase
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Bubble References")]
        [Tooltip("The CHILD RectTransform to reposition left/center/right " +
                 "(e.g. Background, or a BubbleContent wrapper). " +
                 "Do NOT assign the root Bubble Presenter RectTransform here.")]
        [SerializeField] private RectTransform bubbleRect;

        [Tooltip("TextMeshPro label that shows the dialogue line text.")]
        [SerializeField] private TMP_Text lineText;

        [Tooltip("(Optional) TextMeshPro label that shows the character name.")]
        [SerializeField] private TMP_Text characterNameText;

        [Tooltip("Root GameObject of the whole bubble — shown/hidden per line. " +
                 "Usually the Bubble Presenter GameObject itself.")]
        [SerializeField] private GameObject bubbleContainer;

        [Header("Typewriter")]
        [Tooltip("Enable typewriter effect.")]
        [SerializeField] private bool useTypewriterEffect = true;

        [Tooltip("Characters revealed per second.")]
        [SerializeField] private float typewriterSpeed = 60f;

        [Header("Input Handler")]
        [Tooltip("BubblePresenterButtonHandler that manages click input. " +
                 "Auto-found in children if left empty.")]
        [SerializeField] private BubblePresenterButtonHandler buttonHandler;

        [Header("Alignment Fallback")]
        [Tooltip("Alignment used for any character name not in the list.")]
        [SerializeField] private BubbleAlignment defaultAlignment = BubbleAlignment.Center;

        // ── Types ────────────────────────────────────────────────────────────

        public enum BubbleAlignment { Left, Center, Right }

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            SetBubbleVisible(false);

            if (buttonHandler == null)
                buttonHandler = GetComponentInChildren<BubblePresenterButtonHandler>();
        }

        // ── DialoguePresenterBase ────────────────────────────────────────────

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

        public override YarnTask OnDialogueCompleteAsync()
        {
            SetBubbleVisible(false);
            buttonHandler?.OnLineDismiss();
            return YarnTask.CompletedTask;
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            // ── 1. New line starting ───────────────────────────────────────
            buttonHandler?.OnLineBegin();

            // ── 2. Resolve and apply alignment ────────────────────────────
            string characterName = line.CharacterName;
            Debug.Log(characterName);
            ApplyAlignment(ResolveAlignment(characterName));

            // ── 3. Update character name label ────────────────────────────
            if (characterNameText != null)
                characterNameText.text = string.IsNullOrEmpty(characterName)
                    ? string.Empty
                    : characterName;

            // ── 4. Show bubble ────────────────────────────────────────────
            string lineBody = line.TextWithoutCharacterName.Text;
            SetBubbleVisible(true);

            // ── 5. Typewriter ─────────────────────────────────────────────
            if (useTypewriterEffect && lineText != null)
            {
                lineText.text = lineBody;
                lineText.maxVisibleCharacters = 0;
                int totalChars = lineBody.Length;

                for (int i = 0; i <= totalChars; i++)
                {
                    // External next-line → dismiss immediately
                    if (token.IsNextContentRequested)
                    {
                        lineText.maxVisibleCharacters = totalChars;
                        buttonHandler?.OnLineDismiss();
                        SetBubbleVisible(false);
                        return;
                    }

                    // Hurry up: YarnSpinner signal OR player first-click
                    if (token.IsHurryUpRequested ||
                        (buttonHandler != null && buttonHandler.IsHurryUpRequested))
                    {
                        break;
                    }

                    lineText.maxVisibleCharacters = i;

                    float delay = typewriterSpeed > 0f ? 1f / typewriterSpeed : 0f;
                    await YarnTask.Delay(
                        System.TimeSpan.FromSeconds(delay),
                        token.HurryUpToken
                    ).SuppressCancellationThrow();
                }

                lineText.maxVisibleCharacters = totalChars;
            }
            else
            {
                if (lineText != null)
                {
                    lineText.maxVisibleCharacters = int.MaxValue;
                    lineText.text = lineBody;
                }
            }

            // ── 6. Typewriter done — show continue indicator ───────────────
            buttonHandler?.OnTypewriterComplete();

            // ── 7. Wait for player click or external advance ───────────────
            while (!token.IsNextContentRequested)
            {
                if (buttonHandler != null && buttonHandler.IsAdvanceRequested)
                    break;

                await YarnTask.Delay(
                    System.TimeSpan.FromSeconds(0),
                    token.NextContentToken
                ).SuppressCancellationThrow();
            }

            // ── 8. Dismiss ────────────────────────────────────────────────
            buttonHandler?.OnLineDismiss();
            SetBubbleVisible(false);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private BubbleAlignment ResolveAlignment(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
                return defaultAlignment;

            return characterName switch
            {
                "Player"   => BubbleAlignment.Left,
                "Narrator" => BubbleAlignment.Center,
                "NPC"      => BubbleAlignment.Right,
                _          => defaultAlignment
            };
        }

        /// <summary>
        /// Moves bubbleRect to the left, centre, or right of its parent by
        /// locking the anchor to centre-centre and computing anchoredPosition
        /// from the parent's actual pixel width at runtime.
        ///
        ///   Left   → flush to left  edge of parent
        ///   Center → centred in parent
        ///   Right  → flush to right edge of parent
        ///
        /// Y position and height are never changed.
        /// </summary>
        private void ApplyAlignment(BubbleAlignment alignment)
        {
            if (bubbleRect == null) return;

            // Lock anchor + pivot X to centre so anchoredPosition is always
            // relative to the parent's centre — stable regardless of design-time setup.
            bubbleRect.anchorMin = new Vector2(0.5f, bubbleRect.anchorMin.y);
            bubbleRect.anchorMax = new Vector2(0.5f, bubbleRect.anchorMax.y);
            bubbleRect.pivot     = new Vector2(0.5f, bubbleRect.pivot.y);

            // Read parent width at runtime (accounts for Canvas scaling).
            float parentWidth = 0f;
            if (bubbleRect.parent is RectTransform parentRect)
                parentWidth = parentRect.rect.width;

            float bubbleWidth = bubbleRect.rect.width;

            // Half the gap between bubble edge and parent edge.
            float maxOffset = (parentWidth - bubbleWidth) * 0.5f;

            float targetX = alignment switch
            {
                BubbleAlignment.Left   => -maxOffset,   // move left
                BubbleAlignment.Center =>  0f,          // stay centre
                BubbleAlignment.Right  => +maxOffset,   // move right
                _                      =>  0f
            };

            Vector2 pos = bubbleRect.anchoredPosition;
            pos.x = targetX;
            bubbleRect.anchoredPosition = pos;
        }

        private void SetBubbleVisible(bool visible)
        {
            if (bubbleContainer != null)
                bubbleContainer.SetActive(visible);
        }
    }
}
