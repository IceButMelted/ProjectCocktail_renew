/*
 * BubblePresenter.cs
 * Based on LinePresenter.cs from YarnSpinner-Unity (current branch)
 *
 * Extends DialoguePresenterBase to display dialogue lines as speech bubbles.
 * The bubble RectTransform anchor/pivot is repositioned based on the speaker:
 *
 *   "Player"   → anchored LEFT   (pivot X = 0, anchor X = 0)
 *   "Narrator" → anchored CENTER (pivot X = 0.5, anchor X = 0.5)
 *   "NPC"      → anchored RIGHT  (pivot X = 1, anchor X = 1)
 *
 * Requires a BubblePresenterButtonHandler on the same (or any child) GameObject.
 * The presenter WAITS for the player to click before advancing — no auto-advance.
 *
 * Click behaviour (mirrors LinePresenterButtonHandler):
 *   • While typewriter is running  → click HURRIES (shows full text immediately).
 *   • Once text is fully displayed → click ADVANCES to next line.
 */

using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Unity;

namespace YarnSpinner.Custom
{
    public class BubblePresenter : DialoguePresenterBase
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Bubble References")]
        [Tooltip("The root RectTransform of the bubble panel that gets repositioned.")]
        [SerializeField] private RectTransform bubbleRect;

        [Tooltip("TextMeshPro label that shows the dialogue line text.")]
        [SerializeField] private TMP_Text lineText;

        [Tooltip("(Optional) TextMeshPro label that shows the character name.")]
        [SerializeField] private TMP_Text characterNameText;

        [Tooltip("(Optional) Root GameObject of the whole bubble — shown/hidden per line.")]
        [SerializeField] private GameObject bubbleContainer;

        [Header("Typewriter")]
        [Tooltip("Enable typewriter effect.")]
        [SerializeField] private bool useTypewriterEffect = true;

        [Tooltip("Characters revealed per second.")]
        [SerializeField] private float typewriterSpeed = 60f;

        [Header("Input Handler")]
        [Tooltip("Drag the BubblePresenterButtonHandler component here. " +
                 "It manages click input and the continue indicator.")]
        [SerializeField] private BubblePresenterButtonHandler buttonHandler;

        [Header("Alignment Fallback")]
        [Tooltip("Alignment used when the character name is not Player / NPC / Narrator.")]
        [SerializeField] private BubbleAlignment defaultAlignment = BubbleAlignment.Center;

        // ── Types ────────────────────────────────────────────────────────────

        public enum BubbleAlignment { Left, Center, Right }

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            SetBubbleVisible(false);

            // Auto-find handler on the same GameObject if not assigned
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
            // ── 1. Notify handler that a new line is starting ──────────────
            buttonHandler?.OnLineBegin();

            // ── 2. Resolve alignment from character name ───────────────────
            string characterName = line.CharacterName;
            ApplyAlignment(ResolveAlignment(characterName));

            // ── 3. Update name label ───────────────────────────────────────
            if (characterNameText != null)
                characterNameText.text = string.IsNullOrEmpty(characterName) ? string.Empty : characterName;

            // ── 4. Show bubble ─────────────────────────────────────────────
            string lineBody = line.TextWithoutCharacterName.Text;
            SetBubbleVisible(true);

            // ── 5. Typewriter ──────────────────────────────────────────────
            if (useTypewriterEffect && lineText != null)
            {
                lineText.text = lineBody;
                lineText.maxVisibleCharacters = 0;

                int totalChars = lineBody.Length;

                for (int i = 0; i <= totalChars; i++)
                {
                    // YarnSpinner external next-line signal — dismiss immediately
                    if (token.IsNextLineRequested)
                    {
                        lineText.maxVisibleCharacters = totalChars;
                        buttonHandler?.OnLineDismiss();
                        SetBubbleVisible(false);
                        return;
                    }

                    // YarnSpinner hurry-up (e.g. LineAdvancer) OR player first click
                    if (token.IsHurryUpRequested ||
                        (buttonHandler != null && buttonHandler.IsHurryUpRequested))
                    {
                        break; // show full text below
                    }

                    lineText.maxVisibleCharacters = i;

                    float delay = typewriterSpeed > 0f ? 1f / typewriterSpeed : 0f;
                    await YarnTask.Delay(
                        System.TimeSpan.FromSeconds(delay),
                        token.HurryUpToken
                    ).SuppressCancellationThrow();
                }

                // Ensure all characters are visible
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

            // ── 7. Wait for player click OR external advance ───────────────
            //
            // Poll each frame. Exits when:
            //   a) Player clicked  → buttonHandler.IsAdvanceRequested == true
            //   b) YarnSpinner signals next line externally (LineAdvancer, etc.)
            //        → token.IsNextLineRequested == true
            //
            // We yield with a zero-duration Delay so we don't spin the CPU.
            // token.NextLineToken cancels the Delay automatically when YS moves on.

            while (!token.IsNextLineRequested)
            {
                if (buttonHandler != null && buttonHandler.IsAdvanceRequested)
                    break;

                await YarnTask.Delay(
                    System.TimeSpan.FromSeconds(0),
                    token.NextLineToken
                ).SuppressCancellationThrow();
            }

            // ── 8. Dismiss ─────────────────────────────────────────────────
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
        /// Repositions bubbleRect's anchor and pivot horizontally.
        ///   Left   → X = 0  |  Center → X = 0.5  |  Right → X = 1
        /// Y values are untouched.
        /// </summary>
        private void ApplyAlignment(BubbleAlignment alignment)
        {
            if (bubbleRect == null) return;

            float x = alignment switch
            {
                BubbleAlignment.Left   => 0f,
                BubbleAlignment.Center => 0.5f,
                BubbleAlignment.Right  => 1f,
                _                      => 0.5f
            };

            Vector2 anchorMin = bubbleRect.anchorMin;
            Vector2 anchorMax = bubbleRect.anchorMax;
            Vector2 pivot     = bubbleRect.pivot;

            anchorMin.x = x;
            anchorMax.x = x;
            pivot.x     = x;

            bubbleRect.anchorMin = anchorMin;
            bubbleRect.anchorMax = anchorMax;
            bubbleRect.pivot     = pivot;

            Vector2 pos = bubbleRect.anchoredPosition;
            pos.x = 0f;
            bubbleRect.anchoredPosition = pos;
        }

        private void SetBubbleVisible(bool visible)
        {
            if (bubbleContainer != null)
                bubbleContainer.SetActive(visible);
        }
    }
}
