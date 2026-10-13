// ============================================================
//  TalkToCustomerManager.cs — Everything about the people at the
//  bar that is not a drink: who is standing where, what their face
//  and gaze are, and the Yarn commands that move them.
//
//  Sits on the object the NPCs live under (NPCs_And_Set). It owns the
//  NPC lookup the Yarn commands used to keep in a static dictionary on
//  NPC_Base, and the snapshot / restore the save system uses.
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

    private readonly Dictionary<NPC_Name, NPC_Base> _byName = new Dictionary<NPC_Name, NPC_Base>();

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
