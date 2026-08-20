// ============================================================
//  DrinkQuery.cs — Read-only totals about a drink. GDD §15.
//  Pure functions, no state, no Unity lifecycle.
// ============================================================

public static class DrinkQuery
{
    /// <summary>GDD §15 — every recipe's ingredients sum to exactly this many parts.</summary>
    public const int MaxTotalParts = 10;

    public static int GetTotalAlcohol(S_Drink d)
        => d == null ? 0 : IngredientMath.Sum<AlcoholIngredient, E_Cocktail.BaseSpirit>(d.AlcoholList);

    public static int GetTotalLiqueur(S_Drink d)
        => d == null ? 0 : IngredientMath.Sum<LiqueurIngredient, E_Cocktail.Liqueur>(d.LiqueurList);

    public static int GetTotalMixer(S_Drink d)
        => d == null ? 0 : IngredientMath.Sum<MixerIngredient, E_Cocktail.Mixer>(d.MixerList);

    /// <summary>Parts across every category. เพิ่มหมวดใหม่: แก้ที่นี่</summary>
    public static int GetTotalIngredient(S_Drink d)
        => GetTotalAlcohol(d) + GetTotalLiqueur(d) + GetTotalMixer(d);

    /// <summary>Alcohol-bearing parts only (BaseSpirit + Liqueur). GDD §15.2.</summary>
    public static int GetAlcoholUnits(S_Drink d)
        => GetTotalAlcohol(d) + GetTotalLiqueur(d);

    /// <summary>
    /// True when <paramref name="amount"/> more parts still fit under the cap.
    ///
    /// Fixes plan bug B6: the old IsValidRatio only checked that the CURRENT total was
    /// below 10, so pouring 5 parts onto a 9-part drink produced a 14-part glass.
    /// </summary>
    public static bool CanAdd(S_Drink d, int amount)
        => GetTotalIngredient(d) + amount <= MaxTotalParts;

    /// <summary>True while the glass still has room for at least one more part.</summary>
    public static bool HasRoom(S_Drink d) => CanAdd(d, 1);

    /// <summary>GDD §15 — a finished recipe must total exactly 10 parts.</summary>
    public static bool IsCompleteRecipe(S_Drink d) => GetTotalIngredient(d) == MaxTotalParts;
}
