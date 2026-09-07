// ============================================================
//  PricingRules.cs — GDD §18.1 payout per satisfaction outcome.
//  Plan fix S5: old code had no multipliers — paid the matched
//  recipe's price flat, or a 5.0 fallback where GDD specifies 50.
// ============================================================

using static E_Cocktail;

public static class PricingRules
{
    /// <summary>GDD §18.1 — flat payout when nothing matched at all (Fail "b").</summary>
    public const float UnmatchedPayout = 50f;

    public const float PerfectMultiplier = 1.5f;
    public const float AcceptableMultiplier = 1.0f;
    public const float FailNearMultiplier = 0.5f;

    /// <summary>
    /// GDD §18.1:
    ///   Perfect     -> recipe.price * 1.5
    ///   Acceptable  -> recipe.price * 1.0
    ///   Fail (a)    -> recipe.price * 0.5     (a nearby recipe was matched)
    ///   Fail (b)    -> 50, fixed              (nothing matched; price is type-independent)
    /// </summary>
    public static float Payout(in RecipeMatch match, Satisfaction result)
    {
        if (match.IsFailB || !match.HasRecipe) return UnmatchedPayout;

        float basePrice = match.Recipe.Price;

        switch (result)
        {
            case Satisfaction.Perfect: return basePrice * PerfectMultiplier;
            case Satisfaction.Acceptable: return basePrice * AcceptableMultiplier;
            case Satisfaction.Fail: return basePrice * FailNearMultiplier;   // Fail (a)
            default: return basePrice * AcceptableMultiplier;
        }
    }

    /// <summary>
    /// GDD §18.2 — relationship change applied after the reaction plays. Written into
    /// Yarn's $rel_&lt;id&gt;, the single source of truth (D8); never mirrored to an SO.
    /// </summary>
    public static float RelationshipDelta(Satisfaction result)
    {
        switch (result)
        {
            case Satisfaction.Perfect: return 0.5f;
            case Satisfaction.Acceptable: return 0.25f;
            default: return 0f;      // both flavours of Fail leave the relationship alone
        }
    }
}
