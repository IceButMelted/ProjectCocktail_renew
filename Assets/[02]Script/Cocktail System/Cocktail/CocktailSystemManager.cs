using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using static E_Cocktail;

public class CocktailSystemManager : MonoBehaviour
{
    // ═════════════════════════════════════════════════════
    // Inspector Fields
    // ═════════════════════════════════════════════════════

    [Header("Cocktail Repository")]
    [Tooltip("Assign an SO_CocktailList asset — or any IDrinkRepository implementation.")]
    [SerializeField] private SO_CocktailList _normalCocktailRepository;
    [SerializeField] private SO_CocktailList _specialCocktailRepository;

    [Header("Fallback")]
    [SerializeField] private Sprite _failCocktailSprite;

    [Header("BTN End Shift")]
    [SerializeField] private Button _endShiftBTN;

    [Header("Cocktail References")]
    public CocktailShakerData _cocktailShakerData;
    public CocktailShaker _cocktailShaker;

    [Header("Yarn / Task References")]
    [SerializeField] private DialogueRunner _dialogueRunner;

    // ═════════════════════════════════════════════════════
    // Private State
    // ═════════════════════════════════════════════════════

    // ── Repositories (as interfaces) ──
    private IDrinkRepository _normalRepository;

    // ── Cocktail ──
    private IReadOnlyList<S_Drink> _normalDrinks;
    private S_Drink _targetCocktail;
    public S_Drink TargetCocktail { get; private set; }

    // ── Task / Yarn ──
    private CharacterData _characterData;
    private bool _myGameCondition = false;
    private TypeOfCocktail _cocktailType = TypeOfCocktail.None;
    private Satisfaction _satisfaction = Satisfaction.None;

    // ── Yarn variable name constants ──
    private const string TaskDoneVariableName = "$task_done";
    private const string TypeOfCocktailVariableName = "$type_of_cocktail";
    private const string SatisfactionVariableName = "$satisfaction";

    // ═════════════════════════════════════════════════════
    // Unity Lifecycle
    // ═════════════════════════════════════════════════════

    private void Awake()
    {
        _characterData = GetComponent<CharacterData>();
        if (_characterData == null)
            Debug.LogError("[CocktailSystemManager] CharacterData not found in Component.");

        // Depend on the interface, not the concrete type.
        _normalRepository = _normalCocktailRepository;
    }

    private void Start()
    {
        // Cache the read-only list once — no .ToDrink() copy needed;
        // S_Drink IS the data now.
        _normalDrinks = _normalRepository.GetDrinks();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
        {
            UpdateCocktailInShaker();
            Debug.Log(DrinkUtility.GetCocktailInfo(_cocktailShakerData.CurrentCocktail));
        }
        if (Input.GetKeyDown(KeyCode.E)) { ServeDrink(); Debug.Log("[CocktailSystemManager] Served via E key."); }
        if (Input.GetKeyDown(KeyCode.A)) SetSatisfactionPerfect();
        if (Input.GetKeyDown(KeyCode.S)) SetSatisfactionAcceptable();
        if (Input.GetKeyDown(KeyCode.D)) SetSatisfactionFail();
#endif
    }

    // ═════════════════════════════════════════════════════
    // Cocktail — Public API
    // ═════════════════════════════════════════════════════

    /// <summary>Picks a uniformly random cocktail as the current target.</summary>
    public S_Drink RandomCocktail()
    {
        _targetCocktail = _normalRepository.GetRandom();
        TargetCocktail = _targetCocktail;
        return _targetCocktail;
    }

    /// <summary>Picks a random cocktail of a specific type as the current target.</summary>
    public S_Drink RandomCocktail(TypeOfCocktail type)
    {
        _targetCocktail = _normalRepository.GetRandom(type);
        TargetCocktail = _targetCocktail;
        return _targetCocktail;
    }

    /// <summary>
    /// Calculates how well the shaker's current cocktail matches the target.
    /// Delegates comparison to DrinkUtility — no algorithm here.
    /// </summary>
    public Satisfaction CalculateSatisfaction()
        => DrinkUtility.CalculateSatisfaction(_cocktailShakerData.CurrentCocktail, _targetCocktail);

    public string GetTargetName() => _targetCocktail != null ? _targetCocktail.Name : string.Empty;

    /// <summary>
    /// Derives the identity of whatever is in the shaker and refreshes visuals.
    /// Used during debug (P key) and can be called externally.
    /// </summary>
    public void UpdateCocktailInShaker()
    {
        S_Drink current = _cocktailShakerData.CurrentCocktail;
        DrinkUtility.UpdateTypeOfAlcohol(current, _normalDrinks);
        DrinkUtility.UpdateName(current, _normalDrinks);
        DrinkUtility.UpdatePrice(current, _normalDrinks);

        Sprite sprite = DrinkUtility.GetCocktailSprite(current, _normalDrinks)
                        ?? _failCocktailSprite;
        _cocktailShaker.SetBTNSprite(sprite, sprite, sprite);
    }

    /// <summary>Editor / debug helper — picks a random target without returning it.</summary>
    public void RandomCocktailForDebug() => RandomCocktail();

    // ═════════════════════════════════════════════════════
    // Yarn Functions — called from .yarn scripts
    // ═════════════════════════════════════════════════════

    /// <summary>Returns the name of a random cocktail suited to the given NPC.</summary>
    [YarnFunction("Order_Cocktail_OutName")]
    public static string RandomOrderCocktail_OutName(int NPC)
    {
        var csm = FindAnyObjectByType<CocktailSystemManager>();
        //var data = FindAnyObjectByType<CharacterData>();
        
        return csm.ResolveOrderCocktail(NPC, out var drink) ? drink.Name : string.Empty;
    }

    /// <summary>Returns the description of a random cocktail suited to the given NPC.</summary>
    [YarnFunction("Order_Cocktail_OutDescription")]
    public static string RandomOrderCocktail_OutDescription(int NPC)
    {
        var csm = FindAnyObjectByType<CocktailSystemManager>();
        //var data = FindAnyObjectByType<CharacterData>();
        return csm.ResolveOrderCocktail(NPC, out var drink) ? drink.Description : string.Empty;
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

    // ═════════════════════════════════════════════════════
    // Task — Public API
    // ═════════════════════════════════════════════════════

    public void CompleteTask()
    {
        _myGameCondition = true;
        _dialogueRunner.VariableStorage.SetValue(TaskDoneVariableName, true);
    }

    public void SetSatisfaction(Satisfaction satisfaction) => _satisfaction = satisfaction;

    public void ServeDrink() => UpdateVariableInYarnTrigger();

    private void DebugServeDrink() => DebugUpdateVariableInYarnTrigger();

    // ═════════════════════════════════════════════════════
    // Yarn Commands — called from .yarn scripts
    // ═════════════════════════════════════════════════════

    /// <summary>
    /// &lt;&lt;wait_for_task CocktailSystemManager&gt;&gt;
    /// Suspends Yarn until UpdateVariableInYarn() returns true.
    /// Does NOT freeze Unity — the game keeps running normally.
    /// </summary>
    [YarnCommand("wait_for_task")]
    public IEnumerator WaitForTask()
    {
        EnableButtonInYarn(true);
        yield return new WaitUntil(() => UpdateVariableInYarn());
    }

    [YarnCommand("Can_End_Shift")]
    public void CanEndShift()
    {
        EnableButtonInYarn(false);
        _endShiftBTN.gameObject.SetActive(true);
        Debug.Log("[CocktailSystemManager] CanEndShift called.");
    }

    [YarnCommand("Enable_InteractableObject")]
    public void EnableButtonInYarn(bool enable)
    {
        _cocktailShaker.Interactable = enable;
        _cocktailShakerData.SetIngredientActive(enable);

        foreach (var btn in _cocktailShakerData.ingredientButtons)
        {
            if (btn == null) continue;
            if (btn.TryGetComponent<Interactable3DObject>(out var interactable)) interactable.Interactable = enable;
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
        _myGameCondition = false;
        _satisfaction = Satisfaction.None;
        _cocktailType = TypeOfCocktail.None;

        _dialogueRunner.VariableStorage.SetValue(SatisfactionVariableName, 0f);
        _dialogueRunner.VariableStorage.SetValue(TypeOfCocktailVariableName, 0f);
        _dialogueRunner.VariableStorage.SetValue(TaskDoneVariableName, false);

        Debug.Log("[CocktailSystemManager] All variables reset — ready for next loop.");
    }

    // ═════════════════════════════════════════════════════
    // Private Yarn Helpers
    // ═════════════════════════════════════════════════════

    private void UpdateVariableInYarnTrigger()
    {
        _myGameCondition = true;
        SetSatisfaction(CalculateSatisfaction());
        Debug.Log($"[CocktailSystemManager] Satisfaction → {_satisfaction}");
        DebugVariableFromYarn();
        UpdateVariableInYarn();
        DebugVariableFromYarn();
    }

    private void DebugUpdateVariableInYarnTrigger()
    {
        _myGameCondition = true;
        Debug.Log($"[CocktailSystemManager] (Debug) Satisfaction → {_satisfaction}");
        DebugVariableFromYarn();
        UpdateVariableInYarn();
        DebugVariableFromYarn();
    }

    /// <summary>
    /// Writes C# state into Yarn's VariableStorage.
    /// Returns true only when both game-condition and satisfaction are ready.
    /// Used as the WaitUntil predicate in WaitForTask().
    /// CRITICAL: enum values MUST be cast to (float)(int) — Yarn stores enums as integers.
    /// </summary>
    private bool UpdateVariableInYarn()
    {
        if (_myGameCondition && _satisfaction != Satisfaction.None)
        {
            EnableButtonInYarn(false);
            _dialogueRunner.VariableStorage.SetValue(SatisfactionVariableName, (float)(int)_satisfaction);
            _dialogueRunner.VariableStorage.SetValue(TypeOfCocktailVariableName, (float)(int)_cocktailType);
            _dialogueRunner.VariableStorage.SetValue(TaskDoneVariableName, true);
            return true;
        }
        return false;
    }

    // ═════════════════════════════════════════════════════
    // Debug
    // ═════════════════════════════════════════════════════

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
            + FormatEnumRow(TaskDoneVariableName, _myGameCondition.ToString(), yarnTaskDone.ToString(), yarnTaskDone.ToString())
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