using UnityEngine;
using Yarn.Unity;
using System.Collections;
using static E_Cocktail;
using System.Collections.Generic;

/// <summary>
/// Manages a task condition that YarnSpinner polls via a loop.
///
/// ── KEY RULE: HOW YARN STORES ENUM VALUES ────────────────────────────────
///  Yarn enums declared with <<enum>> are stored as INTEGERS (float) in
///  VariableStorage — NOT as strings like "Satisfaction.Perfect".
///
///  Proof from debug output:
///    $type_of_cocktail (never set from C#) → Yarn returns "0"   ← integer!
///    $satisfaction (set via string "Satisfaction.Perfect")       ← string mismatch!
///
///  Yarn evaluates:   $satisfaction == Satisfaction.Perfect
///    Left side:  the string "Satisfaction.Perfect"  (wrong type — set by old code)
///    Right side: the integer 3                       (how Yarn stores Perfect)
///    Result:     NEVER MATCHES → <<if>> branch never fires
///                → <<Reset_Variable>> never runs
///                → _myGameCondition stays true
///                → next <<wait_for_task>> skips instantly
///
///  Fix: always SetValue with (float)(int)enumValue so the type matches.
///
/// ── ENUM → INTEGER MAPPING (must match .yarn declaration order) ──────────
///  Satisfaction:   None=0  Fail=1  Acceptable=2  Perfect=3
///  TypeOfCocktail: None=0  HighAlcohol=1  LowAlcohol=2  NoneAlcohol=3  NotMatch=4
/// </summary>
public class TaskManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueRunner _dialogueRunner;
    [SerializeField] public static CocktailSystemManager _cocktailSystemManager;
    [SerializeField] public static CharacterData _characterData;

    private bool _myGameCondition = false;
    private TypeOfCocktail _cocktailType = TypeOfCocktail.None;
    private Satisfaction _satisfaction = Satisfaction.None;

    const string TaskDoneVariableName = "$task_done";
    const string TypeOfCocktailVariableName = "$type_of_cocktail";
    const string SatisfactionVariableName = "$satisfaction";

    private void Awake()
    {
        _cocktailSystemManager = FindAnyObjectByType<CocktailSystemManager>();
        if (_cocktailSystemManager == null)
        {
            Debug.LogError("[TaskManager] CocktailSystemManager not found in scene.");
        }
        _characterData = FindAnyObjectByType<CharacterData>();
        if (_characterData == null)
        {
            Debug.LogError("[TaskManager] CharacterData not found in scene.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────
    [YarnFunction("Order_Cocktail_OutName")]
    public static string RandomOrderCocktail_OutName(int NPC)
    {
        List<TypeOfCocktail> defaultOptions = new() { TypeOfCocktail.HighAlcohol, TypeOfCocktail.LowAlcohol, TypeOfCocktail.NoneAlcohol };

        NPCName npcName = (NPCName)NPC;

        if (!_characterData.NPC_Favorite_Drink.TryGetValue(npcName, out List<TypeOfCocktail> cocktailOptions)
            || cocktailOptions.Count == 0)
        {
            Debug.LogWarning($"[TaskManager] No favorites found for {npcName}, using default.");
            cocktailOptions = defaultOptions;
        }

        S_Drink TargetOCktail = _cocktailSystemManager.RandomCocktail(cocktailOptions[Random.Range(0, cocktailOptions.Count)]);

        return TargetOCktail.Name;
        //Test out put
        //return string.Join(", ", cocktailOptions);
    }
    [YarnFunction("Order_Cocktail_OutDescription")]
    public static string RandomOrderCocktail_OutDescription(int NPC)
    {
        List<TypeOfCocktail> defaultOptions = new() { TypeOfCocktail.HighAlcohol, TypeOfCocktail.LowAlcohol, TypeOfCocktail.NoneAlcohol };

        NPCName npcName = (NPCName)NPC;

        if (!_characterData.NPC_Favorite_Drink.TryGetValue(npcName, out List<TypeOfCocktail> cocktailOptions)
            || cocktailOptions.Count == 0)
        {
            Debug.LogWarning($"[TaskManager] No favorites found for {npcName}, using default.");
            cocktailOptions = defaultOptions;
        }

        S_Drink TargetOCktail = _cocktailSystemManager.RandomCocktail(cocktailOptions[Random.Range(0, cocktailOptions.Count)]);

        return TargetOCktail.Description;
        //Test out put
        //return string.Join(", ", cocktailOptions);
    }

    public void CompleteTask()
    {
        _myGameCondition = true;
        _dialogueRunner.VariableStorage.SetValue(TaskDoneVariableName, true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            CompleteTask();
            Debug.Log("[TaskManager] Task marked complete via E key.");
        }

        if (Input.GetKeyDown(KeyCode.A)) { 
            SetSatifiactionPerfect();
        }
        if(Input.GetKeyDown(KeyCode.S)) { 
            SetSatisfactionAcceptable();
        }
        if(Input.GetKeyDown(KeyCode.D)) { 
            SetSatisfactionFail();
        }
    }

    [ContextMenu("Perfect Satisfaction")]
    public void SetSatifiactionPerfect()
    {
        DebugVariableFromYarn();
        SetSatisfaction(Satisfaction.Perfect);
        CompleteTask();
        UpdateVariableInYarn();
        DebugVariableFromYarn();
    }

    [ContextMenu("Acceptable Satisfaction")]
    public void SetSatisfactionAcceptable()
    {
        DebugVariableFromYarn();
        SetSatisfaction(Satisfaction.Acceptable);
        CompleteTask();
        UpdateVariableInYarn();
        DebugVariableFromYarn();
    }

    [ContextMenu("Fail Satisfaction")]
    public void SetSatisfactionFail()
    {
        DebugVariableFromYarn();
        SetSatisfaction(Satisfaction.Fail);
        CompleteTask();
        UpdateVariableInYarn();
        DebugVariableFromYarn();
    }

    public void SetSatisfaction(Satisfaction satisfaction)
    {
        _satisfaction = satisfaction;
    }

    /// <summary>
    /// Writes C# state into Yarn's VariableStorage.
    /// Returns true (and writes) only when both conditions are ready.
    /// Used as the WaitUntil predicate in WaitForTask().
    ///
    /// CRITICAL: enum values MUST be cast to (float)(int) — Yarn stores
    /// enums as integers, not strings. Passing a string like
    /// "Satisfaction.Perfect" causes a type mismatch and the <<if>>
    /// comparison in Yarn always evaluates to false.
    /// </summary>
    public bool UpdateVariableInYarn()
    {
        if (_myGameCondition && _satisfaction != Satisfaction.None)
        {
           
            // Cast enum to its integer index — this is the type Yarn actually stores.
            // Satisfaction order in .yarn: None=0, Fail=1, Acceptable=2, Perfect=3
            _dialogueRunner.VariableStorage.SetValue(SatisfactionVariableName,
                                                    (float)(int)_satisfaction);

            // TypeOfCocktail order: None=0, HighAlcohol=1, LowAlcohol=2, NoneAlcohol=3, NotMatch=4
            _dialogueRunner.VariableStorage.SetValue(TypeOfCocktailVariableName,
                                                    (float)(int)_cocktailType);

            _dialogueRunner.VariableStorage.SetValue(TaskDoneVariableName, true);
            return true;
        }
        return false;
    }
    public void UpdateVariableInYarnTrigger()
    {
        _myGameCondition = true;
        SetSatisfaction(_cocktailSystemManager.CalculateSatisfaction());
        Debug.Log($"[TaskManager] Satisfaction set to {_satisfaction} based on cocktail comparison.");
        DebugVariableFromYarn();
        UpdateVariableInYarn();
        DebugVariableFromYarn();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Yarn Commands
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <<wait_for_task TaskManager>>
    /// Suspends Yarn until UpdateVariableInYarn() returns true.
    /// Does NOT freeze the game — Unity keeps running normally.
    /// </summary>
    [YarnCommand("wait_for_task")]
    public IEnumerator WaitForTask()
    {
        yield return new WaitUntil(() => UpdateVariableInYarn());
    }

    /// <summary>
    /// <<Reset_Variable TaskManager>>
    /// Resets BOTH C# fields AND Yarn storage.
    /// Must reset C# fields — if only Yarn storage is cleared,
    /// the next WaitForTask() sees _myGameCondition=true and skips instantly.
    /// </summary>
    [YarnCommand("Reset_Variable")]
    public void ResetVariableInYarn()
    {
        // Reset C# state
        _myGameCondition = false;
        _satisfaction = Satisfaction.None;
        _cocktailType = TypeOfCocktail.None;

        // Reset Yarn storage — use 0f for enums (integer index of None)
        _dialogueRunner.VariableStorage.SetValue(SatisfactionVariableName, 0f);
        _dialogueRunner.VariableStorage.SetValue(TypeOfCocktailVariableName, 0f);
        _dialogueRunner.VariableStorage.SetValue(TaskDoneVariableName, false);

        Debug.Log("[TaskManager] All variables reset — ready for next loop.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Debug
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Prints a side-by-side snapshot of every tracked variable.
    ///
    /// Enum variables are read from Yarn as float (their integer index)
    /// and converted back to the enum name for readability.
    ///
    ///   C# Value  = what this script holds in memory
    ///   Yarn Raw  = the raw value in VariableStorage
    ///   Yarn Enum = human-readable name decoded from the raw integer
    ///   Match?    = do C# and Yarn agree?
    ///
    /// Call from:
    ///   Inspector → right-click → "Debug Yarn Variables"
    ///   Code      → taskManager.DebugVariableFromYarn()
    ///   Yarn      → <<debug_yarn_variables TaskManager>>
    /// </summary>
    [ContextMenu("Debug Yarn Variables")]
    public void DebugVariableFromYarn()
    {
        if (_dialogueRunner == null || _dialogueRunner.VariableStorage == null)
        {
            Debug.LogWarning("[TaskManager] Cannot debug — DialogueRunner or VariableStorage is null.");
            return;
        }

        var storage = _dialogueRunner.VariableStorage;

        // Read bool
        storage.TryGetValue(TaskDoneVariableName, out bool yarnTaskDone);

        // Read enums as float (Yarn stores them as integer index)
        storage.TryGetValue(SatisfactionVariableName, out float yarnSatisfactionRaw);
        storage.TryGetValue(TypeOfCocktailVariableName, out float yarnCocktailRaw);

        // Decode raw integer back to enum name for readability
        string yarnSatisfactionName = System.Enum.IsDefined(typeof(Satisfaction), (int)yarnSatisfactionRaw)
            ? ((Satisfaction)(int)yarnSatisfactionRaw).ToString()
            : $"UNKNOWN({yarnSatisfactionRaw})";

        string yarnCocktailName = System.Enum.IsDefined(typeof(TypeOfCocktail), (int)yarnCocktailRaw)
            ? ((TypeOfCocktail)(int)yarnCocktailRaw).ToString()
            : $"UNKNOWN({yarnCocktailRaw})";

        string separator = new string('─', 72);

        string log = "\n" + separator
            + "\n  [TaskManager] Variable Debug Snapshot"
            + "\n" + separator
            + string.Format("\n  {0,-26} {1,-20} {2,-8} {3,-20} {4}",
                "Variable", "C# Value", "Yarn Raw", "Yarn Enum", "Match?")
            + "\n" + separator
            + FormatEnumRow(TaskDoneVariableName,
                            _myGameCondition.ToString(),
                            yarnTaskDone.ToString(),
                            yarnTaskDone.ToString())
            + FormatEnumRow(SatisfactionVariableName,
                            _satisfaction.ToString(),
                            yarnSatisfactionRaw.ToString("0"),
                            yarnSatisfactionName)
            + FormatEnumRow(TypeOfCocktailVariableName,
                            _cocktailType.ToString(),
                            yarnCocktailRaw.ToString("0"),
                            yarnCocktailName)
            + "\n" + separator
            + "\n  NOTE: Match compares C# enum name vs Yarn decoded enum name."
            + "\n" + separator;

        Debug.Log(log);
    }

    private string FormatEnumRow(string varName, string csValue,
                                  string yarnRaw, string yarnEnum)
    {
        bool match = string.Equals(csValue, yarnEnum, System.StringComparison.Ordinal);
        string icon = match ? "OK" : "MISMATCH <--";
        return string.Format("\n  {0,-26} {1,-20} {2,-8} {3,-20} {4}",
                             varName, csValue, yarnRaw, yarnEnum, icon);
    }

    /// <summary><<debug_yarn_variables TaskManager>></summary>
    [YarnCommand("debug_yarn_variables")]
    public void YarnDebugVariables() => DebugVariableFromYarn();
}