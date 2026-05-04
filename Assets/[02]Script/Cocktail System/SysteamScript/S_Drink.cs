// ============================================================
//  S_Drink — Cocktail data class
// ============================================================

using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static E_Cocktail;

[System.Serializable]
public class S_Drink
{
    // ── Fields ─────────────────────────────────────────────

    public string Name;
    [TextArea(3,10)]
    public string Description;
    public TypeOfCocktail AlcoholStrength;
    public Method PreparationMethod;
    public bool AddIce;
    public float Price;

    [Header("Ingredients")]
    [SerializedDictionary("Alcohol", "Amount")]
    public SerializedDictionary<Alcohol, int> AlcoholList = new SerializedDictionary<Alcohol, int>();

    [SerializedDictionary("Mixer", "Amount")]
    public SerializedDictionary<Mixer, int> MixerList = new SerializedDictionary<Mixer, int>();

    public List<GlassType> CompatibleGlasses = new List<GlassType>();

    /// <summary>Visual representation of this cocktail. Set via SO_Cocktails.</summary>
    public Texture2D CocktailSprite;

    // ── Constants ──────────────────────────────────────────

    private const int MAX_TOTAL_PARTS = 10;
    private const float DEFAULT_PRICE = 5f;
    private const string NO_MATCH_NAME = "NOT MATCH ANY";

    // ── Validation ─────────────────────────────────────────

    /// <summary>Returns true when total ingredient parts are below the cap.</summary>
    public bool IsValidRatio()
        => AlcoholList.Values.Sum() + MixerList.Values.Sum() < MAX_TOTAL_PARTS;

    // ── Update Derived Fields ──────────────────────────────

    /// <summary>
    /// Derives Name by matching this drink against a recipe list.
    /// If no match is found, sets Name to "NOT MATCH ANY".
    /// Same lookup logic as GetTypeOfAlcohol(List).
    /// </summary>
    public void UpdateName(List<S_Drink> recipes)
    {
        var match = recipes?.FirstOrDefault(r =>
            r.IngredientsMatch(this) 
            //&&
            //r.PreparationMethod == PreparationMethod &&
            //r.AddIce == AddIce
            );

        Name = match != null ? match.Name : NO_MATCH_NAME;
    }

    /// <summary>
    /// Derives Price by matching this drink against a recipe list.
    /// If no match is found, sets Price to the default (5).
    /// </summary>
    public void UpdatePrice(List<S_Drink> recipes)
    {
        var match = recipes?.FirstOrDefault(r =>
            r.IngredientsMatch(this) &&
            r.PreparationMethod == PreparationMethod &&
            r.AddIce == AddIce);

        Price = match != null ? match.Price : DEFAULT_PRICE;
    }

    /// <summary>Derives AlcoholStrength from actual alcohol parts in the drink.</summary>
    public void UpdateTypeOfAlcohol() => AlcoholStrength = GetTypeOfAlcohol();

    /// <summary>Derives AlcoholStrength by looking up this drink in a recipe list.</summary>
    public void UpdateTypeOfAlcohol(List<S_Drink> recipes) => AlcoholStrength = GetTypeOfAlcohol(recipes);

    // ── Queries ────────────────────────────────────────────

    public int GetTotalAlcohol() => AlcoholList.Values.Sum();
    public int GetTotalMixer() => MixerList.Values.Sum();
    public int GetTotalIngredient() => GetTotalAlcohol() + GetTotalMixer();

    /// <summary>Calculates AlcoholStrength from actual parts (>= 5 = High, > 0 = Low, else None).</summary>
    public TypeOfCocktail GetTypeOfAlcohol()
    {
        int total = GetTotalAlcohol();
        if (total >= 5) return TypeOfCocktail.HighAlcohol;
        if (total > 0) return TypeOfCocktail.LowAlcohol;
        return TypeOfCocktail.NoneAlcohol;
    }

    /// <summary>Resolves AlcoholStrength from an external recipe list, falls back to calculated value.</summary>
    public TypeOfCocktail GetTypeOfAlcohol(List<S_Drink> recipes)
    {
        var match = recipes?.FirstOrDefault(r => r.Name == Name);
        return match != null ? match.AlcoholStrength : GetTypeOfAlcohol();
    }

    // ── Comparison ─────────────────────────────────────────

    /// <summary>
    /// Compares this drink to another.
    /// technicalOnly = true skips Price and GlassType.
    /// </summary>
    public bool Check(S_Drink other, bool technicalOnly = true)
    {
        if (other == null) return false;

        bool technical = AddIce == other.AddIce &&
                         PreparationMethod == other.PreparationMethod &&
                         IngredientsMatch(other);

        if (technicalOnly) return technical;

        return technical &&
               Mathf.Approximately(Price, other.Price) &&
               CompatibleGlasses.OrderBy(g => g).SequenceEqual(other.CompatibleGlasses.OrderBy(g => g));
    }

    /// <summary>Calculates customer satisfaction against a target recipe.</summary>
    public Satisfaction CalculateSatisfaction(S_Drink recipe)
    {
        int errors = CountIngredientErrors(recipe);

        bool methodMatch = AddIce == recipe.AddIce && PreparationMethod == recipe.PreparationMethod;

        if (methodMatch && errors == 0)
            return Satisfaction.Perfect;

        if ((!methodMatch || errors == 1) && errors <= 1)
            return Satisfaction.Acceptable;

        return Satisfaction.Fail;
    }

    // ── Mutation ───────────────────────────────────────────

    /// <summary>Adds alcohol if the total ingredient cap allows it.</summary>
    public void TryToAddAlcohol(Alcohol alcohol, int amount)
    {
        if (!IsValidRatio()) return;
        AlcoholList[alcohol] = AlcoholList.TryGetValue(alcohol, out int current) ? current + amount : amount;
        Debug.Log($"[S_Drink] Added {amount}x {alcohol}");
    }

    /// <summary>Adds mixer if the total ingredient cap allows it.</summary>
    public void TryToAddMixer(Mixer mixer, int amount)
    {
        if (!IsValidRatio()) return;
        MixerList[mixer] = MixerList.TryGetValue(mixer, out int current) ? current + amount : amount;
        Debug.Log($"[S_Drink] Added {amount}x {mixer}");
    }

    // ── Debug ──────────────────────────────────────────────

    public string GetOfCocktailInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Name);
        sb.AppendLine($"Type: {AlcoholStrength} | Method: {PreparationMethod} | Ice: {AddIce} | Price: {Price}");
        sb.Append("Alcohol: ");
        sb.AppendLine(string.Join(", ", AlcoholList.Select(kv => $"{kv.Key}x{kv.Value}")));
        sb.Append("Mixer:   ");
        sb.AppendLine(string.Join(", ", MixerList.Select(kv => $"{kv.Key}x{kv.Value}")));
        return sb.ToString();
    }

    // ── Private Helpers ────────────────────────────────────

    // Promoted to public so GetCocktailTexture can be called from outside
    // (e.g. recipes[i].IngredientsMatch(playerDrink) in SO_Cocktails)
    public bool IngredientsMatch(S_Drink other)
        => DictEquals(AlcoholList, other.AlcoholList) &&
           DictEquals(MixerList, other.MixerList);

    /// <summary>
    /// Returns the CocktailSprite of the first recipe that matches
    /// this drink's ingredients, method, and ice.
    /// Returns null if no recipe matches.
    /// </summary>
    public Texture2D GetCocktailTexture(List<S_Drink> recipes)
    {
        var match = recipes?.FirstOrDefault(r =>
            r.IngredientsMatch(this) 
            //&&
            //r.PreparationMethod == PreparationMethod &&
            //r.AddIce == AddIce
            );

        return match?.CocktailSprite;
    }

    private int CountIngredientErrors(S_Drink recipe)
        => CountDictErrors(AlcoholList, recipe.AlcoholList) +
           CountDictErrors(MixerList, recipe.MixerList);

    private static bool DictEquals<TKey, TVal>(
        IDictionary<TKey, TVal> a, IDictionary<TKey, TVal> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
            if (!b.TryGetValue(kv.Key, out var val) ||
                !EqualityComparer<TVal>.Default.Equals(kv.Value, val)) return false;
        return true;
    }

    private static int CountDictErrors<TKey>(
        IDictionary<TKey, int> player, IDictionary<TKey, int> recipe)
    {
        var keys = new HashSet<TKey>(player.Keys);
        keys.UnionWith(recipe.Keys);
        return keys.Count(k =>
        {
            player.TryGetValue(k, out int p);
            recipe.TryGetValue(k, out int r);
            return p != r;
        });
    }
}