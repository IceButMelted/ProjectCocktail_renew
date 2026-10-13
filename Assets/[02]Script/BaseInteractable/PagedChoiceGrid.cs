// ============================================================
//  PagedChoiceGrid.cs — A fixed set of visible buttons showing a
//  longer ScriptableObject catalog through a sliding window.
//
//  Each button's icon and click behaviour come from whichever option
//  sits under it, so adding content means adding assets to the list,
//  never adding buttons. Next/Previous move the window by one option
//  and wrap around the catalog, so every button stays filled. Only a
//  catalog shorter than the button count leaves buttons empty: those
//  are made non-interactable and their image is hidden.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class PagedChoiceGrid<TOption> : MonoBehaviour where TOption : ScriptableObject
{
    [Tooltip("The visible slots. The window shows this many options at once.")]
    [SerializeField] private List<Button> _buttons = new List<Button>();

    [Tooltip("Full catalog, any length.")]
    [SerializeField] private List<TOption> _options = new List<TOption>();

    [Header("Paging")]
    [SerializeField] private Button _btnPreviousPage;
    [SerializeField] private Button _btnNextPage;

    private int _first; // index into _options of the option under the first button
    private bool _locked;

    // Nothing to scroll to when every option already has a button.
    private bool CanScroll => _options.Count > _buttons.Count;

    protected virtual void Awake()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] == null) continue;
            int slot = i; // capture per-iteration for the closure
            _buttons[i].onClick.AddListener(() => OnSlotClicked(slot));
        }

        if (_btnPreviousPage != null) _btnPreviousPage.onClick.AddListener(PreviousPage);
        if (_btnNextPage != null) _btnNextPage.onClick.AddListener(NextPage);

        Refresh();
    }

    protected virtual void OnDestroy()
    {
        if (_btnPreviousPage != null) _btnPreviousPage.onClick.RemoveListener(PreviousPage);
        if (_btnNextPage != null) _btnNextPage.onClick.RemoveListener(NextPage);
    }

    public void NextPage() => Scroll(1);
    public void PreviousPage() => Scroll(-1);

    private void Scroll(int step)
    {
        if (!CanScroll) return;
        _first = (_first + step + _options.Count) % _options.Count;
        Refresh();
    }

    /// <summary>Locks/unlocks every slot button at once. Empty slots stay locked regardless.</summary>
    public void SetInteractable(bool interactable)
    {
        _locked = !interactable;
        Refresh();
    }

    private void OnSlotClicked(int slot)
    {
        var option = OptionAt(slot);
        if (option != null) Apply(option);
    }

    private TOption OptionAt(int slot)
    {
        if (slot >= _options.Count) return null;
        return _options[(_first + slot) % _options.Count];
    }

    private void Refresh()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            var button = _buttons[i];
            if (button == null) continue;

            var option = OptionAt(i);
            bool hasOption = option != null;

            button.interactable = hasOption && !_locked;

            if (button.image != null)
            {
                button.image.enabled = hasOption;

                // Keep the authored sprite if the asset has no icon yet.
                var icon = hasOption ? GetIcon(option) : null;
                if (icon != null) button.image.sprite = icon;
            }
        }

        if (_btnPreviousPage != null) _btnPreviousPage.interactable = CanScroll;
        if (_btnNextPage != null) _btnNextPage.interactable = CanScroll;
    }

    protected abstract Sprite GetIcon(TOption option);
    protected abstract void Apply(TOption option);
}
