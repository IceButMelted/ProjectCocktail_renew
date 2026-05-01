/*
 * BubbleOptionItem.cs
 * Based on OptionItem.cs from YarnSpinner-Unity (current branch)
 *
 * Represents a single choice button inside the BubbleOptionsPresenter.
 * Attach this to the option button prefab together with a Unity Button component.
 *
 * Wire the Unity Button's OnClick event in the prefab to call OnButtonClicked().
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace YarnSpinner.Custom
{
    [RequireComponent(typeof(Button))]
    public class BubbleOptionItem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Label that shows the option text.")]
        [SerializeField] private TMP_Text optionText;

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>The option this item represents.</summary>
        public DialogueOption Option { get; private set; }

        /// <summary>
        /// Called by BubbleOptionsPresenter to be notified when the player picks this option.
        /// </summary>
        public Action<DialogueOption> OnOptionSelected { get; set; }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Populate this item with the given option data.</summary>
        public void Configure(DialogueOption option)
        {
            Option = option;

            if (optionText != null)
                optionText.text = option.Line.TextWithoutCharacterName.Text;

            // Disable the button if the option is not available
            var btn = GetComponent<Button>();
            if (btn != null)
                btn.interactable = option.IsAvailable;
        }

        // ── Button callback ───────────────────────────────────────────────────

        /// <summary>
        /// Called when the player clicks / taps this button.
        /// Wire this to the Unity Button OnClick event in the prefab.
        /// </summary>
        public void OnButtonClicked()
        {
            if (Option == null) return;
            OnOptionSelected?.Invoke(Option);
        }
    }
}
