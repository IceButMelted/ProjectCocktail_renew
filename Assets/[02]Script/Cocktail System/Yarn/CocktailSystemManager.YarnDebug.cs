// ============================================================
//  CocktailSystemManager.YarnDebug.cs — ContextMenu helpers.
//  Editor-only; does not ship. The Yarn variable snapshot table lives
//  with the variables, in TalkToCustomerManager.
// ============================================================

using UnityEngine;
using static E_Cocktail;

public partial class CocktailSystemManager
{
#if UNITY_EDITOR

    [ContextMenu("Perfect Satisfaction")]
    public void SetSatisfactionPerfect() => DebugForceSatisfaction(Satisfaction.Perfect);

    [ContextMenu("Acceptable Satisfaction")]
    public void SetSatisfactionAcceptable() => DebugForceSatisfaction(Satisfaction.Acceptable);

    [ContextMenu("Fail Satisfaction")]
    public void SetSatisfactionFail() => DebugForceSatisfaction(Satisfaction.Fail);

    /// <summary>
    /// Publishes a satisfaction with no real drink behind it, so dialogue branches can be
    /// tested without mixing. Scores the order and raises OrderScored, so Yarn is written
    /// and &lt;&lt;wait_for_task&gt;&gt; releases.
    /// </summary>
    private void DebugForceSatisfaction(Satisfaction satisfaction)
    {
        var servedType = Order.OrderedType != TypeOfCocktail.None ? Order.OrderedType : TypeOfCocktail.LowAlcohol;

        Order.Score(RecipeMatch.None, satisfaction, servedType,
                    PricingRules.Payout(RecipeMatch.None, satisfaction),
                    PricingRules.RelationshipDelta(satisfaction));

        OrderScored?.Invoke(Order);
    }

    [ContextMenu("Debug Order Context")]
    public void DebugOrderContext()
        => Debug.Log($"[Order] customer={Order.Customer} mode={Order.Mode} target='{Order.TargetName}' " +
                     $"orderedType={Order.OrderedType} scored={Order.IsScored} result={Order.Result} " +
                     $"served={Order.ServedType} payout={Order.Payout} rel={Order.RelationshipDelta}");

#endif
}
