// ============================================================
//  TalkToCustomerManager.cs — Everything about the people at the
//  bar that is not a drink: who is standing where, what their face
//  and gaze are, and the Yarn commands that move them.
//
//  Sits on the object the NPCs live under (NPCs_And_Set). It owns the
//  NPC lookup the Yarn commands used to keep in a static dictionary on
//  NPC_Base, the snapshot / restore the save system uses, the dialogue's
//  variables (the only writer of $task_done, $satisfaction,
//  $type_of_cocktail and $rel_*) and who likes which drink.
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

public class TalkToCustomerManager : MonoBehaviour
{
    /// <summary>The manager of the loaded scene, or null. Set in Awake, cleared in OnDestroy.</summary>
    public static TalkToCustomerManager Current { get; private set; }

    [Header("Dialogue")]
    [SerializeField] private DialogueRunner _dialogueRunner;

    [Header("Customer preferences")]
    [Tooltip("Preferred source (GDD section 19.1). Falls back to the CharacterData component on this object when empty.")]
    [SerializeField] private SO_CustomerRoster _customerRoster;

    private readonly Dictionary<NPC_Name, NPC_Base> _byName = new Dictionary<NPC_Name, NPC_Base>();

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

        // Every NPC under this object, by the name dialogue and saves use. An NPC left at None
        // (a placeholder) is not addressable and is skipped.
        foreach (var npc in GetComponentsInChildren<NPC_Base>(true))
        {
            if (npc.Name == NPC_Name.None) continue;

            if (_byName.ContainsKey(npc.Name))
                Debug.LogWarning($"[TalkToCustomerManager] Two NPCs are named {npc.Name}; '{npc.name}' is ignored.", npc);
            else
                _byName[npc.Name] = npc;
        }
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

    /// <summary>The NPC with this name, or null.</summary>
    public NPC_Base Find(NPC_Name name) => _byName.TryGetValue(name, out var npc) ? npc : null;

    /// <summary>
    /// Resolves the spelling dialogue uses: "Walter" or "NPC_Name.Walter". Warns and returns
    /// null for a name that is not an NPC, so a typo in a .yarn file shows up in the console.
    /// </summary>
    public NPC_Base Find(string npcNameStr)
    {
        string raw = npcNameStr.Contains(".") ? npcNameStr.Split('.')[1] : npcNameStr;
        if (!System.Enum.TryParse(raw, true, out NPC_Name target) || target == NPC_Name.None)
        {
            Debug.LogWarning($"[FindNPC] Invalid NPC name: '{npcNameStr}'");
            return null;
        }

        var npc = Find(target);
        if (npc == null) Debug.LogWarning($"[FindNPC] No NPC with NPCName.{target} found in scene.");
        return npc;
    }

    // ── Save / load ────────────────────────────────────────

    /// <summary>Where every NPC is, for the save system.</summary>
    public Dictionary<NPC_Name, NPCData> Snapshot()
    {
        var snapshot = new Dictionary<NPC_Name, NPCData>();

        foreach (var pair in _byName)
        {
            var npc = pair.Value;
            snapshot[pair.Key] = new NPCData
            {
                position = npc.transform.position,
                rotation = npc.transform.rotation,
                CurrentWayPointIndex = npc.CurrentWaypointIndex,
                CurrentLookDirection = npc.CurrentLookDirection
            };
        }

        return snapshot;
    }

    /// <summary>Puts NPCs back where <see cref="Snapshot"/> found them. Names with no NPC are skipped.</summary>
    public void Restore(IReadOnlyDictionary<NPC_Name, NPCData> saved)
    {
        foreach (var pair in saved)
        {
            var npc = Find(pair.Key);
            if (npc == null) continue;

            var state = pair.Value;
            npc.transform.position = state.position;
            npc.transform.rotation = state.rotation;
            npc.CurrentWaypointIndex = state.CurrentWayPointIndex;
            npc.CurrentLookDirection = state.CurrentLookDirection;

            // An NPC that never moved has no waypoint (-1) and is already where it was saved.
            if (state.CurrentWayPointIndex >= 0) npc.TeleportToWaypoint(state.CurrentWayPointIndex);
        }
    }

    // ── Yarn commands (names are a contract with the .yarn files) ──

    [YarnCommand("SetEmotion")]
    public static void SetEmotion(string npcNameStr, string emotion)
        => Current?.Find(npcNameStr)?.SetEmotion(emotion);

    [YarnCommand("SetLookDirection")]
    public static void SetLookDirection(string npcNameStr, string direction)
        => Current?.Find(npcNameStr)?.LookDirection(direction);

    [YarnCommand("MoveIn")]
    public static IEnumerator MoveIn(string npcNameStr, int waypointIndex)
    {
        if (SceneLoaderBridge.IsSilentReplay) yield break;
        yield return MoveTo(npcNameStr, waypointIndex);
    }

    [YarnCommand("MoveOut")]
    public static IEnumerator MoveOut(string npcNameStr)
    {
        if (SceneLoaderBridge.IsSilentReplay) yield break;
        yield return MoveTo(npcNameStr, 0);
    }

    private static IEnumerator MoveTo(string npcNameStr, int waypointIndex)
    {
        var npc = Current?.Find(npcNameStr);
        if (npc == null) yield break;

        // Runs on the NPC, as it always has, so the walk stops with the NPC.
        yield return npc.StartCoroutine(npc.MoveTo(waypointIndex));
    }
}
