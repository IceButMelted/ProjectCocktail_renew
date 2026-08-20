// ============================================================
//  CocktailSystemManager.YarnDebug.cs — ContextMenu helpers and the
//  variable snapshot table. Editor only; none of it ships.
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
    /// Publishes a satisfaction without a real drink behind it, so dialogue branches can be
    /// exercised without mixing anything. Writes Yarn directly and marks the order scored so
    /// &lt;&lt;wait_for_task&gt;&gt; releases.
    /// </summary>
    private void DebugForceSatisfaction(Satisfaction satisfaction)
    {
        var servedType = Order.OrderedType != TypeOfCocktail.None ? Order.OrderedType : TypeOfCocktail.LowAlcohol;

        Order.Score(RecipeMatch.None, satisfaction, servedType,
                    PricingRules.Payout(RecipeMatch.None, satisfaction),
                    PricingRules.RelationshipDelta(satisfaction));

        Variables.WriteSatisfaction(satisfaction, servedType);
        DebugVariableFromYarn();
    }

    [ContextMenu("Debug Order Context")]
    public void DebugOrderContext()
        => Debug.Log($"[Order] customer={Order.Customer} mode={Order.Mode} target='{Order.TargetName}' " +
                     $"orderedType={Order.OrderedType} scored={Order.IsScored} result={Order.Result} " +
                     $"served={Order.ServedType} payout={Order.Payout} rel={Order.RelationshipDelta}");

#endif

    /// <summary>Prints a side-by-side snapshot of every tracked Yarn variable.</summary>
    [ContextMenu("Debug Yarn Variables")]
    public void DebugVariableFromYarn()
    {
        if (!Variables.IsUsable)
        {
            Debug.LogWarning("[CocktailSystemManager] Cannot debug — DialogueRunner or VariableStorage is null.");
            return;
        }

        float yarnSatisfactionRaw = Variables.ReadRaw(YarnVariableSync.SatisfactionVariableName);
        float yarnCocktailRaw = Variables.ReadRaw(YarnVariableSync.TypeOfCocktailVariableName);

        string yarnSatisfactionName = System.Enum.IsDefined(typeof(Satisfaction), (int)yarnSatisfactionRaw)
            ? ((Satisfaction)(int)yarnSatisfactionRaw).ToString() : $"UNKNOWN({yarnSatisfactionRaw})";
        string yarnCocktailName = System.Enum.IsDefined(typeof(TypeOfCocktail), (int)yarnCocktailRaw)
            ? ((TypeOfCocktail)(int)yarnCocktailRaw).ToString() : $"UNKNOWN({yarnCocktailRaw})";

        string sep = new string('─', 72);
        string log = "\n" + sep
            + "\n  [CocktailSystemManager] Variable Debug Snapshot"
            + "\n" + sep
            + string.Format("\n  {0,-26} {1,-20} {2,-8} {3,-20} {4}", "Variable", "C# Value", "Yarn Raw", "Yarn Enum", "Match?")
            + "\n" + sep
            + FormatEnumRow(YarnVariableSync.SatisfactionVariableName, Order.Result.ToString(), yarnSatisfactionRaw.ToString("0"), yarnSatisfactionName)
            + FormatEnumRow(YarnVariableSync.TypeOfCocktailVariableName, Order.ServedType.ToString(), yarnCocktailRaw.ToString("0"), yarnCocktailName)
            + "\n" + sep;

        Debug.Log(log);
    }

    private static string FormatEnumRow(string varName, string csValue, string yarnRaw, string yarnEnum)
    {
        bool match = string.Equals(csValue, yarnEnum, System.StringComparison.Ordinal);
        string icon = match ? "OK" : "MISMATCH <--";
        return string.Format("\n  {0,-26} {1,-20} {2,-8} {3,-20} {4}", varName, csValue, yarnRaw, yarnEnum, icon);
    }

    public void YarnDebugVariables() => DebugVariableFromYarn();
}
