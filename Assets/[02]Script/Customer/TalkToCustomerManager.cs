// ============================================================
//  TalkToCustomerManager.cs — Everything about the people at the
//  bar that is not a drink: who is standing where, what their face
//  and gaze are, and the Yarn commands that move them.
//
//  Sits on the object the characters live under (NPCs_And_Set). This
//  file: the lookup of CharacterViews by name, the dialogue's
//  variables (the only writer of $task_done, $satisfaction,
//  $type_of_cocktail and $rel_*) and who likes which drink.
//
//  The characters' Yarn commands, spots and save snapshot are in
//  TalkToCustomerManager.Characters.cs (same partial class).
//
//  It does not know the cocktail: a scored order is handed in
//  (CommitOrderResult) by the bridge that listens to the cocktail.
//
//  The Yarn command names are a contract with the .yarn files and do not
//  change. They are static (Yarn calls them with no target object) and
//  reach the live manager through Current, the same scene-scoped
//  handle N_PlacementSystem and GameFlowCommands use.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using static E_Cocktail;

public partial class TalkToCustomerManager : MonoBehaviour
{
    /// <summary>The manager of the loaded scene, or null. Set in Awake, cleared in OnDestroy.</summary>
    public static TalkToCustomerManager Current { get; private set; }

    [Header("Dialogue")]
    [SerializeField] private DialogueRunner _dialogueRunner;

    [Header("Customer preferences")]
    [Tooltip("Preferred source (GDD section 19.1). Falls back to the CharacterData component on this object when empty.")]
    [SerializeField] private SO_CustomerRoster _customerRoster;

    private readonly Dictionary<CharacterName, CharacterView> _byName = new Dictionary<CharacterName, CharacterView>();

    private CharacterData _characterData;
    private YarnVariableSync _variables;
    private DrinkOrderContext _lastCommitted;

    /// <summary>Lazily built so it survives domain reloads and Awake ordering.</summary>
    private YarnVariableSync Variables => _variables ??= new YarnVariableSync(_dialogueRunner);

    /// <summary>
    /// Who likes which drink: the roster asset when assigned, otherwise the legacy
    /// <see cref="CharacterData"/> component on this object. Null when neither exists.
    /// </summary>
    public ICustomerPreferences Preferences
    {
        get
        {
            if (_customerRoster != null) return _customerRoster;
            if (_characterData == null) _characterData = GetComponent<CharacterData>();
            return _characterData;
        }
    }

    private void Awake()
    {
        if (Current != null && Current != this)
        {
            Debug.LogWarning($"[TalkToCustomerManager] A second manager on '{name}' is being ignored; commands stay bound to '{Current.name}'.", this);
            return;
        }

        Current = this;

        // Every character under this object, by the name dialogue and saves use. A view left at None
        // (a placeholder) is not addressable and is skipped.
        foreach (var view in GetComponentsInChildren<CharacterView>(true))
        {
            if (view.Name == CharacterName.None) continue;

            if (_byName.ContainsKey(view.Name))
                Debug.LogWarning($"[TalkToCustomerManager] Two characters are named {view.Name}; '{view.name}' is ignored.", view);
            else
                _byName[view.Name] = view;
        }

        ApplyIdleBob();
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    // ── Story ──────────────────────────────────────────────

    /// <summary>The current day (Yarn <c>$day</c>).</summary>
    public int CurrentDay => Variables.ReadDay();

    /// <summary>Moves <c>$day</c> on by one. Called when the bar closes into the next BeforeOpen.</summary>
    public void AdvanceDay() => Variables.WriteDay(CurrentDay + 1);

    /// <summary>
    /// Starts <paramref name="node"/> (the runner's start node when empty). Used instead of the
    /// runner's own auto start so the story waits for the game flow (BeforeOpen). When dialogue is
    /// still running (the old day called &lt;&lt;flow_next_day&gt;&gt;), the node starts once it ends.
    /// </summary>
    public void StartStory(string node = null)
    {
        if (_dialogueRunner == null) return;

        if (string.IsNullOrEmpty(node)) node = _dialogueRunner.startNode;
        if (!_dialogueRunner.Dialogue.NodeExists(node))
        {
            Debug.LogError($"[TalkToCustomerManager] Yarn node '{node}' does not exist — story not started.", this);
            return;
        }

        if (_dialogueRunner.IsDialogueRunning) StartCoroutine(StartWhenIdle(node));
        else _dialogueRunner.StartDialogue(node);
    }

    private IEnumerator StartWhenIdle(string node)
    {
        yield return new WaitWhile(() => _dialogueRunner.IsDialogueRunning);
        _dialogueRunner.StartDialogue(node);
    }

    // ── Dialogue variables ─────────────────────────────────

    /// <summary>
    /// Writes a scored order into the dialogue ($satisfaction, $type_of_cocktail, $task_done) and
    /// applies its relationship change. Called once per order, when the drink was scored.
    /// </summary>
    public void CommitOrderResult(DrinkOrderContext order)
    {
        _lastCommitted = order;
        Variables.WriteResult(order);
        Variables.ApplyRelationshipDelta(order.Customer, order.RelationshipDelta);
    }

    /// <summary>Back to the start of a customer loop: satisfaction, served type and task flag.</summary>
    public void ResetVariables() => Variables.Reset();

    /// <summary>Prints the tracked dialogue variables next to the last order this manager committed.</summary>
    [ContextMenu("Debug Yarn Variables")]
    public void DebugVariables()
    {
        if (!Variables.IsUsable)
        {
            Debug.LogWarning("[TalkToCustomerManager] Cannot debug: DialogueRunner or VariableStorage is null.", this);
            return;
        }

        float satisfactionRaw = Variables.ReadRaw(YarnVariableSync.SatisfactionVariableName);
        float cocktailRaw = Variables.ReadRaw(YarnVariableSync.TypeOfCocktailVariableName);

        string satisfactionName = System.Enum.IsDefined(typeof(Satisfaction), (int)satisfactionRaw)
            ? ((Satisfaction)(int)satisfactionRaw).ToString() : $"UNKNOWN({satisfactionRaw})";
        string cocktailName = System.Enum.IsDefined(typeof(TypeOfCocktail), (int)cocktailRaw)
            ? ((TypeOfCocktail)(int)cocktailRaw).ToString() : $"UNKNOWN({cocktailRaw})";

        string result = _lastCommitted != null ? _lastCommitted.Result.ToString() : "(none committed)";
        string served = _lastCommitted != null ? _lastCommitted.ServedType.ToString() : "(none committed)";
        string sep = new string('-', 72);

        Debug.Log("\n" + sep + "\n  [TalkToCustomerManager] Variable Debug Snapshot\n" + sep
                  + Row(YarnVariableSync.SatisfactionVariableName, result, satisfactionRaw, satisfactionName)
                  + Row(YarnVariableSync.TypeOfCocktailVariableName, served, cocktailRaw, cocktailName)
                  + "\n" + sep);
    }

    private static string Row(string variable, string csValue, float yarnRaw, string yarnName)
        => string.Format("\n  {0,-26} {1,-20} {2,-8} {3,-20} {4}", variable, csValue, yarnRaw.ToString("0"), yarnName,
                         string.Equals(csValue, yarnName, System.StringComparison.Ordinal) ? "OK" : "MISMATCH <--");

    // ── Lookup ─────────────────────────────────────────────

    /// <summary>The character with this name, or null.</summary>
    public CharacterView Find(CharacterName name) => _byName.TryGetValue(name, out var view) ? view : null;

    /// <summary>
    /// Resolves the spelling dialogue uses: "Walter" or "CharacterName.Walter". Warns and returns
    /// null for a name that is not a character, so a typo in a .yarn file shows up in the console.
    /// </summary>
    public CharacterView Find(string nameStr)
    {
        string raw = nameStr.Contains(".") ? nameStr.Split('.')[1] : nameStr;
        if (!System.Enum.TryParse(raw, true, out CharacterName target) || target == CharacterName.None)
        {
            Debug.LogWarning($"[FindCharacter] Invalid character name: '{nameStr}'");
            return null;
        }

        var view = Find(target);
        if (view == null) Debug.LogWarning($"[FindCharacter] No CharacterView named {target} found in scene.");
        return view;
    }
}
