// ============================================================
//  SatisfactionEvaluator.cs — GDD §18 satisfaction ladder.
//
//  Plan fix S4: cases 3 and 4 are told apart by comparing SERVED
//  drink type vs. ORDERED type. Old code never stored the ordered
//  type, so "Fail (a)" couldn't occur and GDD §18.1's 0.5x price
//  was unreachable.
// ============================================================

using static E_Cocktail;

public static class SatisfactionEvaluator
{
    /// <summary>
    /// GDD §18, checked top to bottom, first match wins:
    ///
    ///   1. deviation == 0 and methodMatch and iceMatch          -> Perfect
    ///   2. deviation == 0 and (method or ice wrong)             -> Acceptable
    ///   3. 0 &lt; deviation &lt;= 3 and servedType == orderedType  -> Acceptable
    ///   4. 0 &lt; deviation &lt;= 3 and servedType != orderedType  -> Fail (a)
    ///   5. deviation &gt; 3                                      -> Fail (b)
    /// </summary>
    /// <param name="orderedType">
    /// Type the customer asked for. <see cref="TypeOfCocktail.None"/> means no type was
    /// pinned down; cases 3/4 then fall back to Acceptable since there's no expectation to violate.
    /// </param>
    public static Satisfaction Evaluate(in RecipeMatch match, TypeOfCocktail servedType, TypeOfCocktail orderedType)
    {
        if (match.IsFailB) return Satisfaction.Fail;                       // case 5

        if (match.Deviation == 0)                                          // cases 1-2
            return match.MethodMatch && match.IceMatch ? Satisfaction.Perfect : Satisfaction.Acceptable;

        if (orderedType == TypeOfCocktail.None) return Satisfaction.Acceptable;

        return servedType == orderedType ? Satisfaction.Acceptable          // case 3
                                         : Satisfaction.Fail;               // case 4 — Fail (a)
    }

    /// <summary>
    /// True when the failure is the "matched a nearby recipe" flavour (GDD §18 case 4).
    /// Needed by <see cref="PricingRules"/>: 0.5x for Fail (a), flat amount for Fail (b).
    /// </summary>
    public static bool IsFailA(in RecipeMatch match, Satisfaction result)
        => result == Satisfaction.Fail && !match.IsFailB;
}
