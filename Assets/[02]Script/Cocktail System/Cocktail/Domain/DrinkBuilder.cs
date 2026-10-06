// ============================================================
//  DrinkBuilder.cs — only place a runtime S_Drink is mutated.
//
//  Never call on a recipe asset: writes to the given instance,
//  which would modify the asset on disk. CocktailShakerData
//  uses CreateInstance, so the live drink is always throw-away.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

public static class DrinkBuilder
{
    /// <summary>
    /// Shown when nothing matched (GDD §17.3 "Fail b").
    ///
    /// TODO(design, plan S6): GDD §17.3 wants a RANDOM name from a designer-authored
    /// pool that doesn't exist yet, so this placeholder stands in. Replaces the old
    /// "NOT MATCH ANY" debug text that was leaking to players.
    /// </summary>
    public const string UnmatchedName = "???";

    // ── Ingredient Addition ────────────────────────────────
    // Each overload checks the 10-part cap BEFORE adding, including the amount being
    // added (plan bug B6 — old check only looked at the current total).

    public static bool TryAddAlcohol(S_Drink d, BaseSpirit alcohol, int amount)
    {
        if (d == null || !DrinkQuery.CanAdd(d, amount)) return false;
        IngredientMath.Add<AlcoholIngredient, BaseSpirit>(d.AlcoholList, alcohol, amount, AlcoholIngredient.Make);
        return true;
    }

    public static bool TryAddLiqueur(S_Drink d, Liqueur liqueur, int amount)
    {
        if (d == null || !DrinkQuery.CanAdd(d, amount)) return false;
        IngredientMath.Add<LiqueurIngredient, Liqueur>(d.LiqueurList, liqueur, amount, LiqueurIngredient.Make);
        return true;
    }

    public static bool TryAddMixer(S_Drink d, Mixer mixer, int amount)
    {
        if (d == null || !DrinkQuery.CanAdd(d, amount)) return false;
        IngredientMath.Add<MixerIngredient, Mixer>(d.MixerList, mixer, amount, MixerIngredient.Make);
        return true;
    }

    // ── Identity ───────────────────────────────────────────

    /// <summary>
    /// Writes name, price, strength, glass and colours onto <paramref name="runtime"/>
    /// from a single <see cref="RecipeMatch"/>.
    ///
    /// Replaces five separate Update* methods that each ran their own recipe scan
    /// (plan §2.4) and depended on each other's ordering — old UpdateTypeOfAlcohol
    /// looked the drink up by the Name that UpdateName had just written. One match
    /// in, one consistent identity out.
    /// </summary>
    public static void ApplyRecipeIdentity(S_Drink runtime, in RecipeMatch match)
    {
        if (runtime == null) return;

        bool recognised = match.IsRecognised;

        // GDD §17.3 — name from match, or unmatched placeholder.
        runtime.Name = recognised ? match.Recipe.Name : UnmatchedName;

        // Menu price of matched recipe. Actual customer PAYMENT is decided at serve
        // time by PricingRules (GDD §18.1) since it depends on satisfaction.
        runtime.Price = recognised ? match.Recipe.Price : PricingRules.UnmatchedPayout;

        // GDD §17.3 — unmatched drink is classified from what's actually in the glass
        // (plan fix S7). Matched drink inherits the recipe's authored strength.
        runtime.AlcoholStrength = recognised
            ? AlcoholClassifier.Resolve(match.Recipe)
            : AlcoholClassifier.Compute(runtime);

        DrinkColorBlender.Resolve(match, runtime, out var top, out var bottom);
        runtime.waterColorTop = top;
        runtime.waterColorBottom = bottom;
    }

    // ── Reset ──────────────────────────────────────────────

    /// <summary>Resets a runtime drink to empty state. New ingredient category? Edit here too.</summary>
    public static void Clear(S_Drink d)
    {
        if (d == null) return;

        d.Name = string.Empty;
        d.AlcoholStrength = TypeOfCocktail.None;
        d.PreparationMethod = Method.None;
        d.AddIce = false;
        d.Price = 0f;
        d.waterColorTop = Color.clear;
        d.waterColorBottom = Color.clear;

        d.AlcoholList = new List<AlcoholIngredient>();
        d.LiqueurList = new List<LiqueurIngredient>();
        d.MixerList = new List<MixerIngredient>();
    }
}
