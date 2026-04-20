// ============================================================
//  BookUI — A page-based UI controller for Unity
//
//  Usage:
//    1. Add this script to a GameObject (e.g. "Book")
//    2. Drag any number of page GameObjects into _pages in the Inspector
//    3. Call from other scripts or UI Buttons:
//         _book.SetActive(true)       — show the book
//         _book.NextPage()            — go forward
//         _book.PreviousPage()        — go backward
//         _book.GoToPage(2)           — jump to a specific page (0-indexed)
//         _book.SetActive(false)      — hide the book
// ============================================================

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class BookUI : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────
    [Header("Book")]

    [Header("Pages")]
    [Tooltip("Drag all page GameObjects here in order.")]
    [SerializeField] private List<GameObject> _pages = new List<GameObject>();

    [Header("Settings")]
    [Tooltip("Which page to show when the book first opens (0 = first page).")]
    [SerializeField] private int _startPageIndex = 0;

    [Tooltip("Wrap around from last page to first and vice versa.")]
    [SerializeField] private bool _wrapAround = false;

    [Header("Events")]
    public UnityEvent<int> OnPageChanged;   // passes the new page index
    public UnityEvent      OnBookOpened;
    public UnityEvent      OnBookClosed;

    // ── State ──────────────────────────────────────────────

    /// <summary>Current page index (0-based).</summary>
    public int CurrentPageIndex { get; private set; }

    /// <summary>Total number of pages.</summary>
    public int PageCount => _pages.Count;

    public bool IsOpen { get; private set; }

    // ── Unity ──────────────────────────────────────────────

    private void Awake()
    {
        // Hide all pages on start; SetActive() controls visibility
        HideAllPages();
        gameObject.SetActive(false);
        IsOpen = false;
    }

    // ── Public API ─────────────────────────────────────────

    /// <summary>Open or close the book.</summary>
    public void SetActive(bool open)
    {
        IsOpen = open;
        gameObject.SetActive(open);

        if (open)
        {
            GoToPage(_startPageIndex);
            OnBookOpened?.Invoke();
        }
        else
        {
            HideAllPages();
            OnBookClosed?.Invoke();
        }
    }

    public void ToggleAcitve() { 
        IsOpen = !IsOpen;
        gameObject.SetActive(IsOpen);

        if (IsOpen)
        {
            GoToPage(_startPageIndex);
            OnBookOpened?.Invoke();
        }
        else
        {
            HideAllPages();
            OnBookClosed?.Invoke();
        }

    }

    /// <summary>Advance to the next page.</summary>
    public void NextPage()
    {
        if (_pages.Count == 0) return;

        int next = CurrentPageIndex + 1;

        if (next >= _pages.Count)
        {
            if (_wrapAround) next = 0;
            else return; // already on last page
        }

        GoToPage(next);
    }

    /// <summary>Go back to the previous page.</summary>
    public void PreviousPage()
    {
        if (_pages.Count == 0) return;

        int prev = CurrentPageIndex - 1;

        if (prev < 0)
        {
            if (_wrapAround) prev = _pages.Count - 1;
            else return; // already on first page
        }

        GoToPage(prev);
    }

    /// <summary>Jump directly to a page by index (0-based).</summary>
    public void GoToPage(int index)
    {
        if (_pages.Count == 0) return;
        if (index < 0 || index >= _pages.Count)
        {
            Debug.LogWarning($"[BookUI] Page index {index} is out of range (0–{_pages.Count - 1}).");
            return;
        }

        HideAllPages();

        CurrentPageIndex = index;
        _pages[index]?.SetActive(true);

        OnPageChanged?.Invoke(CurrentPageIndex);
        Debug.Log($"[BookUI] Page {CurrentPageIndex + 1} / {_pages.Count}");
    }

    /// <summary>Add a page at runtime.</summary>
    public void AddPage(GameObject page)
    {
        if (page == null) return;
        page.SetActive(false);
        _pages.Add(page);
    }

    /// <summary>Remove a page at runtime by reference.</summary>
    public void RemovePage(GameObject page)
    {
        int index = _pages.IndexOf(page);
        if (index < 0) return;

        _pages.RemoveAt(index);

        // Stay in bounds after removal
        if (CurrentPageIndex >= _pages.Count)
            CurrentPageIndex = Mathf.Max(0, _pages.Count - 1);
    }

    public bool IsFirstPage => CurrentPageIndex == 0;
    public bool IsLastPage  => CurrentPageIndex == _pages.Count - 1;

    // ── Private ────────────────────────────────────────────

    private void HideAllPages()
    {
        foreach (var page in _pages)
            page?.SetActive(false);
    }
}
