using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using static E_Cocktail;

/// <summary>
/// Part of CocktailSystemManager dealing exclusively with YarnSpinner:
/// commands invoked from .yarn scripts, functions queried by .yarn scripts,
/// task/satisfaction state, and Yarn variable sync + debugging.
/// </summary>
public partial class CocktailSystemManager
{
    public static bool IsWaitingForTask { get; private set; }

    // Inspector Fields

    [Header("BTN End Shift")]
    [SerializeField] private Button _endShiftBTN;

    [Header("Yarn / Task References")]
    [SerializeField] private DialogueRunner _dialogueRunner;

    [Header("UI Post-It Order")]
    [SerializeField] private Post_It_Order _postItOrder;

    // ── Task / Yarn ──
    private CharacterData _characterData;
    private bool _TaskDone = false;
    private TypeOfCocktail _cocktailType = TypeOfCocktail.None;
    private Satisfaction _satisfaction = Satisfaction.None;

    // ── Yarn variable name constants ──
    private const string TaskDoneVariableName = "$task_done";
    private const string TypeOfCocktailVariableName = "$type_of_cocktail";
    private const string SatisfactionVariableName = "$satisfaction";

    // Task — Public API

    public void CompleteTask()
    {
        _TaskDone = true;
        _dialogueRunner.VariableStorage.SetValue(TaskDoneVariableName, true);
    }

    public void SetSatisfaction(Satisfaction satisfaction) => _satisfaction = satisfaction;

    public void ServeDrink()
    {
        if (_targetCocktail == null)
        {
            Debug.LogWarning("[CocktailSystemManager] ServeDrink called with no target cocktail set.");
            return;
        }
        UpdateVariableInYarnTrigger();
    }

    private void DebugServeDrink() => DebugUpdateVariableInYarnTrigger();

    // Yarn Functions 

    /// <summary>Returns the name of a random cocktail suited to the given NPC.</summary>
    [YarnFunction("Order_Cocktail_OutName")]
    public static string RandomOrderCocktail_OutName(int NPC)
    {
        var csm = FindAnyObjectByType<CocktailSystemManager>();
        //var data = FindAnyObjectByType<CharacterData>();
        if (csm.ResolveOrderCocktail(NPC, out var drink))
        {
            return drink.Name;
        }
        else
        {
            Debug.Log($"Can't find cocktail for NPC with ID '{NPC}'");
            csm.TargetCocktail = null;
            return string.Empty;
        }
    }

    /// <summary>Returns the description of a random cocktail suited to the given NPC.</summary>
    [YarnFunction("Order_Cocktail_OutDescription")]
    public static string RandomOrderCocktail_OutDescription(int NPC)
    {
        var csm = FindAnyObjectByType<CocktailSystemManager>();

        if (csm.ResolveOrderCocktail(NPC, out var drink))
        {
            csm.TargetCocktail = drink; // Store the resolved cocktail for later reference (e.g. when serving)
            return drink.Description;
        }
        else
        {
            Debug.Log($"Can't find cocktail for NPC with ID '{NPC}'");
            csm.TargetCocktail = null;
            return string.Empty;
        }
    }

    /// <summary>Returns the name of a specific cocktail by its name.</summary>
    [YarnFunction("Order_Cocktail_ByName_OutName")]
    public static string OrderCocktail_OutSpecificName(string Name)
    {
        var csm = FindAnyObjectByType<CocktailSystemManager>();

        if (csm.ResolveOrderCocktail(Name, out var drink))
        {
            csm.TargetCocktail = drink; // Store the resolved cocktail for later reference (e.g. when serving)
            return drink.Name;
        }
        else
        {
            Debug.Log($"Can't find cocktail with name '{Name}'");
            csm.TargetCocktail = null;
            return string.Empty;
        }
    }

    [YarnFunction("Order_Cocktail_ByName_OutDescription")]
    public static string OrderCocktail_OutSpecificDescription(string Name)
    {
        var csm = FindAnyObjectByType<CocktailSystemManager>();
        if (csm.ResolveOrderCocktail(Name, out var drink))
        {
            csm.TargetCocktail = drink; // Store the resolved cocktail for later reference (e.g. when serving)
            return drink.Description;
        }
        else
        {
            Debug.Log($"Can't find cocktail with name '{Name}'");
            csm.TargetCocktail = null;
            return string.Empty;
        }
    }

    /// <summary>
    /// Shared helper used by both Yarn functions above.
    /// Picks a cocktail for the NPC and returns it via <paramref name="drink"/>.
    /// </summary>
    private bool ResolveOrderCocktail(int NPC, out S_Drink drink)
    {
        drink = default;
        var defaultOptions = new List<TypeOfCocktail>
            { TypeOfCocktail.HighAlcohol, TypeOfCocktail.LowAlcohol, TypeOfCocktail.NoneAlcohol };

        NPC_Name npcName = (NPC_Name)NPC;

        if (_characterData == null
            || !_characterData.NPC_Favorite_Drink.TryGetValue(npcName, out List<TypeOfCocktail> cocktailOptions)
            || cocktailOptions.Count == 0)
        {
            Debug.LogWarning($"[CocktailSystemManager] No favorites for {npcName}, using defaults.");
            cocktailOptions = defaultOptions;
        }

        drink = RandomCocktail(cocktailOptions[Random.Range(0, cocktailOptions.Count)]);
        return true;
    }
    private bool ResolveOrderCocktail(string Name, out S_Drink drink)
    {
        drink = default;
        if (string.IsNullOrEmpty(Name))
        {
            Debug.LogWarning("[CocktailSystemManager] Empty name provided to Order_Cocktail_SpecificName.");
            return false;
        }
        var match = _normalDrinks.FirstOrDefault(d => d.Name.Equals(Name, System.StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            Debug.LogWarning($"[CocktailSystemManager] No cocktail found with name '{Name}'.");
            return false;
        }
        drink = match;
        return true;
    }

    // Yarn Commands 

    /// <summary>
    /// &lt;&lt;wait_for_task CocktailSystemManager&gt;&gt;
    /// Suspends Yarn until UpdateVariableInYarn() returns true.
    /// Does NOT freeze Unity — the game keeps running normally.
    /// </summary>
    [YarnCommand("wait_for_task")]
    public IEnumerator WaitForTask()
    {
        _postItOrder.ShowPostItForDefaultDuration();
        if (SceneLoaderBridge.IsSilentReplay) yield break;
        IsWaitingForTask = true;
        EnableButtonInYarn(true);
        yield return new WaitUntil(() => UpdateVariableInYarn());
        IsWaitingForTask = false;
    }

    [YarnCommand("wait_scene")]         // use <<wait_scene 1>> in .yarn instead of <<wait 1>>
    public static IEnumerator WaitScene(float seconds)
    {
        if (SceneLoaderBridge.IsSilentReplay) yield break;
        yield return new WaitForSeconds(seconds);
    }

    [YarnCommand("Can_End_Shift")]
    public void CanEndShift()
    {
        if (SceneLoaderBridge.IsSilentReplay) return; // ponytail: skip — UI side effect
        EnableButtonInYarn(false);
        _endShiftBTN.gameObject.SetActive(true);
        Debug.Log("[CocktailSystemManager] CanEndShift called.");
    }

    [YarnCommand("Enable_InteractableObject")]
    public void EnableButtonInYarn(bool enable)
    {
        if (SceneLoaderBridge.IsSilentReplay) return; // ponytail: skip — UI side effect
        _cocktailShaker.Interactable = enable;
        _cocktailShakerData.SetIngredientActive(enable);

        foreach (var btn in _cocktailShakerData.ingredientButtons)
        {
            if (btn == null) continue;
            if (btn.TryGetComponent<Interactable_2_5DObject>(out var interactable)) interactable.Interactable = enable;
            if (btn.TryGetComponent<UIPointerSound>(out var sound)) sound.Interactable = enable;
            if (btn.TryGetComponent<DragableObject>(out var drag)) drag.Interactable = enable;
            if (btn.TryGetComponent<ScaleOnHover>(out var scale)) scale.Interactable = enable;
        }
    }

    /// <summary>
    /// &lt;&lt;Reset_Variable CocktailSystemManager&gt;&gt;
    /// Resets BOTH C# fields AND Yarn storage.
    /// </summary>
    [YarnCommand("Reset_Variable")]
    public void ResetVariableInYarn()
    {
        if (SceneLoaderBridge.IsSilentReplay) return; // ponytail: skip — vars already restored from save
        _TaskDone = false;
        _satisfaction = Satisfaction.None;
        _cocktailType = TypeOfCocktail.None;

        _dialogueRunner.VariableStorage.SetValue(SatisfactionVariableName, 0f);
        _dialogueRunner.VariableStorage.SetValue(TypeOfCocktailVariableName, 0f);
        _dialogueRunner.VariableStorage.SetValue(TaskDoneVariableName, false);

        Debug.Log("[CocktailSystemManager] All variables reset — ready for next loop.");
    }

    // Private Yarn Helpers
    private void UpdateVariableInYarnTrigger()
    {
        _TaskDone = true;
        SetSatisfaction(CalculateSatisfaction());
        Debug.Log($"[CocktailSystemManager] Satisfaction → {_satisfaction}");
        DebugVariableFromYarn();
        UpdateVariableInYarn();
        DebugVariableFromYarn();
    }

    private void DebugUpdateVariableInYarnTrigger()
    {
        _TaskDone = true;
        Debug.Log($"[CocktailSystemManager] (Debug) Satisfaction → {_satisfaction}");
        DebugVariableFromYarn();
        UpdateVariableInYarn();
        DebugVariableFromYarn();
    }

    /// <summary>
    /// Writes C# state into Yarn's VariableStorage.
    /// Returns true only when both game-condition and satisfaction are ready.
    /// Used as the WaitUntil predicate in WaitForTask().
    /// </summary>
    private bool UpdateVariableInYarn()
    {
        if (_TaskDone && _satisfaction != Satisfaction.None)
        {
            EnableButtonInYarn(false);
            _dialogueRunner.VariableStorage.SetValue(SatisfactionVariableName, (int)_satisfaction);
            _dialogueRunner.VariableStorage.SetValue(TypeOfCocktailVariableName, (int)_cocktailType);
            _dialogueRunner.VariableStorage.SetValue(TaskDoneVariableName, true);

            //disable UI Post-it
            _postItOrder.SetOutScreen(true);

            return true;
        }
        return false;
    }

    // Debug
#if UNITY_EDITOR
    [ContextMenu("Perfect Satisfaction")]
    public void SetSatisfactionPerfect()
    {
        DebugVariableFromYarn();
        SetSatisfaction(Satisfaction.Perfect);
        DebugServeDrink();
        DebugVariableFromYarn();
    }

    [ContextMenu("Acceptable Satisfaction")]
    public void SetSatisfactionAcceptable()
    {
        DebugVariableFromYarn();
        SetSatisfaction(Satisfaction.Acceptable);
        DebugServeDrink();
        DebugVariableFromYarn();
    }

    [ContextMenu("Fail Satisfaction")]
    public void SetSatisfactionFail()
    {
        DebugVariableFromYarn();
        SetSatisfaction(Satisfaction.Fail);
        DebugServeDrink();
        DebugVariableFromYarn();
    }
#endif

    /// <summary>Prints a side-by-side snapshot of every tracked Yarn variable.</summary>
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
            ? ((Satisfaction)(int)yarnSatisfactionRaw).ToString() : $"UNKNOWN({yarnSatisfactionRaw})";
        string yarnCocktailName = System.Enum.IsDefined(typeof(TypeOfCocktail), (int)yarnCocktailRaw)
            ? ((TypeOfCocktail)(int)yarnCocktailRaw).ToString() : $"UNKNOWN({yarnCocktailRaw})";

        string sep = new string('─', 72);
        string log = "\n" + sep
            + "\n  [CocktailSystemManager] Variable Debug Snapshot"
            + "\n" + sep
            + string.Format("\n  {0,-26} {1,-20} {2,-8} {3,-20} {4}", "Variable", "C# Value", "Yarn Raw", "Yarn Enum", "Match?")
            + "\n" + sep
            //+ FormatEnumRow(TaskDoneVariableName, _TaskDone.ToString(), yarnTaskDone.ToString(), yarnTaskDone.ToString())
            + FormatEnumRow(SatisfactionVariableName, _satisfaction.ToString(), yarnSatisfactionRaw.ToString("0"), yarnSatisfactionName)
            + FormatEnumRow(TypeOfCocktailVariableName, _cocktailType.ToString(), yarnCocktailRaw.ToString("0"), yarnCocktailName)
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