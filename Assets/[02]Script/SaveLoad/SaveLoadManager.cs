using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using System;
using System.Collections;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor.Overlays;
#endif

public class SaveLoadManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueRunner dialogueRunner;

    public static SaveLoadManager Instance { get; private set; }

    private const int MAX_SLOTS = 6;
    private SaveData _saveData = new SaveData();
    private const string SAVE_FOLDER = "Saves";
    private const string SAVE_EXTENSION = ".json";

    private string SaveFolderPath => Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

    // ── Shared JSON settings: registers converters for Unity math types ────────
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        Converters = new List<JsonConverter> { new Vector3Converter(), new QuaternionConverter() }
    };

    // ── Yarn internals ─────────────────────────────────────────────────────────
    private VariableStorageBehaviour _storage;
    private Yarn.Program _program;

    // ── Scene reload / load state ──────────────────────────────────────────────
    // True after Start() runs for the first time — gates OnSceneLoaded re-init
    // so it doesn't fire redundantly on the very first scene load.
    private bool _hasInitialized = false;

    // Set by LoadFromFile(); consumed by OnSceneLoaded() once the new scene is ready.
    private bool _pendingLoad = false;

    // ──────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!Directory.Exists(SaveFolderPath))
            Directory.CreateDirectory(SaveFolderPath);
    }

    // Subscribe before any sceneLoaded event can fire.
    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start()
    {
        // First-time initialization only. Subsequent reloads are handled by OnSceneLoaded.
        InitializeReferences();
        _hasInitialized = true;
        Debug.Log("[SaveLoadManager] Ready.");
    }

    /// <summary>
    /// Called by Unity after EVERY scene load — including LoadingScene and the
    /// game scene after a reload.  This is the ONLY safe place to re-grab scene
    /// references and apply loaded data, because all Awake() calls in the new
    /// scene are guaranteed to have finished before this fires.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Skip the loading screen — its objects aren't what we care about.
        if (scene.name == "LoadingScene") return;

        // On the very first launch Start() handles init; skip the duplicate call.
        if (!_hasInitialized) return;

        // Re-grab DialogueRunner + storage from the freshly loaded scene.
        // The old references are now pointing at destroyed objects.
        InitializeReferences();

        // If a load was requested, apply the deserialized data now that
        // every Awake() in the new scene has already run.
        if (_pendingLoad)
        {
            _pendingLoad = false;
            ApplyYarnData();
            ApplyNPCData();
            Debug.Log("[SaveLoadManager] Pending load applied after scene ready.");
        }
    }

    /// <summary>Find the DialogueRunner in the active scene and cache its internals.</summary>
    private void InitializeReferences()
    {
        dialogueRunner = FindFirstObjectByType<DialogueRunner>();

        if (dialogueRunner == null)
        {
            Debug.LogError("[SaveLoadManager] ❌ No DialogueRunner found in scene!");
            return;
        }

        _storage = dialogueRunner.VariableStorage;
        CacheProgram();

        // Track node transitions to distinguish <<jump>> (new root) from <<detour>> (sub-node).
        // See OnNodeStart / OnNodeComplete / OnDialogueComplete for the logic.
        dialogueRunner.onNodeStart.AddListener(OnNodeStart);
        dialogueRunner.onNodeComplete.AddListener(OnNodeComplete);
        dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);

        // Reset tracking state for the new scene's runner instance.
        _lastNodeCompleted = true;
        _detourDepth = 0;
    }

    // ── Node tracking: jump vs detour ──────────────────────────────────────────
    // With <<jump>>:   onNodeComplete(A) fires → onNodeStart(B) fires.  A completed first.
    // With <<detour>>: onNodeStart(B) fires while A is still running.   A has NOT completed.
    // The _lastNodeCompleted flag captures exactly this difference.

    private bool _lastNodeCompleted = true; // true at startup = "no previous node"
    private int _detourDepth = 0;

    private void OnNodeStart(string nodeName)
    {
        if (_lastNodeCompleted)
        {
            // <<jump>> or fresh StartDialogue — this is a new root segment.
            _detourDepth = 0;
            SceneLoaderBridge.DialogueRootNode = nodeName;
            SceneLoaderBridge.SessionOptionChoices.Clear(); // choices reset per root, not per detour
            SceneLoaderBridge.IsInDetour = false;
        }
        else
        {
            // <<detour>> — parent node is still running; don't update root or clear choices.
            _detourDepth++;
            SceneLoaderBridge.IsInDetour = true;
        }
        _lastNodeCompleted = false;
    }

    private void OnNodeComplete(string nodeName)
    {
        if (_detourDepth > 0)
        {
            // Returning from a detour — parent node resumes, depth decreases.
            _detourDepth--;
            SceneLoaderBridge.IsInDetour = _detourDepth > 0;
            // Do NOT set _lastNodeCompleted — the parent is still running.
        }
        else
        {
            // Normal node end (or root node end after all detours returned).
            _lastNodeCompleted = true;
        }
    }

    private void OnDialogueComplete()
    {
        // Dialogue ended cleanly — reset for the next StartDialogue call.
        _lastNodeCompleted = true;
        _detourDepth = 0;
        SceneLoaderBridge.IsInDetour = false;
    }

    #endregion

    // ──────────────────────────────────────────────────────────────────────────
    #region Public Save / Load API

    public void SaveToFile(int slot)
    {
        CollectYarnData();
        CollectNPCData();

        _saveData.MetaData.Slot = slot;
        _saveData.MetaData.SaveName = $"Save {slot + 1}";
        _saveData.MetaData.Timestamp = DateTime.Now.ToString("MMM d, yyyy  HH:mm");
        _saveData.MetaData.PlaytimeSeconds = PlaytimeTracker.TotalSeconds;
        _saveData.MetaData.PlaytimeFormatted = PlaytimeTracker.Formatted();
        _saveData.MetaData.ChapterName = SceneLoaderBridge.DialogueRootNode; // root, not CurrentNode — survives <<detour>>
        _saveData.MetaData.LastLineId = CocktailSystemManager.IsWaitingForTask
            ? SceneLoaderBridge.CheckpointLineId   // rewind before task on load
            : SceneLoaderBridge.CurrentLineId;
        _saveData.MetaData.ReplayOptionChoices = new List<int>(SceneLoaderBridge.SessionOptionChoices);
        _saveData.MetaData.IsEmpty = false;

        StartCoroutine(CaptureAndSave(_saveData, slot));
    }

    public void LoadFromFile(int slot)
    {
        string path = GetFilePath(slot);
        if (!File.Exists(path)) return;

        try
        {
            _saveData = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(path), JsonSettings);
            PlaytimeTracker.SetAccumulated(_saveData.MetaData.PlaytimeSeconds);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadManager] Load failed: {e.Message}");
            return;
        }

        // Store the target node for the Yarn runner to pick up in OnSceneLoaded.
        SceneLoaderBridge.ChapterNodeName = _saveData.MetaData.ChapterName;

        // Flag that data is waiting — OnSceneLoaded will apply it once
        // the new scene's Awake() calls have all finished.
        _pendingLoad = true;

        FindAnyObjectByType<SceneLoader>()?.ReloadCurrentScene();
    }

    public SaveMetaData[] GetAllSlotsMeta()
    {
        var result = new SaveMetaData[MAX_SLOTS];

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string path = GetFilePath(i);

            if (!File.Exists(path))
            {
                result[i] = new SaveMetaData { Slot = i, IsEmpty = true };
                continue;
            }

            try
            {
                result[i] = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(path), JsonSettings).MetaData;
            }
            catch
            {
                result[i] = new SaveMetaData { Slot = i, IsEmpty = true };
            }
        }

        return result;
    }

    public void DeleteSave(int slot)
    {
        string path = GetFilePath(slot);
        if (File.Exists(path)) File.Delete(path);
    }

    #endregion

    // ──────────────────────────────────────────────────────────────────────────
    #region Private Helpers

    private string GetFilePath(int slot) =>
        Path.Combine(SaveFolderPath, $"save_slot_{slot}{SAVE_EXTENSION}");

    private IEnumerator CaptureAndSave(SaveData data, int slot)
    {
        yield return new WaitForEndOfFrame();

        File.WriteAllText(GetFilePath(slot), JsonConvert.SerializeObject(data, JsonSettings));

        Debug.Log($"[SaveLoadManager] Slot {slot} saved.");
    }

    #endregion

    // ──────────────────────────────────────────────────────────────────────────
    #region NPC Data

    /// <summary>Snapshot all NPC_Base instances in the scene into _saveData.</summary>
    public void CollectNPCData()
    {
        var npcs = FindObjectsByType<NPC_Base>(FindObjectsSortMode.None);

        foreach (var npc in npcs)
        {
            _saveData.NPCData.npcDataDict[npc.Name] = new NPCData
            {
                position = npc.transform.position,
                rotation = npc.transform.rotation,
                CurrentWayPointIndex = npc.CurrentWaypointIndex,
                CurrentLookDirection = npc.CurrentLookDirection
            };
        }
    }

    /// <summary>Apply loaded NPC data back onto scene objects.</summary>
    public void ApplyNPCData()
    {
        var npcs = FindObjectsByType<NPC_Base>(FindObjectsSortMode.None);

        foreach (var item in _saveData.NPCData.npcDataDict)
        {
            var npc = npcs.FirstOrDefault(n => n.Name == item.Key);
            if (npc == null) continue;

            npc.transform.position = item.Value.position;
            npc.transform.rotation = item.Value.rotation;
            npc.CurrentWaypointIndex = item.Value.CurrentWayPointIndex;
            npc.CurrentLookDirection = item.Value.CurrentLookDirection;
            npc.TeleportToWaypoint(npc.CurrentWaypointIndex);
        }
    }

    #endregion

    // ──────────────────────────────────────────────────────────────────────────
    #region Yarn Data

    /// <summary>Read all Yarn variables from VariableStorage into _saveData.</summary>
    public void CollectYarnData()
    {
        CacheProgram();

        foreach (var varName in _program.InitialValues.Keys)
            ResolveDateType(varName);

        Debug.Log("[SaveLoadManager] Yarn data collected.");
    }

    /// <summary>Push _saveData Yarn variables back into VariableStorage.</summary>
    public void ApplyYarnData()
    {
        if (_saveData.YarnData.BoolVariableInYarn.Count == 0 &&
            _saveData.YarnData.FloatVariableInYarn.Count == 0 &&
            _saveData.YarnData.StringVariableInYarn.Count == 0)
        {
            Debug.LogWarning("[SaveLoadManager] ApplyYarnData called but YarnData is empty.");
            return;
        }

        foreach (var item in _saveData.YarnData.BoolVariableInYarn)
            _storage.SetValue(item.Key, item.Value);

        foreach (var item in _saveData.YarnData.FloatVariableInYarn)
            _storage.SetValue(item.Key, item.Value);

        foreach (var item in _saveData.YarnData.StringVariableInYarn)
            _storage.SetValue(item.Key, item.Value);

        // ── Arm silent-replay BEFORE BubblePresenter.Start() fires ────────────
        // BubblePresenter.Start() calls StartDialogue(ChapterNodeName) — it runs
        // AFTER OnSceneLoaded, so these flags are already set when RunLineAsync
        // receives its first line.
        string targetLineId = _saveData.MetaData.LastLineId;
        if (!string.IsNullOrEmpty(targetLineId))
        {
            SceneLoaderBridge.TargetLineId = targetLineId;
            SceneLoaderBridge.IsSilentReplay = true;

            // Populate the option queue so OptionPresenterType2 can auto-select
            // the correct option at each branch point during replay.
            var choices = _saveData.MetaData.ReplayOptionChoices;
            SceneLoaderBridge.ReplayOptionQueue = choices != null && choices.Count > 0
                ? new Queue<int>(choices)
                : new Queue<int>();
        }
        // Do NOT call StartDialogue here — BubblePresenter.Start() owns that call.

        Debug.Log("[SaveLoadManager] Yarn data applied.");
    }

    #endregion

    // ──────────────────────────────────────────────────────────────────────────
    #region Helper Methods

    private void CacheProgram()
    {
        _program = typeof(Yarn.Dialogue)
            .GetProperty("Program",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(dialogueRunner.Dialogue) as Yarn.Program;

        if (_program == null)
            Debug.LogWarning("[SaveLoadManager] Could not cache Yarn Program via reflection.");
    }

    public void ResolveDateType(string varName)
    {
        if (_program == null || !_program.InitialValues.TryGetValue(varName, out var operand))
            return;

        switch (operand.ValueCase)
        {
            case Yarn.Operand.ValueOneofCase.BoolValue:
                _storage.TryGetValue<bool>(varName, out bool bVal);
                _saveData.YarnData.BoolVariableInYarn[varName] = bVal;
                break;

            case Yarn.Operand.ValueOneofCase.FloatValue:
                _storage.TryGetValue<float>(varName, out float fVal);
                _saveData.YarnData.FloatVariableInYarn[varName] = fVal;
                break;

            case Yarn.Operand.ValueOneofCase.StringValue:
                _storage.TryGetValue<string>(varName, out string sVal);
                _saveData.YarnData.StringVariableInYarn[varName] = sVal;
                break;
        }
    }

    #endregion

    // ──────────────────────────────────────────────────────────────────────────
    #region Yarn Commands

    [YarnCommand("save_chapter_node")]
    public void SaveChapterNode()
    {
        _saveData.MetaData.ChapterName = dialogueRunner.Dialogue.CurrentNode;
    }

    #endregion

    // ──────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    #region Editor Context Menus

    [ContextMenu("Debug Save to Slot 1")] public void DebugSaveToFile() => SaveToFile(0);
    [ContextMenu("Debug Save to Slot 2")] public void DebugSaveToFile2() => SaveToFile(1);
    [ContextMenu("Debug Save to Slot 3")] public void DebugSaveToFile3() => SaveToFile(2);
    [ContextMenu("Debug Save to Slot 4")] public void DebugSaveToFile4() => SaveToFile(3);
    [ContextMenu("Debug Save to Slot 5")] public void DebugSaveToFile5() => SaveToFile(4);
    [ContextMenu("Debug Save to Slot 6")] public void DebugSaveToFile6() => SaveToFile(5);

    [ContextMenu("Debug Load from Slot 1")] public void DebugLoadFromFile() => LoadFromFile(0);
    [ContextMenu("Debug Load from Slot 2")] public void DebugLoadFromFile2() => LoadFromFile(1);
    [ContextMenu("Debug Load from Slot 3")] public void DebugLoadFromFile3() => LoadFromFile(2);
    [ContextMenu("Debug Load from Slot 4")] public void DebugLoadFromFile4() => LoadFromFile(3);
    [ContextMenu("Debug Load from Slot 5")] public void DebugLoadFromFile5() => LoadFromFile(4);
    [ContextMenu("Debug Load from Slot 6")] public void DebugLoadFromFile6() => LoadFromFile(5);


    [ContextMenu("Debug Collect Yarn Data")] public void DebugSaveYarnData() => CollectYarnData();
    [ContextMenu("Debug Apply Yarn Data")] public void DebugLoadYarnData() => ApplyYarnData();

    #endregion
#endif
}

// ─── Data containers ──────────────────────────────────────────────────────────

public class SaveData
{
    public SaveMetaData MetaData = new SaveMetaData();
    public SaveYarnData YarnData = new SaveYarnData();
    public SaveSettingData SettingData = new SaveSettingData();
    public SaveNPCData NPCData = new SaveNPCData();
}

// ─── Unity type JSON converters ───────────────────────────────────────────────

/// <summary>
/// Serializes Vector3 as { "x": 0, "y": 0, "z": 0 }, bypassing Unity's
/// computed properties (normalized, magnitude, etc.) that cause circular loops.
/// </summary>
public class Vector3Converter : JsonConverter<Vector3>
{
    public override void WriteJson(JsonWriter writer, Vector3 v, JsonSerializer s)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x"); writer.WriteValue(v.x);
        writer.WritePropertyName("y"); writer.WriteValue(v.y);
        writer.WritePropertyName("z"); writer.WriteValue(v.z);
        writer.WriteEndObject();
    }

    public override Vector3 ReadJson(JsonReader reader, Type t, Vector3 existing,
                                     bool hasExisting, JsonSerializer s)
    {
        var jo = JObject.Load(reader);
        return new Vector3(
            jo["x"].Value<float>(),
            jo["y"].Value<float>(),
            jo["z"].Value<float>());
    }
}

/// <summary>
/// Serializes Quaternion as { "x": 0, "y": 0, "z": 0, "w": 1 }.
/// </summary>
public class QuaternionConverter : JsonConverter<Quaternion>
{
    public override void WriteJson(JsonWriter writer, Quaternion q, JsonSerializer s)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x"); writer.WriteValue(q.x);
        writer.WritePropertyName("y"); writer.WriteValue(q.y);
        writer.WritePropertyName("z"); writer.WriteValue(q.z);
        writer.WritePropertyName("w"); writer.WriteValue(q.w);
        writer.WriteEndObject();
    }

    public override Quaternion ReadJson(JsonReader reader, Type t, Quaternion existing,
                                        bool hasExisting, JsonSerializer s)
    {
        var jo = JObject.Load(reader);
        return new Quaternion(
            jo["x"].Value<float>(),
            jo["y"].Value<float>(),
            jo["z"].Value<float>(),
            jo["w"].Value<float>());
    }
}