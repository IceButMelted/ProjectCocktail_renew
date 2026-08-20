// ============================================================
//  AlcoholClassifier.cs — GDD §15.2, alcohol units -> DrinkType.
//
//  Plan decision S3: the GDD threshold wins over the old code.
//  The previous UtilityDrink used ">= 5 -> High", which classified
//  a 5-part drink as High; GDD §15.2 states 5 is inclusive of Low.
// ============================================================

using static E_Cocktail;

public static class AlcoholClassifier
{
    /// <summary>
    /// GDD §15.2:
    ///   0        -> NoneAlcohol
    ///   1 .. 5   -> LowAlcohol   (5 is inclusive)
    ///   >= 6     -> HighAlcohol
    /// </summary>
    public static TypeOfCocktail FromUnits(int alcoholUnits)
    {
        if (alcoholUnits <= 0) return TypeOfCocktail.NoneAlcohol;
        if (alcoholUnits <= 5) return TypeOfCocktail.LowAlcohol;
        return TypeOfCocktail.HighAlcohol;
    }

    /// <summary>Derives strength purely from what is in the glass, ignoring any authored value.</summary>
    public static TypeOfCocktail Compute(S_Drink d) => FromUnits(DrinkQuery.GetAlcoholUnits(d));

    /// <summary>
    /// Authored strength when a recipe asset sets one, otherwise the computed value.
    /// Recipe assets set it in the Inspector; the shaker's runtime drink does not.
    /// </summary>
    public static TypeOfCocktail Resolve(S_Drink d)
    {
        if (d == null) return TypeOfCocktail.None;
        return d.AlcoholStrength != TypeOfCocktail.None ? d.AlcoholStrength : Compute(d);
    }
}
