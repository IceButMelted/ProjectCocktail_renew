// ============================================================
//  E_Cocktail.Drink.cs — part of E_Cocktail.
//  Ingredients, glassware, preparation method and scoring.
//
//  Plan §4.6: the enums are grouped by domain across four files, but
//  E_Cocktail stays ONE partial class. Twenty files do
//  `using static E_Cocktail;`; splitting it into separate classes
//  would break every one of them for no gain.
//
//  NOTE — deliberate deviation from GDD §15.1 (plan decision D6):
//  the GDD models ingredients as one flat IngredientType enum plus a
//  category lookup. Three typed enums are kept instead so a Mixer
//  button cannot be assigned a spirit and each Inspector dropdown
//  stays short. See DrinkIngredients.cs and Domain/IngredientMath.cs.
// ============================================================

public partial class E_Cocktail
{
    /// <summary>Base spirits — the alcoholic backbone of a drink.</summary>
    public enum BaseSpirit
    {
        None = 0,
        Vodka,
        Gin,
        Whiskey,
        Rum,
        Tequila
    }

    /// <summary>Liqueurs — also alcoholic, counted with base spirits by GDD §15.2.</summary>
    public enum Liqueur
    {
        None = 0,
        Triplesec,
        DryVermouth,
        SweetVermouth,
        Campari
    }

    /// <summary>Non-alcoholic mixers.</summary>
    public enum Mixer
    {
        None = 0,
        Soda,
        CranberryJuice,     // was "CanberryJuice" — spelling only; the underlying value is
                            // unchanged, so every serialized asset and prefab keeps working.
        LimeJuice,
        LemonJuice,
        GrapefruitJuice,
        Syrup,
        PepperMint,
        OrangeJuice
    }

    /// <summary>
    /// Glassware. Cosmetic only — GDD §21 states the matching algorithm never reads it.
    /// </summary>
    public enum GlassType : byte
    {
        None,
        Hi_ball,
        Rocks,
        Magrita,
        Martini,
        Cocktail,
        LongDrink,
        NotFix
    }

    /// <summary>
    /// How a drink is mixed. GDD §16 also lists Build; it is not here yet because no
    /// Building minigame exists (plan decision D4 / Bar410_Minigame_Integration_Plan §1).
    /// </summary>
    public enum Method : byte
    {
        None = 0,
        Shaking,
        Stirring,
    }

    /// <summary>GDD §15.2 — alcohol strength band. Thresholds live in AlcoholClassifier.</summary>
    public enum TypeOfCocktail
    {
        None,
        HighAlcohol,
        LowAlcohol,
        NoneAlcohol,
        NotMatch
    }

    /// <summary>
    /// GDD §18 — how happy the customer is.
    ///
    /// The numeric values are written into Yarn's $satisfaction, so .yarn files compare
    /// against them. Do not reorder or insert members. Fail (a) and Fail (b) are told
    /// apart by RecipeMatch.IsFailB, not by a new member here.
    /// </summary>
    public enum Satisfaction
    {
        None,
        Fail,
        Acceptable,
        Perfect
    }
}
