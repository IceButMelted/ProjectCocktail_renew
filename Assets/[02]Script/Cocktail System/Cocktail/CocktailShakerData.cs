using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static E_Cocktail;

// Runtime cocktail instance is created via ScriptableObject.CreateInstance in Awake —
// a fresh in-memory object that never modifies a disk asset.

[System.Serializable] public class AlcoholEvent : UnityEvent<BaseSpirit, int> { }
[System.Serializable] public class LiqueurEvent : UnityEvent<Liqueur, int> { }
[System.Serializable] public class MixerEvent : UnityEvent<Mixer, int> { }

public class CocktailShakerData : MonoBehaviour, IIngredientReceiver
{
    // ── Inspector ──────────────────────────────────────────

    [Header("Ingredient Events")]
    public AlcoholEvent OnAddAlcohol;
    public LiqueurEvent OnAddLiqueur;
    public MixerEvent OnAddMixer;
    public UnityEvent OnAddIngredient;
    public UnityEvent OnResetedCocktail;

    [Header("Ingredient Buttons")]
    public List<GameObject> ingredientButtons = new List<GameObject>();

    // ── Runtime State ──────────────────────────────────────

    /// <summary>Live drink in the shaker. Fresh instance, never an asset reference.</summary>
    public S_Drink CurrentCocktail { get; private set; }

    // ── Unity Lifecycle ────────────────────────────────────

    private void Awake()
    {
        CurrentCocktail = ScriptableObject.CreateInstance<S_Drink>();
        ResetCocktailData();
    }

    private void OnDestroy()
    {
        // Runtime ScriptableObject instances must be explicitly destroyed.
        if (CurrentCocktail != null)
            Destroy(CurrentCocktail);
    }

    // ── Preparation Setters ────────────────────────────────

    public void SetMethod(Method method) => CurrentCocktail.PreparationMethod = method;
    public void SetMethodToShake() => CurrentCocktail.PreparationMethod = Method.Shaking;
    public void SetMethodToMixing() => CurrentCocktail.PreparationMethod = Method.Stirring;
    public void ToggleIce() => CurrentCocktail.AddIce = !CurrentCocktail.AddIce;

    // ── IIngredientReceiver ────────────────────────────────

    public void TryToAddAlcohol(BaseSpirit alcohol, int amount = 1)
    {
        DrinkUtility.TryToAddAlcohol(CurrentCocktail, alcohol, amount);
        
        OnAddIngredient?.Invoke();
    }

    public void TryToAddLiqueur(Liqueur liqueur, int amount = 1)
    {
        DrinkUtility.TryToAddLiqueur(CurrentCocktail, liqueur, amount);
        
        OnAddIngredient?.Invoke();
    }

    public void TryToAddMixer(Mixer mixer, int amount = 1)
    {
        DrinkUtility.TryToAddMixer(CurrentCocktail, mixer, amount);
        
        OnAddIngredient?.Invoke();
    }

    // ── Cocktail Identity Update ───────────────────────────

    /// <summary>Derives name, price, and strength from current ingredients vs recipe list.</summary>
    public void UpdateCocktailInShaker(IReadOnlyList<S_Drink> recipes, Sprite failCocktailSprite)
    {
        DrinkUtility.UpdateTypeOfAlcohol(CurrentCocktail, recipes);
        DrinkUtility.UpdateName(CurrentCocktail, recipes);
        DrinkUtility.UpdatePrice(CurrentCocktail, recipes);
    }

    /// <summary>Returns the best-matching sprite, or <paramref name="fallback"/> if none found.</summary>
    public Sprite GetCurrentSprite(IReadOnlyList<S_Drink> recipes, Sprite fallback)
        => DrinkUtility.GetCocktailSprite(CurrentCocktail, recipes) ?? fallback;

    // ── Reset ──────────────────────────────────────────────

    /// <summary>Fires OnResetedCocktail for Inspector-wired listeners.</summary>
    public void ResetShaker() => OnResetedCocktail?.Invoke();

    /// <summary>Clears all cocktail data to defaults. No UI side-effects.</summary>
    public void ResetCocktailData()
    {
        CurrentCocktail.Name = string.Empty;
        CurrentCocktail.AlcoholStrength = TypeOfCocktail.None;
        CurrentCocktail.PreparationMethod = Method.None;
        CurrentCocktail.AddIce = false;
        CurrentCocktail.Price = 0f;
        CurrentCocktail.AlcoholList = new List<AlcoholIngredient>();
        CurrentCocktail.LiqueurList = new List<LiqueurIngredient>();
        CurrentCocktail.MixerList = new List<MixerIngredient>();
        CurrentCocktail.CompatibleGlasses = new List<GlassType>();
        SetIngredientActive(true);
    }

    // ── Ingredient Button Helpers ──────────────────────────

    /// <summary>Disable all ingredient buttons once the 10-part cap is reached.</summary>
    public void CanIngredientActive()
        => SetIngredientActive(DrinkUtility.GetTotalIngredient(CurrentCocktail) < 10);

    /// <summary>Enable or disable every ingredient button uniformly.</summary>
    public void SetIngredientActive(bool active)
    {
        foreach (var btn in ingredientButtons)
        {
            if (btn == null) continue;
            if (btn.TryGetComponent<Interactable3DObject>(out var interactable)) interactable.Interactable = active;
            if (btn.TryGetComponent<UIPointerSound>(out var sound)) sound.Interactable = active;
            if (btn.TryGetComponent<DragableObject>(out var drag)) drag.Interactable = active;
        }
    }
}