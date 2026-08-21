// ============================================================
//  DrinkDeviation.cs — GDD §17.1 deviation formula + best match.
//
//  Plan fix S1: the old UtilityDrink counted HOW MANY ingredient
//  types differed. GDD §17.1 wants the SUM OF THE DIFFERENCES.
//  With 3-4 ingredients per recipe the old value could barely
//  exceed 3, so the Fail threshold almost never fired — a drink
//  of Gin 1 / Vodka 9 against a Gin 7 / Vodka 3 recipe scored 2
//  ("Acceptable") where the GDD scores it 12 ("Fail").
// ============================================================

using System.Collections.Generic;
using static E_Cocktail;

public static class DrinkDeviation
{
    /// <summary>
    /// GDD §17.3 — the largest deviation still counted as Seem_Like.
    ///
    /// Confirmed by design on 2026-08-21 (plan decision D7): 3 stands, tested against the
    /// full 26-recipe list after the S1 fix. This is a decision, not a placeholder.
    /// Change it HERE ONLY — never hardcode the number anywhere else.
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
    /// Compares the poured drink against ONE specific recipe — the one the customer asked
    /// for — rather than searching the database.
    ///
    /// NOTE(design): GDD is ambiguous about which deviation §18 scores. §17 computes a
    /// best-match across the whole database to give the drink its identity (name, colour,
    /// price). §18's ladder only mentions "deviation". Read literally, a flawless drink
    /// the customer did not order would score Perfect, because its best-match deviation
    /// is 0. This project therefore scores §18 against the ORDERED recipe (what the old
    /// code did too) and uses best-match only for identity. Confirm with design.
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
    /// Scans every recipe once and returns the closest one, together with the flags
    /// GDD §17.3 needs. This is the ONLY recipe scan in the system — everything that
    /// used to scan on its own (name, price, colour, glass, strength, sprite) now
    /// consumes the returned <see cref="RecipeMatch"/>.
    ///
    /// GDD §17.2 tie-break: on equal deviation the FIRST recipe in list order wins.
    /// That is intentional, not incidental — the strict "&lt;" below is what implements it.
    /// (§24 keeps the player-facing tie-break UI out of v1.)
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
