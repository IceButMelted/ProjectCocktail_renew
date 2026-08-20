# Bar410 — Minigame ↔ Game Loop Integration Plan

**Date:** 2026-08-20
**Branch:** `GameLoop/main`
**Goal:** Make step **2.2 Minigame** (`GameLoop → OpenBarPhase → PrepareDrinksPhase → MinigameState`)
actually run a minigame, with the *choice of which minigame* driven from `GameFlowCommands.cs`.

Companion to `Bar410_StateMachine_Implementation.md` (§8 "Not done yet" — this closes the
`MinigameState.OnEnter` TODO).

---

## 1. What exists today

### Flow side — `Assets/[02]Script/Hierarchical State Machine/`

`MinigameState` is a **plain C# class** (`StateBase`), not a MonoBehaviour. Both hooks are stubs:

```csharp
protected override void OnEnter()  { /* TODO: hand off to MinigameSystemManager */ }
protected override void OnExit()   { /* TODO: record the minigame result */ }
```

It raises two events, both already wired by `PrepareDrinksPhase`:

| Event | Consequence |
|---|---|
| `OnRequestAnotherIngredient` | 2.2 → 2.1 |
| `OnDrinkComplete` | exits container → `OpenSub.Garnish` |

`GameFlowCommands` forwards to them via `AnotherIngredient()` / `DrinkComplete()`.
There is **no** type parameter anywhere in the flow layer — that is the gap.

### Minigame side — `Assets/[02]Script/Minigame/`

| File | Role |
|---|---|
| `IMinigame.cs` | `IMinigame` (`IsRunning`, `OnGameEnd(bool)`, `StartGame/EndGame/ProcessedGame`), `IMinigameContext`, and the **gameplay** `MiniGameState` enum (`Standby/Processing/Success`) |
| `BaseMiniGame.cs` | `MonoBehaviour`, abstract `Enum_MiniGameType GameType`, panel-slide phase machine, `FireEndEvent(bool)` |
| `MinigameSystemManager.cs` | Auto-registers every `BaseMiniGame` on its own GameObject into `Dictionary<Enum_MiniGameType, BaseMiniGame>`; `StartMinigame(type)`, `GetMinigame(type)`, `NextPhase()`, `ClosePanelReset()`, `OnStartedMinigame` / `OnEndedGame` UnityEvents |
| `Minigame/ShakingMinigame.cs` | `GameType => Shaking` — gauge + shrinking target zone |
| `Minigame/MixingMinigame.cs` | `GameType => Stiring` — oscillating needle, N hits |
| `SO_MinigameSetting.cs` / `SO_ShakingSetting.cs` / `SO_MixingSetting.cs` | Per-game tuning assets |

`Enum_MiniGameType` (`[02]Script/Cocktail System/Enum_Class.cs:108`) = `None, Shaking, Stiring, Building`.
**`Building` has no implementation** — `StartMinigame(Building)` logs a warning and does nothing.

### Current scene wiring (`New Drag Drop System.unity`)

`[GameLoop]` (`GameLoopFSM` + `GameFlowCommands` + `GameFlowHooks`) and `MinigameSystem`
(`MinigameSystemManager` + both `BaseMiniGame`s) are in the **same scene** but **never talk to each other**.

Today the minigame is started by UI buttons calling, in order:

1. `IngredientButtonUI.SetShaking()` → `CocktailShakerData.SetMethod(Method.Shaking)`
2. `MinigameSystemManager.StartShakingMinigame()`

…and closed by buttons calling `NextPhase()` / `ClosePanelReset()`. The FSM never moves.

---

## 2. Direction of control

Two options; **pick A**.

| | A — Flow drives minigame *(recommended)* | B — Minigame drives flow |
|---|---|---|
| Trigger | Entering `MinigameState` starts the minigame | Button starts minigame, bridge pushes FSM to 2.2 |
| Buttons | Rewired to `GameFlowCommands.IngredientAdded(type)` | Left as-is |
| Truth | FSM is authoritative | Minigame is authoritative, FSM shadows it |

A matches the doc's rule "nothing outside `GameFlowCommands` touches `TryTransition`", makes
`flow_step()` trustworthy in Yarn, and means the Garnish/Serve backtrack automatically tears the
minigame down. Cost: the existing two buttons must be rewired.

---

## 3. Design

### 3.1 Where the type comes from

Three sources, resolved in priority order by the bridge:

1. **Explicit** — `GameFlowCommands.IngredientAdded("Shaking")` / `<<flow_minigame Shaking>>`
2. **Shaker method** — `CocktailShakerData.CurrentCocktail.PreparationMethod`
   (`Method.Shaking → Enum_MiniGameType.Shaking`, `Method.Stirring → Enum_MiniGameType.Stiring`)
3. **Inspector default** — a serialized fallback on the bridge

This keeps the existing `SetShaking`/`SetMixing` buttons meaningful (they still set the shaker's
method) while letting Yarn override per-node.

### 3.2 `MinigameState` gains a type, not a dependency

`MinigameState` stays a plain C# class with **no reference to `MinigameSystemManager`** — that would
drag `UnityEngine` and a scene object into the flow layer and break the layering the HSM doc
established. Instead it carries data and raises an event:

```csharp
// Level 3 - Prepare Drinks/MinigameState.cs
public Enum_MiniGameType PendingType { get; private set; } = Enum_MiniGameType.None;
public bool? LastResult { get; private set; }          // null until a game has ended

public event Action<Enum_MiniGameType> OnStartRequested;   // fired from OnEnter
public event Action OnStopRequested;                       // fired from OnExit

public void SelectType(Enum_MiniGameType type) => PendingType = type;
public void ReportResult(bool success) => LastResult = success;
```

`OnEnter` fires `OnStartRequested(PendingType)`; `OnExit` fires `OnStopRequested` and (later) writes
`LastResult` into the scoring context. Requires `using static E_Cocktail;` inside `namespace Bar410.GameFlow`.

### 3.3 New file — `MinigameFlowBridge.cs`

`Assets/[02]Script/Hierarchical State Machine/Level 3 - Prepare Drinks/MinigameFlowBridge.cs`,
a `MonoBehaviour` on the `[GameLoop]` object. It is the only place the two systems meet.

```
[SerializeField] GameLoopFSM            _gameLoop;
[SerializeField] MinigameSystemManager  _minigames;
[SerializeField] CocktailShakerData     _shaker;          // optional — method fallback
[SerializeField] Enum_MiniGameType      _defaultType = Shaking;
[SerializeField] bool                   _autoAdvanceOnWin = false;
```

Awake → `_gameLoop.EnsureBuilt()`, then subscribe to `Minigame.OnStartRequested / OnStopRequested`
(same pattern as `GameFlowHooks.Awake`).

| Bridge callback | Action |
|---|---|
| `OnStartRequested(type)` | `Resolve(type)` → subscribe `IMinigame.OnGameEnd` → `_minigames.StartMinigame(resolved)` |
| `OnGameEnd(success)` | unsubscribe, `Minigame.ReportResult(success)`; if `_autoAdvanceOnWin` → `GameFlowCommands.DrinkComplete()` |
| `OnStopRequested` | unsubscribe; if still `IsRunning` → `_minigames.ClosePanelReset()` |

`Resolve` = explicit → shaker method → `_defaultType`, with a `Debug.LogWarning` if it lands on
`None` or `Building` (no registered game).

### 3.4 `GameFlowCommands` additions

```csharp
// existing, unchanged
public void IngredientAdded()                              // 2.1 → 2.2, keeps last-selected type

// new
public void SelectMinigame(Enum_MiniGameType type)         // Inspector dropdown-friendly
public void SelectMinigame(string type)                    // Yarn-friendly, Enum.TryParse
public void IngredientAdded(string minigameType)           // select + advance in one call
public void SelectShaking()                                // no-arg wrappers for Button.OnClick,
public void SelectStiring()                                //   which cannot pass an enum
```

New Yarn surface:

| Yarn | Effect |
|---|---|
| `<<flow_minigame Shaking>>` | Sets `PendingType` without transitioning |
| `<<flow_ingredient_added Shaking>>` | Select + 2.1 → 2.2 (optional string param on the existing command) |
| `flow_minigame_type()` | Returns `"Shaking"` / `"Stiring"` / `"None"` |
| `flow_minigame_result()` | Returns `"win"` / `"lose"` / `""` if not played yet |

Guard every command with the existing `Resolve()` null check. `Enum.TryParse` failures warn and
leave `PendingType` untouched rather than silently setting `None`.

Note for designers: `"Stiring"` is the enum's spelling (single `r`) — accept `"Mixing"` and
`"Stirring"` as aliases in the string parser so `.yarn` files don't have to reproduce the typo.

---

## 4. Work items

> Structural question — *should the minigames be rewritten onto `StateBase` / `StateMachine<TKey>`
> so both layers share one shape?* Answered **no** in §8. Read it before starting item 2.

| # | File | Change |
|---|---|---|
| 1 | `Level 3 - Prepare Drinks/MinigameState.cs` | Add `PendingType`, `LastResult`, `SelectType`, `ReportResult`, `OnStartRequested`, `OnStopRequested`; fire them from `OnEnter`/`OnExit` |
| 2 | `Level 3 - Prepare Drinks/MinigameFlowBridge.cs` | **New** — subscribe, resolve type, start/stop, capture result |
| 3 | `GameFlowCommands.cs` | `SelectMinigame` ×2, `IngredientAdded(string)`, `SelectShaking/SelectStiring`, 2 Yarn commands, 2 Yarn functions |
| 4 | `MinigameSystemManager.cs` | Fix `SwitchTo` (see §5.1); optionally expose `ActiveType` |
| 5 | Scene `New Drag Drop System.unity` | Add `MinigameFlowBridge` to `[GameLoop]`, wire the 3 object refs |
| 6 | Scene — buttons | Repoint the two Shaking/Mixing buttons from `StartShakingMinigame`/`StartMixingMinigame` to `GameFlowCommands.SelectShaking`/`SelectStiring` + `IngredientAdded`; keep the `IngredientButtonUI.SetShaking/SetMixing` call ahead of it |
| 7 | Scene — `NextPhase` / `ClosePanelReset` buttons | Decide per §5.3 |
| 8 | `BaseMiniGame.cs` | *Optional, independent* — route every `CurrentSlidePhase` write through one `SetSlidePhase(next)` chokepoint with a legality table (§5.6) |
| 9 | `BaseMiniGame.cs`, both minigames | *Optional, independent* — delete `SlidePhase.InitBackground`, the dead `_backgroundSnapped` / `openPanelDoneCount` / `_closingSnapApplied` fields, and the dead `ResetGame` slide-phase lines (§5.6) |

Order: 1 → 3 → 2 → compile-check via `read_console` → 4 → 5 → 6/7 → play-test.

Items 8–9 are cleanup of an existing mess, not integration work. Do them **before** item 2 (so the
bridge is built against a machine you can reason about) or well after — but not interleaved, or a
panel-animation regression and an integration regression will be indistinguishable.

---

## 5. Problems found while reading — decide before implementing

### 5.1 `SwitchTo` logs a warning on every switch *(real bug)*

`MinigameSystemManager.cs:133` — `_activeMinigame?.SetState(MiniGameState.Standby)`.
The legal table in `BaseMiniGame.IsValidTransition` allows `Success → Standby` but **not
`Processing → Standby`**, so switching away from a running game hits
`"Invalid state transition: Processing → Standby. Ignoring."` and the old game is left `Processing`.
With the FSM driving this on every 2.1 → 2.2 it will fire routinely.
**Fix:** call `EndGame()` / force `Standby` before switching, or add the `Processing → Standby`
edge to the table (it's the "abort" transition and it genuinely exists now).

### 5.2 Two exit paths, only one raises `OnGameEnd`

- **Win path:** `ProcessedGame` → `SlidePhase.RemoveMinigame` → `SetState(Success)` → `FireEndEvent(true)` → `OnGameEnd(true)` ✅ and also `OnEndedGame` UnityEvent.
- **Manual close:** `NextPhase()` / `ClosePanelReset()` → `ClosePanel()` → `SlidePhase.Closing` → `OnEndedGame` UnityEvent only, **no `OnGameEnd`**.

The bridge listens on `OnGameEnd`, so a player who closes the panel manually never reports a result.
**Recommendation:** have `SlidePhase_Closing` call `FireEndEvent(false)` (an aborted minigame is a
loss), so both paths converge on one event.

### 5.3 `NextPhase()` invokes `OnEndedGame` a second time

`NextPhase()` calls `ClosePanel()` *and* `OnEndedGame?.Invoke()`, and `SlidePhase_Closing` invokes
`OnEndedGame` again when the slide finishes — so every scene listener on that event runs twice.
Relevant because those buttons are the natural home for `AnotherIngredient()` / `DrinkComplete()`.
Recommend dropping the eager `Invoke()` in `NextPhase` and letting the slide-out fire it once.

### 5.4 `MinigameSystemManager.Update` runs the active game unconditionally

`Update()` calls `_activeMinigame.ProcessedGame()` every frame regardless of flow phase, and
`#if UNITY_EDITOR` hotkeys `1/2/R/V` can start, swap, or end a minigame behind the FSM's back.
Mostly harmless (`IsRunning` guards the gameplay body) but the hotkeys will desync the FSM in the
editor. Options: leave them (dev-only), or route `V` through `GameFlowCommands`.

### 5.5 Default active minigame at `Awake`

`Awake` picks `Shaking` as `_activeMinigame` before any flow exists. Harmless with `IsRunning` false,
but means `GetGameState()` reports a game that was never selected. No action needed; noted so it
isn't mistaken for the bridge's doing.

### 5.6 `SlidePhase` is an unenforced state machine, and it has already rotted

`SlidePhase` (`BaseMiniGame.cs:42`) has 8 states, **no legality table**, and is assigned from 13
sites across 3 files — including two subclasses writing directly into the base class's machine.
Evidence that nobody can currently reason about it:

- **`SlidePhase.InitBackground` is declared, never assigned, and has no handler** in the
  `SlidePanelMinigame` switch. If anything ever set it, the switch falls through to `default: break`
  and the panel hangs forever with `IsRunning` stuck `false`. A dead-end state that compiles.
- **The two minigames disagree in the identical branch.** `ResetGame()` when `!IsRunning` sets
  `InitMinigame` in `ShakingMinigame.cs:151` but `Closing` in `MixingMinigame.cs:223`. Both are then
  overwritten by `base.StartGame()`'s `InitPanel`, so both lines are dead — but they are dead
  *differently*, which is the tell.
- `_backgroundSnapped` and `openPanelDoneCount` are written and never read. `_closingSnapApplied`
  guards a block whose entire body is commented out.

**Fix (work items 8–9):** one `SetSlidePhase(next)` chokepoint with a legality table that warns on
illegal moves — the same *idea* as `StateMachine<TKey>`, implemented locally, sharing no code
(see §8). Then delete the dead state, dead fields, and dead assignments.

This is the maintenance problem in the Minigame folder. `MiniGameState` is not.

---

## 6. Open questions for design

1. **Who ends step 2.2?** Auto-advance on win (`_autoAdvanceOnWin`), or does the player still press a
   button to choose "another ingredient" vs "drink complete"? The current UI implies the latter —
   plan defaults to `_autoAdvanceOnWin = false`.
2. **Does a lost minigame block progress?** Right now neither minigame can be lost — the win
   condition is the only exit. If `FireEndEvent(false)` becomes reachable (§5.2), does 2.2 retry,
   or continue with a quality penalty?
3. **`Building` method.** `Enum_MiniGameType.Building` and `Method` have no matching pair
   (`Method` is only `None/Shaking/Stirring`). Is Building a real third minigame, or a "no minigame"
   path that should skip 2.2 entirely?
4. **Result storage.** `MinigameState.LastResult` is a single bool overwritten each loop. The real
   need is the `DrinkOrderContext` from the HSM doc §8 Q4 — a per-ingredient list of
   `(ingredient, minigameType, result)` accumulated across 2.1/2.2 and scored at Serve.
   `LastResult` is deliberately a placeholder for that.

---

## 7. Verification

1. `mcp__unityMCP__read_console` after each script change — nothing usable until compilation clears.
2. Play-test `New Drag Drop System`: `<<flow_open_bar>>` → `<<flow_prepare_drinks>>` →
   ingredient button → confirm the shaking panel slides in and `[GameLoop] ...` logs 2.1 → 2.2.
3. Confirm `flow_step()` returns `"Minigame"` while the panel is up.
4. Win the minigame → confirm `LastResult` is set and no `Invalid state transition` warnings appear.
5. Backtrack: from Garnish call `<<flow_remake_drink>>` mid-minigame → confirm `OnStopRequested`
   tears the panel down and the flow restarts at 2.1.

---

## 8. Decision — should the minigames adopt the HSM structure?

**Question:** should `Minigame/` be rewritten onto `StateBase` / `StateMachine<TKey>` /
`IState` so both layers share one shape, on the theory that uniformity is easier to maintain?

**Decision: no.** Keep the minigames on their own machine. Fix the seam and the rot instead
(§5.6, work items 8–9).

### Why not

**1. `MiniGameState` is already the same design.** `BaseMiniGame.IsValidTransition` (`:133`) *is* a
legality table; `SetState` warns instead of failing silently; `OnExitState` runs before
`OnEnterState`. Those are the three guarantees the HSM doc §2 lists as its rules. Porting 3 states
from a `switch` expression to `AddState(key, state, canGoTo)` is a lateral move that changes the
spelling and nothing else.

It also would not have caught the `Processing → Standby` bug in §5.1. That is a wrong *entry* in the
table, and a table-driven machine enforces a wrong table just as faithfully as a right one.

**2. `StateBase` is an abstract class, so a MonoBehaviour cannot inherit it.** Every minigame state
would become a plain C# class holding a back-reference to `BaseMiniGame` to reach `_minigamePanel`,
`ArtWorks`, `amc`, and `StartCoroutine`. That is *more* indirection to do RectTransform math, not
less. Implementing the `IState` interface directly on MonoBehaviours avoids the inheritance clash
but costs a GameObject per state, which is worse again.

**3. The two machines run at different rates and on different triggers.**

| | HSM (`Bar410.GameFlow`) | Minigame (`SlidePhase` / `MiniGameState`) |
|---|---|---|
| Transitions on | player action, dialogue node | a lerp finishing, an input frame |
| Interval | seconds to minutes | per frame |
| Audience | designers, via UnityEvent hooks | gameplay/animation programmers |
| Driven from | `GameLoopFSM.Update` | `MinigameSystemManager.Update` |

`StateMachine<TKey>` has no notion of "transition when the tween completes". Adopting it means
calling `TryTransition` from inside `Tick` every frame — legal, but the gain is a dictionary lookup.

**4. It worsens the name collision.** `MiniGameState` (gameplay enum) vs
`Bar410.GameFlow.MinigameState` (flow class) differ only by letter case. HSM doc §6 already flags
this as a trap; merging the two worlds into one vocabulary makes it harder to keep straight, not
easier.

### The one thing HSM has that the minigames lack

`StateMachine<TKey>.OnTransition` — an observer event fired after a transition settles, for HUD
overlays, analytics, and logging. `BaseMiniGame` has no equivalent. That is worth roughly three
lines (`event Action<MiniGameState, MiniGameState> StateChanged;`, fired at the end of `SetState`)
and does **not** require adopting anything else.

### What actually makes this maintainable

The seam, not the shape. `IMinigame` is already the right boundary, and the rule to hold is:

> The HSM never learns what `SlidePhase` is. The minigames never learn what `PrepSub` is.

Uniform internals are not what make systems maintainable — a narrow, stable interface between them
is. Sharing `StateMachine<TKey>` across both layers would actively couple gameplay-feel code to flow
architecture, so a change to the game loop's transition rules could break a panel animation. That is
precisely the coupling the hierarchy was built to avoid.

### Revisit this if

- A third and fourth minigame land and each grows its own bespoke slide/phase logic — at that point
  the shared abstraction is `BaseMiniGame`'s own, still not the HSM's.
- Minigames ever need to nest (a minigame containing sub-minigames). Nesting is what
  `StateMachine<TKey>` is actually *for*; `SlidePhase` is flat and has no such need today.
