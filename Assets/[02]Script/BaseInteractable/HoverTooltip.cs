using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// ── Tooltip Provider Interface ────────────────────────────────────────────────

/// <summary>
/// Implement on any MonoBehaviour to feed dynamic text to HoverTooltip.
///
/// Example:
///     public class EnemyStats : MonoBehaviour, ITooltipProvider
///     {
///         public string GetTooltipText() => $"HP: {_hp}  ATK: {_atk}";
///     }
/// </summary>
public interface ITooltipProvider
{
    string GetTooltipText();
}

// ── HoverTooltip ──────────────────────────────────────────────────────────────

/// <summary>
/// After hovering for <see cref="_hoverDelay"/> seconds a Screen-Space tooltip
/// appears near the cursor and follows it.
///
/// Background sizing uses ContentSizeFitter + HorizontalLayoutGroup so Unity's
/// own layout system keeps the panel exactly as large as the text, with no
/// manual size calculations that can drift.
///
/// Text source priority (first non-empty wins):
///   1. ITooltipProvider.GetTooltipText() on <see cref="_providerObject"/>
///   2. <see cref="_tooltipText"/> (static fallback)
/// </summary>
[RequireComponent(typeof(Collider))]
public class HoverTooltip : PointerInteractableBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Text Source")]
    [Tooltip("GameObject with an ITooltipProvider component for dynamic text.\n" +
             "Leave empty to use the static text below.")]
    [SerializeField] private GameObject _providerObject;

    [Tooltip("Static fallback text when no provider is set or it returns empty.")]
    [SerializeField] private string _tooltipText = "Tooltip";

    [Header("Timing")]
    [SerializeField, Min(0f)] private float _hoverDelay = 1f;

    [Header("Cursor Offset (pixels)")]
    [SerializeField] private Vector2 _cursorOffset = new Vector2(16f, -24f);

    [Header("Visuals")]
    [SerializeField] private Color _backgroundColour = new Color(0.08f, 0.08f, 0.08f, 0.90f);
    [SerializeField] private Color _textColour = Color.white;
    [SerializeField] private int _fontSize = 18;

    [Tooltip("Inner padding around the text (x = left/right, y = top/bottom).")]
    [SerializeField] private Vector2 _padding = new Vector2(14f, 8f);

    [Header("Animation")]
    [SerializeField, Min(0f)] private float _fadeDuration = 0.12f;

    [Header("Optional — shared canvas")]
    [Tooltip("Assign an existing Screen-Space Overlay Canvas, or leave null to auto-create.")]
    [SerializeField] private Canvas _sharedCanvas;

    // ── Shared (static) UI ────────────────────────────────────────────────────

    private static Canvas s_canvas;
    private static CanvasGroup s_canvasGroup;
    private static RectTransform s_panel;
    private static Image s_background;
    private static TextMeshProUGUI s_label;
    private static HoverTooltip s_owner;

    // ── Private State ─────────────────────────────────────────────────────────

    private ITooltipProvider _provider;
    private Coroutine _delayCoroutine;
    private Coroutine _fadeCoroutine;
    private bool _isShowing;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureSharedUI();
        CacheProvider();
    }

    private void LateUpdate()
    {
        if (!_isShowing || s_owner != this) return;
        FollowMouse();
    }

    private void OnDestroy()
    {
        if (s_owner == this) ReleasePanel();
    }

    // ── PointerInteractableBase Overrides ─────────────────────────────────────

    protected override void OnInteractableChanged(bool interactable)
    {
        if (!interactable) CancelAndHide();
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!Interactable) return;
        if (DragableObject.IsAnyDragging) return;

        StopDelayCoroutine();
        _delayCoroutine = StartCoroutine(DelayedShow());
    }

    public override void OnPointerExit(PointerEventData eventData) => CancelAndHide();
    public override void OnDrag(PointerEventData eventData) => CancelAndHide();

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetText(string text)
    {
        _tooltipText = text;
        if (_isShowing && s_owner == this && s_label != null)
        {
            s_label.text = text;
            RebuildLayout();
        }
    }

    public void SetProvider(GameObject providerObject)
    {
        _providerObject = providerObject;
        CacheProvider();
    }

    // ── Private — Provider ────────────────────────────────────────────────────

    private void CacheProvider()
    {
        _provider = null;
        if (_providerObject == null) return;

        _provider = _providerObject.GetComponent<ITooltipProvider>();

        if (_provider == null)
            Debug.LogWarning($"[HoverTooltip] '{_providerObject.name}' has no " +
                             "ITooltipProvider component. Falling back to static text.", this);
    }

    private string ResolveText()
    {
        if (_provider != null)
        {
            string dynamic = _provider.GetTooltipText();
            if (!string.IsNullOrEmpty(dynamic)) return dynamic;
        }
        return string.IsNullOrEmpty(_tooltipText) ? "(no text)" : _tooltipText;
    }

    // ── Private — Show / Hide ─────────────────────────────────────────────────

    private IEnumerator DelayedShow()
    {
        yield return new WaitForSeconds(_hoverDelay);
        _delayCoroutine = null;
        ShowTooltip();
    }

    private void ShowTooltip()
    {
        if (_isShowing) return;
        _isShowing = true;
        s_owner = this;

        s_background.color = _backgroundColour;
        s_label.color = _textColour;
        s_label.fontSize = _fontSize;
        s_label.text = ResolveText();

        // Sync padding from inspector to the LayoutGroup.
        HorizontalLayoutGroup layout = s_panel.GetComponent<HorizontalLayoutGroup>();
        int px = Mathf.RoundToInt(_padding.x);
        int py = Mathf.RoundToInt(_padding.y);
        layout.padding = new RectOffset(px, px, py, py);

        // Let Unity rebuild the layout so ContentSizeFitter sets the correct
        // size before we position and clamp the panel.
        s_panel.gameObject.SetActive(true);
        RebuildLayout();
        FollowMouse();

        StopFadeCoroutine();
        _fadeCoroutine = StartCoroutine(Fade(0f, 1f));
    }

    private void CancelAndHide()
    {
        StopDelayCoroutine();
        if (!_isShowing) return;
        _isShowing = false;

        if (s_owner == this)
        {
            StopFadeCoroutine();
            _fadeCoroutine = StartCoroutine(FadeAndDeactivate());
        }
    }

    private void ReleasePanel()
    {
        _isShowing = false;
        s_owner = null;
        if (s_panel != null) s_panel.gameObject.SetActive(false);
        if (s_canvasGroup != null) s_canvasGroup.alpha = 0f;
    }

    // ── Private — Layout & Position ───────────────────────────────────────────

    /// <summary>
    /// Forces an immediate layout rebuild so ContentSizeFitter updates
    /// s_panel.sizeDelta before we read it for clamping.
    /// </summary>
    private static void RebuildLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(s_panel);
    }

    private void FollowMouse()
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                s_canvas.GetComponent<RectTransform>(),
                Input.mousePosition,
                s_canvas.worldCamera,
                out Vector2 localPoint))
        {
            s_panel.anchoredPosition = localPoint + _cursorOffset;
        }

        ClampToScreen();
    }

    private void ClampToScreen()
    {
        Vector2 size = s_panel.sizeDelta;
        Vector2 pos = s_panel.anchoredPosition;
        Vector2 canvasSize = s_canvas.GetComponent<RectTransform>().sizeDelta;

        float halfW = canvasSize.x * 0.5f;
        float halfH = canvasSize.y * 0.5f;

        pos.x = Mathf.Clamp(pos.x, -halfW, halfW - size.x);
        pos.y = Mathf.Clamp(pos.y, -halfH + size.y, halfH);

        s_panel.anchoredPosition = pos;
    }

    // ── Private — Fade Coroutines ─────────────────────────────────────────────

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        s_canvasGroup.alpha = from;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            s_canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
            yield return null;
        }
        s_canvasGroup.alpha = to;
        _fadeCoroutine = null;
    }

    private IEnumerator FadeAndDeactivate()
    {
        yield return StartCoroutine(Fade(s_canvasGroup.alpha, 0f));
        if (s_owner == this) s_owner = null;
        s_panel.gameObject.SetActive(false);
        _fadeCoroutine = null;
    }

    // ── Private — Coroutine Helpers ───────────────────────────────────────────

    private void StopDelayCoroutine()
    {
        if (_delayCoroutine == null) return;
        StopCoroutine(_delayCoroutine);
        _delayCoroutine = null;
    }

    private void StopFadeCoroutine()
    {
        if (_fadeCoroutine == null) return;
        StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = null;
    }

    // ── Private — Shared UI Construction ─────────────────────────────────────

    /// <summary>
    /// Hierarchy:
    ///
    ///  [Canvas — ScreenSpaceOverlay]
    ///    └─ Panel  (Image + HorizontalLayoutGroup + ContentSizeFitter + CanvasGroup)
    ///         └─ Label  (TextMeshProUGUI)
    ///
    /// ContentSizeFitter (PreferredSize on both axes) + HorizontalLayoutGroup
    /// (padding) make the panel hug the label exactly — no manual size math.
    /// </summary>
    private void EnsureSharedUI()
    {
        if (s_canvas != null) return;

        // ── Canvas ────────────────────────────────────────────────────────────
        if (_sharedCanvas != null)
        {
            s_canvas = _sharedCanvas;
        }
        else
        {
            GameObject canvasGO = new GameObject("[TooltipCanvas]");
            DontDestroyOnLoad(canvasGO);
            s_canvas = canvasGO.AddComponent<Canvas>();
            s_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            s_canvas.sortingOrder = 9999;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // ── Panel ─────────────────────────────────────────────────────────────
        GameObject panelGO = new GameObject("TooltipPanel");
        panelGO.transform.SetParent(s_canvas.transform, false);

        // Image acts as the background — lives directly on the panel.
        s_background = panelGO.AddComponent<Image>();
        s_background.raycastTarget = false;

        // HorizontalLayoutGroup provides padding; child-control-size makes it
        // drive the label's rect so TMP can report preferred size correctly.
        HorizontalLayoutGroup layout = panelGO.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(14, 14, 8, 8); // default; overwritten on show

        // ContentSizeFitter resizes the panel to fit its content automatically.
        ContentSizeFitter fitter = panelGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        s_panel = panelGO.GetComponent<RectTransform>();
        s_panel.pivot = new Vector2(0f, 1f);        // top-left → offset from cursor is intuitive
        s_panel.anchorMin = new Vector2(0.5f, 0.5f);
        s_panel.anchorMax = new Vector2(0.5f, 0.5f);

        s_canvasGroup = panelGO.AddComponent<CanvasGroup>();
        s_canvasGroup.interactable = false;
        s_canvasGroup.blocksRaycasts = false;
        s_canvasGroup.alpha = 0f;

        // ── Label ─────────────────────────────────────────────────────────────
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(panelGO.transform, false);

        s_label = labelGO.AddComponent<TextMeshProUGUI>();
        s_label.raycastTarget = false;
        s_label.alignment = TextAlignmentOptions.Center;

        // Unconstrained overflow lets ContentSizeFitter see the true preferred
        // size without TMP clipping the measurement to a fixed rect.
        s_label.overflowMode = TextOverflowModes.Overflow;
        s_label.enableWordWrapping = false;

        panelGO.SetActive(false);
    }
}