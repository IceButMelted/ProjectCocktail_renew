// ============================================================
//  PagedChoiceGrid.cs — A fixed set of visible buttons showing a
//  longer ScriptableObject catalog one page at a time.
//
//  Page size = number of buttons. Each button's icon and click
//  behaviour come from whichever option sits at its position on the
//  current page, so adding content means adding assets to the list,
//  never adding buttons. Next/Previous page wrap circularly. On a
//  short last page, the leftover buttons are made non-interactable
//  and their image is hidden.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class PagedChoiceGrid<TOption> : MonoBehaviour where TOption : ScriptableObject
{
    [Tooltip("The visible slots. Page size = this count.")]
    [SerializeField] private List<Button> _buttons = new List<Button>();

    [Tooltip("Full catalog, any length.")]
    [SerializeField] private List<TOption> _options = new List<TOption>();

    [Header("Paging")]
    [SerializeField] private Button _btnPreviousPage;
    [SerializeField] private Button _btnNextPage;

    private int _page;
    private bool _locked;

    public int PageCount => _buttons.Count == 0
        ? 0
        : Mathf.Max(1, Mathf.CeilToInt(_options.Count / (float)_buttons.Count));

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

    public void NextPage()
    {
        if (PageCount == 0) return;
        _page = (_page + 1) % PageCount;
        Refresh();
    }

    public void PreviousPage()
    {
        if (PageCount == 0) return;
        _page = (_page - 1 + PageCount) % PageCount;
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
        int index = _page * _buttons.Count + slot;
        return index < _options.Count ? _options[index] : null;
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

        bool multiPage = PageCount > 1;
        if (_btnPreviousPage != null) _btnPreviousPage.interactable = multiPage;
        if (_btnNextPage != null) _btnNextPage.interactable = multiPage;
    }

    protected abstract Sprite GetIcon(TOption option);
    protected abstract void Apply(TOption option);
}
