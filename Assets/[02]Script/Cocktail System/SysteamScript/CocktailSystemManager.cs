using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using static E_Cocktail;

/// <summary>
/// Combined manager for cocktail logic AND Yarn task/variable management.
///
/// ── KEY RULE: HOW YARN STORES ENUM VALUES ────────────────────────────────
///  Yarn enums declared with &lt;&lt;enum&gt;&gt; are stored as INTEGERS (float) in
///  VariableStorage — NOT as strings like "Satisfaction.Perfect".
///
///  Fix: always SetValue with (float)(int)enumValue so the type matches.
///
/// ── ENUM → INTEGER MAPPING (must match .yarn declaration order) ──────────
///  Satisfaction:   None=0  Fail=1  Acceptable=2  Perfect=3
///  TypeOfCocktail: None=0  HighAlcohol=1  LowAlcohol=2  NoneAlcohol=3  NotMatch=4
/// </summary>
public class CocktailSystemManager : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════
    // Inspector Fields
    // ═════════════════════════════════════════════════════════════════════

    [Header("Cocktail Lists")]
    [SerializeField] private SO_CocktailList _normalCocktailList;
    [SerializeField] private SO_CocktailList _specialCocktailList;

    [Header("Fallback")]
    [SerializeField] private Sprite _failCocktailSprite;

    [Header("BTN End Shift")]
    [SerializeField] private Button _endShiftBTN;

    [Header("Cocktail References")]
    public CocktailShakerData _cocktailShakerData;
    public CocktailShaker _cocktailShaker;

    [Header("Yarn / Task References")]
    [SerializeField] private DialogueRunner _dialogueRunner;

    // ═════════════════════════════════════════════════════════════════════
    // Private State
    // ═════════════════════════════════════════════════════════════════════

    // — Cocktail —
    private List<S_Drink> _normalDrinks = new List<S_Drink>();
    private S_Drink _targetCocktail;
    public S_Drink TargetCocktail { get; set; }

    // — Task / Yarn —
    private CharacterData _characterData;
    private bool _myGameCondition = false;
    private TypeOfCocktail _cocktailType = TypeOfCocktail.None;
    private Satisfaction _satisfaction = Satisfaction.None;

    // — Yarn variable name constants —
    private const string TaskDoneVariableName = "$task_done";
    private const string TypeOfCocktailVariableName = "$type_of_cocktail";
    private const string SatisfactionVariableName = "$satisfaction";

    // ═════════════════════════════════════════════════════════════════════
    // Unity Lifecycle
    // ═════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _characterData = FindAnyObjectByType<CharacterData>();
        if (_characterData == null)
            Debug.LogError("[CocktailSystemManager] CharacterData not found in scene.");
    }

    private void Start()
    {
        // Cache runtime drink list once
        foreach (var so in _normalCocktailList.cocktails)
            _normalDrinks.Add(so.ToDrink());
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
        {
            UpdateCocktailInShaker();
            Debug.Log(_cocktailShakerData.currentCocktail.GetOfCocktailInfo());
        }


        // ── Task debug shortcuts ─────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.E))
        {
            ServeDrink();
            Debug.Log("[CocktailSystemManager] Task marked complete via E key.");
        }
        if (Input.GetKeyDown(KeyCode.A)) SetSatisfactionPerfect();
        if (Input.GetKeyDown(KeyCode.S)) SetSatisfactionAcceptable();
        if (Input.GetKeyDown(KeyCode.D)) SetSatisfactionFail();
#endif
    }

    // ═════════════════════════════════════════════════════════════════════
    // Cocktail — Public API
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Pick a random cocktail (any type) as the current target.</summary>
    public S_Drink RandomCocktail()
    {
        int idx = Random.Range(0, _normalCocktailList.cocktails.Count);
        _targetCocktail = _normalCocktailList.cocktails[idx].ToDrink();
        return _targetCocktail;
    }

    /// <summary>Pick a random cocktail of a specific type as the current target.</summary>
    public S_Drink RandomCocktail(TypeOfCocktail type)
    {
        var matches = _normalCocktailList.cocktails
            .Where(c => c.GetTypeOfAlcohol() == type)
            .ToList();

        if (matches.Count == 0)
        {
            Debug.LogWarning($"[CocktailSystem] No cocktails of type {type}. Falling back to random.");
            return RandomCocktail();
        }

        _targetCocktail = matches[Random.Range(0, matches.Count)].ToDrink();
        return _targetCocktail;
    }

    public Satisfaction CalculateSatisfaction()
        => _targetCocktail.CalculateSatisfaction(_cocktailShakerData.currentCocktail);

    public string GetTargetName() => _targetCocktail.Name;

    /// <summary>
    /// This Using Only Debug: Call this to force-update the cocktail in the shaker based on current ingredients,
    /// Derive identity of whatever is currently in the shaker and update visuals.
    /// </summary>
    public void UpdateCocktailInShaker()
    {
        S_Drink current = _cocktailShakerData.currentCocktail;
        current.UpdateTypeOfAlcohol(_normalDrinks);
        current.UpdateName(_normalDrinks);
        current.UpdatePrice(_normalDrinks);

        Sprite sprite = current.GetCocktailSprite(_normalDrinks) ?? _failCocktailSprite;
        //Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, 0));
        _cocktailShaker.SetBTNSprite(sprite, sprite, sprite);
    }

    /// <summary>Editor/debug helper — sets a random target without returning it.</summary>
    public void RandomCocktailForDebug() => RandomCocktail();

    // ═════════════════════════════════════════════════════════════════════
    // Yarn Functions — called from .yarn scripts
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Returns the name of a random cocktail suited to the given NPC.</summary>
    [YarnFunction("Order_Cocktail_OutName")]
    public static string RandomOrderCocktail_OutName(int NPC)
    {
        // Static Yarn functions need access to scene objects; locate them fresh each call.
        var csm = FindAnyObjectByType<CocktailSystemManager>();
        var data = FindAnyObjectByType<CharacterData>();
        return csm.ResolveOrderCocktail(NPC, data, out var drink) ? drink.Name : string.Empty;
    }

    /// <summary>Returns the description of a random cocktail suited to the given NPC.</summary>
    [YarnFunction("Order_Cocktail_OutDescription")]
    public static string RandomOrderCocktail_OutDescription(int NPC)
    {
        var csm = FindAnyObjectByType<CocktailSystemManager>();
        var data = FindAnyObjectByType<CharacterData>();
        return csm.ResolveOrderCocktail(NPC, data, out var drink) ? drink.Description : string.Empty;
    }

    /// <summary>
    /// Shared helper used by both static Yarn functions above.
    /// Picks a random cocktail for the NPC and returns it via <paramref name="drink"/>.
    /// </summary>
    private bool ResolveOrderCocktail(int NPC, CharacterData data, out S_Drink drink)
    {
        drink = default;
        List<TypeOfCocktail> defaultOptions = new() { TypeOfCocktail.HighAlcohol, TypeOfCocktail.LowAlcohol, TypeOfCocktail.NoneAlcohol };

        NPC_Name npcName = (NPC_Name)NPC;

        if (data == null
            || !data.NPC_Favorite_Drink.TryGetValue(npcName, out List<TypeOfCocktail> cocktailOptions)
            || cocktailOptions.Count == 0)
        {
            Debug.LogWarning($"[CocktailSystemManager] No favorites found for {npcName}, using defaults.");
            cocktailOptions = defaultOptions;
        }

        drink = RandomCocktail(cocktailOptions[Random.Range(0, cocktailOptions.Count)]);
        return true;
    }

    // ═════════════════════════════════════════════════════════════════════
    // Task — Public API
    // ═════════════════════════════════════════════════════════════════════

    public void CompleteTask()
    {
        _myGameCondition = true;
        _dialogueRunner.VariableStorage.SetValue(TaskDoneVariableName, true);
    }

    public void SetSatisfaction(Satisfaction satisfaction) => _satisfaction = satisfaction;

    public void ServeDrink() => UpdateVariableInYarnTrigger();
    private void DebugServeDrink() => DebugUpdateVariableInYarnTrigger();

    #region Uitility / Debug
#if UNITY_EDITOR

    [ContextMenu("Perfect Satisfaction")]
    public void SetSatisfactionPerfect()
    {
        DebugVariableFromYarn();
        SetSatisfaction(Satisfaction.Perfect);
        DebugServeDrink();
        //UpdateVariableInYarn();
        DebugVariableFromYarn();
    }

    [ContextMenu("Acceptable Satisfaction")]
    public void SetSatisfactionAcceptable()
    {
        DebugVariableFromYarn();
        SetSatisfaction(Satisfaction.Acceptable);
        DebugServeDrink();
        //UpdateVariableInYarn();
        DebugVariableFromYarn();
    }

    [ContextMenu("Fail Satisfaction")]
    public void SetSatisfactionFail()
    {
        DebugVariableFromYarn();
        SetSatisfaction(Satisfaction.Fail);
        DebugServeDrink();
        //UpdateVariableInYarn();
        DebugVariableFromYarn();
    }
    #endif
    #endregion

    /// <summary>
    /// Writes C# state into Yarn's VariableStorage.
    /// Returns true (and writes) only when both conditions are ready.
    /// Used as the WaitUntil predicate in WaitForTask().
    ///
    /// CRITICAL: enum values MUST be cast to (float)(int) — Yarn stores
    /// enums as integers, not strings.
    /// </summary>
    private bool UpdateVariableInYarn()
    {
        if (_myGameCondition && _satisfaction != Satisfaction.None)
        {
            EnableButtonInYarn(false);
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

    /// <summary>
    /// Calculates satisfaction from the current shaker vs target, then pushes
    /// all variables to Yarn. Call this when the player submits their cocktail.
    /// </summary>
    private void UpdateVariableInYarnTrigger()
    {
        _myGameCondition = true;
        SetSatisfaction(CalculateSatisfaction());
        Debug.Log($"[CocktailSystemManager] Satisfaction set to {_satisfaction} based on cocktail comparison.");
        DebugVariableFromYarn();
        UpdateVariableInYarn();
        DebugVariableFromYarn();
    }

    private void DebugUpdateVariableInYarnTrigger()
    {
        _myGameCondition = true;
        //SetSatisfaction(CalculateSatisfaction());
        Debug.Log($"[CocktailSystemManager] Satisfaction set to {_satisfaction} based on cocktail comparison.");
        DebugVariableFromYarn();
        UpdateVariableInYarn();
        DebugVariableFromYarn();
    }


    // ═════════════════════════════════════════════════════════════════════
    // Yarn Commands
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// &lt;&lt;wait_for_task CocktailSystemManager&gt;&gt;
    /// Suspends Yarn until UpdateVariableInYarn() returns true.
    /// Does NOT freeze the game — Unity keeps running normally.
    /// </summary>
    [YarnCommand("wait_for_task")]
    public IEnumerator WaitForTask()
    {
        EnableButtonInYarn(true);
        yield return new WaitUntil(() => UpdateVariableInYarn());
    }

    [YarnCommand("Can_End_Shift")]
    public void CanEndShift() { 
        EnableButtonInYarn(false);
        _endShiftBTN.gameObject.SetActive(true);
        Debug.Log($"[CocktailSystemManager] CanEndShift called.");
    }


    /// <summary>
    /// Enables the interactable object (e.g. cocktail shaker) so the player can click it.
    /// </summary>
    [YarnCommand("Enable_InteractableObject")]
    public void EnableButtonInYarn(bool enable)
    {
        _cocktailShaker.Interactable = enable;
        _cocktailShakerData.SetIngredientActive(enable);
        foreach (var btn in _cocktailShakerData.ingredientButtons)
        {
            var interactable = btn.GetComponent<Interactable3DObject>();
            if (interactable != null)
                interactable.Interactable = enable;
            var sound = btn.GetComponent<UIPointerSound>();
            if (sound != null)
                sound.Interactable = enable;
            var draggable = btn.GetComponent<DragableObject>();
            if (draggable != null)
                draggable.Interactable = enable;
            var scaleOnHover = btn.GetComponent<ScaleOnHover>();
            if (scaleOnHover != null)
                scaleOnHover.Interactable = enable;
        }

    }

    /// <summary>
    /// &lt;&lt;Reset_Variable CocktailSystemManager&gt;&gt;
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

        Debug.Log("[CocktailSystemManager] All variables reset — ready for next loop.");
    }
    #region Debug Commands
    // ═════════════════════════════════════════════════════════════════════
    // Debug
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Prints a side-by-side snapshot of every tracked Yarn variable.
    ///
    ///   C# Value  = what this script holds in memory
    ///   Yarn Raw  = the raw value in VariableStorage
    ///   Yarn Enum = human-readable name decoded from the raw integer
    ///   Match?    = do C# and Yarn agree?
    ///
    /// Call from:
    ///   Inspector → right-click → "Debug Yarn Variables"
    ///   Code      → cocktailSystemManager.DebugVariableFromYarn()
    ///   Yarn      → &lt;&lt;debug_yarn_variables CocktailSystemManager&gt;&gt;
    /// </summary>
    [ContextMenu("Debug Yarn Variables")]
    public void DebugVariableFromYarn()
    {
        if (_dialogueRunner == null || _dialogueRunner.VariableStorage == null)
        {
            Debug.LogWarning("[CocktailSystemManager] Cannot debug — DialogueRunner or VariableStorage is null.");
            return;
        }

        var storage = _dialogueRunner.VariableStorage;

        storage.TryGetValue(TaskDoneVariableName, out bool yarnTaskDone);
        storage.TryGetValue(SatisfactionVariableName, out float yarnSatisfactionRaw);
        storage.TryGetValue(TypeOfCocktailVariableName, out float yarnCocktailRaw);

        string yarnSatisfactionName = System.Enum.IsDefined(typeof(Satisfaction), (int)yarnSatisfactionRaw)
            ? ((Satisfaction)(int)yarnSatisfactionRaw).ToString()
            : $"UNKNOWN({yarnSatisfactionRaw})";

        string yarnCocktailName = System.Enum.IsDefined(typeof(TypeOfCocktail), (int)yarnCocktailRaw)
            ? ((TypeOfCocktail)(int)yarnCocktailRaw).ToString()
            : $"UNKNOWN({yarnCocktailRaw})";

        string sep = new string('─', 72);

        string log = "\n" + sep
            + "\n  [CocktailSystemManager] Variable Debug Snapshot"
            + "\n" + sep
            + string.Format("\n  {0,-26} {1,-20} {2,-8} {3,-20} {4}",
                            "Variable", "C# Value", "Yarn Raw", "Yarn Enum", "Match?")
            + "\n" + sep
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
            + "\n" + sep
            + "\n  NOTE: Match compares C# enum name vs Yarn decoded enum name."
            + "\n" + sep;

        Debug.Log(log);
    }

    private string FormatEnumRow(string varName, string csValue, string yarnRaw, string yarnEnum)
    {
        bool match = string.Equals(csValue, yarnEnum, System.StringComparison.Ordinal);
        string icon = match ? "OK" : "MISMATCH <--";
        return string.Format("\n  {0,-26} {1,-20} {2,-8} {3,-20} {4}",
                             varName, csValue, yarnRaw, yarnEnum, icon);
    }

    /// <summary>&lt;&lt;debug_yarn_variables CocktailSystemManager&gt;&gt;</summary>
    [YarnCommand("debug_yarn_variables")]
    public void YarnDebugVariables() => DebugVariableFromYarn();
    #endregion
}