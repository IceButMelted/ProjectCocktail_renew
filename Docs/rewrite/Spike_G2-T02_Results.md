# Spike G2-T02 results (Localization, Yarn overrides, retype, Thai)

Date: 2026-11-07 · Unity 6000.3.19f1 · Yarn Spinner 3.2.5 (YarnTask = `Awaitable`, no UniTask) · Localization 1.5.14 · Addressables 2.9.1 · Text Animator v3 (`YS_USE_TEXT_ANIMATOR_3`) · branch `RefactorCode`.

**Method.** Throwaway code was written in `Assets/_Spikes/`. The `.cs` tests were in asmdef `Spikes.G2.PlayMode`. The `_Spikes` folder also held:
- a scratch `.yarnproject` with 22 lines (18 copied from `DEMO_3Days.yarn` with their `#line:` IDs, plus 4 test lines)
- a scratch Addressables settings asset
- a scratch `LocalizationSettings` with locales `en` and `th` (`th` falls back to `en`)
- two preloaded String Table Collections: `SpikeDialogue` (bound to the Yarn importer) and `SpikeData`
- `NotoSansThai-Regular.ttf`, copied from the system
- a copy of `LiberationSans SDF` with the Thai font as its fallback

7 PlayMode tests ran through Unity MCP `run_tests`. Item (e) ran through `execute_code` against the scratch project only.

Everything was then deleted:
- `_Spikes` and its `.meta`
- both `EditorBuildSettings` config objects (`com.unity.localization.settings`, `com.unity.addressableassets`)
- the generated `ProjectSettings/Packages/dev.yarnspinner/Spikes.G2.PlayMode-generated.ysls.json`

`git status` and an md5 check of `ProjectSettings/*.asset`, the Yarn `ysls` files and `Packages/*.json` match the pre-spike snapshot. The two files that were already modified (`.slnx`, `Assembly-CSharp-generated.ysls.json`) were not touched. Everything ran in the Editor (Mono, Addressables in "Use Asset Database" mode). No player build was made.

**Font:** the project has **no Thai-capable TMP font**. The only TMP font is LiberationSans SDF. The spike used the system's Noto Sans Thai (OFL) as a dynamic SDF fallback.

## Verdicts

| # | Item | Verdict | Evidence (short) |
|---|---|---|---|
| a | `UnityLocalisedLineProvider` EN/TH | **PASS** | The importer filled 22 EN base entries in the bound collection. `line:1` gives EN, then `SelectedLocale=th` gives the TH text. `CharacterName` stays `Walter` in TH. |
| U4 | Empty / absent TH entry | **PASS (falls back)** | Both `""` and a missing key in TH render the EN text (`raw='Walter: Empty TH entry test.'`). Nothing is logged, as the spec says. |
| U5 | Placeholder format in the String Table | **PASS** | The importer writes `{0}` (`line:sp_sub = "Walter: You ordered {0} tonight."`). |
| c / U2 | `[loc …/]` token inside a substitution | **PASS, with a caveat** | `[loc key="recipe.gin_fizz.desc"/]` passed as `Substitutions[0]` was parsed as markup: the marker was hit once per fetch, in the right locale (EN → `a … drink`, TH → `จิน… สดชื่น`). It also works end to end through real Yarn (`{Order_Cocktail_ByName_OutDescription("Gin Fizz")}` with the function overridden). **Caveat:** the text that `LocMarker` returns is **not re-parsed**, so `[wave]` inside a Data-table value showed up as literal `[wave]`. |
| — | `LocMarker` sync lookup | **PASS, with a rule** | `GetLocalizedStringAsync(...).IsDone` was true in 2/2 lookups (tables preloaded). Calling `WaitForCompletion()` (or sync `GetLocalizedString`) inside the marker **threw** `Reentering the Update method is not allowed`. The marker runs inside an Addressables callback. |
| d / U1 | Remove + Add over generated commands/functions; `params string[]`; `Awaitable` return | **PASS** | Adding `flow_open_bar` / `Order_Cocktail_ByName_OutDescription` without removing first logs an error, which proves the generated handlers are present. After `Remove…` + `Add…`, `<<flow_open_bar a "b c" 3>>` reached the gate as exactly `[a, b c, 3]`. A gate returning `YarnTask` and one returning `Awaitable` both blocked the next line (0 lines in 10 frames; the next line appeared 1 frame after release). The function override received `"Gin Fizz"`. The overrides survived a 2nd `StartDialogue` on the same runner. |
| e / U3 | Duplicate function declaration | **FAIL (fatal)** | Declaring `Order_Cocktail_ByName_OutDescription` a second time (same signature, or a different one) breaks the import: `CompilationFailed: An item with the same key has already been added`, 0 nodes. The **whole project** fails, not just that name. After the duplicate was removed, the import succeeded again. |
| b / U6 | Retype pattern (cancel → await → Prepare → Run) | **PASS (Character, Instant). Word not tested.** | See (b) below. |
| f / U7 | Thai in TMP + TA | **PARTIAL** | Fallback, glyphs, ZWSP and mark positioning are PASS, provided the `mark`/`mkmk` features are on. TA wave on marks and TA Word mode are **INCONCLUSIVE**. See (f) below. |
| g / U8 | 0 extra frames per line with preloaded tables | **FAIL (stock provider) / PASS with a custom provider** | See (g) below. |
| — | Sync presenter + async line fetch (affects replay) | **FAIL, new finding** | See (g) below. |
| h / U9 | Boot time | **PASS (Editor only)** | `BeforeSceneLoad` → `LocalizationSettings.InitializationOperation` done, including preload of the EN tables: **1020–1106 ms over 4 frames** (4 runs: 1020 / 1053 / 1053 / 1106 ms). Tables were tiny, so the Addressables init dominates. **A player build was not measured.** |
| U11 | Saliency strategy (code read) | **Risk** | The prefab `Text Animator Dialogue System` has `saliencyStrategy: 0` = `RandomBestLeastRecentlyViewed`. Any line group or node group becomes non-deterministic in replay. |

### (b) Retype detail
- The TA settings were copied from the real prefab (`startTypewriterMode = FromScriptOnly`). With TA's default settings (from `AddComponent`), the line **never completed**. With the prefab settings it completed in 228 frames. The likely cause, inferred and not traced, is that the default start mode lets `PrepareForContent` start typing before `RunTypewriter`. So the presenter depends on the prefab's `FromScriptOnly`.
- **Baseline:** the TH line (`line:10`) typed in **228 frames**.
- **Naive restart** (call `RunTypewriter(B)` while A is typing, without cancel/await): the old task completed at once, and **the new task never completed** (900-frame cap).
  - The spec predicted "B completes instantly". The real result is worse: **a hang**.
- **The spec pattern** (cancel the CTS → await the old task → `ContentWillDismiss` → `PrepareForContent` → `RunTypewriter` with a new CTS) worked in every case. The await of the cancelled call took **0 frames** (synchronous). Each final line typed in full with `allLettersShown=True`:
  - mid-type: 244 frames
  - after the line had finished typing: 240 frames
  - 5 quick switches: 245 frames
- **Instant mode** (pre-cancelled token, as `LinePresenterCocktailProject` does): completed in 0 frames, all shown.
- **Not tested:**
  - Word mode (needs a "delays by word" timings asset, and the presenter's `onTextShowed` workaround)
  - whether `LinePresented` fires once
  - whether the voice restarts (no presenter or voice was built)

- **U6, second half (code read): PASS.**
  - The prefab's single `eventHandlers` entry (fileID 4572785415830099715, on `Button Container`) is Yarn's `LinePresenterButtonHandler` (guid `fccdb370…`).
  - It only toggles the continue button's `interactable` state and visibility, so it is visual and safe to re-fire on retype.

### (f) Thai detail
- **Fallback works.**
  - Latin SDF + Thai fallback: `HasCharacters` is true, and every Thai glyph came from the Thai asset.
  - The dynamic Noto Thai asset loaded its OpenType tables: MarkToBase 585, MarkToMark 171, pair 40, ligatures 2.
- **Mark positioning depends on `TMP_Text.fontFeatures`.** The prefab's dialogue TMP has **only `kern`** (`m_ActiveFontFeatures: 6e72656b`). With `kern` only:
  - The tone mark on ที่ sits at y 184.2–192.8 and overlaps ี (184.2–190.6).
  - The tone mark on ป่ overlaps the ascender of ป.

  With `kern, mark, mkmk`:
  - ่ on ที่ is stacked above ี (184.2 above ี top 182.1).
  - ่ on ป่ moves left, clear of the ascender (x −725.2..−721.0 against ป's right edge −715.8).
  - ้ on ปั้น is stacked above ั.
  - The base line also shifts (y −8.5).

  **Every TMP text that can show Thai needs `mark` + `mkmk` enabled.**
- **ZWSP:** in a 220 px box, text with U+200B wraps only at the ZWSPs (`ยินดี·ต้อนรับ· | สู่บาร์·ของเรา· | คืนนี้·ครับ`). Without ZWSP, TMP breaks mid-word (`…สู่บ | าร์…`). A TMP created from code defaults to `NoWrap`; the prefab uses `Normal`.
- **TA wave on marks: INCONCLUSIVE.** The sampling read `textInfo.meshInfo` vertices, which returned 0 while TA animated. TA's per-character offsets are not visible there, so whether marks detach was not measured.
- **TA Word typewriter with ZWSP-only words: not tested.**

### (g) Frames per line, and the replay finding
- **Stock provider:** with preloaded tables, every line cost **exactly 1 frame** (20 lines → 20 frames, 0/20 synchronous, in both EN and TH). This happens even though `GetTableEntryAsync(...).IsDone` was true **20/20** straight away.
  - Cause: `YarnTask.WaitForAsyncOperation` subscribes `Completed` on a handle that is already done. Addressables defers that callback to the next `ResourceManager.Update` (`YarnTask.Awaitable.cs` L194-216).
- **Locale switch:** a fetch made straight after `SelectedLocale=th`, with no wait, took 6 frames (≈70–79 ms) while the TH table loaded. In test G the code awaited `InitializationOperation` after the switch. After that, `GetTableAsync` and the `line:0` entry were done at once. How long that wait took, and whether the op was pending at the switch, were not logged.
- **Custom provider:** a minimal `LineProviderBehaviour` (about 20 lines) took the result synchronously when `op.IsDone`. With a presenter that returns synchronously, 20 EN lines ran in the **same frame** (0 frames, 6–87 ms, 20/20 sync).
- **New finding: sync presenter + async line fetch.** A presenter that returns `YarnTask.CompletedTask` synchronously throws when the line fetch completed **asynchronously**: `InvalidOperationException: SignalContentComplete can only be called when a command is being dispatched` (`DialogueRunner.RunLocalisedLine` L860), and the dialogue **hangs**.
  - It reproduces with the stock provider (1 line shown, then stuck).
  - It also reproduces with the fast provider in TH. The TH table and `line:0` were `IsDone`, yet one later fetch went async. The probable cause is the first fallback lookups (`sp_empty` / `sp_absent`); this was not isolated further.
  - The spec's replay rule (A7: "Not Live: return immediately, no awaits") is therefore unsafe as written.

## Spec corrections (`API_G2_Dialogue.md`)
1. **A7 / A5 replay:**
   - **Verified:** a presenter that awaits one frame per line is safe with any fetch. Test D used an async presenter on the stock provider with no exception.
   - **Verified:** a presenter that returns synchronously after an async fetch throws and hangs (tests E and G).
   - **Proposal (not run):** return synchronously only when this line's fetch completed synchronously, using a provider flag such as `LastFetchWasSync`. That gives 0 frames per line in the common case.
2. **New `CocktailLineProvider` (Bar410.Dialogue):** replaces `UnityLocalisedLineProvider`. It is a small copy (about 30 lines) with an `op.IsDone` fast path and the same `ExpandSubstitutions` → `ParseString` order. Without it, U8 fails (1 frame per line → a full day of about 230 lines ≈ 230 frames). The shadow-line and asset-table support of the stock provider are not needed.
3. **A12 `Language.Set`:**
   - After changing `SelectedLocale`, await `InitializationOperation` or `GetTableAsync` for each preloaded table before the next line or retype. A fetch with no wait took ≈6 frames.
   - Which of the two is the reliable signal was not measured.
4. **A13 `LocMarker`:**
   - It must not call `WaitForCompletion` / sync `GetLocalizedString` (this re-enters Addressables). Use `GetLocalizedStringAsync(...)` and read `.Result` when `IsDone`; otherwise append the key and log a warning.
   - The returned text is not re-parsed, so `Data` / `UI` values must not contain Yarn markup. Validator rule: no `[` in `Data` values.
5. **A3:**
   - U3 is **fatal**: one duplicate `RegisterFunctionDeclaration` makes every `.yarnproject` fail to compile. The "declare only names with no old `[YarnFunction]`" rule is mandatory. Add an EditMode test that the declared names ∩ `TypeCache` `[YarnFunction]` names = ∅.
   - Note also that the Yarn source generator **does** run on non-Assembly-CSharp asmdefs that reference `YarnSpinner.Unity` (it emitted `ActionRegistration` and a `*-generated.ysls.json` even with no Yarn attributes). Expect one ysls file per `Bar410.*` assembly.
6. **A7 retype:**
   - The naive restart **hangs** the new line; it does not complete instantly.
   - The pattern relies on the prefab's TA `startTypewriterMode = FromScriptOnly`. Write that requirement into the presenter or prefab notes.
7. **A12 rule 5 (Thai font):** fallback alone is not enough. Every TMP component that can show Thai needs Font Features `kern, mark, mkmk`. The current prefab has `kern` only. A Thai TMP font asset must be added to the project; none exists yet.
8. **U11:** set the runner's saliency strategy to a deterministic one (`First` or `Best`), or ban line and node groups, before relying on replay.
9. **Paths:** the spec names `Assets/Bar410/Spikes/G2` and `Docs/Bar410_G2_Spike.md`. This spike used `Assets/_Spikes` (deleted) and this file.

## Owner eyeball checklist (Thai)
Do this once a Thai TMP font is in the project and set as a fallback, in the real dialogue box at the real font size:
1. Tone and upper-vowel stacking with `mark`+`mkmk` on: ที่, ปั้น, น้ำ, กุ้ง, ปู่, ป่, ฟ้า, ฝั่ง. No overlap, and no marks clipped by the line above.
2. Then the same with `kern` only, to confirm the difference is visible. If it isn't, the measurement here was misleading.
3. Line breaks only at the ZWSPs the translator placed. No orphan marks at the start of a line.
4. TA `[wave]`, `[wiggle]` and `[dangle]` over Thai: tone marks and vowels must move with their base consonant, not float off it.
5. Typewriter in Character mode: a mark appears together with its base, not one beat later.
6. Word mode with Thai (no spaces, ZWSP only): does it reveal per ZWSP chunk, or the whole line at once? The presenter's Word-mode workaround still ends the line.
7. Line height and descenders (ปู่, กุ้ง) inside the box. Does Thai need a size or line-spacing override?
8. EN↔TH switch mid-type, after typing, and 5× fast in the real presenter. Typing restarts, `[wave]` survives, and the voice is not restarted.

## Not tested
- Player build or IL2CPP boot time.
- TA Word mode.
- TA wave on marks (measurement invalid).
- The real `CocktailLinePresenter` (does not exist yet), `LinePresented`, voice.
- `wait` built-in override.
- Option presenter.
- U10, U12.
- A large String Table (only 22 or 23 entries). Boot and preload times with the full Dialogue/UI/Data tables are unknown.
