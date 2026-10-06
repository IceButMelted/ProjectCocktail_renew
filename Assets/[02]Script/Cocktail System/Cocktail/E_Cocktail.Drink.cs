// ============================================================
//  E_Cocktail.Drink.cs — part of E_Cocktail.
//  Ingredients, glassware, preparation method and scoring.
//
//  Plan §4.6: enums are grouped by domain across 4 files, but
//  E_Cocktail stays ONE partial class. 20 files do
//  `using static E_Cocktail;`; splitting into separate classes
//  would break all of them for no gain.
//
//  NOTE — deliberate deviation from GDD §15.1 (plan decision D6): GDD
//  models ingredients as one flat IngredientType enum + category lookup.
//  Three typed enums kept instead so a Mixer button can't be assigned a
//  spirit, and each Inspector dropdown stays short. See
//  DrinkIngredients.cs and Domain/IngredientMath.cs.
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
        CranberryJuice,     // was "CanberryJuice" — spelling fix only, value unchanged,
                            // serialized assets/prefabs keep working.
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

        /// <summary>
        /// Legacy "player picks" marker from when recipes carried a CompatibleGlass field.
        /// S_Drink no longer has that field — glass choice is entirely the player's via a
        /// placed serving glass (Cocktail/Glass/). Kept only because this enum is reused as
        /// SO_GlassOption.Shape.
        /// </summary>
        NotFix
    }

    /// <summary>
    /// How a drink is mixed. GDD §16 also lists Build; not here yet — no Building minigame
    /// exists (plan decision D4 / Bar410_Minigame_Integration_Plan §1).
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
    /// Numeric values are written into Yarn's $satisfaction; .yarn files compare against
    /// them — do not reorder or insert members. Fail (a)/(b) told apart by
    /// RecipeMatch.IsFailB, not a new member here.
    /// </summary>
    public enum Satisfaction
    {
        None,
        Fail,
        Acceptable,
        Perfect
    }
}
