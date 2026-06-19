using System.Collections.Generic;
using UnityEngine;

public static class SceneLoaderBridge
{
    public static string TargetScene;
    public static string ChapterNodeName;
    public static System.Action OnCompleteCallback;

    // ── Dialogue save/load ─────────────────────────────────
    /// <summary>Updated every time a line runs — read by SaveLoadManager on save.</summary>
    public static string CurrentLineId;

    /// <summary>Last line tagged #save_checkpoint — used when IsWaitingForTask is true.</summary>
    public static string CheckpointLineId;

    /// <summary>Set before StartDialogue on load; cleared by BubblePresenter when reached.</summary>
    public static string TargetLineId;

    /// <summary>True during silent replay walk-through. Commands, views, and option presenter check this.</summary>
    public static bool IsSilentReplay;

    // ── Node tracking (jump vs detour) ─────────────────────
    /// <summary>
    /// The node that started the current dialogue segment.
    /// Updated on fresh starts and <<jump>> (where onNodeComplete fires before onNodeStart).
    /// NOT updated on <<detour>> (parent node is still running).
    /// Always use this — not dialogueRunner.Dialogue.CurrentNode — when saving ChapterName.
    /// </summary>
    public static string DialogueRootNode;

    /// <summary>True while inside a <<detour>> sub-node.</summary>
    public static bool IsInDetour;

    // ── Option choice tracking ─────────────────────────────
    /// <summary>
    /// Accumulates DialogueOptionIDs chosen since the current root node started.
    /// Cleared only on root node start (jump / fresh start) — NOT on detour node start,
    /// so choices from the parent node survive across detours.
    /// </summary>
    public static List<int> SessionOptionChoices = new List<int>();

    /// <summary>
    /// Populated from SaveMetaData.ReplayOptionChoices before StartDialogue fires on load.
    /// OptionPresenterType2 dequeues from here during silent replay to auto-select options.
    /// </summary>
    public static Queue<int> ReplayOptionQueue = new Queue<int>();
}