// ============================================================
//  UtilityDrink.cs — All cocktail operations, zero state.
//
//  SOLID — S (Single Responsibility):
//    One class, one job: compute things about S_Drink objects.
//    No MonoBehaviour, no ScriptableObject, no Unity lifecycle.
//
//  SOLID — O (Open / Closed):
//    New scoring rules or matching strategies are added as new
//    static methods — existing callers are never broken.
//
//  Every method is pure or makes its side-effects explicit
//  (Update* methods mutate the passed-in runtime instance only,
//   never a recipe asset).
// ============================================================

using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static E_Cocktail;

public static class DrinkUtility
{
    // ── Constants ──────────────────────────────────────────

    private const int   MAX_TOTAL_PARTS  = 10;
    private const float DEFAULT_PRICE    = 5f;
    private const string NO_MATCH_NAME   = "NOT MATCH ANY";

    // ══════════════════════════════════════════════════════
    // Queries — read-only, return values
    // ══════════════════════════════════════════════════════

    public static int GetTotalAlcohol(S_Drink d)
        => d.AlcoholList.Sum(a => a.Amount);

    public static int GetTotalLiqueur(S_Drink d)
        => d.LiqueurList.Sum(l => l.Amount);

    public static int GetTotalMixer(S_Drink d)
        => d.MixerList.Sum(m => m.Amount);

    public static int GetTotalIngredient(S_Drink d)
        => GetTotalAlcohol(d) + GetTotalLiqueur(d) + GetTotalMixer(d);

    /// <summary>True when total parts are below the per-drink cap.</summary>
    public static bool IsValidRatio(S_Drink d)
        => GetTotalIngredient(d) < MAX_TOTAL_PARTS;

    // ── Alcohol Strength ───────────────────────────────────

    /// <summary>
    /// Derives strength from actual alcohol + liqueur parts in <paramref name="d"/>.
    /// Rule: >= 5 parts → High, > 0 → Low, else None.
    /// </summary>
    public static TypeOfCocktail GetTypeOfAlcohol(S_Drink d)
    {
        // Respect a manually-set override (recipe assets with explicit strength)
        if (d.AlcoholStrength != TypeOfCocktail.None)
            return d.AlcoholStrength;

        int total = GetTotalAlcohol(d) + GetTotalLiqueur(d);
        if (total >= 5) return TypeOfCocktail.HighAlcohol;
        if (total >  0) return TypeOfCocktail.LowAlcohol;
        return TypeOfCocktail.NoneAlcohol;
    }

    /// <summary>
    /// Resolves strength by looking up <paramref name="d"/> by name in
    /// <paramref name="recipes"/>; falls back to the calculated value.
    /// </summary>
    public static TypeOfCocktail GetTypeOfAlcohol(S_Drink d, IReadOnlyList<S_Drink> recipes)
    {
        var match = recipes?.FirstOrDefault(r => r.Name == d.Name);
        return match != null ? match.AlcoholStrength : GetTypeOfAlcohol(d);
    }

    // ══════════════════════════════════════════════════════
    // Mutations — write to a runtime S_Drink instance only.
    // Never call these on recipe assets.
    // ══════════════════════════════════════════════════════

    /// <summary>Writes the derived name onto <paramref name="d"/>.</summary>
    public static void UpdateName(S_Drink d, IReadOnlyList<S_Drink> recipes)
    {
        var (match, errors) = FindBestIngredientMatch(d, recipes);

        d.Name = (match != null && errors <= 2) ? match.Name : NO_MATCH_NAME;
    }

    /// <summary>Writes the derived price onto <paramref name="d"/>.</summary>
    public static void UpdatePrice(S_Drink d, IReadOnlyList<S_Drink> recipes)
    {
        var (match, errors) = FindBestIngredientMatch(d, recipes);

        d.Price = (match != null && errors <= 2) ? match.Price : DEFAULT_PRICE;
    }

    /// <summary>
    /// Writes the derived top and bottom colors onto <paramref name="d"/>.
    /// </summary>
    public static void UpdateColorInGlass(S_Drink d, IReadOnlyList<S_Drink> recipes)
    {
        var (match, errors) = FindBestIngredientMatch(d, recipes);

        bool hasMatch = match != null && errors <= 2;
        d.waterColorTop = hasMatch ? match.waterColorTop : Color.black;
        d.waterColorBottom = hasMatch ? match.waterColorBottom : Color.black;

        Debug.Log("Update Color");
    }

    /// <summary>Writes the derived alcohol strength onto <paramref name="d"/>.</summary>
    public static void UpdateTypeOfAlcohol(S_Drink d)
        => d.AlcoholStrength = ComputeTypeOfAlcohol(d);

    /// <summary>Writes the recipe-looked-up strength onto <paramref name="d"/>.</summary>
    public static void UpdateTypeOfAlcohol(S_Drink d, IReadOnlyList<S_Drink> recipes)
        => d.AlcoholStrength = GetTypeOfAlcohol(d, recipes);

    public static void UpdateGlassType(S_Drink d, IReadOnlyList<S_Drink> recipes)
    {
        var (match, errors) = FindBestIngredientMatch(d, recipes);
        bool hasMatch = match != null && errors <= 2;
        //if match get the glass type from the match, else Random glass type
        d.CompatibleGlass = hasMatch ? match.CompatibleGlass : GlassType.Rocks;
        //d.compatableGlass = hasMatch ? match.compatableGlass : ;
    }

    // ── Ingredient Addition ────────────────────────────────

    /// <summary>Adds alcohol if the total-parts cap allows it.</summary>
    public static void TryToAddAlcohol(S_Drink d, BaseSpirit alcohol, int amount)
    {
        if (!IsValidRatio(d)) return;

        int idx = d.AlcoholList.FindIndex(a => a.Type == alcohol);
        if (idx >= 0)
            d.AlcoholList[idx] = new AlcoholIngredient { Type = alcohol, Amount = d.AlcoholList[idx].Amount + amount };
        else
            d.AlcoholList.Add(new AlcoholIngredient { Type = alcohol, Amount = amount });

        Debug.Log($"[DrinkUtility] Added {amount}x {alcohol}");
    }

    /// <summary>Adds liqueur if the total-parts cap allows it.</summary>
    public static void TryToAddLiqueur(S_Drink d, Liqueur liqueur, int amount)
    {
        if (!IsValidRatio(d)) return;

        int idx = d.LiqueurList.FindIndex(l => l.Type == liqueur);
        if (idx >= 0)
            d.LiqueurList[idx] = new LiqueurIngredient { Type = liqueur, Amount = d.LiqueurList[idx].Amount + amount };
        else
            d.LiqueurList.Add(new LiqueurIngredient { Type = liqueur, Amount = amount });

        Debug.Log($"[DrinkUtility] Added {amount}x {liqueur}");
    }

    /// <summary>Adds mixer if the total-parts cap allows it.</summary>
    public static void TryToAddMixer(S_Drink d, Mixer mixer, int amount)
    {
        if (!IsValidRatio(d)) return;

        int idx = d.MixerList.FindIndex(m => m.Type == mixer);
        if (idx >= 0)
            d.MixerList[idx] = new MixerIngredient { Type = mixer, Amount = d.MixerList[idx].Amount + amount };
        else
            d.MixerList.Add(new MixerIngredient { Type = mixer, Amount = amount });

        Debug.Log($"[DrinkUtility] Added {amount}x {mixer}");
    }

    // ══════════════════════════════════════════════════════
    // Comparison
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Full drink equality check.
    /// <paramref name="technicalOnly"/> = true skips price and glass type.
    /// </summary>
    public static bool Check(S_Drink a, S_Drink b, bool technicalOnly = true)
    {
        if (b == null) return false;

        bool technical = a.AddIce == b.AddIce &&
                         a.PreparationMethod == b.PreparationMethod &&
                         IngredientsMatch(a, b);

        if (technicalOnly) return technical;

        return technical &&
               Mathf.Approximately(a.Price, b.Price)
               ;
    }

    /// <summary>
    /// Returns how satisfied a customer would be if served <paramref name="served"/>
    /// when they ordered <paramref name="recipe"/>.
    /// </summary>
    public static Satisfaction CalculateSatisfaction(S_Drink served, S_Drink recipe)
    {
        int  errors      = CountIngredientErrors(served, recipe);
        bool methodMatch = served.AddIce == recipe.AddIce &&
                           served.PreparationMethod == recipe.PreparationMethod;

        if (methodMatch && errors == 0) return Satisfaction.Perfect;
        if (errors <= 2)                return Satisfaction.Acceptable;
        return Satisfaction.Fail;
    }

    // ── Ingredient matching ────────────────────────────────

    /// <summary>True when both drinks have identical ingredient types and amounts.</summary>
    public static bool IngredientsMatch(S_Drink a, S_Drink b)
        => AlcoholListEquals(a.AlcoholList, b.AlcoholList) &&
           LiqueurListEquals(a.LiqueurList, b.LiqueurList) &&
           MixerListEquals  (a.MixerList,   b.MixerList);

    // ── Sprite Resolution ──────────────────────────────────

    /// <summary>
    /// Returns the sprite of the best-matching recipe.
    /// Perfect or Acceptable (≤ 2 errors) → sprite, otherwise null.
    /// </summary>
    public static Sprite GetCocktailSprite(S_Drink d, IReadOnlyList<S_Drink> recipes)
    {
        if (recipes == null || recipes.Count == 0) return null;

        S_Drink bestRecipe = null;
        int     bestErrors = int.MaxValue;

        foreach (var r in recipes)
        {
            int errors = CountIngredientErrors(d, r);
            if (errors < bestErrors)
            {
                bestErrors = errors;
                bestRecipe = r;
                if (bestErrors == 0) break;
            }
        }

        if (bestRecipe == null)    return null;
        if (bestErrors == 0)       return bestRecipe.CocktailSprite; // Perfect
        if (bestErrors <= 2)       return bestRecipe.CocktailSprite; // Acceptable
        return null;                                                  // Fail
    }

    public static Color[] GetCocktailColors(S_Drink d)
    {
        return new Color[] { d.waterColorTop, d.waterColorBottom };
    }

    public static Color GetCocktailTopColor(S_Drink d) { 
        return d.waterColorTop;
    }
    public static Color GetCocktailBottomColor(S_Drink d)
    {
        return d.waterColorBottom;
    }

    // ── Debug ──────────────────────────────────────────────

    public static string GetCocktailInfo(S_Drink d)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(d.Name);
        sb.AppendLine($"Type: {d.AlcoholStrength} | Method: {d.PreparationMethod} | Ice: {d.AddIce} | Price: {d.Price}");
        sb.Append("Alcohol: "); sb.AppendLine(string.Join(", ", d.AlcoholList.Select(a => $"{a.Type}x{a.Amount}")));
        sb.Append("Liqueur: "); sb.AppendLine(string.Join(", ", d.LiqueurList.Select(l => $"{l.Type}x{l.Amount}")));
        sb.Append("Mixer:   "); sb.AppendLine(string.Join(", ", d.MixerList  .Select(m => $"{m.Type}x{m.Amount}")));
        sb.Append("Color top:"); sb.AppendLine(string.Join(", ", d.waterColorTop));
        sb.Append("Color btm:"); sb.AppendLine(string.Join(", ", d.waterColorBottom));
        return sb.ToString();
    }
    public static string GetCocktailIngredient(S_Drink d)
    {
        var sb = new System.Text.StringBuilder();

        if (d.AlcoholList.Count > 0)
            sb.AppendLine(string.Join("\n", d.AlcoholList.Select(a => $"{a.Type} x {a.Amount}")));
        if (d.LiqueurList.Count > 0)
            sb.AppendLine(string.Join("\n", d.LiqueurList.Select(l => $"{l.Type} x {l.Amount}")));
        if (d.MixerList.Count > 0)
            sb.AppendLine(string.Join("\n", d.MixerList.Select(m => $"{m.Type} x {m.Amount}")));

        return sb.ToString();
    }


    // ══════════════════════════════════════════════════════
    // Private Helpers
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Derives strength purely from ingredient counts (ignores AlcoholStrength field).
    /// Used internally so GetTypeOfAlcohol can honour the manual override
    /// without infinite recursion.
    /// </summary>
    private static TypeOfCocktail ComputeTypeOfAlcohol(S_Drink d)
    {
        int total = d.AlcoholList.Sum(a => a.Amount) + d.LiqueurList.Sum(l => l.Amount);
        if (total >= 5) return TypeOfCocktail.HighAlcohol;
        if (total >  0) return TypeOfCocktail.LowAlcohol;
        return TypeOfCocktail.NoneAlcohol;
    }

    private static int CountIngredientErrors(S_Drink served, S_Drink recipe)
        => CountAlcoholErrors(served.AlcoholList, recipe.AlcoholList)
         + CountLiqueurErrors(served.LiqueurList, recipe.LiqueurList)
         + CountMixerErrors  (served.MixerList,   recipe.MixerList);

    private static bool AlcoholListEquals(List<AlcoholIngredient> a, List<AlcoholIngredient> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var item in a)
        {
            int i = b.FindIndex(x => x.Type == item.Type);
            if (i < 0 || b[i].Amount != item.Amount) return false;
        }
        return true;
    }

    private static bool LiqueurListEquals(List<LiqueurIngredient> a, List<LiqueurIngredient> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var item in a)
        {
            int i = b.FindIndex(x => x.Type == item.Type);
            if (i < 0 || b[i].Amount != item.Amount) return false;
        }
        return true;
    }

    private static bool MixerListEquals(List<MixerIngredient> a, List<MixerIngredient> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var item in a)
        {
            int i = b.FindIndex(x => x.Type == item.Type);
            if (i < 0 || b[i].Amount != item.Amount) return false;
        }
        return true;
    }

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

    /// <summary>
    /// Scans <paramref name="recipes"/> and returns the recipe with the fewest
    /// ingredient errors versus <paramref name="d"/>, together with that error count.
    /// Ice and preparation method are intentionally ignored — only ingredients matter.
    /// A result with errors > 2 is a Fail (mirrors <see cref="CalculateSatisfaction"/>).
    /// </summary>
    private static (S_Drink recipe, int errors) FindBestIngredientMatch(
        S_Drink d, IReadOnlyList<S_Drink> recipes)
    {
        if (recipes == null || recipes.Count == 0)
            return (null, int.MaxValue);

        S_Drink bestRecipe = null;
        int bestErrors = int.MaxValue;

        foreach (var r in recipes)
        {
            int errors = CountIngredientErrors(d, r);
            if (errors >= bestErrors) continue;

            bestErrors = errors;
            bestRecipe = r;
            if (bestErrors == 0) break; // Perfect — no need to keep searching
        }

        return (bestRecipe, bestErrors);
    }

}
