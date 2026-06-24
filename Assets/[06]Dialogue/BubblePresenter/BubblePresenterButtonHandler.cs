/*
 * BubblePresenterButtonHandler.cs
 * Based on LinePresenterButtonHandler.cs from YarnSpinner-Unity (current branch)
 *
 * Attach to the same GameObject (or any child) as your BubblePresenter.
 * Wire a UI Button (or any clickable area) to OnContinueClicked() via the Inspector.
 *
 * Behaviour:
 *   • While the typewriter is running → click calls LineAdvancer.RequestLineHurryUp()
 *     (skips to full text).
 *   • Once text is fully displayed    → click calls LineAdvancer.RequestNextLine()
 *     (advances to the next line).
 *
 * These calls go through the LineAdvancer rather than being handled locally, so
 * keyboard / gamepad / Input Action bindings configured on the LineAdvancer work
 * the same way a button tap does — both end up calling DialogueRunner.RequestHurryUpLine()
 * / RequestNextLine(), which BubblePresenter already observes via
 * LineCancellationToken.IsHurryUpRequested / IsNextContentRequested.
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

        [Tooltip("(Optional) The LineAdvancer that hurry-up / next-line requests are " +
                 "forwarded to, in addition to the local IsHurryUpRequested / " +
                 "IsAdvanceRequested flags. If left empty, BubblePresenter will assign " +
                 "one automatically on Awake if it can find one (see SetLineAdvancer). " +
                 "Set the LineAdvancer's 'Separate Hurry Up And Advance Controls' to " +
                 "true, since BubblePresenter doesn't expose a Typewriter for it to " +
                 "track line-completion itself — this script does that tracking " +
                 "instead via IsWaitingForInput.")]
        [SerializeField] private LineAdvancer lineAdvancer;

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
        /// Called by BubblePresenter (typically from its own Awake) to supply a
        /// LineAdvancer reference if one wasn't already assigned in the
        /// Inspector. Existing Inspector assignments take priority and are not
        /// overwritten.
        /// </summary>
        public void SetLineAdvancer(LineAdvancer advancer)
        {
            if (lineAdvancer == null)
                lineAdvancer = advancer;
        }

        /// <summary>
        /// Called by BubblePresenter at the start of each line, before the
        /// typewriter begins. Resets state and hides the continue indicator.
        /// </summary>
        public void OnLineBegin()
        {
            IsWaitingForInput = false;
            IsAdvanceRequested = false;
            IsHurryUpRequested = false;
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
            IsWaitingForInput = false;
            IsAdvanceRequested = false;
            SetWaitingVisuals(false);
        }

        // ── Button callback ───────────────────────────────────────────────────

        /// <summary>
        /// Wire this to your UI Button's OnClick event.
        /// • If the typewriter is still running → sets IsHurryUpRequested for one frame,
        ///   and (if assigned) calls LineAdvancer.RequestLineHurryUp().
        /// • If the typewriter is done          → sets IsAdvanceRequested for one frame,
        ///   and (if assigned) calls LineAdvancer.RequestNextLine().
        /// Routing through LineAdvancer means BubblePresenter's RunLineAsync sees the
        /// request via LineCancellationToken.IsHurryUpRequested / IsNextContentRequested,
        /// the same channel that keyboard/gamepad input on the LineAdvancer uses — so a
        /// click and a key press behave identically.
        /// </summary>
        public void OnContinueClicked()
        {
            if (IsWaitingForInput)
            {
                // Text is fully visible — advance to the next line
                IsAdvanceRequested = true;
                if (lineAdvancer != null) lineAdvancer.RequestNextLine();
            }
            else
            {
                // Text is still typing — hurry it up
                IsHurryUpRequested = true;
                if (lineAdvancer != null) lineAdvancer.RequestLineHurryUp();
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