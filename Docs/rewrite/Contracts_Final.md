# Bar410 Rewrite — Contracts_Final (N-RECON)

Written 2026-11-07, after spikes G1-0, G4-S0 and G2-T02. Nothing has been compiled.
**This file overrides `API_G1`–`API_G4` and `Bar410_Rewrite_ExecutionPlan.md` §3 and the §6 task tables wherever they disagree.**
**Phase move:** G3-T1/G4-S1 (Contracts) move from R1 to R2, after N1, because Contracts names Domain types. G1-4 and G1-5 follow them. Whatever is not mentioned here still holds as written in the group specs.

Inputs: ExecutionPlan §2/§3/§5/§8, `API_G1_CoreFlow.md`, `API_G2_Dialogue.md`, `API_G3_Features.md`, `API_G4_SaveInput.md`, `Spike_G1-0_Results.md`, `Spike_G4-S0_Results.md`, `Spike_G2-T02_Results.md`.
**G4-I0 (virtual cursor, needs the owner's pad) is NOT done.** Every decision marked **(prov. I0)** is provisional until it is.

---

## 0. Status at reconciliation time

| Item | Status |
|---|---|
| G1-0, G4-S0, G2-T02 | done (results files above) |
| G4-I0 | **not done** (needs the owner's gamepad) |
| G3-T0 (type splits, `Mono.Cecil` removed) | done: `b76719b`, `b91cbb9` (`HubPhase.cs`, `Facing.cs`, `MinigameOutcome.cs` now exist as their own files) |
| N6 (`N_InputManager` → UTF-8) | done: `bcc0176` |
| D12 (install `com.unity.localization`) | answered by action: committed in `edd03af` (Localization 1.5.14 + Addressables) |
| D1–D11, D13, D14 | blank in ExecutionPlan §8 → defaults below (§7) |

---

## 1. Rule used to place interfaces

An interface goes into `Bar410.Contracts` **only if an asmdef consumer calls it** (Flow, Dialogue, Verbs, Save, Input). Anything that only Host (Assembly-CSharp) calls stays a concrete Host class, with no interface.
Results of this rule:
- Dropped as interfaces: `IHub`, `IEventPopups`, `IChapterTitle` (G3), `IUiScenes`/`IUiSceneRegistry` move out of Contracts into `Bar410.UiScenes` (G3-internal).
- Dropped: `GameRoot.Register/TryGet/GetAsync` (G1). The single cross-scene handshake is G3's `UiSceneRegistry` + `UiSceneLoader`.
- G1's port names are replaced by G3's feature interfaces where they overlap (§3.2). Flow's mirror enums (`MinigameKind`, `MinigameOutcome`, string `HubKey`) are deleted. Flow uses the Domain types directly.

---

## 2. Final assemblies

Folder root: `Assets/Bar410/`. Unity asmdef references are **not transitive**: list every assembly you use. Rule: every asmdef that references `Contracts` also lists `Domain`.

| asmdef | Folder | References | Contents | Owner |
|---|---|---|---|---|
| `Bar410.Core` | `Core/` | UnityEngine | `FlowAwait` (+ `FromCoroutine`), `Forcible<T>` | G1 |
| `Bar410.Domain` | `Domain/` | UnityEngine, Odin (auto-ref) — **not** `noEngineReferences` | the G3 §1 move list (all 4 `E_Cocktail` partials, `S_Drink`, Session/*, `DrinkOrderContext`, `ICustomerPreferences`, `MinigameOutcome`, `MinigamePhase`, `HubPhase`, `Facing`, garnish SOs, `IngredientId`, `SO_IngredientUnlockTable`) **+ `SO_DaySchedule`** | G3 (move = N1), G1 (`SO_DaySchedule` = G1-3) |
| ~~`Bar410.Data`~~ | — | — | **dropped.** Everything it held is a Domain SO. Add it back only when an SO needs Core. | — |
| `Bar410.Contracts` | `Contracts/` | UnityEngine, Domain | every interface/DTO in §3 (no MonoBehaviour) | G1 owns the asmdef; G3/G4 add files |
| `Bar410.Flow` | `Flow/` | Core, Domain, Contracts | `FlowTypes`, `FlowState`, `FlowPorts`, `Station`+3 stations, `BartendingSession`, `DayRunner` (`: IDayResume`), `FlowCommands` | G1 |
| `Bar410.Localization` | `Localization/` | Unity.Localization | `Language`, `OrderDescriptionProvider`, `LocKeys` | G2 |
| `Bar410.Input` | `Input/` | Contracts, Domain, Unity.InputSystem, UnityEngine.UI, Unity.TextMeshPro | `InputService`, `VirtualCursor`, `InputGlyphs`, `ButtonPrompt`, `FocusOnEnable`, `ActionInputProvider` | G4 **(prov. I0)** |
| `Bar410.Dialogue` | `Dialogue/` | Core, Contracts, Domain, Localization, Input, YarnSpinner.Unity, TA addon, Febucci TA runtime/TMP, Unity.TextMeshPro, UnityEngine.UI, Unity.Localization, Unity.InputSystem | registry, Director, presenters, `CocktailLineProvider`, `LocMarker`, voice, event log, `RootTracker`, `DialogueAdvanceInput`. **No reference to Flow.** | G2 |
| `Bar410.Dialogue.Editor` | `Dialogue/Editor/` | Dialogue, YarnSpinner.Unity.Editor, YarnSpinner.Compiler | validators, function declarations | G2 |
| `Bar410.Verbs` | `Verbs/` | Core, Dialogue, Flow, Contracts, Domain, Localization, YarnSpinner.Unity | every `[YarnVerb]` + compat shims | G2 (bodies by verb owner) |
| `Bar410.Save` | `Save/` | Contracts, Domain, Newtonsoft | save/slot/resume/autosave | G4 |
| `Bar410.WorldView` | `Features/WorldView/` | UnityEngine.UI, Unity.Cinemachine | `WorldViewHandle`, `WorldViewSource`, `WorldView` | G3 |
| `Bar410.UiScenes` | `Features/UiScenes/` | UnityEngine.UI, Unity.Cinemachine | `UiSceneId`, `IUiSceneRegistry`, `UiSceneRegistry` (static), `UiSceneLoader`, `UiSceneEntry` | G3 |
| *(no asmdef)* | `Host/` (adapters under `Host/Adapters/<Feature>/`) | everything (Assembly-CSharp) | `GameRoot`, `MainWiring`, every adapter that names an old manager, UI-scene hosts, scene entries | G1 + G3 adapters |
| `*.Tests` | `Tests/<Asm>/` | the asm under test + Core + nunit, Editor only, `UNITY_INCLUDE_TESTS` | EditMode tests | each group |

Rules (compiler-enforced): arrows point down only; Flow never sees Dialogue/Verbs/Save/Input/Host; Dialogue never sees Flow; Save sees only Contracts/Domain; no asmdef uses DOTween module extensions; G3's `Assets/Bar410/Adapters/` is renamed `Assets/Bar410/Host/Adapters/`.
Note (G2-T02): the Yarn source generator emits one `*-generated.ysls.json` per asmdef that references `YarnSpinner.Unity` (Dialogue, Verbs). Commit them like the existing ones.
Note (namespace trap, R1): a namespace `Bar410.Input` makes `Input` resolve to that namespace inside any `namespace Bar410.*` block, so `Input.GetKey(...)` in old code (`Bar410.GameFlow` › `GameFlowDebugHotkeys`) stops compiling. Rule: **no namespace segment under `Bar410` may equal a type name used unqualified** (`Input`, `Time`, `Debug`, `Random`, `Object`, `Cursor`, `Screen`, `Application`, `Camera`, `Physics`, `PlayerInput`). The asmdef keeps the name `Bar410.Input`; its C# namespace is **`Bar410.Controls`** (G4 sets `rootNamespace` to it and replaces the `Bar410.InputAsm` marker). Other asmdefs keep namespace = asmdef name.

---

## 3. Final shared signatures (`Bar410.Contracts`, namespace `Bar410.Contracts`)

Shared conventions (all groups):
- Async = `Awaitable`/`Awaitable<T>` + `CancellationToken`. Cancellation = `OperationCanceledException` (G1-0 U4: it is OCE, not `TaskCanceledException`).
- **Never await an `Awaitable` twice; a new `AwaitableCompletionSource` per wait.** G1-0 U2: a second await of a completed Awaitable throws `NullReferenceException`; two awaiters on a pending one → **the first is silently lost (hang)**. Several waiters → poll a flag with `Awaitable.NextFrameAsync(ct)`.
- `TrySetResult`/`TrySetCanceled` continue inline (G1-0 S1). Labels set before a command returns hold.

### 3.1 Flow / day

```csharp
public enum DayPart { None, Event, Story, Open, Close }

public interface IStory {                       // impl: G2 DialogueDirector
    int Day { get; }                            // Yarn $day (Yarn is the only store)
    void AdvanceDay();                          // $day+1; called inside the Journal verb flow_next_day
    void StartStory(string node);               // queues until the runner is idle; NEVER awaited by Flow (NextDay deadlock trap)
}

public interface IDayUi {                       // impl: Host DayUiAdapter (PanelTransition + EventPopupQueue + IngredientUnlocks + ChapterTitleUI)
    Awaitable WaitTransitionAsync(CancellationToken ct);                       // opening transition finished (5 s cap)
    Awaitable ShowDayEventsAsync(CancellationToken ct);                        // TakeNewUnlocks → one unlock popup → ShowAll → AllClosed; subscribe BEFORE ShowAll; empty queue = same frame
    Awaitable ShowChapterTitleAsync(string chapterKey, bool wait, CancellationToken ct);
}

public interface IDayResume {                   // impl: G1 DayRunner
    // Waits for the transition, sets Part without Event or chapter title (Story/Open/Close; Open → talking mode),
    // then IStory.StartStory(rootNode). Part None/Event → error log + normal StartDay.
    Awaitable ResumeAsync(DayPart part, string rootNode, CancellationToken ct);
}
```
`ResumePoint` (G1) is deleted. G2's "flow snapshot + `ResumeAt`" is deleted: the resume point is a presented line, so the bar is at `Idle` there (see §4.4).

### 3.2 Bar, orders, hosts, camera (Flow + Verbs consume)

```csharp
public enum CameraShot { Bar, Shaker, Garnish }
public interface ICameraDirector {              // impl: Host CameraDirector (G3-T5)
    string ActiveCameraId { get; }
    void Show(CameraShot shot);                 // blend
    void Cut(CameraShot shot);
    void SwitchTo(string cameraId);             // Yarn Switch_Camera; unknown id → error, no change
    void CutTo(string cameraId);                // Yarn Cut_Camera
    void SetLookFree(bool free);                // ONE polarity: true only in 2.1 while the shaker has room; false snaps forward
    void ResetLook();
    void AimCustomerInset(CharacterName who);   // None = stop the inset camera. Adapter resolves the anchor through CustomerStage itself
}

public interface IBarCounter {                  // impl: Host CocktailFeature (G3-T4). Holds IIngredientUnlocks itself.
    bool HasRoom { get; }  bool IsEmpty { get; }
    Enum_MiniGameType RequiredMinigame { get; }
    event Action ShakerFull;                    // adapter already did its bar-side reactions
    event Action ShakerCleared;
    event Action<Enum_MiniGameType> MethodChosen; // Shake/Stir pressed; adapter maps E_Cocktail.Method → Enum_MiniGameType (Stirring → Stiring)
    event Action ResetChosen;                   // method-panel Reset; station treats it exactly like ShakerCleared
    void BeginDrink();                          // shaker.Clear (raises ShakerCleared synchronously → subscribe first), permissions reset, Order.ClearResult
    void OpenIngredients();                     // 2.1 enter (uses unlocks.IsUnlocked)
    void CloseIngredients();                    // 2.1 exit
    void LockIngredients();
    void RefreshUnlocked();                     // Event: hide locked bottles
    S_Drink ResolveDrink();                     // UpdateCocktailInShaker(); throw-away runtime SO (F10)
    void SetIce(bool on);                       // F9: before scoring
    void SetShakerGrabbable(bool on);
    void ResetDrink();                          // remake
    void SetOrderNoteAvailable(bool on);
    void CloseBook();                           // was missing in G3
}

[Serializable] public struct ScoreResult { public CharacterName customer; public Satisfaction result; public TypeOfCocktail servedType;
    public float payout; public float relationshipDelta; public string recipeName; public bool isValid; }   // delta, never a total

public interface IOrderDesk {                   // impl: Host CocktailFeature (G3-T4)
    DrinkOrderContext Order { get; }            // read-only by convention (HasOrder, IsScored, Customer live here)
    string PlaceNamed(string recipeName, bool byFlavor);
    string PlaceByPreference(CharacterName customer, bool byFlavor);
    bool   PlaceByType(TypeOfCocktail type);
    void   SetCustomer(CharacterName customer);
    void   ClearOrder();
    bool   EnsureOrder();                       // PARITY with RandomCocktailIfNoOrder(): no order → random order + warning; returns true if one was placed
    ScoreResult CommitServe();                  // THE scoring call; idempotent; 2nd call returns cached, raises nothing
    void UseRecipeFilter(Func<S_Drink, bool> canMake);
    void UsePreferences(ICustomerPreferences prefs);
    event Action<ScoreResult> Scored;           // synchronously inside the first CommitServe
    event Action Cleared;
}
// G3's WaitScoredAsync is dropped (serve_order polls the session, §4.3).

public readonly struct MinigameRequest { public readonly Enum_MiniGameType Selected, FromDrink; public readonly bool ShowCustomer;
    public MinigameRequest(Enum_MiniGameType selected, Enum_MiniGameType fromDrink, bool showCustomer); }
public interface IMinigameHost {                // impl: MinigameUiHost (UI scene) or LegacyMinigames (R3 fallback)
    bool IsBusy { get; }                        // = a RunAsync call has not returned yet (background outro does NOT count)
    event Action<Enum_MiniGameType> Started;
    Awaitable<MinigameOutcome> RunAsync(MinigameRequest request, CancellationToken ct);
}
public interface IMinigameAnimation { void Play(); void Stop(); }

[Serializable] public struct GarnishDecoration { public SO_GlassOption glass; public bool ice; public SO_GarnishItemOption item0, item1; public SO_GarnishRimOption rim; }
public enum GarnishExit { Finished, Remake }
public readonly struct GarnishResult { public readonly GarnishExit Exit; public readonly GarnishDecoration Decoration; public GarnishResult(GarnishExit exit, GarnishDecoration d); }
public enum ServeDecision { Served, Remake }
public readonly struct GarnishRequest { public readonly S_Drink Drink; public GarnishRequest(S_Drink drink); }
public interface IGarnishHost {                 // impl: GarnishUiHost (UI scene) or LegacyGarnish (R3 fallback)
    bool IsBusy { get; }
    void ClearGlass();                          // NEW (was G1 IGarnish.ClearGlass): forwards to the bound IGarnishTable.Clear(); cancels a running pour
    Awaitable<GarnishResult> RunAsync(GarnishRequest request, CancellationToken ct); // step 3
    Awaitable<ServeDecision> ServeAsync(CancellationToken ct);                       // step 4
}
```
`IMinigameHost.RunAsync` timing (merges G1 C3 and G3 §2.4):
1. `IsBusy` → `InvalidOperationException`. Then wait (bounded: outro fallback + 0.5 s) until the manager is `Idle` (F11).
2. Subscribe, then `StartMinigame`. Show views (+ the customer view when `ShowCustomer`).
3. **Completed** → returned after the outro. **Back** → `CancelMinigame()` and return `Cancelled` **at once** (the outro continues alone). No fail outcome. Unknown type (`Building`) → error log, `Completed` (D5).
4. **`ct` cancelled** (load, teardown, or a Yarn force through `Forcible`) → `CancelMinigame()` and throw OCE **synchronously**. Do not wait for the outro: a forced `flow_another_ingredient`/`flow_drink_complete` must change the label before the command returns (S1).
5. Hiding views/canvas after a Back or a cancel happens in a background `FlowAwait.Run` that waits for `Idle`, then hides. It skips the hide if a newer run has started (run id).

### 3.3 Customers, unlocks (Verbs consume)

```csharp
public enum StageTiming { Normal, Instant }     // verbs pass Instant when Director.Mode != Live
[Serializable] public struct CharacterState { public CharacterName name; public bool present; public int spot; public int expression; public Facing look; }
public interface ICustomerStage {               // impl: Host CustomerStage (G3-T7)
    bool TryParse(string raw, out CharacterName who);   // warns on typo
    bool Exists(CharacterName who); bool IsPresent(CharacterName who);
    Transform Anchor(CharacterName who);
    Awaitable EnterAsync(CharacterName who, int spot, StageTiming timing, CancellationToken ct);  // idempotent (present → move/no-op)
    Awaitable ExitAsync(CharacterName who, StageTiming timing, CancellationToken ct);
    bool SetExpression(CharacterName who, string idOrIndex);
    void SetLook(CharacterName who, Facing look);
    void SetSpeaking(CharacterName who, bool speaking);
}
// Capture/Restore leave the interface: the adapter implements ISnapshotSource "characters" directly.

[Serializable] public struct UnlockNotice { public string key; public string displayName; public Sprite icon; }
public interface IIngredientUnlocks {           // impl: Host UnlocksAdapter (G3-T6)
    bool IsUnlocked(string ingredientKey);      // exact case; not in table → true
    bool IsRecipeComplete(S_Drink drink);
    void Unlock(string ingredientKey);          // case-insensitive; idempotent
    IReadOnlyList<UnlockNotice> TakeNewUnlocks();
    event Action Changed;
}
// Capture/Restore leave the interface: the adapter implements ISnapshotSource "unlocks" (UnlockState with storyUnlocked + notified).
```

### 3.4 Save / replay (G4 owns; G2 implements the dialogue side)

```csharp
public interface ISnapshotSource {              // G3 features + G2 event log; registered by GameRoot
    string Key { get; }                         // "characters" "unlocks" "order" "camera" "eventlog"
    Type StateType { get; }                     // plain DTO: no UnityEngine.Object, no S_Drink (store RecipeKey, N4)
    object Capture();                           // NEW object every call
    void Restore(object state);                 // null = key missing → reset; must not throw
}
// SnapshotScope is DELETED (and SaveData.RootStartSnapshots): every function that reads C# state is Journal (§4.2),
// so only Yarn variables are replay-relevant, and RootStartVars covers them. All sources are restored at the target.

public sealed class YarnVars {                  // ONE shape (G4's). Newtonsoft is the serializer.
    public Dictionary<string, float> F = new(); public Dictionary<string, string> S = new(); public Dictionary<string, bool> B = new();
    public static YarnVars Diff(YarnVars before, YarnVars after);   // changed + added keys only; exact float compare
}
// Rules (G4-S0): keep $Yarn.Internal.* keys (visit counts live there; never filter by prefix). Yarn numbers are float32:
// integers above 2^24 are not exact, and the save format promises no more. Tests compare via GetAllVariables, never TryGetValue.

public sealed class JournalEntry {
    public int Seq; public string Verb;         // "serve_order", "fn:Order_Cocktail_OutName", "flow_next_day"…
    public string Result;                       // function return as string; null for commands. Floats: ToString("G9", InvariantCulture)
    public YarnVars Writes;                     // Yarn-variable diff made by the call
}

public sealed class DialogueResumeState {       // replaces G2 DialogueSavePoint
    public string RootNode;
    public int LineIndex; public string LineId; // 0/null = start the root fresh
    public List<int> OptionChoices = new();     // DialogueOptionID, since root start
    public List<JournalEntry> Journal = new();  // since root start
    public YarnVars RootStartVars, LineVars;    // root start / at PRESENTATION of line LineIndex (not at save time; F13)
    public string DayPart;                      // FlowState.Part at presentation (string so a rename fails loudly)
}
// G4's CameraShotId is dropped: camera is the "camera" ISnapshotSource (Switch_Camera/Cut_Camera are absolute, so a re-run is harmless).

public interface IDialogueResumeSource {        // impl: G2 DialogueDirector
    bool HasRoot { get; }
    DialogueResumeState CaptureResumePoint();   // deep copy of the state at the last presented line
    event Action LinePresented;                 // live, first presentation only; not on retype, not in replay, not for the target line
    event Action<string> RootStarted;           // a ROOT from RootTracker (§4.1); not a detour, not a resume; not raised while replaying
}
public interface IReplayEngine {                // impl: G2 DialogueDirector; replaces G2 ReplayToAsync/ReplayResult
    bool IsReplaying { get; }
    void Arm(DialogueResumeState target, ReplayWatchdog watchdog);   // before the root starts; 2nd Arm or Arm after start → error, ignored
    event Action ReachedTarget;                 // synchronous, BEFORE the target line presents
    event Action<ReplayAbort> Aborted;          // engine turns replay off and presents whatever comes next
}
public enum ReplayAbort { DialogueEnded, OptionQueueEmpty, UnexpectedJump, LineIdMismatch, LineBudget, StallBudget, JournalMismatch }
public interface IVariableAccess { YarnVars GetAll(); void SetAll(YarnVars v); }  // impl: G2; SetAll = SetAllVariables(clear: true)
public sealed class ReplayWatchdog { /* exactly as API_G4 §1.4; OnNodeStart(isJump) is fed with RootTracker "root" events */ }
```

### 3.5 Input (G4) — **(prov. I0)**

```csharp
public interface IPointerSource { Vector2 ScreenPosition { get; } bool PointerVisible { get; } }
public static class PointerRay { public static Ray From(IPointerSource p, Camera cam); }
public interface IInputProvider { bool IsClickedThisFrame { get; } void Poll(); }
// Moved from [02]Script/Minigame/IInputProvider.cs. It is self-contained (checked). Keep it in the GLOBAL namespace so BaseMiniGame
// compiles unchanged. UnityInputProvider stays in Assembly-CSharp until G4-I9.
```

### 3.6 Not in Contracts (one home each)
| Type | Home | Note |
|---|---|---|
| `FlowPorts` bag, `StepContext`, `FlowState`, `Next`, `BarStep`, `StepCommand`, `FlowMilestone` | Flow | `FlowPorts { ICameraDirector Camera; IBarCounter Bar; IOrderDesk Orders; IMinigameHost Minigames; IGarnishHost Garnish; IStory Story; IDayUi DayUi; }` |
| `EventLogEntry` | Dialogue | G2's shape `{Kind, LineId, Substitutions, SpeakerId}` wins; G4 §1.6 "Edge" shape is dropped. Save sees it only as the `eventlog` source's `StateType`. |
| `SaveData`, `SaveMeta`, `SlotId`, `SaveContext`, … | Save | `SaveData` loses `RootStartSnapshots`. `SaveContext.DayPart` is the `DayPart` enum. |
| `UiSceneId`, `IUiSceneRegistry`, `UiSceneRegistry`, `UiSceneLoader` | UiScenes | `IUiScenes` interface dropped; GameRoot holds the concrete `UiSceneLoader`. |
| `UnlockPopup`, `IGarnishTable`, `GarnishTableCore` | Host | Host-only; `UnlockPopup` is used by `DayUiAdapter`. |
| `HubPhase` | Domain | No `IHub`: `MainWiring` calls `HubManager.SetPhase(state.HubPhase)` on `FlowState.Changed`. |

---

## 4. Behaviour decisions (one answer each)

### 4.1 Replay root = the corrected stack tracker (G4-S0)
G2's ponytail "root = the `RunNodeAsync` node, a jump is normal" is **superseded**. Roots change on every jump, as in G4.
`RootTracker` (pure C#, `Bar410.Dialogue`, fed by `onNodeStart`, `onNodeComplete`, line events and `onDialogueComplete`) **replaces `SaveLoadManager` lines 141–186**:
- `complete(x)`: pop the top; `resumed = stack.Count > 0 ? stack.Peek() : null`.
- `start(n)`: if `resumed != null && n == resumed` → **resume**, do nothing. Otherwise push `n`; if the stack was empty before the push → **root** (covers a fresh start, a jump, and a jump out of a detour).
- Any line clears `resumed`. `dialogueComplete` clears the stack. Depth = `stack.Count - 1`.
- A root raises `RootStarted` (live) or `ReplayWatchdog.OnNodeStart(isJump: true)` → `UnexpectedJump` (replay).
- EditMode test: the three spike logs (A/B/D/C, E/F/G, S/T) must give `ROOT(A)…ROOT(C)`, `E`,`G` and `S`. Still untested: the order through a real `DialogueRunner` (G2-T12 PlayMode covers it).

### 4.2 Replay policies (G2 table stands, with two fixes)
- `unlock_ingredient` = **Skip** and `ingredient_unlocked()` = **Journal** (G2). G4 §1.0 "runs during replay / not journaled" is superseded. Rule: **every function that reads C# state is Journal; `Run` only for functions that read nothing but Yarn state.**
- During replay a Journal entry applies `Writes` with `clear: false`. `RootStartVars` and `LineVars` are applied with `clear: true`.
- Target line: `ReachedTarget` runs synchronously (G4 restores `LineVars` + every snapshot), then Mode = Live. The target line is **not** appended to the event log (the restored log already has it), does **not** raise `LinePresented`, and keeps `LineIndex = target`. Leftover journal entries are dropped.

### 4.3 Flow ↔ features
- **Camera look:** `SetLookFree(bool)` only. `RefreshLook` = `Camera.SetLookFree(Step == AddIngredient && Bar.HasRoom)`. Everywhere G1 wrote `SetLookFixed(true)` → `SetLookFree(false)`.
- **Scoring:** `BartendingSession` calls `Orders.CommitServe()` once, after every step returned `Continue`. Remake never scores. `ClearGlass` in `finally` (after scoring); the G1 U8 check (`ServeDrink` reads no glass) still applies.
- **No order on 2.1 entry:** parity. `PrepareStation` calls `Orders.EnsureOrder()` (random + warning, as the old code did). Making it an error is a later one-line change. This keeps G1-15 parity strict.
- **Remake:** `BartendingSession` does `Bar.ResetDrink()` + `Garnish.ClearGlass()`, then restarts at `steps[0]`. Stations don't.
- **Ice:** `GarnishStation` calls `Bar.SetIce(r.Decoration.ice)` only when `r.Exit == Finished`. A forced `flow_garnish_done` (debug) returns a default decoration (no ice). Accepted.
- **Customer inset:** `PrepareStation` calls `Camera.AimCustomerInset(Orders.Order.Customer)` before `RunAsync` and `AimCustomerInset(CharacterName.None)` in `finally`. `ShowCustomer = Order.Customer != None` (the warning "add `<<order_customer>>`" stays). The host only shows or hides its `WorldView`.
- **Story start:** `DayRunner` calls `Story.StartStory(node)` (non-blocking, queued). G2's "DayRunner awaits `RunNodeAsync`" is superseded. `RunNodeAsync` stays public on the Director for tests.
- **`serve_order` / `wait_for_task`:** `director.StopSkip(); if (!session.TryBegin()) return;` (`wait_for_task` skips this part), then `while (session.IsRunning) await Awaitable.NextFrameAsync(ct);`. If `!Orders.Order.IsScored` afterwards → `Debug.LogError` (fault). This never hangs on `Faulted`. `wait_for_task` with no session → returns at once with a warning (the old code hung; deliberate).
- **Hub:** a single listener in `MainWiring` (`FlowState.Changed` → `HubManager.SetPhase(state.HubPhase)`).
- **Pointer mode (prov. I0):** stations do **not** push modes (Flow can't see Input). `MainWiring` maps `FlowState` → `PointerMode` (Talking=Direct, AddIngredient=Cursor, Minigame=Direct, Garnish/Serve=Cursor). The option presenter pushes Focus. Pause pushes Focus. Hosts push nothing.
- **Voice → speaking:** Host wires `director.SpeakerVoiceStarted/Ended(string id)` → `Enum.TryParse<CharacterName>(id)` (no warning, so "Player" is silent) → `stage.SetSpeaking`.
- **Order result → Yarn:** `MainWiring` keeps the old `OrderScored → TalkToCustomerManager.CommitOrderResult` and `OrderCleared → ResetVariables` rows (parity, no new writer). The writes happen inside `serve_order`, so its Journal diff records them.
- **Boot order (the only valid one):**
  - **`GameRoot.Awake`:** build Flow, the services and the verb resolver; `YarnVerbRegistry.Install` through `Director.Init`; `PendingLoad.TryTake(out data)` only **stores** the data.
  - **`GameRoot.Start`** (via `FlowAwait.Run`), in order:
    1. `await Language.InitAsync()`
    2. `await loader.PreloadAsync(ct)`
    3. `ports.Minigames = await loader.GetHostAsync<IMinigameHost>(UiSceneId.Minigame, ct)` (same for Garnish)
    4. if data was stored: `ResumeCoordinator.ResumeAsync(data, ct)` (`timeScale = 1`, `SetAll(RootStartVars)`, `Arm`, then `IDayResume.ResumeAsync`); else `Day.BootAsync(ct)`.
  - If R3 decides "UI stays in the main scene", `LegacyMinigames`/`LegacyGarnish` are assigned instead of step 3 (an Inspector change).

### 4.4 Save
- Saving is blocked when `DayPart ∈ {None, Event}`, or while replaying, skipping, with no root, or while loading. **Story is saveable** (the DayStart autosave happens there). G4's `"BeforeOpen"` entry is removed. Bartending is allowed, with the `ui.save.warn.drink_lost` warning.
- G2's `AtSaveableLine` is dropped. Eligibility = `SaveEligibility` + `HasRoot`, and the save always uses the last presented line.
- Resume always lands with `Step = Idle` (talking mode). A resume point captured while a session was running (test scripts that drive the bar with `flow_*` between lines; Y7 warns about it) loses the drink. Accepted.
- `FlowState.SelectedMinigame` and `LastMinigameOutcome` are not saved (`flow_minigame_result()` reads `""` after a load until the next drink). Accepted.
- Autosave: `AutosaveReason { DayStart, AfterServe }` (D10: EndOfDay dropped). `GameRoot` maps `Day.Milestone` (`DayStarted` → DayStart, `DrinkServed` → AfterServe) to `autosave.Request`.
- `#save_checkpoint` is dropped (it appears only in the test file `YarnFolder2/WaitLoop.yarn`). `IDialogueResumeSource` has no checkpoint member.
- Load: see "Boot order" in §4.3 (`TryTake` in Awake stores; resume or boot runs at the end of Start).
- File write (G4-S0): `File.WriteAllText(tmp)` → `File.Exists(dst) ? File.Replace(tmp, dst, null) : File.Move(tmp, dst)`. No `.bak`. Verified on the Linux Editor only.
- D9: the save stores the Yarn program hash; a mismatch refuses the load (with the dev-menu "ignore hash" toggle).

### 4.5 Dialogue, from G2-T02
- **New `CocktailLineProvider`** (`Bar410.Dialogue`, about 30 lines) replaces `UnityLocalisedLineProvider`. It takes the result synchronously when `op.IsDone`, keeps the `ExpandSubstitutions` → `ParseString` order, and exposes `bool LastFetchWasSync`. TA `StyleMarkerProcessor`/`PaletteMarkerProcessor` serialized `lineProvider` fields and `LocMarker` registration point at it.
- **Frame rule (all non-Live modes, SkipToOrder included):** if every presenter returns synchronously after an async fetch, Yarn throws `SignalContentComplete can only be called…` and hangs. The **Director** (the only presenter active in every mode) does all its bookkeeping synchronously, then `if (Mode != Live && !provider.LastFetchWasSync) await Awaitable.NextFrameAsync();`. Other presenters may return at once when not Live. G2's T05 checklist line "`RunLineAsync` never awaits" becomes "bookkeeping happens before the first await".
- **Retype:** cancel → await the old run to completion → `ContentWillDismiss` → `PrepareForContent` → new run. The naive restart **hangs** (it does not complete instantly). This depends on the prefab's TA `startTypewriterMode = FromScriptOnly`; write that into the presenter docs.
- **Command overrides:** remove-then-add at the runner (verified, including `params string[]` and `Awaitable` returns, and survival across `StartDialogue`). **A duplicate function *declaration* breaks every `.yarnproject` (fatal).** Declare only names with no old `[YarnFunction]`, plus an EditMode test: declared names ∩ `TypeCache` `[YarnFunction]` names = ∅.
- **`LocMarker`:** never call `WaitForCompletion` or sync `GetLocalizedString` (it re-enters Addressables). Use `GetLocalizedStringAsync(...)`: read `.Result` if `IsDone`, otherwise append the key + warn. Its output is **not re-parsed**, so `Data`/`UI` values must not contain markup (validator rule L7: no `[` in `Data` values).
- **`Language`:** `Set(string)` becomes `Awaitable SetAsync(string code, CancellationToken ct)`. It awaits the new locale's preloaded tables before raising `Changed` (the first TH fetch otherwise takes about 6 frames). `InitializationOperation` is not the signal.
- **Saliency:** set the runner's saliency strategy to a deterministic one (`Best`; G2-T05 confirms from the strategy source that it has no randomness). Today the prefab has `RandomBestLeastRecentlyViewed`. Current `.yarn` files use no line or node groups (grep), so this is future-proofing. Validator rule Y13 warns on line/node groups.
- **Thai:** every TMP that can show Thai needs font features `kern, mark, mkmk` (the prefab has `kern` only). **No Thai TMP font exists in the project** → task N7. ZWSP wrapping works (`Normal` wrap mode). TA wave on marks and Word mode are unverified → owner eyeball checklist in `Spike_G2-T02_Results.md`.
- **Input component:** one `ILineAdvancerInput`: G4's `DialogueAdvanceInput`, placed in `Bar410.Dialogue` (`Dialogue/Input/`). G2's `DialogueInput` is dropped. The pause gate is "Dialogue map disabled by `InputService`", with no `isPaused` func.

### 4.6 Core additions
```csharp
public static Awaitable FromCoroutine(MonoBehaviour runner, IEnumerator routine, CancellationToken ct); // FlowAwait; for CustomerStage fades. On ct: stop waiting + OCE (the coroutine runs to its end; G3 §2.7 accepts this)
```
Test notes (G1-0): use `[Test] async Task` for completion-source tests, and `[UnityTest]` only to step frames. `LogAssert.NoUnexpectedReceived()` also fails on earlier plain `Debug.Log`s in the same test. Raw `async void` exceptions are posted, so expect them a frame later; `FlowAwait.Run` logs synchronous throws synchronously. `UnloadSceneAsync` cancels `destroyCancellationToken` one frame **after** the call.

---

## 5. Changes vs each group spec (executors: apply these overrides)

### G1 (`API_G1_CoreFlow.md`)
1. §0.1, §1: no `Bar410.Data`. `SO_DaySchedule` goes to `Bar410.Domain` (G1-3). Flow refs Core, Domain, Contracts. Domain refs UnityEngine (not `noEngineReferences`).
2. §2.3: delete `MinigameKind`, `MinigameOutcome`, `ResumePoint`, and the `Enum.GetNames` assert. Use `E_Cocktail.Enum_MiniGameType` and the Domain `MinigameOutcome`. `DayPart` moves to Contracts. `Next`, `BarStep`, `StepCommand` and `FlowMilestone` stay.
3. §2.4: `HubKey` (string) → `HubPhase HubPhase { get; }` (Domain enum). `SelectedMinigame` is `Enum_MiniGameType`.
4. §2.5: the ports are replaced by the Contracts interfaces in §3.2 (`ICameraRig`→`ICameraDirector`, `IBar`→`IBarCounter`+`IOrderDesk`, `IMinigames`→`IMinigameHost`, `IGarnish`→`IGarnishHost`). `IStory` and `IDayUi` move to Contracts unchanged. `FlowPorts` per §3.6. Member mapping: `StartFreshDrink`→`BeginDrink`, `SetPicking`→`Open/CloseIngredients`, `Lock`→`LockIngredients`, `RefreshBottles`→`RefreshUnlocked`, `ResolveDrinkIdentity`→`ResolveDrink`, `SetOrderNote`→`SetOrderNoteAvailable`, `Serve`→`Orders.CommitServe`, `Filled/Emptied`→`ShakerFull/ShakerCleared`(+`ResetChosen`), `HasOrder`→`Orders.Order.HasOrder`, `EnsureOrder`→`Orders.EnsureOrder`.
5. §2.6/2.7: label-first stands; **no F1 fallback** (remove it from the G1-6 DoD). Stations follow §4.3 (look polarity, inset, ice, `MinigameRequest` built from `State.SelectedMinigame` + `Bar.RequiredMinigame`). `GarnishStation`: `Garnish.RunAsync(new GarnishRequest(Bar.ResolveDrink()))`, `Finished`→`Continue`. `ServeStation`: `Served`→`Continue`.
6. §2.8: remake reset and `ClearGlass` happen in the session. Scoring = `Orders.CommitServe()`.
7. §2.9: `DayRunner : IDayResume`. `BootAsync(CancellationToken ct)` (no `ResumePoint?`). `ResumeAsync(DayPart, string, ct)`. `CheckEventsAsync` calls `Bar.RefreshUnlocked()`.
8. §2.12: `GameRoot` has no `Register/TryGet/GetAsync`. It owns `UiSceneLoader` (`UiSceneRegistry.Set/Clear`), a private `Dictionary<Type, object>` as the verb resolver, `SaveService`/`ResumeCoordinator`/`AutosaveScheduler` and `PendingLoad`.
9. §2.13 `MainWiring`: adds the PointerMode listener (prov. I0) and the voice→speaking wiring. Keeps the `CommitOrderResult`/`ResetVariables` rows.
10. §2.14: `LegacyCameraRig` and `LegacyBar` are dropped (G3's `CameraDirector`/`CocktailFeature` replace them). Left: `LegacyMinigames : IMinigameHost`, `LegacyGarnish : IGarnishHost` (R3 fallback), `DayUiAdapter : IDayUi` (absorbs G3's `EventPopupsAdapter`, `ChapterTitleAdapter`, `UnlockPopup`).
11. §2.1: add `FlowAwait.FromCoroutine`, the double-await rule (NRE / silent hang) and the test notes of §4.6.

### G2 (`API_G2_Dialogue.md`)
1. §0: Dialogue refs Contracts/Domain/Input/Core; **not Flow**. Verbs refs as in §2.
2. A3: duplicate declaration is fatal → the ∩ = ∅ test is mandatory. Expect one ysls per asmdef.
3. A4/A6: `YarnVars`/`JournalEntry` use the Contracts shape (dictionaries, `Writes`, `Result`). `DialogueSavePoint` → `DialogueResumeState`. `CurrentVariables` (save-time) → `LineVars` (presentation-time).
4. A5: Director implements `IStory`, `IDialogueResumeSource`, `IReplayEngine`, `IVariableAccess`, `IVerbHost`. `ReplayToAsync`/`ReplayResult` → `Arm`/`ReachedTarget`/`Aborted`/`ReplayAbort`. `LinePresented` becomes `event Action` (read `CurrentLine`). `AtSaveableLine` is dropped. Root = `RootTracker` roots (ponytail note removed). Frame rule §4.5. Target-line rules §4.2. Line-time capture of `LineVars` + `DayPart` (GameRoot passes `Func<DayPart>`). Saliency `Best`.
5. A7: a non-Live return may be synchronous (the Director owns the frame rule). Retype hang note and `FromScriptOnly` requirement. The provider is `CocktailLineProvider`.
6. A9: `DialogueInput` dropped in favour of `DialogueAdvanceInput` (G4-I7, lives in Dialogue).
7. A12: `SetAsync` awaits the tables. A13: async read only; no markup in `Data`.
8. A15/T17 dropped (D1). §C policy table: `unlock_ingredient` Skip and `ingredient_unlocked` Journal confirmed. `serve_order`/`wait_for_task` per §4.3.
9. A16: add Y12 (a line after `flow_next_day` in the same node → warning: it cannot be a valid resume target), Y13 (line/node groups → warning), L7 (`[` in a `Data` value → error).
10. E (expected from G1): "flow snapshot + `ResumeAt`" is withdrawn (§3.1).

### G3 (`API_G3_Features.md`)
1. §1: `Adapters/` → `Host/Adapters/`. `SO_DaySchedule` in Domain is confirmed. `UiSceneId`/`IUiScenes`/`IUiSceneRegistry` live in `Bar410.UiScenes`, not Contracts (`IUiScenes` dropped).
2. §2.1: `IOrderDesk` adds `EnsureOrder()` and drops `WaitScoredAsync`. `IBarCounter`: no `Func` params (the adapter holds the unlocks), adds `CloseBook()`, and `MethodChosen` carries `Enum_MiniGameType`. `CocktailFeature` also implements `ISnapshotSource "order"` (customer, mode, RecipeKey, served type, result, `IsScored`; needs N4).
3. §2.4: timing per §3.2 (OCE synchronous on ct, Back immediate, background hide by run id, `IsBusy` definition).
4. §2.5: `IGarnishHost.ClearGlass()` added. `ResetDrink` on Remake moves to the session.
5. §2.6: `AimCustomerInset(CharacterName)` instead of `Transform`.
6. §2.7/2.8: `Capture/Restore` leave the interfaces; the adapters implement `ISnapshotSource` (`characters`, `unlocks`, both restored at the target only). Every mutating verb is idempotent (F13).
7. §2.9: `IHub`, `IEventPopups`, `IChapterTitle` are not created. `HubManager` stays unchanged. Popups and title are inside G1's `DayUiAdapter`.
8. T14 dropped (D6 default: the decoration stays on the GarnishTable glass).
9. `CameraDirector` also implements `ISnapshotSource "camera"`.

### G4 (`API_G4_SaveInput.md`)
1. §0: Contracts refs **Domain** too (not "UnityEngine core only").
2. §1.1: delete `SnapshotScope`. §1.6: delete `RootStartSnapshots`. §1.0: "replay-relevant = Yarn vars only" (functions are Journal; §4.2).
3. §1.2: no `CheckpointLineIndex`; `#save_checkpoint` is dropped.
4. §1.5: `JournalEntry.ResultJson` → `Result` (invariant `G9` for floats). `CameraShotId` is dropped. `$Yarn.Internal.*` keys are kept. float32 note.
5. §1.8 (S3): write path per §4.4. `File.Replace` throws `FileNotFoundException` when `dst` is missing; `File.Move` onto an existing file throws.
6. §1.9: blocked set `{None, Event}`. `SaveContext.DayPart` is the enum.
7. §1.12: `IDayResume.ResumeAsync(DayPart, rootNode, ct)` confirmed. Protocol step 1 drops "restore `RootStartSnapshots`".
8. §1.13: `AutosaveReason { DayStart, AfterServe }`. Requests come from `GameRoot` mapping `Milestone`.
9. §2.2/§(e): no `PushMode` inside stations; `MainWiring` owns the step→mode table. `DialogueAdvanceInput` lives in `Bar410.Dialogue`. `IInputProvider` moves to Contracts.
10. §0 "SaveLoadManager 141–186 can be reused" → replaced by `RootTracker` (§4.1).
11. Old `EventLogEntry` shape dropped (G2 shape).

---

## 6. New / changed tasks caused by the spikes and this reconciliation

| id | change | model | depends on |
|---|---|---|---|
| G1-0, G4-S0, G2-T02, G3-T0, N6 | **done** | — | — |
| G4-I0 | still open; I1, I2, I3, I8, G2-T09 and the PointerMode table stay provisional until it passes | O | owner's pad |
| G1-1 | skeleton per §2 (Core, Domain, Contracts, Flow, Tests; **no Data**) | H | N-RECON |
| G1-3 | move `SO_DaySchedule` into `Domain/` (Editor move, GUID kept) | H | G1-1 |
| G3-T1 / G4-S1 | one task: all of §3 into Contracts, + move the `IInputProvider` interface | S | **N1** (Contracts names Domain types) |
| G1-4, G1-5 | now depend on **N1** and G3-T1 (they use Domain enums / Contracts ports); G1-5 = `FlowPorts` + fakes only | S | N1, G3-T1 |
| G1-6 | DoD: F1 fallback removed | S | — |
| G1-11 | shrinks to `LegacyMinigames`, `LegacyGarnish`, `DayUiAdapter` | S (O reviews) | G3-T1 |
| G3-T4 | + `EnsureOrder`, `CloseBook`, `ISnapshotSource "order"` | S | N1, N4, G3-T1 |
| G3-T5 | + `ISnapshotSource "camera"`, `AimCustomerInset(CharacterName)` | H | G3-T1 |
| G3-T6 | shrinks to `UnlocksAdapter` (+ `ISnapshotSource "unlocks"`) and `HubManager` untouched | H | G3-T1 |
| G3-T14, G2-T17 | **dropped** (D6, D1 defaults) | — | — |
| G2-T03 | + `RootTracker` with an EditMode test on the 3 spike logs | S | — |
| **G2-T07a** (new) | `CocktailLineProvider` (`op.IsDone` fast path, `LastFetchWasSync`), repoint TA marker processors, register `LocMarker` | S | G2-T01 |
| G2-T04 | + ∩ = ∅ declaration test | O | — |
| G2-T05 | + frame rule (§4.5), saliency `Best`, `IStory`. The spike saw the hang with a single presenter only, so verify the frame rule with the real presenter list in both SkipToOrder and Replay | O | G2-T07a |
| G2-T07 | + retype hang note, `FromScriptOnly` | O | G2-T07a |
| G2-T09 | drops `DialogueInput` | S | G4-I7 (prov. I0) |
| G2-T11 | `Language.SetAsync` waits for the tables; `LocMarker` async read only | S | — |
| G2-T12 | Arm/events API; tracker roots; target-line rules; test that options during replay don't hang (option-line fetch is untested) | O | G2-T03, G4-S2 |
| G2-T14 / G2-T16 | + Y12, Y13 / + L7 | S | — |
| G2-T18 | still conditional: now decided by the owner's Thai eyeball checklist (TA wave on marks, Word mode) | S | N7 |
| **N7** (new) ⚙ | add a Thai TMP font asset (default **Noto Sans Thai, OFL**) as a fallback of LiberationSans SDF; set `kern, mark, mkmk` on every TMP that can show Thai (dialogue prefab first) | S | — (before G2-T11 testing) |
| G4-S3 | write path `Exists ? Replace(tmp,dst,null) : Move`; tests for both branches | S | — |
| G4-S5 / G4-S6 | protocol without root-start snapshots / two reasons only | O | as before |
| G4-I7 | `DialogueAdvanceInput` goes in `Assets/Bar410/Dialogue/Input/` | S | prov. I0 |

Notes for the coordinator:
- G1-0 side effect: HEAD has `m_EnterPlayModeOptionsEnabled: 1` while the editor reported it disabled, so the next project-settings save may write a diff. Don't commit it by accident.
- Spikes ran in the Editor only (Mono). Nothing was tested on IL2CPP/player builds or on Windows `File.Replace`.

---

## 7. D1–D14: defaults assumed (all §8 answers were blank)

| # | Default used |
|---|---|
| D1 | No "skip read lines" mode. `SeenLines`/G2-T17 dropped. |
| D2 | English `Name:` prefix = speaker ID; Thai display name from `character.<id>`. |
| D3 | `<<voice Isla huh>>`. |
| D4 | CSV with BOM; no xlsx script. |
| D5 | Minigame with no game → error log + `Completed`. |
| D6 | No separate bar glass: the decoration stays on the GarnishTable glass (old behaviour). |
| D7 | Garnish = two calls (`RunAsync` + `ServeAsync`). |
| D8 | **(prov. I0)** RS tap = next fixed view; edge-hover off with a pad. |
| D9 | Yarn program hash in the save; mismatch refuses (dev toggle to ignore). |
| D10 | EndOfDay autosave dropped (DayStart + AfterServe). |
| D11 | **(prov. I0)** G4 §2.1 defaults: Advance = South; Back = East; Pause = Start; MinigameAction = South or RT (mash); RS = camera views; parallax eases to centre with a pad; `#save_checkpoint` dropped. |
| D12 | Answered by action: the package is installed (`edd03af`). |
| D13 | Owner play checkpoints at the end of R3, R4 and R6. |
| D14 | Run phase by phase, with a stop and report after each phase. |
