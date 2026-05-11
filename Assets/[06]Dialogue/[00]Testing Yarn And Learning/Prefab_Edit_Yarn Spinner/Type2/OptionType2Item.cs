/*
 * OptionType2Item.cs
 * Companion component for OptionPresenterType2.
 *
 * Updated to use YarnTaskCompletionSource<DialogueOption> directly,
 * matching how the official Yarn Spinner OptionItem works — so the
 * presenter's completion source is resolved safely and only once
 * per option set, with proper cancellation guard.
 *
 * Wire the Unity Button's OnClick → OnButtonClicked() in your prefab.
 */

using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace YarnSpinner.Custom
{
    [RequireComponent(typeof(Button))]
    public class OptionType2Item : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Label that shows the option text.")]
        [SerializeField] private TMP_Text optionText;

        [Tooltip("Optional GameObject shown when the option is unavailable " +
                 "(e.g. a greyed overlay, lock icon, strikethrough image). " +
                 "Leave empty to rely solely on the button's non-interactable state.")]
        [SerializeField] private GameObject unavailableIndicator;

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>The dialogue option this item represents.</summary>
        public DialogueOption Option { get; private set; }

        // Shared completion source written by the presenter — resolved when this item is clicked.
        private YarnTaskCompletionSource<DialogueOption> _completionSource;

        // Token cancelled by the presenter when another option has already been chosen.
        private CancellationToken _completionToken;

        // Guard so we can never resolve the completion source twice.
        private bool _hasSubmitted = false;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Configure this item for a new option set.
        /// </summary>
        /// <param name="option">The dialogue option to display.</param>
        /// <param name="showIfUnavailable">
        /// When true, unavailable options are shown greyed-out and non-interactable.
        /// When false, the presenter never activates this item for unavailable options.
        /// </param>
        /// <param name="completionSource">
        /// The shared task completion source from the presenter. Clicking this item
        /// resolves it with <paramref name="option"/>.
        /// </param>
        /// <param name="completionToken">
        /// Cancelled when another option has already been selected — prevents
        /// double-resolution of the completion source.
        /// </param>
        public void Configure(
            DialogueOption option,
            bool showIfUnavailable,
            YarnTaskCompletionSource<DialogueOption> completionSource,
            CancellationToken completionToken)
        {
            Option            = option;
            _completionSource = completionSource;
            _completionToken  = completionToken;
            _hasSubmitted     = false;

            // Label text
            if (optionText != null)
                optionText.text = option.Line.TextWithoutCharacterName.Text;

            // Button interactability
            var btn = GetComponent<Button>();
            if (btn != null)
                btn.interactable = option.IsAvailable;

            // Unavailable indicator
            if (unavailableIndicator != null)
                unavailableIndicator.SetActive(showIfUnavailable && !option.IsAvailable);
        }

        // ── Button Callback ───────────────────────────────────────────────────

        /// <summary>
        /// Called by the Unity Button OnClick event when the player taps this item.
        /// </summary>
        public void OnButtonClicked()
        {
            if (Option == null)                        return;
            if (!Option.IsAvailable)                   return;
            if (_hasSubmitted)                         return;
            if (_completionToken.IsCancellationRequested) return;
            if (_completionSource == null)             return;

            _hasSubmitted = true;
            _completionSource.TrySetResult(Option);
        }
    }
}
