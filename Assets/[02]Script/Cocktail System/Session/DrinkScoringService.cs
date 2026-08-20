// ============================================================
//  DrinkScoringService.cs — Runs GDD §17-18 once, at serve time,
//  and writes the whole outcome into the order context.
//
//  Sits between the Domain rules (which are pure) and the flow layer
//  (which owns when scoring happens). CocktailFlowBridge calls this
//  from ServeState.Exited — the TODO left in ServeState.cs:29 and
//  Bar410_StateMachine_Implementation.md §3.2.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

public class DrinkScoringService
{
    private readonly IDrinkRepository _lookup;

    public DrinkScoringService(IDrinkRepository lookup) => _lookup = lookup;

    /// <summary>
    /// Scores <paramref name="served"/> against the order held in <paramref name="context"/>
    /// and stores the result there. Returns the satisfaction for convenience.
    ///
    /// Two comparisons happen, and they are not the same one:
    ///   identity  — best match across the whole database: what did the player MAKE?
    ///   order     — comparison against the requested recipe: does it match the ORDER?
    /// See the note on DrinkDeviation.MatchAgainst for why §18 scores the second.
    /// </summary>
    public Satisfaction Score(DrinkOrderContext context, S_Drink served)
    {
        if (context == null || served == null)
        {
            Debug.LogWarning("[DrinkScoringService] Score called with no context or no drink.");
            return Satisfaction.None;
        }

        if (!context.HasOrder)
        {
            Debug.LogWarning("[DrinkScoringService] Score called before a customer ordered anything.");
            return Satisfaction.None;
        }

        IReadOnlyList<S_Drink> recipes = _lookup != null ? _lookup.GetDrinks() : null;
        RecipeMatch identity = DrinkDeviation.FindBestMatch(served, recipes);

        TypeOfCocktail servedType = identity.IsRecognised
            ? AlcoholClassifier.Resolve(identity.Recipe)
            : AlcoholClassifier.Compute(served);

        // Mode 5 (GDD §12) has no target recipe, so the identity match IS the comparison —
        // any recipe of the right type satisfies the order.
        RecipeMatch orderMatch = context.Target != null
            ? DrinkDeviation.MatchAgainst(served, context.Target)
            : identity;

        Satisfaction result = SatisfactionEvaluator.Evaluate(orderMatch, servedType, context.OrderedType);
        float payout = PricingRules.Payout(orderMatch, result);
        float relationship = PricingRules.RelationshipDelta(result);

        context.Score(orderMatch, result, servedType, payout, relationship);
        return result;
    }
}
