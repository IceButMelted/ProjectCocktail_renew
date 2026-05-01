using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Page-based UI controller.
///
/// Usage:
///   book.SetActive(true)   — open
///   book.NextPage()        — next page
///   book.PreviousPage()    — previous page
///   book.GoToPage(2)       — jump to page (0-indexed)
///   book.SetActive(false)  — close
/// </summary>
public class BookUI : MonoBehaviour
{
    [Header("Pages")]
    [Tooltip("All page GameObjects in order.")]
    [SerializeField] private List<GameObject> _pages = new List<GameObject>();

    [Header("Settings")]
    [SerializeField] private int  _startPageIndex = 0;
    [SerializeField] private bool _wrapAround     = false;

    [Header("Events")]
    public UnityEvent<int> OnPageChanged;
    public UnityEvent      OnBookOpened;
    public UnityEvent      OnBookClosed;

    // ── Properties ───────────────────────────────────────
    public int  CurrentPageIndex { get; private set; }
    public int  PageCount        => _pages.Count;
    public bool IsOpen           { get; private set; }
    public bool IsFirstPage      => CurrentPageIndex == 0;
    public bool IsLastPage       => CurrentPageIndex == _pages.Count - 1;

    // ── Unity ────────────────────────────────────────────
    private void Awake()
    {
        HideAllPages();
        gameObject.SetActive(false);
        IsOpen = false;
    }

    // ── Public API ───────────────────────────────────────
    public void SetActive(bool open)
    {
        IsOpen = open;
        gameObject.SetActive(open);

        if (open) { GoToPage(_startPageIndex); OnBookOpened?.Invoke(); }
        else      { HideAllPages();             OnBookClosed?.Invoke(); }
    }

    public void Toggle() => SetActive(!IsOpen);

    public void NextPage()
    {
        if (_pages.Count == 0) return;
        int next = CurrentPageIndex + 1;
        if (next >= _pages.Count)
        {
            if (_wrapAround) next = 0; else return;
        }
        GoToPage(next);
    }

    public void PreviousPage()
    {
        if (_pages.Count == 0) return;
        int prev = CurrentPageIndex - 1;
        if (prev < 0)
        {
            if (_wrapAround) prev = _pages.Count - 1; else return;
        }
        GoToPage(prev);
    }

    public void GoToPage(int index)
    {
        if (_pages.Count == 0) return;
        if (index < 0 || index >= _pages.Count)
        {
            Debug.LogWarning($"[BookUI] Index {index} out of range (0–{_pages.Count - 1}).");
            return;
        }

        HideAllPages();
        CurrentPageIndex = index;
        _pages[index]?.SetActive(true);
        OnPageChanged?.Invoke(CurrentPageIndex);
    }

    public void AddPage(GameObject page)
    {
        if (page == null) return;
        page.SetActive(false);
        _pages.Add(page);
    }

    public void RemovePage(GameObject page)
    {
        int idx = _pages.IndexOf(page);
        if (idx < 0) return;
        _pages.RemoveAt(idx);
        if (CurrentPageIndex >= _pages.Count)
            CurrentPageIndex = Mathf.Max(0, _pages.Count - 1);
    }

    // ── Private ──────────────────────────────────────────
    private void HideAllPages()
    {
        foreach (var p in _pages) p?.SetActive(false);
    }
}
