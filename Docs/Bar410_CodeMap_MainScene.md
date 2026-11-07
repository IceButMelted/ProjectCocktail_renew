# Bar410 — Code Map of the Main Scene (`New Drag Drop System`)

Written 2026-11-06 as a snapshot **before the code refactor**. Scope: only what the main scene
`Assets/[05]Scenes/Deverlopment/New Drag Drop System.unity` uses, from `[02]Script`, `[04]Prefab`
and `[06]Dialogue`. Main menu, loading and Pause scenes are mentioned only where the main scene touches them.

**How this was made / how far to trust it**
- Read in full: the whole state machine (`Hierarchical State Machine/`), `FlowWiring`, `GameFlowCommands`,
  `TalkToCustomerManager` (both partials), `CocktailSystemManager` (core), `MinigameSystemManager` (top half).
- Read at header + public-API level only: everything else. Those descriptions come from file headers,
  doc comments and public members, not from reading every line. Where this doc says "(header only)" treat it as a guide, not a guarantee.
- The scene hierarchy was read live through Unity MCP (root + `[GameLoop]` children + a few more).
  Inactive objects are marked.
- Nothing was run in Play mode for this document.

Related docs that already exist: `Docs/Bar410_Systems_Guide.md` (per-system guide), `CONTEXT.md` (vocabulary),
`Docs/adr/` (decisions that override the GDD).

---

## 1. One-paragraph picture

The game is a day loop driven by a **hierarchical state machine written in plain C#** (`GameLoopFSM`).
**Yarn Spinner dialogue** moves the story and tells the flow what to do through `<<flow_*>>` commands
(`GameFlowCommands`). The flow itself never touches Unity objects: **`FlowWiring` is the only class that listens to the
flow's events and calls the managers** (cocktail, minigame, garnish, customers, camera, HUD). Managers do not know the
flow and do not know each other. When the player serves a drink, `CocktailSystemManager` scores it and
`TalkToCustomerManager` writes the result back into Yarn variables, so the next dialogue lines can react.

```
 Yarn (.yarn)  ──<<flow_*>>──►  GameFlowCommands ──►  GameLoopFSM (states: pure C#)
      ▲                                                     │ events (Entered/Exited/…)
      │ Yarn variables                                      ▼
 TalkToCustomerManager ◄── OrderScored ──  FlowWiring  ──► verbs on every manager
      (writes $satisfaction …)         (only bridge)       Cocktail · Minigame · Garnish · Camera
                                                            · Customers · HubManager · Book · PostIt
```

---

## 2. The scene, object by object

Root objects of `New Drag Drop System` (18):

| Root object | Active | What it is |
|---|---|---|
| `[GameLoop]` | yes | Everything the game logic lives under (see below). Prefab: `[04]Prefab/GameSystemPrefab/[GameLoop].prefab`. Components on the root: `GameLoopFSM`, `GameFlowCommands`, `FlowWiring` |
| `[SoundManager]` | yes | `SoundManager` + 4 AudioSource children (UISFX, SFX, Ambient, Music) |
| `[SoundManager] (1)` | yes | **A second `SoundManager`** (empty object). Duplicates destroy themselves; likely a leftover — see §10 |
| `[CinemachineBrain]` | yes | Cinemachine brain. `CameraManager` logs *"No CinemachineBrain assigned"* → its field is probably empty |
| `[SceneLoader]` | yes | `SceneLoader` (load scene by name) |
| `[PauseManager]` | yes | `PauseManager` — loads `PauseScene` additively and freezes time |
| `[SaveLoadSystem]` | **no** | `SaveLoadManager` + `SceneLoader`. Inactive, so `<<save_chapter_node>>` finds no target (known bug in CLAUDE.md) |
| `[Bar410DialogueOrderTest]` | yes | Manual integration test component (`Bar410DialogueOrderTest`) |
| `[Debug]` | yes | `DebugScreenCapture` + `GameFlowDebugHotkeys` (number keys 0–9 drive the flow) |
| `BarInteriorV2` | yes | The 3D bar room: `BarInterior_A1`, `Assets` (29 props), 8× `WallLight`, 3× `CeilingLight` (static, baked) |
| `Directional Light (BarInteriorV2)`, `LightProbes_Bar`, `IngredientShadowLight`, `Light` | yes | Lighting. `IngredientShadowLight` = spot light that only hits Rendering Layer 3 (ingredient sprite shadows) |
| `BarVer_3`, `Table`, `TestObjectOntable`, `TMP_NPC` | **no** | Old / test objects, inactive |

`[GameLoop]` children (10):

| Child | Key components | Role |
|---|---|---|
| `[CocktailSystem]` | `CocktailSystemManager`, `IngredientButtonGroup`, `CocktailInspectorReadout` | Orders, scoring, shaker, bottles. Has its own Canvas (Method panel etc.) |
| `[MiniGameSystem]` | `MinigameSystemManager`, `ShakingMinigame`, `MixingMinigame` | Minigames + own Canvas + `MinigameCharacterView` camera |
| `[PlacmentSystem]` (sic) | `N_InputManager`, `N_PlacementSystem` | Drag/hover ray queries and placement zones |
| `[GarnishSystem]` | `GarnishManager` | Glass, ice, garnish, pour, serve panel; own `Canvas_Garnish` |
| `[EventLogSystem]` | `EventLogPresenter` | Records dialogue history |
| `[IngredientUnlocks]` | `IngredientUnlocks` | Which ingredients the player has |
| `Text Animator Dialogue System` | `DialogueRunner` (+ prefab children: `Canvas`, `Markup Processors`, `Line Advancer` = `PauseAwareKeyCodes`, `EventSystem`) | Yarn Spinner runner and presenters |
| `Canvas_HUD` | `DialogueControls`, `HubManager` | The Hub UI (13 buttons/panels, see below) |
| `CamController` | `CameraManager` | Owns the live virtual camera (prefab `CamController.prefab`) |
| `NPCs_And_Set` | `TalkToCustomerManager`, `CharacterData`, `Demo_KeysShortCut` | 8× `NPC_*` (`CharacterView`), 4× `WayPoint (n)` (the spots) |

`Canvas_HUD` children: `BTN_Open_CloseBook`, `BTN_Setting`, `BTN_Toggle_AutoPlay`, `BTN_Skip`, `BTN_OpenEventLog`,
`BTN_Clock`\*, `EndShiftButton`\*, `Panel_SkipWarning`\*, `Panel_BookUI`\* (`BookUI_V2`), `Panel_Thx`\* (`PH_Thx`),
`Panel_Transition` (`PanelTransition` + `Animator`), `Panel_ChapterTitle` (`ChapterTitleUI`), `Panel_EventPopup`
(`EventPopupQueue`). \* = inactive at edit time.

Scene-scoped static handles (set in Awake, cleared in OnDestroy) that static Yarn commands use to find a live object:
`GameFlowCommands.Instance`, `TalkToCustomerManager.Current`, `CameraManager.Current`, `IngredientUnlocks.Current`,
`N_PlacementSystem.Current`, `PauseManager.Instance`, `SoundManager.Instance`, `SaveLoadManager.Instance`;
plus a cached `FindAnyObjectByType` in `CocktailSystemManager` (`Resolve()`).

---

## 3. Game flow

### 3.1 State machine (3 levels)

`StateMachine<TKey>` (generic, transition table is data; illegal transitions log a warning and return false).
`StateBase` runs the hook first, then fires `Entered/Exited/Ticked` (so listeners see the finished state).
A container state (phase) owns a child machine, starts it in `OnEnter` (`SetInitial`) and stops it in `OnExit` (`Stop`).

```
Level 1  GameLoopFSM  (GamePhase)
  BeforeOpen ──► Open ──► Close ──► BeforeOpen          (no backward edges)
  │              │
  │              └─ Level 2  OpenBarPhase (OpenSub)
  │                   1 TalkingWithCustomer ──► 2 PrepareDrinks, or exit to Close
  │                   2 PrepareDrinks       ──► 3 Garnish          (exit of the Level-3 container)
  │                   3 Garnish             ──► 2 PrepareDrinks (remake) | 4 Serve
  │                   4 Serve               ──► 2 PrepareDrinks (remake) | 1 Talking
  │                        └─ Level 3  PrepareDrinksPhase (PrepSub)
  │                             2.1 AddIngredient ──► 2.2 Minigame
  │                             2.2 Minigame      ──► 2.1 (another ingredient) | exit → 3 Garnish
  │
  └─ Level 2  BeforeOpenPhase (BeforeOpenSub)
        Event ──► StoryBeforeOpen ──► Event (mid-story check) | exit → Open
```

Rules worth remembering
- Every entry to BeforeOpen starts at **Event**. Only the **first** Story entry of the day starts the dialogue (`Story.IsFirstEntry`).
- Entering PrepareDrinks always **resets the drink** (back to 2.1), including remake from Garnish/Serve.
- Open is reachable only from **Story**; Close only from **Talking** (`<<flow_close_bar>>`).
- Scoring happens on **Serve → Talking** only (`OnOpenBarTransition`); a remake never scores.
- Minigames cannot be lost: outcome is `Completed` or `Cancelled`.
- `ClosingBarPhase` is an empty stub (no UI, nothing enters it yet).

### 3.2 Who can ask for a transition

All outside intent goes through `GameFlowCommands` (instance methods for UnityEvents, static `[YarnCommand]` wrappers for Yarn).
While a save is replayed silently (`SceneLoaderBridge.IsSilentReplay`) every Yarn flow command is ignored (`CommandTarget()` returns null).

| Request | Method | Transition |
|---|---|---|
| open the bar | `OpenBar` / `<<flow_open_bar>>` (waits for the chapter title to fade out) | Story → Open |
| day events mid-story | `CheckEvents` / `<<flow_check_events>>` (waits until popups closed) | Story → Event → Story |
| next day | `NextDay` / `<<flow_next_day>>` | Close → BeforeOpen |
| customer ordered | `PrepareDrinks` / `<<flow_prepare_drinks>>` (also stops Skip) | 1 → 2 |
| close bar | `CloseBar` / `<<flow_close_bar>>` | 1 → Close |
| ingredient chosen | `IngredientAdded([type])` / `<<flow_ingredient_added [Shaking]>>` | 2.1 → 2.2 |
| back from minigame | `AnotherIngredient` / `<<flow_another_ingredient>>` | 2.2 → 2.1 |
| drink finished | `DrinkComplete` / `<<flow_drink_complete>>` | 2.2 → 3 |
| garnish confirmed | `GarnishDone` / `<<flow_garnish_done>>` | 3 → 4 |
| served | `ServeDone` / `<<flow_serve_done>>` | 4 → 1 |
| remake | `RemakeDrink` / `<<flow_remake_drink>>` | 3 or 4 → 2 |
| pick minigame | `SelectMinigame(type)` / `<<flow_minigame X>>` | none (selection only) |

Yarn functions for branching: `flow_phase()`, `flow_step()`, `flow_minigame_type()`, `flow_minigame_result()`.

### 3.3 What `FlowWiring` does at each step

(`FlowWiring` also calls `HubManager.SetPhase(...)` on every phase entry.)

| Step | On enter | On exit |
|---|---|---|
| *Scene start* | `Start()`: lock bar, Hub = None, `yield WaitUntilFinished()` of `PanelTransition`, then `GameLoopFSM.StartDay()` | |
| BeforeOpen | camera look fixed + reset + **cut** to bar | |
| Event | lock bar, Hub = BeforeOpen, `RefreshBottles`, enqueue one "Unlock New Ingredient" popup from `IngredientUnlocks.TakeNewUnlocks()`, `EventPopupQueue.ShowAll()`; `AllClosed` → `Event.Finish()` | |
| Story | on first entry: `TalkToCustomerManager.StartStory(node of $day from SO_DaySchedule)` | |
| Open | show chapter title (`ChapterTitleUI.Show`), camera → bar | |
| 1 Talking | disable ingredients, look fixed, close book, Hub = Talking, camera → bar | warns if no order was placed |
| 2 Prepare | clear shaker, reset panel permissions, clear order result, camera reset → bar, post-it available | close book, post-it away, look fixed |
| 2.1 AddIngredient | Hub, refresh bottles (locked ones hidden), bottles interactive, random order if none (test fallback), show method panel, refresh look | ingredients off, hide method panel, reset fill bars |
| 2.2 Minigame | Hub; `OnStartRequested` → show customer inset camera + `MinigameSystemManager.StartMinigame(Resolve(selected, fromDrink))` | `OnStopRequested` → `CancelMinigame()` if not idle |
| 3 Garnish | Hub; `UpdateCocktailInShaker()` (resolve recipe identity); camera → garnish; ingredients off; `GarnishManager.Open()`; post-it | `Close()`, post-it away |
| 4 Serve | Hub; `GarnishManager.OpenServe()` | `CloseServe()`, camera → bar |
| Close | Hub = Close | **`AdvanceDay()` ($day + 1)** — Exit runs before the next Enter, so the new Story reads the new day |
| Prepare enter / Serve exit | `GarnishManager.ClearGlass()` (empty table for every customer) | |

Manager → flow translations (the reverse direction):
- Method panel (`MethodPanel`): Shaking/Mixing pressed → `ShakerContents.SetMethod`, `SelectShaking/SelectStiring`, `IngredientAdded()`.
- `ShakerContents.OnFulled` → disable ingredients, look fixed, show method panel, camera → shaker.
- `MinigameSystemManager.MinigameFinished(type, outcome)` → `Minigame.ReportResult`, hide inset; `Cancelled` re-enables the shaker; `Completed` (and still in 2.2) → `DrinkComplete()`.
- `MinigameSystemManager.BackRequested` → `AnotherIngredient()`.
- `GarnishManager` events: `PourRequested` (pour the shaker's drink), `IceToggled`, `FinishRequested` → `GarnishDone`, `ServeRequested` → `ServeDone`, `ResetRequested`/`RemakeRequested` → `ResetCocktail` + `RemakeDrink`.
- `CocktailSystemManager.OrderScored` → `TalkToCustomerManager.CommitOrderResult`; `OrderCleared` → `ResetVariables`.

### 3.4 One full day, as it actually runs

1. Scene loads. `GameLoopFSM` (Auto Start off) waits. `FlowWiring.Start` locks the bar and waits for `Panel_Transition`.
2. `StartDay()` → **BeforeOpen** → **Event**: popup(s) for newly unlocked ingredients (queue empty → passes at once).
3. **Story**: `StartStory(node)` for `$day` (from `DaySchedule_*.asset`). The Yarn script brings characters in
   (`<<Isla Enter 1>>`), talks, and ends with `<<flow_open_bar>>`.
4. **Open → Talking**: chapter title shows; dialogue continues with the customer. The script places an order with
   `Order_Cocktail_*` / `order_by_type` and `<<order_customer X>>`, then `<<flow_prepare_drinks>>`, then `<<wait_for_task>>`.
5. **2.1 AddIngredient**: player drags bottles / fruit into the shaker. Shaker full → camera on shaker, method panel.
   Player picks Shake or Stir → **2.2 Minigame** (shaking or mixing). Completed → **3 Garnish**.
6. **3 Garnish**: choose glass, ice, garnish, **Pour**, then **Finish** → **4 Serve** → **Serve** button.
7. **Serve → Talking**: `ServeDrink()` scores once → `OrderScored` → Yarn gets `$satisfaction`, `$type_of_cocktail`,
   `$task_done`, `$rel_<id>` change. `<<wait_for_task>>` releases; the customer reacts in the next lines.
8. Repeat 4–7 per customer. The last node calls `<<flow_close_bar>>` → **Close** (empty) → `<<flow_next_day>>` → `$day + 1` → BeforeOpen.

### 3.5 Order, scoring, Yarn variables

```
Yarn: Order_Cocktail_ByName_OutName("X") / Order_Cocktail_OutName(npc) / <<order_by_type Type>> / <<order_customer Name>>
   └─► CocktailSystemManager.Orders (OrderService) ──► Order (DrinkOrderContext: Customer, Mode, Target, OrderedType)
Player builds drink ──► ShakerContents.CurrentCocktail (runtime S_Drink, throw-away ScriptableObject)
Garnish Enter ──► UpdateCocktailInShaker() ──► ShakerContents.UpdateIdentity(allDrinks)   (DrinkDeviation best match)
Serve → Talking ──► ServeDrink() [idempotent] ──► DrinkScoringService.Score(Order, served)
        Domain: DrinkDeviation → DrinkFlagResolver (Perfect/…) → SatisfactionEvaluator (Perfect/Acceptable/Fail)
                PricingRules (payout), relationship delta
   └─► OrderScored ──► TalkToCustomerManager.CommitOrderResult ──► YarnVariableSync.WriteResult + ApplyRelationshipDelta
```
`YarnVariableSync` is the only writer of `$task_done`, `$type_of_cocktail`, `$satisfaction`, `$day`, `$rel_*`.
`wait_for_task` waits on `Order.IsScored`; `CocktailSystemManager.IsWaitingForTask` (static) is read by `SaveLoadManager`.

---

## 4. `[02]Script` — every folder

### 4.1 `Hierarchical State Machine/` (the flow)
| File | What it does |
|---|---|
| `Base/IState.cs`, `StateBase.cs`, `StateMachine.cs` | Contract, event plumbing (hook then event), generic table-driven machine |
| `Level 1 - Game Loop/GameLoopFSM.cs` | MonoBehaviour, `[DefaultExecutionOrder(-100)]`, builds the 3 phases in Awake, ticks in Update, `StartDay()`. `Auto Start` should be OFF in this scene |
| `Level 1 - Game Loop/ClosingBarPhase.cs` | Stub. `RequestNextDay()` event only |
| `Level 2 - Before Open/BeforeOpenPhase.cs`, `EventState.cs`, `StoryBeforeOpenState.cs` | See §3.1. `Story.IsFirstEntry` / `ResetDay()` gate the start of dialogue |
| `Level 2 - Open Bar/OpenBarPhase.cs`, `TalkingWithCustomerState.cs`, `GarnishState.cs`, `ServeState.cs` | Open Bar machine and its states (events only) |
| `Level 3 - Prepare Drinks/PrepareDrinksPhase.cs`, `AddIngredientState.cs`, `MinigameState.cs` | Drink-building loop. `MinigameState` carries `PendingType` / `LastOutcome` and the Start/Stop request events |
| `GameFlowCommands.cs` | The only door into the machines (instance methods + static Yarn commands/functions) |
| `FlowWiring.cs` | The bridge between flow and managers (§3.3). Largest file in the project |
| `GameFlowDebugHotkeys.cs` | Number keys 1–9, 0 call the commands (editor dev tool) |

### 4.2 `Cocktail System/`
| File | What it does |
|---|---|
| `Cocktail/CocktailSystemManager.cs` (+ `Yarn/*.cs` partials) | Scene owner of recipes, `Order`, services. `ServeDrink()`, `ResetCocktail()`, random order helpers. Partials: `YarnOrders` (order functions/commands, read-back functions), `YarnTask` (`wait_for_task`, `wait_scene`, `Can_End_Shift`, `IsWaitingForTask`), `YarnDebug` (context-menu helpers) |
| `Yarn/YarnVariableSync.cs` | Plain class; the only place C# state is written to Yarn variables |
| `Session/DrinkOrderContext.cs` | State of the current order (customer, mode, target, result, payout, delta, `IsScored`) |
| `Session/OrderService.cs` | Picks the order: by name, by preference (uniform over matching recipes), by type. Recipe filter from unlocks |
| `Session/DrinkScoringService.cs` | Runs the §17–18 scoring once at serve |
| `Session/ICustomerPreferences.cs`, `SO_Customer.cs`, `SO_CustomerRoster.cs` | Customer data asset (name colour, liked types, sprites incl. LEGACY VISUAL mode) and roster |
| `Cocktail/Domain/*` | Pure rules, no Unity lifecycle: `AlcoholClassifier`, `DrinkBuilder` (only place a runtime drink is mutated), `DrinkColorBlender`, `DrinkDeviation` (sum of differences + best match), `DrinkFlagResolver`, `DrinkFormatter`, `DrinkQuery`, `IngredientMath`, `PricingRules`, `RecipeMatch`, `SatisfactionEvaluator` |
| `Cocktail/S_Drink.cs`, `DrinkIngredients.cs`, `E_Cocktail.*.cs` | Recipe data (3 ingredient lists: Alcohol / Liqueur / Mixer, total 10 parts) and the partial `E_Cocktail` enums (`CharacterName`, `TypeOfCocktail`, `Satisfaction`, `Method`, `Enum_MiniGameType`, glassware…) |
| `Cocktail/SO_CocktailList.cs`, `CompositeDrinkRepository.cs`, `IDrinkInterfaces.cs`, `SO_UnmatchedDrinkColor.cs` | Recipe repositories behind `IDrinkRepository` |
| `Cocktail/Shaker/ShakerContents.cs` | What is in the glass (live drink, method, ice, `Changed/Cleared/OnFulled`) |
| `Cocktail/Shaker/ShakerPanelController.cs`, `ShakerTooltip.cs`, `IngredientButtonGroup.cs`, `InteractableToggle.cs` | Which panels may open; tooltip text; the set of bottles that can be locked/refreshed; the single "enable this object" routine |
| `Cocktail/Ingredients/*` | `BottleIngredientSource` (drag bottle to shaker), `FruitTray*`, `FruitPieceInstance`, `DragableFruitTraySlot`, `IngredientHoverDetector` |
| `Cocktail/Glass/*` | `GlassPlacementZone`, `PlacedGlassInstance`, `SO_GlassOption`, `SO_GarnishItemOption`, `SO_GarnishRimOption`, `GarnishCategory`, `Glass/Garnish*ChoiceGrid`, `GarnishSlotButton` |
| `Cocktail/PourVisual.cs` | Door into the third-party pour effect (CocktailCrafter_V2 `PourController`) |
| `IngredientButtonUI.cs`, `MethodPanel.cs`, `VisualizeCocktail.cs` | Bottle/ice/method button, Shake/Stir/Reset panel (raises events only), fill bars |
| `CocktailInspectorReadout.cs`, `Bar410DialogueOrderTest.cs` | Debug readout / manual test (both on active objects in the scene) |

### 4.3 `Customer/`, `NPC/`, `Day Schedule/`, `Ingredient Unlock/`
| File | What it does |
|---|---|
| `Customer/TalkToCustomerManager.cs` | Finds `CharacterView` by name, `StartStory`, `CurrentDay`/`AdvanceDay`, `CommitOrderResult`, `ResetVariables`, preferences |
| `Customer/TalkToCustomerManager.Characters.cs` | Spots, Enter/Exit (fade + bob + sounds), save `Snapshot/Restore`, Yarn commands `CharacterExpression/Look/Enter/Exit` and short form `<<Isla Sad>>` (one line per character at the bottom) |
| `Customer/CharacterView.cs` | One person: body + face sprites, expression, facing, presence, idle bob, fade (DOTween). Has LEGACY VISUAL branch |
| `NPC/CharacterData.cs`, `E_Cocktail.Character.cs` | Legacy preference component (still on `NPCs_And_Set`); `CharacterName` enum |
| `Day Schedule/SO_DaySchedule.cs`, `ChapterTitleUI.cs` | Day → start node + chapter name; the title UI |
| `Ingredient Unlock/IngredientUnlocks.cs`, `SO_IngredientUnlockTable.cs`, `IngredientId.cs` | Unlock authority (table by `$day` or story `<<unlock_ingredient>>`); keyed by enum name |

### 4.4 `Minigame/`
| File | What it does |
|---|---|
| `MinigameSystemManager.cs` | Registry of all `BaseMiniGame` on the object, start/cancel, back buttons, `MinigameFinished/Started/BackRequested`, `Resolve(selected, fromDrink)`; editor hotkeys |
| `BaseMiniGame.cs`, `IMinigame.cs` | One phase machine `Idle → Intro → Play → Outro → Idle`, four hooks, `Complete()` / `Cancel`; outcome reported after outro |
| `Minigame/ShakingMinigame.cs`, `MixingMinigame.cs` | Spam-click gauge / stop-the-needle |
| `SO_MinigameSetting.cs`, `SO_ShakingSetting.cs`, `SO_MixingSetting.cs` | Settings assets |
| `MinigamePanelAnimator.cs`, `IInputProvider.cs`, `E_Cocktail.Minigame.cs` | Panel slide animations, input abstraction (legacy `Input`), enum (`Stiring` spelling is the enum's) |

### 4.5 `Garnish/`, `CameraControl/`, `PlacementSystem/`, `BaseInteractable/`
| File | What it does |
|---|---|
| `Garnish/GarnishManager.cs` | Garnish station: glass, ice, items, rim, pour, finish, serve panel; own canvas; events listed in §3.3 |
| `Garnish/RenderTextureClickRelay.cs` | Passes clicks on a RenderTexture RawImage to the collider drawn in it |
| `CameraControl/CameraManager.cs` | Owner of the live camera: shots (bar / shaker / garnish), `SetLookFixed`, `ResetLook`, customer inset, Yarn `Switch_Camera` / `Cut_Camera` |
| `CameraControl/CameraController.cs`, `CameraControllerSettings.cs`, `CameraEntry.cs`, `CameraRotator.cs` | Mouse look, settings asset, data tag on each virtual camera (found by ID), rotation helper |
| `PlacementSystem/N_PlacementSystem.cs`, `N_InputManager.cs`, `InputSystemManager.cs` | Scene placement singleton; mouse ray/zones; input manager |
| `PlacementSystem/PlacementZoneBase.cs`, `SurfacePlacementZone.cs`, `VolumePlacementZone.cs` | Where a dragged item may land |
| `BaseInteractable/*` | `PointerInteractableBase` → `Interactable_2_5DObject`, `Interactable_3DObject`, `BTN_2_5D`; `DragableObject` (click vs drag), `InteractableController`, `HoverTooltip`, `ScaleOnHover`, `ObjectPlaySound`, `PagedChoiceGrid` |

### 4.6 `UI Script/`, `Pause/`, `Sound/`, `SaveLoad/`, `SceneLoader/`
| File | What it does |
|---|---|
| `UI Script/HubManager.cs` | CanvasGroup gate that shows/hides each Hub UI per `HubPhase` from an Inspector table |
| `UI Script/EventPopupQueue.cs`, `PanelTransition.cs`, `Post_It_Order.cs`, `BookUI_V2.cs` | Event popups (`AllClosed`); scene-open transition (`WaitUntilFinished`); order note; recipe book |
| `UI Script/ButtonFeedback/*` | `ButtonScaleFeedback` (DOTween scale), `ButtonFeedbackSound`, `SO_ButtonFeedback` |
| `UI Script/UIOnPosition3D*.cs`, `UIParallaxMouse.cs`, `PH_Thx.cs` | UI follows a 3D point; parallax; thanks panel |
| `Pause/PauseManager.cs`, `PausePanel.cs`, `OpenPanelButton.cs`, `ClosePageButton.cs`, `SettingWindowTabs.cs` | Additive Pause scene, layered panels, tabs |
| `Sound/SoundManager.cs` (+ `SoundData`, `SoundId(s)`, `SoundSettings`, `SoundPlayer`, `VolumeSlider`) | Mixer-based audio, id-based SFX/BGM/ambient/voice |
| `SaveLoad/SaveLoadManager.cs` (+ `Save*Data`, `PlaytimeTracker`) | JSON save slots; collects Yarn vars, NPCs, event log, unlocks. **Known broken in this scene** (CLAUDE.md "สิ่งที่ต้องแก้") |
| `SceneLoader/SceneLoaderBridge.cs`, `LoadingManager.cs`, `SceneLoader.cs`, `LoadSceneButton.cs` | Static state for dialogue save/load (`IsSilentReplay`, `CurrentLineId`, …) and scene loading |
| `AnimationCode/*`, `Debug Script/*`, `MainMenu/*`, `InitActiveGameObject.cs`, `ToggleActive.cs`, `DeselectButton.cs`, `Demo_KeysShortCut.cs` | Animation event relays, debug tools, main-menu-only scripts (not used in this scene), small helpers |

---

## 5. `[06]Dialogue`

| File / folder | What it does | In the main scene? |
|---|---|---|
| `Text Animator Dialogue System.prefab` | The dialogue rig (DialogueRunner, Canvas with presenters, Line Advancer, EventSystem) — instance under `[GameLoop]` | yes |
| `CocktailLine/LinePresenterCocktailProject.cs` | Shows a line: name box, typewriter (Text Animator), fade, auto-advance, skip-to-order, speaker image, applies player settings; calls `DialogueControls.OnLineStarted` | yes |
| `CocktailOption/OptionPresenterCocktailProject.cs` | Shows options, timeout, "unavailable" options, first-available for Skip; stores last line | yes |
| `DialogueControls.cs` | AutoPlay toggle, Skip-to-order + warning popup; static state read by presenters | yes (`Canvas_HUD`) |
| `PauseAwareKeyCodes.cs` | Key-driven "next line / hurry up" with cooldown; ignores input while paused | yes (`Line Advancer`) |
| `EventLog/EventLogPresenter.cs` | Records lines and choices; `Snapshot/Restore/Clear` for save | yes (`[EventLogSystem]`) |
| `Settings/SO_DialogueSettings.cs`, `DialogueSettingStepper.cs` | Defaults + player override (PlayerPrefs `dlg.speed`, `dlg.display`) | yes / Pause scene |
| `SkipScan.cs` | Looks ahead in the compiled Yarn for `flow_prepare_drinks` and options | yes |
| `LineTracker.cs` | (header only — not inspected) | ? |
| `BubblePresenter/*`, `BubbleOptionType1/*`, `BubbleOptionType2/*` | Older bubble-style presenters (hold the old save/replay logic). Per CLAUDE.md not in the scene any more | no |
| `[00]Testing Yarn And Learning/*` | Learning/test prefabs and debug scripts | no |
| `Common Tags Cocktail.asset` | Tag list asset | ? |

Yarn files (`YarnScript/`)
- `Demo/DEMO_3Days.yarn` — the 3-day demo (bound in `DaySchedule_Demo.asset`).
- `Demo/PLAYTEST_Demo.yarn` — 2-day play test; header of the file is the template for writing a new day.
- `Demo/TEST_AllCommands.yarn` — exercises every command (`Docs/YarnCommands/`).
- `Demo/VDO_Rec*.yarn`, `Day1_Demo_EventNabLab.yarn` — video recording / event scripts.
- `YarnFolder1`, `YarnFolder2` — old small scripts.

**Yarn command contract** (names must not change; full list in `Docs/YarnCommands/README.md`)
- Flow: `flow_open_bar`, `flow_check_events`, `flow_next_day`, `flow_prepare_drinks`, `flow_close_bar`, `flow_garnish_done`, `flow_serve_done`, `flow_remake_drink`, `flow_ingredient_added`, `flow_minigame`, `flow_another_ingredient`, `flow_drink_complete`; functions `flow_phase`, `flow_step`, `flow_minigame_type`, `flow_minigame_result`.
- Order: functions `Order_Cocktail_OutName/OutDescription`, `Order_Cocktail_ByName_OutName/OutDescription`, `order_name`, `order_flavor`, `order_type`, `order_satisfaction`; commands `order_by_type`, `order_customer`, `wait_for_task`, `wait_scene`, `Can_End_Shift`.
- Characters: `CharacterExpression/Look/Enter/Exit` and `<<Name verb [arg]>>` for 8 names.
- Camera: `Switch_Camera`, `Cut_Camera`. Unlock: `unlock_ingredient`, `ingredient_unlocked()`. Save: `save_chapter_node`, `eventlog_clear`.

---

## 6. `[04]Prefab`

| Folder | Contents | Notes |
|---|---|---|
| `GameSystemPrefab/` | `[GameLoop]`, `CamController`, `[PauseManager]`, `[SaveLoadSystem]`, `SoundSystem`, `MakingCocktail`, `ObjectMaker`, `GridSelected`, `Dialogue System Edit v004` | `[GameLoop]` is the root of the whole system. `Dialogue System Edit v004` looks like an older rig (not verified) |
| `Bar/`, `BarInteriorV3.prefab`, `Decorates/` | 3D room and lamps | The scene uses `BarInteriorV2` objects, not V3 |
| `Character/` | `NPC_Isla`, `NPC_Owen`, `NPC_Walter`, `NPC_Shadow` | Old per-character prefabs; the scene's `NPC_*` objects are `CharacterView` instances. CLAUDE.md: Isla/Owen/Walter prefabs still used as instances |
| `Ingredient/` | `BaseSpirit/` (Gin, Rum, Tequila, Vodka, Wishkey), `Liqeurs/` (Campari, Dry/Sweet Vermouth, Triplesec), `Mixer/BottleBase/` (Soda, Syrup), `Mixer/FruitBase/` (6 fruits + `FruitInstance/FruitPiece_*`), `BaseInteractbleIngredient` | Bottles use `BottleIngredientSource`; fruit trays use `FruitTraySlot` / `FruitPieceInstance` |
| `ServingGlass/` | `BaseGlass`, `GlassIce`, `Hi_Ball_Glass`, `Margarita`, `Martini`, `Rock_Glass` | One prefab per `SO_GlassOption`; spawned as `PlacedGlassInstance` |
| `UI/` | `PF_Button_Hub` (all HUD/Garnish/Minigame buttons are instances), `PF_EventLog_*` (3 row types) | Editing `PF_Button_Hub` changes every non-overridden button |
| root | `Panel - BookUI`, `Recipebook`, `Text Animator Dialogue System` | Book UI and a second copy of the dialogue rig (the one in `[06]Dialogue` is the one referenced by CLAUDE.md) |

---

## 7. Dependency picture (who knows whom)

- **Flow states** (`Hierarchical State Machine/Level*`) → know nothing but each other. Only `MinigameState` references an enum (`Enum_MiniGameType`) from the cocktail namespace.
- **FlowWiring** → references: all 5 managers, `ShakerContents`, `ShakerPanelController`, `IngredientButtonGroup`, `VisualizeCocktail`, `MethodPanel`, `BookUI_V2`, `Button` (book), `Post_It_Order`, `HubManager`, `PanelTransition`, `SO_DaySchedule`, `ChapterTitleUI`, `EventPopupQueue`, `IngredientUnlocks`, `GameFlowCommands`.
- **GameFlowCommands** → `GameLoopFSM`, `DialogueControls` (static `StopSkip`, `IsSkipping`), `SceneLoaderBridge` (silent replay).
- **Cocktail** → `TalkToCustomerManager` only through the `ICustomerPreferences` interface and events (no direct reference).
- **TalkToCustomerManager** → `DialogueRunner`, `SoundManager`, `SceneLoaderBridge`, `DialogueControls`.
- **Dialogue presenters** → `DialogueControls`, `SceneLoaderBridge`, `SO_DialogueSettings`; `DialogueControls` → `OptionPresenterCocktailProject` (found at runtime).
- **SaveLoadManager** → `DialogueRunner`, `EventLogPresenter`, `SceneLoader` (all via `FindFirstObjectByType`), `TalkToCustomerManager`, `IngredientUnlocks`, `CocktailSystemManager.IsWaitingForTask`.

---

## 8. Things I noticed that matter for the refactor

Ordered roughly by how much they are likely to bite. These are observations, not fixes; each needs your decision.

1. **Container `Entered` fires after its child's `Entered`.** `StateBase.Enter` = `OnEnter()` then `Entered`, and a phase's `OnEnter` starts its child machine. So on a new day `OnEventEntered` runs **before** `OnBeforeOpenEntered`. When the popup queue is empty `AllClosed` fires at once, so `Event.Finish()` → Story → `StartStory(...)` can run **before** the camera cut / look reset in `OnBeforeOpenEntered`. Same shape for Open: `OnTalkingEntered` runs before `OnOpenEntered` (chapter title). Not verified in Play; worth a test before changing anything.
2. **`FlowWiring` is 31 KB and does 5 jobs** (scene start, cameras/locking, hub, Event/unlock popups, garnish/serve, minigame bridge). Natural split lines are the sections already marked in the file (§3.3). Contains duplicated/null-unsafe code in `OnShakerFull` (`_ingredients?.Disable()` then `_ingredients.Disable()`, `_btnBook.interactable` set twice and once without `?.`) and `OnTalkingEntered` (`Disable()` twice).
3. **Many static Yarn commands depend on scene-scoped statics** (`Instance`/`Current` ×8, plus `FindAnyObjectByType` in `CocktailSystemManager`). Fine while there is one scene, but each one is a hidden global.
4. **`CocktailSystemManager.IsWaitingForTask` is a static** read by `SaveLoadManager` (the code comment calls it a deliberate compromise).
5. **Save/Load is detached from the flow.** `[SaveLoadSystem]` is inactive in the scene; the FSM phase is not saved; presenters in the scene don't set `SceneLoaderBridge.CurrentLineId`. Details in CLAUDE.md "สิ่งที่ต้องแก้". The old logic still lives in `BubblePresenter` / `OptionPresenterType2`, which are not in the scene.
6. **`SaveLoadManager` and `SoundManager` use `DontDestroyOnLoad`,** while CLAUDE.md says scene-scoped singletons with no DontDestroyOnLoad (it lists `SoundManager` as an example). One of the two is out of date. Also the scene holds **two `SoundManager` objects**.
7. **`CameraManager` warns "No CinemachineBrain assigned"** although `[CinemachineBrain]` exists in the scene — likely just an empty serialized field.
8. **Legacy `Input.*` is used in:** `MinigameSystemManager`, `IInputProvider`, `PauseManager`, `HoverTooltip`, `UIParallaxMouse`, `GameFlowDebugHotkeys`, `Demo_KeysShortCut`, `DebugPhysicsRaycaster`, while the project lists the Input System package. Not checked which backend the Player Settings enable. Relevant for the controller request in CLAUDE.md.
9. **Runtime `FindFirstObjectByType` calls:** `BTN_2_5D` (InputSystemManager), `VisualizeCocktail` and `IngredientButtonUI` (ShakerContents), `DialogueControls` (option presenter), `SaveLoadManager` (several), `PH_Thx`, `DeselectButton`.
10. **Dead or probably unused (not confirmed by reference search):** `BubblePresenter/`, `BubbleOptionType1/2`, `[00]Testing Yarn And Learning/` (script + prefabs), `NPC/CharacterData` (legacy; still on `NPCs_And_Set`), prefabs `Character/NPC_*`, `BarInteriorV3`, `BarVer_3`, `Dialogue System Edit v004`, `Panel - BookUI` / `Recipebook` (root), `MainMenu/*` scripts (menu scene only), inactive scene objects `BarVer_3`, `Table`, `TestObjectOntable`, `TMP_NPC`.
11. **LEGACY VISUAL branches** in `CharacterView`, `SO_Customer`, `TalkToCustomerManager.Characters` (`grep -rn "LEGACY VISUAL" Assets`).
12. **Naming typos that are now identifiers:** `Stiring` (enum), `[PlacmentSystem]` (scene object), `Deverlopment` (folder), `Liqeurs` (folder), `Wishkey` (prefab). Renaming enums breaks serialized data and Yarn strings (`flow_minigame` accepts `Mixing`/`Stirring` as aliases).
13. **Debug objects shipped in the scene:** `[Bar410DialogueOrderTest]`, `[Debug]` (`GameFlowDebugHotkeys`, `DebugScreenCapture`), `Demo_KeysShortCut`, `CocktailInspectorReadout`, and an editor-only hotkey block in `MinigameSystemManager`.
14. **`ClosingBarPhase` and the end-of-day loop** are empty; `EndShiftButton` and `Panel_Thx` load another scene instead.
15. **Test fallback in the flow:** `AddIngredient` entry calls `RandomCocktailIfNoOrder()`, so a missing `Order_Cocktail_*` in Yarn silently becomes a random drink.

---

## 9. Not covered / not verified

- No Play-mode run; no check that every reference in `FlowWiring` is assigned in the scene.
- Header-only: Domain classes, ingredient/glass/garnish scripts, `GarnishManager` internals, camera shot IDs, `SoundManager`, `PauseManager`, `SaveLoadManager` internals, `BookUI_V2`, the `BaseInteractable` family.
- Not opened: `LineTracker`, `Common Tags Cocktail.asset`, third-party packages (Yarn Spinner, Text Animator, Odin, DOTween, CocktailCrafter_V2), the Pause / Loading / Main menu scenes, `Docs/*` other than CLAUDE.md.
- Which presenter components are on the dialogue prefab canvas was taken from CLAUDE.md, not read from the prefab.
