/*
 * BubbleOptionItem.cs
 * Based on OptionItem.cs from YarnSpinner-Unity (#current branch)
 * Yarn Spinner is licensed under the terms found in LICENSE.md.
 *
 * Modification: styled as a speech bubble with left/right visual variants.
 * Use this as the OptionItem prefab reference in BubbleOptionsPresenter.
 */

using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Yarn.Unity.Attributes;

#nullable enable

namespace Yarn.Unity
{
    public sealed class BubbleOptionItem : UnityEngine.UI.Selectable,
        ISubmitHandler, IPointerClickHandler, IPointerEnterHandler
    {
        // ── Text ──────────────────────────────────────────────────────────

        [MustNotBeNull, SerializeField] TMP_Text? text;

        // ── Bubble graphics ───────────────────────────────────────────────

        [Header("Bubble Graphics")]
        [Tooltip("The Image component that shows the bubble background sprite.")]
        [SerializeField] UnityEngine.UI.Image? bubbleImage;

        [Tooltip("Sprite used for the bubble when it is in its normal (unselected) state.")]
        [SerializeField] Sprite? normalSprite;

        [Tooltip("Sprite used for the bubble when it is selected / highlighted.")]
        [SerializeField] Sprite? selectedSprite;

        [Tooltip("Sprite used for the bubble when the option is unavailable.")]
        [SerializeField] Sprite? disabledSprite;

        // ── Colours ───────────────────────────────────────────────────────

        [Header("Bubble Colours")]
        [SerializeField] Color normalColour = Color.white;
        [SerializeField] Color selectedColour = new Color(0.8f, 0.95f, 1f);
        [SerializeField] Color disabledColour = new Color(0.6f, 0.6f, 0.6f);

        // ── Options ───────────────────────────────────────────────────────

        [Header("Options")]
        [Tooltip("Wrap unavailable option text in strikethrough tags.")]
        [SerializeField] bool disabledStrikeThrough = true;

        // ── YarnSpinner internals (same as OptionItem) ────────────────────

        public YarnTaskCompletionSource<DialogueOption?>? OnOptionSelected;
        public System.Threading.CancellationToken completionToken;

        private bool hasSubmittedOptionSelection = false;

        private DialogueOption? _option;

        public DialogueOption Option
        {
            get
            {
                if (_option == null)
                    throw new System.NullReferenceException(
                        "Option has not been set on BubbleOptionItem");
                return _option;
            }

            set
            {
                _option = value;
                hasSubmittedOptionSelection = false;

                if (text == null)
                {
                    Debug.LogWarning(
                        $"{nameof(text)} is null — is it connected in the Inspector?", this);
                    return;
                }

                // Build display text, applying strikethrough if unavailable.
                string lineText = value.Line.TextWithoutCharacterName.Text;
                if (disabledStrikeThrough && !value.IsAvailable)
                    lineText = $"<s>{lineText}</s>";

                text.text = lineText;
                interactable = value.IsAvailable;

                ApplyStyle(value.IsAvailable ? normalColour : disabledColour,
                           value.IsAvailable ? normalSprite : disabledSprite);
            }
        }

        // ── IsHighlighted (same logic as OptionItem) ──────────────────────

        public new bool IsHighlighted =>
            EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject;

        // ── Style helpers ─────────────────────────────────────────────────

        private void ApplyStyle(Color colour, Sprite? sprite)
        {
            if (text != null)
                text.color = colour;

            if (bubbleImage != null)
            {
                bubbleImage.color = colour;

                if (sprite != null)
                    bubbleImage.sprite = sprite;
            }
        }

        // ── Selectable overrides (same as OptionItem) ─────────────────────

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            ApplyStyle(selectedColour, selectedSprite);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);

            // Revert to normal or disabled depending on availability.
            if (_option != null)
                ApplyStyle(_option.IsAvailable ? normalColour : disabledColour,
                           _option.IsAvailable ? normalSprite : disabledSprite);
        }

        // ── Selection submission (same as OptionItem) ─────────────────────

        public void OnSubmit(BaseEventData eventData) => InvokeOptionSelected();

        public void OnPointerClick(PointerEventData eventData) => InvokeOptionSelected();

        public override void OnPointerEnter(PointerEventData eventData) => base.Select();

        public void InvokeOptionSelected()
        {
            if (!IsInteractable()) return;

            if (!hasSubmittedOptionSelection && !completionToken.IsCancellationRequested)
            {
                hasSubmittedOptionSelection = true;
                OnOptionSelected?.TrySetResult(Option);
            }
        }
    }
}