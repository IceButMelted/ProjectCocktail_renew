> **Superseded in part by [`Contracts_Final.md`](Contracts_Final.md) (2026-11-07): where they disagree, Contracts_Final wins.**

# Bar410 rewrite — G4 API: Save/Load + Input

Status: design contract, not compiled. Code examples only show signatures. "UNVERIFIED" items are collected in (d).
Locked inputs: Rewrite_Options §9 (1–25), §3.5, §3.6, §13, §14, §16; Refactor_Plan Phase 4; ADR 0002 + Amendment.

## 0. Facts checked in the repo (2026-11-07)

| Fact | Evidence | Consequence |
|---|---|---|
| Yarn 3.2.5 `VariableStorageBehaviour.GetAllVariables()` / `SetAllVariables(f,s,b,clear)` exist | `Packages/dev.yarnspinner.unity/Runtime/Storage/VariableStorageBehaviour.cs:89,95` | Drop the reflection on `Dialogue.Program.InitialValues` (`SaveLoadManager.CacheProgram`). With `clear:true`, unset variables fall back to the program defaults. |
| `DialogueRunner.onNodeStart/onNodeComplete/onDialogueComplete` exist; the order of node events on detour is decided inside `YarnSpinner.dll` | `DialogueRunner.cs:314-338,648,667` | The jump/detour root tracker in `SaveLoadManager.OnNodeStart/OnNodeComplete` (lines 141-186) can be reused **if** the event order holds (see d1). |
| Yarn `LineAdvancer` has an `InputMode.InputActions` | `Views/LineAdvancer.cs:90` | Dialogue advancing can be a thin `ILineAdvancerInput` over the Dialogue map (keeps the existing pause gate and the 0.25 s cooldown). |
| Input System 1.19.0, `activeInputHandler: 2`, `m_UpdateMode: 1` (dynamic update) | `ProjectSettings.asset:960`, `InputSystem.inputsettings.asset:16` | Input still updates at `timeScale 0`, so the Pause menu and the virtual cursor keep working. Keep `UpdateMode` dynamic. |
| Every build scene uses `InputSystemUIInputModule` with `DefaultInputActions` (guid `ca9f5fa9…`); no `StandaloneInputModule` found | grep of `MainMenu_V2`, `Out/MainMenu`, `LoadingScene`, `PauseScene`, `Text Animator Dialogue System.prefab` | The UI module only needs re-pointing to the new asset. `General/InputSystem_Actions.inputactions` (maps Player/UI/Cocktail) is used only by `InputSystemManager`/`BTN_2_5D` → delete it. |
| No Cinemachine input-axis controller / "Mouse X" in `CamController.prefab` | grep | Nothing in Cinemachine needs migrating. Look is `CameraController` only. |
| `VirtualMouseInput` adds a device named `VirtualMouse` (layout Mouse), moves it with `InputState.Change` using `InputState.currentTime` (real time), and clamps to `m_Canvas.pixelRect`; `HardwareCursorIfAvailable` warps the system mouse | `Library/PackageCache/com.unity.inputsystem@21a28c3a6c83/InputSystem/Plugins/UI/VirtualMouseInput.cs:284-330,420-480` | Use `SoftwareCursor`; filter out the `VirtualMouse` device when detecting the last-used device (see §2.4). |
| UI module default `pointerBehavior = SingleMouseOrPenButMultiTouchAndTrack` merges every Mouse (real + virtual) into one pointer | `ExtendedPointerEventData.cs:216-243` | uGUI clicks, PhysicsRaycaster pointer events and `RenderTextureClickRelay` should work with the virtual cursor without any changes (see d3). |
| API compatibility level 6 (.NET Standard 2.1) | `ProjectSettings.asset:958` | `File.Move(src,dst,overwrite)` is not available. Use `File.Replace` when the target exists, otherwise `File.Move`. |
| `N_InputManager.cs` is **not UTF-8** (Windows-1252 bytes in comments). Normal `grep -I` skips it | Read tool warning | It drives ingredient hover/drag in the main scene. Convert it to UTF-8 first (`iconv -f cp1252 -t utf-8`) in its own commit, then edit. |
| No slot-select UI exists. Save/Load is reachable only through `[ContextMenu]` | grep of `SaveToFile/LoadFromFile` callers | The slot UI is new work, not a migration. |
| **29 live legacy `Input.*` calls in 10 files**, plus `Mouse.current` ×2, `Keyboard.current` ×1, `InputSystemAvailability.GetKeyDown` 3 in `PauseAwareKeyCodes` + 4 in test `CustomLineAdvancer` (these already work under new input) | `grep -rna` (see §3) | The docs say "31". That count came from a `grep -I` that skipped the non-UTF-8 `N_InputManager` and included 4 commented lines in `TaskManager.cs` plus the header comment of `IInputProvider.cs`. §3 lists the real sites. |

Assemblies used below: `Bar410.Contracts` (owned by G1; interfaces + DTOs only, no MonoBehaviours; **references UnityEngine core** because `Awaitable`, `Vector2` and `Ray` are engine types; holds `DayPart` too), `Bar410.Save` (G4, refs Contracts + Newtonsoft), `Bar410.Input` (G4, refs Contracts + Unity.InputSystem + UnityEngine.UI + Unity.TextMeshPro), `Bar410.Dialogue` (G2), `Bar410.Flow` (G1), `Bar410.Host` (G1 GameRoot).

---

## 1. Save / Load

### 1.0 Core model (read first)

- A **resume point** is the state at the moment line *N* of the current root was **presented**. A save stores the latest resume point, not the state at the moment Save was pressed. Reason: commands that run after line N (`<<set $x to $x+1>>`, `flow_open_bar`, `Switch_Camera`) run live again after load, so state captured at save time would apply them twice.
- **Replay-relevant state** is read by Yarn conditions/functions during replay: Yarn variables and `ingredient_unlocked()`. It is captured **twice**: at root start (restored *before* replay) and at the resume point (restored *at the target*). Everything else is restored only at the target. `unlock_ingredient` **runs** during replay (it is idempotent) and `ingredient_unlocked()` is **not** journaled, so the root-start unlock snapshot plus the live command reproduce the answer.
- **Timing of ISnapshotSource capture:** sources are captured when Save is pressed. Variables, DayPart and camera are captured when the line is shown. Every verb that mutates a snapshot source between two lines must therefore be **idempotent** when run again live after load: `CharacterEnter` on a present character = move/no-op, `order_customer`/`Order_*` overwrite, `unlock_ingredient` on an unlocked item = no-op, `Switch_Camera` to the current shot = no-op. Rule enforced by review (c) and asked of G3 (e).
- **Target line re-presentation:** at the target, line N is shown again. It must not be appended to the Event Log again, not counted as a new `LineIndex`, and not fire `LinePresented` for autosave (G2).
- **Leftover journal entries** after the target are discarded (from then on the live run records new entries).
- **Function return values and random picks are journaled.** Replay uses the journal value and never calls the function again (`Order_Cocktail_*` random pick, `order_*()`, `flow_phase()`/`flow_step()`). Decision: **journal + root-start snapshot**. Snapshots cover state, the journal covers calls.
- `LineIndex = 0` means "start the root fresh, no replay". Saving with no line shown yet is therefore valid (the day-start autosave relies on this).
- **Replay never awaits.** Every verb completes synchronously while replaying (G2 enforces this in `YarnVerbs`). The watchdog has a stall budget for the case where one doesn't.
- **Seam with G2:** G4 owns the DTOs, schema, files, eligibility, autosave and the resume orchestration. G2's `DialogueDirector` implements `IDialogueResumeSource` (capture side) and `IReplayEngine` (arm/run side), and calls G4's `ReplayWatchdog` (pure C#, **built by G4 only**).

### 1.1 `ISnapshotSource` (in `Bar410.Contracts`)
- **Responsibility:** a feature with restorable state exposes it as one serializable DTO.
- **Signature:**
```csharp
public enum SnapshotScope { AtTarget, RootStartAndTarget }
public interface ISnapshotSource {
    string Key { get; }                 // stable, lowercase: "characters","unlocks","order","camera","eventlog"
    SnapshotScope Scope { get; }
    Type StateType { get; }             // DTO type; plain fields, no UnityEngine.Object refs, no S_Drink refs (store ids)
    object Capture();                   // must return a NEW object (no aliasing of live lists)
    void Restore(object state);         // state is StateType or null (= key missing in file → reset to default)
}
```
- **Called by:** `SaveService`/`ResumeCoordinator`. **Implemented by:** G3 features (characters, unlocks, order, camera), G2 (event log). Registered by G1 `GameRoot`.
- **Replaces:** `SaveLoadManager.CollectNPCData/ApplyNPCData`, the direct calls to `EventLogPresenter.Snapshot`, `IngredientUnlocks.Current.Snapshot`.
- **Edge cases:** a duplicate `Key` → `SaveService.Register` throws in the editor. `Restore(null)` must not throw. An exception in one source is logged and the load continues with the other sources (it does not abort the load). A `Capture` that returns live references corrupts root-start snapshots.
- **Test:** EditMode, round-trip each DTO through `SaveSerializer` and compare it to the original.

### 1.2 `IDialogueResumeSource` (in Contracts; implemented by G2 `DialogueDirector`)
- **Responsibility:** gives the current resume point and the root-start state.
```csharp
public interface IDialogueResumeSource {
    bool HasRoot { get; }                       // a root node has started this scene
    DialogueResumeState CaptureResumePoint();   // copy of the state taken when the current LineIndex was presented
    event Action LinePresented;                 // after the line is shown (not during replay)
    event Action<string> RootStarted;           // jump or fresh StartDialogue; NOT detour; NOT raised while replaying. Before raising: RootStartVars = LineVars = current vars, LineIndex = 0, choices/journal cleared
}
```
- **Called by:** `SaveService`, `AutosaveScheduler`. **Replaces:** `SceneLoaderBridge.CurrentLineId/DialogueRootNode/SessionOptionChoices/CheckpointLineId`, `SaveLoadManager` node tracking.
- **Edge cases:** at a `#save_checkpoint` line, keep `CheckpointLineIndex`. With a drink in progress the save uses the latest line anyway (spec: rewind to the line before the order). `#save_checkpoint` is used only if G2 confirms a writer still needs it; otherwise drop it (YAGNI).

### 1.3 `IReplayEngine` (in Contracts; implemented by G2)
```csharp
public interface IReplayEngine {
    bool IsReplaying { get; }
    void Arm(DialogueResumeState target, ReplayWatchdog watchdog);   // before the root starts
    event Action ReachedTarget;      // raised synchronously BEFORE the target line presents
    event Action<ReplayAbort> Aborted;
}
public enum ReplayAbort { DialogueEnded, OptionQueueEmpty, UnexpectedJump, LineIdMismatch, LineBudget, StallBudget, JournalMismatch }
```
- **Called by:** `ResumeCoordinator`. **Edge cases:** the target line must not present until the `ReachedTarget` handlers have restored state (a synchronous invoke guarantees this). After `Aborted` the engine turns replay off and presents whatever comes next normally. It never leaves an empty screen.

### 1.4 `ReplayWatchdog` (G4-owned, file in `Bar410.Contracts`, pure C#)
- **Responsibility:** the only place that decides when replay has diverged or stalled.
```csharp
public sealed class ReplayWatchdog {
    public ReplayWatchdog(int targetIndex, string targetLineId, int extraLineBudget = 500, float stallSeconds = 5f);
    public enum Verdict { Continue, Target, Abort }
    public Verdict OnLine(int lineIndex, string lineId, out ReplayAbort reason);  // index==target && id==targetId → Target;
                                                                                  // index==target && id!=id → keep scanning for first id match up to budget
    public ReplayAbort? OnNodeStart(bool isJump);    // a jump during replay = divergence (the target is inside the root by definition)
    public ReplayAbort? OnOptionsWithEmptyQueue();
    public ReplayAbort? OnDialogueComplete();
    public ReplayAbort? OnJournalMiss(string verb);  // a verb wanted a journal entry and none / the wrong verb is next
    public ReplayAbort? Tick(float realtimeNow);     // G2 calls it each frame while replaying; aborts after stallSeconds with no OnLine/command progress
    public void Progress(float realtimeNow);         // G2 calls it on every line, option or command during replay
}
```
- **Called by:** G2 `DialogueDirector`. **Test:** EditMode table tests (exact hit, id mismatch then match, mismatch over budget, jump, empty queue, stall).

### 1.5 `DialogueResumeState`, `JournalEntry`, `YarnVars` (DTOs, Contracts)
```csharp
public sealed class YarnVars { public Dictionary<string,float> F = new(); public Dictionary<string,string> S = new(); public Dictionary<string,bool> B = new(); }
public sealed class JournalEntry {
    public int Seq; public string Verb;                    // "serve_order", "Order_Cocktail_Random", "unlock_ingredient", "flow_next_day", "fn:order_name"...
    public YarnVars Writes;                                // variables C# wrote (YarnVariableSync is the only writer)
    public string ResultJson;                              // function return / random pick (e.g. drink id); null if none
}
public sealed class DialogueResumeState {
    public string RootNode;
    public int LineIndex; public string LineId;            // 0/null = start root fresh
    public List<int> OptionChoices = new();                // since root start; not cleared by detour
    public List<JournalEntry> Journal = new();             // since root start
    public YarnVars RootStartVars, LineVars;               // root start / at line presentation
    public string DayPart; public string CameraShotId;     // at line presentation (G1/G3 values captured by G2 on LinePresented via delegates GameRoot supplies)
}
```
- **Edge cases:** `DayPart` is a string so that renaming the enum fails loudly (`Enum.TryParse` false → refuse the load with a message). Option IDs and line indexes are valid only against the same compiled Yarn program → `SaveMeta.YarnProgramHash` (see 1.7; owner decision in d).

### 1.6 `SaveData` (Bar410.Save)
```csharp
public sealed class SaveData {
    public const int CurrentVersion = 1;                   // bump on any schema change; no migration
    public int Version = CurrentVersion;
    public DialogueResumeState Dialogue;
    public Dictionary<string,string> Snapshots = new();    // Key → JSON of ISnapshotSource at save time (restored at target)
    public Dictionary<string,string> RootStartSnapshots = new();  // Key → JSON, RootStartAndTarget sources only; SaveService captures on RootStarted (ignored while replaying)
    public float PlaytimeSeconds;
}
```
- **Replaces:** `SaveLoadManager.SaveData`, `SaveMetaData`, `SaveYarnData`, `SaveSettingData` (deleted: settings stay in PlayerPrefs/settings, not in slots), `SaveNPCData/SaveUnlockData/SaveEventLogData` (become feature DTOs owned by the features).
- **Edge:** `EventLogEntry` changes to `{ string LineId; CharacterName Speaker; bool IsChoice; bool ShowName; }`. No text (G2 work, rule from §11.4).

### 1.7 `SaveMeta` + `SlotId`
```csharp
public enum SlotKind { Manual, Auto }
public readonly struct SlotId { public readonly SlotKind Kind; public readonly int Index; public string FileStem { get; } /* "slot_3" | "auto_1" */ }
public sealed class SaveMeta {
    public int Version; public string YarnProgramHash;
    public int Day; public string ChapterKey;              // localization key (chapter.<day>), never text
    public float PlaytimeSeconds; public long SavedUtcTicks;  // format at display time
    public string ThumbnailFile;                           // optional, null in v1
    public bool IsCompatible => Version == SaveData.CurrentVersion;
}
```

### 1.8 `SaveFileStore` (Bar410.Save, plain C#)
- **Responsibility:** atomic file I/O for slots. Nothing else touches `Saves/`.
```csharp
public sealed class SaveFileStore {
    public SaveFileStore(string folder);                   // Path.Combine(Application.persistentDataPath,"Saves")
    public IReadOnlyList<(SlotId id, SaveMeta meta)> ListMeta();     // reads *.meta.json only; meta without slot → skipped; slot without meta → meta=null ("damaged")
    public bool TryReadMeta(SlotId id, out SaveMeta meta);
    public bool TryRead(SlotId id, out SaveData data, out LoadError error);  // reads meta first: Version mismatch → error without deserializing slot
    public void Write(SlotId id, SaveData data, SaveMeta meta);      // slot first, then meta; each: write X.tmp → flush → Replace/Move
    public void Delete(SlotId id);                                    // slot + meta + thumb
}
public enum LoadError { None, Missing, VersionMismatch, ProgramMismatch, Corrupt }
```
- **Replaces:** `SaveLoadManager.GetFilePath/CaptureAndSave/GetAllSlotsMeta/DeleteSave`. Old `save_slot_N.json` files are ignored (different name). Old saves are discarded.
- **Edge cases:** crash between slot and meta → the old meta points at a new slot. Acceptable, because the slot carries its own `Version` and `TryRead` re-checks it. Disk full / IOException → the `.tmp` is deleted, the exception goes to the caller as `false`, and the UI shows "Save failed". Writes run on the main thread (files are small). Make it async only if profiling shows a hitch.
- **Test:** EditMode in a temp folder: write/read round trip, a pre-existing target, a meta with the wrong version, truncated JSON → `Corrupt`, a leftover `.tmp` ignored.

### 1.9 `SaveEligibility` (pure static)
```csharp
public enum SaveBlock { None, NoGame, BeforeOpenOrEvent, Replaying, Skipping, NoRoot, Loading }
public static class SaveEligibility {
    public static SaveBlock Check(in SaveContext c);   // first matching block wins, in enum order
    public static string MessageKey(SaveBlock b);      // "ui.save.blocked.<b>" for localization
}
public readonly struct SaveContext { public bool InGame, IsLoading, IsReplaying, IsSkipping, HasRoot; public string DayPart; public bool IsBartending; }
```
- Blocked when: DayPart ∈ {None, BeforeOpen, Event}, replaying, skipping, no root yet, scene loading. **Not** blocked while bartending: allowed with the warning key `ui.save.warn.drink_lost` (the UI shows it, Refactor Plan Phase 4).
- **Replaces:** the checks in `SaveLoadManager.SaveToFile` and the static `CocktailSystemManager.IsWaitingForTask`.
- **Test:** EditMode truth table.

### 1.10 `SaveService` (Bar410.Save, plain C#, no MonoBehaviour)
- **Responsibility:** captures a save, writes and lists slots, loads into `PendingLoad`. Works in MainMenu with no GameRoot.
```csharp
public sealed class SaveService {
    public SaveService(SaveFileStore store, Func<SaveContext> context = null, IDialogueResumeSource dialogue = null, IPlaytimeClock clock = null);
    public void Register(ISnapshotSource source);           // GameRoot only
    public SaveBlock CanSave();
    public bool TrySave(SlotId slot, out SaveBlock block);  // manual and auto; Capture → Serialize → store.Write
    public IReadOnlyList<(SlotId, SaveMeta)> ListSlots() => store.ListMeta();
    public SlotId? MostRecent();                            // "Continue": max SavedUtcTicks over Manual+Auto, compatible only
    public LoadError BeginLoad(SlotId slot);                // TryRead → PendingLoad.Set(data) → caller loads the main scene through the existing LoadingScene
    public void Delete(SlotId slot);
    internal (SaveData, SaveMeta) Capture();                // Dialogue.CaptureResumePoint + every source at target + playtime
}
public interface IPlaytimeClock { float TotalSeconds { get; } void SetAccumulated(float s); }  // adapter over the existing static PlaytimeTracker
```
- **Called by:** the slot-select UI (Pause and MainMenu), `AutosaveScheduler`, debug menu. **Constructed by:** G1 GameRoot in the main scene (full ctor). In MainMenu_V2 a tiny `MenuSaveHost` MonoBehaviour (G4, 10 lines) does `new SaveService(new SaveFileStore(path))` with no dialogue/context (load/list/delete only; `TrySave` returns `NoGame`). **Deps:** Contracts, Newtonsoft (`com.unity.nuget.newtonsoft-json` 3.2.2, already installed).
- **Replaces:** `SaveLoadManager` (whole file), `SceneLoaderBridge.ChapterNodeName/TargetLineId/IsSilentReplay/ReplayOptionQueue`, the `[SaveLoadSystem]` GameObject, the `DontDestroyOnLoad` singleton, `<<save_chapter_node>>` (G2 keeps a no-op shim that only warns).
- **ChapterKey/Day:** comes from a `Func<(int day,string chapterKey)>` supplied by GameRoot (G1 reads `SO_DaySchedule`).
- **Edge cases:** `TrySave` while `CanSave()!=None` → false, no file. Loading from in-game goes through `LoadingScene`, so the main scene reloads and GameRoot rebuilds everything (no in-place reset). `BeginLoad` on an incompatible slot → returns `VersionMismatch`, and the UI shows `ui.save.incompatible`.
- **Test:** EditMode with fake sources + fake dialogue: Capture/Restore order, a duplicate key, a missing key in the file → `Restore(null)`.

### 1.11 `PendingLoad` (static single-use handoff)
```csharp
public static class PendingLoad { public static void Set(SaveData d); public static bool TryTake(out SaveData d); public static void Clear(); }
```
- **Responsibility:** the only data that crosses a scene load. It is not a service. **Called by:** `SaveService.BeginLoad`, G1 GameRoot (`TryTake` in `Awake`), "New Game" (`Clear`). **Replaces:** `SceneLoaderBridge` dialogue fields. **Edge:** domain-reload-off Play mode → `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` clears it.

### 1.12 `ResumeCoordinator` (Bar410.Save, plain C#)
- **Responsibility:** runs the load protocol in the new scene.
```csharp
public sealed class ResumeCoordinator {
    public ResumeCoordinator(SaveService save, IReplayEngine replay, IVariableAccess vars, IDayResume day, IToast toast = null);
    public Awaitable ResumeAsync(SaveData data, CancellationToken ct);
}
public interface IVariableAccess { YarnVars GetAll(); void SetAll(YarnVars v); }   // G2 wraps VariableStorage.Get/SetAllVariables
public interface IDayResume { Awaitable ResumeAsync(DayPart part, string rootNode, CancellationToken ct); }  // G1 DayRunner; DayPart enum lives in Contracts
```
- **Protocol:**
  0. `Time.timeScale = 1` (a load from Pause reloads the scene with timeScale still 0, so DOTween fades and timed awaits would hang).
  1. `vars.SetAll(RootStartVars)`. Restore `SaveData.RootStartSnapshots` into the `RootStartAndTarget` sources. Playtime `SetAccumulated`.
  2. If `LineIndex > 0`: `replay.Arm(data.Dialogue, new ReplayWatchdog(...))`.
  3. `await day.ResumeAsync(part, RootNode, ct)`. G1 waits for `Panel_Transition`, sets the mode without Event/chapter title, then starts the root.
  4. On `ReachedTarget` (synchronous): `vars.SetAll(LineVars)`, restore every target snapshot (`Snapshots`, which include camera/order/characters/unlocks/eventlog). G1 applies `CameraShotId` through the camera snapshot source.
  5. On `Aborted(reason)`: do the same as step 4, plus `Debug.LogError` and a toast `ui.save.resume_inexact`.
  6. `LineIndex == 0`: skip step 2 and apply step 4 right after `RootStarted` (state equals the root start anyway, so only the non-variable snapshots matter).
- **Called by:** G1 GameRoot after `PendingLoad.TryTake`. **Replaces:** `SaveLoadManager.OnSceneLoaded/ApplyYarnData/_pendingLoad`.
- **Edge cases:** `DayPart` parse fails → refuse, toast, start a new day instead. `RootNode` missing from the program → same. A second load while replaying → impossible, because the scene reloads.
- **Test:** PlayMode `TEST_SaveLoad.yarn` (Refactor Plan 4d list) + EditMode with fakes for the event order.

### 1.13 `AutosaveScheduler` (Bar410.Save, plain C#)
- **Responsibility:** autosave on request, filled at the next eligible presented line. Rotates through N auto slots.
```csharp
public enum AutosaveReason { DayStart, AfterServe, EndOfDay }
public sealed class AutosaveScheduler : IDisposable {
    public AutosaveScheduler(SaveService save, IDialogueResumeSource dialogue, int rotation = 3);
    public void Request(AutosaveReason reason);   // G1 calls it: after Event ends (DayStart), after a Serve is scored (AfterServe), on close_bar (EndOfDay)
}
```
- **Rule:** a request is stored (the latest reason wins) and fulfilled at the next `LinePresented` where `CanSave()==None`. Reason: an immediate "after serve" save targets the line *before* `serve_order` and would make the player remake the drink after load. Exception: `DayStart` is fulfilled at `RootStarted` with `LineIndex 0` (valid by §1.0).
- **Rotation:** write to the auto slot with the oldest `SavedUtcTicks` (empty first).
- **Edge:** if a request is pending when the day ends with no further line, the next day's DayStart replaces it. **Owner note:** EndOfDay and the next day's DayStart are often a single line apart. Consider dropping EndOfDay (d, owner decision).
- **Test:** EditMode with a fake dialogue: request → no save until LinePresented, blocked line skipped, rotation order.

### 1.14 Slot-select UI contract (`SlotListView`, Bar410.Save.UI or G3 hosts; uGUI)
```csharp
public sealed class SlotListView : MonoBehaviour {
    public enum Mode { Save, Load }
    public void Open(SaveService service, Mode mode);  // builds rows from ListSlots(); 6 manual rows + auto rows (Load only)
    public event Action Closed;
}
```
- Rows show: chapter (`LocalizedString` from `ChapterKey`, so it follows the language live), day, playtime, local date from UTC ticks, plus "Incompatible"/"Damaged" states (not loadable, deletable).
- Controller: Focus mode, first row selected, South = save/load, X/Square = delete (with confirm), East = close. Overwrite and delete always confirm. If `CanSave()` blocks, show the message key and disable Save rows. While bartending, show `ui.save.warn.drink_lost` in the confirm dialog.
- Hosted in PauseScene (Save+Load) and MainMenu_V2 (Load + Continue button = `MostRecent()`).

---

## 2. Input

### 2.1 Action asset `Assets/Bar410/Input/Bar410Controls.inputactions` (+ generated C# `Bar410Controls`)
Control schemes: `KeyboardMouse`, `Gamepad` (Xbox/PS via the generic Gamepad layout).

| Map | Action (type) | Keyboard/Mouse | Gamepad | Notes |
|---|---|---|---|---|
| **UI** (always on) | Navigate (V2), Submit, Cancel, Point, Click, RightClick, MiddleClick, ScrollWheel | WASD/arrows, Enter, Esc, `<Pointer>/position`, LMB… | LS + D-pad, South, East | Fed to `InputSystemUIInputModule` (replaces DefaultInputActions). `Point`/`Click` include `<VirtualMouse>` through the `<Mouse>` path |
| | TabPrev/TabNext (Button) | Q / E | LB / RB | Settings tabs (`SettingWindowTabs`), book pages |
| | ScrollView (V2) | — | RS | Event Log / long panels in Focus mode |
| **Dialogue** | Advance (Button) | Space, Enter, (LMB on the dialogue box stays a uGUI button) | South | Replaces `PauseAwareKeyCodes` hurry/next lists. Disabled while options show |
| | AutoPlay, Skip (hold), OpenLog | A? / Ctrl (hold) / L? | X/Square, LT hold, View/Touchpad | **design input**: exact keys |
| **Gameplay** | Pause (Button) | Esc | Start/Options | Replaces `PauseManager.EscapePressed` |
| | Back (Button) | RMB? | East | Minigame cancel, Garnish back, close book |
| | CursorMove (V2) | — | LS | `VirtualMouseInput.stickAction` |
| | CursorPress / CursorAlt (Button) | — | South / West | `VirtualMouseInput.leftButton/rightButton` |
| | CameraLook (V2) | — | RS | Discrete views (§2.7) |
| | MinigameAction (Button) | LMB, Space | South, RT | §4 |
| | OpenBook | B | Y/Triangle | |
| | PointerDelta (V2, passthrough) | `<Mouse>/delta` | — | Last-device detection only |

Debug keys are **not** actions. They use `Keyboard.current` inside `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.

### 2.2 `InputService` (Bar410.Input, MonoBehaviour, scene-scoped)
- **Responsibility:** owns the action asset instance, map enabling, pointer mode stack, last-used device and the virtual cursor. One per scene (`[Input]` prefab in MainMenu_V2 and the main scene; PauseScene uses the main scene's).
```csharp
public enum DeviceKind { KeyboardMouse, Xbox, PlayStation }   // generic/unknown gamepad → Xbox glyphs
public enum PointerMode { Direct, Focus, Cursor }              // Direct = no focus, no cursor (dialogue, minigame play)
public sealed class InputService : MonoBehaviour, IPointerSource {
    public static InputService Current { get; }                // scene-scoped, duplicate destroys itself, cleared OnDestroy
    public Bar410Controls Controls { get; }
    public DeviceKind ActiveDevice { get; }  public bool IsGamepad => ActiveDevice != DeviceKind.KeyboardMouse;
    public event Action<DeviceKind> DeviceChanged;
    public PointerMode Mode { get; }  public event Action<PointerMode> ModeChanged;
    public IDisposable PushMode(PointerMode mode, GameObject firstSelected = null);  // stack; Dispose pops (any order safe: removes its own entry)
    public void SetMapEnabled(InputMapId map, bool on);         // Dialogue / Gameplay; UI never disabled
    // IPointerSource
    public Vector2 ScreenPosition { get; }  public bool PointerVisible { get; }
}
public enum InputMapId { Dialogue, Gameplay }
```
- **Mode effects:** `Cursor` → virtual cursor enabled (only when `IsGamepad`), `EventSystem.sendNavigationEvents=false`, selection cleared, so South does not also Submit. `Focus` → cursor hidden, navigation on, `firstSelected` selected (or restored). `Direct` → no cursor, navigation off, selection cleared. The real mouse works in every mode.
- **Pause:** `PauseManager` pushes `Focus` and disables Dialogue+Gameplay; Dispose restores them (replaces `PauseManager.IsPaused` checks in `PauseAwareKeyCodes`).
- **Why the static `Current`:** additive scenes (PauseScene, future minigame UI scene §15.6) and prefab widgets (`ButtonPrompt`) cannot take an Inspector reference across scenes. G1 may expose it as `GameRoot.Current.Input` instead. Same object either way.
- **Replaces:** `InputSystemManager`, the `Cocktail` map of `General/InputSystem_Actions.inputactions`, `PauseAwareKeyCodes`' key lists, `PauseManager.EscapePressed`.
- **Edge cases:** an unpaired gamepad plugged in mid-game → `DeviceChanged` on its first button. Device removed → fall back to KeyboardMouse. Mode stack empty → `Direct`. A missing Dispose (Station without finally) leaks the mode, so `PushMode` logs the stack depth > 4 as a warning.
- **Test:** PlayMode with `InputTestFixture` (package test utils): a gamepad press flips the device, the virtual mouse moving does **not** flip it, Push/Pop restores the mode and selection.

### 2.3 `IPointerSource` (in `Bar410.Contracts`, so features read the pointer without referencing `Bar410.Input`)
```csharp
public interface IPointerSource { Vector2 ScreenPosition { get; } bool PointerVisible { get; } }
public static class PointerRay { public static Ray From(IPointerSource p, Camera cam) => cam.ScreenPointToRay(p.ScreenPosition); }
```
- **Implementation:** reads `Controls.UI.Point.ReadValue<Vector2>()`. That action is bound to `<Pointer>/position`, so it carries whichever mouse (real or virtual) moved last (d3). `PointerVisible` = false in Focus/Direct with a gamepad (consumers stop hover logic).
- **Called by:** every former `Input.mousePosition`/`Mouse.current` reader (§3). **Test:** PlayMode, the virtual mouse moved by a test gamepad stick changes `ScreenPosition`.

### 2.4 `VirtualCursor` (Bar410.Input, wraps the package `VirtualMouseInput`)
- **Responsibility:** the stick-driven cursor in Cursor mode. A software cursor image on its own Overlay canvas, sorted below the Pause canvas (10000). Pause pushes Focus, which hides the cursor anyway.
```csharp
public sealed class VirtualCursor : MonoBehaviour {
    public void SetActive(bool on);            // InputService only; on → warp virtual mouse to the last real-mouse position; off → hide image
    public Vector2 Position { get; }
    [SerializeField] float speed = 1200f;      // calibration knob; tune with the owner on a real pad
}
```
- **Config:** `VirtualMouseInput.cursorMode = SoftwareCursor` (no system-mouse warp, so no device ping-pong). Actions: `Gameplay/CursorMove`, `CursorPress`, `CursorAlt`, `UI/ScrollWheel`←RS optional.
- **Device detection rule (InputService):** use `InputSystem.onActionChange` (Performed) → `action.activeControl.device`, plus the `PointerDelta` action above a 2 px threshold. **Ignore devices whose `name` starts with "VirtualMouse"**.
- **Edge cases:** canvas `pixelRect` clamp uses the canvas found by `TryFindCanvas`, so the cursor canvas must be Screen Space Overlay (d4). Speed scales with resolution via `CanvasScaler` (package does this). Cursor hidden when the mouse moves (real mouse wins until the pad is touched again).
- **Test:** PlayMode, the pad stick moves the cursor while time scale is 0. A press over a uGUI Button invokes it. A press over the Garnish RawImage reaches `RenderTextureClickRelay`. A press on a bottle reaches `DragableObject.OnPointerDown` (PhysicsRaycaster).

### 2.5 `InputGlyphs` (SO) + `ButtonPrompt` (MonoBehaviour)
```csharp
[CreateAssetMenu] public sealed class InputGlyphs : ScriptableObject {
    [Serializable] public sealed class Table { public DeviceKind device; public List<Entry> entries; }
    [Serializable] public struct Entry { public string controlPath; public Sprite sprite; }   // "buttonSouth","leftTrigger","escape","leftButton"
    public bool TryGet(InputAction action, DeviceKind device, out Sprite sprite);  // GetBindingDisplayString(group,out layout,out path) → table lookup
}
public sealed class ButtonPrompt : MonoBehaviour {                // placed next to a button: icon + localized label
    [SerializeField] InputActionReference action; [SerializeField] Image icon; [SerializeField] LocalizedString label; // label via G2 LocalizedText
    [SerializeField] bool hideOnKeyboardMouse;                    // e.g. hub prompts only when a pad is active
}
```
- **Called by:** UI prefabs (Hub, Pause, Minigame, Garnish, slot UI). Listens to `InputService.Current.DeviceChanged`. **Replaces:** nothing (new). **Edge:** missing sprite → show the text from `GetBindingDisplayString()` and warn once. PlayStation detection: `DualShockGamepad` base layout covers DS4/DualSense (d5). **Test:** EditMode `TryGet` per device for every action in the asset (catches missing art).

### 2.6 `FocusOnEnable` (focus helper, MonoBehaviour)
```csharp
public sealed class FocusOnEnable : MonoBehaviour {
    [SerializeField] Selectable first; [SerializeField] bool rememberLast = true;
    // OnEnable / on DeviceChanged→gamepad / when EventSystem selection becomes null or inactive: select last-or-first
}
```
- Put it on: every PausePanel, EventPopup (already focuses its close button, replace that), the slot list, dialogue options container, book, Garnish option rows in Focus fallback.
- **Feedback in Focus:** `ButtonScaleFeedback` and `HoverTooltip` also implement `ISelectHandler/IDeselectHandler` → same as hover (G3 change, listed in §3).
- `InputSystemUIInputModule.deselectOnBackgroundClick = false` in every scene so a mouse click on the background doesn't strand the pad.

### 2.7 Camera look contract (for G3 `CameraController`)
Today the camera does not rotate continuously. It switches discrete views (`Forward/Left/Right/Down`) when the mouse sits at a screen edge for `hoverDelayDuration`.
- **Keyboard/mouse:** edge-hover stays, reading `IPointerSource`.
- **Gamepad:** edge-hover **off**, otherwise the virtual cursor at an edge would rotate the camera. RS past 0.6 for `hoverDelay` → that view. RS in the opposite direction → Forward. **Design input:** snap vs hold-to-peek.
- Gate: G1 still enables this only in AddIngredient while the shaker is not full (ADR 0002 Amendment).
- Also flag: `CameraController.SetCanRotateCamera` writes into the SO asset at runtime (persists in the Editor). Fix it in the same task.

---

## 3. Migration of call sites (grep -rna, first-party only)

| File (scene/prefab use) | Lines | Today | Replacement | Exec |
|---|---|---|---|---|
| `Minigame/IInputProvider.cs` (all minigames) | 48 | `Input.GetMouseButtonDown(0)` | `ActionInputProvider(InputAction a)` → `IsClickedThisFrame = a.WasPressedThisFrame()` on `Gameplay/MinigameAction`. `BaseMiniGame.Initialize` gets it injected (no `new UnityInputProvider()` default) | haiku |
| `Minigame/MinigameSystemManager.cs` (editor only) | 223×2, 225-228 | Shift+1/2/B/V | `Keyboard.current` (shiftKey, digit1Key…) inside `#if UNITY_EDITOR` | haiku |
| `Hierarchical State Machine/GameFlowDebugHotkeys.cs` (`[GameLoop]`) | 26×2, 28-37 | debug flow keys | deleted with the HSM. G1's new debug hotkeys use `Keyboard.current` | (G1) |
| `Demo_KeysShortCut.cs` (`[GameLoop]`, main scene) | 15 | R key | delete after checking what R does (remove the component from the prefab) | haiku |
| `Debug Script/DebugPhysicsRaycaster.cs` (`CamController`, `[GameLoop]` prefabs) | 94, 115, 128 | F9 + mousePosition | `Keyboard.current[_debugKey]` (change field to `Key`), `IPointerSource` | haiku |
| `PlacementSystem/N_InputManager.cs` (`[GameLoop]`, main scene) **non-UTF-8** | 75 | `Input.mousePosition` in `MouseRay()` | inject `IPointerSource`; `MouseRay() => PointerRay.From(_pointer, _sceneCamera)`. Convert encoding first | sonnet |
| `BaseInteractable/HoverTooltip.cs` (ingredient prefabs) | 239 | `Input.mousePosition` | `InputService.Current.ScreenPosition`. When a pad is in Focus mode, anchor to the selected object's screen rect (+ `ISelectHandler`) | sonnet |
| `CameraControl/CameraController.cs` (`CamController`, `[GameLoop]`) | 201 | `Mouse.current.position` | §2.7 | sonnet |
| `MainMenu/MenuParallaxLook.cs` (MainMenu_V2) | 24 | `Mouse.current.position` | `IPointerSource`. With a pad in Focus mode, ease to center (or RS; design) | haiku |
| `UI Script/UIParallaxMouse.cs` (no scene use found) | 41 | `Input.mousePosition` | delete if still unused after a check of `[01]Art`/`[04]Prefab` UI prefabs, else `IPointerSource` | haiku |
| `Pause/PauseManager.cs` (PauseManager) | 184, 186 | `Keyboard.escapeKey` / `Input.GetKeyDown(Escape)` | `Gameplay/Pause` opens/closes; `UI/Cancel` closes the top sub-panel (`PausePanel.CloseTopSub`). Push Focus on open | sonnet |
| `[06]Dialogue/PauseAwareKeyCodes.cs` (`Line Advancer` in dialogue prefab) | 33, 34, 49 | `InputSystemAvailability.GetKeyDown(KeyCode)` | rename → `DialogueAdvanceInput : ILineAdvancerInput` reading `Dialogue/Advance` (cooldown kept). Pause gate = map disabled by InputService | sonnet (G2 owns presenter, G4 writes the adapter) |
| `BaseInteractable/BTN_2_5D.cs` + `PlacementSystem/InputSystemManager.cs` (`MakingCocktail.prefab` only; `InputSystemManager` in no scene) | — | old `Cocktail` map refs; `Awake` would NRE | delete both + `General/InputSystem_Actions.inputactions` after a "find references" check that `MakingCocktail.prefab` isn't in the main scene | haiku |
| `[00]Testing Yarn And Learning/CustomLineAdvancer.cs`, `YarnVariableDebugger.cs`, `TaskManager.cs` (commented) | 60,363-375 / 60-61 | legacy keys | delete the folder's scripts (test-only), or move them out of the build | haiku |
| Not input, but needed for the cursor: `ButtonScaleFeedback.cs`, `HoverTooltip.cs` | — | hover only | add `ISelectHandler/IDeselectHandler` | haiku |

**Gate for `activeInputHandler → 1` (new only):** `grep -rna --include=*.cs -E "\bInput\.(Get|mouse|any|touch)" Assets | grep -v -E "^Assets/(Plugins|TextMesh Pro|TableForge|CocktailCrafter_V2|\[09\])"` returns nothing, **and** a play-through of every build scene (`Out/MainMenu` index 0, `MainMenu_V2`, `LoadingScene`, main, `PauseScene`) with no `InvalidOperationException`. A clean compile proves nothing here, because legacy calls only throw at runtime. Also remove the second EventSystem in the main scene before testing the cursor.

## 4. Minigame input mapping

| Game | Code | Needs from input | Controller mapping | Flags |
|---|---|---|---|---|
| Shaking (`ShakingMinigame.OnTick`) | `Input.IsClickedThisFrame` → gauge += per click, decays over time; stay in zone | a "pressed this frame" only | `MinigameAction`: South **or** RT (mashing a trigger is gentler on thumbs) | **Design:** is button-mashing OK on a pad, or is a stick-wiggle / hold-RT-pressure better? The latter would need an analog value (`IInputProvider` would gain `float Axis`, so not now) |
| Mixing/Stirring (`MixingMinigame.OnTick`) | timing press while a needle sweeps | "pressed this frame" | South / RT | none |
| Both | cancel button `BTN - Cancel Minigame` (uGUI) | Back | `Gameplay/Back` (East) → the same `Cancel()` as the button. Pointer mode **Direct** during Play so South never Submits a focused button | Existing bug-ish behavior: a mouse click on the Cancel button also counts as `MinigameAction` (global LMB). Fix: in `ActionInputProvider`, ignore pointer-device presses when `EventSystem.current.IsPointerOverGameObject()` (polled in Update, so it is safe) |
| Intro/Outro | Animator only | nothing | — | Pause during minigame: maps disabled by InputService, `Poll` sees nothing |

Garnish (G3) = Cursor mode: virtual cursor over the RawImage → `RenderTextureClickRelay` + 28 buttons. Ingredient selection = Cursor mode + RS camera. Dialogue = Direct (Advance) and options = Focus. Pause/Settings/slots/book/event log/popups = Focus.

---

## 5. Remaining public types: missing fields filled in

| Type | Signature | Responsibility | Called by | Asm | Replaces | Edge cases | Test |
|---|---|---|---|---|---|---|---|
| `SaveSerializer` | `static string Serialize<T>(T v); static bool TryDeserialize<T>(string json, out T v)`; `static string ToJson(object state, Type t); static object FromJson(string json, Type t)` | the only Newtonsoft settings (indented in the editor, compact in builds; `TypeNameHandling.None`; Vector3/Quaternion converters moved from `SaveLoadManager`) | `SaveFileStore`, `SaveService` | Save | `SaveLoadManager.JsonSettings`, `Vector3Converter`, `QuaternionConverter` | bad JSON → false + log, never throws | EditMode round trip of every DTO |
| `SaveMeta` / `SlotId` | §1.7 | slot-list data without opening the slot / slot naming | `SaveFileStore`, `SlotListView`, `AutosaveScheduler` | Save | `SaveMetaData` | `SlotId` index < 0 → `ArgumentOutOfRange`; manual 0..5, auto 0..rotation-1 | covered by S3 |
| `LoadError` | §1.8 | why a slot can't load | `SlotListView` (message key `ui.save.error.<e>`) | Save | the silent `return` in `LoadFromFile` | — | S3 |
| `SaveContext` | §1.9 | snapshot of flow/dialogue flags for eligibility | built by a `Func<SaveContext>` from GameRoot | Save | scattered statics (`DialogueControls.IsSkipping`, `SceneLoaderBridge.IsSilentReplay`, `CocktailSystemManager.IsWaitingForTask`) | MainMenu: `InGame=false` → `NoGame` | S4 truth table |
| `IPlaytimeClock` | §1.10 | playtime for meta, injectable | `SaveService`, `ResumeCoordinator` | Save | direct `PlaytimeTracker` calls (the static stays, wrapped) | paused time counts (as today, realtime); the owner can change it | fake in S4 |
| `IReplayEngine` | §1.3 | arm/run replay; tells the coordinator about target/abort | `ResumeCoordinator` | Contracts (impl G2) | `SceneLoaderBridge.IsSilentReplay/TargetLineId/ReplayOptionQueue` + replay code in `BubblePresenter`/`OptionPresenterType2` | Arm twice → second ignored + error; Arm after the root started → error (too late) | fake in S5; real one in G2 PlayMode |
| `IVariableAccess` | §1.12 | get/set all Yarn variables as `YarnVars` | `ResumeCoordinator`, G2 Director (line capture) | Contracts (impl G2) | `SaveLoadManager.CollectYarnData/ApplyYarnData/ResolveDateType` + reflection | `SetAll` uses `clear:true` (d2); ints are stored as float (Yarn numbers are float) | S0 round trip |
| `IDayResume` | §1.12 | start the day at a saved DayPart without Event/chapter title | `ResumeCoordinator` | Contracts (impl G1) | `DayRunner.ResumeAsync` sketch (Refactor Plan §6.4), `BubblePresenter.Start` load path | `DayPart` None/BeforeOpen/Event → refuse (cannot be saved anyway) | G1 PlayMode |
| `PointerRay` | §2.3 | `ScreenPointToRay` from the pointer source | `N_InputManager`, `DebugPhysicsRaycaster`, `CameraController` | Contracts | `Camera.ScreenPointToRay(Input.mousePosition)` | null camera → `ArgumentNullException` (caller bug) | trivial, none |
| `FocusOnEnable` | §2.6 | always keep a valid selection on Focus screens | UI prefabs | Input | `EventPopupQueue` close-button focus code, ad-hoc `Select()` calls | the selected object is disabled → move to `first`; `first` inactive → first active `Selectable` child; no mouse-driven reselect while KeyboardMouse is active (avoids a focus ring under the mouse) | PlayMode: open Pause with only a pad, navigate to every control |
| `ActionInputProvider` | `public sealed class ActionInputProvider : IInputProvider { public ActionInputProvider(InputAction action, Func<bool> pointerOverUi = null); }` | minigame "pressed this frame" from `Gameplay/MinigameAction` | injected by the minigame host into `BaseMiniGame.Initialize` | Input (refs Features.Minigame's `IInputProvider`, or move `IInputProvider` to Contracts — **recommended**) | `UnityInputProvider` | a press from a `<Pointer>` control while `pointerOverUi()` → ignored; paused → map disabled → false | PlayMode `InputTestFixture`: press South → true for one frame only |
| `DialogueAdvanceInput` | `public sealed class DialogueAdvanceInput : MonoBehaviour, ILineAdvancerInput { [SerializeField] LineAdvancer lineAdvancer; [SerializeField] InputActionReference advance, hurryOptions; [Min(0)] public float inputCooldown = 0.25f; }` | Dialogue-map → Yarn `LineAdvancer` (hurry if typing, else next), unscaled cooldown | `Line Advancer` object in the dialogue prefab (LineAdvancer Input Mode = External) | Input (+ YarnSpinner.Unity ref) | `PauseAwareKeyCodes` | map disabled (Pause/options) → nothing; one action per frame | PlayMode: South while typing → hurry; again → next; while paused → nothing |
| `MenuSaveHost` | `public sealed class MenuSaveHost : MonoBehaviour { public SaveService Service { get; } }` | builds the load-only `SaveService` in MainMenu_V2 for the slot list + Continue | MainMenu UI | Save | none (no load UI exists) | no `Saves/` folder → empty list | manual |

## (a) Dependency diagram

```
Bar410.Contracts (UnityEngine core only) ← ISnapshotSource, IDialogueResumeSource, IReplayEngine, ReplayWatchdog, DTOs (DialogueResumeState, JournalEntry, YarnVars), IDayResume, IVariableAccess, DayPart, IPointerSource, PointerRay
   ▲            ▲                 ▲                    ▲
Bar410.Save     Bar410.Input      Bar410.Dialogue(G2)  Bar410.Features.*(G3)  Bar410.Flow(G1)
 SaveFileStore   InputService      DialogueDirector     implement ISnapshotSource; read IPointerSource
 SaveService     VirtualCursor      : IDialogueResumeSource, IReplayEngine, IVariableAccess
 PendingLoad     InputGlyphs        uses ReplayWatchdog (Contracts)
 ResumeCoord.    ButtonPrompt       DialogueAdvanceInput ──► (Bar410.Input)
 AutosaveSched.  FocusOnEnable
 SaveEligibility
 SlotListView ──► Bar410.Input (FocusOnEnable)
        ▲                ▲
        └──── Bar410.Host (G1 GameRoot): new SaveService/ResumeCoordinator/AutosaveScheduler, Register(sources), PendingLoad.TryTake → ResumeAsync; passes InputService/IPointerSource into features
```
`Bar410.Save` must not reference Dialogue/Flow/Features (interfaces only). `ReplayWatchdog` lives in Contracts (pure C#), so Dialogue never references Save. Features read `IPointerSource` from Contracts. Only hosts/Stations (G1/G3) that push modes reference `Bar410.Input`.

## (b) Task breakdown

| Id | Task | Files to create (under `Assets/Bar410/`) | Inputs | Definition of done | Diff | Model / reason |
|---|---|---|---|---|---|---|
| S0 | Spike: Yarn node-event order on detour + `GetAllVariables` round trip + `File.Replace` on Linux/Windows | none (scratch script, not committed) | d1, d6 | written result in `Docs/`; tracker logic confirmed or a corrected rule | 2 | sonnet: reading/trying, small |
| I0 | Spike: virtual cursor with InputSystemUIInputModule, PhysicsRaycaster, RenderTextureClickRelay, timeScale 0, device-detection filter | none | d3, d4, d7 | each check pass/fail noted; decision on `Point`-action pointer source | 3 | opus: subtle input pipeline interplay |
| S1 | Contracts + DTOs | `Contracts/Save/*.cs` (ISnapshotSource, IDialogueResumeSource, IReplayEngine, IVariableAccess, IDayResume, DialogueResumeState, JournalEntry, YarnVars, ReplayWatchdog) | §1.1-1.5, §2.3 | compiles; Contracts has no MonoBehaviour and no reference beyond UnityEngine core | 2 | haiku: boilerplate from this spec (watchdog → S2) |
| S2 | `ReplayWatchdog` + EditMode tests | `Contracts/Save/ReplayWatchdog.cs`, `Tests/EditMode/ReplayWatchdogTests.cs` | §1.4 | table tests green | 3 | opus: divergence rules are the risky core |
| S3 | `SaveData/SaveMeta/SlotId/SaveSerializer/SaveFileStore` + tests | `Save/*.cs`, `Tests/EditMode/SaveFileStoreTests.cs` | §1.6-1.8 | round-trip, atomic, version-refuse tests green | 2 | sonnet |
| S4 | `SaveEligibility` + `SaveService` + `PendingLoad` + `IPlaytimeClock` adapter + tests with fakes | `Save/SaveService.cs`, `Save/SaveEligibility.cs`, `Save/PendingLoad.cs` | S1, S3 | fake-source capture/restore tests green | 3 | sonnet |
| S5 | `ResumeCoordinator` (protocol 1-6) | `Save/ResumeCoordinator.cs`, tests with fake IReplayEngine/IDayResume | S1-S4, G1 `IDayResume`, G2 `IReplayEngine` | EditMode order tests; PlayMode `TEST_SaveLoad.yarn` save/load at every listed spot (Refactor Plan 4d "done when") | 5 | opus: ordering across async + replay |
| S6 | `AutosaveScheduler` + tests | `Save/AutosaveScheduler.cs` | S4, G1 Request calls | deferral + rotation tests; Play: autosave after serve resumes *after* the drink | 4 | opus: timing semantics |
| S7 | Slot-select UI (Pause + MainMenu, Continue) | `Save/UI/SlotListView.cs`, `SlotRowView.cs`, prefab | S4, I4 (focus), G2 LocalizedString | controller-only flow: save, overwrite confirm, load, delete, incompatible row | 3 | sonnet |
| S8 | Remove old save code | delete `SaveLoadManager.cs`, `Save*Data.cs` (feature DTOs move to features), `SceneLoaderBridge` dialogue fields, `[SaveLoadSystem]` | S5 working | grep shows no references; scene has no missing scripts | 2 | haiku |
| I1 | Action asset + generated class | `Input/Bar410Controls.inputactions` (+ `.cs`) | §2.1 | all maps/schemes present; all 5 build scenes' UI modules point at it | 2 | sonnet |
| I2 | `InputService` (maps, mode stack, device detection) + `IPointerSource` | `Input/InputService.cs`, `Input/IPointerSource.cs`, `[Input].prefab` | I0, I1 | InputTestFixture tests: device flip, virtual-mouse ignored, push/pop | 4 | opus: device feedback loops, stack semantics |
| I3 | `VirtualCursor` | `Input/VirtualCursor.cs`, cursor prefab + sprite placeholder | I2 | Play: buttons, PhysicsRaycaster objects, Garnish relay clickable by pad, paused too | 3 | opus (with I2) |
| I4 | `FocusOnEnable` + select feedback on `ButtonScaleFeedback`/`HoverTooltip` + `deselectOnBackgroundClick=false` | `Input/FocusOnEnable.cs`, edits | I2 | every Focus screen reachable from pad without mouse | 2 | sonnet |
| I5 | `InputGlyphs` SO + `ButtonPrompt` + glyph table test | `Input/InputGlyphs.cs`, `Input/ButtonPrompt.cs`, `InputGlyphs.asset` | I1, art (placeholder sprites ok) | EditMode coverage test green; prompts swap on device change | 3 | sonnet |
| I6 | Call-site swaps (mechanical rows of §3) | edits | I2 | rows marked haiku done | 1 | haiku |
| I7 | `N_InputManager` (encoding then pointer), `HoverTooltip`, `PauseManager`, `DialogueAdvanceInput` | edits + `Input/DialogueAdvanceInput.cs` | I2 | Play: hover/drag ingredients by mouse and pad; Esc/Start pause; Enter/South advance; no advance while paused | 3 | sonnet |
| I8 | `CameraController` gamepad views + SO write fix | edit | I2, design answer §2.7 | Play: RS views only in AddIngredient & shaker not full; cursor at edge doesn't rotate | 3 | sonnet |
| I9 | Minigame `ActionInputProvider` + Back + Direct mode | edits | I2 | Play: both games by pad; Cancel by East; mouse click on Cancel no longer counts as a hit | 2 | sonnet |
| I10 | Switch to new-only | ProjectSettings | all I* | gate in §3 passes | 1 | haiku |

Order: S0, I0 → S1 → S2/S3 → S4 → (G2 Director + G1 DayRunner ready) S5 → S6 → S7 → S8. In parallel: I1 → I2 → I3/I4/I5 → I6-I9 → I10.

## (c) Review checklist (Opus reviewer)

- **S1:** No `UnityEngine.Object` in DTOs. Every `Capture` contract says "new object". `DayPart` is a string. JSON shape stable (no polymorphic `object` fields, no `TypeNameHandling`; that is a security risk on loaded files).
- **S2:** a jump during replay → abort. An id mismatch at the target scans forward with a bounded budget. The stall uses real time (`Time.realtimeSinceStartup`), not scaled time. No allocation per line.
- **S3:** the slot is written before the meta. `.tmp` lives in the same folder (rename is atomic only on the same volume). `File.Replace(tmp,dst,null)`, with no backup file left behind. Version is read from the meta before the full deserialize. Exceptions don't escape `Write`/`TryRead` unlogged. No `DontDestroyOnLoad`.
- **S4:** `TrySave` checks eligibility itself (the UI is not trusted). Settings/language are absent from `SaveData`. Chapter is a key. Time in UTC ticks. Register throws on a duplicate key in the editor.
- **S5:** `timeScale` reset to 1. RootStartVars are applied **before** the root starts. Leftover journal entries are dropped at the target. Idempotency of mutating verbs checked against G3's list (`CharacterEnter`, `order_customer`, `unlock_ingredient`, `Switch_Camera`). Target restores run synchronously inside `ReachedTarget`, before the line presents. Abort and LineIndex 0 paths also restore. `ct` comes from `destroyCancellationToken`. No `async void`. An exception in one source doesn't skip the others.
- **S6:** no save fires on the frame of the request. A blocked line keeps the request pending. Rotation picks the oldest. DayStart uses `RootStarted`. Saving during replay is impossible.
- **S7:** works with no mouse. Overwrite/delete confirm. Incompatible rows disabled. Text comes from localization keys. The selection after deletion lands on a valid row.
- **S8:** every `SceneLoaderBridge` dialogue field reference is gone (BubblePresenter, OptionPresenterType2, DialogueControls, EventLog, SkipScan, Post_It_Order, CocktailSystemManager.YarnTask, Characters, GameFlowCommands). G2/G3 have replaced them with `IReplayEngine.IsReplaying`.
- **I1:** the UI map still contains everything `InputSystemUIInputModule` needs. Point uses `<Pointer>/position`. Gamepad scheme set. No action shares South in a way where two maps are enabled at once without a mode rule (§2.2).
- **I2:** VirtualMouse is filtered from detection. Mode stack Dispose is idempotent. UI map is never disabled. Scene-scoped static cleared in `OnDestroy`. `onActionChange` unsubscribed (it is a static event; otherwise it leaks across Play sessions with domain reload off).
- **I3:** `SoftwareCursor`. Cursor canvas Overlay. Works at timeScale 0. Hidden when the real mouse moves. No hardware warp.
- **I4:** every `PausePanel`, popup and slot list has a first selection. Select gives the same feedback as hover. `deselectOnBackgroundClick` false.
- **I5:** missing glyph falls back to text and doesn't throw. Labels are localized. PlayStation is recognized via the `DualShockGamepad` base.
- **I6/I7:** `N_InputManager` saved as UTF-8 in a separate commit before the logic change. `DialogueAdvanceInput` keeps the cooldown and is unscaled. Pause no longer leaks Esc into dialogue (CLAUDE.md "Input ซ้ำกัน").
- **I8:** edge-hover is off with a gamepad. The runtime SO write is removed. Look is enabled only by flow (ADR 0002 Amendment).
- **I9:** pointer press over UI is ignored by `ActionInputProvider`. Back = `Cancel()` path (outcome `Cancelled`, cannot lose).
- **I10:** the `grep -rna` gate is clean and all build scenes have been played.

## (d) UNVERIFIED assumptions

1. **Yarn detour event order:** `onNodeStart(B)` fires before `onNodeComplete(A)` is skipped for detour, and `onNodeComplete(B)` fires when the detour returns (logic in `YarnSpinner.dll`, not source). The existing tracker assumes this. → S0.
2. `GetAllVariables` returns only explicitly set variables (InMemoryVariableStorage). Restore with `clear:true` therefore resets the rest to defaults. Read at `InMemoryVariableStorage.cs:235-269` in S0.
3. With `pointerBehavior = SingleMouseOrPen…`, the real mouse + `VirtualMouse` act as one UI pointer. `PhysicsRaycaster` objects and `RenderTextureClickRelay` get press/drag/click from the virtual mouse. `UI/Point` reflects the virtual mouse position. → I0.
4. `VirtualMouseInput` clamps to the canvas from `TryFindCanvas` (the cursor's parent canvas). It must be Overlay or the clamp/speed is wrong. → I0.
5. `DualShockGamepad` is the base layout of both DS4 and DualSense on Windows/Linux (HID). Xbox pads on Linux show as generic `Gamepad` (glyph fallback Xbox is fine). → I0 with real pads (the owner tests; I cannot).
6. `File.Replace` works on Linux Mono/IL2CPP standalone (Unity's Mono implements it with rename). → S0; fallback `File.Delete(dst); File.Move(tmp,dst)` (small window, acceptable).
7. Events from `VirtualMouseInput`'s `InputState.Change` do not trigger `onActionChange` with the virtual device as `activeControl.device` for Gameplay actions bound to `<Gamepad>` (they come from the pad itself). Only `<Pointer>` bindings would see `VirtualMouse`, and those are filtered by name. → I0.
8. `IsPointerOverGameObject()` polled in `Update` is reliable with InputSystemUIInputModule (the warning is only for action callbacks). → I9.
9. G2 can make every verb complete synchronously during replay (Text Animator / DOTween fades skipped). If one still awaits, the watchdog stall budget aborts, so the risk is an inexact resume rather than a hang.
10. Owner decisions needed (not technical unknowns): (i) bump `SaveData.Version` on every Yarn content change, **or** store `YarnProgramHash` (recommended: hash, refuse on mismatch; this means any `.yarn` edit invalidates saves in development, so the dev menu needs an "ignore hash" toggle); (ii) drop the EndOfDay autosave (it is usually one line from the next DayStart); (iii) dialogue/hub button keys and pad buttons (§2.1 "?"); (iv) shake = mash vs analog; (v) camera RS snap vs hold-to-peek; (vi) parallax with pad (static vs RS); (vii) whether `#save_checkpoint` is still needed.

## (e) Interfaces expected from other groups / provided

**From G1 (core flow / GameRoot / DayRunner):**
- `Bar410.Contracts` assembly exists (G4 adds files to it).
- `IDayResume.ResumeAsync(DayPart, rootNode, ct)`: waits for the transition, sets the mode for Story/Open/Close without Event or chapter title, starts the root through G2.
- `DayRunner.CurrentPart` (exposed to G2's line-time capture through a `Func<string>` GameRoot passes in), the day number + chapter key for meta.
- Calls `AutosaveScheduler.Request(DayStart)` after Event ends, `(AfterServe)` after scoring, `(EndOfDay)` on close.
- `IsBartending` flag for `SaveContext`, and `IsLoading`.
- GameRoot: `PendingLoad.TryTake` in Awake → `ResumeCoordinator.ResumeAsync`, otherwise `StartDayAsync`. Registers all `ISnapshotSource`s. Hands `IPointerSource`/`InputService` to features. Pushes `PointerMode` per Station (`using var m = input.PushMode(...)` inside the Station try/finally).

**From G2 (dialogue / Yarn / localization):**
- `DialogueDirector : IDialogueResumeSource, IReplayEngine, IVariableAccess`. It counts `LineIndex` per root, stores choices, keeps the journal (every `YarnVariableSync` write + function results + random picks), captures `LineVars` + DayPart + camera at each presented line, drives `ReplayWatchdog`.
- `YarnVerbs` `ReplayPolicy` (§3.4) honored. Functions `Order_Cocktail_*`, `order_*()`, `flow_phase()`, `flow_step()` return journaled values during replay (`ingredient_unlocked()` is NOT journaled; see §1.0). The target line is not re-logged and not re-counted. All verbs complete without awaiting during replay.
- The EventLog snapshot stores line IDs. `LocalizedString`/`LocalizedText` exist for prompts and slot rows.
- The options presenter pushes `PointerMode.Focus` while options show and disables `Dialogue/Advance`. It uses `DialogueAdvanceInput` (G4 writes it, G2 wires it).
- `<<save_chapter_node>>` becomes a warning-only shim.

**From G3 (features + hosts):**
- **Idempotent verbs** (§1.0): every verb that changes a snapshot source must give the same end state when run twice.
- `ISnapshotSource` for characters (`NPCData` dict), unlocks (`StoryUnlocked`, `Notified`; Scope `RootStartAndTarget`), order (customer, mode, **drink id** not `S_Drink`, ordered/served type, result, `IsScored`), camera (shot id; Scope AtTarget), event log (from G2).
- Features read `IPointerSource`, not the mouse. `CameraController` per §2.7. `BaseMiniGame` takes `IInputProvider` injected. `ButtonScaleFeedback`/`HoverTooltip` handle select. Minigame/Garnish hosts push the modes in §4.
- `S_Drink` needs a stable id (G3 data).

**G4 provides:** `ISnapshotSource`, `IDialogueResumeSource`, `IReplayEngine`, `IVariableAccess`, `IDayResume` (contracts), DTOs, `ReplayWatchdog`, `SaveService`, `SaveFileStore`, `SaveEligibility`, `PendingLoad`, `ResumeCoordinator`, `AutosaveScheduler`, `SlotListView`; `Bar410Controls` asset, `InputService` (+`Current`), `IPointerSource`, `PointerMode`, `VirtualCursor`, `InputGlyphs`, `ButtonPrompt`, `FocusOnEnable`, `ActionInputProvider`, `DialogueAdvanceInput`.

## Summary

G4 defines save/load and input for the rewrite.

**Save:** a resume point is the state when line N of the current root was shown. Replay goes from the root, with variables restored to the root-start snapshot. A journal covers C#-written variables, function results and random picks, and `<<detour>>` is supported. At the target, the line-time variables and the save-time `ISnapshotSource` states are restored. That timing requires idempotent mutating verbs.

G4 owns the DTOs, files, eligibility, autosave, resume orchestration and `ReplayWatchdog` (line/id/jump/stall budgets). G2's `DialogueDirector` implements the capture and replay interfaces.

Files are `slot_N.json` + `.meta.json` (and `auto_N`), written atomically (tmp → Replace/Move). The version is read from the meta, there is no migration, and chapter keys and UTC ticks are stored instead of text. Autosave requests are fulfilled at the next eligible line. This prevents the "remake the drink" bug that an immediate after-Serve save would cause.

**Input:** one `Bar410Controls` asset (UI/Dialogue/Gameplay maps), and a scene-scoped `InputService` with a pointer mode stack (Direct/Focus/Cursor). `IPointerSource` reads `UI/Point`, and a `VirtualMouseInput` software cursor is filtered out of device detection. The rest:
- glyphs per device;
- `FocusOnEnable`, with select feedback;
- discrete camera views on the right stick (edge-hover off with a pad);
- a minigame `ActionInputProvider` (South/RT, Back = East).

The real count is 29 live legacy `Input.*` calls in 10 files, not 31. `N_InputManager` is non-UTF-8 and was missed by the earlier grep. The gate for going new-only is a `grep -a` plus a play-through, not a compile.

Open items are in (d): spikes S0/I0 and owner decisions such as Yarn hash vs version, EndOfDay autosave, button layout, and shake style.

File: /tmp/claude-1000/-home-icebutmelted-WorkSpace-Git-ProjectCocktail-renew/5d41d2d1-898d-4666-8fdf-68fea549af8c/scratchpad/api_G4.md
