# Bar410 — Hierarchical State Machine Implementation

**Date:** 2026-08-20
**Branch:** `DialogueSystem/NewSystem/DragDropPlacement`
**Source spec:** `Bar410_StateMachine_Spec.md` (Draft v1.0)
**Scene touched:** `Assets/[05]Scenes/Deverlopment/New Drag Drop System.unity`

Implementation of the nested game-loop state machine from the spec, plus the scene object
that runs it. This document records what was built, what was decided, and what is still open.

---

## 1. What was built

16 C# files under `Assets/[02]Script/Hierarchical State Machine/`, all in namespace
`Bar410.GameFlow`, and one `[GameLoop]` GameObject in the New Drag Drop System scene.

```
Hierarchical State Machine/
├── GameFlowCommands.cs          Yarn commands + UnityEvent entry point
├── GameFlowHooks.cs             Inspector Enter/Update/Exit for all 9 states
├── Base/
│   ├── IState.cs                Entered / Exited / Ticked + Enter / Exit / Tick
│   ├── StateBase.cs             Default IState impl — event plumbing, virtual hooks
│   ├── StateMachine.cs          Generic StateMachine<TKey>, table-driven
│   └── StateHooks.cs            Serializable Enter/Update/Exit UnityEvent block
├── Level 1 - Game Loop/
│   ├── GameLoopFSM.cs           MonoBehaviour, owns StateMachine<GamePhase>
│   ├── PrepareBarPhase.cs
│   └── ClosingBarPhase.cs
├── Level 2 - Open Bar/
│   ├── OpenBarPhase.cs          Owns StateMachine<OpenSub>
│   ├── TalkingWithCustomerState.cs
│   ├── GarnishState.cs
│   └── ServeState.cs
└── Level 3 - Prepare Drinks/
    ├── PrepareDrinksPhase.cs    Owns StateMachine<PrepSub>
    ├── AddIngredientState.cs
    └── MinigameState.cs
```

`GameLoopFSM` is the only MonoBehaviour in the hierarchy. Every phase and state is a plain
C# class, built in code and driven from `Update`.

---

## 2. Architecture

Three levels of state, each container owning and driving its own child machine.

```
GameLoopFSM (linear, no backward transitions)
├── PrepareBarPhase
├── OpenBarPhase                     (owns OpenBarFSM)
│   ├── 1 TalkingWithCustomer
│   ├── 2 PrepareDrinks              (owns PrepareDrinksFSM)
│   │   ├── 2.1 AddIngredient
│   │   └── 2.2 Minigame
│   ├── 3 Garnish
│   └── 4 Serve
└── ClosingBarPhase
```

### Level 1 — `StateMachine<GamePhase>`

| State | Can go to |
|---|---|
| `Prepare` | `Open` |
| `Open` | `Close` |
| `Close` | `Prepare` |

### Level 2 — `StateMachine<OpenSub>`

| # | State | Can go to |
|---|---|---|
| 1 | `TalkingWithCustomer` | 2, **or exit container → `Close`** |
| 2 | `PrepareDrinks` | 3 |
| 3 | `Garnish` | 2, 4 |
| 4 | `Serve` | 2, 1 |

### Level 3 — `StateMachine<PrepSub>`

| # | State | Can go to |
|---|---|---|
| 2.1 | `AddIngredient` | 2.2 |
| 2.2 | `Minigame` | 2.1, **or exit container → 3 Garnish** |

### Rules the implementation enforces

- Transitions are a data table (`AddState(key, state, canGoTo)`), not `if`/`else`. An illegal
  move is rejected by `TryTransition` with a `Debug.LogWarning` — never a silent no-op.
- `Exit()` on the old state always runs before `Enter()` on the new one. `OnTransition`
  fires after both, once the machine is stable.
- A child never references its parent's key type. It raises its own event; the parent
  subscribes and decides what that means. This is why `OpenSub.Garnish` does not appear in
  the `PrepSub` table — leaving the container is `PrepareDrinksPhase.OnFinished`, not a
  transition.
- Container `Exit()` calls `StateMachine.Stop()`, so the active child state also gets its
  `Exit()` instead of being abandoned mid-state.

---

## 3. Design decisions applied on top of the spec

### 3.1 Backtracking resets the drink (spec §4.3, open question 2)

**Decided: reset.** Entering step 2 always restarts at 2.1 `AddIngredient`, including
backtracks from Garnish (3) or Serve (4). Implemented as the only behaviour — the
configurable `ResetOnReenter` flag and the `StateMachine.Resume()` path were both removed
once the decision was made, rather than left as dead branches.

### 3.2 Talking states merged, feedback removed (spec §4.1)

`TalkingWithCustomer_Before`, `TalkingWithCustomer_After` and `ReceiveFeedback` are gone.
One `TalkingWithCustomer` state replaces them in slot 1:

- Serve (4) loops back to it, so the same state covers pre-order and post-serve conversation.
- It is the **only** place the bar can close — the branch point of the whole Open Bar flow.
- The customer's reaction is now part of the conversation on the next loop, not its own step.

Two judgment calls inside this change, both easy to reverse:

- **Serve kept its backtrack to step 2.** Only `4 → 1` was specified; the old table had
  `4 → 2, 5`, so the `5` was read as replaced by `1` and the remake path left intact,
  matching Garnish.
- **Scoring moved to `ServeState.OnExit`.** With step 5 gone the drink still has to be scored
  before the customer reacts. Marked `TODO`, pending the data-passing spec.

Exit from step 1 is two named events (`OnRequestPrepareDrinks`, `OnRequestCloseBar`) rather
than the spec's `OnTalk2ExitDecision(bool)` — a bool argument does not bind cleanly from a
Yarn command or an Inspector UnityEvent.

### 3.3 Flow control lives in its own file (spec §6)

`GameFlowCommands` is the single place the outside world drives the machines. Nothing else
should call `TryTransition`.

---

## 4. Yarn Spinner interface

Every command is **static**, so `.yarn` scripts call it without naming a target GameObject.
All 12 are confirmed registered in Unity's generated
`ProjectSettings/Packages/dev.yarnspinner/Assembly-CSharp-generated.ysls.json`.

| Yarn command | Effect |
|---|---|
| `<<flow_open_bar>>` | Prepare → Open |
| `<<flow_prepare_drinks>>` | Step 1 → 2 |
| `<<flow_ingredient_added>>` | Step 2.1 → 2.2 |
| `<<flow_another_ingredient>>` | Step 2.2 → 2.1 |
| `<<flow_drink_complete>>` | Exit step 2 → 3 |
| `<<flow_garnish_done>>` | Step 3 → 4 |
| `<<flow_serve_done>>` | Step 4 → 1 |
| `<<flow_remake_drink>>` | Step 3 or 4 → 2 (warns elsewhere) |
| `<<flow_close_bar>>` | Step 1 → exit Open Bar → Close |
| `<<flow_next_day>>` | Close → Prepare |

| Yarn function | Returns |
|---|---|
| `flow_phase()` | `"Prepare"` / `"Open"` / `"Close"` |
| `flow_step()` | Current sub-step, drilling into `AddIngredient` / `Minigame` inside step 2 |

Note: existing project commands (`Can_End_Shift`, `Enable_InteractableObject`) are *instance*
`[YarnCommand]`s that need an object name in the call. These are static instead, so designers
do not have to name the flow object on every line. Flag if consistency is preferred.

Each command is also a public instance method on `GameFlowCommands`, so the same actions bind
from any UnityEvent / UnityAction dropdown in the Inspector.

---

## 5. Inspector hooks

`GameFlowHooks` exposes **On Enter / On Update / On Exit** for all 9 states as UnityEvents,
grouped by level. This is the designer/artist tier from spec §6 — SFX, VFX, animation
triggers, UI toggles. Logic-to-logic flow control stays on the plain C# events.

Container states fire alongside their children: entering 2.1 fires `Add Ingredient → On Enter`
while the enclosing `Prepare Drinks` is already entered, so "show the shaker UI" can live on
the container and "highlight the bottle shelf" on the leaf.

**`On Update` is per-frame reflection.** At most three fire per frame (active top phase,
open-bar step, prepare-drinks step), which is fine for polish. Anything heavy should subscribe
to `state.Ticked` in code instead — both paths coexist.

---

## 6. Deviations from the spec's literal code

| Deviation | Reason |
|---|---|
| Namespace `Bar410.GameFlow` | The rest of `[02]Script` is global-namespace, but the spec's `MinigameState` differs from the existing `MiniGameState` enum in `Minigame/IMinigame.cs` only by letter case — legal C#, but a trap. Also keeps generic `StateMachine<T>` out of the global pool. |
| `StateBase` added | Absorbs the `Entered`/`Exited`/`Ticked` plumbing instead of repeating it in 12 classes. Spec §2.1 ordering preserved: hook runs, then event fires. |
| Setup moved to constructors | The spec's `OpenBarPhase.Enter()` re-registered the transition table and re-subscribed on every entry, and `Open` is re-entered every game day. Public shape unchanged. |
| `StateMachine.Stop()` added | Without it, a container's `Exit()` leaves its active child entered forever and the child's cleanup never runs. |
| `GameLoopFSM.EnsureBuilt()` added | Component `Awake` order on one GameObject is not guaranteed. `GameFlowHooks` and `GameFlowCommands` both call it before touching phases, so hooks cannot silently bind to nulls. Binding in `Awake` also means the first `Prepare → On Enter` is not missed when `Auto Start` fires in `Start`. |

---

## 7. Scene setup

Root object **`[GameLoop]`** in `New Drag Drop System`, named to match the existing
`[SoundSystem]` / `[SaveLoadSystem]` convention. Three components:

| Component | Settings |
|---|---|
| `GameLoopFSM` | `Log Transitions: ✓`, `Auto Start: ✓` |
| `GameFlowCommands` | `Game Loop` → wired to the `GameLoopFSM` on the same object |
| `GameFlowHooks` | `Game Loop` → wired; 9 hook groups, all empty |

**`Auto Start` is on**, so the machine enters `Prepare` on `Start()` and logs transitions.
Every phase is a stub, so nothing gameplay-facing happens and `CocktailSystemManager` is not
touched — but the machine *is* live, and the first `<<flow_open_bar>>` will move it. Uncheck
`Auto Start` to bring it up manually with `StartDay()` instead.

---

## 8. Not done yet

- **Nothing is wired to the flow.** No buttons, interactables, or dialogue nodes call the
  `flow_*` commands, and all 27 UnityEvent slots are empty.
- **Every leaf state is a stub** with `TODO:` markers where gameplay hooks in.
- `ProjectSettings/Packages/dev.yarnspinner/Assembly-CSharp-generated.ysls.json` is
  **deliberately uncommitted** — it is a single-line generated file that also absorbed an
  unrelated in-progress `CameraController_001.cs` → `CameraController.cs` rename. It
  regenerates on compile; commit it alongside that rename when it lands.

### Open design questions (from spec §7)

1. **Step 1 exit condition** — what decides "another customer" vs. "close for the day"?
   Player action, empty queue, or time limit? Currently nothing drives it, so the Open Bar
   loop will not exit until wired.
2. ~~Backtracking reset behaviour~~ — **resolved, see §3.1.**
3. **Closing Bar's 3 actions** — ordered, optional, or all required before "Next Day"?
   Currently flat, no sub-FSM.
4. **Customer satisfaction scoring** — reads data from steps 1–3 (ingredients from 2.1,
   minigame result from 2.2, garnish from 3). Needs its own spec for a `DrinkOrderContext`
   built up across steps and scored at serve time.

---

## 9. Git

| Commit | Contents |
|---|---|
| `d741f7e` | Add hierarchical state machine for the Bar410 game loop — 34 files, +1242/−3 |

Folder reorganisation into `Base/` + `Level 1-3` is **staged but not committed**. Git records
all 28 file moves as pure renames; Unity's `MoveAsset` carried the `.meta` files, so GUIDs are
preserved and the `[GameLoop]` component references survived intact.

Pre-existing uncommitted changes, untouched by this work: the `CameraController` rename,
`Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectCocktail_renew.slnx`,
`ProjectSettings/ProjectSettings.asset`.
