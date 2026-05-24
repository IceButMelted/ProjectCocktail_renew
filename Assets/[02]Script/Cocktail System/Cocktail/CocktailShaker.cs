// ============================================================
//  CocktailShaker.cs — Interaction + UI shell for the shaker.
//
//  SOLID — S (Single Responsibility):
//    Owns interaction guarding (CanClick) and UI state only.
//    All cocktail data and ingredient logic live in the sibling
//    CocktailShakerData component.
//
//  SOLID — D (Dependency Inversion):
//    UpdateCocktailInShaker accepts IDrinkRepository so this
//    component never couples to SO_CocktailList directly.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

[RequireComponent(typeof(CocktailShakerData))]
public class CocktailShaker : Interactable3DObject
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

    /// <summary>Direct access to the sibling data component.</summary>
    public CocktailShakerData Data => _data;

    /// <summary>
    /// Convenience passthrough — keeps external references unbroken.
    /// Prefer <c>Data.CurrentCocktail</c> for new code.
    /// </summary>
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

    // ── Click Override ─────────────────────────────────────

    /// <summary>Block the click if locked or the shaker is empty.</summary>
    protected override bool CanClick()
        => _canClick && DrinkUtility.GetTotalIngredient(_data.CurrentCocktail) > 0;

    // ── UI ─────────────────────────────────────────────────

    public void SetActiveServe(bool active) => ServeUI.gameObject.SetActive(active);

    public void ToggleUI()
    {
        Debug.Log($"Toggling UI: MethodUI active={MethodUI.gameObject.activeSelf}");
        if (_canShowMethodUI) MethodUI.ToggleAtiveGameObject();
        if (_canShowServeUI) ServeUI.ToggleAtiveGameObject();
    }

    public void DebugClicked()
        => Debug.Log("Shaker clicked!\n" + DrinkUtility.GetCocktailInfo(_data.CurrentCocktail));

    // ── Passthrough ────────────────────────────────────────

    /// <summary>
    /// Derives cocktail identity, updates visuals.
    /// Accepts the abstracted repository so this component
    /// has no direct dependency on SO_CocktailList.
    /// </summary>
    public void UpdateCocktailInShaker(IDrinkRepository repository, Sprite failCocktailSprite)
    {
        var recipes = repository.GetDrinks();
        _data.UpdateCocktailInShaker(recipes, failCocktailSprite);

        Sprite sprite = _data.GetCurrentSprite(recipes, failCocktailSprite);
        SetBTNSprite(sprite, sprite, sprite);
    }

    // ── Reset ──────────────────────────────────────────────

    /// <summary>Resets UI state back to defaults without touching cocktail data.</summary>
    public void ResetShakerUI()
    {
        SetCanShowMethodUI(true);
        SetCanShowServeUI(false);
        SetCanClick(true);
        SetBTNSprite(ShakerSprite, ShakerSprite, ShakerSprite);
    }

    /// <summary>Full reset — cocktail data AND UI.</summary>
    public void InternalResetShaker()
    {
        _data.ResetCocktailData();
        ResetShakerUI();
    }
}