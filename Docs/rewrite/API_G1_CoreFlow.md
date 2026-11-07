> **Superseded in part by [`Contracts_Final.md`](Contracts_Final.md) (2026-11-07): where they disagree, Contracts_Final wins.**

# Bar410 Rewrite — G1 Core Flow: API spec + task breakdown

Scope: level B, flow style ① (Async Stations on Unity `Awaitable`, plus an Inspector `Station[] steps` list), side-by-side in `Assets/Bar410/` with scene `Main_V2`, hand-wired `GameRoot`, no DI framework.
Sources read: `Docs/Bar410_Rewrite_Options.md` (§3, §4.1, §6, §9, §11–16), `Docs/Bar410_CodeMap_MainScene.md`, `Docs/Bar410_Refactor_Plan_Remove_HSM.md` (§4 tables A/B, §5, §5.1, §6, §9 checklist), `FlowWiring.cs`, `GameFlowCommands.cs`, `MinigameState.cs`, `SO_DaySchedule.cs`, and the public APIs of every manager FlowWiring touches.
Facts checked in this repo: Unity 6000.3.19f1, Yarn Spinner 3.2.5, UTF 1.6.0. **No project code is in an asmdef today**: everything is in Assembly-CSharp, except the third-party SerializedCollections package.

---

## 0. Decisions this spec makes (read first)

1. **The assembly boundary inverts part of Rewrite §3.1.** An asmdef cannot reference Assembly-CSharp, and every manager, `HubPhase`, `Enum_MiniGameType`, `MinigameOutcome`, `PanelTransition`, `ChapterTitleUI` and `EventPopupQueue` lives there. So:
   - `Bar410.Flow` owns small **port interfaces** and its **own label enums**, and has no reference to any manager.
   - **Adapters** (old manager → port) and **`GameRoot`** live in Assembly-CSharp. `Assets/Bar410/Host/` is a folder **without an asmdef**.
   - `Bar410.Host.asmdef` is added later, once the managers live in asmdefs (G3/R2). It is a one-file change.
   - This keeps old code untouched, because Assembly-CSharp auto-references every asmdef.
2. **Stations are MonoBehaviours** so that `Station[] steps` can be dragged in the Inspector. `DayRunner`, `BartendingSession`, `FlowState` and `FlowCommands` are **plain C#**, built by `GameRoot`, and can be tested in EditMode with fake ports. Unity cannot serialize interface fields, so stations get their ports through the `StepContext` passed to `RunAsync`. No `Init` is needed.
3. **One state source.** `FlowState` = (`DayPart`, `BarStep`, `SessionRunning`, minigame selection/result) with one `Changed` event. Two pure maps read it: the Yarn strings (`flow_phase`/`flow_step`) and the Hub key. A single Host listener calls `HubManager.SetPhase`.
4. **Flow has no Yarn reference.** Yarn verbs and compat shims belong to G2 (`Bar410.Dialogue`). They call `FlowCommands`, the Yarn-facing facade of `Bar410.Flow`, which holds the guards, the warnings and the string tables.
5. **Labels change before a command returns.** `PLAYTEST_Demo.yarn:88–101` reads `flow_step()` on the line straight after a flow command. This is the contract; spike S1 checks it.
6. **Remake always restarts at `steps[0]`.** There is no "remake target" field (YAGNI).

---

## 1. Assembly layers

| asmdef | Folder | References (allowed) | Contents | Owner |
|---|---|---|---|---|
| `Bar410.Core` | `Assets/Bar410/Core/` | UnityEngine only | `FlowAwait`, `Forcible<T>` | G1 |
| `Bar410.Domain` | `Assets/Bar410/Domain/` | none (`noEngineReferences: true`) | cocktail rules (R2) | G3/R2 |
| `Bar410.Data` | `Assets/Bar410/Data/` | Core, Odin DLLs (auto-referenced, verified `isExplicitlyReferenced: 0`) | `SO_DaySchedule` (moved here with its GUID), other SOs later | G1 owns the schedule; G3 owns the rest |
| `Bar410.Flow` | `Assets/Bar410/Flow/` | Core, Data | ports, `FlowState`, `DayRunner`, `BartendingSession`, `Station` + 3 stations, `FlowCommands` | G1 |
| `Bar410.Features.*` | `Assets/Bar410/Features/<X>/` | Core, Data, Domain | new feature code. **Must not reference Flow** | G3 |
| `Bar410.Dialogue` | `Assets/Bar410/Dialogue/` | Core, Data, **Flow**, YarnSpinner.Unity | verbs, shims, `DialogueDirector` (implements `IStory`) | G2 |
| `Bar410.Save` | `Assets/Bar410/Save/` | Core, Data, Flow (only for `DayPart` / `ResumePoint`) | snapshots, slots | G4 |
| *(none)* Host | `Assets/Bar410/Host/` | everything (Assembly-CSharp) | `GameRoot`, `MainWiring`, `Legacy*Adapter` | G1 |
| `Bar410.Flow.Tests` | `Assets/Bar410/Tests/Flow/` | Flow, Core, Data, nunit; Editor platform only | EditMode tests | G1 |

Rules, enforced by the compiler:
- Arrows point down only.
- Flow never sees Features, Dialogue, Save or Host.
- Features never see Flow. They implement ports only through Host adapters, or move the ports into a shared `Bar410.Contracts` asmdef if G3 prefers (see §5).
- **Flow must not use DOTween.** DOTween Modules are source files in Assembly-CSharp (`Assets/Plugins/Demigiant/DOTween/Modules`, no asmdef). Tweens stay behind ports.

---

## 2. Public types (per type: responsibility · signature · callers · deps · replaces · edge cases · test)

Shared conventions:
- Every async method returns `Awaitable` or `Awaitable<T>` and takes a `CancellationToken`.
- Nothing stores an `Awaitable` to await it later.
- Every wait on an event uses a **new** `AwaitableCompletionSource` for each wait.

### 2.1 `FlowAwait` (Bar410.Core, static)
- **Responsibility:** wait on a completion source with cancellation, and run fire-and-forget work without losing exceptions.
- **Signature:**
```csharp
public static class FlowAwait {
    public static Awaitable<T> Wait<T>(AwaitableCompletionSource<T> source, CancellationToken ct);
    public static Awaitable Wait(AwaitableCompletionSource source, CancellationToken ct);
    public static void Run(Func<CancellationToken, Awaitable> work, CancellationToken ct, string label, UnityEngine.Object context = null);
}
```
  - `Wait` = `using (ct.Register(() => source.TrySetCanceled())) return await source.Awaitable;`
  - `Run` is `async void` internally. It swallows `OperationCanceledException` and calls `Debug.LogException(e, context)` for anything else.
- **Callers:** `BartendingSession`, `DayRunner`, stations, `GameRoot`, G2 (`flow_next_day` shim), G3 hosts.
- **Deps:** UnityEngine.
- **Replaces:** the coroutine `while (...) yield return null` waits in `GameFlowCommands` and `FlowWiring.Start`.
- **Edge cases:**
  - If `ct` is already cancelled, `Register` fires at once and the await throws OCE. That is the wanted result.
  - Disposing the registration inside its own callback, on the same thread, must not deadlock (.NET handles this; the reviewer checks it).
  - Callers must never `await` the same `source.Awaitable` twice.
- **Test:** EditMode. Result passes through; a cancelled token throws OCE; a pre-cancelled token throws OCE; `Run` logs a thrown exception (`LogAssert.Expect`) and does not log OCE.

### 2.2 `Forcible<T>` (Bar410.Core, sealed, IDisposable)
- **Responsibility:** let a Yarn "press the button" command end a running host await with a chosen result. `Awaitable` has no `WhenAny`, so it cancels a linked token and returns the forced value.
- **Signature:**
```csharp
public sealed class Forcible<T> : IDisposable {
    public Forcible(CancellationToken outer);
    public CancellationToken Token { get; }   // pass to the host
    public bool IsForced { get; }
    public bool TryForce(T result);           // false if already forced or finished
    public Awaitable<T> Await(Awaitable<T> running); // on OCE: returns the forced value if forced and outer is not cancelled, else rethrows
    public void Dispose();
}
```
- **Callers:** `PrepareStation` (minigame), `GarnishStation`, `ServeStation`.
- **Deps:** Core.
- **Replaces:** nothing. The FSM transitioned synchronously.
- **Edge cases:**
  - `TryForce` after the host has already completed returns false, and the caller warns.
  - If the host ignores the token and completes normally after being forced, `Await` still returns the **forced** value, so the result is deterministic.
  - Outer cancellation always wins over a force.
- **Test:** EditMode, with a fake host built on a completion source that honours the token.

### 2.3 Label enums (Bar410.Flow, `FlowTypes.cs`)
```csharp
public enum Next     { Continue, Remake }
public enum DayPart  { None, Event, Story, Open, Close }              // label only, no transition table
public enum BarStep  { Idle, AddIngredient, Minigame, Garnish, Serve } // label only
public enum MinigameKind    { None, Shaking, Stiring, Building }      // same names and order as E_Cocktail.Enum_MiniGameType ("Stiring" spelling is load-bearing)
public enum MinigameOutcome { Completed, Cancelled }                  // same names as the old MinigameOutcome
public enum StepCommand { IngredientAdded, AnotherIngredient, DrinkComplete, GarnishDone, ServeDone, Remake }
public enum FlowMilestone { DayStarted /*Event finished, story about to start*/, DrinkServed, DayClosed }
public readonly struct ResumePoint { public readonly DayPart Part; public readonly string RootNode; public ResumePoint(DayPart p, string node); }
```
- **Replaces:** `GamePhase`, `OpenSub`, `PrepSub` and `BeforeOpenSub` as labels.
- **Edge cases:**
  - Adapters map `MinigameKind` ↔ `Enum_MiniGameType` and `MinigameOutcome` ↔ the old enum **by name**.
  - `MainWiring.Awake` asserts once that the names match (`Enum.GetNames` equality, `Debug.Assert`), so a rename fails loudly.
  - `DayPart` drops the old separate `BeforeOpen` container. Event and Story both report `"BeforeOpen"`.
- **Test:** EditMode, name-table sanity only. The cross-assembly assert runs in PlayMode in Host.

### 2.4 `FlowState` (Bar410.Flow, sealed class)
- **Responsibility:** the single observable flow state, plus the two pure maps (Yarn strings, Hub key).
- **Signature:**
```csharp
public sealed class FlowState {
    public DayPart Part { get; }
    public BarStep Step { get; }
    public bool SessionRunning { get; }
    public MinigameKind SelectedMinigame { get; }          // persists across runs and days (old PendingType)
    public string LastMinigameOutcome { get; }             // "" | "Completed" | "Cancelled"; cleared only when a minigame starts
    public event Action Changed;                           // raised after any field changes, once per Set* call
    internal void SetPart(DayPart p); internal void SetStep(BarStep s); internal void SetSession(bool running);
    internal void SelectMinigame(MinigameKind k); internal void SetMinigameOutcome(string o);
    public string PhaseString { get; }   // flow_phase()
    public string StepString  { get; }   // flow_step()
    public string HubKey      { get; }   // a HubPhase member name
}
```
- **Maps** (the exact old strings; source `GameFlowCommands.Yarn_CurrentPhase/Step`):

| State | `PhaseString` | `StepString` | `HubKey` |
|---|---|---|---|
| `Part==None` | `""` | `""` | `None` |
| `Event` | `BeforeOpen` | `Event` | `BeforeOpen` |
| `Story` | `BeforeOpen` | `StoryBeforeOpen` | `BeforeOpen` (old Story entry never set the hub) |
| `Open`, `Step==Idle` | `Open` | `TalkingWithCustomer` | `Talking` |
| `Open`, `Step!=Idle` | `Open` | `Step.ToString()` | `Step.ToString()` |
| `Close` | `Close` | `""` | `Close` |

- **Callers:** written only by `DayRunner`, `BartendingSession` and stations (internal setters, same assembly). Read by `FlowCommands`, `MainWiring` (Hub), G2 (`<<wait_step>>`, functions), G4 (save gating, `DayPart`) and the debug overlay (Odin `[ShowInInspector]` on `GameRoot`).
- **Deps:** none.
- **Replaces:** `GameFlowCommands.Yarn_CurrentPhase/Yarn_CurrentStep/Yarn_MinigameType/Yarn_MinigameResult`, `MinigameState.PendingType/LastOutcome`, and the `HubManager.SetPhase` calls spread through `FlowWiring`.
- **Edge cases:**
  - A new station needs a new `BarStep` value. It also needs a `HubPhase` member of the same name if it has its own Hub row; otherwise `MainWiring` warns and keeps the previous phase.
  - **`Changed` re-entrancy:** handlers must not call `Set*`. The reviewer checks this.
- **Test:** EditMode, the table above row by row, with `PLAYTEST_Demo.yarn` expectations as the oracle strings.

### 2.5 Ports (Bar410.Flow, `Ports/*.cs`) — what Flow needs from the rest of the game
All are sync verbs and events, except where the wait belongs to the feature (host awaits, UI waits).
```csharp
public interface ICameraRig {               // Legacy adapter → CameraManager
    void ShowBar(); void CutToBar(); void ShowShaker(); void ShowGarnish();
    void SetLookFixed(bool fixedLook); void ResetLook();
}
public interface IBar {                     // Legacy adapter → CocktailSystemManager, ShakerContents, ShakerPanelController,
                                            // IngredientButtonGroup, VisualizeCocktail, MethodPanel, BookUI_V2, Post_It_Order, IngredientUnlocks
    event Action Filled;                    // ShakerContents.OnFulled (adapter already did: ingredients off, method panel, book button on)
    event Action Emptied;                   // ShakerContents.Cleared (adapter already did: reset bars, hide method, bottles on)
    event Action<MinigameKind> MethodChosen;// Shake/Stir pressed (adapter already did SetMethod)
    bool HasRoom { get; }
    bool HasOrder { get; }
    void StartFreshDrink();                 // shaker.Clear() (fires Emptied), panel permissions reset, Order.ClearResult()
    void EnsureOrder();                     // parity: RandomCocktailIfNoOrder() + warning (Phase 5 turns it into an error)
    void SetPicking(bool on);               // 2.1 enter: RefreshBottles, SetInteractable, EnablePrepare, ShowMethod, UpdateBars / exit: off, HideMethod, ResetBars
    void Lock();                            // ingredients.Disable()
    void RefreshBottles();                  // ingredients.Refresh(unlocks.IsUnlocked)
    void CloseBook();
    void SetOrderNote(bool available);      // Post_It_Order.SetAvailable
    void SetShakerGrabbable(bool on);       // InteractableToggle.Apply(shaker, on)
    void ResolveDrinkIdentity();            // UpdateCocktailInShaker()
    void ResetDrink();                      // ResetCocktail()
    void Serve();                           // ServeDrink(): scores once, idempotent
}
public interface IMinigames {               // G3 host (additive scene, §15.6) or a legacy adapter over MinigameSystemManager
    // Resolves selected vs the drink's required kind itself, and shows/hides the customer inset itself.
    // Returns Completed AFTER the outro; returns Cancelled AS SOON AS Back is pressed (outro continues alone);
    // on ct cancel: starts cancel+outro on its own and throws OCE at once.
    Awaitable<MinigameOutcome> RunAsync(MinigameKind selected, CancellationToken ct);
}
public interface IGarnish {                 // G3 host or a legacy adapter over GarnishManager (pour + ice handled inside the adapter)
    void ClearGlass();
    Awaitable<Next> RunGarnishAsync(CancellationToken ct); // Open → Finish=Continue | Reset/Remake=Remake → Close (in its finally)
    Awaitable<Next> RunServeAsync(CancellationToken ct);   // OpenServe → Serve=Continue | Remake → CloseServe
}
public interface IStory {                   // G2 DialogueDirector
    int Day { get; }                        // $day
    void AdvanceDay();                      // $day+1 (journalled by G2 for replay)
    void StartStory(string node);           // must queue until the current dialogue is idle (old StartWhenIdle)
}
public interface IDayUi {                   // Legacy adapter → PanelTransition, EventPopupQueue+IngredientUnlocks, ChapterTitleUI
    Awaitable WaitTransitionAsync(CancellationToken ct);                      // returns when the opening transition ends (5 s cap kept)
    Awaitable ShowDayEventsAsync(CancellationToken ct);                       // TakeNewUnlocks → 1 popup → ShowAll → AllClosed; empty passes
    Awaitable ShowChapterTitleAsync(string chapterKey, bool wait, CancellationToken ct); // wait=false: start the fade, return at once
}
public sealed class FlowPorts {             // plain bag built once by GameRoot; no lookups at run time
    public ICameraRig Camera; public IBar Bar; public IMinigames Minigames; public IGarnish Garnish; public IStory Story; public IDayUi DayUi;
}
```
- **Callers:** stations and `DayRunner` (consumers); implemented in Host or by G2/G3.
- **Replaces:** the manager fields of `FlowWiring` and its translation handlers (`OnShakerFull/Cleared`, `OnMinigame*`, `OnPour/Ice/Finish/Serve/Remake`, `OnEventEntered`).
- **Edge cases:**
  - `IDayUi.ShowDayEventsAsync`: `EventPopupQueue.ShowAll` with an empty queue may raise `AllClosed` **synchronously**, so the adapter subscribes before calling it.
  - `IBar.StartFreshDrink` fires `Emptied` synchronously, so `PrepareStation` subscribes first (plan principle 5).
  - `IMinigames.RunAsync` timing contract: see §5, conflict C3.
  - `IGarnish`'s split into two awaits is required so that `flow_step` and the Hub still see Garnish and Serve separately (conflict C4).
  - **Destroyed objects:** after scene teardown, adapters may be called from `finally` blocks. Stations skip UI restore when `ctx.Lifetime` is cancelled; adapters must still be null-safe for destroyed Unity objects.
- **Test:** fakes in `Bar410.Flow.Tests`; the ports have no logic of their own.

### 2.6 `Station` (Bar410.Flow, abstract MonoBehaviour) and `StepContext`
- **Responsibility:** one drink-making step. Its entry and exit live in one method, and everything it opens is closed in `finally`.
- **Signature:**
```csharp
public sealed class StepContext {
    public FlowState State { get; }
    public FlowPorts Ports { get; }
    public CancellationToken Token { get; }    // session token (Load/cancel + lifetime)
    public CancellationToken Lifetime { get; } // GameRoot lifetime; if cancelled in finally → skip UI restore
}
public abstract class Station : MonoBehaviour {
    public abstract Awaitable<Next> RunAsync(StepContext ctx);        // synchronous until its first real await; sets ctx.State step label first
    public virtual bool Handle(StepCommand cmd, string arg) => false; // Yarn/compat "press"; true = handled. MUST set any new label synchronously
}
```
- **Callers:** `BartendingSession`.
- **Deps:** Flow, Core.
- **Replaces:** `StateBase`, `IState`, `StateMachine`, and every `*State` class's Enter/Exit + `FlowWiring` hook pair.
- **Edge cases:**
  - `RunAsync` must not await before setting its label (PLAYTEST:88 reads `AddIngredient` right after `flow_prepare_drinks`).
  - A step that throws is handled by the session (§2.7).
  - A station dragged into `steps` twice works, because each run makes its own completion sources and the station keeps no per-run fields across awaits except the current `Forcible`.
- **Test:** through the concrete stations.

### 2.7 `PrepareStation` / `GarnishStation` / `ServeStation` (Bar410.Flow, sealed : Station)
**PrepareStation** — steps 2 / 2.1 / 2.2. Old order is preserved.
- **Entry:**
  1. Subscribe `Filled`, `Emptied` and `MethodChosen`.
  2. `Bar.StartFreshDrink()`.
  3. `Camera.ResetLook()`, `Camera.ShowBar()`, `Bar.SetOrderNote(true)`.
- **Loop:**
  1. `Step=AddIngredient`.
  2. `Bar.EnsureOrder()`, `Bar.SetPicking(true)`, `RefreshLook()`.
  3. Await the method choice: a new completion source is resolved by `MethodChosen(kind)` (which also calls `State.SelectMinigame(kind)`) or by `Handle(IngredientAdded, arg)` (parses `arg` if it is not empty).
  4. `Bar.SetPicking(false)`, then `Step=Minigame`.
  5. `State.SetMinigameOutcome("")` and `Camera.SetLookFixed(true)`.
  6. Await `Forcible.Await(Minigames.RunAsync(State.SelectedMinigame, f.Token))`.
  7. **Completed:** set the outcome to "Completed" and return `Continue`.
  8. **Cancelled, or forced `AnotherIngredient`:** set the outcome to "Cancelled", call `Bar.SetShakerGrabbable(true)`, and loop.
  9. **Forced `DrinkComplete`:** set the outcome to "Cancelled" (parity: the old code recorded the cancelled run's late report), then return `Continue`.
- **`finally`:** unsubscribe; `Bar.SetPicking(false)` if we were still in 2.1; `Bar.CloseBook()`; `Bar.SetOrderNote(false)`; `Camera.SetLookFixed(true)`.
- **Handlers:**
  - `Emptied` → `Camera.ShowBar()` + `RefreshLook()`. This covers method-panel Reset (old `OnMethodResetClicked`).
  - `Filled` → `Camera.SetLookFixed(true)` + `Camera.ShowShaker()`.
- **`RefreshLook` (ADR 0002 Amendment):** `Camera.SetLookFixed(!(Step==AddIngredient && Bar.HasRoom))`.
- **`Handle`:**
  - `IngredientAdded` is valid only in 2.1.
  - `AnotherIngredient` and `DrinkComplete` are valid only in 2.2 (old `InMinigameStep`).
  - The label is set before the source is resolved. For `DrinkComplete`, the next label (Garnish) comes from the next station: rely on inline continuation (S1) or the S1 fallback.

**GarnishStation** — step 3.
- **Sequence:**
  1. `Step=Garnish`.
  2. `Bar.ResolveDrinkIdentity()`, `Camera.ShowGarnish()`, `Bar.Lock()`, `Bar.SetOrderNote(true)`.
  3. Return `await f.Await(Garnish.RunGarnishAsync(f.Token))`.
- **`finally`:** `Bar.SetOrderNote(false)`.
- **`Handle`:** `GarnishDone` forces `Continue`; `Remake` forces `Remake`.

**ServeStation** — step 4.
- **Sequence:**
  1. `Step=Serve`.
  2. Return `await f.Await(Garnish.RunServeAsync(f.Token))`.
- **`finally`:** `Camera.ShowBar()`.
- **`Handle`:** `ServeDone` forces `Continue`; `Remake` forces `Remake`.

Common to all three stations:
- **Callers:** `BartendingSession`, via the `steps` list on `GameRoot`.
- **Deps:** Flow ports.
- **Replaces:** `PrepareDrinksPhase`, `AddIngredientState`, `MinigameState`, `GarnishState`, `ServeState`, and `FlowWiring` §2–4 (`OnPrepareDrinks*`, `OnAddIngredient*`, `OnShaker*`, `OnShaking/Mixing/MethodResetClicked`, `OnMinigame*`, `RefreshLook`, `InMinigameStep`, `InAddIngredientStep`, `OnGarnish*`, `OnServe*`, `OnPour/Ice/Finish/ServeRequested`, `OnRemakeRequested`).
- **Edge cases:**
  - **Stale minigame results:** a late result from a forced or cancelled run must never count for the next run. The new `Forcible` and the new completion source per run guarantee this (old `InMinigameStep` guard).
  - **Pause during the minigame or Garnish:** no flow timers exist, so the only effect is on the hosts.
  - **Missing order:** `EnsureOrder` warns, as before.
  - **`Handle` called while not active:** returns false, and `FlowCommands` warns.
- **Test:**
  - EditMode with fake ports:
    - the label sequence for each command (PLAYTEST:88–101)
    - `RefreshLook` truth table
    - subscribe happens before `StartFreshDrink`
    - `finally` closes everything on cancel
    - a stale result is ignored
  - PlayMode: checklist §9 items 3–10 in `Main_V2`.

### 2.8 `BartendingSession` (Bar410.Flow, sealed class)
- **Responsibility:** run the `steps` list until a drink is served; on Remake, restart from `steps[0]`; score once.
- **Signature:**
```csharp
public sealed class BartendingSession {
    public BartendingSession(IReadOnlyList<Station> steps, FlowPorts ports, FlowState state, CancellationToken lifetime, Action enterTalkingMode);
    public bool IsRunning { get; }                  // G4 save gating (replaces static CocktailSystemManager.IsWaitingForTask)
    public Station Current { get; }
    public event Action DrinkServed;                // after Bar.Serve(); G2 serve_order / G4 autosave
    public event Action<Exception> Faulted;         // a station threw; talking mode already restored
    public bool TryBegin();                         // false + warning if running or steps empty; runs synchronously up to the first await
    public void Cancel();                           // Load / scene change; finally blocks restore UI unless lifetime is cancelled
    public bool Handle(StepCommand cmd, string arg = null); // routes to Current; false if none or not handled
}
```
- **Loop:**
  1. `SetSession(true)`, then `Garnish.ClearGlass()`.
  2. Run each step in order.
  3. On `Remake`: `Bar.ResetDrink()`, `Garnish.ClearGlass()`, restart.
  4. When every step returns `Continue`: `Bar.Serve()`, then `DrinkServed`.
- **`finally`:**
  1. `Current = null`, `Garnish.ClearGlass()`, `Step=Idle`, `SetSession(false)`, dispose the CTS.
  2. If the lifetime is still alive: `enterTalkingMode()`.
- **Callers:** `FlowCommands.PrepareDrinks()` (Yarn `flow_prepare_drinks`); G2's `serve_order` (`TryBegin` + await `DrinkServed`).
- **Deps:** Flow, Core.
- **Replaces:** `OpenBarPhase` sub-machine; `FlowWiring.OnOpenBarTransition` (scoring hook), `OnTableReset`, `OnConversationExited` warning (no order → warning in `TryBegin`).
- **Edge cases:**
  - **Duplicate `flow_prepare_drinks`:** warns and is refused.
  - **A station throws:** `finally` restores talking mode, `Faulted` fires, `FlowAwait.Run` logs it. **`wait_for_task` would hang**, because the order is never scored, so G2 must listen to `Faulted` (or `IsRunning` going false without `DrinkServed`) and release with a logged error.
  - **Scoring order vs the old code:** the old code cleared the glass (Serve exit) **before** `ServeDrink()`; the new code scores and then clears in `finally`. The reviewer confirms `ServeDrink` does not read the glass.
  - **Tutorial steps list without Serve:** still scores at the end. That is intended.
  - **`steps` length 0:** `TryBegin` returns false with an error.
- **Test:** EditMode with fake stations (`new GameObject().AddComponent<FakeStation>()` works in EditMode):
  - Remake from step 2 and from step 3 restarts at 0
  - `Serve` is called exactly once
  - `enterTalkingMode` is called once after serve, after cancel and after a fault
  - `IsRunning` is true synchronously after `TryBegin`

### 2.9 `DayRunner` (Bar410.Flow, sealed class)
- **Responsibility:** the day around the bar: boot, Event, Story, Open (talking mode + chapter title), Close, next day, resume after load.
- **Signature:**
```csharp
public sealed class DayRunner {
    public DayRunner(FlowPorts ports, SO_DaySchedule schedule, FlowState state, BartendingSession session, CancellationToken lifetime);
    public event Action<FlowMilestone> Milestone;
    public Awaitable BootAsync(ResumePoint? resume, CancellationToken ct); // Lock, Part=None, await DayUi.WaitTransitionAsync, then StartDay or Resume
    public Awaitable StartDayAsync(CancellationToken ct);  // cam look fixed+reset+cut, ShowDayEvents, Part=Story, Milestone(DayStarted), Story.StartStory(node of Story.Day)
    public Awaitable CheckEventsAsync(CancellationToken ct); // guard Part==Story; Part=Event, Lock, Bar.RefreshBottles, await DayUi.ShowDayEventsAsync, Part=Story
    public Awaitable OpenBarAsync(bool waitForTitle, CancellationToken ct); // guard Part==Story; Part=Open; EnterTalkingMode; Camera.ShowBar; ShowChapterTitleAsync(key, waitForTitle)
    public bool CloseBar();                                 // guard Part==Open && !session.IsRunning; Part=Close; Milestone(DayClosed)
    public bool NextDay();                                  // guard Part==Close; Story.AdvanceDay; FlowAwait.Run(StartDayAsync) — NEVER awaited by Yarn
    public Awaitable ResumeAsync(ResumePoint p, CancellationToken ct); // G4: Story/Open/Close modes without Event or chapter title, then Story.StartStory(p.RootNode)
    public void EnterTalkingMode();                         // Step=Idle; Bar.Lock; Camera.SetLookFixed(true); Bar.CloseBook; Camera.ShowBar
}
```
- **Callers:** `GameRoot.Start` (`BootAsync` via `FlowAwait.Run`); `FlowCommands` (Yarn shims); `BartendingSession` (`enterTalkingMode` delegate); G4 (`ResumePoint`).
- **Deps:** Flow, Data (`SO_DaySchedule`).
- **Replaces:**
  - `GameLoopFSM`, `BeforeOpenPhase`, `EventState`, `StoryBeforeOpenState` (`IsFirstEntry`/`ResetDay`), `ClosingBarPhase`
  - `FlowWiring.Start`, `OnBeforeOpenEntered`, `OnEventEntered`, `OnEventPopupsClosed`, `OnStoryEntered`, `OnOpenEntered`, `OnTalkingEntered`, `OnClosingEntered/Exited`, `TryGetToday`, `LockBar`
  - `GameFlowCommands.OpeningBusy`
- **Edge cases:**
  - **Every guard warns, and never stays silent** (`"[Flow] flow_open_bar ignored: not in Story (now Event)"`). This replaces the "Illegal transition" warnings.
  - **Missing schedule row:** error log, story not started (as before). A null schedule is impossible because GameRoot marks it `[Required]`.
  - **Null transition:** the old code never started the day; the new code starts at once (**deliberate change**).
  - **Order of the start-of-day camera cut:** the old code ran `OnEventEntered` before the BeforeOpen cut (CodeMap §8.1). The new code cuts first (**deliberate**). R0 must check that nothing depended on the old order.
  - **The `NextDay` deadlock trap:** `StoryStart` of the new day must queue behind the still-running old dialogue. If the Yarn command awaited it, the dialogue could never end, so the shim must not await.
  - **Talking mode runs before the chapter title**, matching the old order (Talking Entered before Open Entered).
  - **Skip:** G2 passes `waitForTitle = !IsSkipping`.
  - **Re-entrancy:** `CheckEventsAsync` while in Event is refused by the guard. `ResumeAsync` with `Part` None or Event logs an error and does `StartDayAsync`.
- **Test:**
  - EditMode with fakes:
    - the `PhaseString`/`StepString` sequence through boot, CheckEvents, Open, Close and NextDay matches PLAYTEST/TEST_AllCommands
    - each guard refuses and warns
    - `NextDay` does not block
    - a missing row is logged
  - PlayMode: checklist items 1, 2, 12, 13.

### 2.10 `FlowCommands` (Bar410.Flow, sealed class) — the Yarn-facing compat surface
- **Responsibility:** the one door G2's verbs and shims call. It holds the old command semantics, guards, warnings, minigame-name parsing and Yarn strings. It has no Yarn attributes.
- **Signature:**
```csharp
public sealed class FlowCommands {
    public FlowCommands(DayRunner day, BartendingSession session, FlowState state, CancellationToken lifetime);
    public Awaitable OpenBarAsync(bool waitForTitle, CancellationToken ct);   // flow_open_bar
    public Awaitable CheckEventsAsync(CancellationToken ct);                  // flow_check_events
    public void NextDay();                                                    // flow_next_day (fire-and-forget)
    public void CloseBar();                                                   // flow_close_bar
    public void PrepareDrinks();                                              // flow_prepare_drinks (G2 calls DialogueControls.StopSkip before)
    public void IngredientAdded(string minigameType = "");                    // flow_ingredient_added [type]
    public void AnotherIngredient(); public void DrinkComplete();             // flow_another_ingredient / flow_drink_complete
    public void GarnishDone(); public void ServeDone(); public void RemakeDrink(); // flow_garnish_done / flow_serve_done / flow_remake_drink
    public void SelectMinigame(string type);                                  // flow_minigame X (selection only, valid any time)
    public string Phase { get; } public string Step { get; }                  // flow_phase() / flow_step()
    public string MinigameType { get; } public string MinigameResult { get; } // flow_minigame_type() / flow_minigame_result()
    public static bool TryParseMinigame(string raw, out MinigameKind kind);   // "Mixing"/"Stirring" → Stiring; enum names case-insensitive; else warn
}
```
- **Callers:** G2 shim and verb layer; debug hotkeys; an optional UI button binder (add only when a button needs it).
- **Deps:** Flow.
- **Replaces:** `GameFlowCommands` (all instance methods + static wrapper bodies + `TryParseMinigame`). The `Instance` static and `CommandTarget()` replay gating move to G2's `YarnVerbs` (`ReplayPolicy`).
- **Edge cases:**
  - A button command nobody handled warns: `"[Flow] flow_garnish_done ignored: not in Garnish (now AddIngredient)"`.
  - `RemakeDrink` is valid only in Garnish or Serve.
  - `MinigameType` returns `SelectedMinigame.ToString()` (`"None"` before any selection, never `""`).
  - `MinigameResult` is `""` before the first run and **survives session end** (PLAYTEST:107).
  - An unknown minigame name leaves the selection unchanged.
- **Test:** EditMode: parse aliases; warnings via `LogAssert`; strings.

### 2.11 `SO_DaySchedule` (moved into Bar410.Data, GUID kept)
- **Responsibility:** day → start node + chapter key.
- **Signature:** unchanged: `bool TryGet(int day, out Day row)`; `Day { int day; string startNode; string chapterName; }`.
  - The `chapterName` value is treated as a **localization key** once G2's String Table exists (G2 owns renaming or `[FormerlySerializedAs]`).
- **How it moves:** in the Unity Editor (never with shell). The old `FlowWiring` keeps compiling, because Assembly-CSharp sees the asmdef. The four `DaySchedule_*.asset` files keep working because the GUID is unchanged.
- **Edge cases:** the file depends only on Odin and UnityEngine. Verify there are no other Assembly-CSharp deps before moving.
- **Test:** compile + open each asset.

### 2.12 `GameRoot` (Host, Assembly-CSharp, MonoBehaviour, `[DefaultExecutionOrder(-1000)]`)
- **Responsibility:** the composition root. It builds Flow by hand, owns the lifetime, starts the day, and is the late-registration point for additive UI scenes.
- **Signature:**
```csharp
public sealed class GameRoot : MonoBehaviour {
    public static GameRoot Current { get; }                  // scene-scoped; duplicate → error + Destroy(this); no DontDestroyOnLoad
    public CancellationToken Lifetime { get; }               // destroyCancellationToken
    public FlowState State { get; } public FlowCommands Commands { get; }
    public DayRunner Day { get; } public BartendingSession Session { get; }
    public void Register<T>(T service) where T : class;      // additive scenes in Awake/OnEnable; duplicate type → error
    public void Unregister<T>(T service) where T : class;    // OnDisable/OnDestroy; ignored if not the registered instance
    public bool TryGet<T>(out T service) where T : class;
    public Awaitable<T> GetAsync<T>(CancellationToken ct) where T : class; // waits for a late Register (additive scene load)
    // Inspector (Odin [Required]): Station[] steps; SO_DaySchedule schedule; adapters (as concrete types or Component + [ValidateInput]); G2 director; managers for MainWiring
}
```
- **Awake:**
  1. Set `Current`.
  2. Build `FlowState`, `FlowPorts` (from serialized adapters), `BartendingSession`, `DayRunner`, `FlowCommands`.
  3. Run `MainWiring.Bind(...)`.
  4. Bind G2 (`YarnVerbs.Bind(Commands, ...)`).
  - Stations need no `Init`: `StepContext` carries the ports.
- **Start:**
  1. `FlowAwait.Run(BootAsync)`: `Ports.Minigames ??= await GetAsync<IMinigames>(ct)` (and the same for `IGarnish`) after G3 loads its UI scenes.
  2. G4 supplies `ResumePoint?` (`save.TakePendingResume()`).
  3. `Day.BootAsync(resume)`.
- **Callers:** Unity; G2 (bind), G3 (Register), G4 (resume, `Session.IsRunning`, `Day.Milestone`).
- **Deps:** everything (Assembly-CSharp).
- **Replaces:** the `[GameLoop]` root components (`GameLoopFSM`, `FlowWiring`, `GameFlowCommands`) and 8 scene statics as entry points (they are wrapped, not deleted, in level B).
- **Edge cases:**
  - **`Register` after boot:** the port bag is filled once at boot, so a re-registered host (UI scene reloaded mid-day) is not picked up. This is accepted: scenes are preloaded and kept. If hot reload becomes a need, `FlowPorts.Minigames`/`Garnish` become getters over `TryGet`.
  - `GetAsync` must honour `ct` and complete for registrations that happened before the call.
  - **Two `GameRoot`s:** the second destroys itself.
  - **Destroy mid-day:** `Lifetime` is cancelled → every await throws OCE → `finally` blocks skip UI restore.
- **Test:**
  - PlayMode only (Assembly-CSharp cannot be referenced by a test asmdef), using an Editor smoke menu or a Unity MCP run in `Main_V2`.
  - `Register/GetAsync` logic stays tiny. If it grows, move it into `Bar410.Core` as `ServiceSlots` and test it there.

### 2.13 `MainWiring` (Host, static or plain class) — the always-on wiring (Refactor Plan table B)
- **Responsibility:** manager↔manager links that are not tied to a step, plus the Hub listener.
- **Signature:** `public static void Bind(GameRoot root, Refs r)` and `Unbind(...)`. It is called from `GameRoot.Awake` and `OnDestroy`.

| Table B row | Lands in |
|---|---|
| `ShakerContents.Changed` → `UpdateCocktailBars` | **`LegacyBarAdapter`** (always on is harmless: only fires while adding) |
| `ShakerContents.Cleared` → reset bars, hide method, bottles on (+ cam bar, RefreshLook) | adapter does the bar part, raises `Emptied`; `PrepareStation` does cam + look |
| `MethodPanel.ResetChosen` → `ResetCocktail`, update bars, hide panel, cam bar | adapter (bar part, then `Emptied` via Cleared); `PrepareStation` (cam + look) |
| Book button `onClick` → `BookUI_V2.SetActive(true)` | `MainWiring` |
| `IngredientUnlocks.Changed` → `RefreshBottles` | `MainWiring` |
| `UsePreferences(customers.Preferences)`, `OrderScored → CommitOrderResult`, `OrderCleared → ResetVariables`, `UseRecipeFilter(unlocks.IsRecipeComplete)` | `MainWiring` (Awake / OnDestroy) |
| `RefreshLook` rule (ADR 0002 Amendment) | `PrepareStation` |
| Minigame `Cancelled` → shaker grabbable, `HideCustomerInset` | `PrepareStation` (`SetShakerGrabbable`), host (inset) |
| Minigame result → `flow_minigame_result()` / `_type()` | `FlowState` via `PrepareStation` |
| `InMinigameStep` stale-result guard | `Forcible` + a completion source per run |
| `RandomCocktailIfNoOrder()` on 2.1 entry | `IBar.EnsureOrder()` (parity; Phase 5 makes it an error) |
| `HubManager.SetPhase` on every phase | `MainWiring`: `State.Changed += () => hub.SetPhase(Parse(State.HubKey))`, set once at bind (`None`) |

- **Edge cases:**
  - Unbind in `OnDestroy`, in reverse order.
  - Hub parse failure → warning once per key.
  - The `HubPhase`/enum name assert (§2.3) lives here.
- **Test:** PlayMode checklist items 3, 4, 13 plus Hub visibility per step.

### 2.14 Legacy adapters (Host, MonoBehaviours, one file each)
`LegacyCameraRig : ICameraRig`, `LegacyBar : IBar`, `LegacyMinigames : IMinigames`, `LegacyGarnish : IGarnish`, `LegacyDayUi : IDayUi`.
- **Responsibility:** 1:1 translation of old manager calls (exact calls listed in §2.5 comments).
- **Replaces:** the remaining bodies of `FlowWiring`.
- `LegacyMinigames` / `LegacyGarnish` are the R3 fallback if G3's additive-scene hosts are late. They implement the same ports, so swapping one for the other is an Inspector change.
- **Edge cases:**
  - The `FlowWiring.OnShakerFull` null-unsafe duplicates are fixed here (no `_ingredients.Disable()` without a null check, no double calls).
  - `LegacyMinigames.RunAsync` resolves `Resolve(selected, shaker.RequiredMinigame)`, shows the inset first, and completes on `MinigameFinished`/`BackRequested` with a **new** source per run. On ct cancel it calls `CancelMinigame()` if not Idle.
- **Test:** PlayMode checklist.

---

## (a) Dependency diagram

```
                 Bar410.Core (FlowAwait, Forcible)            Bar410.Domain (pure)
                    ▲      ▲        ▲        ▲                     ▲
        Bar410.Data (SO_DaySchedule, SOs) ───┼─────────────────────┤
                    ▲      ▲                 │                     │
              Bar410.Flow  │           Bar410.Features.* (G3) ─────┘   (Features ✗→ Flow)
   (FlowState, ports,      │
    DayRunner, Session,    │
    Stations, FlowCommands)│
          ▲         ▲      │
   Bar410.Dialogue  Bar410.Save
   (G2: verbs, shims,  (G4: slots, uses DayPart/ResumePoint)
    DialogueDirector : IStory)
          ▲         ▲      ▲           ▲
          └─────────┴──────┴───────────┘
             Host = Assembly-CSharp (no asmdef yet)
             GameRoot · MainWiring · Legacy*Adapter
                         │ calls
                         ▼
             old managers (Assembly-CSharp, untouched)

Runtime calls: Yarn → G2 YarnVerbs → FlowCommands → DayRunner / BartendingSession → Station → ports → adapters/hosts → managers
Events back:   managers → adapters → port events / host Awaitables → Station; FlowState.Changed → MainWiring → HubManager
```

---

## (b) Task breakdown (ordered)

Each task ends with: compile check (`read_console` has no errors), then a Systems Guide / CLAUDE.md component-table update where a component is added. Model key: **haiku** = mechanical, **sonnet** = normal implementation, **opus** = concurrency / architecture.

| id | Task | Files to create | Inputs | Definition of done | Diff | Model | Why |
|---|---|---|---|---|---|---|---|
| G1-0 | **Spike S1–S6** (throwaway, not committed): S1 inline continuation of `TrySetResult`/`TrySetCanceled` (oracle: PLAYTEST:88–101 sequencing); S2 Yarn command returning `Awaitable` blocks the line until complete; S3 `async void` exception surfacing vs `FlowAwait.Run`; S4 `destroyCancellationToken` cancels a pending `FlowAwait.Wait` on scene unload; S5 UTF 1.6 `async Task` tests vs `[UnityTest]` yielding `Awaitable`; S6 `Awaitable` in EditMode tests without a player loop | scratch script only | §(d) list | written results appended to this file / a Docs note; if S1 fails → enable fallback F1 in G1-6 | 2 | opus | results decide contracts; subtle runtime semantics |
| G1-1 | asmdef skeleton + folders | `Assets/Bar410/{Core,Data,Flow,Tests/Flow}/*.asmdef`, empty `Host/` folder | §1 table | compiles; `Bar410.Flow` cannot see `CocktailSystemManager` (deliberate test: a temp reference fails) | 1 | haiku | boilerplate |
| G1-2 | `FlowAwait` + `Forcible<T>` + EditMode tests | `Core/FlowAwait.cs`, `Core/Forcible.cs`, `Tests/Flow/FlowAwaitTests.cs` | G1-0 S1/S3/S5 | tests green (results, cancel, pre-cancel, force-after-complete, outer-wins, logging) | 3 | opus | cancellation races, double-await hazards |
| G1-3 | Move `SO_DaySchedule` into `Bar410.Data` (in the Editor, GUID kept) | move `SO_DaySchedule.cs` (+ .meta) | — | old scene still plays day 1; 4 assets open with data | 1 | haiku | mechanical, but must be done in the Unity Editor |
| G1-4 | `FlowTypes` + `FlowState` + map tests | `Flow/FlowTypes.cs`, `Flow/FlowState.cs`, `Tests/Flow/FlowStateTests.cs` | §2.3/2.4 table, PLAYTEST strings | every table row tested; `Changed` fires once per set | 2 | sonnet | small but load-bearing strings |
| G1-5 | Ports + `FlowPorts` + test fakes | `Flow/Ports/*.cs` (6 interfaces + bag), `Tests/Flow/Fakes/*.cs` | §2.5 | compiles; fakes record calls in order | 2 | sonnet | interface design already fixed; fakes need care for event ordering |
| G1-6 | `Station`, `StepContext`, `GarnishStation`, `ServeStation` | `Flow/Station.cs`, `Flow/Stations/GarnishStation.cs`, `ServeStation.cs` | G1-2, G1-5 | EditMode: labels, forced results, finally on cancel; **F1 fallback** if S1 failed: `FlowCommands` button methods become `Awaitable` and wait ≤ N frames for the label to change | 3 | sonnet | pattern is set by `Forcible`; moderate |
| G1-7 | `PrepareStation` | `Flow/Stations/PrepareStation.cs`, tests | G1-6, §2.7 | EditMode: subscribe-before-Clear, RefreshLook truth table, 2.1⇄2.2 loop, stale result ignored, outcome strings, grabbable on cancel | 4 | opus | the trickiest state: two nested waits, forced vs natural ends, parity edges |
| G1-8 | `BartendingSession` | `Flow/BartendingSession.cs`, tests | G1-6/7 | EditMode: remake restart, single Serve, talking mode after serve, cancel and fault, synchronous `IsRunning`/label | 4 | opus | lifetime / finally / fault semantics |
| G1-9 | `DayRunner` | `Flow/DayRunner.cs`, tests | G1-4/5/8 | EditMode: boot→Story→Open→Close→NextDay string sequence matches PLAYTEST/TEST_AllCommands; all guards warn; NextDay non-blocking; ResumeAsync modes | 4 | opus | ordering, the next-day deadlock trap, resume |
| G1-10 | `FlowCommands` | `Flow/FlowCommands.cs`, tests | G1-8/9 | EditMode: each old command's warning text; parse aliases; result survives session end | 2 | sonnet | straight port of `GameFlowCommands` |
| G1-11 | Legacy adapters | `Host/LegacyCameraRig.cs`, `LegacyBar.cs`, `LegacyMinigames.cs`, `LegacyGarnish.cs`, `LegacyDayUi.cs` | old manager APIs (§2.5 comments), `FlowWiring.cs` | compile; each method maps to the exact old call; null-safe | 3 | sonnet | mechanical, apart from `LegacyMinigames`/`LegacyDayUi` (completion sources, subscribe-before-ShowAll); have opus review those two |
| G1-12 | `GameRoot` + `MainWiring` | `Host/GameRoot.cs`, `Host/MainWiring.cs` | G1-8..11, G2 bind API, G4 resume API (stub ok) | Odin `[Required]` all green; duplicate root destroys itself; Hub follows state; table B rows bound/unbound | 3 | sonnet | hand wiring; GetAsync is small |
| G1-13 | `Main_V2` scene | `Assets/Bar410/Content/Scenes/Main_V2.unity` (copy of `New Drag Drop System`; on the `[GameLoop]` **instance**, remove `FlowWiring` → `GameFlowCommands` → `GameLoopFSM` as overrides; never edit the prefab) + `GameRoot` object | G1-12, G2 shim (or stub) | `Main_V2` boots to Story on day 1 with no errors; the old scene is unchanged (`git diff` of the old .unity is empty) | 2 | sonnet | scene surgery via Unity MCP, careful with the prefab |
| G1-14 | Debug hotkeys (editor only) | `Host/FlowDebugHotkeys.cs` (Input System `Keyboard.current`, `#if UNITY_EDITOR`) | `FlowCommands` | keys 1–9/0 drive the same commands as before | 1 | haiku | mechanical port of `GameFlowDebugHotkeys` |
| G1-15 | PlayMode parity run | `Docs/Bar410_Rewrite_G1_Parity.md` (results log) | R0 reference file of `flow_phase/step`, checklist §9 | `PLAYTEST_Demo` + `TEST_AllCommands` in `Main_V2` print the same strings as the R0 baseline; checklist 1–18 pass or are listed as not tested | 3 | sonnet | execution + honest reporting |
| G1-16 | Docs | `Docs/adr/0004-flow-rewrite.md`, Systems Guide section, CLAUDE.md architecture + component table | all | docs state: Host is Assembly-CSharp for now; FlowWiring rule superseded | 1 | haiku | writing to a known outline |

Order: G1-0 → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12 → 13 → 14 → 15 → 16.
- G1-3 and G1-14 can slot in anywhere.
- The R3 kill-criteria check happens after G1-15, together with G3.

---

## (c) Opus reviewer checklist per task

- **G1-0**
  - Every UNVERIFIED item in (d) has a written PASS/FAIL with the snippet used.
  - The S1 result sets either the "label first" or the F1 fallback.
- **G1-1**
  - No asmdef references Assembly-CSharp-only code.
  - `Bar410.Flow` has no YarnSpinner, DOTween, Cinemachine or InputSystem references.
  - The test asmdef is Editor-only and has `UNITY_INCLUDE_TESTS`.
- **G1-2**
  - `Wait` disposes the registration on every path.
  - No `Awaitable` is stored or awaited twice.
  - `Run` never rethrows, logs with context, and stays silent on OCE.
  - `Forcible`: outer cancel beats force; force after complete returns false; the linked CTS is disposed.
- **G1-3**
  - The GUID is unchanged (`.meta` moved with the file).
  - The old `FlowWiring` still compiles.
  - All 4 schedule assets still show their rows.
- **G1-4**
  - Strings are byte-identical to `GameFlowCommands` (`TalkingWithCustomer`, `StoryBeforeOpen`, `""` in Close and None).
  - `HubKey` for Story = `BeforeOpen`.
  - No setter is public.
- **G1-5**
  - Ports carry no Assembly-CSharp types.
  - Every port method maps to one or a few old calls (no logic hiding in ports).
  - `IMinigames` timing contract is written in the XML doc.
- **G1-6**
  - Labels are set before the first await.
  - `finally` restores only what the station opened, and skips the restore when `Lifetime` is cancelled.
  - `Handle` returns false outside its step.
- **G1-7**
  - Subscribe happens before `StartFreshDrink`.
  - `RefreshLook` is called on 2.1 entry, on `Emptied`, and as fixed on `Filled`, the minigame and exit.
  - `SelectedMinigame` is set by `MethodChosen`.
  - Stale minigame completion cannot leak into the next run.
  - Forced `DrinkComplete` records "Cancelled" (parity).
  - Book closed, note off and look fixed on every exit path.
- **G1-8**
  - `TryBegin` is synchronous through the first station's entry.
  - `Serve` runs exactly once per served drink and never on remake.
  - `ClearGlass` runs at start, on each restart and in `finally`.
  - Talking mode is restored on fault and cancel.
  - `Faulted` fires.
  - **Check that `ServeDrink` does not read the glass** (the old code cleared it first).
- **G1-9**
  - The guard set matches §5 of the refactor plan.
  - `NextDay` is not awaitable from Yarn.
  - Camera cut happens before events (deliberate; R0 verified nothing depends on the old order).
  - The chapter title is skipped on resume.
  - `Milestone` events fire at the right moments for G4 autosave.
- **G1-10**
  - Warning text names both the command and the current step.
  - `TryParseMinigame` keeps "Stiring".
  - `MinigameType` is never `""`.
- **G1-11**
  - No duplicated or null-unsafe calls (old `OnShakerFull`).
  - Every event subscribed in `OnEnable` is unsubscribed in `OnDisable`.
  - `LegacyDayUi` subscribes `AllClosed` before `ShowAll`.
  - `LegacyMinigames` uses a new source per run and cancels a non-Idle run on ct.
- **G1-12**
  - `[DefaultExecutionOrder(-1000)]`.
  - Duplicate root destroys itself.
  - No `DontDestroyOnLoad`.
  - `MainWiring.Unbind` mirrors `Bind`.
  - Every serialized ref is `[Required]`.
  - `GetAsync` completes for pre-registered services and honours ct.
- **G1-13**
  - Old `New Drag Drop System.unity` and `[GameLoop].prefab` are unchanged in git.
  - Only one EventSystem in `Main_V2`.
  - Old `flow_*` Yarn handlers are replaced (with G2), and there is no "already registered" error in the console.
- **G1-14:** editor-only, with no `Input.*` legacy calls.
- **G1-15**
  - Results are recorded honestly: untested items are listed (controller, real clicks, visuals).
  - String diff vs the R0 baseline is empty.
- **G1-16:** the ADR names the inversion of §3.1 (Host has no asmdef) and when it ends.

---

## (d) Assumptions

**VERIFIED in this repo**
- `AwaitableCompletionSource` and `<T>` have `SetResult`, `TrySetResult`, `SetCanceled`, `TrySetCanceled`, `SetException`, `TrySetException`, `Reset` and `.Awaitable` (Unity 6000.3.19f1 `UnityEngine.CoreModule.xml`).
- `Awaitable` has `NextFrameAsync(ct)`, `WaitForSecondsAsync(float, ct)`, `EndOfFrameAsync(ct)`, `FixedUpdateAsync(ct)`, `FromAsyncOperation`, `MainThreadAsync`, `BackgroundThreadAsync`, `IsCompleted` and `Cancel()`.
- `MonoBehaviour.destroyCancellationToken` and `Application.exitCancellationToken` exist.
- Yarn Spinner 3.2.5 dispatches commands that return `Awaitable` natively (`Actions.cs:432`). The code comment there says Awaitables implement `IEnumerator`.
- Registering a duplicate command name logs an error (`Actions.cs:563/605`). `RemoveCommandHandler` and `RemoveFunction` exist.
- Yarn actions are registered lazily on first access of `DialogueRunner.CommandDispatcher` (`DialogueRunner.cs:445`).
- Odin attribute DLLs are auto-referenced (`isExplicitlyReferenced: 0`). DOTween Modules are source files in Assembly-CSharp.
- No project code is in an asmdef today.

**UNVERIFIED (spike G1-0 or reviewer)**
1. **UNVERIFIED:** `TrySetResult`/`TrySetCanceled` run the awaiting continuation **inline, synchronously**. If they don't, fallback F1 is needed.
2. **UNVERIFIED:** awaiting a pooled `Awaitable` twice is undefined behaviour (we never do it, but the exact failure mode is unknown).
3. **UNVERIFIED:** an exception thrown out of `async void` is logged by Unity's synchronization context (`FlowAwait.Run` catches anyway).
4. **UNVERIFIED:** the await of a cancelled completion source throws `OperationCanceledException` rather than `TaskCanceledException`. Catch `OperationCanceledException`, which covers both.
5. **UNVERIFIED:** UTF 1.6 supports `async Task` test methods. The fallback is `[UnityTest] IEnumerator` returning `yield return awaitable`.
6. **UNVERIFIED:** completion-source-only Awaitables work in EditMode tests without a running player loop.
7. **UNVERIFIED:** `MonoBehaviour` stations created with `new GameObject().AddComponent<T>()` in EditMode run `RunAsync` correctly (no Awake dependency; stations must not rely on Awake).
8. **UNVERIFIED:** `CocktailSystemManager.ServeDrink()` does not read the glass or garnish state (scoring order changed; see §2.8).
9. **UNVERIFIED:** `ResetCocktail()` clears the shaker and so raises `Cleared`. `PrepareStation` relies on `Emptied` after a method-panel Reset; otherwise `LegacyBar` must raise `Emptied` itself.
10. **UNVERIFIED:** `SO_DaySchedule.cs` has no Assembly-CSharp dependency beyond Odin and UnityEngine (it looked that way when read; recheck on move).
11. **UNVERIFIED:** `TalkToCustomerManager.StartStory` queues behind a running dialogue (`StartWhenIdle`), per the plan note. G2's `IStory` must keep this.
12. **UNVERIFIED:** CancellationTokenRegistration disposal inside its own callback on the main thread is safe (standard .NET behaviour, but under Unity's Mono/IL2CPP).

---

## (e) Conflicts and expected interfaces from other groups

**G2 — dialogue / Yarn / voice / localization**
- **C1, name collision:** old `[YarnCommand("flow_*")]`/`[YarnFunction("flow_phase"/"flow_step"/"flow_minigame_*")]` attributes in Assembly-CSharp still register in `Main_V2`.
  - G2 must not redeclare those names by attribute.
  - Expected: `YarnVerbs.ReplaceCommand(string name, Delegate handler)` / `ReplaceFunction(...)`. These touch `runner.CommandDispatcher` (forcing registration), then `RemoveCommandHandler`/`RemoveFunction` + `Add*`, before `StartDialogue`.
- **Expected bind API:** `public static void YarnVerbs.Bind(FlowCommands flow, ...)`, called by `GameRoot.Awake`. G2 cannot reach `GameRoot.Current`, which is in Assembly-CSharp.
- **Expected:** `DialogueDirector : IStory` with `StartStory(node)` queuing until idle; `AdvanceDay()` journalled for replay.
- **G2 owns:**
  - `ReplayPolicy` gating of every flow shim (old `CommandTarget()` null during replay).
  - `StopSkip()` before `PrepareDrinks()`.
  - `waitForTitle = !IsSkipping`.
  - Not awaiting `NextDay`.
- **`serve_order` verb:** `Session.TryBegin()` then wait for `DrinkServed` **or** `Faulted`/`IsRunning==false` (so it never hangs). `wait_for_task` keeps polling `IsScored`, but should also release on `Faulted`.
- **`<<wait_step X>>`:** uses `FlowState.Changed` + `Step` (no API from G1 needed).
- **Localization:** `ShowChapterTitleAsync(chapterKey)` receives the **key** (`SO_DaySchedule.chapterName`). G2/G3 resolve it to text and decide the field rename.

**G3 — features, hosts, WorldView, additive UI scenes**
- **C2, port ownership:** G1 defines `IMinigames`/`IGarnish`/`IBar`/`ICameraRig`/`IDayUi` in `Bar410.Flow`.
  - If G3 wants feature asmdefs to implement them directly (no Host adapter), move the port files into a shared `Bar410.Contracts` asmdef (Core-level) that both Flow and Features reference. That is a file move only, with no API change.
- **C3, `IMinigameHost` timing:** §15.6's draft `RunAsync(MinigameRequest)` is acceptable if:
  - Back returns `Cancelled` **immediately**, with the outro on its own (old flow went to 2.1 on Back press).
  - `Completed` returns after the outro.
  - On ct cancel, the host cancels itself and throws OCE at once.
  - The host owns the customer inset.
  - `MinigameRequest` reduces to `MinigameKind selected` (the host reads the drink's required kind itself), or G3 adds fields without Flow needing them.
- **C4, `IGarnishHost`:** §15.6's single `RunAsync → GarnishResult (decoration + serve)` **conflicts** with keeping `flow_step` = Garnish then Serve and separate Hub rows.
  - G1 needs **two** awaits: `RunGarnishAsync` and `RunServeAsync`, each → `Next`, plus `ClearGlass()`.
  - Pour/ice stay inside G3 (the host talks to the bar feature, not to Flow).
- **Registration:** additive UI scenes call `GameRoot.Current.Register<IMinigames>(this)` in `Awake` and `Unregister` in `OnDestroy`. Boot awaits `GetAsync<T>` before the first `StartDayAsync`, so G3 must preload in BeforeOpen or earlier (§15.6: 300 ms hitch budget).
- **`IBar` façade:** G1 writes it as a legacy adapter first. G3 may own and replace it later.
  - The always-on shaker→bar-panel reactions belong inside it (feature-internal).
  - The camera and look reactions stay in `PrepareStation`.

**G4 — save / input**
- **G1 provides:**
  - `FlowState.Part` (save `DayPart`)
  - `BartendingSession.IsRunning` (replaces static `IsWaitingForTask`; "drink in progress is not saved" UI)
  - `DayRunner.Milestone` (`DayStarted`, after Event = start-of-day autosave; `DrinkServed`; `DayClosed`)
  - `DayRunner.ResumeAsync(ResumePoint)`
  - `BartendingSession.Cancel()` before a load.
- **Expected from G4:** a way for `GameRoot` to ask, at boot, `ResumePoint? TakePendingResume()` (null = new day).
- **Save-forbidden rules are G4's,** reading `FlowState`: Part None or Event, replay, Skip.
- **Snapshots:** `FlowState.SelectedMinigame`/`LastMinigameOutcome` need no snapshot (replay re-derives them, or they are reset). If G4 disagrees, G1 adds `Capture/Restore` on `FlowState`.
- **Input:** Flow reads no input. Debug hotkeys use `Keyboard.current` in the editor only. Pointer/cursor modes per station (§14.2) are G4's; if G4 needs each station to declare `Cursor`/`Focus`, they read it from `FlowState.Step` (no Station API needed).
