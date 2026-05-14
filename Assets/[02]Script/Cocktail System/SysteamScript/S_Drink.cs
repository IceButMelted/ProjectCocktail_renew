// ============================================================
//  S_Drink — Cocktail data class
// ============================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static E_Cocktail;

// ── Ingredient Structs ─────────────────────────────────────
// Marked [System.Serializable] so Unity can display them
// in the Inspector inside List<> fields.

[System.Serializable]
public struct AlcoholIngredient
{
    public BaseSpirit Type;
    public int Amount;
}

[System.Serializable]
public struct LiqueurIngredient
{
    public Liqueur Type;
    public int Amount;
}

[System.Serializable]
public struct MixerIngredient
{
    public Mixer Type;
    public int Amount;
}

// ──────────────────────────────────────────────────────────

[System.Serializable]
public class S_Drink
{
    // ── Fields ─────────────────────────────────────────────

    public string Name;
    [TextArea(3, 10)]
    public string Description;
    public TypeOfCocktail AlcoholStrength;
    public Method PreparationMethod;
    public bool AddIce;
    public float Price;

    [Header("Ingredients")]
    public List<AlcoholIngredient> AlcoholList = new List<AlcoholIngredient>();
    public List<LiqueurIngredient> LiqueurList = new List<LiqueurIngredient>();
    public List<MixerIngredient> MixerList = new List<MixerIngredient>();

    public List<GlassType> CompatibleGlasses = new List<GlassType>();

    /// <summary>Visual representation of this cocktail.</summary>
    public Texture2D CocktailSprite;

    // ── Constants ──────────────────────────────────────────

    private const int MAX_TOTAL_PARTS = 10;
    private const float DEFAULT_PRICE = 5f;
    private const string NO_MATCH_NAME = "NOT MATCH ANY";

    // ── Validation ─────────────────────────────────────────

    /// <summary>Returns true when total ingredient parts are below the cap.</summary>
    public bool IsValidRatio()
        => AlcoholList.Sum(a => a.Amount) +
           LiqueurList.Sum(l => l.Amount) +
           MixerList.Sum(m => m.Amount) < MAX_TOTAL_PARTS;

    // ── Update Derived Fields ──────────────────────────────

    /// <summary>
    /// Derives Name by matching this drink against a recipe list.
    /// Sets Name to "NOT MATCH ANY" when no recipe matches.
    /// </summary>
    public void UpdateName(List<S_Drink> recipes)
    {
        var match = recipes?.FirstOrDefault(r => r.IngredientsMatch(this));
        Name = match != null ? match.Name : NO_MATCH_NAME;
    }

    /// <summary>
    /// Derives Price by matching this drink against a recipe list.
    /// Sets Price to the default (5) when no recipe matches.
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

    public int GetTotalAlcohol() => AlcoholList.Sum(a => a.Amount);
    public int GetTotalLiqueur() => LiqueurList.Sum(l => l.Amount);
    public int GetTotalMixer() => MixerList.Sum(m => m.Amount);
    public int GetTotalIngredient() => GetTotalAlcohol() + GetTotalLiqueur() + GetTotalMixer();

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

        if (methodMatch && errors == 0) return Satisfaction.Perfect;
        if (errors <= 2) return Satisfaction.Acceptable;
        return Satisfaction.Fail;
    }

    // ── Mutation ───────────────────────────────────────────

    /// <summary>Adds alcohol if the total ingredient cap allows it.</summary>
    public void TryToAddAlcohol(BaseSpirit alcohol, int amount)
    {
        if (!IsValidRatio()) return;

        int idx = AlcoholList.FindIndex(a => a.Type == alcohol);
        if (idx >= 0)
            AlcoholList[idx] = new AlcoholIngredient { Type = alcohol, Amount = AlcoholList[idx].Amount + amount };
        else
            AlcoholList.Add(new AlcoholIngredient { Type = alcohol, Amount = amount });

        Debug.Log($"[S_Drink] Added {amount}x {alcohol}");
    }

    /// <summary>Adds liqueur if the total ingredient cap allows it.</summary>
    public void TryToAddLiqueur(Liqueur liqueur, int amount)
    {
        if (!IsValidRatio()) return;

        int idx = LiqueurList.FindIndex(l => l.Type == liqueur);
        if (idx >= 0)
            LiqueurList[idx] = new LiqueurIngredient { Type = liqueur, Amount = LiqueurList[idx].Amount + amount };
        else
            LiqueurList.Add(new LiqueurIngredient { Type = liqueur, Amount = amount });

        Debug.Log($"[S_Drink] Added {amount}x {liqueur}");
    }

    /// <summary>Adds mixer if the total ingredient cap allows it.</summary>
    public void TryToAddMixer(Mixer mixer, int amount)
    {
        if (!IsValidRatio()) return;

        int idx = MixerList.FindIndex(m => m.Type == mixer);
        if (idx >= 0)
            MixerList[idx] = new MixerIngredient { Type = mixer, Amount = MixerList[idx].Amount + amount };
        else
            MixerList.Add(new MixerIngredient { Type = mixer, Amount = amount });

        Debug.Log($"[S_Drink] Added {amount}x {mixer}");
    }

    // ── Debug ──────────────────────────────────────────────

    public string GetOfCocktailInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Name);
        sb.AppendLine($"Type: {AlcoholStrength} | Method: {PreparationMethod} | Ice: {AddIce} | Price: {Price}");
        sb.Append("Alcohol: ");
        sb.AppendLine(string.Join(", ", AlcoholList.Select(a => $"{a.Type}x{a.Amount}")));
        sb.Append("Liqueur: ");
        sb.AppendLine(string.Join(", ", LiqueurList.Select(l => $"{l.Type}x{l.Amount}")));
        sb.Append("Mixer:   ");
        sb.AppendLine(string.Join(", ", MixerList.Select(m => $"{m.Type}x{m.Amount}")));
        return sb.ToString();
    }

    // ── Ingredient Matching ────────────────────────────────

    /// <summary>
    /// True when both drinks have identical ingredient types and amounts.
    /// Order-independent: looks up each entry by Type.
    /// </summary>
    public bool IngredientsMatch(S_Drink other)
        => AlcoholListEquals(AlcoholList, other.AlcoholList) &&
           LiqueurListEquals(LiqueurList, other.LiqueurList) &&
           MixerListEquals(MixerList, other.MixerList);

    /// <summary>
    /// Returns the CocktailSprite of the first recipe whose ingredients match this drink.
    /// Returns null if no recipe matches.
    /// </summary>
    public Texture2D GetCocktailTexture(List<S_Drink> recipes)
    {
        var match = recipes?.FirstOrDefault(r => r.IngredientsMatch(this));
        return match?.CocktailSprite;
    }

    // ── Private Helpers ────────────────────────────────────

    private int CountIngredientErrors(S_Drink recipe)
        => CountAlcoholErrors(AlcoholList, recipe.AlcoholList) +
           CountLiqueurErrors(LiqueurList, recipe.LiqueurList) +
           CountMixerErrors(MixerList, recipe.MixerList);

    private static bool AlcoholListEquals(List<AlcoholIngredient> a, List<AlcoholIngredient> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var item in a)
        {
            int bIdx = b.FindIndex(x => x.Type == item.Type);
            if (bIdx < 0 || b[bIdx].Amount != item.Amount) return false;
        }
        return true;
    }

    private static bool LiqueurListEquals(List<LiqueurIngredient> a, List<LiqueurIngredient> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var item in a)
        {
            int bIdx = b.FindIndex(x => x.Type == item.Type);
            if (bIdx < 0 || b[bIdx].Amount != item.Amount) return false;
        }
        return true;
    }

    private static bool MixerListEquals(List<MixerIngredient> a, List<MixerIngredient> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var item in a)
        {
            int bIdx = b.FindIndex(x => x.Type == item.Type);
            if (bIdx < 0 || b[bIdx].Amount != item.Amount) return false;
        }
        return true;
    }

    /// <summary>
    /// Counts mismatched amounts across all keys that appear in either list.
    /// Each unique Type with a differing amount counts as one error.
    /// </summary>
    private static int CountAlcoholErrors(List<AlcoholIngredient> player, List<AlcoholIngredient> recipe)
    {
        var keys = new HashSet<BaseSpirit>(player.Select(a => a.Type));
        keys.UnionWith(recipe.Select(a => a.Type));

        return keys.Count(k =>
        {
            int p = player.FirstOrDefault(a => a.Type == k).Amount;
            int r = recipe.FirstOrDefault(a => a.Type == k).Amount;
            return p != r;
        });
    }

    private static int CountLiqueurErrors(List<LiqueurIngredient> player, List<LiqueurIngredient> recipe)
    {
        var keys = new HashSet<Liqueur>(player.Select(l => l.Type));
        keys.UnionWith(recipe.Select(l => l.Type));

        return keys.Count(k =>
        {
            int p = player.FirstOrDefault(l => l.Type == k).Amount;
            int r = recipe.FirstOrDefault(l => l.Type == k).Amount;
            return p != r;
        });
    }

    private static int CountMixerErrors(List<MixerIngredient> player, List<MixerIngredient> recipe)
    {
        var keys = new HashSet<Mixer>(player.Select(m => m.Type));
        keys.UnionWith(recipe.Select(m => m.Type));

        return keys.Count(k =>
        {
            int p = player.FirstOrDefault(m => m.Type == k).Amount;
            int r = recipe.FirstOrDefault(m => m.Type == k).Amount;
            return p != r;
        });
    }
}