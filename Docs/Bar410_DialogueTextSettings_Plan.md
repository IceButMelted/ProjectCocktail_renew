# Dialogue Text Speed + Display Type in Pause Settings (plan for Sonnet 5.5)


## Context
The player should be able to change how dialogue text appears from the Settings page in the Pause menu (`PauseScene` › `Panel - Setting`):
- **Text Speed:** Slow / Normal / Fast. Fixed levels, not a slider.
- **Display Type:** Character / Word / Instant.

The user decided:
- **UI:** the user builds it, as a `◀ value ▶` row for each setting. Sonnet writes the scripts only. Wiring waits until the user gives the object names.
- **Storage:** values are stored in `PlayerPrefs`, the same way `SoundManager` does it. They are per player, not per save slot. Do not use `SaveSettingData.TextSpeed`.
- **Single source of truth:** this follows the "Future: Settings menu" section of `Docs/Bar410_DialogueSkipAutoPlay_Plan.md`.
  - Defaults live in `SO_DialogueSettings`.
  - Every read and write goes through the static `DialogueSettings`.

CLAUDE.md rules:
- Work in stages and compile-check each one (`read_console` has no errors).
- Code and comments in English.
- Do not commit.

## What exists today
- **`LinePresenterCocktailProject`** (`Assets/[06]Dialogue/CocktailLine/LinePresenterCocktailProject.cs`) is the line presenter the scene uses.
  - It builds Yarn's own typewriter once in `Awake` (`:170-210`) from the Inspector enum `typewriterStyle` (Instant / ByLetter / ByWord / Custom) and the fields `lettersPerSecond = 60` / `wordsPerSecond = 10`.
  - It uses `LetterTypewriter.CharactersPerSecond` and `WordTypewriter.WordsPerSecond`.
  - It does not use the Text Animator typewriter.
- **`SO_DialogueSettings` + `DialogueSettings`** (`Assets/[06]Dialogue/Settings/SO_DialogueSettings.cs`):
  - Currently holds AutoPlay only, with a TODO for text speed and display type.
  - `DialogueSettings.Init(so)` is called by `DialogueControls.Awake` in the main scene.
  - `PauseScene` loads additively on top of the main scene, so the static is already set while Pause is open.
- **Patterns to copy:**
  - Storage: `SoundManager` (`Assets/[02]Script/Sound/SoundManager.cs:243,257`) saves with `PlayerPrefs.SetFloat(key)` and loads with `GetFloat(key, fallback)`.
  - UI: `VolumeSlider` (`Assets/[02]Script/Sound/VolumeSlider.cs`) reads the value in `OnEnable` without firing a change, adds its listener in `OnEnable`, and removes it in `OnDisable`. It holds no scene references.

## Changes

### 1. `SO_DialogueSettings.cs`: defaults + accessor
Add the enums to the same file:
```csharp
public enum TextSpeed { Slow, Normal, Fast }
public enum TextDisplay { Character, Word, Instant }
```
Then:
- **Fields on `SO_DialogueSettings`:**
  - `[Header("Text")]`, `TextSpeed defaultSpeed = Normal` and `TextDisplay defaultDisplay = Character`.
  - `int[] lettersPerSecond = { 30, 60, 120 }` and `int[] wordsPerSecond = { 5, 10, 20 }`. The index is the `TextSpeed` value.
  - Replace the TODO with a comment that says these fields are the defaults the player can override.
- **`DialogueSettings` (static):**
  - `Speed` / `Display` getters: `PlayerPrefs.GetInt("dlg.speed" / "dlg.display", (int)default)`. With no SO loaded, the fallback is Normal / Character.
  - Clamp the stored int to the enum range, in case PlayerPrefs holds an old or out-of-range value.
  - `SetSpeed(TextSpeed)` / `SetDisplay(TextDisplay)`: write PlayerPrefs, then fire `public static event Action Changed`.
  - `LettersPerSecond` / `WordsPerSecond`: read the array at index `(int)Speed`. With no SO, fall back to 60 / 10.
  - Don't change `AutoPlayDelay`. Remove the "FUTURE" comment for the parts that are now built.

### 2. `LinePresenterCocktailProject.cs`: build the typewriter from settings
- **Check first:** look at the scene's `typewriterStyle` value. If it is `Custom`, stop and ask the user. Otherwise:
  - Remove `typewriterStyle`, `lettersPerSecond`, `wordsPerSecond`, `customTypewriter` and the `TypewriterType` enum.
  - The Settings now own the typewriter.
- **Move the build into a method:**
  - Move the switch into `void BuildTypewriter()`, which switches on `DialogueSettings.Display`.
  - Character → `LetterTypewriter { CharactersPerSecond = DialogueSettings.LettersPerSecond }`
  - Word → `WordTypewriter { WordsPerSecond = DialogueSettings.WordsPerSecond }`
  - Instant → `InstantTypewriter`
  - Keep `ActionMarkupHandlers` / `TextElement` as they are.
- **When to build:**
  - Call `BuildTypewriter()` from `Awake`.
  - `OnEnable`: `DialogueSettings.Changed += MarkTypewriterDirty`; `OnDisable`: `-=`. `MarkTypewriterDirty` only sets `bool _typewriterDirty = true`.
  - At the start of `RunLineAsync`, before `PrepareForContent`: if dirty, rebuild and clear the flag.
  - The rebuild happens between lines, so a line that is typing when the player changes the setting is never swapped mid-way.
  - The game is paused while Settings is open, so the next line picks up the new values.
- **Order matters:** `DialogueControls.Awake` sets `DialogueSettings.Init`. If `LinePresenterCocktailProject.Awake` can run first, the presenter builds with the fallbacks.
  - Either set `_typewriterDirty = true` in Awake as well, so the first line always rebuilds with the SO loaded,
  - or give `DialogueControls` an earlier `[DefaultExecutionOrder]`.
  - Pick the smaller one.

### 3. New `Assets/[06]Dialogue/Settings/DialogueSettingStepper.cs`: one `◀ value ▶` row
- **Serialized fields:**
  - `enum Target { Speed, Display } target`
  - `Button previous`, `Button next`, `TMP_Text valueLabel`
  - `string[] labels`: what each value shows, in enum order. For example, Speed → "ช้า","ปกติ","เร็ว" and Display → "ตัวอักษร","คำ","ทันที". The user types these in the Inspector.
- **`OnEnable`:**
  - Show the current value from `DialogueSettings`.
  - `previous.onClick.AddListener(() => Step(-1))` and the same for `next`.
- **`OnDisable`:** remove the listeners.
- **`Step(int dir)`:**
  - Compute the new index and **wrap around**: Fast + ▶ → Slow.
  - Call `SetSpeed` / `SetDisplay`, then refresh the label.
- **Labels:** if `labels` is shorter than the enum, show the enum name. Never throw.
- **No scene references**, the same as `VolumeSlider`, so the same row also works if a Main Menu settings page is added later.
- **Controller:** both arrows are normal uGUI Buttons, so gamepad navigation works with no extra code.

### 4. Scene wiring: wait for the user
- After stages 1-3 compile, **stop and ask the user** for the object names in `PauseScene` › `Panel - Setting` (the two rows, their ◀ / ▶ buttons and their value labels).
- Then add `DialogueSettingStepper` to each row, assign the references and labels, and save `PauseScene`.

## Stages (compile-check after each)
1. `SO_DialogueSettings` enums, fields and accessor. Fill the arrays on `DialogueSettings.asset`.
2. `LinePresenterCocktailProject` builds the typewriter from settings.
3. `DialogueSettingStepper`.
4. Ask for the object names, wire them, save `PauseScene`.
5. Play test.

## Verification
- **Compile:** `read_console` shows no errors after every stage.
- **Before the UI exists:** test in Play via MCP `execute_code`:
  - `DialogueSettings.SetDisplay(Word)` → the next line is typed word by word.
  - `SetDisplay(Instant)` → the next line appears at once.
  - `SetSpeed(Fast)` / `SetSpeed(Slow)` → typing time clearly changes. Log `Typewriter` type and speed after the rebuild.
- **Persistence:** set a value, stop Play, start again → the same value is still there (PlayerPrefs `dlg.speed` / `dlg.display`).
- **After wiring:**
  - Open Pause › Settings → the rows show the current values.
  - ◀ / ▶ cycle the values and wrap at the ends.
  - Close Pause → the next line uses the new setting.
- **Check against the existing features:**
  - AutoPlay still advances after typing ends.
  - Skip still works with Instant.
  - Hurry-up (Space) still finishes a line in every mode.
- **Tell the user what was not tested by real input:** real mouse or gamepad on the arrows.

## Out of scope
- AutoPlay delay in Settings, a Main Menu settings page, and "Reset to defaults". The design allows each of these later, but none is built now.
- Text Animator typewriter: not used by this presenter, so don't touch it.
