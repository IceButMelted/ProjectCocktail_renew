// ============================================================
//  CocktailShakerData.cs — Cocktail state component.
//
//  SOLID — S (Single Responsibility):
//    Owns only the live shaker state (current cocktail) and
//    ingredient-addition routing.  No UI, no interaction logic.
//
//  SOLID — I (Interface Segregation):
//    Implements IIngredientReceiver so any ingredient button
//    only needs that narrow contract — not the full component.
//
//  SOLID — D (Dependency Inversion):
//    All computation is delegated to the stateless DrinkUtility,
//    keeping this component free of algorithm details.
//
//  Runtime cocktail instance:
//    currentCocktail is created via ScriptableObject.CreateInstance
//    in Awake so it is a fresh, mutable, in-memory object that
//    never modifies an asset on disk.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static E_Cocktail;

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

    /// <summary>
    /// The live drink currently in the shaker.
    /// Created as a fresh ScriptableObject instance in Awake —
    /// never a reference to a recipe asset.
    /// </summary>
    public S_Drink CurrentCocktail { get; private set; }

    // ── Unity Lifecycle ────────────────────────────────────

    private void Awake()
    {
        // Create an in-memory, mutable S_Drink with empty lists.
        CurrentCocktail = ScriptableObject.CreateInstance<S_Drink>();
        ResetCocktailData();
    }

    private void OnDestroy()
    {
        // Prevent memory leaks — runtime instances must be manually destroyed.
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
        => DrinkUtility.TryToAddAlcohol(CurrentCocktail, alcohol, amount);

    public void TryToAddLiqueur(Liqueur liqueur, int amount = 1)
        => DrinkUtility.TryToAddLiqueur(CurrentCocktail, liqueur, amount);

    public void TryToAddMixer(Mixer mixer, int amount = 1)
        => DrinkUtility.TryToAddMixer(CurrentCocktail, mixer, amount);

    // ── Cocktail Identity Update ───────────────────────────

    /// <summary>
    /// Derives name, price, and strength from the current ingredients
    /// by comparing against the provided recipe list.
    /// </summary>
    public void UpdateCocktailInShaker(IReadOnlyList<S_Drink> recipes, Sprite failCocktailSprite)
    {
        DrinkUtility.UpdateTypeOfAlcohol(CurrentCocktail, recipes);
        DrinkUtility.UpdateName(CurrentCocktail, recipes);
        DrinkUtility.UpdatePrice(CurrentCocktail, recipes);

        // Sprite resolution is returned to the caller (CocktailShaker / CocktailSystemManager)
        // so this component stays free of UI dependencies.
        // Use GetCurrentSprite() when you need it externally.
    }

    /// <summary>
    /// Returns the best-matching sprite for the current cocktail,
    /// or <paramref name="fallback"/> if nothing is close enough.
    /// </summary>
    public Sprite GetCurrentSprite(IReadOnlyList<S_Drink> recipes, Sprite fallback)
        => DrinkUtility.GetCocktailSprite(CurrentCocktail, recipes) ?? fallback;

    // ── Reset ──────────────────────────────────────────────

    /// <summary>Fires OnResetedCocktail for Inspector-wired listeners.</summary>
    public void ResetShaker() => OnResetedCocktail?.Invoke();

    /// <summary>Clears all cocktail data back to defaults (no UI side-effects).</summary>
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

    /// <summary>Enables ingredient buttons only while under the parts cap.</summary>
    public void CanIngredientActive()
        => SetIngredientActive(DrinkUtility.GetTotalIngredient(CurrentCocktail) < 10);

    /// <summary>Activates or deactivates every ingredient button uniformly.</summary>
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