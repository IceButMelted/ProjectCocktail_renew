# Dialogue AutoPlay + Skip-to-order (plan for Sonnet 5.5)


## Context
The designer set up two buttons on the dialogue HUD in `New Drag Drop System`, plus a warning popup:
- `BTN_Toggle_AutoPlay` turns AutoPlay on and off. With AutoPlay on, a line advances by itself once it has finished typing.
- `BTN_Skip` jumps ahead to the next drink order, the way silent replay fast-forwards. Before skipping, the game looks ahead. If the player would have to answer a question on the way, `Panel_SkipWarning` asks first.
- `Panel_SkipWarning` (CanvasGroup) holds `Blocker` (click outside = cancel), `Button_CancelSkip` and `Button_ConfirmSkip`.

The user's decisions:
1. **Where Skip stops:** on the line that carries the order (the line with `[order]` markup, e.g. `Walter: [order] "{Order_Cocktail_ByName_OutName(...)}" [/order] ...`). That line plays normally, then the dialogue continues normally into `<<flow_prepare_drinks>>`.
2. **Options during a skip:** always pick the first available option.
3. **AutoPlay timing:** read it from a dialogue settings asset that is the single source of truth. Mark that asset as the future home of text speed and display type too, but do not build those now.
4. **Skipped lines go into the Event Log**, including the options that were auto-picked.

Follow the CLAUDE.md rules:
- Work in stages and compile-check each one (`read_console` has no errors).
- Code and comments in English.
- Do not commit.

## Existing pieces to reuse
- `SceneLoaderBridge` (`Assets/[02]Script/SceneLoader/SceneLoaderBridge.cs`) holds `IsSilentReplay`, `CurrentLineId` and `SessionOptionChoices`. **Do not reuse `IsSilentReplay` for Skip.** It makes `GameFlowCommands.CommandTarget()` drop every flow command, and it short-circuits `WaitForTask`, `EnableButtonInYarn` and `ResetVariableInYarn`. Skip must let all of those run for real.
- How silent replay works today:
  - `BubblePresenter.RunLineAsync` (`Assets/[06]Dialogue/BubblePresenter/BubblePresenter.cs:289-306`) returns early for lines it doesn't show.
  - `OptionPresenterType2.RunOptionsAsync:167-181` auto-picks an option.
  - Follow the same pattern for Skip.
- `OptionPresenterCocktailProject.RunOptionsAsync` (`Assets/[06]Dialogue/CocktailOption/OptionPresenterCocktailProject.cs:184`) is the scene's option presenter. Its `OptionChosen` event feeds the Event Log, so the auto-pick must raise it with `timedOut = false`.
- `EventLogPresenter` (`Assets/[06]Dialogue/EventLog/EventLogPresenter.cs`) only skips logging during `IsSilentReplay`, so skipped lines are logged with no change there.
- `PausePanel` (`Assets/[02]Script/Pause/PausePanel.cs`) on a panel freezes dialogue (timeScale 0, and `PauseAwareKeyCodes` ignores keys). This is the same freeze the Event Log uses, so reuse it on `Panel_SkipWarning`.
- `CocktailSystemManager.WaitScene` (`<<wait_scene>>`, `Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnTask.cs:102`) is a pure wait. It should also be skipped while skipping.

## New files
### 1. `Assets/[06]Dialogue/Settings/SO_DialogueSettings.cs` + one asset `DialogueSettings.asset`
```csharp
[CreateAssetMenu(menuName = "Bar410/Dialogue Settings")]
public class SO_DialogueSettings : ScriptableObject
{
    // Single source of truth for dialogue presentation.
    // TODO: move text speed (Text Animator typewriter) and display type here.
    [Header("Auto Play")]
    [Min(0f)] public float autoPlayBaseDelay = 1f;      // seconds after the line finishes typing
    [Min(0f)] public float autoPlayPerCharDelay = 0.03f; // extra seconds per visible character
    public float AutoPlayDelay(int charCount) => autoPlayBaseDelay + autoPlayPerCharDelay * charCount;
}
```
Keep it to these fields. The TODO comment is the marker the user asked for.

**Read rule, so a Settings menu can plug in later:** no presenter or UI reads the SO directly. Every read goes through one static accessor, `DialogueSettings` (in the same file):
```csharp
public static class DialogueSettings
{
    static SO_DialogueSettings _defaults;              // set once by DialogueControls.Awake
    public static void Init(SO_DialogueSettings defaults) => _defaults = defaults;
    public static float AutoPlayDelay(int charCount) => _defaults != null ? _defaults.AutoPlayDelay(charCount) : 1f;
    // FUTURE (Settings menu): player overrides + PlayerPrefs + `event Action Changed` live here.
}
```
- The SO holds the **designer defaults** and is never written at runtime.
- `DialogueSettings` is the only door the rest of the code uses.

### 2. `Assets/[06]Dialogue/DialogueControls.cs` (one MonoBehaviour on the HUD that owns both buttons)
- **Serialized fields:**
  - `SO_DialogueSettings settings`
  - `DialogueRunner runner`
  - `Button autoPlayButton` and the on/off visual. First check what `BTN_Toggle_AutoPlay` already has (Button, Toggle, or a child "on" image). Use what is there, and do not add a new visual system.
  - `Button skipButton`
  - `CanvasGroup skipWarning`
  - `Button blocker`, `Button cancelSkip`, `Button confirmSkip`
- **Static state:** `public static bool AutoPlay { get; private set; }` and `public static bool IsSkipping { get; private set; }`. Awake calls `DialogueSettings.Init(settings)`. Static matches the `SceneLoaderBridge` and `PauseManager.IsPaused` style that the presenters already read.
- **AutoPlay toggle:** flips `AutoPlay` and updates the visual. AutoPlay does not carry over into options: an options block still waits for the player.
- **Skip click:**
  1. Run `SkipScan.FindOrderAhead(runner, out bool hasOptions)`.
  2. If no order is ahead, do nothing. The button should already be non-interactable in that case (see refresh below).
  3. If `hasOptions` → show the warning: CanvasGroup alpha 1 / interactable / blocksRaycasts, plus the PausePanel freeze.
  4. Otherwise → `BeginSkip()`.
- **Confirm** → hide the warning, then `BeginSkip()`. **Cancel / Blocker** → hide the warning.
- **`BeginSkip()`:**
  - Set `IsSkipping = true`.
  - Call `runner` `RequestNextLine()` so the line on screen ends now.
  - If options are open at that moment, the option presenter picks the first one.
- **`public static void StopSkip()`:** sets `IsSkipping = false`.
- **Skip button refresh:**
  - Every time a line begins, set `skipButton.interactable = !IsSkipping && SkipScan.FindOrderAhead(...)`. The cheapest hook is an event `BubblePresenter.LineStarted`, or have `BubblePresenter` call `DialogueControls.OnLineStarted()`.
  - Also refresh on dialogue complete, which sets it non-interactable.
- **Save guard:** in `SaveLoadManager.SaveToFile`, return early (with a warning) while `DialogueControls.IsSkipping`, the same way it does for silent replay.

### 3. `Assets/[06]Dialogue/SkipScan.cs` (static, look-ahead over the compiled Yarn program)
`static bool FindOrderAhead(DialogueRunner runner, out bool hasOptions)`:
- **Start point:**
  - Start at `runner.Dialogue.CurrentNode` (check the exact property name in the installed Yarn Spinner version).
  - Get the node from `runner.YarnProject.Program.Nodes`.
  - Begin at the instruction whose `RunLine.LineID == SceneLoaderBridge.CurrentLineId`. If no such instruction is found, begin at 0.
- **Walk the instructions in order and follow jumps:**
  - Follow `RunNode` / `PeekAndRunNode` / detour into the target node from its first instruction.
  - Keep a visited-set of node names so loops end.
  - Scan branches linearly. That is conservative: an option in any branch counts.
  - **Before writing the walk, check the installed package's `Yarn.Instruction` API** (`InstructionTypeCase` names), because it differs between Yarn Spinner 2 and 3.
- **`AddOption` / `ShowOptions`** → `hasOptions = true`.
- **`RunCommand`** whose text starts with `flow_prepare_drinks` → return true. This is the end marker. Options found after it don't count.
- **End of program / `Stop`** → return false.

## Edits to existing code
- **`BubblePresenter.RunLineAsync`**, right after the silent-replay block:
  - While `DialogueControls.IsSkipping`:
    - If the line has the `order` markup attribute (`line.Text.TryGetAttributeWithName("order", out _)`) → `DialogueControls.StopSkip()` and show the line normally.
    - Otherwise → return early.
  - The Event Log still receives the line, because it is its own presenter.
  - **AutoPlay:**
    - In the "Wait for input" loop (`:389-394`), keep a timer that adds `Time.deltaTime` each frame. It is 0 while paused, so the timer freezes in Pause and while the Event Log or the Skip warning is open.
    - Once `DialogueControls.AutoPlay` is on and the timer reaches `DialogueSettings.AutoPlayDelay(lineBody visible length)`, call `lineAdvancer.RequestNextLine()`.
    - Read the delay each frame, not once per line, so a later Settings change applies at once.
    - Reset the timer for each line. Turning AutoPlay on in the middle of a wait starts counting from that moment.
- **`OptionPresenterCocktailProject.RunOptionsAsync`**, at the top after the all-unavailable guard:
  - While `DialogueControls.IsSkipping` → pick the first `IsAvailable` option, `OptionChosen?.Invoke(option, false)`, then return it with no UI.
  - If options are already on screen when the skip starts, the presenter must resolve with that same first option. Wire this through its existing completion source when `IsSkipping` turns on, or let `BeginSkip` call a public `ChooseFirstAvailable()` on the presenter.
  - Pick whichever of the two is the smaller change.
- **`OptionPresenterType2`:** if it is also registered on the scene's DialogueRunner, give it the same skip branch. Otherwise leave it alone.
- **`CocktailSystemManager.WaitScene`:** `if (SceneLoaderBridge.IsSilentReplay || DialogueControls.IsSkipping) yield break;`
- **`GameFlowCommands.PrepareDrinks()`:** add `DialogueControls.StopSkip();` as a safety stop in case an order line has no `[order]` markup.
- **`DialogueControls` end-of-dialogue handling:** call `StopSkip()` when the runner's dialogue completes.

## Scene wiring (`New Drag Drop System`)
- Add `DialogueControls` to the object that owns the HUD buttons. Assign every reference, the DialogueRunner and `DialogueSettings.asset`.
- On `Panel_SkipWarning`: add the PausePanel setup, the same as `Panel_EventLog`, and make sure it starts hidden.
- Wire the button listeners in code (`onClick.AddListener` in OnEnable / RemoveListener in OnDisable), not as Inspector UnityEvents. That matches `Post_It_Order`.
- Save the scene.

## Stages (compile-check after each)
1. `SO_DialogueSettings` and its asset.
2. `SkipScan`. Unit-check it via MCP `execute_code` on `Day1_Demo.yarn`:
   - From the start of `D1_01_Walter_Arrive` → true with hasOptions = true.
   - From the start of `D1_02_Walter_Order` → true with hasOptions = false.
   - From `D1_14_Owen_Leave` → false.
3. `DialogueControls` plus the presenter, command and save edits.
4. Scene wiring.
5. Play test.

## Verification
- **Compile:** `read_console` shows no errors after every stage.
- **Play** via MCP, talking to Walter:
  - **AutoPlay:** turn it on, and check that lines advance by themselves after the delay. An options block waits for the player. Opening Pause or the Event Log freezes the timer.
  - **Skip from `D1_01` (options ahead):**
    - The warning appears and the dialogue is frozen.
    - Cancel / Blocker → nothing changes.
    - Confirm → lines fly past, and the first option is picked.
    - The skip stops on Walter's `[order]` line, and the Post-it shows Sea Breeze. After that the flow enters Prepare Drinks.
    - The Event Log contains the skipped lines and the auto-picked option.
  - **Skip with no options ahead:** it skips at once, with no warning.
  - **No order ahead** (after the last order): `BTN_Skip` is non-interactable.
- **Tell the user what was not tested by real input:** mouse clicks on the real buttons, if MCP only invoked `onClick`, and save during or after a skip.

## Future: adjusting values from a Settings menu (design only, do not build now)
This is the path for a player Settings panel. It lives in `PauseScene` next to the existing Pause sub-panels. Nothing outside `DialogueSettings` should need to change.

1. **Player overrides in `DialogueSettings`:**
   - Nullable fields per value, e.g. `float? _autoPlayBaseDelay`.
   - A getter that returns the override, or the SO default when there is none.
   - Setters `SetAutoPlayBaseDelay(float)` etc. that fire `public static event Action Changed`.
   - `ResetToDefaults()` clears the overrides.
2. **Persistence:** `PlayerPrefs` keys `dlg.<field>`. Load in `Init`, save in each setter. These are per-player options, not per-save, so they stay out of `SaveLoadManager`.
3. **Values that move into the same pattern:**
   - Text speed: Text Animator typewriter wait per character, applied on `Changed` to the scene's typewriter.
   - Display type (e.g. bubble vs box).
   - AutoPlay on/off, if the user wants it remembered.
   - Each value gets an SO default field, an override, and a setter. AutoPlay delay is the first example.
4. **Settings UI:**
   - Sliders and toggles call the setters, and read the getters on open.
   - Live objects (`BubblePresenter`, the typewriter) subscribe to `Changed` in OnEnable, or simply read the getter each line.
5. **Controller support** (designer request in CLAUDE.md): Settings widgets use normal uGUI Selectables so navigation works with a gamepad.

Ask the user before building any of this. Which values, which UI, and whether AutoPlay on/off persists are their decisions.

## Out of scope / flag to the user
- Text speed, display type, player overrides, PlayerPrefs and the Settings UI: these are in the Future section above. Build none of them now.
- `OptionPresenterCocktailProject` does not add to `SceneLoaderBridge.SessionOptionChoices` (only `OptionPresenterType2` does), so a save/load replay may pick the wrong options. This gap already exists. Report it, don't fix it.
