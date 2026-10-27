# Bar410 — Event Log (dialogue history) — Implementation Plan

> Written for an implementing agent (Sonnet 5.5). Read `CLAUDE.md` first. Work in stages, compile-check
> after every stage (`read_console` has no errors), and say plainly what was not tested. Do not commit
> unless the user asks. Conversation with the user is in Thai; code, comments and commit messages in English.

## 1. Goal
The player can open a panel and read back what was said in today's conversations:
- every character line (speaker name + text)
- what the player chose at each option (the option's text, spoken by the player)
- `*Silent*` when the player let the option timer run out

The log covers the **whole day**, is **kept in the save**, and is **cleared at the start of a new day** by a
Yarn command. Each character name is shown **in that character's own colour**.

## 2. Decisions already made (do not re-ask)
| Topic | Decision |
|---|---|
| Scope | Whole day, saved with the game, cleared by a Yarn command at day start |
| Player speaker name | A fixed string set in the Inspector (default `Player`, which matches the `Player:` lines in the .yarn files, so they group together) |
| What a choice logs | The text of the option that was pressed, one entry |
| Timeout | Logs the text `*Silent*` as the player |
| Row templates | Turn the two scene objects into prefab assets in `[04]Prefab`, then remove the samples from `Content` |
| Name colour | **Option A (SO_Customer)** is the recommendation. Section 8 describes both A and B; build the one the user picks, A by default |

## 3. What exists today (verified 2026-10-26)
**Scene `New Drag Drop System`:**
- `[GameLoop]/Canvas_HUD/Panel_EventLog` — Image, child `Scroll View` (ScrollRect) → `Viewport/Content`
- `Content` holds two sample rows (scene objects, **not** prefab assets yet):
  - `PF_EventLog_With_Name` — Image, VerticalLayoutGroup, ContentSizeFitter; children `TMP_Name`, `TMP_Text` (TextMeshProUGUI)
  - `PF_EventLog_WithOut_Name` — same, child `TMP_Text` only
- `[GameLoop]/Canvas_HUD/BTN_EventLog` — Button, nothing wired
- DialogueRunner: `Text Animator Dialogue System` (prefab instance of `[04]Prefab/Text Animator Dialogue System.prefab`)
  - presenters: `LinePresenterCocktailProject`, `OptionPresenterCocktailProject`

**Code:**
- `Assets/[06]Dialogue/CocktailOption/OptionPresenterCocktailProject.cs` — option UI. Timeout = the option tagged
  `#timeout` (visible) or `#SilentTimeout` (hidden) is picked by `RunTimeout()` after `autoSelectDuration`.
  **No .yarn file uses either tag yet**, so `*Silent*` only appears once a writer adds one.
- Yarn Spinner has **no "option selected" event**, and `DialoguePresenterBase.RunOptionsAsync` returns `null` by
  default (means "this presenter did not pick"). `runSelectedOptionAsLine` would re-show the option through the
  line presenter — do not use it.
- `Assets/[02]Script/SceneLoader/SceneLoaderBridge.cs` — `static bool IsSilentReplay` is true while a loaded save
  fast-forwards the dialogue to the saved line. `Post_It_Order.SetPostItOrderText` already skips side effects with it.
- `Assets/[02]Script/SaveLoad/SaveLoadManager.cs` — `SaveData` (bottom of file) holds `MetaData`, `YarnData`,
  `SettingData`, `NPCData`. `SaveToFile` collects, `OnSceneLoaded` applies when `_pendingLoad`. It finds scene objects
  with `FindFirstObjectByType` (DontDestroyOnLoad manager, scene objects die on reload).
- `Assets/[02]Script/Cocktail System/Session/SO_Customer.cs` — per-character asset: `Id` (NPC_Name), `CustomerName`,
  `PreferredDrinkTypes`. Assets in `Assets/[02]Script/NPC/CustomerData/`: `Isla`, `Owen`, `Walter` +
  `Demo_CustomerRoster` (`SO_CustomerRoster.Customers`). Cole and Freya have no asset yet.
- Speakers used in the game .yarn files: `Owen`, `Isla`, `Player`, `Walter` (others are in test yarn only).

## 4. Architecture
```
DialogueRunner ──RunLineAsync──▶ EventLogPresenter (new, a DialoguePresenterBase)
                                   │  records entries, renders rows, owns the panel
OptionPresenterCocktailProject ──OptionChosen(option, timedOut)──▶ EventLogPresenter
<<eventlog_clear>> (Yarn) ──────▶ EventLogPresenter.Clear()
SaveLoadManager ──Snapshot()/Restore()──▶ EventLogPresenter
```
Rules kept from `CLAUDE.md`: composition, no new singleton (SaveLoadManager finds it like it finds the runner),
the log never touches game flow, so it does **not** go through `FlowWiring`.

## 5. Data
New file `Assets/[02]Script/SaveLoad/SaveEventLogData.cs`:
```csharp
using System.Collections.Generic;

[System.Serializable]
public class EventLogEntry
{
    public string Speaker;   // "" for a line with no character name
    public string Text;
    public bool ShowName;    // true → With_Name row, false → WithOut_Name row
}

public class SaveEventLogData
{
    public List<EventLogEntry> Entries = new List<EventLogEntry>();
}
```
`ShowName` is stored (not recomputed) so a restored log looks exactly like it did, including the
"first line of a conversation always shows the name" rule.

## 6. Stages

### Stage 1 — Option event (`OptionPresenterCocktailProject.cs`)
1. Add `public event System.Action<DialogueOption, bool> OptionChosen;` (args: chosen option, timedOut).
2. In `RunTimeout`, set a local/field flag `timedOut = true` right before `source.TrySetResult(fallback)`.
   Simplest: a private `bool _timedOut` reset to false at the top of `RunOptionsAsync`.
3. At the end of `RunOptionsAsync`, **after** the `NextContentToken.IsCancellationRequested` early return and just
   before `return completedTask;`: `if (completedTask != null) OptionChosen?.Invoke(completedTask, _timedOut);`
   (So a cancelled option set logs nothing.)
4. Keep the file's style (it is a modified Yarn Spinner file — keep the header comment, mark the addition).
- Compile-check.

### Stage 2 — `EventLogPresenter` recording (no UI yet)
New file `Assets/[06]Dialogue/EventLog/EventLogPresenter.cs`, `public class EventLogPresenter : DialoguePresenterBase`.

Serialized fields:
```csharp
[SerializeField] OptionPresenterCocktailProject _options;   // source of OptionChosen
[SerializeField] string _playerName = "Player";
[SerializeField] string _silentText = "*Silent*";
```
State: `readonly List<EventLogEntry> _entries`, `string _lastSpeaker` (null = next named line shows its name).

Behaviour:
- `OnEnable`/`OnDisable`: subscribe/unsubscribe `_options.OptionChosen`.
- `OnDialogueStartedAsync`: `_lastSpeaker = null;` return `YarnTask.CompletedTask`.
- `OnDialogueCompleteAsync`: return `YarnTask.CompletedTask`.
- `RunLineAsync(LocalizedLine line, LineCancellationToken token)`:
  `Add(line.CharacterName ?? "", line.TextWithoutCharacterName.Text);` then **return `YarnTask.CompletedTask`
  immediately** — the runner waits for every presenter, so never await here.
- Option handler: `Add(_playerName, timedOut ? _silentText : option.Line.TextWithoutCharacterName.Text);`
- Do **not** override `RunOptionsAsync` (base returns null = "not mine").
- `Add(speaker, text)`:
  - `if (SceneLoaderBridge.IsSilentReplay) return;` (a loaded save replays lines it already restored)
  - `bool showName = !string.IsNullOrEmpty(speaker) && speaker != _lastSpeaker;`
  - `_lastSpeaker = speaker;` (a no-name line sets it to "", so the next named line shows its name again)
  - add the entry, then `Render(entry)` (stage 3; for now `Debug.Log`).
- Public API: `IReadOnlyList<EventLogEntry> Entries`, `List<EventLogEntry> Snapshot()` (copy),
  `void Restore(IEnumerable<EventLogEntry> entries)` (clear + add + render each, then `_lastSpeaker = null`),
  `void Clear()` (entries + rows + `_lastSpeaker = null`).

Scene wiring (MCP, in `New Drag Drop System`):
- Add `EventLogPresenter` to `Panel_EventLog`'s parent or to `Panel_EventLog` itself — **it must stay active**, so put
  it on `Canvas_HUD/Panel_EventLog`'s parent-level object, or on a new active child `EventLog` under `Canvas_HUD`, and let
  it toggle the panel object. Do not put it on the panel that gets hidden (a disabled presenter still gets called by
  the runner, but OnEnable subscriptions would drop).
- Add it to the DialogueRunner's `dialoguePresenters` list (scene override on the prefab instance is fine).
- Assign `_options` to `Text Animator Dialogue System/Canvas/Options Presenter`.
- Compile-check. Quick Play check: talk to a customer, console shows one log per line and one per choice.

### Stage 3 — UI
1. Prefabs: with MCP `manage_prefabs`, save the two sample rows as
   `Assets/[04]Prefab/UI/PF_EventLog_With_Name.prefab` and `.../PF_EventLog_WithOut_Name.prefab`
   (create `[04]Prefab/UI` if missing), then delete the samples from `Content`.
2. New serialized fields on `EventLogPresenter`:
   ```csharp
   [SerializeField] GameObject _panel;              // Panel_EventLog
   [SerializeField] ScrollRect _scroll;             // Scroll View
   [SerializeField] RectTransform _content;         // Viewport/Content
   [SerializeField] GameObject _rowWithName;        // prefab
   [SerializeField] GameObject _rowWithoutName;     // prefab
   [SerializeField] Button _btnToggle;              // BTN_EventLog
   ```
3. `Render(entry)`: instantiate the right prefab under `_content`; find `TMP_Name` / `TMP_Text` by child name
   (`transform.Find`) and set text; set the name colour (section 8). Keep the instances in a list for `Clear()`.
   Do not add a row component unless child lookup proves fragile.
4. Toggle: `_btnToggle.onClick` → `_panel.SetActive(!_panel.activeSelf)`; when opening, scroll to the newest entry:
   `Canvas.ForceUpdateCanvases(); _scroll.verticalNormalizedPosition = 0f;`. Panel starts hidden (`Awake`).
5. Rows must not block dialogue clicks when the panel is closed (it is inactive, so they cannot).
- Compile-check. Play check: open/close, rows group correctly, long text wraps, scroll lands at the bottom.

### Stage 4 — Clear command + Save/Load
1. Clear at day start — static Yarn command on `EventLogPresenter` (same pattern as the static commands in
   `GameFlowCommands`):
   ```csharp
   [YarnCommand("eventlog_clear")]
   public static void Yarn_Clear() { if (!SceneLoaderBridge.IsSilentReplay) FindFirstObjectByType<EventLogPresenter>()?.Clear(); }
   ```
   Tell the user to put `<<eventlog_clear>>` at the top of each day's first node. Do not hook `flow_next_day`
   (no .yarn uses it today).
2. Save:
   - `SaveData` gets `public SaveEventLogData EventLogData = new SaveEventLogData();`
   - `SaveToFile`, next to `CollectNPCData()`:
     `_saveData.EventLogData.Entries = FindFirstObjectByType<EventLogPresenter>()?.Snapshot() ?? new List<EventLogEntry>();`
   - `OnSceneLoaded`, inside `if (_pendingLoad)` after `ApplyNPCData()`:
     `FindFirstObjectByType<EventLogPresenter>()?.Restore(_saveData.EventLogData?.Entries ?? new List<EventLogEntry>());`
   - Old save files have no `EventLogData`; Newtonsoft leaves the default → empty log. Verify that.
- Compile-check. Play check: talk, save, load the slot → same rows, no duplicates from the silent replay.

### Stage 5 — Name colours
Build the option the user chose in section 8 (A by default).

## 7. Edge cases
- Line with no speaker (narration): `Speaker = ""`, WithOut_Name row, name hidden.
- Same speaker across two conversations: first line of the new conversation still shows the name
  (`OnDialogueStartedAsync` resets `_lastSpeaker`).
- Player chooses, then the yarn also has a `Player:` line right after → both are "Player", second groups under the first.
- Option set cancelled by the runner → nothing logged (stage 1 step 3).
- Text Animator tags: `LocalizedLine.Text.Text` has Yarn markup stripped; check in Play that Text Animator tags like
  `<wave>` do not show as raw text in the log. If they do, strip `<...>` with a regex in `Add` and tell the user.

## 8. Name colour — two options
Both look up the colour by the speaker string exactly as written in Yarn (`Isla:` → `"Isla"`), case-sensitive.
Unknown name → `_defaultNameColor` on `EventLogPresenter`. Player → `_playerNameColor` on `EventLogPresenter`.
Apply as `TMP_Name.color = colour` in `Render`.

### Option A — on `SO_Customer` (recommended)
- `SO_Customer.cs`: add `[Tooltip("Colour of this character's name in the Event Log and name boxes.")] public Color NameColor = Color.white;`
- `SO_CustomerRoster.cs`: add
  ```csharp
  public bool TryGetNameColor(string customerName, out Color color)
  {
      color = default;
      if (Customers == null) return false;
      foreach (var entry in Customers)
          if (entry != null && entry.CustomerName == customerName) { color = entry.NameColor; return true; }
      return false;
  }
  ```
- `EventLogPresenter`: `[SerializeField] SO_CustomerRoster _roster;` (assign `Demo_CustomerRoster`, the same asset
  `TalkToCustomerManager._customerRoster` uses), `[SerializeField] Color _playerNameColor = Color.white, _defaultNameColor = Color.white;`
- Colour choice: `speaker == _playerName ? _playerNameColor : _roster != null && _roster.TryGetNameColor(speaker, out var c) ? c : _defaultNameColor`
- Set starting colours on `Isla.asset`, `Owen.asset`, `Walter.asset` only if the user gives values; otherwise leave white
  and tell the user where to edit.
- **How the user edits:** Project window → `[02]Script/NPC/CustomerData/<Name>.asset` → Name Color.
- Pros: one asset per character (GDD §19.1), no scene needed, readable in git, reusable by the dialogue name box later.
- Cons: a speaker that is not a customer (Narrator, Guard…) needs an asset or falls back to the default colour.
  Cole/Freya need assets when they get lines.

### Option B — table on `EventLogPresenter`
- `EventLogPresenter`: `[SerializedDictionary("Speaker", "Name Colour")] [SerializeField] SerializedDictionary<string, Color> _nameColors;`
  (`AYellowpaper.SerializedCollections` is already in the project, see `CharacterData.cs`), plus `_playerNameColor`, `_defaultNameColor`.
- Colour choice: `speaker == _playerName ? _playerNameColor : _nameColors.TryGetValue(speaker, out var c) ? c : _defaultNameColor`
- **How the user edits:** open `New Drag Drop System` → select the EventLog object → Name Colours table.
- Pros: every speaker in one place, works for non-customers right away.
- Cons: lives in the scene (scene diffs), only the Event Log can use it.

## 9. Verification checklist (report each as tested / not tested)
- [ ] Compiles, no console errors after every stage
- [ ] Customer line → row with name; next line same speaker → row without name
- [ ] Choose an option → player row with the option text
- [ ] Timeout → `*Silent*` (needs a `#timeout` option in a test yarn; say if not tested)
- [ ] Panel opens/closes from `BTN_EventLog`, scrolls to the newest row
- [ ] Name colours: customer colour, player colour, unknown speaker default
- [ ] `<<eventlog_clear>>` empties the log
- [ ] Save → load → same rows, no duplicates
- [ ] Old save without `EventLogData` loads with an empty log

## 10. Known risks
- `SceneLoaderBridge.IsSilentReplay` is only set back to false in `BubblePresenter.cs`, which the game scene does not use.
  If after loading a save the log records nothing, the flag is stuck — that is an existing save bug. Report it, do not
  silently work around it.
- Clicking inside the log panel may also advance dialogue (the duplicate-input issue in `CLAUDE.md` "สิ่งที่ต้องแก้").
  Report what you see; fixing input is out of scope.

## 11. Files touched
- New: `Assets/[06]Dialogue/EventLog/EventLogPresenter.cs`, `Assets/[02]Script/SaveLoad/SaveEventLogData.cs`,
  `Assets/[04]Prefab/UI/PF_EventLog_With_Name.prefab`, `Assets/[04]Prefab/UI/PF_EventLog_WithOut_Name.prefab`
- Edit: `OptionPresenterCocktailProject.cs`, `SaveLoadManager.cs` (SaveData + 2 calls),
  option A: `SO_Customer.cs`, `SO_CustomerRoster.cs`
- Scene: `New Drag Drop System.unity` (presenter object, runner list, references, sample rows removed)
- Afterwards: update `CLAUDE.md` "เอกสารที่ควรอ่าน" with this file, and tell the user about `<<eventlog_clear>>`.
