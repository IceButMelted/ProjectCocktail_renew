using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Yarn.Unity;

public class YarnVariableDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Header("Debug Settings")]
    [Tooltip("Press to dump ALL declared Yarn variables")]
    [SerializeField] private KeyCode dumpAllKey = KeyCode.F1;

    [Tooltip("Press to log only the watched variables below")]
    [SerializeField] private KeyCode watchedKey = KeyCode.F2;

    [Tooltip("Variables to watch — include the $ prefix, e.g. $player_health")]
    [SerializeField] private List<string> watchedVariables = new();

    // ---------------------------------------
    private VariableStorageBehaviour _storage;
    private Yarn.Program _program;   // cached via reflection

    private void Start()
    {
        if (dialogueRunner == null)
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();

        if (dialogueRunner == null)
        {
            Debug.LogError("[YarnDebugger] ❌ No DialogueRunner found in scene!");
            return;
        }

        _storage = dialogueRunner.VariableStorage;
        CacheProgram();

        Debug.Log("[YarnDebugger] ✅ Ready — F1: dump all  |  F2: watched vars");
    }

    // Cache once so we don't reflect every key press
    private void CacheProgram()
    {
        _program = typeof(Yarn.Dialogue)
            .GetProperty("Program",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)
            ?.GetValue(dialogueRunner.Dialogue) as Yarn.Program;

        if (_program == null)
            Debug.LogWarning("[YarnDebugger] ⚠️ Could not cache Yarn Program via reflection.");
    }

    // ---------------------------------------
    private void Update()
    {
        if (Input.GetKeyDown(dumpAllKey)) DumpAllVariables();
        if (Input.GetKeyDown(watchedKey)) LogWatchedVariables();
    }

    // ---------------------------------------
    // PUBLIC API
    //  ---------------------------------------

    public void DumpAllVariables()
    {
        if (!CheckStorage()) return;

        if (_program == null) CacheProgram();
        if (_program == null)
        {
            Debug.LogWarning("[YarnDebugger] ⚠️ Yarn Program not available yet.");
            return;
        }

        var keys = _program.InitialValues.Keys;

        var sb = new StringBuilder();
        sb.AppendLine("\n╔══════════════════════════════════════╗");
        sb.AppendLine("║        YARN VARIABLES DEBUG          ║");
        sb.AppendLine("╚══════════════════════════════════════╝");

        if (keys.Count == 0)
        { 
            sb.AppendLine("  (no <<declare>> statements found in .yarn files)");
        }
        else
        {
            foreach (string varName in keys)
                sb.AppendLine($"  {FormatVariable(varName)}");

            sb.AppendLine($"\n  Total: {keys.Count} variable(s)");
        }

        sb.AppendLine("════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    public void LogWatchedVariables()
    {
        if (!CheckStorage()) return;

        if (watchedVariables.Count == 0)
        {
            Debug.Log("[YarnDebugger] No watched variables set. Add them in the Inspector.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("\n╔══════════════════════════════════════╗");
        sb.AppendLine("║       WATCHED YARN VARIABLES         ║");
        sb.AppendLine("╚══════════════════════════════════════╝");

        foreach (string varName in watchedVariables)
            sb.AppendLine($"  {FormatVariable(varName)}");

        sb.AppendLine("════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    public void GetVariable(string variableName)
    {
        if (!CheckStorage()) return;
        Debug.Log($"[YarnDebugger] {FormatVariable(variableName)}");
    }

    public bool TryGetFloat(string n, out float v) => _storage.TryGetValue(n, out v);
    public bool TryGetBool(string n, out bool v) => _storage.TryGetValue(n, out v);
    public bool TryGetString(string n, out string v) => _storage.TryGetValue(n, out v);

    /// ---------------------------------------
    /// CONTEXTMENU
    /// ---------------------------------------
    [ContextMenu("Change Variable In Yarn Manual")]
    public void ChangeVariableinYarnManual() { 
        dialogueRunner.VariableStorage.SetValue("$player_name", "Monster"); 
        dialogueRunner.VariableStorage.SetValue("$quest_stage", 123f);

    }
    
    [ContextMenu("Debug Variable Type")]
    public void DebugVariableType() {
        
        string varName = "$quest_stage";

        if (_program != null &&
            _program.InitialValues.TryGetValue(varName, out var operand))
        { 
        
            Debug.Log($"Variable '{varName}' declared as: {operand.ValueCase}");
            Debug.Log(operand.GetType());
        }
    }

    // ---------------------------------------
    // HELPERS
    // ---------------------------------------

    /// <summary>
    /// Uses Operand.ValueCase to pick the EXACT type before calling TryGetValue.
    /// Avoids the FormatException that occurs when Convert.ChangeType tries to
    /// parse e.g. "Hero" as a float.
    /// </summary>
    private string FormatVariable(string varName)
    {
        // Determine the declared type from the compiled program
        if (_program != null &&
            _program.InitialValues.TryGetValue(varName, out var operand))
        {
            switch (operand.ValueCase)
            {
                case Yarn.Operand.ValueOneofCase.BoolValue:
                    _storage.TryGetValue<bool>(varName, out bool bVal);
                    return $"{varName,-30} (bool)    =  {bVal}";

                case Yarn.Operand.ValueOneofCase.FloatValue:
                    _storage.TryGetValue<float>(varName, out float fVal);
                    return $"{varName,-30} (number)  =  {fVal}";

                case Yarn.Operand.ValueOneofCase.StringValue:
                    _storage.TryGetValue<string>(varName, out string sVal);
                    return $"{varName,-30} (string)  =  \"{sVal}\"";
            }
        }

        return $"{varName,-30} ⚠️ NOT FOUND — check spelling & $ prefix";
    }

    private bool CheckStorage()
    {
        if (_storage != null) return true;
        Debug.LogError("[YarnDebugger] ❌ Variable storage is null.");
        return false;
    }
}