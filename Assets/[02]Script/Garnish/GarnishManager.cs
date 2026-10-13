// ============================================================
//  GarnishManager.cs — The garnish station: pick a glass, add ice,
//  decorate, pour, then hand the drink over.
//
//  Owns its own canvas (Canvas_Garnish): the glass / item / rim tabs,
//  the ice buttons and the Pour / Finish / Reset buttons, plus the one
//  glass on the table and the rules around it (one pour per glass, no
//  glass swap and no ice removal after the pour).
//
//  It knows nothing about the game flow, the cocktail or the camera.
//  What it needs comes in as an argument (Pour takes the drink) and
//  what the rest of the game has to react to goes out as an event;
//  GarnishFlowBridge is the one place that connects the two.
// ============================================================

using System;
using UnityEngine;
using UnityEngine.UI;

public class GarnishManager : MonoBehaviour
{
    [Header("Glass")]
    [Tooltip("Where the chosen serving glass spawns (position + rotation).")]
    [SerializeField] private Transform _glassSpawnPoint;

    [Header("Panels")]
    [Tooltip("The whole garnish panel. Opened by Open(), closed by Close().")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private GameObject _panelGlassSelection;
    [SerializeField] private GameObject _panelGarnishItem;
    [SerializeField] private GameObject _panelGarnishRim;

    [Header("Tabs")]
    [SerializeField] private Button _btnGlassSelection;
    [SerializeField] private Button _btnGarnishItem;
    [SerializeField] private Button _btnGarnishRim;

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

    /// <summary>True once a pour has fully finished filling — <see cref="TryFinish"/> only succeeds then.</summary>
    public bool CanFinish => _pourComplete;

    /// <summary>True from the moment the pour button was accepted until the next <see cref="Open"/>.</summary>
    public bool HasPoured => _hasPoured;

    /// <summary>The one serving glass on the plate, or null. Owned here; nothing else keeps a reference.</summary>
    public PlacedGlassInstance Glass { get; private set; }

    // ── Unity ──────────────────────────────────────────────

    // The buttons belong to this manager's canvas, so it wires them itself. The panel starts
    // inactive, but onClick listeners can be added to an inactive button just the same.
    private void OnEnable()
    {
        Listen(_btnGlassSelection, ShowGlassTab);
        Listen(_btnGarnishItem, ShowItemTab);
        Listen(_btnGarnishRim, ShowRimTab);
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
        Unlisten(_btnGlassSelection, ShowGlassTab);
        Unlisten(_btnGarnishItem, ShowItemTab);
        Unlisten(_btnGarnishRim, ShowRimTab);
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

    /// <summary>A fresh garnish step: glass tab first, nothing poured, pour button live.</summary>
    public void Open()
    {
        _pourComplete = false;
        _hasPoured = false;
        _activeGarnishSlot = 0;

        ShowGlassTab();
        if (_btnPour != null) _btnPour.interactable = true;
        if (_panel != null) _panel.SetActive(true);

        Opened?.Invoke();
    }

    /// <summary>Hides the panel and puts the ice buttons back to "add" for the next time.</summary>
    public void Close()
    {
        if (_panel != null) _panel.SetActive(false);

        if (_btnAddIce != null)
        {
            _btnAddIce.gameObject.SetActive(true);
            _btnAddIce.interactable = true;
        }

        if (_btnRemoveIce != null)
        {
            _btnRemoveIce.gameObject.SetActive(false);
            _btnRemoveIce.interactable = true;
        }
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
    }

    /// <summary>
    /// Takes the glass off the table. Leaves the poured flags alone: they belong to the step
    /// and are reset by <see cref="Open"/>.
    /// </summary>
    public void ClearGlass()
    {
        UnsubscribeFromPouringGlass();
        UnsubscribeFromGarnishSlots();

        if (Glass != null) Destroy(Glass.gameObject);
        Glass = null;
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

    // ── Tabs ───────────────────────────────────────────────

    private void ShowGlassTab() => ShowTab(glass: true, item: false, rim: false);
    private void ShowItemTab() => ShowTab(glass: false, item: true, rim: false);
    private void ShowRimTab() => ShowTab(glass: false, item: false, rim: true);

    private void ShowTab(bool glass, bool item, bool rim)
    {
        if (_panelGlassSelection != null) _panelGlassSelection.SetActive(glass);
        if (_panelGarnishItem != null) _panelGarnishItem.SetActive(item);
        if (_panelGarnishRim != null) _panelGarnishRim.SetActive(rim);
    }

    // ── Pour ───────────────────────────────────────────────

    private void OnPourClicked() => PourRequested?.Invoke();

    /// <summary>
    /// Plays the glass's water-rising fill with <paramref name="drink"/>'s colours.
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

        if (_btnAddIce != null) _btnAddIce.interactable = true;
        if (_btnRemoveIce != null) _btnRemoveIce.interactable = false;
        if (_btnPour != null) _btnPour.interactable = false; // one pour per glass — live again on the next Open()

        _pouringGlass = glass;
        glass.OnFillComplete += OnPourFillComplete;

        glass.ApplyDrink(drink);
        glass.StartFill();
    }

    private void OnPourFillComplete()
    {
        UnsubscribeFromPouringGlass();
        _pourComplete = true;
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

    public void OpenServe()
    {
        if (_panelServe != null) _panelServe.SetActive(true);
    }

    public void CloseServe()
    {
        if (_panelServe != null) _panelServe.SetActive(false);
    }

    private void OnServeClicked() => ServeRequested?.Invoke();
    private void OnRemakeClicked() => RemakeRequested?.Invoke();

    private static void TryPlay(string id)
    {
        if (!string.IsNullOrEmpty(id))
            SoundManager.Instance?.PlaySFX(id);
    }
}
