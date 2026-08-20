// ============================================================
//  RecipeMatch.cs — Result of comparing a poured drink to the
//  recipe database. GDD §17.
//
//  Computed ONCE per shaker update and passed around, replacing
//  the five independent recipe scans the old UtilityDrink ran
//  (plan §2.4).
// ============================================================

using static E_Cocktail;

/// <summary>GDD §17.3 — how well the poured drink resembles a known recipe.</summary>
public enum DrinkFlag
{
    /// <summary>deviation == 0, and both method and ice match.</summary>
    Perfect,

    /// <summary>Recognisable as the matched recipe, but not made correctly.</summary>
    Seem_Like,

    /// <summary>No recipe within tolerance.</summary>
    Fail
}

/// <summary>Immutable snapshot of one comparison. Never null; use <see cref="HasRecipe"/>.</summary>
public readonly struct RecipeMatch
{
    public readonly S_Drink Recipe;
    public readonly int Deviation;
    public readonly bool MethodMatch;
    public readonly bool IceMatch;
    public readonly DrinkFlag Flag;

    public RecipeMatch(S_Drink recipe, int deviation, bool methodMatch, bool iceMatch, DrinkFlag flag)
    {
        Recipe = recipe;
        Deviation = deviation;
        MethodMatch = methodMatch;
        IceMatch = iceMatch;
        Flag = flag;
    }

    /// <summary>No recipe was within tolerance at all.</summary>
    public static RecipeMatch None => new RecipeMatch(null, int.MaxValue, false, false, DrinkFlag.Fail);

    public bool HasRecipe => Recipe != null;

    /// <summary>Perfect or Seem_Like — the drink carries the matched recipe's identity.</summary>
    public bool IsRecognised => HasRecipe && Flag != DrinkFlag.Fail;

    /// <summary>
    /// GDD §18 case 5 — "Fail (b)", nothing matched at all.
    /// Distinct from "Fail (a)" (case 4), where a recipe matched but the type was wrong;
    /// the two price differently (GDD §18.1), so the distinction has to survive.
    /// </summary>
    public bool IsFailB => !HasRecipe || Deviation > DrinkDeviation.MaxTolerance;

    public string RecipeName => HasRecipe ? Recipe.Name : string.Empty;
}
