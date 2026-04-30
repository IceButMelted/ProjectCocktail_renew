using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static E_Cocktail;

[System.Serializable] public class AlcoholEvent : UnityEvent<Alcohol, int> { }
[System.Serializable] public class MixerEvent   : UnityEvent<Mixer, int>   { }

public class CocktailShaker : BTN_2_5D
{
    [Header("Default Texture")]
    public Texture2D ShakerSprite;

    [Header("Ingredient Events")]
    public UnityEvent   OnAddIngredient;
    public AlcoholEvent OnAddAlcohol;
    public MixerEvent   OnAddMixer;
    public UnityEvent   OnResetCocktail;

    [Header("Cocktail State")]
    public S_Drink currentCocktail;

    [Header("UI Panels")]
    public ToggleActive MethodUI;
    public ToggleActive ServeUI;

    [Header("Ingredient Buttons")]
    public List<IngredientButton> ingredientButtons = new List<IngredientButton>();

    // ── Private State ────────────────────────────────────
    private bool _canClick        = true;
    private bool _canShowMethodUI = true;
    private bool _canShowServeUI  = false;

    // ── Accessors ────────────────────────────────────────
    public void SetCanShowServeUI(bool active)  => _canShowServeUI  = active;
    public void SetCanShowMethodUI(bool active) => _canShowMethodUI = active;
    public void SetCanClick(bool active)        => _canClick        = active;

    // ── Unity ────────────────────────────────────────────
    protected override void Awake()  => base.Awake();
    protected override void Update() => base.Update();

    // ── Ingredient Helpers ───────────────────────────────
    public void SetMethod(Method method)              => currentCocktail.PreparationMethod = method;
    public void SetMethodToShake()                    => currentCocktail.PreparationMethod = Method.Shaking;
    public void SetMethodToMixing()                   => currentCocktail.PreparationMethod = Method.Mixing;
    public void SetIceAddIce()                        => currentCocktail.AddIce = true;
    public void TryToAddAlcohol(Alcohol a, int n = 1) => currentCocktail.TryToAddAlcohol(a, n);
    public void TryToAddMixer  (Mixer   m, int n = 1) => currentCocktail.TryToAddMixer  (m, n);

    // ── UI ───────────────────────────────────────────────
    public void SetActiveServe(bool active) => ServeUI.gameObject.SetActive(active);

    public void ToggleUI()
    {
        if (_canShowMethodUI) MethodUI.ToggleAtiveGameObject();
        if (_canShowServeUI)  ServeUI .ToggleAtiveGameObject();
    }

    public void ToggleCanClickIngredientBTN(bool active)
    {
        foreach (var btn in ingredientButtons)
            btn.enabled = active;
    }

    // ── Reset ────────────────────────────────────────────
    public void ResetShaker()
    {
        currentCocktail.Name              = string.Empty;
        currentCocktail.AlcoholStrength   = TypeOfCocktail.None;
        currentCocktail.PreparationMethod = Method.None;
        currentCocktail.AddIce            = false;
        currentCocktail.AlcoholList       = new SerializedDictionary<Alcohol, int>();
        currentCocktail.MixerList         = new SerializedDictionary<Mixer, int>();
        currentCocktail.CompatibleGlasses = new List<GlassType>();

        SetCanShowMethodUI(true);
        SetCanShowServeUI(false);
        SetCanClick(true);
        SetBTNSprite(ShakerSprite, ShakerSprite, ShakerSprite);
    }

    // ── Click Override ───────────────────────────────────
    protected override void OnClick(InputAction.CallbackContext context)
    {
        if (!_canClick) return;
        if (currentCocktail.GetTotalIngredient() <= 0) return;
        base.OnClick(context);
    }
}
