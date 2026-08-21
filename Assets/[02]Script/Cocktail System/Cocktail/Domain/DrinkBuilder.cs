// ============================================================
//  DrinkBuilder.cs — The only place a runtime S_Drink is mutated.
//
//  Never call these on a recipe asset: they write to the instance
//  they are given, and a recipe asset would be modified on disk.
//  CocktailShakerData creates its drink with CreateInstance, so
//  the live drink is always a throw-away in-memory object.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

public static class DrinkBuilder
{
    /// <summary>
    /// Shown when nothing matched (GDD §17.3 "Fail b").
    ///
    /// TODO(design, plan S6): GDD §17.3 asks for a RANDOM name here, drawn from a pool
    /// the designers author. No such pool exists yet, so this neutral placeholder stands
    /// in. It replaces the old "NOT MATCH ANY", which was debug text leaking to players.
    /// </summary>
    public const string UnmatchedName = "???";

    // ── Ingredient Addition ────────────────────────────────
    // Each overload guards the 10-part cap BEFORE adding, counting the amount being
    // added (plan bug B6 — the old check only looked at the current total).

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
    /// This replaces five separate Update* methods that each ran their own recipe scan
    /// (plan §2.4) and, worse, depended on each other's ordering — the old
    /// UpdateTypeOfAlcohol looked the drink up by the Name that UpdateName had just
    /// written. One match in, one consistent identity out.
    /// </summary>
    public static void ApplyRecipeIdentity(S_Drink runtime, in RecipeMatch match)
    {
        if (runtime == null) return;

        bool recognised = match.IsRecognised;

        // GDD §17.3 — name comes from the match, or the unmatched placeholder.
        runtime.Name = recognised ? match.Recipe.Name : UnmatchedName;

        // Menu price of the matched recipe. What the customer actually PAYS is decided at
        // serve time by PricingRules (GDD §18.1), because it depends on satisfaction.
        runtime.Price = recognised ? match.Recipe.Price : PricingRules.UnmatchedPayout;

        // GDD §17.3 — an unmatched drink is classified from what is actually in the glass
        // (plan fix S7). A matched drink inherits the recipe's authored strength.
        runtime.AlcoholStrength = recognised
            ? AlcoholClassifier.Resolve(match.Recipe)
            : AlcoholClassifier.Compute(runtime);

        ApplyGlass(runtime, match, recognised);

        DrinkColorBlender.Resolve(match, runtime, out var top, out var bottom);
        runtime.waterColorTop = top;
        runtime.waterColorBottom = bottom;
    }

    /// <summary>Glass used when nobody has decided otherwise.</summary>
    public const GlassType DefaultGlass = GlassType.Hi_ball;

    /// <summary>Glass used for a drink that matched no recipe, so it reads as "not a real drink".</summary>
    public const GlassType UnmatchedGlass = GlassType.Rocks;

    /// <summary>
    /// GDD §21 — glassware is cosmetic, and a recipe may hand the choice to the player.
    ///
    /// A recipe whose CompatibleGlass is <see cref="GlassType.NotFix"/> means exactly that:
    /// the player's own selection stands and is NOT overwritten here. Every other recipe
    /// pins its glass. Until the player has chosen anything, DefaultGlass fills in.
    ///
    /// TODO(design, plan S13): the selection UI itself does not exist yet. When it lands,
    /// it only needs to call ShakerContents.SetGlass — this method already protects the
    /// value from being stamped over on the next ingredient.
    /// </summary>
    private static void ApplyGlass(S_Drink runtime, in RecipeMatch match, bool recognised)
    {
        if (!recognised)
        {
            runtime.CompatibleGlass = UnmatchedGlass;
            return;
        }

        if (match.Recipe.CompatibleGlass == GlassType.NotFix)
        {
            // Player's choice wins; only fill in a default if they have not picked yet.
            if (runtime.CompatibleGlass == GlassType.None) runtime.CompatibleGlass = DefaultGlass;
            return;
        }

        runtime.CompatibleGlass = match.Recipe.CompatibleGlass;
    }

    // ── Reset ──────────────────────────────────────────────

    /// <summary>Returns a runtime drink to its empty state. เพิ่มหมวดใหม่: แก้ที่นี่</summary>
    public static void Clear(S_Drink d)
    {
        if (d == null) return;

        d.Name = string.Empty;
        d.AlcoholStrength = TypeOfCocktail.None;
        d.PreparationMethod = Method.None;
        d.AddIce = false;
        d.Price = 0f;
        d.CompatibleGlass = GlassType.None;
        d.waterColorTop = Color.clear;
        d.waterColorBottom = Color.clear;

        d.AlcoholList = new List<AlcoholIngredient>();
        d.LiqueurList = new List<LiqueurIngredient>();
        d.MixerList = new List<MixerIngredient>();
    }
}
