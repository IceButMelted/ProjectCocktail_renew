using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;
using static E_Cocktail;
using static WaterSlosh;

// Runtime cocktail instance is created via ScriptableObject.CreateInstance in Awake —
// a fresh in-memory object that never modifies a disk asset.

[System.Serializable] public class AlcoholEvent : UnityEvent<BaseSpirit, int> { }
[System.Serializable] public class LiqueurEvent : UnityEvent<Liqueur, int> { }
[System.Serializable] public class MixerEvent : UnityEvent<Mixer, int> { }

public class CocktailShakerData : MonoBehaviour, IIngredientReceiver, ITooltipProvider
{
    // ── Inspector ──────────────────────────────────────────
    [Header("Glass Cocktail")]
    public WaterSlosh glassWaterSlosh;
    public void StartFill() => glassWaterSlosh.StartFilling();
    public void StopFill() => glassWaterSlosh.StopFilling();
    public void FinishFill() => glassWaterSlosh.FinishFilling(); 
    


    [Header("Ingredient Events")]
    public AlcoholEvent OnAddAlcohol;
    public LiqueurEvent OnAddLiqueur;
    public MixerEvent OnAddMixer;
    public UnityEvent OnAddIngredient;
    public UnityEvent OnResetedCocktail;

    [Header("Ingredient Buttons")]
    public List<GameObject> ingredientButtons = new List<GameObject>();
    public List<GameObject> bookUi = new List<GameObject>();

    [Header("Glass And Ice")]
    [SerializedDictionary("Glass Type", "Visual")]
    public SerializableDictionary<GlassType, VisualCocktailGlass> GlassVisualData = new SerializableDictionary<GlassType, VisualCocktailGlass>();
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
    public void ToggleIce(bool enable) => CurrentCocktail.AddIce = enable;  

    

    // ── IIngredientReceiver ────────────────────────────────

    public void TryToAddAlcohol(BaseSpirit alcohol, int amount = 1)
    {
        if (!DrinkBuilder.TryAddAlcohol(CurrentCocktail, alcohol, amount)) return;
        OnAddIngredient?.Invoke();
    }

    public void TryToAddLiqueur(Liqueur liqueur, int amount = 1)
    {
        if (!DrinkBuilder.TryAddLiqueur(CurrentCocktail, liqueur, amount)) return;
        OnAddIngredient?.Invoke();
    }

    public void TryToAddMixer(Mixer mixer, int amount = 1)
    {
        if (!DrinkBuilder.TryAddMixer(CurrentCocktail, mixer, amount)) return;
        OnAddIngredient?.Invoke();
    }

    // ── Cocktail Identity Update ───────────────────────────

    /// <summary>Most recent comparison against the recipe database. Refreshed by <see cref="UpdateCocktailInShaker"/>.</summary>
    public RecipeMatch LastMatch { get; private set; } = RecipeMatch.None;

    /// <summary>
    /// Derives name, price, strength, glass and colour from the current ingredients.
    ///
    /// One recipe scan, one identity. The previous version called five Update* methods
    /// that each re-scanned the whole database and depended on each other's ordering.
    /// </summary>
    public void UpdateCocktailInShaker(IReadOnlyList<S_Drink> recipes)
    {
        LastMatch = DrinkDeviation.FindBestMatch(CurrentCocktail, recipes);
        DrinkBuilder.ApplyRecipeIdentity(CurrentCocktail, LastMatch);

        SetColorAndGlass(glassWaterSlosh, CurrentCocktail);
    }

    /// <summary>Returns the matched recipe's sprite, or <paramref name="fallback"/> when nothing matched.</summary>
    public Sprite GetCurrentSprite(IReadOnlyList<S_Drink> recipes, Sprite fallback)
    {
        var match = DrinkDeviation.FindBestMatch(CurrentCocktail, recipes);
        return match.IsRecognised ? (match.Recipe.CocktailSprite ?? fallback) : fallback;
    }

    // ── Reset ──────────────────────────────────────────────

    /// <summary>Fires OnResetedCocktail for Inspector-wired listeners.</summary>
    public void ResetShaker() => OnResetedCocktail?.Invoke();

    /// <summary>Clears all cocktail data to defaults. No UI side-effects beyond re-enabling buttons.</summary>
    public void ResetCocktailData()
    {
        DrinkBuilder.Clear(CurrentCocktail);
        LastMatch = RecipeMatch.None;

        if (glassWaterSlosh != null) glassWaterSlosh.waterLevel = 0f;

        SetIngredientActive(true);
    }

    // ── Ingredient Button Helpers ──────────────────────────

    /// <summary>Disable all ingredient buttons once the part cap is reached.</summary>
    public void CanIngredientActive()
        => SetIngredientActive(DrinkQuery.HasRoom(CurrentCocktail));

    /// <summary>Enable or disable every ingredient button uniformly.</summary>
    public void SetIngredientActive(bool active)
    {
        foreach (var btn in ingredientButtons)
        {
            if (btn == null) continue;
            if (btn.TryGetComponent<Interactable_2_5DObject>(out var interactable)) interactable.Interactable = active;
            if (btn.TryGetComponent<UIPointerSound>(out var sound)) sound.Interactable = active;
            if (btn.TryGetComponent<DragableObject>(out var drag)) drag.Interactable = active;
            if (btn.TryGetComponent<ScaleOnHover>(out var hover)) hover.Interactable = active;
            if (btn.TryGetComponent<Interactable_3DObject>(out var objec)) objec.Interactable = active;
            if (btn.TryGetComponent<HoverTooltip>(out var tooltip)) tooltip.Interactable = active;
        }
    }

    public void SetBookUiActive(bool active)
    {
        foreach (var btn in bookUi)
        {
            if (btn == null) continue;
            if (btn.TryGetComponent<Interactable_2_5DObject>(out var interactable)) interactable.Interactable = active;
            if (btn.TryGetComponent<UIPointerSound>(out var sound)) sound.Interactable = active;
            if (btn.TryGetComponent<DragableObject>(out var drag)) drag.Interactable = active;
            if (btn.TryGetComponent<ScaleOnHover>(out var hover)) hover.Interactable = active;
            if (btn.TryGetComponent<Interactable_3DObject>(out var objec)) objec.Interactable = active;
            if (btn.TryGetComponent<HoverTooltip>(out var tooltip)) tooltip.Interactable = active;
            if (btn.TryGetComponent<BookUI_V2>(out var bookUI)) bookUI.SetActive(active);
        }
    }

    public void SetColorAndGlass(WaterSlosh d, S_Drink r) {
        if (d == null) { Debug.LogWarning("WaterSlosh ref is null"); return; }

        d.waterColorTop = r.waterColorTop;
        d.waterColorBottom = r.waterColorBottom;
        
        GlassType compateGlass = r.CompatibleGlass;
        GlassVisualData.TryGetValue(compateGlass, out var visualData);

        if (visualData != null)
        {
            d.UpdateVisual(visualData.IceSprite, visualData.GlassSprite, visualData.WaterSprite);
        }
        else
        {
            Debug.LogWarning($"No visual data found for glass type {compateGlass}");
        }

        glassWaterSlosh.UpdateColor();
    }

    public string GetTooltipText()
    {
        return DrinkFormatter.GetCocktailIngredient(CurrentCocktail);
    }

    [System.Serializable]
    public class VisualCocktailGlass
    {
        public Sprite GlassSprite;
        public Sprite WaterSprite;
        public Sprite IceSprite;
    }
}