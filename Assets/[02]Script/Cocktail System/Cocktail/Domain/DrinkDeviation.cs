// ============================================================
//  DrinkDeviation.cs — GDD §17.1 deviation formula + best match.
//
//  Fix S1: old UtilityDrink counted HOW MANY ingredient types
//  differed; GDD §17.1 wants SUM OF DIFFERENCES. With 3-4 ingredients
//  per recipe the old value rarely exceeded 3, so Fail almost never
//  fired — Gin 1/Vodka 9 vs a Gin 7/Vodka 3 recipe scored 2
//  ("Acceptable") vs GDD's 12 ("Fail").
// ============================================================

using System.Collections.Generic;
using static E_Cocktail;

public static class DrinkDeviation
{
    /// <summary>
    /// GDD §17.3 — largest deviation still counted as Seem_Like.
    /// Confirmed by design 2026-08-21 (plan decision D7): 3 stands, tested against the
    /// full 26-recipe list post S1 fix. A decision, not a placeholder — change HERE ONLY,
    /// never hardcode elsewhere.
    /// </summary>
    public const int MaxTolerance = 3;

    /// <summary>
    /// GDD §17.1 — Σ |recipe[i] − poured[i]| over the union of ingredient types present
    /// on either side. No division. เพิ่มหมวดใหม่: แก้ที่นี่
    /// </summary>
    public static int Compute(S_Drink poured, S_Drink recipe)
    {
        if (poured == null || recipe == null) return int.MaxValue;

        return IngredientMath.Deviation<AlcoholIngredient, BaseSpirit>(poured.AlcoholList, recipe.AlcoholList)
             + IngredientMath.Deviation<LiqueurIngredient, Liqueur>(poured.LiqueurList, recipe.LiqueurList)
             + IngredientMath.Deviation<MixerIngredient, Mixer>(poured.MixerList, recipe.MixerList);
    }

    /// <summary>True when both drinks hold identical ingredients in identical amounts.</summary>
    public static bool IngredientsMatch(S_Drink a, S_Drink b)
    {
        if (a == null || b == null) return false;

        return IngredientMath.ListEquals<AlcoholIngredient, BaseSpirit>(a.AlcoholList, b.AlcoholList)
            && IngredientMath.ListEquals<LiqueurIngredient, Liqueur>(a.LiqueurList, b.LiqueurList)
            && IngredientMath.ListEquals<MixerIngredient, Mixer>(a.MixerList, b.MixerList);
    }

    /// <summary>
    /// Compares the poured drink against ONE specific recipe — the customer's order —
    /// rather than searching the database.
    ///
    /// NOTE(design): GDD is ambiguous which deviation §18 scores. §17's best-match across
    /// the whole database gives the drink its identity (name, colour, price); §18's ladder
    /// only says "deviation". Read literally, a flawless drink the customer didn't order
    /// would score Perfect (best-match deviation 0). This project scores §18 against the
    /// ORDERED recipe instead (as old code did), using best-match only for identity.
    /// Confirm with design.
    /// </summary>
    public static RecipeMatch MatchAgainst(S_Drink poured, S_Drink recipe)
    {
        if (poured == null || recipe == null) return RecipeMatch.None;

        int deviation = Compute(poured, recipe);
        bool methodMatch = poured.PreparationMethod == recipe.PreparationMethod;
        bool iceMatch = poured.AddIce == recipe.AddIce;
        var flag = DrinkFlagResolver.Resolve(deviation, methodMatch, iceMatch, hasRecipe: true);

        return new RecipeMatch(recipe, deviation, methodMatch, iceMatch, flag);
    }

    /// <summary>
    /// Scans every recipe once, returns the closest match plus the flags GDD §17.3 needs.
    /// The ONLY recipe scan in the system — name, price, colour, glass, strength, sprite
    /// now all consume the returned <see cref="RecipeMatch"/> instead of scanning separately.
    ///
    /// GDD §17.2 tie-break: on equal deviation the FIRST recipe in list order wins —
    /// intentional; the strict "&lt;" below implements it. (§24 keeps tie-break UI out of v1.)
    /// </summary>
    public static RecipeMatch FindBestMatch(S_Drink poured, IReadOnlyList<S_Drink> recipes)
    {
        if (poured == null || recipes == null || recipes.Count == 0) return RecipeMatch.None;

        S_Drink best = null;
        int bestDeviation = int.MaxValue;

        for (int i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];
            if (recipe == null) continue;

            int deviation = Compute(poured, recipe);
            if (deviation >= bestDeviation) continue;   // strict: first recipe wins a tie (§17.2)

            bestDeviation = deviation;
            best = recipe;
            if (bestDeviation == 0) break;              // cannot do better than exact
        }

        if (best == null) return RecipeMatch.None;

        bool methodMatch = poured.PreparationMethod == best.PreparationMethod;
        bool iceMatch = poured.AddIce == best.AddIce;
        var flag = DrinkFlagResolver.Resolve(bestDeviation, methodMatch, iceMatch, hasRecipe: true);

        return new RecipeMatch(best, bestDeviation, methodMatch, iceMatch, flag);
    }
}
