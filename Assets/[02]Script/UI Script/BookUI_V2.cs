using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class BookUI_V2 : MonoBehaviour
{
    public enum PageLayoutMode
    {
        TwoTextTwoImage,
        SingleImage
    }

    [System.Serializable]
    public class BookPage
    {
        [Tooltip("Root GameObject for this page.")]
        public GameObject pageRoot;

        [Header("Layout Mode")]
        public PageLayoutMode layoutMode = PageLayoutMode.TwoTextTwoImage;

        [Header("Two-Text / Two-Image Layout")]
        public TMP_Text textA;
        public TMP_Text textB;
        public Image imageA;
        public Image imageB;

        [Header("Single Image Layout")]
        public Image singleImage;

        [Header("Pre-authored Content (optional)")]
        [TextArea(2, 6)] public string textAContent;
        [TextArea(2, 6)] public string textBContent;

        [Tooltip("Source UI Image whose sprite is copied into imageA.")]
        public Image imageASource;

        [Tooltip("Source UI Image whose sprite is copied into imageB.")]
        public Image imageBSource;

        [Tooltip("Source UI Image whose sprite is copied into singleImage.")]
        public Image singleImageSource;
    }

    [System.Serializable]
    public class PageSpread
    {
        [Tooltip("Optional root parenting both pages.")]
        public GameObject spreadRoot;

        [Header("Pages")]
        public BookPage leftPage;
        public BookPage rightPage;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Spreads")]
    [SerializeField] private List<PageSpread> _spreads = new();

    [Header("Settings")]
    [SerializeField] private int _startSpreadIndex = 0;
    [SerializeField] private bool _wrapAround = false;

    [Header("Page Number Labels (optional)")]
    [SerializeField] private TMP_Text _leftPageNumberLabel;
    [SerializeField] private TMP_Text _rightPageNumberLabel;
    [SerializeField] private string _pageNumberFormat = "{0}";

    [Header("Events")]
    public UnityEvent<int> OnSpreadChanged;
    public UnityEvent OnBookOpened;
    public UnityEvent OnBookClosed;

    // ── Properties ────────────────────────────────────────────────────────────

    public int CurrentSpreadIndex { get; private set; }
    public int SpreadCount => _spreads.Count;
    public bool IsOpen { get; private set; }
    public bool IsFirstSpread => CurrentSpreadIndex == 0;
    public bool IsLastSpread => CurrentSpreadIndex == _spreads.Count - 1;
    public int LeftPageNumber => CurrentSpreadIndex * 2 + 1;
    public int RightPageNumber => CurrentSpreadIndex * 2 + 2;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        HideAllSpreads();
        gameObject.SetActive(false);
        IsOpen = false;
    }

    // ── Public API — Book control ─────────────────────────────────────────────

    public void SetActive(bool open)
    {
        IsOpen = open;
        gameObject.SetActive(open);

        if (open)
        {
            GoToSpread(_startSpreadIndex);
            OnBookOpened?.Invoke();
        }
        else
        {
            HideAllSpreads();
            OnBookClosed?.Invoke();
        }
    }

    public void Toggle() => SetActive(!IsOpen);

    public void NextSpread()
    {
        if (_spreads.Count == 0) return;
        int next = CurrentSpreadIndex + 1;
        if (next >= _spreads.Count) { if (_wrapAround) next = 0; else return; }
        GoToSpread(next);
    }

    public void PreviousSpread()
    {
        if (_spreads.Count == 0) return;
        int prev = CurrentSpreadIndex - 1;
        if (prev < 0) { if (_wrapAround) prev = _spreads.Count - 1; else return; }
        GoToSpread(prev);
    }

    public void GoToSpread(int index)
    {
        if (_spreads.Count == 0) return;
        if (!IsValidIndex(index)) return;

        HideAllSpreads();
        CurrentSpreadIndex = index;

        PageSpread spread = _spreads[index];
        ApplyPreAuthoredContent(spread.leftPage);
        ApplyPreAuthoredContent(spread.rightPage);
        ShowSpread(spread);
        UpdatePageNumberLabels();

        OnSpreadChanged?.Invoke(CurrentSpreadIndex);
    }

    // ── Public API — Runtime content injection ────────────────────────────────

    /// <summary>
    /// Set content on a page at runtime.
    /// Pass null to leave any field unchanged.
    /// imageASource / imageBSource are UI Image components — their sprite (and color) are copied across.
    /// </summary>
    public void SetPageContent(int spreadIndex,
                               bool isLeftPage,
                               string textAContent = null,
                               string textBContent = null,
                               Image imageASource = null,
                               Image imageBSource = null)
    {
        if (!IsValidIndex(spreadIndex)) return;

        BookPage page = isLeftPage ? _spreads[spreadIndex].leftPage
                                   : _spreads[spreadIndex].rightPage;

        if (textAContent != null && page.textA != null) page.textA.text = textAContent;
        if (textBContent != null && page.textB != null) page.textB.text = textBContent;

        if (imageASource != null && page.imageA != null) CopyImage(imageASource, page.imageA);
        if (imageBSource != null && page.imageB != null) CopyImage(imageBSource, page.imageB);

        if (spreadIndex == CurrentSpreadIndex) ShowPage(page);
    }

    /// <summary>
    /// Set a full-page image on a SingleImage-mode page at runtime.
    /// source is a UI Image component — its sprite and color are copied across.
    /// </summary>
    public void SetPageSingleImage(int spreadIndex, bool isLeftPage, Image source)
    {
        if (!IsValidIndex(spreadIndex)) return;

        BookPage page = isLeftPage ? _spreads[spreadIndex].leftPage
                                   : _spreads[spreadIndex].rightPage;

        if (source != null && page.singleImage != null) CopyImage(source, page.singleImage);

        if (spreadIndex == CurrentSpreadIndex) ShowPage(page);
    }

    /// <summary>
    /// Switch the layout mode of a page at runtime.
    /// </summary>
    public void SetPageLayoutMode(int spreadIndex, bool isLeftPage, PageLayoutMode mode)
    {
        if (!IsValidIndex(spreadIndex)) return;

        BookPage page = isLeftPage ? _spreads[spreadIndex].leftPage
                                   : _spreads[spreadIndex].rightPage;
        page.layoutMode = mode;

        if (spreadIndex == CurrentSpreadIndex) ShowPage(page);
    }

    // ── Public API — Collection management ───────────────────────────────────

    public void AddSpread(PageSpread spread)
    {
        if (spread == null) return;
        spread.spreadRoot?.SetActive(false);
        spread.leftPage?.pageRoot?.SetActive(false);
        spread.rightPage?.pageRoot?.SetActive(false);
        _spreads.Add(spread);
    }

    public void RemoveSpreadAt(int index)
    {
        if (!IsValidIndex(index)) return;
        _spreads.RemoveAt(index);
        CurrentSpreadIndex = Mathf.Clamp(CurrentSpreadIndex, 0, Mathf.Max(0, _spreads.Count - 1));
    }

    public PageSpread GetCurrentSpread() =>
        _spreads.Count > 0 ? _spreads[CurrentSpreadIndex] : null;

    // ── Private — Presentation ────────────────────────────────────────────────

    private void ShowSpread(PageSpread spread)
    {
        if (spread == null) return;
        spread.spreadRoot?.SetActive(true);
        ShowPage(spread.leftPage);
        ShowPage(spread.rightPage);
    }

    private void ShowPage(BookPage page)
    {
        if (page == null) return;

        page.pageRoot?.SetActive(true);

        bool isTwoLayout = page.layoutMode == PageLayoutMode.TwoTextTwoImage;

        SetActive(page.textA, isTwoLayout);
        SetActive(page.textB, isTwoLayout);
        SetActive(page.imageA, isTwoLayout);
        SetActive(page.imageB, isTwoLayout);
        SetActive(page.singleImage, !isTwoLayout);
    }

    private void HideAllSpreads()
    {
        foreach (var spread in _spreads)
        {
            if (spread == null) continue;
            spread.spreadRoot?.SetActive(false);
            spread.leftPage?.pageRoot?.SetActive(false);
            spread.rightPage?.pageRoot?.SetActive(false);
        }
    }

    private void ApplyPreAuthoredContent(BookPage page)
    {
        if (page == null) return;

        if (!string.IsNullOrEmpty(page.textAContent) && page.textA != null)
            page.textA.text = page.textAContent;

        if (!string.IsNullOrEmpty(page.textBContent) && page.textB != null)
            page.textB.text = page.textBContent;

        if (page.imageASource != null && page.imageA != null) CopyImage(page.imageASource, page.imageA);
        if (page.imageBSource != null && page.imageB != null) CopyImage(page.imageBSource, page.imageB);
        if (page.singleImageSource != null && page.singleImage != null) CopyImage(page.singleImageSource, page.singleImage);
    }

    private void UpdatePageNumberLabels()
    {
        if (_leftPageNumberLabel != null)
            _leftPageNumberLabel.text = string.Format(_pageNumberFormat, LeftPageNumber);
        if (_rightPageNumberLabel != null)
            _rightPageNumberLabel.text = string.Format(_pageNumberFormat, RightPageNumber);
    }

    // ── Private — Helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Copies sprite, color, and image type from one UI Image to another.
    /// </summary>
    private static void CopyImage(Image source, Image target)
    {
        target.sprite = source.sprite;
        target.color = source.color;
        target.type = source.type;
    }

    private static void SetActive(Component c, bool active)
    {
        if (c != null) c.gameObject.SetActive(active);
    }

    private bool IsValidIndex(int index)
    {
        if (index >= 0 && index < _spreads.Count) return true;
        Debug.LogWarning($"[BookUI] Spread index {index} is out of range (0 – {_spreads.Count - 1}).");
        return false;
    }
}