// ============================================================
//  GarnishManager.cs — The garnish station: pick a glass, add ice,
//  decorate, pour, then hand the drink over.
//
//  Owns its own canvas (Canvas_Garnish): the glass / item / rim grids
//  (all three on screen at once), the ice buttons and the Pour /
//  Finish / Reset buttons, plus the one glass on the table and the
//  rules around it (glass first — nothing else works without one; one
//  pour per glass, no glass swap and no ice removal after the pour).
//
//  It knows nothing about the game flow, the cocktail or the camera.
//  What it needs comes in as an argument (Pour takes the drink) and
//  what the rest of the game has to react to goes out as an event;
//  FlowWiring is the one place that connects the two.
// ============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GarnishManager : MonoBehaviour
{
    [Header("Glass")]
    [Tooltip("Where the chosen serving glass spawns (position + rotation).")]
    [SerializeField] private Transform _glassSpawnPoint;

    [Tooltip("The pour (shaker, stream) that fills the glass on the plate.")]
    [SerializeField] private PourVisual _pour;

    [Tooltip("The picture of the glass on the canvas (the glass is drawn by its own camera). Shown only while a glass exists, so it never blocks clicks on the bar the rest of the time.")]
    [SerializeField] private GameObject _glassView;

    [Header("Panels")]
    [Tooltip("The whole garnish panel. Opened by Open(), closed by Close().")]
    [SerializeField] private GameObject _panel;
    [Tooltip("Backdrop behind the garnish and serve steps. Shown from Open until the serve step ends (or a remake), and never a raycast target.")]
    [SerializeField] private Image _background;

    [Header("Buttons")]
    [SerializeField] private Button _btnPour;
    [SerializeField] private Button _btnFinish;
    [SerializeField] private Button _btnReset;

    [Header("Ice")]
    [SerializeField] private Button _btnAddIce;
    [SerializeField] private Button _btnRemoveIce;

    [Header("Serve panel (step 4)")]
    [Tooltip("The hand-over panel. It lives on this canvas because the glass it serves sits on this plate.")]
    [SerializeField] private GameObject _panelServe;
    [SerializeField] private Button _btnServe;
    [SerializeField] private Button _btnRemake;

    [Tooltip("Seconds the garnish panel takes to slide off, and the glass to slide to the middle.")]
    [SerializeField] private float _slideDuration = 0.4f;

    [Tooltip("Seconds the finished glass is shown on its own before the serve buttons appear.")]
    [SerializeField] private float _serveButtonsDelay = 1.2f;

    [Header("SFX")]
    [SerializeField] private string _sfxAddIce = "Add_Ice";
    [SerializeField] private string _sfxRemoveIce = "Remove_Ice";

    // ── Events out ─────────────────────────────────────────

    /// <summary>The panel is on screen again (after <see cref="Open"/>). Grids unlock on this.</summary>
    public event Action Opened;

    /// <summary>The pour button was pressed. The listener answers with <see cref="Pour"/> and the drink.</summary>
    public event Action PourRequested;

    /// <summary>A pour started on the glass. Grids lock on this: one glass per pour.</summary>
    public event Action PourStarted;

    /// <summary>A glass was put on the plate (true) or taken off it (false). The item and rim grids follow this.</summary>
    public event Action<bool> GlassChanged;

    /// <summary>Ice was added (true) or removed (false). The listener records it on the drink.</summary>
    public event Action<bool> IceToggled;

    /// <summary>The player is done garnishing and the glass is full. Only raised after a finished pour.</summary>
    public event Action FinishRequested;

    /// <summary>The player pressed reset: throw the drink away and start it again.</summary>
    public event Action ResetRequested;

    /// <summary>The serve button was pressed: the drink is handed over.</summary>
    public event Action ServeRequested;

    /// <summary>The remake button on the serve panel was pressed: back to preparing the drink.</summary>
    public event Action RemakeRequested;

    // ── State ──────────────────────────────────────────────

    private bool _pourComplete;
    private bool _hasPoured;
    private PlacedGlassInstance _pouringGlass;

    // Which of the placed glass's 2 garnish slots the next Garnish Item click fills —
    // set by clicking a slot on the glass itself (GarnishSlotButton).
    private int _activeGarnishSlot;
    private PlacedGlassInstance _garnishSlotGlass;

    // Where the panel and the glass picture sit while garnishing; they slide away from / back to these.
    private RectTransform _panelRect;
    private RectTransform _glassViewRect;
    private Vector2 _panelHome;
    private Vector2 _glassViewHome;
    private Vector2 _glassViewHomeSize;
    private Rect _glassViewHomeUv;
    private RawImage _glassViewImage;
    private Coroutine _panelSlide;
    private Coroutine _glassSlide;
    private Coroutine _serveReveal;

    /// <summary>True once a pour has fully finished filling — <see cref="TryFinish"/> only succeeds then.</summary>
    public bool CanFinish => _pourComplete;

    /// <summary>True from the moment the pour button was accepted until the next <see cref="Open"/>.</summary>
    public bool HasPoured => _hasPoured;

    /// <summary>The one serving glass on the plate, or null. Owned here; nothing else keeps a reference.</summary>
    public PlacedGlassInstance Glass { get; private set; }

    // ── Unity ──────────────────────────────────────────────

    private void Awake()
    {
        if (_panel != null)
        {
            _panelRect = (RectTransform)_panel.transform;
            _panelHome = _panelRect.anchoredPosition;
        }

        if (_glassView != null)
        {
            _glassViewRect = (RectTransform)_glassView.transform;
            _glassViewHome = _glassViewRect.anchoredPosition;
            _glassViewHomeSize = _glassViewRect.sizeDelta;
            _glassViewImage = _glassView.GetComponent<RawImage>();
            if (_glassViewImage != null) _glassViewHomeUv = _glassViewImage.uvRect;
        }

        // It covers the whole screen, so it must not sit between the pointer and the bar.
        if (_background != null)
        {
            _background.raycastTarget = false;
            _background.gameObject.SetActive(false);
        }
    }

    // The buttons belong to this manager's canvas, so it wires them itself. The panel starts
    // inactive, but onClick listeners can be added to an inactive button just the same.
    private void OnEnable()
    {
        Listen(_btnPour, OnPourClicked);
        Listen(_btnFinish, TryFinish);
        Listen(_btnReset, OnResetClicked);
        Listen(_btnAddIce, OnAddIceClicked);
        Listen(_btnRemoveIce, OnRemoveIceClicked);
        Listen(_btnServe, OnServeClicked);
        Listen(_btnRemake, OnRemakeClicked);
    }

    private void OnDisable()
    {
        Unlisten(_btnPour, OnPourClicked);
        Unlisten(_btnFinish, TryFinish);
        Unlisten(_btnReset, OnResetClicked);
        Unlisten(_btnAddIce, OnAddIceClicked);
        Unlisten(_btnRemoveIce, OnRemoveIceClicked);
        Unlisten(_btnServe, OnServeClicked);
        Unlisten(_btnRemake, OnRemakeClicked);
    }

    private void OnDestroy()
    {
        UnsubscribeFromPouringGlass();
        UnsubscribeFromGarnishSlots();
    }

    private static void Listen(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.AddListener(action);
    }

    private static void Unlisten(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.RemoveListener(action);
    }

    // ── Open / close ───────────────────────────────────────

    /// <summary>A fresh garnish step: nothing poured, and nothing but the glass grid usable until a glass is chosen.</summary>
    public void Open()
    {
        _pourComplete = false;
        _hasPoured = false;
        _activeGarnishSlot = 0;

        StopSlide(ref _panelSlide);
        StopSlide(ref _glassSlide);
        ResetGlassView();
        if (_panel != null) _panel.SetActive(true);
        if (_background != null) _background.gameObject.SetActive(true);
        RefreshButtons();

        // Panel comes in from the right edge, the glass picture from the left, together.
        if (isActiveAndEnabled)
        {
            if (_panelRect != null)
            {
                _panelRect.anchoredPosition = _panelHome + new Vector2(_panelRect.rect.width, 0f);
                _panelSlide = StartCoroutine(SlideIn(_panelRect, _panelHome, () => _panelSlide = null));
            }
            if (_glassViewRect != null)
            {
                _glassView.SetActive(true);
                _glassViewRect.anchoredPosition = _glassViewHome - new Vector2(_glassViewRect.rect.width, 0f);
                _glassSlide = StartCoroutine(SlideIn(_glassViewRect, _glassViewHome, () => _glassSlide = null));
            }
        }
        else
        {
            if (_panelRect != null) _panelRect.anchoredPosition = _panelHome;
            if (_glassView != null) _glassView.SetActive(true);
        }

        Opened?.Invoke();
        GlassChanged?.Invoke(Glass != null);
    }

    /// <summary>Slides the panel off the screen and puts the ice buttons back to "add" for the next time.</summary>
    public void Close()
    {
        StopSlide(ref _panelSlide);
        StopSlide(ref _glassSlide);
        if (_panel != null && _panel.activeInHierarchy && isActiveAndEnabled)
        {
            _panelSlide = StartCoroutine(SlidePanelOut());
            // Off the left edge, then hidden; OpenServe in the same frame stops this and keeps it.
            if (_glassViewRect != null && _glassView.activeSelf)
                _glassSlide = StartCoroutine(SlideGlassOut());
        }
        else
        {
            if (_panel != null) _panel.SetActive(false);
            if (_glassView != null) _glassView.SetActive(false);
        }

        if (_background != null) _background.gameObject.SetActive(false);

        if (_btnAddIce != null) _btnAddIce.gameObject.SetActive(true);
        if (_btnRemoveIce != null) _btnRemoveIce.gameObject.SetActive(false);
    }

    // The one place the glass-first rule is applied to this manager's own buttons.
    private void RefreshButtons()
    {
        bool hasGlass = Glass != null;

        if (_btnPour != null) _btnPour.interactable = hasGlass && !_hasPoured; // one pour per glass
        if (_btnFinish != null) _btnFinish.interactable = hasGlass && _pourComplete;
        if (_btnAddIce != null) _btnAddIce.interactable = hasGlass;
        if (_btnRemoveIce != null) _btnRemoveIce.interactable = hasGlass && !_hasPoured;
    }

    // ── Glass ──────────────────────────────────────────────

    /// <summary>Called by a glass button of the glass grid. Ignored once the glass has been poured into.</summary>
    public void ChooseGlass(SO_GlassOption option)
    {
        if (_glassSpawnPoint == null) return;
        if (_hasPoured) return;

        if (option == null || option.PlacedPrefab == null)
        {
            Debug.LogWarning("[GarnishManager] ChooseGlass needs an SO_GlassOption with a PlacedPrefab.", this);
            return;
        }

        ClearGlass(); // the old glass is about to be replaced — drop its subscriptions with it

        var instance = Instantiate(option.PlacedPrefab, _glassSpawnPoint.position, _glassSpawnPoint.rotation);
        var glass = instance.GetComponent<PlacedGlassInstance>();
        if (glass == null)
        {
            Debug.LogWarning($"[GarnishManager] '{option.PlacedPrefab.name}' has no PlacedGlassInstance component.", this);
            Destroy(instance);
            return;
        }

        glass.Initialize(option);
        Glass = glass;
        _pourComplete = false; // a freshly-placed glass has nothing poured into it yet
        _hasPoured = false;
        _activeGarnishSlot = 0;

        SubscribeToGarnishSlots();
        RefreshButtons();
        GlassChanged?.Invoke(true);
    }

    /// <summary>
    /// Takes the glass off the table. Leaves the poured flags alone: they belong to the step
    /// and are reset by <see cref="Open"/>.
    /// </summary>
    public void ClearGlass()
    {
        if (_pouringGlass != null && _pour != null) _pour.Cancel(); // the pour must let go of the glass before it dies

        UnsubscribeFromPouringGlass();
        UnsubscribeFromGarnishSlots();

        if (Glass != null) Destroy(Glass.gameObject);
        Glass = null;
        RefreshButtons();
        GlassChanged?.Invoke(false);
    }

    // ── Ice ────────────────────────────────────────────────

    /// <summary>
    /// Sets the ice on the glass and swaps the add/remove button, then reports it through
    /// <see cref="IceToggled"/> — the drink's ice flag belongs to the cocktail, not to this
    /// station. Once the pour has started ice can only be added, never taken back out.
    /// </summary>
    public void ToggleIce(bool enable)
    {
        if (!enable && _hasPoured)
        {
            Debug.LogWarning("[GarnishManager] Already poured — ice can no longer be removed.", this);
            return;
        }

        if (Glass != null) Glass.ApplyIce(enable);

        // The icon swap happens here with the glass, in one place, so the two can never drift
        // apart (a lone Inspector listener flipping only one half was a past desync bug).
        if (_btnAddIce != null) _btnAddIce.gameObject.SetActive(!enable);
        if (_btnRemoveIce != null) _btnRemoveIce.gameObject.SetActive(enable);

        IceToggled?.Invoke(enable);

        TryPlay(enable ? _sfxAddIce : _sfxRemoveIce);
    }

    private void OnAddIceClicked() => ToggleIce(true);
    private void OnRemoveIceClicked() => ToggleIce(false);

    // ── Decoration ─────────────────────────────────────────

    /// <summary>
    /// Fills whichever of the glass's 2 shared slots was last clicked on the glass
    /// (GarnishSlotButton). Fresh and Novelty share both slots. Null clears the active slot.
    /// </summary>
    public void ChooseGarnishItem(SO_GarnishItemOption item)
        => Glass?.ApplyGarnishItem(_activeGarnishSlot, item);

    /// <summary>Sets the whole-rim treatment. Null clears it.</summary>
    public void ChooseGarnishRim(SO_GarnishRimOption rim)
        => Glass?.ApplyGarnishRim(rim);

    private void OnGarnishSlotClicked(int slotIndex) => _activeGarnishSlot = slotIndex;

    private void SubscribeToGarnishSlots()
    {
        _garnishSlotGlass = Glass;
        if (_garnishSlotGlass != null) _garnishSlotGlass.GarnishSlotClicked += OnGarnishSlotClicked;
    }

    private void UnsubscribeFromGarnishSlots()
    {
        if (_garnishSlotGlass == null) return;
        _garnishSlotGlass.GarnishSlotClicked -= OnGarnishSlotClicked;
        _garnishSlotGlass = null;
    }

    // ── Pour ───────────────────────────────────────────────

    private void OnPourClicked() => PourRequested?.Invoke();

    /// <summary>
    /// Pours <paramref name="drink"/>'s colours into the glass.
    /// <see cref="TryFinish"/> only succeeds once the fill has finished.
    /// </summary>
    public void Pour(S_Drink drink)
    {
        var glass = Glass;
        if (glass == null)
        {
            Debug.LogWarning("[GarnishManager] No glass placed yet — choose a glass before pouring.", this);
            return;
        }

        UnsubscribeFromPouringGlass(); // re-pouring into the same glass shouldn't double-subscribe

        _hasPoured = true;
        PourStarted?.Invoke(); // one glass per pour

        RefreshButtons();

        _pouringGlass = glass;
        glass.OnFillComplete += OnPourFillComplete;

        glass.Pour(_pour, drink);
    }

    private void OnPourFillComplete()
    {
        UnsubscribeFromPouringGlass();
        _pourComplete = true;
        RefreshButtons();
    }

    private void UnsubscribeFromPouringGlass()
    {
        if (_pouringGlass == null) return;

        _pouringGlass.OnFillComplete -= OnPourFillComplete;
        _pouringGlass = null;
    }

    // ── Finish / reset ─────────────────────────────────────

    /// <summary>The "done garnishing" button. Refused until a pour has finished filling.</summary>
    public void TryFinish()
    {
        if (!_pourComplete)
        {
            Debug.LogWarning("[GarnishManager] Nothing has been poured yet — pour before finishing.", this);
            return;
        }

        FinishRequested?.Invoke();
    }

    private void OnResetClicked() => ResetRequested?.Invoke();

    // ── Serve panel ────────────────────────────────────────

    /// <summary>
    /// The finished glass takes the middle of the screen on its own; the serve buttons follow
    /// after <see cref="_serveButtonsDelay"/>.
    /// </summary>
    public void OpenServe()
    {
        StopSlide(ref _serveReveal);
        StopSlide(ref _glassSlide);
        if (_glassView != null) _glassView.SetActive(true);

        // Close() just hid it on the way out of Garnish; in the same frame, so it never blinks.
        if (_background != null) _background.gameObject.SetActive(true);

        if (isActiveAndEnabled) _serveReveal = StartCoroutine(RevealServe());
        else if (_panelServe != null) _panelServe.SetActive(true);
    }

    public void CloseServe()
    {
        StopSlide(ref _serveReveal);
        if (_panelServe != null) _panelServe.SetActive(false);
        if (_background != null) _background.gameObject.SetActive(false);
        StopSlide(ref _glassSlide);
        ResetGlassView();
        if (_glassView != null) _glassView.SetActive(false);
    }

    private void ResetGlassView()
    {
        if (_glassViewRect == null) return;
        _glassViewRect.anchoredPosition = _glassViewHome;
        _glassViewRect.sizeDelta = _glassViewHomeSize;
        if (_glassViewImage != null) _glassViewImage.uvRect = _glassViewHomeUv;
    }

    private IEnumerator SlideIn(RectTransform rect, Vector2 to, System.Action done)
    {
        yield return Slide(rect, to);
        done();
    }

    private IEnumerator SlideGlassOut()
    {
        yield return Slide(_glassViewRect, _glassViewHome - new Vector2(_glassViewRect.rect.width, 0f));
        _glassView.SetActive(false);
        _glassSlide = null;
    }

    private IEnumerator SlidePanelOut()
    {
        // Off the edge it is anchored to: one panel-width further out.
        yield return Slide(_panelRect, _panelHome + new Vector2(_panelRect.rect.width, 0f));
        _panel.SetActive(false);
        _panelSlide = null;
    }

    private IEnumerator RevealServe()
    {
        if (_glassViewRect != null)
        {
            // GlassView is anchored to the left edge: grow it to the parent's full width and show the
            // whole render texture, so the glass keeps its size and the picture is not stretched.
            Vector2 fromPos = _glassViewRect.anchoredPosition;
            Vector2 fromSize = _glassViewRect.sizeDelta;
            Rect fromUv = _glassViewImage != null ? _glassViewImage.uvRect : default;
            Vector2 toSize = new Vector2(((RectTransform)_glassViewRect.parent).rect.width, fromSize.y);
            Vector2 toPos = new Vector2(0f, fromPos.y);
            Rect toUv = new Rect(0f, 0f, 1f, 1f);

            for (float e = 0f; e < _slideDuration; e += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, e / _slideDuration);
                _glassViewRect.anchoredPosition = Vector2.Lerp(fromPos, toPos, k);
                _glassViewRect.sizeDelta = Vector2.Lerp(fromSize, toSize, k);
                if (_glassViewImage != null)
                    _glassViewImage.uvRect = new Rect(Vector2.Lerp(fromUv.position, toUv.position, k), Vector2.Lerp(fromUv.size, toUv.size, k));
                yield return null;
            }
            _glassViewRect.anchoredPosition = toPos;
            _glassViewRect.sizeDelta = toSize;
            if (_glassViewImage != null) _glassViewImage.uvRect = toUv;
        }

        yield return new WaitForSeconds(_serveButtonsDelay);

        if (_panelServe != null) _panelServe.SetActive(true);
        _serveReveal = null;
    }

    private IEnumerator Slide(RectTransform rect, Vector2 to)
    {
        Vector2 from = rect.anchoredPosition;
        for (float e = 0f; e < _slideDuration; e += Time.deltaTime)
        {
            rect.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, e / _slideDuration));
            yield return null;
        }
        rect.anchoredPosition = to;
    }

    private void StopSlide(ref Coroutine routine)
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }

    private void OnServeClicked() => ServeRequested?.Invoke();
    private void OnRemakeClicked() => RemakeRequested?.Invoke();

    private static void TryPlay(string id)
    {
        if (!string.IsNullOrEmpty(id))
            SoundManager.Instance?.PlaySFX(id);
    }
}
