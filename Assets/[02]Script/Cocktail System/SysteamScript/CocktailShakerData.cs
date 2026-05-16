using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static E_Cocktail;

[System.Serializable] public class AlcoholEvent : UnityEvent<BaseSpirit, int> { }
[System.Serializable] public class LiqueurEvent : UnityEvent<Liqueur, int> { }
[System.Serializable] public class MixerEvent : UnityEvent<Mixer, int> { }

/// <summary>
/// Pure cocktail data component — no interaction logic.
/// Holds the current cocktail state, ingredient helpers, and reset.
/// Sits as a sibling component alongside <see cref="CocktailShaker"/>.
/// </summary>
public class CocktailShakerData : MonoBehaviour
{
    [Header("Ingredient Events")]
    public AlcoholEvent OnAddAlcohol;
    public LiqueurEvent OnAddLiqueur;
    public MixerEvent OnAddMixer;
    public UnityEvent OnAddIngredient;
    public UnityEvent OnResetedCocktail;

    [Header("Cocktail State")]
    public S_Drink currentCocktail;

    [Header("Ingredient Buttons")]
    public List<Interactable3DObject> ingredientButtons = new List<Interactable3DObject>();

    // ── Ingredient Helpers ────────────────────────────────────────────────────
    public void SetMethod(Method method) => currentCocktail.PreparationMethod = method;
    public void SetMethodToShake() => currentCocktail.PreparationMethod = Method.Shaking;
    public void SetMethodToMixing() => currentCocktail.PreparationMethod = Method.Mixing;
    public void SetIceAddIce() => currentCocktail.AddIce = true;
    public void TryToAddAlcohol(BaseSpirit a, int n = 1) => currentCocktail.TryToAddAlcohol(a, n);
    public void TryToAddLiqueur(Liqueur l, int n = 1) => currentCocktail.TryToAddLiqueur(l, n);
    public void TryToAddMixer(Mixer m, int n = 1) => currentCocktail.TryToAddMixer(m, n);

    // ── Cocktail Identity ─────────────────────────────────────────────────────
    /// <summary>Derive identity of whatever is currently in the shaker and update visuals.</summary>
    public void UpdateCocktailInShaker(List<S_Drink> normalDrinks, Sprite failCocktailSprite)
    {
        currentCocktail.UpdateTypeOfAlcohol(normalDrinks);
        currentCocktail.UpdateName(normalDrinks);
        currentCocktail.UpdatePrice(normalDrinks);

        Sprite sprite = currentCocktail.GetCocktailTexture(normalDrinks) ?? failCocktailSprite;
        // SetBTNSprite(sprite, sprite, sprite); — handled by CocktailShaker / CocktailSystemManager
    }

    // ── Reset ─────────────────────────────────────────────────────────────────
    /// <summary>Fires OnResetedCocktail — wire listeners in the Inspector.</summary>
    public void ResetShaker() => OnResetedCocktail?.Invoke();

    /// <summary>Wipes cocktail data back to defaults (no UI side-effects).</summary>
    public void ResetCocktailData()
    {
        currentCocktail.Name = string.Empty;
        currentCocktail.AlcoholStrength = TypeOfCocktail.None;
        currentCocktail.PreparationMethod = Method.None;
        currentCocktail.AddIce = false;
        currentCocktail.AlcoholList = new List<AlcoholIngredient>();
        currentCocktail.LiqueurList = new List<LiqueurIngredient>();
        currentCocktail.MixerList = new List<MixerIngredient>();
        currentCocktail.CompatibleGlasses = new List<GlassType>();
        SetIngredientActive(true);
    }

    // ── Ingredient Button Helpers ─────────────────────────────────────────────
    public void CanIngredientActive()
        => SetIngredientActive(currentCocktail.GetTotalIngredient() < 10);

    public void SetIngredientActive(bool active)
    {
        foreach (var btn in ingredientButtons)
            btn.Interactable = active;
    }
}