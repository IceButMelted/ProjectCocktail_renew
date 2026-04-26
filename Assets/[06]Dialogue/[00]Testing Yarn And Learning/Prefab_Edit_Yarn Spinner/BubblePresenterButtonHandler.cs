/*
 * BubblePresenterButtonHandler.cs
 * Based on LinePresenterButtonHandler.cs from YarnSpinner-Unity (current branch)
 *
 * Attach to the same GameObject (or any child) as your BubblePresenter.
 * Wire a UI Button (or any clickable area) to OnContinueClicked() via the Inspector.
 *
 * Behaviour mirrors LinePresenterButtonHandler:
 *   • While the typewriter is running → first click HURRIES it (skips to full text).
 *   • Once text is fully displayed    → click ADVANCES to the next line.
 *
 * BubblePresenter polls IsAdvanceRequested each frame to know when to proceed.
 *
 * SETUP
 * ─────
 * 1. Add this component to your bubble/dialogue GameObject.
 * 2. In your bubble prefab, add a transparent Button that covers the screen
 *    (or a dedicated "tap to continue" button) and wire its OnClick → OnContinueClicked().
 * 3. In BubblePresenter's Inspector, drag this component into the "Button Handler" slot.
 */

using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace YarnSpinner.Custom
{
    public class BubblePresenterButtonHandler : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("(Optional) Button to show while waiting for player input. " +
                 "If assigned it will be shown/hidden automatically.")]
        [SerializeField] private Button continueButton;

        [Tooltip("(Optional) GameObject shown as a 'tap to continue' indicator " +
                 "once the typewriter has finished. Hidden while text is still typing.")]
        [SerializeField] private GameObject continueIndicator;

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>True when the typewriter has finished and we are waiting for the player.</summary>
        public bool IsWaitingForInput { get; private set; }

        /// <summary>
        /// True for one frame after the player clicks when we are already
        /// waiting for input (i.e. typewriter is done). BubblePresenter reads
        /// this to know it should advance.
        /// </summary>
        public bool IsAdvanceRequested { get; private set; }

        /// <summary>
        /// True for one frame after the player clicks while the typewriter is
        /// still running. BubblePresenter reads this to know it should hurry up.
        /// </summary>
        public bool IsHurryUpRequested { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Wire the button if provided
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            SetWaitingVisuals(false);
        }

        private void OnDestroy()
        {
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        private void LateUpdate()
        {
            // Flags are one-frame pulses — clear them after BubblePresenter has
            // had a chance to read them in its Update / async loop.
            IsAdvanceRequested = false;
            IsHurryUpRequested = false;
        }

        // ── Public API called by BubblePresenter ──────────────────────────────

        /// <summary>
        /// Called by BubblePresenter at the start of each line, before the
        /// typewriter begins. Resets state and hides the continue indicator.
        /// </summary>
        public void OnLineBegin()
        {
            IsWaitingForInput   = false;
            IsAdvanceRequested  = false;
            IsHurryUpRequested  = false;
            SetWaitingVisuals(false);
        }

        /// <summary>
        /// Called by BubblePresenter when the typewriter has finished and the
        /// presenter is ready for the player to advance. Shows the continue indicator.
        /// </summary>
        public void OnTypewriterComplete()
        {
            IsWaitingForInput = true;
            SetWaitingVisuals(true);
        }

        /// <summary>
        /// Called by BubblePresenter just before the line is dismissed, so we
        /// can hide the continue indicator before the next line begins.
        /// </summary>
        public void OnLineDismiss()
        {
            IsWaitingForInput  = false;
            IsAdvanceRequested = false;
            SetWaitingVisuals(false);
        }

        // ── Button callback ───────────────────────────────────────────────────

        /// <summary>
        /// Wire this to your UI Button's OnClick event.
        /// • If the typewriter is still running → sets IsHurryUpRequested for one frame.
        /// • If the typewriter is done          → sets IsAdvanceRequested for one frame.
        /// </summary>
        public void OnContinueClicked()
        {
            if (IsWaitingForInput)
            {
                // Text is fully visible — advance to the next line
                IsAdvanceRequested = true;
            }
            else
            {
                // Text is still typing — hurry it up
                IsHurryUpRequested = true;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetWaitingVisuals(bool waiting)
        {
            if (continueIndicator != null)
                continueIndicator.SetActive(waiting);

            // The button itself stays active the whole time so the player can
            // always click; only the indicator changes.
        }
    }
}
