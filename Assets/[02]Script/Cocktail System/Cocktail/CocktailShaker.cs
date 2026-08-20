using UnityEngine;

/// <summary>
/// The shaker as an interactable object: can it be clicked, and what sprite does it show.
///
/// The panel-permission flags it used to own moved to <see cref="ShakerPanelController"/>
/// (plan §4.2) so the HSM can decide what the player may open in each flow state. The
/// SetCanShow* methods below stay because five scenes bind them to UnityEvents; they now
/// forward to that component.
/// </summary>
[RequireComponent(typeof(CocktailShakerData))]
public class CocktailShaker : Interactable_2_5DObject
{
    // ── Inspector ─────────────────────────────────────────-
    [Header("Default Sprite")]
    public Sprite ShakerSprite;

    [Header("UI Panels")]
    [Tooltip("LEGACY — handed to ShakerPanelController on Awake. Assign them there once migrated.")]
    public ToggleActive MethodUI;
    public ToggleActive ServeUI;
    public ToggleActive AddIceUI;

    // ── Private State ──────────────────────────────────────

    private CocktailShakerData _data;
    private ShakerPanelController _panels;
    private bool _canClick = true;

    // ── Accessors ──────────────────────────────────────────

    /// <summary>Sibling data component.</summary>
    public CocktailShakerData Data => _data;

    /// <summary>Panel permissions and toggling.</summary>
    public ShakerPanelController Panels => _panels;

    /// <summary>Passthrough to the live drink for legacy references.</summary>
    public S_Drink CurrentCocktail => _data.CurrentCocktail;

    public void SetCanShowServeUI(bool active) => _panels.SetCanShowServeUI(active);
    public void SetCanShowMethodUI(bool active) => _panels.SetCanShowMethodUI(active);
    public void SetCanShowAddIceUI(bool active) => _panels.SetCanShowAddIceUI(active);
    public void SetCanClick(bool active) => _canClick = active;

    // ── Unity Lifecycle ────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _data = GetComponent<CocktailShakerData>();

        _panels = GetComponent<ShakerPanelController>();
        if (_panels == null) _panels = gameObject.AddComponent<ShakerPanelController>();
        _panels.Initialize(MethodUI, AddIceUI, ServeUI);
    }

    // ── Interaction ────────────────────────────────────────

    /// <summary>Block click when locked or shaker is empty.</summary>
    protected override bool CanClick()
        => _canClick && DrinkQuery.GetTotalIngredient(_data.CurrentCocktail) > 0;

    // ── UI ─────────────────────────────────────────────────

    /// <summary>Show/hide the serve panel.</summary>
    public void SetActiveServe(bool active) => _panels.SetActiveServe(active);

    /// <summary>Toggle whichever panels are currently permitted.</summary>
    public void ToggleUI() => _panels.ToggleUI();

    // ── Reset ──────────────────────────────────────────────

    /// <summary>Reset UI flags and sprite only — cocktail data untouched.</summary>
    public void ResetShakerUI()
    {
        _panels.ResetPermissions();
        SetCanClick(true);
        SetBTNSprite(ShakerSprite, ShakerSprite, ShakerSprite);
    }

    /// <summary>Full reset — cocktail data and UI.</summary>
    public void InternalResetShaker()
    {
        _data.ResetCocktailData();
        ResetShakerUI();
    }

    // ── Debug ──────────────────────────────────────────────
    [ContextMenu("Debug Clicked")]
    public void DebugClicked()
        => Debug.Log("Shaker clicked!\n" + DrinkFormatter.GetCocktailInfo(_data.CurrentCocktail));
}
