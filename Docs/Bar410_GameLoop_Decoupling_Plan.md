# [GameLoop] Decoupling — Audit and Plan (whole prefab: root + all children)

Date: 2026-10-05 · Status: proposal, no code changed · Branch at audit: `GameFlow/Main`

Scope: the `[GameLoop]` prefab (`[04]Prefab/GameSystemPrefab/[GameLoop].prefab`, instanced in the open scene with 16 overrides) — the 10 scripts on the root **and** everything under it: `SystemGame`, `Canvas`, `MiniGameSystem`, `CamController` (nested prefab), `PlacmentSystem`. Numbers come from a live dump of the scene hierarchy (every serialized scene reference and every Inspector UnityEvent call under the root) plus reading the scripts. Dump tooling was a throwaway editor script; nothing was added to the project.

---

## 1. Size of the problem

| | |
|---|---|
| GameObjects with scripts | whole tree: 941 components, 414 MonoBehaviours (includes 112 `Image`, 43 `Button`, 31 TMP UI built-ins) |
| Scene references (non-UI-noise) | **246**: 168 stay inside one child, **78 cross between children** |
| Inspector UnityEvent calls | **28** (hidden logic, see F-C1) |
| Who creates the cross-child links | the 10 scripts on the root create **58 of the 78** (74%) |

Per child:

| Child | MonoBehaviours | Refs inside itself | Refs it sends out | Refs received from root bridges |
|---|---|---|---|---|
| root `[GameLoop]` | 10 | 7 | **58** (Canvas 37, SystemGame 12, MiniGame 5, Cam 4) | — |
| `Canvas` | 176 | 38 | 9 (SystemGame 6, root 3) | **37** |
| `SystemGame` | 141 | 89 | 10 (Canvas 8, Cam 1, outside prefab 1) | 12 |
| `MiniGameSystem` | 63 | 28 | **0** | 5 |
| `CamController` | 22 | 4 | 0 | 4 (+1 UnityEvent) |
| `PlacmentSystem` | 2 | 2 | 1 | 0 (found at runtime only) |

`MiniGameSystem` is the only healthy child: `MinigameSystemManager` has zero Inspector refs, discovers its `BaseMiniGame` siblings with `GetComponents`, raises one `MinigameFinished` event, and exactly one bridge listens. **That is the template for everything else.**

## 2. Four wiring planes, not one

The "spaghetti" is not only Inspector slots. The same objects are driven through four separate planes, and nothing shows all four together:

1. **Inspector references** — 246 (above).
2. **Inspector UnityEvent calls** — 28: `ShakerContents` ×9, 18 bottle clicks, 2 shaker/book clicks.
3. **Code subscriptions** — HSM bridges subscribing to state `Entered`/`Exited`.
4. **Runtime lookup / static** — `FindFirstObjectByType`/`FindAnyObjectByType` in `IngredientButtonUI` (16 of 19 bottles have `_shaker` null → each searches on first pour), `VisualizeCocktail`, `DragableObject.OnEnable` (up to 14 searches for `N_PlacementSystem`), `IngredientHoverDetector`, `Post_It_Order` (`BubblePresenter`), `CocktailSystemManager._instance`, `SaveLoadManager`; statics: `GameFlowCommands.Instance`, `CinimachineCameraSwitcher.Instance`, `PlacedGlassInstance.Current`, `SoundManager.Instance`.

Plus **Yarn** as a fifth writer: `Enable_InteractableObject`, `Switch_Camera`, `Cut_Camera`, `Can_End_Shift`, `flow_*`.

Because the planes don't know each other, the same object has many writers:

| Object | Writers (all planes) |
|---|---|
| `IngredientButtonGroup` (ingredient lock) | TalkingBridge, CocktailBridge, GarnishBridge, `CocktailSystemManager` (`ResetCocktail`, Yarn `Enable_InteractableObject`), `ShakerContents` events ×2, Yarn → **6** |
| Camera | TalkingBridge, CocktailBridge, GarnishBridge, `ShakerContents.OnFulled` event (`"PrepareCam"`), Yarn `Switch_Camera`, `MinigameSystemManager` (null) → 3 camera IDs (`MainCam`, `GranishCam`, `PrepareCam`) defined in 3 different places |
| Book button / `BookUI_V2` | TalkingBridge, CocktailBridge, UIFlowBridge, `IngredientButtonGroup` (member #19!), `ShakerContents.OnFulled` event, shaker-click event → **5–6** |
| `Post_It_Order` | CocktailBridge, ServeBridge, UIFlowBridge, `CocktailSystemManager` ×2 → **4** |
| Serve panel | ServeBridge, CocktailBridge, `ShakerPanelController._serveUI` → **3** |
| EndShift button | `UIFlowBridge` (click), `CocktailSystemManager._endShiftBTN` (show) → **2** |
| `ShakerContents` | CocktailBridge, GarnishBridge, MinigameBridge, `CocktailSystemManager`, bottles, 5 listeners |

## 3. Findings

### Root scripts (unchanged from first pass, condensed)

**F1 — Bridges split by state; side effects split by subsystem** (N×M). Root cause.
**F2 — Redundant slots.** `_gameLoop`/`_commands` on 6 bridges are auto-resolved by `GetComponent`; 3 are already null in the scene (`Talking._gameLoop`, `Serve._gameLoop`, `Serve._commands`) and everything works — proof the slots are dead. 8 shared objects are wired 21 times.
**F3 — `GarnishFlowBridge`** is a 330-line / 20-slot gameplay controller, circular with `GlassChoiceGrid`; three grids and `Bar410DialogueOrderTest` point back at the root.
**F4 — `CocktailFlowBridge`** owns five unrelated jobs and is misnamed.
**F5 — Competing logic.** Scoring from the Serve button (`ServeDrink`: scores **and** writes Yarn vars, relationship delta, post-it) *and* from `OnServeExited` (scores only — skips all of that). Redundant `ResetCocktail()`+`RemakeDrink()`. `DestroyCurrent()` ×2.
**F6 — Possible bug.** `MinigameFlowBridge` Give-up / Cancel-direct cancel the minigame without a HSM transition. New detail from the child dump: Give-up also calls `ShakerContents.Clear()`, whose `Cleared` UnityEvent calls `IngredientButtonGroup.EnableInteractablePrepareDrinksPhase` — so ingredients get re-enabled while the HSM is still in 2.2. Verify in play mode.
**F7 — Boilerplate ×6** (+ `EnsureBuilt` hack). **F8 — Dev scripts on prod root.** **F9 — Domain logic in MonoBehaviours** (`RemakeDrink` switch). **F10 — Smaller** (stale comments, magic strings, mismatched unsubscribe).

### Children (new)

**F-C1 — A second control plane lives in `ShakerContents` UnityEvents.** 8 persistent calls fan out to 5 targets: `VisualizeCocktail` (×2), `ShakerPanelController` (×2), `IngredientButtonGroup` (×2), the book button (`set_interactable`), `CinimachineCameraSwitcher.SwitchCamera("PrepareCam")`. They run outside the HSM and overlap with what the bridges do (ingredients, camera, panels). The camera plan for "the drink is full" exists only inside an Inspector event. This also contradicts the locked decision "flow wiring in code, not persistent listeners" — the gameplay layer never followed it.

**F-C2 — Panels are owned by the root, not by themselves.** 37 of the 58 root outward refs go to Canvas buttons/panels: the Method panel's 3 buttons and 2 panels are wired from `CocktailFlowBridge`; Serve's panel + 2 buttons from `ServeFlowBridge` (and again from `CocktailFlowBridge`, and `ShakerPanelController`); Garnish's 12 buttons/panels from `GarnishFlowBridge`; book buttons and post-it toggle from `UIFlowBridge`; the 4 minigame cancel buttons from `MinigameFlowBridge`. A panel that could be self-contained is instead taken apart and wired from the top.

**F-C3 — `IngredientButtonGroup` is a generic lock list.** 20 hand-maintained entries: `CocktailShaker` listed **twice** (#0 and #18), the Open/Close-book button (#19), then 17 bottles. Name says "ingredient"; contents say "everything the player can touch".

**F-C4 — `CocktailSystemManager` (4 partial files) is a scene god object.** Session state + Yarn adapter + scoring + UI: it holds `_postItOrder`, `_endShiftBTN` (the same button `UIFlowBridge` owns), `_ingredientButtons`, `_shakerContents`, and `_dialogueRunner` — the **only** reference that leaves the prefab. Static `_instance` is resolved with `FindAnyObjectByType` for Yarn.

**F-C5 — Duplicate and dead objects inside the prefab.**
- `Canvas/Panel - VisurlCocktail` (inactive) duplicates `CocktailShaker/Canvas - World Space/PanelWS - VisurlCocktail`; both carry `VisualizeCocktail` + `_shaker`. The bridge updates the world-space one but toggles the Canvas one (`_panelVisualCocktail`).
- `GlassPlacementZone` on `SystemGame/Garnish_Plate`: no script references it (ADR 0002 says unused).
- `PlacmentSystem`: `N_PlacementSystem` reached only by `DragableObject.OnEnable` search; `MouseIndicator` inactive. May be dead after the garnish restructure — verify.
- `SystemGame/RecipeBook` (inactive): sprite renderer and all 4 sprites null.
- `ShakerPanelController`: `_addIceUI` null, serve/ice permission flags and `_serveUI` are legacy from before `ServeFlowBridge`.
- `MinigameSystemManager`: `_cocktailCamera` null (so `ResetCamera()` is a silent no-op); `OnStartedMinigame`/`OnEndedGame` have **zero** listeners, though `MinigameFlowBridge` comments say the scene locks the shaker there — stale.
- 18 `Interactable_2_5DObject._disabledSprite` null, 17 `HoverTooltip._providerObject` null, 16 `_sharedCanvas` null — unused or runtime-resolved, undocumented.
- `DebugPhysicsRaycaster` on the production Main Camera.

**F-C6 — Editor hotkey collisions.** `MinigameSystemManager` binds `1`, `2`, `R`, `B`, `V` in `Update`; `GameFlowDebugHotkeys` binds `2`–`0`; `Demo_KeysShortCut` binds `R`. In the editor, pressing **2** both runs `PrepareDrinks` and switches the minigame to Stirring; **R** ends the active minigame and fires the demo reload event (whose prefab listener has an empty target).

**F-C7 — Camera has two controllers and a singleton.** `CameraController` (381 lines) and `CinimachineCameraSwitcher` (singleton *and* referenced by 2 bridges *and* called from an event *and* from Yarn). Small: `ApplyTransition` comment says "disable previous camera" but code sets `previousVcam.enabled = true`.

**F-C8 — Hidden dependency on a runtime search.** 16 of 19 bottles resolve `ShakerContents` via `FindFirstObjectByType(Include)` on first pour; `VisualizeCocktail` has both an Inspector slot and a Find fallback.

## 4. Principles for the target state

1. **Own your subtree.** A script references only things inside its own subtree (found with `GetComponentInChildren`/`InParent`, or a slot inside the same panel). It never reaches into a sibling subtree.
2. **Events up, commands down.** Panels and systems raise C# events and expose a few public methods. Only the flow layer (root) subscribes and calls.
3. **One owner per shared object** — camera, ingredient lock, book, post-it, Serve panel, EndShift button.
4. **No hidden wiring.** No persistent UnityEvents for gameplay, no runtime `Find`, no singletons except what Yarn needs (`GameFlowCommands`, the order manager). This is the existing locked decision, applied to the children too.
5. **Discover by type, fail loudly.** Everything is a child of `[GameLoop]`, so the root bridges find owners with `GetComponentInChildren<T>(true)` in `Awake` and `LogError` which bridge is missing which piece. That removes the slots without making dependencies invisible.

## 5. Plan

Every phase: compile-check, play one full loop (hotkeys `2`→`9`, `0`, plus remake from Garnish and Serve), commit alone. No DI container, no event bus. Edit the prefab asset, and check overrides on the scene instance first; `CamController` and `Panel - BookUI` are nested prefabs — check whether other scenes use them before editing.

### Phase 0 — delete-only / no behaviour change
- Remove `GlassPlacementZone` (component + script), the duplicate `Canvas/Panel - VisurlCocktail` + `_panelVisualCocktail` slot, the duplicate `CocktailShaker` entry in `IngredientButtonGroup`, and `RecipeBook` — each after you confirm in §6.
- Drop `ShakerPanelController` serve/ice slots and flags, `MinigameSystemManager._cocktailCamera` + `ResetCamera`, stale comments.
- Move `GameFlowDebugHotkeys` and `Demo_KeysShortCut` to `[Debug]`; take `DebugPhysicsRaycaster` off the prefab's Main Camera (keep it on a debug-only scene object); give each editor hotkey set its own keys (fixes F-C6).
- `[DefaultExecutionOrder(-100)]` on `GameLoopFSM`, build in `Awake`, delete `EnsureBuilt()` and its 7 call sites.

### Phase 1 — `FlowBridgeBase`
Abstract MonoBehaviour: resolves `Loop`/`Commands` with `GetComponent`, `Need<T>()` helper (child lookup + loud error), `Bind(Button, UnityAction)` with auto-unbind, one `Subscribe()` hook. Removes the 10 redundant root slots and ~100 lines.

### Phase 2 — panels own themselves (the main child-side fix)
One small script **on each panel's own GameObject**: wires its own buttons, raises C# events, exposes `Show`/`Hide`. Root bridges subscribe to events and find the panel by type. One panel per stage, in this order (biggest first):
- `GarnishPanel` on `Panel - Granish UI`: tab buttons + 3 sub-panels (`OnEnable` → glass tab), pour/finish/reset, ice add/remove (`OnEnable` → "add" state, `LockIceRemoval()`), raises `PourClicked`, `FinishClicked`, `ResetClicked`, `IceChanged(bool)`. The three choice grids find it with `GetComponentInParent` — no pointer back to the root; `GlassChoiceGrid` listens to a `PourStarted` event instead of being called (cycle gone).
- `ServePanel` on `Panel - Serve`: `ServeClicked`, `RemakeClicked`. Replaces 5 slots on ServeBridge, 1 on CocktailBridge, and `ShakerPanelController._serveUI`.
- `MethodPanel` on `Panel - Method`: `ShakingChosen`, `MixingChosen`, `ResetChosen`. Replaces 5 slots on CocktailBridge.
- `BookPanel` (on `Panel - BookUI`, nested prefab) takes the Hub open-button + next/prev/close buttons from `UIFlowBridge`; `Post_It_Order` takes its own toggle button.
- `LoadSceneButton` on `EndShiftButton` and `Panel_Thx` (Button + `SceneLoader` already share a GameObject): scene name as a field. `UIFlowBridge` then disappears.
- `MinigameCancelButtons` on `Canvas_MiniGame`: `CancelRequested`, `GiveUpRequested`; `MinigameFlowBridge` loses 4 slots.

Expected: root→Canvas refs 37 → ~0 Inspector slots (type lookup instead), Canvas→root refs 3 → 0.

### Phase 3 — kill the UnityEvent control plane
- Move the 8 `ShakerContents` persistent calls into **one** subscriber in the Prepare-Drinks bridge (`OnFulled` → lock ingredients, show method panel, lock book button, switch camera). They become visible, ordered, greppable. `ShakerContents` keeps its events but has no Inspector listeners.
- `VisualizeCocktail` subscribes to `Changed`/`Cleared` in code (it already has `_shaker`).
- `CocktailSystemManager` stops touching UI: remove `_postItOrder`, `_endShiftBTN`, `_ingredientButtons`; raise `OrderCommitted` / `CanEndShift` events that the post-it, EndShift button, and ingredient policy listen to. Register `_instance` in `Awake` instead of `FindAnyObjectByType`. `_dialogueRunner` stays (the one legitimate outside reference).
- The shaker/book-prop clicks (`Interactable_3DObject.OnClicked`) become C# events subscribed by the owning panel.

### Phase 4 — one owner per shared subsystem
- Add a flat `FlowStep` (`Talking, AddIngredient, Minigame, Garnish, Serve, Close`) on `GameLoopFSM` with `StepChanged`; the Yarn `flow_step` function and debug overlay read it. Drink reset stays on the container event `PrepareDrinks.Entered` (the flat step cannot tell fresh entry from a cancelled-minigame re-entry).
- Presenters, each on the object it controls, finding the flow with `GetComponentInParent<GameLoopFSM>()`:
  - `CameraPresenter` on `CamController`: owns the camera-ID table (`MainCam`, `GranishCam`, `PrepareCam`); merges the roles of the 3 bridges and the event. `CinimachineCameraSwitcher.Instance` stays only for Yarn's `Switch_Camera`.
  - `IngredientShelf` on `SystemGame` (rename/replace `IngredientButtonGroup`): collects bottles from the 3 container objects (20 slots → 3, book button and shaker removed), hands `ShakerContents` to each bottle (kills the 16 runtime `Find`s), is the **only** writer of the ingredient lock. Yarn `Enable_InteractableObject` goes through it.
  - Book and post-it visibility rules live in `BookPanel` / `Post_It_Order` (step → show/hide), not in 3–4 bridges each.
- `TalkingWithCustomerBridge` has nothing left and is deleted.

### Phase 5 — slim the bridges, one owner for duplicated logic (needs §6 decisions)
- `GarnishStation` (choose glass, pour, garnish slots, `CanFinish`) — plain gameplay controller; `GarnishFlowBridge` becomes a ~60-line seam.
- One scoring path; drop `ResetCocktail()` before `RemakeDrink()`; one glass-cleanup hook; Serve panel owned by `ServePanel` only; `CocktailFlowBridge` → `PrepareDrinksBridge` (rename the file inside Unity so the `.meta` GUID travels); `RemakeDrink` switch → `OpenBarPhase.RequestRemake()`; fix F6 once confirmed.

### Target state
- Root scripts: 10 → ~6 (`GameLoopFSM`, `GameFlowCommands`, `PrepareDrinksBridge`, `MinigameFlowBridge`, `ServeFlowBridge`, slim `GarnishFlowBridge`).
- Root→child Inspector slots: 58 → under 10; cross-child refs 78 → roughly 15–20 (estimate — recount with the dump after each phase).
- Inspector UnityEvent calls: 28 → ~18 (only the per-bottle click that lives inside the one bottle prefab).
- Writers per shared object: 3–6 → 1.
- Runtime `Find`s on the hot path: 16+14 → 0.

## 6. Decisions needed

1. **Scoring authority.** Recommended: `Serve.Exited` → `_cocktail.ServeDrink()` (made idempotent via `IsScored`); the Serve button only calls `ServeDone`.
2. **Confirm what is dead** (Phase 0 deletes depend on it): `GlassPlacementZone`, `Canvas/Panel - VisurlCocktail` (which of the two renders in play?), `RecipeBook`, `PlacmentSystem`/`N_PlacementSystem` (still needed by `DragableObject`?).
3. **Depth.** Full (Phases 0–5) or a cheaper cut: Phases 0, 1, 2 only (cleanup + panels own themselves). That alone removes most of the root fan-out; Phase 3 is what removes the hidden logic.
4. **Discover-by-type** (principle 5) instead of explicit slots — OK? It trades visible Inspector slots for a convention, in exchange for ~50 fewer slots.
5. **Hotkey keys.** Which range should `GameFlowDebugHotkeys` keep, and is the `R` reload still wanted?
6. **F6.** Play-test Give-up / Cancel-direct: is the flow stranded in 2.2?

## 7. Non-goals

- HSM state classes, `StateMachine`, transition tables.
- `[YarnCommand]` / `[YarnFunction]` names — `.yarn` scripts depend on them.
- Renaming typo'd public APIs (`CinimachineCameraSwitcher`, `ResetRotaionAndMovement`, `SelectStiring`).
- Bottle internals (`ScaleOnHover`, `HoverTooltip`, `UIPointerSound` …) — one prefab, authored once, not part of the wiring problem.
- `BookUI_V2` internal null sub-slots (data-struct noise, ~50 unused fields).
- Anything from the Glass Freedom / Garnish restructure that is already locked.
