using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static E_Cocktail;

[System.Serializable] public class AlcoholEvent : UnityEvent<Alcohol, int> { }
[System.Serializable] public class MixerEvent : UnityEvent<Mixer, int> { }

/// <summary>
/// The cocktail shaker object. Inherits hover/click/drag handling from
/// <see cref="Interactable3DObject"/>; overrides <see cref="CanClick"/> to
/// guard against empty cocktails or a locked state, and <see cref="OnClick"/>
/// for shaker-specific behaviour.
/// </summary>
public class CocktailShaker : Interactable3DObject
{
    [Header("Default Sprite")]
    public Sprite ShakerSprite;

    [Header("Ingredient Events")]
    public AlcoholEvent OnAddAlcohol;
    public MixerEvent OnAddMixer;
    public UnityEvent OnAddIngredient;
    public UnityEvent OnResetedCocktail;


    [Header("Cocktail State")]
    public S_Drink currentCocktail;

    [Header("UI Panels")]
    public ToggleActive MethodUI;
    public ToggleActive ServeUI;

    [Header("Ingredient Buttons")]
    public List<Interactable3DObject> ingredientButtons = new List<Interactable3DObject>();

    // ── Private State ────────────────────────────────────────────────────────
    private bool _canClick = true;
    private bool _canShowMethodUI = true;
    private bool _canShowServeUI = false;

    // ── Accessors ────────────────────────────────────────────────────────────
    public void SetCanShowServeUI(bool active) => _canShowServeUI = active;
    public void SetCanShowMethodUI(bool active) => _canShowMethodUI = active;
    public void SetCanClick(bool active) => _canClick = active;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    protected override void Awake() => base.Awake();

    // ── Click Overrides ───────────────────────────────────────────────────────

    /// <summary>
    /// Block the click if the shaker is locked or has no ingredients.
    /// </summary>
    protected override bool CanClick()
        => _canClick && currentCocktail.GetTotalIngredient() > 0;

    /// <summary>
    /// Runs after a confirmed, guarded click — add any shaker-specific
    /// behaviour here (e.g. triggering a shake animation, playing a sound).
    /// <see cref="Interactable3DObject.OnClicked"/> has already been invoked.
    /// </summary>

    // ── Ingredient Helpers ────────────────────────────────────────────────────
    public void SetMethod(Method method) => currentCocktail.PreparationMethod = method;
    public void SetMethodToShake() => currentCocktail.PreparationMethod = Method.Shaking;
    public void SetMethodToMixing() => currentCocktail.PreparationMethod = Method.Mixing;
    public void SetIceAddIce() => currentCocktail.AddIce = true;
    public void TryToAddAlcohol(Alcohol a, int n = 1) => currentCocktail.TryToAddAlcohol(a, n);
    public void TryToAddMixer(Mixer m, int n = 1) => currentCocktail.TryToAddMixer(m, n);

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
    {
        if(currentCocktail.GetTotalIngredient() >= 10)
        {
            SetIngredientActive(false);
        }
        else
        {
            SetIngredientActive(true);
        }
    }

    public void DebugClicked() { 
        Debug.Log("Shaker clicked! Current cocktail:\n" + currentCocktail.GetOfCocktailInfo());
    }



    // ── Reset ─────────────────────────────────────────────────────────────────
    public void ResetShaker()
    {
        OnResetedCocktail?.Invoke();
    }

    public void InternalResetShaker() {
        currentCocktail.Name = string.Empty;
        currentCocktail.AlcoholStrength = TypeOfCocktail.None;
        currentCocktail.PreparationMethod = Method.None;
        currentCocktail.AddIce = false;
        currentCocktail.AlcoholList = new SerializedDictionary<Alcohol, int>();
        currentCocktail.MixerList = new SerializedDictionary<Mixer, int>();
        currentCocktail.CompatibleGlasses = new List<GlassType>();
        SetIngredientActive(true);

        SetCanShowMethodUI(true);
        SetCanShowServeUI(false);
        SetCanClick(true);
    }
}