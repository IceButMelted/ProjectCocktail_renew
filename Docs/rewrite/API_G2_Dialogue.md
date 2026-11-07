> **Superseded in part by [`Contracts_Final.md`](Contracts_Final.md) (2026-11-07): where they disagree, Contracts_Final wins.**

# G2 API spec: Dialogue / Yarn / Voice / Localization (Bar410 rewrite, scope B)

Author: G2 architect, 2026-11-07. Basis: `Docs/Bar410_Rewrite_Options.md` §3.4/§3.5/§9/§11/§12/§16, `Bar410_LanguageSwitch_Options.md` (method C), `Bar410_O1_O4_Decisions.md`, `Bar410_CodeMap_MainScene.md`, the real code in `Assets/[06]Dialogue`, `GameFlowCommands.cs`, `CocktailSystemManager.Yarn*.cs`, `Customer/*`, `SceneLoaderBridge.cs`, and the package source of Yarn Spinner 3.2.5 + the Text Animator addon (TA v3 is active: `TextAnimatorYarnTypewriter_v3`, `YS_USE_TEXT_ANIMATOR_3`).
Nothing in the repo was changed.

Tags: **[V]** = verified in package or project source (file named). **[U]** = unverified (see §D).

---

## 0. Ground rules this spec follows

- **Side by side.** All new code goes under `Assets/Bar410/Dialogue/**` and `Assets/Bar410/Verbs/**` in namespace `Bar410.Dialogue` / `Bar410.Verbs`. Old files stay untouched and keep working in `New Drag Drop System` until R8.
- **Assemblies**
  - `Bar410.Dialogue` (runtime). Refs: `YarnSpinner.Unity`, `YarnSpinner.Addons.TextAnimatorIntegration`, `Febucci.TextAnimatorForUnity.Runtime`/`.TMP.Runtime`, `Unity.TextMeshPro`, `UnityEngine.UI`, `Unity.Localization`, `Unity.InputSystem`. It never refs `Assembly-CSharp`, `Bar410.Features.*` or `Bar410.Host`.
  - `Bar410.Dialogue.Editor`: validators and compile-time declarations.
  - `Bar410.Verbs` (runtime): every `[YarnVerb]` method and every compat shim. Refs: Dialogue, Flow, Features.* (their interfaces), Domain. Only Host refs Verbs. That keeps the verb attribute below everything and the verb bodies above the features.
  - `Bar410.Localization` (runtime, low layer; refs `Unity.Localization` only): `Language`, `OrderDescriptionProvider` and the key conventions. Features (G3: post-it, book, chapter title, popups) and Dialogue both reference it. It contains nothing Yarn-related (`LocMarker` stays in Dialogue).
- **No statics for game state.** Statics are allowed only for `Language` (it wraps the global `LocalizationSettings`), the verb registry (wraps the global `Yarn.Unity.Actions`), and pure helpers.
- **The Director is the only mode authority.** Presenters, verbs and features never read `SceneLoaderBridge` or `DialogueControls` statics again. Features that must know "skip/replay → do it instantly" get a `Func<bool> suppressEffects` from GameRoot (`() => director.Mode != DialogueMode.Live`). That avoids an interface with one implementation crossing an assembly boundary.

---

## A. Public types

Format per type: **Responsibility** / **Signature** / **Called by** / **Assembly** / **Replaces** / **Edge cases** / **Test**.

### A1. `ReplayPolicy` (enum)
- **Responsibility:** declares what a verb does during silent replay.
- **Signature**
  ```csharp
  public enum ReplayPolicy { Run, Skip, Journal }
  ```
  - `Run`: runs during replay too. Only for verbs that are pure over Yarn state.
  - `Skip`: no-op during replay. Visual, audio, camera and flow. Real state comes back from `ISnapshotSource` when replay ends.
  - `Journal`: live, record the Yarn-variable diff (and the return value, for functions); replay, re-apply the record without calling.
- **Called by:** `YarnVerbAttribute`. **Assembly:** Bar410.Dialogue.
- **Replaces:** the scattered `if (SceneLoaderBridge.IsSilentReplay)` checks in 7+ files (`GameFlowCommands.CommandTarget`, `EventLogPresenter`, `WaitForTask`, `WaitScene`, `TalkToCustomerManager.FadeSeconds/SoundsOn`, the bubble presenters).
- **Edge cases:** functions may only be `Run` or `Journal`; `Skip` on a function is rejected at install (it must return a value).
- **Test:** none on its own (covered by A2/A3).

### A2. `YarnVerbAttribute`
- **Responsibility:** marks a static method as a Yarn command or function, with a required policy.
- **Signature**
  ```csharp
  [AttributeUsage(AttributeTargets.Method)]
  public sealed class YarnVerbAttribute : Attribute {
      public YarnVerbAttribute(string name, ReplayPolicy replay);   // positional = missing policy is a compile error
      public string Name { get; }
      public ReplayPolicy Replay { get; }
      public bool IsFunction { get; set; }       // false = command
      public string JournalFrom { get; set; }    // optional: diff baseline = end of the last call of this verb (shim pair)
      public string Usage { get; set; }          // one line, shown by validator/logs
  }
  ```
- **Method rules** (enforced at install + EditMode test):
  - Static only.
  - Yarn-visible params: `string`, `int`, `float`, `bool`, enum. Optional params keep their defaults.
  - Injected params come after them: any other reference type, resolved from GameRoot's resolver, plus `CancellationToken` (the dialogue lifetime).
  - Commands may return `void`, `Awaitable`, `YarnTask` or `IEnumerator`.
  - Functions return `string`, `float`, `int` or `bool`, with ≤ 2 Yarn params.
- **Called by:** verb authors (G1, G3, G2). **Assembly:** Bar410.Dialogue.
- **Replaces:** `[YarnCommand]` / `[YarnFunction]` in new code. An EditMode test fails if any `Bar410.*` assembly contains a raw `YarnCommandAttribute` or `YarnFunctionAttribute`. Yarn's source generator would register those behind the gate's back.
- **Edge cases**
  - Two verbs with the same name: install error that names both methods.
  - The name contains a space: install error (Yarn refuses it anyway [V] `Actions.AddCommandHandler`).
- **Test:** EditMode reflection test `AllVerbsWellFormed` (shape, policy/function combination, no duplicates, no raw Yarn attributes).

### A3. `YarnVerbRegistry` (static)
- **Responsibility:** finds every verb, wraps it in the replay/logging gate, and installs it on one `DialogueRunner`. It also declares new functions to the Yarn compiler.
- **Signature**
  ```csharp
  public static class YarnVerbRegistry {
      public static IReadOnlyList<VerbInfo> All { get; }                 // scanned once, lazily (Bar410.* assemblies)
      public static void Install(DialogueRunner runner, IVerbHost host, Func<Type, object> resolve);
      public static bool TryGet(string name, out VerbInfo info);
  }
  public sealed class VerbInfo {           // read-only metadata, used by the validator and SkipScan
      public string Name; public ReplayPolicy Replay; public bool IsFunction; public string JournalFrom;
      public MethodInfo Method; public Type[] YarnParams; public int RequiredArgs; public string Usage;
  }
  public interface IVerbHost {            // implemented by DialogueDirector; the gate's only view of dialogue state
      DialogueMode Mode { get; }
      CancellationToken Lifetime { get; }
      void RecordJournal(string verb, string returnValue, YarnVars changed);
      bool TryTakeJournal(string verb, out JournalEntry entry);          // false = mismatch → host aborts replay
      YarnVars CaptureVariables();
      void ApplyVariables(YarnVars diff);
  }
  ```
- **Install algorithm (contract)**
  1. Touch `runner.AddCommandHandler`. That lazily builds the dispatcher, which registers every old generated `[YarnCommand]` [V] `DialogueRunner.cs` L422-445. The dispatcher is created once, behind a null check, and `StartDialogue` / `SetProject` never rebuild it [V] (the only assignment is L444). So overrides installed in `Init` survive every `StartDialogue`.
  2. For each verb, call `runner.RemoveCommandHandler(name)` / `runner.RemoveFunction(name)` first [V] `DialogueRunner.ActionRegistration.cs` L22/L29. Then add the gate. Reason: old names are registered globally, and `AddCommandHandler` / `AddFunction` refuse duplicates with an error [V] `Actions.cs` L553-592.
  3. Override Yarn's built-in `wait` the same way, as a `Skip` verb. Otherwise `<<wait 1>>` really waits during replay.
- **Command gate**
  - A closure instance whose method is `YarnTask Invoke(params string[] args)`, registered through `AddCommandHandler(string, Delegate)`. Yarn reflects `.Method`/`.Target` [V] `Actions.cs` L52-58 and supports `params` arrays [V] L192-243, L276-310. Yarn dispatches on the runtime return type, so returning `YarnTask` is fine [V] L415-473.
  - Gate steps:
    1. Policy check (see the table below).
    2. Convert args. On a bad arg it logs the usage and returns completed. It never hangs Yarn.
    3. Inject services.
    4. `try` invoke and `await` the result once.
    5. `catch` → `Debug.LogException`, then complete.
- **Function gate**
  - `Run` functions: typed delegate on the static method. Only when there is no injection; otherwise a gate.
  - `Journal` functions, or any function with injection: generic `FuncGate<TR>` / `FuncGate<T1,TR>` / `FuncGate<T1,T2,TR>` over {string, float, int, bool}, built with `MakeGenericType`. Yarn needs a typed `Func` because the compiler type-checks functions.
  - Add an AOT-hint method that names every combination in case the build uses IL2CPP [U].
  - `CommandGate` and `FuncGate<…>` are `internal` (implementation detail of the registry).
- **`JournalFrom` baselines:** the registry collects every name that appears in some verb's `JournalFrom`. In Live / SkipToOrder, after any call to such a name, the gate snapshots the variables *whatever that verb's own policy* (`flow_prepare_drinks` is Skip and would otherwise record nothing). The later Journal verb diffs against that snapshot. Replay doesn't use the snapshot (it applies the recorded diff).
- **Policy behaviour by mode**

  | Mode | Run | Skip | Journal |
  |---|---|---|---|
  | Live / SkipToOrder | call | call | baseline = `JournalFrom` snapshot ?? vars-before → call → await → `RecordJournal(name, ret, diff)` |
  | Replay | call | return completed | `TryTakeJournal` → `ApplyVariables(e.Changed)` → return `e.ReturnValue` (converted to TR); on mismatch return default and let the host abort |

- **Compile-time declarations:** editor `[InitializeOnLoadMethod]` → `Actions.AddRegistrationMethod((reg, type) => { if (type == RegistrationType.Compilation) reg.RegisterFunctionDeclaration(...) })`, using the pattern of [V] `DefaultActions.cs` L23-31 and `YarnProjectImporter.cs` L467-478.
  - Declare only names that have **no** old `[YarnFunction]` (found via `TypeCache.GetMethodsWithAttribute<YarnFunctionAttribute>()`), so a name is never declared twice [U] (what a duplicate does).
  - At Runtime registration it does nothing; runtime install is per runner via `Install`.
- **Called by:** `DialogueDirector.Init`. **Assembly:** Bar410.Dialogue (+ `.Editor` for declarations).
- **Replaces:** Yarn's generated registration for our names, the 8 scene-scoped `Instance` / `Current` statics used by commands, and `CommandTarget()` in `GameFlowCommands`.
- **Edge cases**
  - A verb returns an `Awaitable` that throws → logged, then completes.
  - An injected service is missing → install-time error (fail fast), not a run-time NRE.
  - Yarn passes interpolated command text (`RunCommandInstruction.SubstitutionCount`) already expanded.
  - The scan is reflection over `Bar410.*` assemblies only, at most once per domain.
- **Test**
  - EditMode: gate arg conversion (string/int/float/bool/enum/optional/bad count).
  - EditMode: policy table with a fake `IVerbHost`.
  - PlayMode smoke `TEST_AllCommands.yarn` on `Main_V2`.
  - Spike T02 proves the Remove+Add override on a runner that also has the old generated commands.

### A4. `JournalEntry`, `YarnVars` (DTOs) and `DialogueJournal`
- **Responsibility:** a serializable record of what C# wrote into Yarn, and a pure diff/apply over Yarn variables.
- **Signature**
  ```csharp
  [Serializable] public sealed class YarnVars {               // JsonUtility-friendly (no Dictionary)
      public List<string> FloatKeys; public List<float> Floats;
      public List<string> StringKeys; public List<string> Strings;
      public List<string> BoolKeys; public List<bool> Bools;
      public static YarnVars From(VariableStorageBehaviour s);  // GetAllVariables [V] InMemoryVariableStorage L235
      public void ApplyTo(VariableStorageBehaviour s, bool clear); // SetAllVariables [V] L269
      public static YarnVars Diff(YarnVars before, YarnVars after);  // changed + added keys only
  }
  [Serializable] public struct JournalEntry { public int Seq; public string Verb; public string ReturnValue; public YarnVars Changed; }
  public sealed class DialogueJournal {
      public void Record(string verb, string ret, YarnVars changed);
      public bool TryTake(string verb, out JournalEntry e);    // in-order cursor; name mismatch → false
      public List<JournalEntry> Capture(); public void Load(List<JournalEntry> entries); public void Clear();
  }
  ```
- **Called by:** DialogueDirector (as `IVerbHost`); G4 serializes the DTOs. **Assembly:** Bar410.Dialogue.
- **Replaces:** the planned `YarnVariableSync` journal (Refactor Plan Phase 4 "ช่องโหว่ 3").
- **Edge cases**
  - Yarn internal visit variables (`$Yarn.Internal.*`) are part of `GetAllVariables` and must be diffed too, because `visited()` depends on them.
  - Deleted keys can't happen in Yarn storage, so they are ignored.
- **Test:** EditMode, pure (`Diff` / `ApplyTo` round trip on `InMemoryVariableStorage`).

### A5. `DialogueMode` (enum) and `DialogueDirector`
- **Responsibility:** the one owner of dialogue presentation state:
  - mode (Live / SkipToOrder / Replay)
  - the displayed `LocalizedLine` and options
  - `LineIndex`, journal and option choices
  - `IsLineBusy`, voice scopes
  - retype on locale change
  - the save point
  - presenter #1 in the runner list (it does bookkeeping and never awaits)
- **Signature**
  ```csharp
  public enum DialogueMode { Idle, Live, SkipToOrder, Replay }

  [DefaultExecutionOrder(-50)]
  public sealed class DialogueDirector : DialoguePresenterBase, IVerbHost {
      // wiring (GameRoot)
      public void Init(DialogueRunner runner, CocktailLinePresenter line, CocktailOptionPresenter options,
                       EventLogView log, IVoicePlayer voice, ISpeakerDirectory speakers,
                       Func<Type, object> resolve, SO_DialogueSettings settings);
      // running
      public Awaitable RunNodeAsync(string node, CancellationToken ct);            // completes on dialogue complete or ct
      public Awaitable<ReplayResult> ReplayToAsync(DialogueSavePoint p, Action beforeTargetLine, CancellationToken ct);
      public Awaitable StopAsync();
      // state
      public DialogueMode Mode { get; }
      public bool IsSkipping => Mode == DialogueMode.SkipToOrder;
      public bool IsReplaying => Mode == DialogueMode.Replay;
      public LocalizedLine CurrentLine { get; }                 // null when no line on screen
      public IReadOnlyList<DialogueOption> CurrentOptions { get; }
      public string CurrentLineId => CurrentLine?.TextID;
      public int LineIndex { get; }                             // lines presented since RunNodeAsync/Replay start
      public bool IsLineBusy { get; }                           // typing || voice of this line playing (capped)
      public bool AtSaveableLine { get; }                       // Live && CurrentLine != null && no options && !retyping
      public bool AutoPlay { get; set; }
      public event Action StateChanged;                         // mode/autoplay/skip availability
      // player intent
      public void RequestNextLine();
      public bool CanSkipToOrder();                             // SkipScan from CurrentLineId
      public void BeginSkipToOrder();                           // stops voice, ends line, picks first option if open
      public void StopSkip();
      // voice (called by the voice verb)
      public void QueueVoice(string speakerId, string soundId);
      public event Action<string> SpeakerVoiceStarted, SpeakerVoiceEnded;   // speakerId; CharacterView pulse
      // save (G4)
      public DialogueSavePoint CaptureSavePoint();              // only valid when AtSaveableLine
      // notifications
      public event Action<LocalizedLine> LinePresented;         // live, first presentation only (not retype, not replay)
      public event Action<DialogueOption, bool> OptionChosen;   // (option, timedOut), live only
      // presenter hooks (same assembly)
      internal void ReportTyping(bool typing);
      internal void ReportOptionChosen(DialogueOption o, bool timedOut);
  }
  public enum ReplayResult { Reached, ReachedLineIdMismatch, DialogueEnded, OptionQueueEmpty, JournalMismatch, Watchdog, Cancelled }
  ```
- **Behaviour contract**
  - `Init` asserts that `this` is first in `runner.DialoguePresenters`, sets `runner.LineProvider`-dependent marker processors (A13), and calls `YarnVerbRegistry.Install`.
  - `RunLineAsync(line)` is synchronous and returns `YarnTask.CompletedTask`:
    1. `LineIndex++`.
    2. If Replay and `LineIndex == target`: call `beforeTargetLine()` synchronously (G4 restores snapshots and the saved variables), set `Mode = Live`, then fall through. Runner calls presenters in list order, so the visual presenter sees Live [V] `DialogueRunner.cs` L793-836.
    3. **In every mode:** close the previous voice scope and take the pending voice queue. The queue is played below only when Live; in SkipToOrder / Replay it is discarded. That way voices queued over skipped lines never burst out on the first live line.
    4. Append to the event log in Live **and SkipToOrder** (parity: today skipped lines still reach the log). Never in Replay (G4 restores the captured log), never on retype.
    5. If not Live: return.
    6. If the locale changed since the last presented line, the line may have been fetched in the old locale. Re-fetch it once before the visual presenter starts, by setting a flag the presenter awaits (`WaitForFreshLineAsync`).
    7. Open a new scope and play the queue taken in step 3 (A10).
    8. Set `CurrentLine`, raise `LinePresented`.
    9. Register `token.NextContentToken` → close the scope.
  - `RunOptionsAsync`:
    - Replay: dequeue the saved `DialogueOptionID` and return that option. The other presenters get cancelled [V] L939-946. An empty queue returns `OptionQueueEmpty` and aborts.
    - Live: set `CurrentOptions`, open an option voice scope, return `null` immediately.
  - Skip-to-order: the visual presenter does not show lines. `[order]` markup or an order verb calls `StopSkip`. Voices are never played while skipping or replaying, because the voice verb only queues and the Director plays only in Live (one rule covers skip, read lines and replay).
  - Watchdog during replay: no new content for `settings.replayWatchdogSeconds` (5 s, realtime) → `Watchdog`. On any abort: Mode → Live, `runner.Stop()`, return the result. G4 decides the fallback (restart node with saved vars).
  - `OnDialogueCompleteAsync`: close scopes, drop pending voices with a warning, clear current, Mode → Idle, finish the `RunNodeAsync` awaitable.
- **Called by**
  - G1 `DayRunner`: `RunNodeAsync(node from SO_DaySchedule)` replaces `TalkToCustomerManager.StartStory` → `DialogueRunner.StartDialogue`.
  - G4: `CaptureSavePoint`, `ReplayToAsync`, `AtSaveableLine`.
  - G3: features read the mode through the GameRoot-supplied `Func<bool>`.
  - DialogueControls, presenters and verbs.
- **Assembly:** Bar410.Dialogue.
- **Replaces**
  - `SceneLoaderBridge` (all dialogue fields)
  - `DialogueControls` statics (`AutoPlay`, `IsSkipping`, `CurrentLineId`, `OnLineStarted`, `RequestNextLine`, `StopSkip`)
  - `LineTracker.cs` (dead stub)
  - the replay logic in `BubblePresenter` / `OptionPresenterType2`
  - `CocktailSystemManager.IsWaitingForTask` as a save gate (use `AtSaveableLine`)
- **Edge cases**
  - Locale change during replay or skip, or with nothing on screen: set the stale flag only (step 4 handles it).
  - Rapid EN→TH→EN: a version counter; only the latest fetch is applied.
  - `RequestNextLine` while retyping: allowed, it ends the line.
  - Pause: AutoPlay and voice caps use scaled `Time.deltaTime`, so they freeze. Input is ignored by `DialogueInput`.
  - Replay root (ponytail): root = the node given to `RunNodeAsync`, not the latest `<<jump>>`. Replay walks from story start, so no jump/detour detection is needed, and detours replay naturally.
    - `// ponytail: root = StartDialogue node; add jump-based roots (onNodeStart/onNodeComplete heuristic) if replay of a full day exceeds ~1 s (see U8).`
  - `LineIndex` counts lines only. `LineId` is a check: a mismatch → `ReachedLineIdMismatch` (warn, still stops).
- **Test**
  - PlayMode on `Main_V2` with `TEST_SaveLoad.yarn` (G4 owns the file; G2 adds lines for replay-only cases: detour twice, loop with a repeated line ID, options, journal function, `<<wait>>`).
  - EditMode is not practical (needs the runner).
  - Debug overlay row: Mode / LineIndex / journal count.

### A6. `DialogueSavePoint` (DTO)
- **Signature**
  ```csharp
  [Serializable] public sealed class DialogueSavePoint {
      public string RootNode; public int LineIndex; public string LineId;
      public List<int> OptionChoices; public List<JournalEntry> Journal;
      public YarnVars RootStartVariables;   // captured at RunNodeAsync start
      public YarnVars CurrentVariables;     // captured at save; G4 applies in beforeTargetLine
  }
  ```
- **Called by:** G4 (serialize into slot JSON). **Assembly:** Bar410.Dialogue.
- **Replaces:** `SceneLoaderBridge.ChapterNodeName/TargetLineId/SessionOptionChoices/ReplayOptionQueue/DialogueRootNode/IsInDetour` and `SaveMetaData.ReplayOptionChoices`.
- **Edge cases:** a script edited after the save → `LineIdMismatch` / `JournalMismatch`; G4 refuses or restarts the day (Version check, §13 of the options doc).
- **Test:** JSON round trip (EditMode).

### A7. `CocktailLinePresenter`
- **Responsibility:** shows one line:
  - name box from `ISpeakerDirectory`
  - fade and typewriter (Text Animator, player speed/display settings)
  - AutoPlay that waits for `IsLineBusy`
  - skip-to-order (hidden lines)
  - retype in place
- **Signature**
  ```csharp
  public sealed class CocktailLinePresenter : DialoguePresenterBase {
      public void Init(DialogueDirector director, ISpeakerDirectory speakers);
      public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token);
      public bool IsTyping { get; }
      internal void Retype(LocalizedLine fresh);        // Director only
  }
  ```
- **Behaviour contract (retype race, from [V] `TextAnimatorYarnTypewriter_v3.cs` L87-160)**
  - The typewriter's cancelled call finishes with `currentContentPresentation?.TrySetResult(); = null`. If a new `RunTypewriter` starts before the old call returns, the new line completes instantly. So the presenter owns `CancellationTokenSource _typing` linked to `token.HurryUpToken`.
  - Retype:
    1. Cancel `_typing`.
    2. `await` the old `RunTypewriter` task to full completion.
    3. `ContentWillDismiss()` → `PrepareForContent(fresh.TextWithoutCharacterName)` → `RunTypewriter` with a new CTS.
    4. Reset the AutoPlay timer.
  - The RunLineAsync body is a loop: `type → wait until NextContent | Retype`. A retype can arrive mid-type or while waiting. Its fade is skipped (the box is already visible).
  - The speaker ID is the `Name:` prefix (`line.CharacterName`), kept identical in every locale (validator rule L4). Display text = `speakers.DisplayName(id)` (localized), not the raw prefix. The sprite lookup is keyed by ID.
  - `ReportTyping(true/false)` goes around every `RunTypewriter`.
  - AutoPlay: count only while `!director.IsLineBusy`. Delay = `DialogueSettings.AutoPlayDelay(charCount)`.
  - Not Live (replay): return immediately, no awaits (keeps replay synchronous).
- **Called by:** DialogueRunner (presenter list); Director (Retype). **Assembly:** Bar410.Dialogue.
- **Replaces:** `LinePresenterCocktailProject.cs`. Keep the TA Word-mode workaround and the `LateUpdate` blocksRaycasts fix.
- **Edge cases**
  - Text Animator action handlers re-fire `OnPrepareForLine` / `OnLineDisplayBegin` on retype. The single `eventHandlers` entry in the prefab (fileID 4572785415830099715 on the Continue Button object, probably `LinePresenterButtonHandler`) is visual only [U: confirm]. Rule: action markup handlers must be visual and idempotent; nothing gameplay- or audio-related may live in markup.
  - A cancelled typewriter runs every remaining action markup with a cancelled token [V] L126-147. That's harmless for pause markers.
  - Retype after the line finished typing: still type from the start (locked decision 20).
- **Test:** PlayMode spike T02:
  - switch EN↔TH mid-type, after typing, and 5× quickly
  - assert that `[wave]` survives, typing restarts, `LinePresented` fired once, and the voice was not restarted
  - also check Word and Instant modes

### A8. `CocktailOptionPresenter`
- **Responsibility:** shows options:
  - the last line as context
  - the `#timeout` / `#SilentTimeout` auto-pick
  - unavailable options
  - `ChooseFirstAvailable` for skip
  - relabel in place on locale change
- **Signature**
  ```csharp
  public sealed class CocktailOptionPresenter : DialoguePresenterBase {
      public void Init(DialogueDirector director, ISpeakerDirectory speakers);
      public override YarnTask<DialogueOption> RunOptionsAsync(DialogueOption[] options, LineCancellationToken ct);
      public void ChooseFirstAvailable();
      internal void Relabel(IReadOnlyList<LocalizedLine> optionLines, LocalizedLine lastLine); // same order as shown
  }
  ```
- **Behaviour:**
  - Not Live → return `null` immediately (the Director returns the replay choice).
  - On pick → `director.ReportOptionChosen(o, timedOut)`, which feeds the event log and `OptionChoices`.
  - `runSelectedOptionAsLine` stays 0 (it is 0 in the prefab [V]).
- **Called by:** runner, Director, DialogueControls (skip). **Assembly:** Bar410.Dialogue.
- **Replaces:** `OptionPresenterCocktailProject.cs`. Its `OptionChosen` event moves to the Director.
- **Edge cases:** the timeout timer keeps running across a relabel; the layout rebuilds for longer Thai labels.
- **Test:** PlayMode: relabel during the timeout; choose during skip.

### A9. `DialogueControls` (UI binder), `SkipScan`, `DialogueInput`
- **Signatures**
  ```csharp
  public sealed class DialogueControls : MonoBehaviour {           // on Canvas_HUD
      public void Init(DialogueDirector director);                  // binds AutoPlay/Skip buttons + Panel_SkipWarning
  }
  public static class SkipScan {
      public static readonly string[] OrderVerbs = { "serve_order", "flow_prepare_drinks" };
      public static bool FindOrderAhead(Yarn.Program program, string node, string lineId, out bool hasOptions);
  }
  [RequireComponent(typeof(LineAdvancer))]
  public sealed class DialogueInput : MonoBehaviour, ILineAdvancerInput {   // on Line Advancer, Input Mode = External
      [SerializeField] InputActionReference hurryUp, next;                 // from G4's Dialogue action map
      public void Init(Func<bool> isPaused);
  }
  ```
- **Called by:** GameRoot (Init); the Director uses SkipScan. **Assembly:** Bar410.Dialogue.
- **Replaces:**
  - `DialogueControls.cs` (statics removed; `FindFirstObjectByType<OptionPresenter>` removed)
  - `SkipScan.cs` (the stop-command set replaces `OrderCommand` const; it reads `CommandText.Split(' ')[0]`, not `StartsWith`)
  - `PauseAwareKeyCodes.cs` (`KeyCode` + `PauseManager.IsPaused` → Input System actions + injected pause)
- **Edge cases**
  - Skip while options are open → `ChooseFirstAvailable`.
  - Skip button availability is refreshed on `StateChanged` and on every new line.
  - SkipScan starts at the first match of a repeated `lineId` (known limitation, unchanged).
- **Test:** PlayMode via `TEST_AllCommands`; a controller test needs the owner's hands (§14).

### A10. Voice: `IVoicePlayer`, `IVoicePlayback`, `LineVoiceScope`, verb `voice`
- **Responsibility:** short expression sounds (sigh, HUH!?), one set for all languages, bound to the line they precede.
- **Signature**
  ```csharp
  public interface IVoicePlayback { bool IsPlaying { get; } void Stop(); event Action Ended; }   // Ended also fires on Stop()
  public interface IVoicePlayer  { IVoicePlayback Play(string soundId); }                        // null = unknown id (warn)
  public sealed class LineVoiceScope {
      public void Add(string speakerId, IVoicePlayback p);
      public bool IsBusy { get; }                  // any playing && elapsed < cap
      public void Tick(float dt, float cap);       // scaled dt → freezes on pause
      public void StopAll();                       // fires Ended → SpeakerVoiceEnded
  }
  // Bar410.Verbs (DialogueVerbs.cs): the dialogue-owned verbs
  [YarnVerb("voice", ReplayPolicy.Skip, Usage = "<<voice Isla huh>>  (before the line it belongs to)")]
  static void Voice(string speaker, string soundId, DialogueDirector director) => director.QueueVoice(speaker, soundId);
  [YarnVerb("wait", ReplayPolicy.Skip)]            // overrides Yarn's built-in so replay doesn't really wait
  static Awaitable Wait(float seconds, CancellationToken ct);              // Awaitable.WaitForSecondsAsync (scaled → freezes on pause)
  [YarnVerb("wait_scene", ReplayPolicy.Skip)]      // old name kept; also instant while skipping
  static Awaitable WaitScene(float seconds, DialogueDirector director, CancellationToken ct);
  [YarnVerb("save_chapter_node", ReplayPolicy.Skip)]
  static void SaveChapterNode(string target = "");                          // shim: no-op + one warning (old save system)
  ```
- **Rules (locked → contract)**
  - The verb only queues; in Replay it is a no-op through its Skip policy. On every line or option set, in any mode, the Director takes the queue. It plays it only if that content is presented in Live mode and otherwise discards it. So nothing plays during replay or skip, never for a line that isn't shown, and nothing piles up for later.
  - The scope closes (StopAll) when that line ends: NextContent, skip start, dialogue complete.
  - Retype doesn't touch the scope, so the sound continues.
  - `IsLineBusy` includes `scope.IsBusy`. The cap is `settings.maxVoiceWaitSeconds` (default 6 s), which guards against looping or overlong clips.
  - Pending voices with no following line (node end) are dropped with a warning; the validator flags `voice` not followed by a line/option within the same node.
  - `SpeakerVoiceStarted(id)` fires on Play; `SpeakerVoiceEnded(id)` fires on the last `Ended` for that speaker.
- **Called by:** Yarn; Director; GameRoot wires `SpeakerVoiceStarted/Ended` → `CharacterView.SetSpeaking(bool)` (G3).
- **Assembly:** interfaces + scope in Bar410.Dialogue. Adapter `SoundManagerVoicePlayer : IVoicePlayer` in Bar410.Host. It needs `SoundManager` to expose a per-call source. Today `PlayVoice` is `PlayOneShot` on one shared source with no handle [V] `SoundManager.cs` L461-469. Request in §E.
- **Replaces:** nothing (new). `SoundManager.PlayVoice/StopVoice` stay for old code.
- **Edge cases:** the same speaker's two sounds overlap → pulse until both end; pause → the handle stays `IsPlaying` while the source is paused (adapter must report `time < length`, not `isPlaying`).
- **Test**
  - EditMode `LineVoiceScope` with a fake playback (cap, stop, ended counting).
  - PlayMode: voice + AutoPlay waits; Next stops it; skip plays nothing; retype continues.

### A11. `EventLogView` + `EventLogEntry`
- **Responsibility:** dialogue history of the day. It stores IDs, never translated text, and re-resolves on open or locale change.
- **Signature**
  ```csharp
  public enum EventLogKind { Line, Choice, SilentChoice }
  [Serializable] public struct EventLogEntry { public EventLogKind Kind; public string LineId; public string[] Substitutions; public string SpeakerId; }
  public sealed class EventLogView : MonoBehaviour {
      public void Init(ILineProvider provider, ISpeakerDirectory speakers);
      public IReadOnlyList<EventLogEntry> Entries { get; }
      public void Append(EventLogEntry e);        // Director only (live lines + choices)
      public void Clear();
      public List<EventLogEntry> Capture(); public void Restore(IEnumerable<EventLogEntry> entries);  // G4 snapshot
      public Awaitable RefreshTextsAsync();       // re-resolve every row via provider (new Yarn.Line(id, subs))
  }
  // Bar410.Verbs: [YarnVerb("eventlog_clear", ReplayPolicy.Skip)] static void Clear(EventLogView log)
  ```
- **Behaviour**
  - Append policy: Live and SkipToOrder (old parity), never Replay, never retype.
  - Text = `provider.GetLocalizedLineAsync(new Yarn.Line(id, subs))`. `Yarn.Line(string, string[])` is public per the XML doc [V] `YarnSpinner.xml` `M:Yarn.Line.#ctor`. Then `TextWithoutCharacterName.Text` with TA tags stripped (existing regex).
  - Name rows are computed at render (speaker changes), not stored. The name text comes from `speakers.DisplayName(id)`; the silent choice text from the `ui.eventlog.silent` String Table key.
  - Re-resolve when the panel opens (if the locale changed while it was closed) and on `Language.Changed` while it is open.
- **Called by:** Director, G4 (snapshot), the verb. **Assembly:** Bar410.Dialogue.
- **Replaces:** `EventLog/EventLogPresenter.cs`. It is no longer a presenter (the Director feeds it), and it no longer stores `Text` / `Speaker` strings. The old save format is dropped (no migration, §13).
- **Edge cases**
  - Rows from replay are not appended; G4 restores the captured list.
  - Hundreds of rows re-resolve as one `WhenAll`. The String Table is preloaded, so that's cheap [U8].
  - A line ID no longer in the table → row shows `…` and logs a warning.
- **Test:** PlayMode: lines + choices, then switch locale, then confirm the rows change; Capture/Restore round trip.

### A12. `Language` (static), localization usage rules
- **Signature**
  ```csharp
  public static class Language {
      public const string English = "en", Thai = "th";
      public static string Current { get; }          // LocalizationSettings.SelectedLocale code
      public static int Version { get; }              // ++ on every change (Director stale check)
      public static event Action Changed;             // after SelectedLocaleChanged
      public static Awaitable InitAsync();            // await LocalizationSettings.InitializationOperation; apply PlayerPrefs "lang" ?? "en"
      public static void Set(string code);            // persist PlayerPrefs "lang" (settings, never in save slots)
  }
  ```
- **Called by**
  - Pause › Settings › Language tab: extend `DialogueSettingStepper` with a Language row (the stepper is moved into Bar410.Dialogue).
  - The debug EN↔TH hotkey (G1 debug overlay).
  - Boot (G1 GameRoot awaits `InitAsync` before the first line).
- **Assembly:** Bar410.Localization (so Features can use it).
- **Replaces:** nothing. Note: the Yarn provider's `LocaleCode` set already maps to `SelectedLocale` [V] `UnityLocalisedLineProvider.Installed.cs` L74-85, so setting the locale is enough.
- **Usage rules** (go into `Docs/Bar410_Localization_HowItWorks.md`)
  1. Static UI text: Unity's `LocalizeStringEvent` component on the TMP object. No custom `LocalizedText` component (installed package covers it).
  2. Text from code or SO: a `LocalizedString` field, using `StringChanged` to update. SOs store keys or `LocalizedString`, never display strings:
     - `recipe.<id>.name/.desc`
     - `ingredient.<id>`
     - `character.<id>`
     - `chapter.<day>`
     - `event.<id>.title/.body`
     - `ui.<screen>.<item>`
  3. Tables: `Dialogue` (Yarn-generated, EN base; Yarn's importer writes base entries [V] `YarnProjectImporter.AddStringTableEntries`), `UI`, `Data`; Asset Table `Sprites` for images with text.
  4. Locale `th` has Fallback = `en`. The provider uses `FallbackBehavior.UseFallback` [V] L106, so missing TH shows EN. Warnings come from the editor validator, because the provider never warns [V].
  5. Thai font: add the Thai TMP font asset as a **fallback** of each Latin font asset, so there is no per-locale font switching code. Per-locale size or line-spacing changes are made only if the spike shows they are needed (then use the package's font localization, or one small SO).
  6. Preload the `Dialogue`, `UI` and `Data` tables (table "Preload" flag) so sync lookups (A13) and replay don't wait frames.
  7. Never cache a translated string in state that outlives a frame: event log, order, popup queue, save metadata (chapter key only).
- **Edge cases:** `Set` before init → queued until `InitAsync` completes; an unknown code → warn and keep the current locale.
- **Test:** PlayMode boot time (< 30 s, target < 10 s; §11.2 check 5); switch in every screen with the debug key.

### A13. `LocMarker` (replacement marker processor) + `OrderDescriptionProvider`
- **Problem [V]:**
  - `{Order_Cocktail_ByName_OutDescription("Gin Fizz")}` puts the EN string into `Yarn.Line.Substitutions` (used in `PLAYTEST_Demo.yarn` L131/163/194).
  - So a retype, the event log and translation would all stay in English.
  - The provider expands substitutions **before** parsing markup (`ParseString(ExpandSubstitutions(text, subs))`, [V] `UnityLocalisedLineProvider.Installed.cs` L136). So markup returned by a function is parsed in the current locale on every fetch.
- **Signature**
  ```csharp
  public sealed class LocMarker : IAttributeMarkerProcessor {        // registered as "loc" on runner.LineProvider by the Director
      public const string Table = "Data";
      public static string Token(string key) => $"[loc key=\"{key}\"/]";
      public ReplacementMarkerResult ProcessReplacementMarker(MarkupAttribute m, StringBuilder child, List<MarkupAttribute> attrs, string localeCode);
      // appends LocalizationSettings.StringDatabase.GetLocalizedString(Table, key, locale(localeCode)); missing → key + warning
  }
  public static class OrderDescriptionProvider {                       // the one place recipe text keys are built
      public static string NameToken(string recipeKey);        // for Yarn functions → "[loc key=\"recipe.<k>.name\"/]"
      public static string DescriptionToken(string recipeKey);
      public static LocalizedString Name(string recipeKey);    // for UI (post-it, book)
      public static LocalizedString Description(string recipeKey);
  }
  ```
- **Called by:**
  - G3's order verbs (`Order_Cocktail_*`, `order_name`, `order_flavor`) return the tokens.
  - G3's `Post_It_Order` / `BookUI` bind the `LocalizedString`.
- **Assembly:** `LocMarker` → Bar410.Dialogue (Yarn interface). `OrderDescriptionProvider` → Bar410.Localization; it builds the token string itself (same format as `LocMarker.Token`, shared const) so it doesn't need Yarn.
- **No order:** functions return `""`, not a token with an empty key. PLAYTEST L286-289 expects an empty `order_name()`.
- **Replaces:** `Order.TargetName/TargetDescription` as display text. G3 keeps them as data. It also replaces the "detailed order text from EN-only SO" path (§11.3).
- **Edge cases**
  - `<<set $order_note = Order_Cocktail_OutName(4)>>` (DEMO L599 etc.) stores the token. That's fine while it's only displayed.
  - Comparing these values (`== "Gin Fizz"`) breaks. Validator rule Y6.
  - Sync lookup needs the preloaded table (rule A12.6).
  - Yarn must not escape markup inside substitutions [U2].
- **Test:** PlayMode spike T02 (a line with the token → EN, switch → TH, retype shows TH); event log row shows TH.

### A14. `ISpeakerDirectory` + `SpeakerInfo`
- **Signature**
  ```csharp
  public readonly struct SpeakerInfo { public readonly LocalizedString DisplayName; public readonly Color NameColor; public readonly Sprite Portrait; }
  public interface ISpeakerDirectory { bool TryGet(string speakerId, out SpeakerInfo info); }   // "Player" included
  ```
- **Called by:** line presenter, option presenter, event log. **Implemented by:** an adapter in **Bar410.Host only** (`RosterSpeakers`, over G3's `SO_CustomerRoster` data), because Features can't reference Dialogue. G3 supplies the data. **Assembly:** Bar410.Dialogue.
- **Replaces:** `EventLogPresenter._roster.TryGetNameColor`, `LinePresenterCocktailProject.characterSprites`, and the raw `line.CharacterName` shown in the name box.
- **Edge cases:** an unknown ID → show the raw ID with the default colour and log one warning per ID.
- **Test:** covered by the presenter PlayMode tests.

### A15. `SeenLines` (small; build only if the owner confirms a "skip read lines" mode)
- **Signature**
  ```csharp
  public sealed class SeenLines { bool Contains(string id); bool Add(string id); string ToJson(); static SeenLines FromJson(string json); }
  ```
- **Behaviour:** the Director adds the line ID on the first live presentation (not on retype, replay or skip). G4 persists it as profile data (not slot data).
- **Called by:** Director (Add); a future read-skip mode (Contains); G4 (JSON). **Assembly:** Bar410.Dialogue. **Replaces:** nothing.
- **Edge cases:** grows by about the number of lines in the game (thousands of IDs, so a JSON array is fine); profile reset clears it.
- **Test:** EditMode JSON round trip.
- **Why optional:** the only skip today is skip-to-order, and voices are already suppressed for every non-presented line by the A10 rule. Nothing consumes `SeenLines` until a read-skip mode exists. See owner question Q1 in §D.

### A16. Editor tools: `YarnValidator`, `TranslationValidator`, CSV script
- **Signatures**
  ```csharp
  public static class YarnValidator        { [MenuItem("Bar410/Validate Yarn")] public static bool Run(); }        // Bar410.Dialogue.Editor
  public static class TranslationValidator { [MenuItem("Bar410/Validate Translations")] public static bool Run(); }
  ```
- **Script:** `Tools/loc/loc_sheet.py export|import` (stdlib `csv` only).
  - Export: input = the Unity Localization CSV export of `Dialogue`/`UI`/`Data` plus the Yarn "Export Strings as CSV" (id, text, file, node, comment). Output = one translator CSV with columns `key, context(node/file/speaker/vars), en, th`, **UTF-8 with BOM** (Excel shows Thai correctly).
  - Import: `key, th` → the Unity import CSV.
  - xlsx only if a translator refuses CSV. Then `openpyxl` goes in a venv (it's not installed [V]).
- **Yarn validator rules** (walks `YarnProject.Program` instructions as `SkipScan` does: `RunCommand.CommandText`, `CallFunction.FunctionName`, `RunLine.LineID` [V] `YarnSpinner.xml`)
  - Y1: every command or function name is a registered verb, a shim or a Yarn built-in. Otherwise it's an error. This also catches old names that would silently run old code in `Main_V2`.
  - Y2: the arg count is within the verb's `[RequiredArgs..YarnParams.Length]`.
  - Y3: `random`, `random_range`, `dice` in any node → error (replay determinism).
  - Y4: a node with `Order_Cocktail_*` / `order_by_type` and no `order_customer` → warning.
  - Y5: every `SO_DaySchedule` start node reaches `flow_open_bar` / `open_bar` (follow jumps/detours) → error otherwise.
  - Y6: source regex: order/`flow_*` functions inside `==` / `!=` → warning.
  - Y7: `flow_prepare_drinks` not followed by `wait_for_task` before the next line → warning (journal window).
  - Y8: `voice` not followed by a line or options in the same node → warning.
  - Y9: count implicit line IDs (`Yarn.Compiler.Compiler.Compile(...)`, `StringInfo.isImplicitTag`) → warning listing the files.
  - Y10: duplicate explicit line IDs across files → error.
  - Y11: the `Name:` prefix of each line is a known speaker ID.
- **Translation validator rules (per locale ≠ en)**
  - L1: the key is missing or empty → warning ("EN fallback").
  - L2: Yarn markup `[x]…[/x]` and TA tags open/close parity versus EN.
  - L3: substitution placeholders (`{0}`… in table text [U5]) parity.
  - L4: the `Name:` prefix is identical to EN.
  - L5: `[order]` presence parity.
  - L6: keys in TH not in EN (stale) → warning.
- **Called by:** the owner (menu), before each translation hand-off and each milestone; optionally a pre-build hook later.
- **Assembly:** Bar410.Dialogue.Editor (C#); `Tools/loc` (Python, outside Unity).
- **Replaces:** nothing. These are the §3.8 "Yarn validator" and the §11.2 import checks; the old checks lived only in docs.
- **Edge cases**
  - Program walk: a validator node that loops is guarded with a visited set (like `SkipScan`).
  - A CSV cell with newlines or quotes uses standard CSV quoting.
  - Keys are never edited by translators: import rejects unknown keys.
- **Test:** run on `DEMO_3Days` and `TEST_AllCommands`; then seed a broken copy and expect each rule to fire.

---

## B. Dependency diagram

```
                 Bar410.Host (GameRoot)  ── wires everything, owns Func<Type,object> resolver
                  │   adapters: SoundManagerVoicePlayer : IVoicePlayer, RosterSpeakers : ISpeakerDirectory
                  ▼
   Bar410.Verbs ──────────────► Bar410.Flow (G1 interfaces: IDayFlow, IBartending …)
   (all [YarnVerb] + shims)  ─► Bar410.Features.* (G3 interfaces)
                  │
                  ▼
   Bar410.Dialogue ─────────────────────────────────────────────────────────────────┐
     YarnVerbRegistry ◄── DialogueDirector (IVerbHost) ──► CocktailLinePresenter    │
          │                   │  │  │                    ──► CocktailOptionPresenter│
          │                   │  │  └► LineVoiceScope ─► IVoicePlayer (Host impl)   │
          │                   │  └──► EventLogView ──► ILineProvider                 │
          │                   └─────► Language / LocMarker ─► Unity.Localization     │
     DialogueControls, DialogueInput (InputActionReference from G4), SkipScan        │
          │                                                                          │
          ▼                                                                          ▼
   YarnSpinner.Unity (DialogueRunner, Actions, UnityLocalisedLineProvider)   TextAnimator addon / TMP

   Bar410.Dialogue.Editor ─► Bar410.Dialogue, YarnSpinner.Unity.Editor, YarnSpinner.Compiler
   Bar410.Save (G4) ─► Bar410.Dialogue (DTOs: DialogueSavePoint, EventLogEntry, SeenLines JSON)
   Features ─✗─► Dialogue (they get Func<bool> suppressEffects from Host instead)
   Features ───► Bar410.Localization (Language, OrderDescriptionProvider, keys) ◄─── Bar410.Dialogue
```

---

## C. Task breakdown (ordered; T02 and T04 gate the rest)

| id | task | files to create | inputs | definition of done | diff | model | why this model |
|---|---|---|---|---|---|---|---|
| G2-T01 | asmdefs + skeleton + settings copy | `Assets/Bar410/Dialogue/Bar410.Dialogue.asmdef`, `Editor/Bar410.Dialogue.Editor.asmdef`, `Assets/Bar410/Verbs/Bar410.Verbs.asmdef`, `Assets/Bar410/Localization/Bar410.Localization.asmdef`, `Dialogue/Settings/SO_DialogueSettings.cs` + `DialogueSettings.cs` (copied into namespace `Bar410.Dialogue`; add `maxVoiceWaitSeconds`, `replayWatchdogSeconds`), `DialogueSettingStepper.cs` copy | §0, old Settings files | compiles with 0 errors; old scene untouched; asmdef refs exactly as §0 | 1 | haiku | mechanical |
| G2-T02 | **Spike** (R1 check list §11.2 + §16) | `Assets/Bar410/Spikes/G2/*` (throwaway scene `Spike_G2`) | install `com.unity.localization` (owner approves), `DEMO_3Days` 20 lines, a Thai font | written result in `Docs/Bar410_G2_Spike.md` for U1–U9:<br>(a) `UnityLocalisedLineProvider` EN/TH<br>(b) `new Yarn.Line(id, subs)` re-fetch + TA retype with the T07 cancel/await pattern<br>(c) `[loc]` token in a substitution parsed<br>(d) Remove+Add command override on a runner that already holds the old generated commands; `params string[]` delegate; `Awaitable` return<br>(e) duplicate function declaration behaviour<br>(f) Thai: fallback font, U+200B line breaks, mark positioning, TA wave on marks, Word typewriter<br>(g) frames per line with the table preloaded<br>(h) boot time | 4 | opus | many APIs at once, judgement on fallbacks |
| G2-T03 | Journal + YarnVars | `Dialogue/Replay/YarnVars.cs`, `JournalEntry.cs`, `DialogueJournal.cs`, `DialogueSavePoint.cs`, `Tests/EditMode/JournalTests.cs` | A4, A6 | EditMode tests: diff/apply round trip; in-order take; mismatch false; JSON round trip | 2 | sonnet | plain C# with tests |
| G2-T04 | Verb layer | `Dialogue/Verbs/ReplayPolicy.cs`, `YarnVerbAttribute.cs`, `YarnVerbRegistry.cs`, `VerbInfo.cs`, `IVerbHost.cs`, `CommandGate.cs`, `FuncGate.cs`, `Editor/VerbDeclarations.cs`, `Tests/EditMode/VerbRegistryTests.cs` | A1–A3, T02(d)(e) | the `AllVerbsWellFormed` + gate tests pass; a sample verb of each kind runs in `Spike_G2`; an old name overridden without a Yarn error; a new function compiles in a `.yarn` | 5 | opus | reflection + Yarn internals + replay semantics + AOT |
| G2-T05 | Director core (Live + skip, no replay yet) | `Dialogue/DialogueDirector.cs`, `DialogueMode.cs` | A5, T04 | `RunNodeAsync` plays `PLAYTEST_Demo` day 1 in `Main_V2` with the T07/T08 presenters; `LinePresented` once per line; skip-to-order works; AutoPlay works | 4 | opus | async state owner, ordering with the runner |
| G2-T06 | Voice | `Dialogue/Voice/IVoicePlayer.cs`, `IVoicePlayback.cs`, `LineVoiceScope.cs`, `Verbs/DialogueVerbs.cs` (voice, eventlog_clear, wait, wait_scene), `Host/SoundManagerVoicePlayer.cs` (G1 reviews), `Tests/EditMode/LineVoiceScopeTests.cs` | A10, G3/G1 sound handle | the scope tests pass; PlayMode: AutoPlay waits; Next stops it; skip/replay silent; retype continues; speaker events fire | 3 | sonnet | clear contract; the subtle parts are in the scope tests |
| G2-T07 | Line presenter | `Dialogue/Presenters/CocktailLinePresenter.cs` | A7, old `LinePresenterCocktailProject.cs`, T02(b) | parity with the old presenter (fade, same-speaker, Word/Instant, speed); `Retype` passes the T02 switch matrix with no instant-complete bug | 4 | opus | TA cancellation race |
| G2-T08 | Option presenter | `Dialogue/Presenters/CocktailOptionPresenter.cs` | A8, old `OptionPresenterCocktailProject.cs` | parity incl. `#timeout`/`#SilentTimeout`; Relabel; returns null when not Live | 3 | sonnet | port + one new method |
| G2-T09 | Controls / input / SkipScan | `Dialogue/UI/DialogueControls.cs`, `Dialogue/DialogueInput.cs`, `Dialogue/SkipScan.cs` | A9, G4 action map | buttons work in `Main_V2`; no statics; keyboard + pad advance (owner checks the pad) | 2 | sonnet | port |
| G2-T10 | Event log | `Dialogue/EventLog/EventLogView.cs`, `EventLogEntry.cs` (reuse `PF_EventLog_*` row prefabs) | A11 | the row stream matches the old one on day 1; a locale switch re-resolves; Capture/Restore round trip | 3 | sonnet | async re-resolve, port |
| G2-T11 | Localization runtime | `Localization/Language.cs`, `Localization/OrderDescriptionProvider.cs`, `Localization/LocKeys.cs` (shared token format + key builders), `Dialogue/Localization/LocMarker.cs`; Director retype wiring (`OnLocaleChanged`, version counter, stale-line re-fetch, options + log refresh); Language row in the stepper | A12, A13, T02(a)(c) | switch during: typing, waiting, options, skip, minigame (UI-only), 5× rapid → always the latest locale, no double `LinePresented`, voice continues | 3 | sonnet | builds on T05/T07, contracts fixed |
| G2-T12 | Replay | Director `ReplayToAsync`, watchdog, option queue, `CaptureSavePoint`; additions to `TEST_SaveLoad.yarn` (with G4) | A5, A6, T03, T04, G4 snapshot hook | the G4 Phase 4 "เสร็จเมื่อ" cases pass: detour twice, loop with a duplicate line ID, journal function (`Order_Cocktail_OutName` random), `<<wait>>`, options; aborts return the right `ReplayResult` | 5 | opus | determinism, ordering with `beforeTargetLine` |
| G2-T13 | Dialogue-side shims + shim table | `Verbs/Shims/DialogueShims.cs` (eventlog_clear is the same name; `save_chapter_node` = Skip no-op + warning) and the shim table in `Docs/YarnCommands/README.md` | §A3 policy table below | `TEST_AllCommands.yarn` passes in `Main_V2` once G1/G3 shims land | 2 | haiku | small, table-driven |
| G2-T14 | Yarn validator | `Editor/YarnValidator.cs` | A16 Y1–Y11, `VerbInfo` | the menu runs; DEMO_3Days clean (or warnings listed); a seeded bad copy triggers each rule | 3 | sonnet | instruction walk + rules |
| G2-T15 | Line tags | (owner presses **Add Line Tags to Yarn Scripts**; the helper is `internal` [V] `YarnProjectUtility`) | commit the pending work first | implicit count = 0 (Y9); `VDO_Rec_Conversation.yarn` excluded from `Bar_410Demo.yarnproject` sources, or its generator strips `#line:`, so IDs don't duplicate DEMO (Y10); separate commit "Yarn: add line tags" | 1 | haiku | procedure |
| G2-T16 | Translation validator + CSV script | `Editor/TranslationValidator.cs`, `Tools/loc/loc_sheet.py`, `Tools/loc/README.md` | A16 L1–L6, a real export | round trip: export → edit TH in Excel → import → validator; Thai shows in Excel (BOM) | 2 | sonnet | format details matter (U5) |
| G2-T17 | SeenLines (only if Q1 = yes) | `Dialogue/SeenLines.cs` | A15 | mark on first live presentation only; JSON round trip | 1 | haiku | trivial |
| G2-T18 | Thai mitigation (only if T02(f) fails) | `Dialogue/Localization/SO_LocaleTextRules.cs` (per-locale TA tags to strip) + one hook in `LocMarker`/presenter | T02 result | Thai renders with no broken marks in the real dialogue box | 2 | sonnet | conditional |
| G2-T19 | Docs | `Docs/Bar410_Dialogue_HowItWorks.md`, `Docs/Bar410_Localization_HowItWorks.md`, sections in `Docs/Bar410_Systems_Guide.md`, `Docs/YarnCommands/voice.md`, `serve_order.md`, update the CLAUDE.md component table | all | the CLAUDE.md rules are met (path, GameObject, owner-editable parts) | 2 | haiku | writing from the finished code |

**Order:** T01 → T02 → T03 ∥ T04 → T05 + T07 + T08 (together; T05 is unusable without them) → T09, T10, T06 → T11 → T13 → T12 (needs G4 snapshots) → T14, T15, T16 → T17/T18 (conditional) → T19.

**Policy table for existing names** (shims; the verb owner is in brackets)

| name | policy | note |
|---|---|---|
| `flow_open_bar` / `open_bar` [G1] | Skip | waits for the chapter title in live play |
| `flow_check_events` [G1] | Skip | saving in Event is forbidden anyway |
| `flow_next_day` [G1] | Journal | `$day + 1` |
| `flow_close_bar`, `flow_garnish_done`, `flow_serve_done`, `flow_remake_drink`, `flow_ingredient_added`, `flow_minigame`, `flow_another_ingredient`, `flow_drink_complete` [G1] | Skip | debug/test drivers |
| `flow_prepare_drinks` [G1] | Skip | starts the serve; journal window baseline |
| `wait_for_task` [G1] | Journal, `JournalFrom = "flow_prepare_drinks"` | the old pair = one `serve_order` |
| `serve_order` [G1] (new) | Journal | waits until served and scored |
| `order_by_type`, `order_customer` [G3] | Skip | the order is restored by the Cocktail snapshot |
| `Order_Cocktail_*` ×4, `order_name/flavor/type/satisfaction` [G3] (functions) | Journal | random and C#-state reads; return `LocMarker` tokens |
| `flow_phase/step/minigame_type/minigame_result`, `ingredient_unlocked` (functions) [G1/G3] | Journal | read C# state |
| `unlock_ingredient` [G3] | Skip | restored by the unlock snapshot |
| `Reset_Variable` [G3] | Journal | writes Yarn vars |
| `Can_End_Shift`, `DisplayThx`, `Fade_To_Summmary`, `Panel_FadeIn/Out`, `Switch_Camera`, `Cut_Camera`, `CharacterEnter/Exit/Expression/Look`, `<<Name …>>` ×8 [G3] | Skip | visual; restored by snapshots |
| `voice`, `eventlog_clear`, `wait`, `wait_scene` [G2] | Skip | |
| `save_chapter_node` [G2] | Skip | no-op + warning (old save) |

---

## C2. Review checklist for the Opus reviewer (per task)

- **T01:** no ref to Assembly-CSharp; namespaces don't collide with the old global `DialogueSettings`/`TextSpeed` (the old ones are global namespace → new ones must be `Bar410.Dialogue.*`); `autoReferenced` false where possible.
- **T02:** every U-item answered with evidence (log or screenshot); the fallback chosen for each failure; the package install recorded in `manifest.json` (owner approved).
- **T03:**
  - `Diff` includes `$Yarn.Internal.*`
  - floats compare exactly (not epsilon), since they are copied values
  - `Apply` with `clear: false` for diffs
- **T04**
  - The policy is positional.
  - Install calls Remove before Add for commands and functions.
  - The gate never throws into Yarn and never awaits an `Awaitable` twice.
  - Bad args → completed, not hang.
  - Injected params come only after the Yarn params.
  - Functions: `Skip` rejected.
  - `MakeGenericType` covered by an AOT hint.
  - Compile-time declaration skips names that have an old `[YarnFunction]`.
  - No allocation per call beyond args.
  - A test exists for raw `[YarnCommand]` in `Bar410.*`.
- **T05**
  - The Director is first in the presenter list (asserted).
  - `RunLineAsync`/`RunOptionsAsync` never await.
  - `LinePresented` fires exactly once per line (not on retype).
  - Mode transitions all go through one setter that raises `StateChanged`.
  - Scopes close on NextContent, skip and complete.
  - No `FindObjectOfType`, no static state.
- **T06:**
  - `Ended` fires on `Stop()`
  - the cap uses scaled time
  - the pending queue is cleared on dialogue complete
  - the CharacterView pulse is on **scale** (the idle bob owns position in `CharacterView.Update`)
  - the adapter's `IsPlaying` stays true while paused
- **T07**
  - Retype = cancel → **await old run to completion** → `ContentWillDismiss` → `PrepareForContent` → new run.
  - The AutoPlay timer resets.
  - The fade isn't replayed.
  - The name comes from `ISpeakerDirectory`.
  - The replay path returns synchronously.
  - The Word-mode `onTextShowed` workaround is kept.
  - The listener is removed in `finally`.
- **T08:** null in non-Live; timeout survives a relabel; `ReportOptionChosen` once; option IDs (not indices) go into `OptionChoices`.
- **T09:** no `Input.*`; actions enabled/disabled with the component; cooldown in unscaled time; `SkipScan` matches the verb name token, not a prefix.
- **T10:** no translated string stored; re-resolve is cancelled when the panel closes; row pooling or Destroy is balanced.
- **T11:** version counter drops stale fetches; stale-line re-fetch happens before typing; `Language.Set` persists; `LocMarker` uses the locale passed in `localeCode`, not `SelectedLocale`.
- **T12**
  - `beforeTargetLine` runs before the visual presenter for the target line.
  - The watchdog uses realtime.
  - On abort, Mode → Live and the runner is stopped.
  - The journal cursor resets per replay.
  - `RootStartVariables` are applied with `clear: true` before `StartDialogue`.
  - `CurrentVariables` are applied in `beforeTargetLine`.
  - The option queue uses `DialogueOptionID`.
- **T13–T16:** every old name in the table; validator rules each have a seeded failing case; the CSV has a BOM, `\n` inside quoted cells survives, and keys are never translated.
- **T19:** the CLAUDE.md "Component ที่เพิ่มเข้ามา" rows are added; Systems Guide sections written.

---

## D. Unverified assumptions, and what was verified

### Verified in source
- **Yarn Spinner 3.2.5**
  - `Actions.AddRegistrationMethod` and `RegistrationType.Compilation`/`Runtime` are public; registration is global (`DefaultActions.cs`, `Actions.cs` L545-787).
  - The runner's dispatcher is built once and never rebuilt (`DialogueRunner.cs` L439-447; the only assignment is L444); the runner exposes `RemoveCommandHandler` / `RemoveFunction` (`DialogueRunner.ActionRegistration.cs` L22/L29).
  - `AddCommandHandler` / `AddFunction` reject duplicates with `Debug.LogError` (L553-592); `RemoveCommandHandler` / `RemoveFunction` exist on `IActionRegistration`.
  - A command delegate is reflected via `.Method` / `.Target`; `params` arrays are supported; the return type is checked at runtime, including `Awaitable` under `UNITY_2023_1_OR_NEWER` (L52-58, L192-310, L415-473).
  - Implicit `Awaitable → YarnTask` conversions exist (`YarnTask.Awaitable.cs` L61-66).
  - The runner calls presenters in list order; options take the first non-null result and cancel the others; the fallthrough flag is in the prefab (`allowOptionFallthrough: 1`, `runSelectedOptionAsLine: 0`).
  - `LocalizedLine` has `TextID` and `Substitutions`; `Yarn.Line(string, string[])` ctor exists in the XML docs.
  - `ILineProvider.GetLocalizedLineAsync(Yarn.Line, ct)`.
  - `UnityLocalisedLineProvider`: `LocaleCode` ↔ `SelectedLocale`, `UseFallback`, substitution expanded before the markup parse, no fallback warning, `PrepareForLinesAsync` is a no-op.
  - The Yarn importer writes base-language entries into the Unity `StringTableCollection`.
  - `InMemoryVariableStorage.GetAllVariables/SetAllVariables`.
  - `Instruction` types incl. `CallFunctionInstruction.FunctionName`.
  - `YarnProjectUtility.AddLineTagsToFilesInYarnProject` is `internal` (use the button).
- **TA v3 typewriter:** the cancel path (`SkipTypewriter`, remaining action markups with a cancelled token, final `TrySetResult; = null`) and marker processors registered with `runner.LineProvider` in `Start`. `StyleMarkerProcessor`/`PaletteMarkerProcessor` hold a serialized `lineProvider` field.
- **Project facts**
  - The prefab uses `BuiltinLocalisedLineProvider`.
  - Only `DEMO_3Days.yarn` has `#line:` (229 lines); the other 9 `.yarn` files have 0.
  - `SoundManager.PlayVoice` = `PlayOneShot` on one source, no handle.
  - `CharacterView` exposes `Idle` (bob on position via Update).
  - `TableForge` folder contains no scripts.
  - `openpyxl` is not installed.

### Unverified (each has an owner task)
- **U1:** the `params string[]` gate delegate parses Yarn args exactly as expected, including quoted strings with spaces (T02d).
- **U2:** Yarn's `ExpandSubstitutions` inserts substitution text verbatim (no escaping), so `[loc …/]` inside a substitution parses as markup (T02c). Fallback: a decorator line provider that resolves `⟦loc:key⟧` tokens in `line.Substitutions` before delegating.
- **U3:** a duplicate `RegisterFunctionDeclaration` (old `[YarnFunction]` + ours with the same name) — error or harmless? The design avoids it, but confirm (T02e).
- **U4:** an **empty** TH entry falls back to EN, or renders empty (T02a; the validator L1 covers it either way).
- **U5:** placeholder format in the exported String Table text is `{0}` (not `{$x}`) (T16).
- **U6:** TA retype pattern (cancel → await → Prepare → Run) gives a clean restart in Character / Word / Instant modes. Also: the prefab's single `eventHandlers` entry is visual only (T02b, T07).
- **U7:** Thai in TMP:
  - Thai font as a fallback asset renders correctly
  - U+200B gives line-break opportunities
  - Mark-to-Base positioning works in Unity 6 TMP
  - TA per-character effects don't detach tone marks
  - TA "Word" typewriter with no spaces
  (T02f → T18)
- **U8:** with preloaded tables, one line costs 0 extra frames in silent replay (`YarnTask.WaitForAsyncOperation` completes synchronously). Otherwise full-day replay may take seconds and the ponytail root choice must change (T02g).
- **U9:** boot time with Localization + Addressables < 30 s (target < 10 s) (T02h).
- **U10:** `onNodeStart` order for jump vs detour (needed only if U8 forces jump-based roots).
- **U11:** Yarn saliency strategy (`once`, node groups) is deterministic with the current setting; if it's a random strategy, replay diverges. Check the runner's saliency field.
- **U12:** desktop builds use the Mono backend. If IL2CPP, `MakeGenericType` needs the AOT hint (T04).

### Owner questions (defaults used; nothing blocks)
- **Q1:** is a "skip already-read lines" mode wanted? Default: no, and T17 is skipped.
- **Q2:** should Thai translations keep the English `Name:` prefix as the speaker ID, with the display name localized via `character.<id>`? Default: yes (validator L4).
- **Q3:** the voice verb name `<<voice Isla huh>>`. Default: yes. A short form `<<Isla voice huh>>` is not added.
- **Q4:** do translators accept CSV with a BOM? Default: yes, so no xlsx script.

---

## E. Interfaces expected from other groups, and what G2 provides

### Expected from G1 (core flow / GameRoot / Stations)
- **`GameRoot`**
  - Constructs and calls `DialogueDirector.Init(...)`, `DialogueControls.Init`, `DialogueInput.Init(isPaused)`, `EventLogView.Init`.
  - Provides `Func<Type, object> resolve` for verb injection, covering every flow and feature interface that verbs take.
  - Awaits `Language.InitAsync()` before the first `RunNodeAsync`.
  - Wires `director.SpeakerVoiceStarted/Ended` → G3 `CharacterView.SetSpeaking`.
  - Passes `Func<bool> suppressEffects = () => director.Mode != DialogueMode.Live` to features.
- **`DayRunner`:** calls `await director.RunNodeAsync(node, ct)` per day. The Story-phase contract stays: Yarn calls `open_bar` itself.
- **Flow resume for replay (required; G4 calls it).** Every flow verb is Skip in replay, so `open_bar` / `serve_order` never run, and without this the DayRunner sits at Story start when replay reaches the target line. G1 must provide:
  - a flow snapshot: DayPart + bar step + current order context, as an `ISnapshotSource`
  - `ResumeAt(snapshot)`, which puts DayRunner / BartendingSession straight into that state without running the intervening steps
  G4 calls it inside `beforeTargetLine`, before the target line is presented. If the target line sits inside a waiting `serve_order` (save not allowed there: `AtSaveableLine` is false while a verb awaits), no resume of an in-progress drink is needed.
- **Flow verbs and shims** in `Bar410.Verbs` using `[YarnVerb]` with the policies of the table in §C, including `serve_order` (it awaits serve + score and calls `director.StopSkip()` when it starts).
- **Debug overlay:** EN↔TH hotkey → `Language.Set`; rows for `director.Mode`, `LineIndex` and journal count.

### Expected from G3 (features, hosts, WorldView)
- **`SoundManager` (or its owner):** a per-call voice API, e.g. `AudioSource PlayVoiceSource(string id)` or similar, so the Host adapter can implement `IVoicePlayback` (`IsPlaying` true while paused, `Stop`, `Ended`).
- **`CharacterView.SetSpeaking(bool)`:** a small DOTween **scale** pulse (no position, so it doesn't fight `_idleBob`/`_walkBob`).
- **`ISpeakerDirectory`** implementation over `SO_CustomerRoster`: `character.<id>` LocalizedString, name colour, portrait, plus "Player".
- **Order verbs** return `OrderDescriptionProvider` tokens; recipes get a stable `RecipeKey`; `Post_It_Order`/Book bind `LocalizedString`; SO text fields move to keys/`LocalizedString`. Features reference `Bar410.Localization`, never `Bar410.Dialogue`.
- **Roster data** for the Host `RosterSpeakers : ISpeakerDirectory` adapter.
- **Features use `suppressEffects`** instead of `SceneLoaderBridge.IsSilentReplay || DialogueControls.IsSkipping` (`TalkToCustomerManager.FadeSeconds/SoundsOn`, `WaitScene`).

### Expected from G4 (save + input)
- **Save:** serialize `DialogueSavePoint`, `List<EventLogEntry>`, and `SeenLines` JSON (profile). Call `ReplayToAsync(point, beforeTargetLine: RestoreAllSnapshots + apply CurrentVariables, ct)` and handle each `ReplayResult`. Gate saving and autosave on `director.AtSaveableLine` plus flow rules (not in Event).
- **Input:** a `Dialogue` action map with `HurryUp` and `Next` (keyboard + Xbox/PS bindings), and `isPaused` (Pause state).
- **Test:** `TEST_SaveLoad.yarn` (shared with G2-T12).

### G2 provides
- **To G1:** `YarnVerbAttribute`, `ReplayPolicy`, `YarnVerbRegistry`, `DialogueDirector.RunNodeAsync/StopAsync/StopSkip/Mode`.
- **To G3:** `OrderDescriptionProvider`, `LocMarker.Token`, `Language`, `ISpeakerDirectory`/`SpeakerInfo` contract, voice speaker events.
- **To G4:** `DialogueSavePoint`, `JournalEntry`/`YarnVars`, `ReplayToAsync`/`ReplayResult`, `AtSaveableLine`, `EventLogView.Capture/Restore`, `SeenLines` (if Q1).
- **To everyone:** `Bar410 > Validate Yarn`, `Bar410 > Validate Translations`, `Tools/loc/loc_sheet.py`.

---

## Summary (≤250 words)

G2 is built around one owner, `DialogueDirector`. It sits first in the presenter list and owns:
- the mode (Live / SkipToOrder / Replay)
- the displayed line and options, `LineIndex`, choices and journal
- the voice scopes and `IsLineBusy`
- retype on locale change

Presenters are thin. Static dialogue state (`SceneLoaderBridge`, `DialogueControls`) is removed.

Verbs use `[YarnVerb(name, ReplayPolicy)]`. The policy is positional, so leaving it out is a compile error. `YarnVerbRegistry` installs gates per runner. It removes and re-adds names, because old generated commands are global and Yarn rejects duplicates; the dispatcher is built once (verified). Commands use a `params string[]` gate with service injection; functions use typed `FuncGate`s.

Journal is generic: a Yarn-variable diff plus the return value. Replay roots at the story node (a ponytail choice, with an upgrade path).

Localization uses Unity Localization with `UnityLocalisedLineProvider`:
- Retype rebuilds `Yarn.Line(id, subs)` with a cancel → await → restart pattern, which avoids a Text Animator v3 race.
- Order text returns `[loc key/]` tokens, because substitutions are expanded before markup is parsed.
- `Language` and `OrderDescriptionProvider` sit in a low `Bar410.Localization` assembly that Features can reference.

Voice: `<<voice Isla huh>>` queues. The queue is drained on every line and played only on live lines.

G1 must provide a flow snapshot and `ResumeAt`, called inside replay's `beforeTargetLine`. T02 (spike) and T04 (verb layer) gate the rest. Owner questions Q1–Q4 have defaults.

File: `/tmp/claude-1000/-home-icebutmelted-WorkSpace-Git-ProjectCocktail-renew/5d41d2d1-898d-4666-8fdf-68fea549af8c/scratchpad/api_G2.md`
