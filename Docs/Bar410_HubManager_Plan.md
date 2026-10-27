# HubManager: Hub UI shown per Phase, set in the Inspector (plan for Sonnet 5.5)


## Context
Which Phase shows which Hub UI (buttons on `Canvas_HUD`) is hard-coded today. For example, AutoPlay/Skip are shown and hidden by `FlowWiring` → `DialogueControls.SetVisible`. Changing the rule means editing code.

The user wants a `HubManager` with a table in the Inspector: one row per Hub UI, with a box to tick for each Phase it shows in.

Agreed rules:
- **Hub UI** (pure on/off by Phase): goes in the HubManager table.
- **UI not in the table** is always shown. `BTN_Setting` stays out of the table.
- **System UI is still controlled by its Manager, as today:**
  - Garnish → `GarnishManager`
  - Minigame → `MinigameSystemManager`
  - Post-it, Method, fill-bar readout → their own scripts
  - Dialogue → Yarn
  - Don't put these in the table. They have their own timing (slide, intro/outro), and two systems would fight over them.
- **Outside the ticked Phases:** fade out and not clickable. There is no Lock mode.
- **Who decides:** a UI that only shows or hides by Phase goes in the table. A UI with its own animation, timing or system goes to its Manager.

CLAUDE.md rules:
- Work in stages and compile-check each one (`read_console` shows no errors).
- Code and comments in English.
- Test in Play via MCP and say plainly what is untested.
- Do not commit.
- Every report lists the files touched and their GameObjects.
- New components go into the CLAUDE.md Component table.

## How it is wired today (for context)
State machine (plain C#) → state `Entered` / `Exited` events → `FlowWiring` → manager verbs.

`FlowWiring` (`Assets/[02]Script/Hierarchical State Machine/FlowWiring.cs`) is the only place where the flow and the managers meet:
- `OnTalkingEntered` and `OnConversationExited` call `_dialogueControls?.SetVisible(true/false)`.
- `DialogueControls` (`Assets/[06]Dialogue/DialogueControls.cs`) holds that fade today: `SetVisible`, `Fade`, `ApplyAlpha`, `EnsureButtonGroups`, `GroupOf`, `fadeDuration`, `_buttonGroups`. Its Awake calls `EnsureButtonGroups()`.
- `BTN_Toggle_AutoPlay` and `BTN_Skip` already have a CanvasGroup.

## Build

### 1. New `Assets/[02]Script/UI Script/HubManager.cs`, on `[GameLoop]/Canvas_HUD`
It sits on its own Canvas, the same as the other Managers (Cocktail, Garnish, …).
```csharp
[System.Flags] public enum HubPhase { None = 0, Talking = 1, AddIngredient = 2, Minigame = 4, Garnish = 8, Serve = 16, Close = 32 }
```
- **Fields:** `[SerializeField] List<Row> rows` and `[SerializeField, Min(0)] float fadeDuration = 0.25f`.

#### Inspector, built with Odin Inspector (Sirenix, already in the project)
The goal is for the Inspector to look and work like the Figma table, so the Phases can be set without opening code.

```csharp
[Serializable] class Row
{
    [TableColumnWidth(160)] [Required] [SceneObjectsOnly] public GameObject target;
    [EnumToggleButtons, HideLabel] public HubPhase shownIn;
    [TableColumnWidth(160)] public string note;   // e.g. "Fade with Garnish too"
}

[InfoBox("Hub UI that only shows or hides by Phase. UI not listed here is always shown. Garnish / Minigame / Post-it UI is controlled by its own Manager — don't add it here.")]
[TableList(AlwaysExpanded = true, ShowIndexLabels = false), ValidateInput(nameof(NoDuplicateTargets), "The same target appears in more than one row")]
[SerializeField] List<Row> rows;
```

- **`TableList`:** one row per UI. `EnumToggleButtons` turns the Phases into a row of toggle buttons (Talking | AddIngredient | Minigame | Garnish | Serve | Close), and several can be pressed at once.
- **`Required` + `SceneObjectsOnly`:** an empty target shows red, and only scene objects can be dragged in.
- **`ValidateInput`:** warns when the same target appears in 2 rows.
- **Preview in edit mode:**
  - `[Button, HorizontalGroup("Preview")] void PreviewPhase(HubPhase phase)`: sets each row's CanvasGroup alpha right away, so you can see the screen for that Phase without pressing Play.
  - `[Button("Show all"), HorizontalGroup("Preview")] void PreviewReset()`: returns every row to alpha 1.
  - **The preview must not leave changes in the scene:**
    - Change values with `Undo.RecordObject`, so preview changes can be undone.
    - Wrap the editor-only code in `#if UNITY_EDITOR`.
- **During Play:**
  - Show the current Phase with `[ShowInInspector, ReadOnly] HubPhase CurrentPhase` (`[HideInEditorMode]`).
  - Use `[PropertySpace]` / `[Title("Rows")]` to group sections.

Look at how other files in the project already use Odin attributes (search `Sirenix.OdinInspector`) and follow the same style.
- **`public void SetPhase(HubPhase phase)`:** for each row, `on = (shownIn & phase) != 0`.
  - Fade the CanvasGroup alpha to 1 or 0 using unscaled time.
  - Clicks are off for the whole fade. At the end, `interactable` and `blocksRaycasts` = `on`.
  - Use one coroutine per call, and stop the old one first. Same pattern as the current `DialogueControls.Fade`.
- **CanvasGroup per row:**
  - Take it from `target`, or add one if it's missing. Do this lazily, because `FlowWiring` can call `SetPhase` before this component's Awake. The same issue was already hit in `DialogueControls`.
  - Never call `SetActive`. The CanvasGroup is an outer gate, so other scripts can still open and close their own UI inside the allowed Phases.
- **Bad rows:** a null target logs a warning once and is skipped. It never throws.
- **Knows only `HubPhase`, not the flow.** That matches the rule that Managers don't know the flow.

### 2. `FlowWiring.cs`
- **Field:** add `[SerializeField] private HubManager _hub;` under Chrome.
- **One line per Phase,** called on each state's Entered:
  - `TalkingWithCustomer.Entered` (`OnTalkingEntered`) → `_hub?.SetPhase(HubPhase.Talking)`
  - `PrepareDrinks.AddIngredient.Entered` (`OnAddIngredientEntered`) → `AddIngredient`
  - `PrepareDrinks.Minigame.Entered` → `Minigame`. This is a new subscription; unsubscribe it in OnDestroy.
  - `Garnish.Entered` (`OnGarnishEntered`) → `Garnish`
  - `Serve.Entered` (`OnServeEntered`) → `Serve`. It is an expression body today, so turn it into a block.
  - `_gameLoop.ClosingBar.Entered` → `Close`. This is a new subscription. Close has no UI yet, but the flag is ready.
- **Remove** `_dialogueControls?.SetVisible(...)` (2 places) and the `_dialogueControls` field.
- Update the summary comment at the top of the file to mention that HubManager shows and hides Hub UI per Phase.

### 3. `DialogueControls.cs`
- Remove `SetVisible`, `Fade`, `ApplyAlpha`, `EnsureButtonGroups`, `GroupOf`, `fadeDuration`, `_fade` and `_buttonGroups`.
- Remove the `EnsureButtonGroups()` call in Awake.
- Keep the rest: AutoPlay, Skip and the warning.

### 4. Scene `New Drag Drop System`
- Add `HubManager` to `[GameLoop]/Canvas_HUD`.
- Starting rows:

| target | shownIn |
|---|---|
| `Canvas_HUD/BTN_Toggle_AutoPlay` | Talking |
| `Canvas_HUD/BTN_Skip` | Talking |
| `Canvas_HUD/BTN_OpenEventLog` | Talking, AddIngredient, Close |
| `Canvas_HUD/BTN_Open_CloseBook` | AddIngredient, Minigame, Garnish |

- Assign `_hub` on `FlowWiring`, then save the scene.
- **Note on the book button:** it stays a member of `IngredientButtonGroup`, which still locks and unlocks it. It is clickable only when both allow it. Don't change that group.

### 5. CLAUDE.md
- **Component table:** add a `HubManager` row. Update the `DialogueControls` row, because the fade is gone.
- **Under "สถาปัตยกรรม (ต้องรักษา)":** add 1 line: "Hub UI (pure on/off by Phase) goes through `HubManager` on `Canvas_HUD`, fed by `FlowWiring`. System UI (Garnish, Minigame, Post-it, etc.) is still controlled by its Manager."

## Stages (compile-check after each)
1. `HubManager.cs`
2. `FlowWiring` and `DialogueControls` edits
3. Scene wiring and save
4. Play test, then update CLAUDE.md

## Verification
- **Compile:** `read_console` shows no errors after every stage.
- **Play** via MCP, driving the steps with `GameFlowCommands` (PrepareDrinks → IngredientAdded("Shaking") → DrinkComplete → GarnishDone → ServeDone). Wait about 0.3 s after each step, then read the alpha and interactable of each row's CanvasGroup.

| Phase | AutoPlay / Skip | Event Log | Book |
|---|---|---|---|
| Talking | visible | visible | hidden |
| AddIngredient | hidden | visible | visible |
| Minigame | hidden | hidden | visible |
| Garnish | hidden | hidden | visible |
| Serve | hidden | hidden | hidden |
| Back to Talking | visible | visible | hidden |

- **BTN_Setting:** visible in every Phase.
- **Odin Inspector, in edit mode:**
  - The table shows 4 rows with Phase toggle buttons.
  - `PreviewPhase(Garnish)` → AutoPlay, Skip and Log fade, and the Book stays.
  - `Show all` → everything returns.
  - Undo reverts the preview.
  - An empty target turns red.
  - A duplicate target shows a warning.
  - Take a screenshot of the Inspector, or describe what you see, in the report.
- **Untested via MCP:** real clicks, how smooth the fade looks, and the Close phase (nothing enters it yet). Say this plainly in the report.

## Out of scope
- Moving Garnish, Minigame, Post-it, Book panel, Serve panel or Dialogue into the table.
- Pause UI, which lives in `PauseScene`.
- Lock mode.
