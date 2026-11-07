# Spike G1-0 results (S1–S6 + cheap UNVERIFIED items)

Date: 2026-11-07 · Unity 6000.3.19f1 · UTF 1.6.0 · Yarn Spinner 3.2.5 · branch `RefactorCode`.
Method: throwaway code in `Assets/_Spikes/` (asmdefs `Spikes.Core`, `Spikes.EditMode`, `Spikes.PlayMode` plus a scratch `.yarnproject`). The tests ran through Unity MCP `run_tests`, then the folder was deleted. Nothing else remains changed (see Side effects). Everything ran in the Editor (Mono); no player / IL2CPP build was tested for any spike.
Runs: EditMode 12 tests (10 pass; the 2 deliberate `EXPECTED_FAIL` tests were reported Failed, which is correct). PlayMode 7/7 pass.

The spike code was a minimal copy of spec §2.1/§2.2/§2.6:
- `FlowAwait.Wait` = `using (ct.Register(() => src.TrySetCanceled())) return await src.Awaitable;`
- `Forcible<T>`: a linked CTS. `Await` catches OCE `when (IsForced && !outer.IsCancellationRequested)`.
- A session loop over `MonoBehaviour` toy stations: Prepare (2.1 ⇄ 2.2 with a fake minigame host), Garnish and Serve (hosts that never finish on their own; they end only through a force).

## Verdicts

| # | Item | Verdict | Evidence |
|---|---|---|---|
| S1 | `TrySetResult`/`TrySetCanceled` continue inline; labels are set before a command returns | **PASS** | See S1 below. |
| S2 | A Yarn command returning `Awaitable` blocks the next line | **PASS** | `S2 log: before@13, wait-start@13, release@23, after@23`. The line after the command did not run for 10 frames, then ran in the same frame the source was resolved. Code: `Actions.cs:432` maps `Awaitable` → `YarnTask`. |
| S3 | `async void` exceptions are surfaced | **PASS** | See S3 below. |
| S4 | `destroyCancellationToken` cancels a pending `FlowAwait.Wait` on scene unload | **PASS** | `S4 scene unload: result=OperationCanceledException, unload started frame 39, cancelled at frame 40`. `Destroy(go)`: cancelled in the same frame (42→42). No error was logged (OCE is swallowed by `Run`). |
| S5 | UTF 1.6 `async Task` tests | **PASS** | See S5 below. |
| S6 | Source-only `Awaitable` in EditMode without a player loop | **PASS** | `SetResult` runs the continuation inline in an EditMode `[Test]`: the value was observed on the next statement. Frame-based waits also work in EditMode `[UnityTest]` (`Awaitable.NextFrameAsync` done, frames 8→9). |
| U2 | Awaiting a pooled `Awaitable` twice | **Observed (hazard confirmed)** | See U2 below. |
| U4 | Exception type on cancel | **PASS** | `System.OperationCanceledException` (not `TaskCanceledException`), both for cancel-while-pending and for a pre-cancelled token. |
| U7 | `AddComponent<T>()` stations in EditMode run `RunAsync` without Awake | **PASS** | All S1 EditMode tests built the stations with `new GameObject().AddComponent<T>()`. |
| U12 | Disposing a CTR inside its own callback (main thread) | **PASS (Editor/Mono only; IL2CPP not run)** | `cts.Cancel()` → callback → `TrySetCanceled` → the inline continuation leaves the `using` and disposes the registration inside its own callback. No deadlock and no exception (U4 test). |
| U8–U11 | `ServeDrink` reads, `ResetCocktail` → `Cleared`, `SO_DaySchedule` deps, `StartStory` queueing | **NOT RUN** | These are code-reading items, not runtime spikes. Left to the reviewer / G1 tasks. |

### S1 (PASS)
- **EditMode:** the PLAYTEST_Demo.yarn:88–101 sequence was asserted after every command. Trace:
  `step=AddIngredient, step=Minigame, minigame ForceAnother, step=AddIngredient, step=Minigame, minigame ForceComplete, returned ToyPrepare Continue, step=Garnish, returned ToyWaitStep Remake, step=AddIngredient, …, step=Serve, returned ToyWaitStep Continue, step=Idle`
- **PlayMode:** the same sequence passed.
- **Through real Yarn:** `<<spike_press X>>` followed by `<<spike_check Label>>` gave `AddIngredient:AddIngredient@11, Minigame:Minigame@11, AddIngredient@11, Garnish@11, AddIngredient@11, Serve@11, Idle@11`. Everything happened in frame 11.
- **Host completing normally:** `MinigameHost.SetResult(Completed)` gives `Step == "Garnish"` on the next statement.

The full chain is synchronous: CTS cancel → registration → `TrySetCanceled` → OCE through `async Awaitable<T>` → `Forcible` returns the forced value → the session loop → the next station sets its label.

### S3 (PASS)
- **Raw `async void`, throw before the first await:** logged as an Exception through `UnitySynchronizationContext.ExecuteTasks`. This was posted, not thrown at the caller: the caller returned first, and the log came in the same frame (31).
- **Raw `async void`, throw after an await:** logged in the next frame (32).
- **`FlowAwait.Run`:** logs non-OCE exceptions (`LogAssert.Expect` passed in EditMode for a synchronous throw, and in PlayMode for a throw after an await). It stays silent on OCE (`LogAssert.NoUnexpectedReceived` passed).

### S5 (PASS)
- `[Test] public async Task` really awaits: `Task.Delay(200)` was measured at ≥150 ms (UTF `TestTaskWrapper`).
- `Assert.Fail` after an await is reported as **Failed**.
- `[UnityTest] IEnumerator` with `yield return someAwaitable` also works, and also reports failures after the yield.
- Both styles are usable.

### U2 (hazard confirmed)
- Awaiting an already-completed `src.Awaitable` a 2nd time throws **`NullReferenceException`**, because the Awaitable has gone back to the pool.
- Two awaiters on one pending Awaitable: **the first awaiter is silently lost** (it did not resume when the source completed; later frames were not checked), and only the last gets the result (`pendB=9`, no `pendA`).
- This confirms the "never await twice / new source per wait" rule. The failure mode is a silent hang, so it is not only undefined behaviour.

## Not tested
- `[Test] async Task` awaiting a frame-based Awaitable (`NextFrameAsync`); only `Task.Delay` and completion sources were awaited in `async Task` tests.
- `async Task` tests in PlayMode.
- Any player build / IL2CPP.
- U8–U11 (code reading).

## Decisions
- **S1 → "label first" stands. F1 fallback is NOT needed.** G1-6 keeps `FlowCommands` button methods synchronous (`void`/`bool`).
- **Tests (G1-2 onward):** use `[Test] async Task` for completion-source tests. `[UnityTest]` is only needed when a test must step frames.

## Spec corrections for `API_G1_CoreFlow.md`
1. §(d) items 1, 3, 4, 5, 6, 7 and 12 are now verified (above). Item 2 is resolved: double-await gives an NRE when the Awaitable is already completed, and a **silently lost continuation** when it is pending. Worth one line in §2 "Shared conventions".
2. §2.1 test note: `LogAssert.NoUnexpectedReceived()` also fails on plain `Debug.Log` messages logged before it in the same test. Call it before any diagnostic `Debug.Log`, or don't log in those tests. (This was hit during the spike.)
3. §2.1 `Run`: raw `async void` does surface exceptions through `UnitySynchronizationContext`. The surfacing is deferred (posted), so a `LogAssert.Expect` for a raw `async void` needs a frame. `FlowAwait.Run` logs synchronously for a synchronous throw. Keep `Run` as specified.
4. S4: on `UnloadSceneAsync`, cancellation arrives on a later frame than the unload call (39 → 40), not inside the call. Anything that checks `ctx.Lifetime` in `finally` is fine with this. Do not assume that cancellation is synchronous with `UnloadSceneAsync`.

## Side effects (cleaned)
- The PlayMode run flipped `ProjectSettings/EditorSettings.asset` `m_EnterPlayModeOptions` 0 → 1. It was reset to `None` and the file now matches HEAD. Note: in memory Unity then reported `enterPlayModeOptionsEnabled=False`, while HEAD has `m_EnterPlayModeOptionsEnabled: 1`, so the next project-settings save may write a diff.
- The active Editor scene was switched from `New Drag Drop System` (not dirty, never saved) to an empty untitled scene before the PlayMode runs, so the test runner could not touch it. It was left that way.
