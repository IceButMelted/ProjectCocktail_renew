using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

/// <summary>
/// The cocktail shaker object. Inherits hover/click/drag handling from
/// <see cref="Interactable3DObject"/>; overrides <see cref="CanClick"/> to
/// guard against empty cocktails or a locked state.
/// All cocktail data lives on the sibling <see cref="CocktailShakerData"/> component.
/// </summary>
[RequireComponent(typeof(CocktailShakerData))]
public class CocktailShaker : Interactable3DObject
{
    [Header("Default Sprite")]
    public Sprite ShakerSprite;

    [Header("UI Panels")]
    public ToggleActive MethodUI;
    public ToggleActive ServeUI;

    [Header("Ingredient Buttons")]
    public List<Interactable3DObject> ingredientButtons = new List<Interactable3DObject>();

    // ── Private State ─────────────────────────────────────────────────────────
    private CocktailShakerData _data;
    private bool _canClick = true;
    private bool _canShowMethodUI = true;
    private bool _canShowServeUI = false;

    // ── Accessors ─────────────────────────────────────────────────────────────
    /// <summary>Direct access to the sibling data component.</summary>
    public CocktailShakerData Data => _data;

    /// <summary>Convenience passthrough — keeps external references (e.g. CocktailSystemManager) unbroken.</summary>
    public S_Drink currentCocktail => _data.currentCocktail;

    public void SetCanShowServeUI(bool active) => _canShowServeUI = active;
    public void SetCanShowMethodUI(bool active) => _canShowMethodUI = active;
    public void SetCanClick(bool active) => _canClick = active;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        _data = GetComponent<CocktailShakerData>();
    }

    // ── Click Overrides ───────────────────────────────────────────────────────
    /// <summary>Block the click if the shaker is locked or has no ingredients.</summary>
    protected override bool CanClick()
        => _canClick && _data.currentCocktail.GetTotalIngredient() > 0;

    // ── UI ────────────────────────────────────────────────────────────────────
    public void SetActiveServe(bool active) => ServeUI.gameObject.SetActive(active);

    public void ToggleUI()
    {
        Debug.Log($"Toggling UI: MethodUI active={MethodUI.gameObject.activeSelf}");
        if (_canShowMethodUI) MethodUI.ToggleAtiveGameObject();
        if (_canShowServeUI) ServeUI.ToggleAtiveGameObject();
    }

    public void SetIngredientActive(bool active)
    {
        foreach (var btn in ingredientButtons)
            btn.Interactable = active;
    }

    public void CanIngredientActive()
        => SetIngredientActive(_data.currentCocktail.GetTotalIngredient() < 10);

    public void DebugClicked()
        => Debug.Log("Shaker clicked! Current cocktail:\n" + _data.currentCocktail.GetOfCocktailInfo());

    // ── Passthrough to data ───────────────────────────────────────────────────
    /// <summary>Passthrough so CocktailSystemManager can call this without knowing about the split.</summary>
    public void UpdateCocktailInShaker(List<S_Drink> normalDrinks, Texture2D failCocktailTexture)
        => _data.UpdateCocktailInShaker(normalDrinks, failCocktailTexture);

    // ── Reset ─────────────────────────────────────────────────────────────────
    /// <summary>Resets UI state back to defaults.</summary>
    public void ResetShakerUI()
    {
        SetIngredientActive(true);
        SetCanShowMethodUI(true);
        SetCanShowServeUI(false);
        SetCanClick(true);
    }

    /// <summary>Full reset — cocktail data + UI. Drop-in replacement for the old InternalResetShaker().</summary>
    public void InternalResetShaker()
    {
        _data.ResetCocktailData();
        ResetShakerUI();
    }
}