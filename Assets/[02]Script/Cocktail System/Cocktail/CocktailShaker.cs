using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

[RequireComponent(typeof(CocktailShakerData))]
public class CocktailShaker : Interactable_2_5DObject
{
    // ── Inspector ──────────────────────────────────────────

    [Header("Default Sprite")]
    public Sprite ShakerSprite;

    [Header("UI Panels")]
    public ToggleActive MethodUI;
    public ToggleActive ServeUI;

    // ── Private State ──────────────────────────────────────

    private CocktailShakerData _data;
    private bool _canClick = true;
    private bool _canShowMethodUI = true;
    private bool _canShowServeUI = false;

    // ── Accessors ──────────────────────────────────────────

    /// <summary>Sibling data component.</summary>
    public CocktailShakerData Data => _data;

    /// <summary>Passthrough to <c>Data.CurrentCocktail</c> for legacy references.</summary>
    public S_Drink CurrentCocktail => _data.CurrentCocktail;

    public void SetCanShowServeUI(bool active) => _canShowServeUI = active;
    public void SetCanShowMethodUI(bool active) => _canShowMethodUI = active;
    public void SetCanClick(bool active) => _canClick = active;

    // ── Unity Lifecycle ────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _data = GetComponent<CocktailShakerData>();
    }

    // ── Interaction ────────────────────────────────────────

    /// <summary>Block click when locked or shaker is empty.</summary>
    protected override bool CanClick()
        => _canClick && DrinkUtility.GetTotalIngredient(_data.CurrentCocktail) > 0;

    // ── UI ─────────────────────────────────────────────────

    /// <summary>Show/hide the serve panel.</summary>
    public void SetActiveServe(bool active) => ServeUI.gameObject.SetActive(active);

    /// <summary>Toggle whichever panels are currently permitted.</summary>
    public void ToggleUI()
    {
        if (_canShowMethodUI) MethodUI.ToggleActiveGameObject();
        if (_canShowServeUI) ServeUI.ToggleActiveGameObject();
    }

    // ── Reset ──────────────────────────────────────────────

    /// <summary>Reset UI flags and sprite only — cocktail data untouched.</summary>
    public void ResetShakerUI()
    {
        SetCanShowMethodUI(true);
        SetCanShowServeUI(false);
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
        => Debug.Log("Shaker clicked!\n" + DrinkUtility.GetCocktailInfo(_data.CurrentCocktail));
}