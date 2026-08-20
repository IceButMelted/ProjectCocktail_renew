// ============================================================
//  YarnVariableSync.cs — The only place C# state is written into
//  Yarn's VariableStorage, and the only place the names live.
//
//  Plain class, not a MonoBehaviour: it takes a DialogueRunner and
//  does nothing else, so the rules it writes can be exercised
//  without a scene.
// ============================================================

using UnityEngine;
using Yarn.Unity;
using static E_Cocktail;

public class YarnVariableSync
{
    public const string TaskDoneVariableName = "$task_done";
    public const string TypeOfCocktailVariableName = "$type_of_cocktail";
    public const string SatisfactionVariableName = "$satisfaction";

    private readonly DialogueRunner _runner;

    public YarnVariableSync(DialogueRunner runner) => _runner = runner;

    public bool IsUsable => _runner != null && _runner.VariableStorage != null;

    // ── Write ──────────────────────────────────────────────

    /// <summary>Pushes a finished order into Yarn. Call once, when the drink has been scored.</summary>
    public void WriteResult(DrinkOrderContext order)
    {
        if (!Guard()) return;

        _runner.VariableStorage.SetValue(SatisfactionVariableName, (int)order.Result);
        _runner.VariableStorage.SetValue(TypeOfCocktailVariableName, (int)order.ServedType);
        _runner.VariableStorage.SetValue(TaskDoneVariableName, true);
    }

    /// <summary>Debug/override path — writes a satisfaction without a scored order behind it.</summary>
    public void WriteSatisfaction(Satisfaction satisfaction, TypeOfCocktail servedType)
    {
        if (!Guard()) return;

        _runner.VariableStorage.SetValue(SatisfactionVariableName, (int)satisfaction);
        _runner.VariableStorage.SetValue(TypeOfCocktailVariableName, (int)servedType);
        _runner.VariableStorage.SetValue(TaskDoneVariableName, true);
    }

    public void SetTaskDone(bool done)
    {
        if (!Guard()) return;
        _runner.VariableStorage.SetValue(TaskDoneVariableName, done);
    }

    /// <summary>Back to the start of a customer loop.</summary>
    public void Reset()
    {
        if (!Guard()) return;

        _runner.VariableStorage.SetValue(SatisfactionVariableName, 0f);
        _runner.VariableStorage.SetValue(TypeOfCocktailVariableName, 0f);
        _runner.VariableStorage.SetValue(TaskDoneVariableName, false);
    }

    // ── Read ───────────────────────────────────────────────

    public bool ReadTaskDone()
    {
        if (!Guard()) return false;
        _runner.VariableStorage.TryGetValue(TaskDoneVariableName, out bool value);
        return value;
    }

    public float ReadRaw(string variableName)
    {
        if (!Guard()) return 0f;
        _runner.VariableStorage.TryGetValue(variableName, out float value);
        return value;
    }

    /// <summary>
    /// GDD §22 / plan decision D8 — relationship lives in Yarn and nowhere else.
    /// Never mirror this into a ScriptableObject; an SO written at runtime keeps its value
    /// between Play sessions in the Editor.
    /// </summary>
    public float ReadRelationship(NPC_Name customer)
    {
        if (!Guard()) return 0f;
        _runner.VariableStorage.TryGetValue($"$rel_{customer}", out float value);
        return value;
    }

    /// <summary>GDD §18.2 — applies the post-serve relationship change.</summary>
    public void ApplyRelationshipDelta(NPC_Name customer, float delta)
    {
        if (!Guard() || Mathf.Approximately(delta, 0f)) return;

        string key = $"$rel_{customer}";
        _runner.VariableStorage.TryGetValue(key, out float current);
        _runner.VariableStorage.SetValue(key, Mathf.Clamp(current + delta, 0f, 10f));
    }

    private bool Guard()
    {
        if (IsUsable) return true;

        Debug.LogWarning("[YarnVariableSync] DialogueRunner or VariableStorage is null — variable ignored.");
        return false;
    }
}
