# Spike G4-S0 results (Yarn detour order, variable round trip, File.Replace)

Date: 2026-11-07 · Unity 6000.3.19f1 (Linux Editor, Mono) · Yarn Spinner 3.2.5 · branch `RefactorCode`.
Method: scratch asmdef + EditMode tests under `Assets/_Spikes` (deleted afterwards, no existing file touched). Yarn source was compiled with `Yarn.Compiler.Compiler.Compile` and run on a bare `Yarn.Dialogue` with `InMemoryVariableStorage`. `DialogueRunner` only forwards `Dialogue.NodeStartHandler/NodeCompleteHandler` to `onNodeStart/onNodeComplete` (`DialogueRunner.cs:132-134`), so the order seen here is the order `DialogueRunner` will raise. 5 tests, all green.

| # | Item | Verdict |
|---|---|---|
| 1 | Existing jump/detour tracker (`SaveLoadManager.OnNodeStart/OnNodeComplete`) is valid | **FAIL** (spec assumption wrong; corrected rule below) |
| 2 | `GetAllVariables` -> `SetAllVariables` round trip keeps types and exact values | **PASS** (with one precision note) |
| 3 | `File.Replace` and the temp+move fallback on Linux | **PASS** (Linux Editor only) |

## 1. Node event order (FAIL for the old tracker)

Observed (`A` detours `B`, `B` detours `D`, then `A` jumps `C`; events logged as `start:`/`complete:`/`line:`):

```
start:A | line:a1 | start:B | line:b1 | start:D | line:d1 | complete:D |
start:B | line:b2 | complete:B | start:A | line:a2 | complete:A |
start:C | line:c1 | line:c2 | complete:C | dialogueComplete
```

Facts:
- Detour start: `onNodeStart(child)` fires while the parent has **not** completed (as the spec assumed).
- Detour return: `onNodeComplete(child)` fires, and then **`onNodeStart(parent)` fires again** (a "resume" re-fire). The parent gets no `onNodeComplete` until it really ends. The spec (open item 1) did not know about the re-fire.
- Jump (plain): `complete:A` then `start:C`, with the stack empty in between.
- `<<jump>>` inside a detour (`E` detours `F`, `F` jumps `G`): `start:E | e1 | start:F | f1 | complete:F | complete:E | start:G` (the whole detour stack unwinds first).
- `<<stop>>` inside a detour (`S` detours `T`): `complete:T | complete:S | dialogueComplete`.
- A direct start at a node (`SetNode("B")`) gives `start:B` with no preceding complete.

Why the old tracker fails: it treats every `start` while `_lastNodeCompleted == false` as a new detour. The re-fire `start:B` / `start:A` after a return increments `_detourDepth` again. In the run above the depth never returns to 0, `complete:A` leaves `_lastNodeCompleted` false, and `start:C` is treated as a detour. Result: `roots=[A]` only (C is never seen as a root), and line depths were `0,1,2,2,2,2,2`. So reusing the lines 141-186 logic unchanged would record the wrong replay root after any detour.

**Corrected rule (simulated on all three logs above, all correct).** Keep a stack of active nodes and the name of the parent resumed by the last `complete`:
- `complete(x)`: pop the top; `resumed = stack.Peek()` if the stack is not empty, else `null`.
- `start(n)`: if `resumed != null && n == resumed`, it is a **resume**: do nothing (no push, no depth change). Otherwise push `n`; if the stack was empty before the push, `n` is a new **root** (this covers jump, fresh start, and a jump out of a detour, because the unwind completes pop everything first).
- Any `line` event clears `resumed`. `dialogueComplete` clears the stack.
- Depth = `stack.Count - 1`. Simulated output for the first run: `ROOT(A) detour(B) detour(D) done(D) resume(B) done(B) resume(A) done(A) ROOT(C) END`. For `E` the roots were `E` then `G`; for `S` the root was `S`.
- Why it is unambiguous: after a return, the resume `start` always comes first. A new detour into the same node name can only come after that, with a line or command in between.
- Not covered: this ran on bare `Dialogue`, not through `DialogueRunner` command handlers (a `<<detour>>` is handled inside the VM, so the event order does not depend on the runner). Running it through a real `DialogueRunner` + `.yarnproject` was not done.

## 2. Variable round trip (PASS)

After running `A -> B -> D -> C` the storage held `{ $n:3, $f:0.1, $big:16777216, $neg:-2.5, $Yarn.Internal.Visiting.A:1, $r:<random> }` (floats), `{ $s:"héllo ..." }` (strings), `{ $b:true, $v:true }` (bools).
- `SetAllVariables(f,s,b,clear:true)` into a fresh storage with a stale variable: `GetAllVariables` returned equal dictionaries (`CollectionAssert.AreEquivalent` on all three), the stale variable was gone, bool/string/number stayed in their own dictionary (a bool never moved into the floats).
- Newtonsoft JSON round trip of the three dictionaries: equal, including the non-ASCII string with escapes and `0.1f`.
- `random_range` results are plain stored floats (they differ every run: 147592, 607357, 918265, 994507), so a saved game keeps them; replay would pick new ones, which is what the journal in the spec is for.
- `visited("A")` works and is stored as the ordinary float variable **`$Yarn.Internal.Visiting.A`** (and `$v` as bool). It travels with `GetAllVariables`, so visit counts are saved with no extra work. The restore-at-root-start snapshot must therefore also include these `$Yarn.Internal.*` keys (they are in the dictionaries; do not filter on a `$` prefix by accident).
- `clear:true` with empty dictionaries: `GetAllVariables` is empty, and `TryGetValue` falls back to the program defaults (`$n` -> 0, `$untouched` -> 7). `GetAllVariables` never returns variables that were never set (`$untouched` was absent), confirming open item 2. `clear:false` merges and keeps other keys.
- Precision note (spec correction): Yarn numbers are **float32**. `$big = 16777217` came back as `16777216`. Integers above 2^24 are not exact. Irrelevant for day/relationship values, but the save format must not promise more than float32 for Yarn numbers.
- Quirk: `TryGetValue<float>("$b")` on a bool variable returns `true` with `0`, and `TryGetValue<string>("$n")` returns `true` with `"0"`. Typed reads do not fail on a wrong type, so tests must compare through `GetAllVariables`, not `TryGetValue`.

## 3. File.Replace on Linux (PASS, scope-limited)

Run in the Linux Editor (Mono, CLR 4.0.30319, `Application.platform = LinuxEditor`), folder `Application.temporaryCachePath/g4s0`:
- `File.Replace(tmp, dst, null)` when `dst` exists: `dst` holds the new content, `tmp` is gone, **no backup file is created**.
- `File.Replace(tmp, dst, bak)` with `bak` absent: `bak` = old `dst`, `dst` = new. With `bak` already present: `bak` is overwritten by the old `dst` (no exception).
- `File.Replace` while another handle holds `dst` open for reading (FileShare.ReadWrite|Delete): succeeded on Linux.
- `File.Replace` when `dst` does **not** exist: throws `FileNotFoundException`, and `tmp` is left untouched. The first write must use `File.Move`.
- `File.Move(tmp, dst)` onto an existing file: throws `IOException` (so the netstandard2.1 no-overwrite rule is real).
- Fallback `File.Delete(dst); File.Move(tmp, dst)`: works (non-atomic window).
- A stale `.tmp` from a crash is simply overwritten by `File.WriteAllText`.

Spec corrections: S3 text "`File.Replace(tmp,dst,null)`, with no backup file left behind" is correct. The `.bak` wanted by the G4-S0 row needs the third argument (`File.Replace(tmp, dst, bak)`), and the existing-`.bak` case is fine. The write path must be `File.Exists(dst) ? File.Replace(...) : File.Move(...)`.

## Not verified
- Windows (NTFS) behavior of `File.Replace` (open-handle case, `.bak` permissions) and standalone Mono/IL2CPP player builds: only the Linux Editor was run. Atomicity under a crash or power loss was not tested (only the API semantics).
- The detour order through a real `DialogueRunner` + `.yarnproject` (see section 1); only the bare `Dialogue` core.
- Detours with `<<call>>`-style custom commands that start dialogue, and `<<detour>>` to a node with `when:` saliency/enum content groups.
