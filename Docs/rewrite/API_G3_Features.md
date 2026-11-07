> **Superseded in part by [`Contracts_Final.md`](Contracts_Final.md) (2026-11-07): where they disagree, Contracts_Final wins.**

# Bar410 rewrite: G3 API (Features + hosts)

Author: G3 architect, 2026-11-07. Scope B: keep the Domain and the managers, put interfaces in front of them, and add hosts for the additive UI scenes.
Nothing has been compiled. Every signature is a contract to implement in R3/R4.
I read: `Rewrite_Options` (all of it, §9/§15.6/§16), `O1_O4_Decisions`, `CodeMap_MainScene`, `Refactor_Plan_Remove_HSM` §6, and the code listed in §(d).

---

## 0. Verified facts that shape these contracts (read before the signatures)

| # | Fact (where checked) | Consequence |
|---|---|---|
| F1 | `[02]Script` has **no asmdef**, so everything is in Assembly-CSharp. `Plugins/` (DOTween modules, Oklab) compiles to **firstpass**. Only DOTween.dll, Odin dlls, Yarn, Febucci and the Unity packages can be referenced from an asmdef. | asmdef code **cannot reference any existing manager**. So the adapters that wrap managers must sit in Assembly-CSharp (§1). |
| F2 | `E_Cocktail` is a **partial class in 4 files** (`Minigame/E_Cocktail.Minigame.cs`, `Cocktail System/E_Cocktail.Dialogue.cs`, `Cocktail/E_Cocktail.Drink.cs`, `NPC/E_Cocktail.Character.cs`). | The partials cannot span assemblies, so all 4 files must move into the same asmdef (`Bar410.Domain`). |
| F3 | `MinigameOutcome` and `MinigamePhase` are declared in `Minigame/IMinigame.cs`. `DrinkOrderContext` (Session) uses `MinigameOutcome`. | Split the two enums into their own file in Domain. `IMinigame`/`IMinigameContext` stay in Assembly-CSharp. |
| F4 | The "Domain" is **not UnityEngine-free**. `S_Drink` is a ScriptableObject. `DrinkBuilder`, `DrinkColorBlender`, `OrderService`, `DrinkScoringService` use `UnityEngine` (Debug, Color, CreateInstance). | `Bar410.Domain` must reference UnityEngine. This contradicts Options §3.1 ("no UnityEngine"). Tests can still run in EditMode. **Closure check (grep of every file in the §1 move list):** they reference only UnityEngine, Odin and each other; mentions of `CocktailGlass`/`CharacterData` are comments only. `Oklab` (firstpass) is used only by `ShakingMinigame`, which stays. |
| F5 | `HubPhase` is declared inside `HubManager.cs`, `Facing` inside `CharacterView.cs`, `OrderMode` inside `DrinkOrderContext.cs`. | `HubPhase` and `Facing` move to their own files in Domain/Contracts. Serialized values are ints, so nothing changes on disk. |
| F6 | Minigame world vs UI ([GameLoop].prefab): `[MiniGameSystem]` holds `StirringRenderTexture/Minigame_Stirring_Cam` (Camera) plus `Minigame_Stirring` (Animator, SpriteRenderer, **`AnimationMinigameController`**), `ShakingAnimationRenderer/ShakingAnimationCam`, and `Canvas_MiniGame` (Screen Space Overlay) containing the Shaking/Mixing canvases. `BaseMiniGame.amc` points at the **world** object. `CameraManager._customerInsetPanel` points at **UI** (`Panel_CharacterView` inside `Canvas_MiniGame`). | The `amc` reference and the inset panel cross the scene split. `amc` has to be injected (small change to kept code). Panel show/hide moves out of `CameraManager`. |
| F7 | Garnish (scene): `[GarnishSystem]` holds `GarnishManager` plus `Canvas_Garnish/GlassView` (RawImage + `RenderTextureClickRelay`, whose `_camera` is `Garnish_Plate/GarnishStage/GarnishStageCam`). `GarnishManager` also owns world fields `_glassSpawnPoint` and `_pour` (`PourVisual`) and spawns `PlacedGlassInstance`. The grids find it with `GetComponentInParent<GarnishManager>` and listen to `Opened`, `PourStarted`, `GlassChanged` and `.Glass != null`. | `GarnishManager` is half UI, half world, so it must be split (task T9). |
| F8 | `Canvas_Garnish`, `Canvas_MiniGame`, `Canvas_HUD` and `Canvas_Cocktail` all use **render mode 0 (Overlay)** with no camera reference. | Moving the canvases to another scene breaks no camera link. Overlay canvases from different scenes sort globally by `sortingOrder`, so keep the current values. |
| F9 | `DrinkDeviation` compares `AddIce` and `PreparationMethod`. In Garnish, ice is written live to the shaker drink (`FlowWiring.OnIceToggled`). | Ice from Garnish must be on the shaker drink **before** `ServeDrink()` scores it. |
| F10 | `UpdateCocktailInShaker()` runs at Garnish entry. Without it the poured glass looks empty (transparent colours). | `ResolveDrink()` must run before a `GarnishRequest` is built. |
| F11 | `BaseMiniGame` reports its result only **after the outro** (`Idle` entry). `StartGame()` is ignored while the game is not Idle. | The host must wait for Idle before starting a run, or a late outro can swallow the next `RunAsync`. |
| F12 | `CameraManager.cs` contains `using Mono.Cecil.Cil;`. It is unused, and Mono.Cecil is editor-only. | Delete it. It is a probable player-build or asmdef break. |
| F13 | Old classes carry `[YarnCommand]` / `[YarnFunction]` attributes (`Order_Cocktail_*`, `order_customer`, `Switch_Camera`, `<<Isla …>>`, `unlock_ingredient`, …). They live in Assembly-CSharp, so they stay registered in Main_V2 too. | G2's compat shim must not re-declare the same names (§e, X2). |

---

## 1. Assembly placement (G3 proposal; G1 decides, see §e X1)

```
Bar410.Domain      (asmdef, refs UnityEngine, Sirenix)   MOVED in R2: E_Cocktail.* (4 files), S_Drink, DrinkIngredients, IDrinkInterfaces,
                                                         CompositeDrinkRepository, SO_CocktailList, SO_UnmatchedDrinkColor, Cocktail/Domain/*, Session/*,
                                                         MinigameOutcome+MinigamePhase (split file), SO_GlassOption, SO_GarnishItemOption, SO_GarnishRimOption,
                                                         GarnishCategory, IngredientId, SO_IngredientUnlockTable, SO_DaySchedule, HubPhase, Facing
Bar410.Contracts   (asmdef, refs Domain)                 NEW: every interface and DTO consumed by Flow / Dialogue / Save (listed in §2)
Bar410.WorldView   (asmdef, refs Contracts, UnityEngine.UI, Unity.Cinemachine) NEW: WorldViewHandle, WorldViewSource, WorldView (does not touch old code)
Bar410.UiScenes    (asmdef, refs Contracts, UnityEngine.UI, Unity.Cinemachine) NEW: UiSceneLoader, UiSceneEntry, UiSceneRegistry (static handle) (does not touch old code)
Assets/Bar410/Adapters/  (NO asmdef = Assembly-CSharp)   NEW: every adapter that touches an old manager, plus the UI-scene hosts
```
Rule: an asmdef type never names an old class. Adapters are the only new code allowed to name `CocktailSystemManager`, `GarnishManager`, `BaseMiniGame` and the like.
Consequence for G1: `GameRoot` (which `new`s the adapters) must be compiled in Assembly-CSharp, or it must receive the adapters from an Assembly-CSharp bootstrap. Doing it the other way needs an asmdef over all of `[02]Script` plus DOTween "Create ASMDEF" plus an asmdef for CocktailCrafter_V2. That is too much for R3.
New G3 asmdef code must **not** use DOTween module extensions (`DOFade` on CanvasGroup lives in firstpass). Use `Awaitable` loops or plain lerps instead.

---

## 2. Public types

Each entry gives: **Resp.** (responsibility), **Sig.** (signature), **Caller**, **Asm** (assembly), **Wraps** (old code it wraps or replaces), **Edge** (edge cases and failure modes) and **Test**.
Async methods return `Awaitable`/`Awaitable<T>` and take a `CancellationToken`. Rule for all of them: cancellation means `OperationCanceledException` thrown after cleanup.
Never store an `Awaitable` and await it twice (they are pooled). When several waiters need the same thing, they poll a flag with `Awaitable.NextFrameAsync(ct)`.

### 2.1 Order and scoring (Cocktail)

**`ScoreResult`** (DTO, Contracts)
- Resp.: holds the values of one scored order, which G2 journals for replay.
- Sig.: `[Serializable] public struct ScoreResult { public CharacterName customer; public Satisfaction result; public TypeOfCocktail servedType; public float payout; public float relationshipDelta; public string recipeName; public bool isValid; }`
- Edge: `relationshipDelta` is a **delta**, never a total. Relationship stays only in Yarn `$rel_*`, and nothing here mirrors it into an SO. `isValid` is false when served without an order.

**`IOrderDesk`** (Contracts)
- Resp.: places orders, scores **once** at serve, and announces the result.
- Sig.:
```csharp
public interface IOrderDesk {
    DrinkOrderContext Order { get; }                       // read-only by convention; mutate only via this interface
    string PlaceNamed(string recipeName, bool byFlavor);   // returns name/description text; "" = not found (order cleared, post-it "None")
    string PlaceByPreference(CharacterName customer, bool byFlavor); // reuses a standing unscored order for the same customer (bug B2)
    bool   PlaceByType(TypeOfCocktail type);
    void   SetCustomer(CharacterName customer);            // <<order_customer>>
    void   ClearOrder();                                   // Reset_Variable (raises Cleared)
    ScoreResult CommitServe();                             // THE scoring call; idempotent
    Awaitable<ScoreResult> WaitScoredAsync(CancellationToken ct); // returns at once if already scored
    void UseRecipeFilter(Func<S_Drink, bool> canMake);     // from IIngredientUnlocks.IsRecipeComplete
    void UsePreferences(ICustomerPreferences prefs);       // SO_CustomerRoster
    event Action<ScoreResult> Scored;                      // raised synchronously inside the first CommitServe
    event Action Cleared;
}
```
- Caller: G2 Yarn verbs (`Order_Cocktail_*`, `order_by_type`, `order_customer`, `wait_for_task` shim, `<<serve_order>>`). G1 `BartendingSession` calls `CommitServe()` only after `ServeAsync` returns `Served`. GameRoot wires `Scored` to G2's Yarn result writer.
- Wraps: `CocktailSystemManager` (+ the `YarnOrders` / `YarnTask` partials), `OrderService`, `DrinkScoringService`, `DrinkOrderContext`.
- Edge:
  - `CommitServe` with no order: warning, returns `isValid=false`, raises no event (matches old `ServeDrink`).
  - A second call returns the cached result and does **not** raise `Scored` again (no double relationship delta).
  - A remake never calls it, because the station is the only caller.
  - `PlaceNamed` reuses the current order when the same recipe name is still unscored.
  - A named order for a recipe that is not makeable is still placed, with a warning (old behaviour).
  - The post-it text `"Task\n* Make a " + text` is hard-coded, so localization (R7) has to change it.
  - The `RandomCocktailIfNoOrder()` test fallback is **not exposed**. G1 warns instead (code map §8.15).
- Test: EditMode on Domain for scoring, using `Validate Cocktail Data` recipes. PlayMode fake for idempotency: call `CommitServe` twice and assert `Scored` was raised once and the delta applied once.

**`IBarCounter`** (Contracts)
- Resp.: the step 2.1 bar: shaker, bottles, method panel, fill bars and the order note.
- Sig.:
```csharp
public interface IBarCounter {
    void BeginDrink();                               // Prepare entry: shaker.Clear, panels.ResetPermissions, Order.ClearResult
    void OpenIngredients(Func<string, bool> isUnlocked); // 2.1 entry: Refresh, SetInteractable(true), EnableInteractablePrepareDrinksPhase, ShowMethod, UpdateCocktailBars
    void CloseIngredients();                         // 2.1 exit: SetInteractable(false), HideMethod, ResetVisualBars
    void LockIngredients();                          // Talking / Event / shaker full: Disable()
    void RefreshUnlocked(Func<string, bool> isUnlocked); // Event: hide locked bottles
    bool HasRoom { get; }  bool IsEmpty { get; }
    Enum_MiniGameType RequiredMinigame { get; }
    event Action ShakerFull;                         // ShakerContents.OnFulled
    event Action ShakerCleared;                      // after the adapter's own bar-side reactions
    event Action<Method> MethodChosen;               // Shaking/Stirring; shaker method already set
    event Action ResetChosen;                        // shaker already reset, bars updated, method panel hidden
    S_Drink ResolveDrink();                          // UpdateCocktailInShaker(); returns the live runtime drink (F10)
    void SetIce(bool on);                            // F9; applied from GarnishResult
    void SetShakerGrabbable(bool on);                // InteractableToggle.Apply(shaker, on) after a Cancelled minigame
    void ResetDrink();                               // CocktailSystemManager.ResetCocktail (remake)
    void SetOrderNoteAvailable(bool on);             // Post_It_Order.SetAvailable
}
```
- Caller: G1 Prepare, Garnish and Serve stations, and the Talking/Event day parts.
- Wraps: `ShakerContents`, `ShakerPanelController`, `IngredientButtonGroup`, `MethodPanel`, `VisualizeCocktail`, `Post_It_Order`, plus the bar-side half of `FlowWiring` §2/§2.1/OnShaker*/OnMethod*.
- Edge:
  - Camera, Hub and look reactions (shaker full: camera to shaker, look fixed, book button on) are **not** here. The station does them.
  - `ResolveDrink()` returns a throw-away runtime SO. Do not keep it past `ResetDrink`/`BeginDrink`.
  - `ShakerContents` events are UnityEvents. The adapter subscribes in code and unsubscribes in `Dispose`.
- Test: PlayMode in Main_V2 with the R0 checklist items 3–6. Count the subscribers on dispose.

**`CocktailFeature`** (adapter, Assembly-CSharp; implements `IOrderDesk` and `IBarCounter`, `IDisposable`)
- Sig.: `public CocktailFeature(CocktailSystemManager manager, ShakerContents shaker, ShakerPanelController panels, IngredientButtonGroup bottles, MethodPanel method, VisualizeCocktail bars, Post_It_Order note)`. It is constructed by GameRoot.
- Change to kept code: in `CocktailSystemManager.YarnOrders.cs`, `PlaceNamedOrder` and `PlacePreferenceOrder` go from `private` to `internal`. Nothing else changes.

**Order / scoring flow (new)**
```
Yarn verb (G2) ─► IOrderDesk.Place*/SetCustomer ─► CocktailSystemManager.Orders ─► DrinkOrderContext (pure)
G1 BartendingSession: loop { Prepare → Garnish(Remake?→continue) → Serve(Remake?→continue) }  → desk.CommitServe()  ← single scoring site
   CommitServe ─► CocktailSystemManager.ServeDrink() [idempotent] ─► DrinkScoringService.Score ─► OrderScored ─► adapter ─► Scored(ScoreResult)
   GameRoot: desk.Scored += g2.ResultWriter.Write   ($satisfaction/$type_of_cocktail/$task_done + ApplyRelationshipDelta, Journal policy)
   <<serve_order>> / wait_for_task shim ─► await desk.WaitScoredAsync(ct)
```

### 2.2 WorldView (camera + RenderTexture + RawImage + click relay)

**`WorldViewHandle`** (plain class, WorldView asm)
- Resp.: a runtime reference to a main-scene render source that a UI scene can use. Runtime references may cross scenes; serialized ones may not.
- Caller: created by `WorldViewSource`, passed to the hosts by the T10 glue, used by `WorldView`. Test: in the T2 tests.
- Sig.:
```csharp
public sealed class WorldViewHandle {
    public WorldViewHandle(string id, Camera camera, RenderTexture texture, LayerMask clickMask);
    public string Id { get; }  public Camera Camera { get; }  public RenderTexture Texture { get; }  public LayerMask ClickMask { get; }
    public bool IsRendering { get; }
    public void SetRendering(bool on);                                   // camera.enabled
    public bool TryPick(Vector2 viewportPoint, out GameObject hit);     // Physics.Raycast(camera.ViewportPointToRay, farClip, ClickMask)
}
```
- Edge:
  - `ClickMask` is the fix for Q2.2 (the old relay raycast with no mask and hit bar props). Default: Garnish layer only.
  - A destroyed camera (main scene unloaded) makes `SetRendering`/`TryPick` no-ops that return false, with a single warning.
  - There is one owner per handle. No ref-counting (Garnish and Serve share one RawImage).

**`WorldViewSource`** (MonoBehaviour, main scene, WorldView asm)
- Resp.: the Inspector side of a handle. It builds the handle in Awake and disables the camera.
- Caller: Unity; the T10 glue reads `Handle`; `CameraDirector` (inset); the preload calls `Warm()`. Test: in T2 (the camera is disabled after Awake, and the warnings fire).
- Sig.: `[SerializeField] string _id; Camera _camera; RenderTexture _texture /*optional, else camera.targetTexture*/; LayerMask _clickMask;` and `public WorldViewHandle Handle { get; }` and `public void Warm()`, which calls `camera.Render()` once at preload to compile shaders before the 300 ms gate.
- Placed on: `GarnishStageCam` (id `GarnishGlass`), `Minigame_Stirring_Cam` (`Stirring`), `ShakingAnimationCam` (`Shaking`), `MinigameCharacterView` (`CustomerInset`).
- Edge: Awake warns if the camera has an AudioListener or CinemachineBrain, or if the target texture is null.

**`WorldView`** (MonoBehaviour on the RawImage, UI scene, WorldView asm; `IPointerClickHandler`)
- Resp.: shows a handle's texture, relays clicks into the world, and owns `blocksRaycasts`.
- Caller: `MinigameUiHost` and `GarnishUiHost` (`Bind`/`Show`/`Hide`); the EventSystem (clicks).
- Sig.: `public void Bind(WorldViewHandle h); public void Unbind(); public void Show(); public void Hide(); public bool IsShown { get; }`. Fields: `[SerializeField] CanvasGroup _group /*optional*/`.
- Wraps / replaces: `RenderTextureClickRelay` (logic copied, including the `uvRect` mapping that `GarnishManager.RevealServe` animates). It also replaces the inset-panel half of `CameraManager.ShowCustomerInset`.
- Edge:
  - `Hide()` sets **`raycastTarget=false` and `blocksRaycasts=false`** and stops rendering. This fixes the bug where hidden UI blocked raycasts.
  - Clicks while unbound or hidden are ignored.
  - Picks are forwarded with `ExecuteEvents.ExecuteHierarchy(hit, eventData, pointerClickHandler)` (as before), so `GarnishSlotButton` works unchanged.
  - Virtual-cursor clicks arrive as ordinary pointer events (UNVERIFIED until the R6 spike).
- Test: PlayMode. Put a test collider behind the glass on another layer and assert it is not hit (O4 §2.4 criterion 1). Toggle Show/Hide 100 times and assert `blocksRaycasts == IsShown`.

### 2.3 Additive UI scenes: loading and registration protocol

**`UiSceneId`** (Contracts): `public enum UiSceneId { Minigame, Garnish }`

**`IUiScenes`** (Contracts) and **`IUiSceneRegistry`** (Contracts)
```csharp
public interface IUiScenes {
    Awaitable PreloadAsync(CancellationToken ct);                       // both scenes; idempotent; call at BeforeOpen
    Awaitable<T> GetHostAsync<T>(UiSceneId id, CancellationToken ct) where T : class; // loads if needed (slow path, warns)
    bool IsReady(UiSceneId id);
    event Action<UiSceneId, object> Registered;                         // Assembly-CSharp glue binds world-side objects here
}
public interface IUiSceneRegistry { void Register(UiSceneId id, object host); void Unregister(UiSceneId id, object host); }
```
**`UiSceneLoader`** (plain C#, UiScenes asm, implements both)
- Sig.: `public UiSceneLoader(IReadOnlyDictionary<UiSceneId, string> sceneNames, float registerTimeoutSeconds = 10f)`. GameRoot holds one instance.
- Caller: G1 DayRunner (`PreloadAsync` at BeforeOpen) and the stations (`GetHostAsync`); the T10 glue subscribes to `Registered`.

**`UiSceneRegistry`** (static, UiScenes asm)
- Resp.: the one static handshake point a UI scene can reach without referencing `GameRoot`. An asmdef cannot see Assembly-CSharp, where §1 puts GameRoot.
- Sig.: `public static class UiSceneRegistry { public static IUiSceneRegistry Current { get; private set; } public static void Set(IUiSceneRegistry r); public static void Clear(IUiSceneRegistry r); }`
- Caller: GameRoot calls `Set` in Awake and `Clear` in OnDestroy; `UiSceneEntry` registers through `Current`.
- Edge: `Clear` with a different instance is a no-op. A duplicate `Set` logs an error.
- Test: covered by T3.
- Protocol:
  1. `SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive)`, then `await Awaitable.FromAsyncOperation(op, ct)`.
  2. Assert `SceneManager.GetActiveScene()` is still the main scene. Ambient light, probes and lightmaps come from the active scene, so the UI scene must never become active.
  3. Wait (polling with a timeout) until the scene's `UiSceneEntry` has called `Register`.
  4. Raise `Registered(id, host)`. Glue binds the world handles (§2.4/§2.5).
  5. Mark ready.
- Multiple waiters on the same id share the in-flight load through the poll flag. A UI scene unloads together with the main scene (Single-mode load). There is no explicit unload in R3.
- Edge:
  - Timeout: error naming the missing entry. Throws `TimeoutException`.
  - A duplicate `Register` for the same id: error; the second registration is ignored.
  - Silent replay (G2/G4) never calls hosts, but preloading is harmless.
  - The Pause scene is also additive. It is not managed here.

**`UiSceneEntry`** (abstract MonoBehaviour, UiScenes asm; one per UI scene root)
- Caller: Unity (Awake/OnDestroy). Subclasses: `MinigameSceneEntry` and `GarnishSceneEntry` (Assembly-CSharp, return their host).
- Sig.: `[SerializeField] UiSceneId _id; protected abstract object Host { get; }`.
- Awake:
  - Scans its own scene with `scene.GetRootGameObjects()` → `GetComponentsInChildren<…>(true)` for **EventSystem, AudioListener, CinemachineBrain and Camera**. Each one found is an error and gets destroyed.
  - Registers through `UiSceneRegistry.Current`. If it is null, it logs "standalone" and calls `OnStandalone()` (a virtual dev harness, editor only).
- OnDestroy: `Unregister`.
- Rule: **the root stays active.** Awake does not run on inactive objects, and `MinigameSystemManager.Update` ticks every frame. Hide with `Canvas.enabled=false` plus `CanvasGroup` (alpha 0, `blocksRaycasts=false`), not with `SetActive`.
- Perf (300 ms gate): at preload, enable the canvas once while at alpha 0, call `Canvas.ForceUpdateCanvases()`, then disable it. This builds the TMP meshes and layout up front. Also call `WorldViewSource.Warm()`.
- Test:
  - PlayMode: load Main_V2, `PreloadAsync`, assert both registered within 10 s and the active scene unchanged.
  - Count EventSystem, AudioListener and CinemachineBrain across all loaded scenes; each must be 1.
  - Profiler marker around Show: under 300 ms; `fps >= 60` over a 10 s run.

### 2.4 Minigame

**`MinigameRequest`** (Contracts)
- Sig.: `public readonly struct MinigameRequest { public readonly Enum_MiniGameType Selected; public readonly Enum_MiniGameType FromDrink; public readonly bool ShowCustomer; public MinigameRequest(Enum_MiniGameType selected, Enum_MiniGameType fromDrink, bool showCustomer); }`
- Edge: the host resolves the type with the existing `MinigameSystemManager.Resolve(selected, fromDrink)`, so `_defaultType` stays in the UI scene. Before the request, `ShowCustomer` is false when `Order.Customer == None` (old warning: "add `<<order_customer>>`").

**`IMinigameHost`** (Contracts)
```csharp
public interface IMinigameHost {
    bool IsBusy { get; }
    event Action<Enum_MiniGameType> Started;          // tutorial / Hub hook
    Awaitable<MinigameOutcome> RunAsync(MinigameRequest request, CancellationToken ct); // Completed | Cancelled only
}
```
- Caller: G1 PrepareStation (2.2).
- Semantics:
  1. If `IsBusy`, throw `InvalidOperationException`.
  2. **Wait until `manager.ActivePhase == Idle`** (F11), with a timeout of outro fallback + 0.5 s.
  3. Show the canvas; bind and show the views for the resolved type plus the customer inset if `ShowCustomer`.
  4. Subscribe to `MinigameFinished` and `BackRequested`, **then** call `StartMinigame`.
  5. Back button → `CancelMinigame()` → returns `Cancelled`. There is no fail outcome (a design rule).
  6. `ct` cancelled → `CancelMinigame()`, wait for the outro (bounded), clean up, throw OCE.
  7. In `finally`: unsubscribe, `Unbind`/`Hide` the views, `SetRendering(false)`, hide the canvas.
- Edge:
  - A type with no registered game (`Building`): the old code only warned and would hang. Now it logs an error and **returns `Completed`** so the drink is not blocked (decision for the owner, §d U7).
  - A stale `MinigameFinished` from an earlier run is ignored by comparing a run id (belt and braces on top of step 2).
  - Pause sets `timeScale=0`, which freezes the run. Correct as is; nothing to do.
  - Re-enabling the shaker after `Cancelled` is **station-side** (`IBarCounter.SetShakerGrabbable(true)`).
- Test: PlayMode with a fake `BaseMiniGame` subclass that completes or cancels on a frame count. Cases: Complete; Back; ct mid-Intro, mid-Play and mid-Outro; two quick RunAsync calls (cancel the first, start the second while its outro plays).

**`IMinigameAnimation`** (Contracts): `public interface IMinigameAnimation { void Play(); void Stop(); }`
- Wraps: `AnimationMinigameController.PlayAnimation/StopAnimation` (the world object `Minigame_Stirring`).

**`MinigameUiHost`** (MonoBehaviour, UI scene `Minigame_UI`, Assembly-CSharp; implements `IMinigameHost`; it is the entry's `Host`)
- Sig. (glue only): `public void Bind(IReadOnlyDictionary<Enum_MiniGameType, MinigameWorld> worlds, WorldViewHandle customerInset)`, with `public sealed class MinigameWorld { public WorldViewHandle View; public IMinigameAnimation Animation; }`.
- Serialized: `MinigameSystemManager _manager`, `WorldView _customerView` (`Panel_CharacterView/RawImage_CharacterCameraView`), `ViewSlot[] _gameViews { Enum_MiniGameType type; WorldView view; }`, `CanvasGroup _root`.
- Change to kept code: `BaseMiniGame` gains `public void UseAnimation(IMinigameAnimation a)` and a `protected IMinigameAnimation Anim` property (injected value, or else a wrapper around the serialized `amc`, or null). `ShakingMinigame`/`MixingMinigame` change `amc.PlayAnimation()` to `Anim?.Play()`, and `BaseMiniGame` Outro uses `Anim?.Stop()`. The old scene is unchanged, because the fallback is the same `amc`.
- Edge: `RunAsync` before `Bind` throws `InvalidOperationException("not bound")`.

**`MinigameWorldSource`** (MonoBehaviour, main scene, Assembly-CSharp)
- Sig.: `[SerializeField] Enum_MiniGameType _type; WorldViewSource _view; AnimationMinigameController _amc;` and `public MinigameWorld Build()`.
- Caller: the T10 glue (collects these in `Registered`).
- Test: inside the T8 run test (Play/Stop reach the animator).

### 2.5 Garnish (steps 3 and 4)

**DTOs** (Contracts)
```csharp
[Serializable] public struct GarnishDecoration { public SO_GlassOption glass; public bool ice; public SO_GarnishItemOption item0, item1; public SO_GarnishRimOption rim; }
public enum GarnishExit { Finished, Remake }           // Remake covers both the Reset button and Remake
public readonly struct GarnishResult { public readonly GarnishExit Exit; public readonly GarnishDecoration Decoration; }
public enum ServeDecision { Served, Remake }
public readonly struct GarnishRequest { public readonly S_Drink Drink; public GarnishRequest(S_Drink drink); } // Drink = IBarCounter.ResolveDrink()
```
**`IGarnishHost`** (Contracts)
```csharp
public interface IGarnishHost {
    bool IsBusy { get; }
    Awaitable<GarnishResult> RunAsync(GarnishRequest request, CancellationToken ct); // step 3: Open() … Finish/Reset → Close()
    Awaitable<ServeDecision> ServeAsync(CancellationToken ct);                       // step 4: OpenServe() … Serve/Remake → CloseServe()
}
```
- Why two methods: §4.1 has Garnish and Serve as separate stations, and each can return Remake. Hub must switch Garnish→Serve between them. The locked phrase "decoration + serve info" maps to `GarnishResult.Decoration` plus `ServeDecision`.
- Caller: G1 GarnishStation and ServeStation.
- What the main scene does around the calls (station-side):
  - On `Finished`: `bar.SetIce(result.Decoration.ice)` (F9).
  - On `Remake`: `bar.ResetDrink()` and do **not** apply ice.
  - `garnishTable.Clear()` at Prepare entry and at Serve exit (old `OnTableReset`).
  - The decoration stays on the GarnishTable glass, which is the "bar glass" until Serve exits (parity). Applying it to a separate bar glass object is optional task T14 (UNVERIFIED that such an object exists).
- Edge:
  - Busy → `InvalidOperationException`.
  - `request.Drink` null or empty → Pour warns and does nothing (old "Nothing in the shaker to pour").
  - Finish before the fill completes → refused (old `TryFinish` rule).
  - Remake mid-pour → `table.Clear()` cancels the pour (old `ClearGlass`).
  - `ct` mid-slide → `Close()`/`CloseServe()` in `finally` stop the coroutines.
  - Live language switch inside Garnish: TMP `LocalizeStringEvent` in the UI scene handles it. Nothing extra here.
- Test: PlayMode in Main_V2 with the checklist: glass, ice, garnish, Pour, Finish, Serve, then score == old score for the same drink. Remake from both panels. Assert that `Scored` was never raised on a remake.

**`IGarnishTable`** (Assembly-CSharp, Adapters; world side. Its types are old classes, and no asmdef consumer needs it.)
```csharp
public interface IGarnishTable {
    PlacedGlassInstance Glass { get; }                 // grids read != null
    GarnishDecoration Decoration { get; }              // recorded on every Apply*
    bool ChooseGlass(SO_GlassOption option);           // destroys the previous glass first
    void Clear();                                      // cancels a running pour, destroys the glass
    void ApplyIce(bool on);
    void ApplyItem(int slot, SO_GarnishItemOption item);
    void ApplyRim(SO_GarnishRimOption rim);
    void Pour(S_Drink drink);
    event Action FillComplete;  event Action<int> SlotClicked;  event Action<bool> GlassChanged;
}
```
- Caller: `GarnishManager` (in the UI scene, after `UseTable`); the main-scene stations call `Clear()`.
- Wraps: the world half of `GarnishManager` (ChooseGlass, ClearGlass, the slot and pour subscriptions, `glass.Pour(_pour, drink)`), plus `PlacedGlassInstance` and `PourVisual`.
- Test: PlayMode in the old scene (legacy self-built table): garnish regression. In Main_V2: `Decoration` matches what was clicked; `Clear()` mid-pour leaves no glass and no running pour.
- Implementation:
  - `GarnishTableCore` is a plain class: `new GarnishTableCore(Transform spawnPoint, PourVisual pour)`, using `Object.Instantiate`/`Destroy`.
  - `GarnishTable` is the MonoBehaviour shell in the main scene (`[GarnishSystem]/Garnish_Plate`).
  - `GarnishManager` keeps every **UI rule** (glass first, one pour per glass, no ice removal after the pour, active slot, slides, serve reveal, SFX) and calls only `IGarnishTable`.
  - New `public void UseTable(IGarnishTable t)`. If none is injected, Awake builds a `GarnishTableCore` from its legacy `_glassSpawnPoint`/`_pour`, so the old scene works unchanged.
  - The grids are untouched (same class name, same events).
- **`GarnishUiHost`** (MonoBehaviour, UI scene `Garnish_UI`, Assembly-CSharp; implements `IGarnishHost`). Glue: `public void Bind(IGarnishTable table, WorldViewHandle glassView)`, which calls `GarnishManager.UseTable(table)` and binds the `GlassView` WorldView.

### 2.6 Camera facade

**`CameraShot`** (Contracts): `public enum CameraShot { Bar, Shaker, Garnish }`

**`ICameraDirector`** (Contracts)
```csharp
public interface ICameraDirector {
    string ActiveCameraId { get; }
    void Show(CameraShot shot);                 // blend (ShowBar/ShowShaker/ShowGarnish)
    void Cut(CameraShot shot);                  // CutToBar et al.
    void SwitchTo(string cameraId);             // Yarn Switch_Camera (unknown id: error log, no change)
    void CutTo(string cameraId);                // Yarn Cut_Camera
    void SetLookFree(bool free);                // ADR 0002 amendment: true only in 2.1 while shaker HasRoom; false snaps forward
    void ResetLook();
    void AimCustomerInset(Transform customer);  // positions the inset camera (offset); null = stop rendering
}
```
- Caller: G1 stations and DayRunner; G2 verbs `Switch_Camera`/`Cut_Camera`.
- **`CameraDirector`** (adapter, Assembly-CSharp). Ctor: `new CameraDirector(CameraManager manager, WorldViewSource customerInset, Vector3 insetOffset)`.
- Wraps: `CameraManager` (kept about 98%). Changes: delete `using Mono.Cecil.Cil;` (F12); assign the `brain` field in the scene (the code map warning). In Main_V2, `_customerInsetCamera` and `_customerInsetPanel` stay **empty**: the inset belongs to the `CustomerInset` WorldViewSource, which has a single owner of `camera.enabled`.
- Edge:
  - The station must call `AimCustomerInset` before `IMinigameHost.RunAsync`, and `AimCustomerInset(null)` in `finally`.
  - `ToggleShakerView`/`IsOnShakerView` are not exposed (unused by the flow).
  - Mouse look still reads `Input.*` inside `CameraController` until R6 (G4).
- Test: PlayMode with each shot: assert `ActiveCameraId`. `SetLookFree(false)` must snap forward.

### 2.7 Customers facade (characters only)

**`StageTiming`** (Contracts): `public enum StageTiming { Normal, Instant }`. G2 passes `Instant` while skipping or replaying, which replaces the `IsSilentReplay`/`IsSkipping` statics.

**`CharacterState`** (Contracts): `[Serializable] public struct CharacterState { public CharacterName name; public bool present; public int spot; public int expression; public Facing look; }`

**`ICustomerStage`** (Contracts)
```csharp
public interface ICustomerStage {
    bool TryParse(string raw, out CharacterName who);    // "Walter" | "CharacterName.Walter"; warns on typo
    bool Exists(CharacterName who);
    bool IsPresent(CharacterName who);
    Transform Anchor(CharacterName who);                 // null if unknown; used for the inset
    Awaitable EnterAsync(CharacterName who, int spot, StageTiming timing, CancellationToken ct);
    Awaitable ExitAsync(CharacterName who, StageTiming timing, CancellationToken ct);
    bool SetExpression(CharacterName who, string idOrIndex);
    void SetLook(CharacterName who, Facing look);
    void SetSpeaking(CharacterName who, bool speaking);  // G2 voice Started/Ended hook (§12.3)
    IReadOnlyList<CharacterState> Capture();
    void Restore(IReadOnlyList<CharacterState> states);  // no fade
}
```
- Caller: G2 verbs (long and short form `<<Isla Sad>>`), G2 voice (`SetSpeaking`), G4 save (`Capture`/`Restore` behind `ISnapshotSource`), G1 (`Anchor` for the inset).
- **`CustomerStage`** (adapter, Assembly-CSharp). Ctor: `new CustomerStage(TalkToCustomerManager manager)`.
- Wraps:
  - The character half of `TalkToCustomerManager` (`Find`, `Enter`, `Exit`, `Snapshot`, `Restore`) and `CharacterView`.
  - **Not wrapped:** `StartStory`, `CurrentDay`/`AdvanceDay`, `CommitOrderResult`/`ResetVariables` (Yarn variables). These go to G2 (DialogueDirector / Yarn variables), and G1 owns the day count.
- Changes to kept code:
  - `TalkToCustomerManager.Enter/Exit` gain an overload with `bool instant`. The old signature passes `SceneLoaderBridge.IsSilentReplay || DialogueControls.IsSkipping`, so it is behaviour-neutral.
  - `CharacterView` gains `SetSpeaking(bool)`.
- Coroutine bridge: G1's `FlowAwait.FromCoroutine(MonoBehaviour runner, IEnumerator routine, CancellationToken ct)` (§e).
- Edge:
  - Cancellation is honoured **after** the running fade. A fade lasts at most `_enterSoundLead` + `_fadeSeconds` ≈ 1.4 s. Its `DOVirtual.Float` has no target and cannot be killed safely, so the fade finishes and then OCE is thrown.
  - An unknown name warns and completes at once.
  - Entering an occupied spot only warns (existing ponytail note).
  - LEGACY VISUAL: `Restore` keeps the existing look-vs-expression rule.
- **Speaking hook (`CharacterView.SetSpeaking`)**:
  - A DOTween looped **scale** pulse on the `Body` child transform (`SetTarget(this)`), tuned by a `SpeakPulse { float scale = 0.02f; float frequency = 3f; }` field on `TalkToCustomerManager`, like `IdleBob`.
  - **Never a position tween.** `CharacterView.Update` writes `transform.position` absolutely during idle, and so does `Fade`. Scale is free.
  - `false`, `FadeOut` and `OnDisable` kill the tween and restore the authored scale. Calls while not present are ignored.
- Test: PlayMode. Enter→Exit with Normal and Instant; `Capture`→`Restore` round-trip equality; `SetSpeaking(true)` during idle bob, then assert the position is unchanged by the pulse and the scale returns exactly to authored on `false`.

### 2.8 Ingredient unlocks

**`UnlockNotice`** / **`UnlockState`** (Contracts)
```csharp
[Serializable] public struct UnlockNotice { public string key; public string displayName; public Sprite icon; } // displayName → key in R7
[Serializable] public sealed class UnlockState { public List<string> storyUnlocked = new(); public List<string> notified = new(); }
```
**`IIngredientUnlocks`** (Contracts)
```csharp
public interface IIngredientUnlocks {
    bool IsUnlocked(string ingredientKey);           // enum name, e.g. "LemonJuice"; not in table → true
    bool IsRecipeComplete(S_Drink drink);
    void Unlock(string ingredientKey);               // case-insensitive; unknown → error log
    IReadOnlyList<UnlockNotice> TakeNewUnlocks();    // marks notified; day-1 rows pre-notified
    event Action Changed;
    UnlockState Capture();  void Restore(UnlockState state);
}
```
- Caller: G1 Event day part and the bar (`RefreshUnlocked`, `UseRecipeFilter`); G2 verbs `unlock_ingredient`/`ingredient_unlocked()`; G4 save.
- **`UnlocksAdapter`** (Assembly-CSharp, about 40 lines). Wraps `IngredientUnlocks` (kept 100%, still reads `$day` through its own `DialogueRunner` reference).
- Edge:
  - The "notified" list must be saved (CLAUDE.md save issue 3).
  - `IsUnlocked` is exact-case and `Unlock` is case-insensitive (existing behaviour). Keep both.
- Test: EditMode is not possible (it reads Yarn), so PlayMode: day 1 → no notices; set `$day=2` → notices; round-trip `Capture`/`Restore`.

**`UnlockPopup`** (static, Contracts)
- Sig.: `public static EventPopup From(IReadOnlyList<UnlockNotice> n)`. Title key `"event.unlock.title"` ("Unlock New Ingredient"), one body line per name, the icons. It replaces the inline code in `FlowWiring.OnEventEntered`.
- Test: EditMode (pure).

### 2.9 Hub, event popups, chapter title

**`IHub`** (Contracts): `public interface IHub { HubPhase CurrentPhase { get; } void SetPhase(HubPhase phase); }`
- Implemented **directly** on `HubManager` (add `: IHub`; the method already exists). `HubPhase` moves to its own file (F5).
- Caller: G1 stations and day parts.
- Edge: rows are `[SceneObjectsOnly]`, so a UI-scene canvas can never be a Hub row. Correct: System UI stays with its host.
- Test: the existing Preview buttons, plus a PlayMode pass through all phases (alpha and `blocksRaycasts` per row).

**`EventPopup`** / **`IEventPopups`** (Contracts)
```csharp
public sealed class EventPopup { public string titleKey; public IReadOnlyList<string> bodyKeys; public IReadOnlyList<Sprite> images; }
public interface IEventPopups {
    bool HasPending { get; }
    void Enqueue(EventPopup popup);
    Awaitable ShowAllAsync(CancellationToken ct);    // completes on AllClosed (after the fade-out)
}
```
- **`EventPopupsAdapter`** (Assembly-CSharp). Ctor: `new EventPopupsAdapter(EventPopupQueue queue, Func<string, string> localize /*G2; identity until R7*/)`. Wraps `EventPopupQueue` (kept 100%).
- Keys are stored, not text (Options §11.4 risk list). They are resolved when shown.
- Edge:
  - **Subscribe to `AllClosed` before `ShowAll()`**: an empty queue raises it synchronously.
  - A second `ShowAllAsync` while showing is joined by polling.
  - `ct` only stops waiting (teardown).
  - Re-rendering the open popup on a live language switch is in R7 (needs a `Refresh` on the queue).
- Test: PlayMode. An empty queue completes in the same frame; 2 popups need 2 closes.

**`IChapterTitle`** (Contracts): `public interface IChapterTitle { bool IsPlaying { get; } Awaitable ShowAsync(string chapterKey, CancellationToken ct); }`
- **`ChapterTitleAdapter`** (Assembly-CSharp). Calls `ChapterTitleUI.Show(localize(key))`, then polls `IsPlaying` each frame. Wraps `ChapterTitleUI` (kept 100%).
- Caller: G1 `OpenBarAsync` (`<<flow_open_bar>>` waits for the fade-out, as before).
- Edge: a missing UI completes at once; Pause freezes the DOTween sequence (default time), so the wait stretches (correct).
- Test: PlayMode: `ShowAsync` returns after the fade-out (about fadeIn+hold+fadeOut), and returns at once when the UI is missing.
- `SO_DaySchedule` is **data**, read by G1 directly (no wrapper). `chapterName` becomes a key in R7.

### 2.10 How much of each manager is kept

| Old class | Kept | Change to kept code | New thin code |
|---|---|---|---|
| `CocktailSystemManager` (+3 partials) | ~100% | 2 methods `private`→`internal` | `CocktailFeature` (~150 lines, IOrderDesk+IBarCounter) |
| `ShakerContents`, `ShakerPanelController`, `IngredientButtonGroup`, `MethodPanel`, `VisualizeCocktail`, `Post_It_Order` | 100% | none | inside `CocktailFeature` |
| `MinigameSystemManager` | 100% (moves to the UI scene) | none | `MinigameUiHost` (~130) |
| `BaseMiniGame`, `ShakingMinigame`, `MixingMinigame` | ~97% | `UseAnimation`/`Anim` injection, 3 call sites | `MinigameWorldSource` (~25), `AmcAnimation` (~10) |
| `GarnishManager` | ~75% (UI rules, slides, events) | world calls routed to `IGarnishTable`; `UseTable` | `GarnishTableCore` (~120, extracted), `GarnishTable` shell, `GarnishUiHost` (~80) |
| Grids (`GlassChoiceGrid`, `GarnishItem/RimChoiceGrid`), `PlacedGlassInstance`, `GarnishSlotButton` | 100% | none | — |
| `RenderTextureClickRelay` | old scene only | none | replaced by `WorldView` in V2 |
| `CameraManager` | ~98% | drop `Mono.Cecil` using; inset fields empty in V2 | `CameraDirector` (~60) |
| `TalkToCustomerManager` (character partial) / `CharacterView` | ~95% | `instant` overloads; `SetSpeaking` | `CustomerStage` (~110) |
| `IngredientUnlocks` | 100% | none | `UnlocksAdapter` (~40) |
| `HubManager` | 100% | `: IHub`, `HubPhase` file split | — |
| `EventPopupQueue` | 100% | none | `EventPopupsAdapter` (~40) |
| `ChapterTitleUI` | 100% | none | `ChapterTitleAdapter` (~25) |
| `FlowWiring` | 0% (G1 replaces) | — | its bar-side logic now lives in `CocktailFeature`; its unlock popup composition in `UnlockPopup` |

---

## (a) Dependency diagram

```
                 Bar410.Domain  (S_Drink, E_Cocktail, DrinkOrderContext, scoring, SO data, HubPhase, Facing, MinigameOutcome)
                        ▲
                 Bar410.Contracts  (IOrderDesk IBarCounter IMinigameHost IGarnishHost ICameraDirector ICustomerStage
                        ▲           IIngredientUnlocks IHub IEventPopups IChapterTitle IUiScenes IUiSceneRegistry DTOs)
        ┌───────────────┼──────────────────────┬───────────────────────┐
   Bar410.WorldView  Bar410.UiScenes      Bar410.Flow (G1)        Bar410.Dialogue (G2) / Bar410.Save (G4)
        ▲               ▲                      (stations use interfaces only)
        └──────┬────────┘
   Assembly-CSharp:  Adapters (CocktailFeature, CameraDirector, CustomerStage, UnlocksAdapter, EventPopupsAdapter,
                     ChapterTitleAdapter, GarnishTable(+Core), MinigameWorldSource, MinigameUiHost, GarnishUiHost)
                     ──wrap──► old managers (CocktailSystemManager, GarnishManager, MinigameSystemManager, CameraManager,
                                TalkToCustomerManager, IngredientUnlocks, HubManager, EventPopupQueue, ChapterTitleUI)
                     GameRoot (G1, must be here or receive adapters from here)

Runtime, across scenes:
 Main_V2 ── WorldViewSource(GarnishGlass, Stirring, Shaking, CustomerInset) ─handles─┐
         ── GarnishTable, MinigameWorldSource                                       │ bound once in IUiScenes.Registered
 Minigame_UI ── UiSceneEntry → MinigameUiHost(MinigameSystemManager+games, WorldViews) ◄┤
 Garnish_UI  ── UiSceneEntry → GarnishUiHost(GarnishManager+grids, WorldView GlassView) ◄┘
 Stations (G1) ── await IMinigameHost.RunAsync / IGarnishHost.RunAsync / ServeAsync ── results only
```

---

## (b) Task breakdown (ordered)

| id | Task | Files to create/change | Inputs | Definition of done | Diff | Model (why) |
|---|---|---|---|---|---|---|
| T0 | Prerequisite file splits so types can move (no behaviour change) | split `MinigameOutcome`/`MinigamePhase` out of `IMinigame.cs`, `HubPhase` out of `HubManager.cs`, `Facing` out of `CharacterView.cs` into new files; delete `using Mono.Cecil.Cil;` | F3, F5, F12 | old scene compiles and plays the demo day 1; `read_console` clean | 1 | haiku (mechanical) |
| T1 | Contracts asmdef + all DTOs/interfaces in §2 | `Assets/Bar410/Contracts/Bar410.Contracts.asmdef`, one `.cs` per interface family (`Orders.cs`, `Minigame.cs`, `Garnish.cs`, `Camera.cs`, `Customers.cs`, `Unlocks.cs`, `Hub.cs`, `UiScenes.cs`) | §2 signatures; R2 Domain asmdef done (G1/R2) | compiles with **no** reference to Assembly-CSharp; XML doc on each member states the edge rules | 2 | sonnet (must transcribe semantics exactly) |
| T2 | WorldView asmdef: `WorldViewHandle`, `WorldViewSource`, `WorldView` | `Assets/Bar410/Features/WorldView/*` | §2.2, old `RenderTextureClickRelay` | PlayMode test: mask excludes a decoy collider; Show/Hide toggles `blocksRaycasts`/`raycastTarget`/camera; uvRect mapping equals old relay | 3 | sonnet (porting the relay logic plus new show/hide state; no concurrency) |
| T3 | UiScenes asmdef: `UiSceneLoader`, `UiSceneEntry` | `Assets/Bar410/Features/UiScenes/*` (incl. `UiSceneRegistry`) | §2.3; G1 calls `UiSceneRegistry.Set` | PlayMode: load two empty UI scenes, both register, active scene unchanged, a forbidden EventSystem placed in a UI scene is reported+destroyed, timeout path throws | 4 | opus (async load + multi-waiter + timeout, Awaitable single-await rule) |
| T4 | `CocktailFeature` adapter (IOrderDesk + IBarCounter) + `internal` visibility change | `Adapters/Cocktail/CocktailFeature.cs`; edit `CocktailSystemManager.YarnOrders.cs` | §2.1, `FlowWiring` §2/§2.1/OnShaker*/OnMethod* | R0 checklist 3–6 pass in Main_V2 with stations; `CommitServe` twice → one `Scored`; dispose leaves 0 listeners | 3 | sonnet (straight mapping, but many members and invariants to keep) |
| T5 | `CameraDirector` adapter | `Adapters/Camera/CameraDirector.cs` | §2.6 | each `CameraShot` switches; inset camera only renders between Aim(t) and Aim(null) | 2 | haiku (thin mapping) |
| T6 | Small facades: `UnlocksAdapter`, `UnlockPopup`, `EventPopupsAdapter`, `ChapterTitleAdapter`, `HubManager : IHub` | `Adapters/Unlocks/*`, `Adapters/Hub/*` | §2.8–2.9 | EditMode test for `UnlockPopup`; PlayMode: empty popup queue completes same frame; chapter title awaits fade | 2 | haiku (boilerplate) — sonnet for `ShowAllAsync` if haiku misses the subscribe-first rule |
| T7 | `CustomerStage` + `instant` overloads + `CharacterView.SetSpeaking` | `Adapters/Customers/CustomerStage.cs`; edit `TalkToCustomerManager.Characters.cs`, `CharacterView.cs` | §2.7; G1 `FlowAwait.FromCoroutine` | old Yarn commands behave identically in old scene; V2: Enter/Exit Normal+Instant, snapshot round-trip, speaking pulse leaves position untouched | 3 | sonnet (coroutine bridging plus a small DOTween addition) |
| T8 | Minigame split: `BaseMiniGame.UseAnimation`, `MinigameWorldSource`, `AmcAnimation`, `MinigameUiHost`, scene `Minigame_UI` (copy `Canvas_MiniGame` + `[MiniGameSystem]` components; world children stay in Main_V2 with `WorldViewSource`s) | `Adapters/Minigame/*`; edit `BaseMiniGame.cs`, `ShakingMinigame.cs`, `MixingMinigame.cs`; `Assets/Bar410/Content/Scenes/Minigame_UI.unity` | §2.4, F6, F11 | all §2.4 test cases pass; old scene minigames unchanged; Profiler entry ≤ 300 ms, ≥ 60 fps | 4 | opus (outro/Idle race, cancellation during animator phases, cross-scene injection) |
| T9 | Garnish split: extract `GarnishTableCore`/`GarnishTable`, route `GarnishManager` through `IGarnishTable` (+ legacy self-built table), `GarnishUiHost`, scene `Garnish_UI` | `Adapters/Garnish/*`; edit `GarnishManager.cs`; `Garnish_UI.unity` | §2.5, F7, F9, F10 | old scene garnish identical (regression play); V2: full garnish+serve, score equals old for same drink; remake never scores; slot clicks via WorldView | 4 | opus (splitting a stateful class while keeping both scenes working) |
| T10 | Glue: on `IUiScenes.Registered` bind world handles/table to hosts; preload + warm at BeforeOpen | `Adapters/UiSceneBindings.cs` (called by GameRoot) | T3, T8, T9 | first entry into Minigame/Garnish after preload ≤ 300 ms (Profiler marker) | 2 | sonnet (small, but ordering-sensitive with the registration) |
| T11 | Main_V2 scene wiring for G3 objects (WorldViewSources, GarnishTable, MinigameWorldSources, remove canvases from V2 copy, assign `CinemachineBrain`, keep canvas `sortingOrder`) | `Main_V2.unity` (via Unity MCP; never move files with shell) | T2–T10 | with `PauseScene` **not** loaded: exactly 1 AudioListener and 1 Brain across loaded scenes, and 0 EventSystems in the UI scenes (the duplicate EventSystem inside the dialogue prefab is G2's, X3); inspector `[Required]` clean | 3 | sonnet (careful editor ops) |
| T12 | Perf/verification pass at R3 decision point | Profiler captures + short report in `Docs/` | O4 §2.4 criteria 1–5 | numbers recorded for fps/hitch, decoy-raycast, mouse click via RT; virtual cursor marked untested if no pad | 2 | sonnet (needs Profiler/MCP judgment, not code) |
| T13 | Docs: Systems Guide + CLAUDE.md component table entries for every new MonoBehaviour | `Docs/Bar410_Systems_Guide.md`, `CLAUDE.md` | all | each new component listed with scene/GameObject | 1 | haiku |
| T14 (optional) | Apply `GarnishDecoration` to a separate bar glass, if one exists | TBD | U4 | only if owner confirms the object | 2 | sonnet (ordinary feature code) |

Order: T0 → (R2 Domain move by G1) → T1 → T2, T3 in parallel → T4–T7 → T8 → T9 → T10 → T11 → T12 → T13.

---

## (c) Review checklist per task (Opus reviewer)

- **T0**
  - Only moves and splits, with no edits to logic.
  - The `.meta` of each MonoBehaviour file is untouched, so its GUID is preserved.
  - Serialized enum fields still load (Hub rows and character Facing values in the scene unchanged).
- **T1**
  - The asmdef references only Domain (and Unity).
  - No old class name appears in any signature.
  - Every async method takes a `CancellationToken`.
  - The docs state idempotency (`CommitServe`), "Completed|Cancelled only", "delta not total", and "subscribe-before-show".
- **T2**
  - Raycasts use `ClickMask`.
  - `Hide` resets **both** `raycastTarget` and `blocksRaycasts`.
  - Exactly one place toggles `camera.enabled`.
  - The uvRect mapping is copied unchanged.
  - No DOTween module calls in the asmdef.
- **T3**
  - No stored `Awaitable` is awaited twice.
  - Timeout and duplicate registration are handled.
  - The active scene is asserted.
  - Root kept active; the hide is via Canvas/CanvasGroup.
  - The forbidden-component scan covers inactive children.
  - `Unregister` happens on destroy.
- **T4**
  - `CommitServe` is the only path to `ServeDrink()` from new code.
  - A second call raises no event.
  - Remake never scores.
  - `ResolveDrink` runs `UpdateCocktailInShaker`.
  - No camera or Hub logic leaked into the adapter.
  - UnityEvent listeners are removed on dispose.
- **T5**
  - No new id table outside `CameraManager`.
  - Inset camera ownership is single.
  - `Mono.Cecil` is gone.
- **T6**
  - `ShowAllAsync` subscribes before `ShowAll`.
  - Keys are stored, not text.
  - `UnlockPopup` is pure.
  - `HubManager` behaviour is unchanged.
- **T7**
  - The old static Yarn commands still pass skip/replay through the default overload (old scene parity).
  - The pulse is scale-only and restores the exact authored scale.
  - Cancellation does not leave `IsPresent` inconsistent.
- **T8**
  - The host waits for Idle before Start.
  - Listeners are subscribed before `StartMinigame`.
  - Back maps to Cancelled; there is no fail path.
  - An unknown type does not hang.
  - The old scene uses the serialized `amc` fallback.
  - The views unbind and stop rendering in `finally`.
- **T9**
  - The grids are unmodified.
  - All the rules are still in `GarnishManager` (glass first, one pour, no ice removal after the pour).
  - The table is world-only.
  - The legacy path builds the table from the old fields.
  - Ice is applied to the drink only on `Finished`.
  - `Clear()` cancels the pour before destroying the glass.
- **T10**
  - Binding happens exactly once per registration.
  - A host used before binding throws a clear error.
  - Warm-up happens at preload, not at step entry.
- **T11**
  - Editor operations only.
  - Overlay `sortingOrder` preserved.
  - The single-instance counts were checked in Play.
  - The baked lightmap data was not regenerated by accident.
- **T12**
  - The numbers come from the Profiler, not from guesses.
  - Untested input paths are stated plainly.
- **T13**: the paths and GameObjects named actually exist.

---

## (d) UNVERIFIED assumptions, and what I read

**Read fully:**
- `CocktailSystemManager.cs` and partials `YarnOrders` and `YarnTask`
- `DrinkOrderContext`, `DrinkScoringService`, `ICustomerPreferences`, `YarnVariableSync`
- `MinigameSystemManager`, `BaseMiniGame`, `IMinigame.cs`, `IInputProvider`, `E_Cocktail.Minigame`
- `GarnishManager`, `RenderTextureClickRelay`, `CameraManager`
- `TalkToCustomerManager` (both partials), `IngredientUnlocks`, `HubManager`, `EventPopupQueue`
- `FlowWiring` lines 260–760

**Skimmed** (public API, grep, or headers only):
- Domain: `OrderService`, `ShakerContents`, `IngredientButtonGroup`, `ShakerPanelController`, `MethodPanel`, `VisualizeCocktail`, `Post_It_Order`, `S_Drink`, `IDrinkInterfaces`
- Domain bodies (only grepped for `AddIce`/`UnityEngine`)
- Minigame: `ShakingMinigame`, `MixingMinigame`, `MinigamePanelAnimator`, `AnimationMinigameController`
- Garnish and characters: `CharacterView` (most of it), `PlacedGlassInstance`, the grids, `GarnishSlotButton`
- Data and UI: `SO_IngredientUnlockTable`, `IngredientId`, `SO_DaySchedule`, `ChapterTitleUI`, `PanelTransition`, `CameraController`/`CameraEntry`

**Not read:** `PourVisual`/CocktailCrafter `PourController`, `SO_Customer` internals, `SO_CustomerRoster`, `InteractableToggle`, `BookUI_V2`, `SoundManager`, the Pause scene.

Scene facts F6–F8 come from parsing the `.unity`/`.prefab` YAML. The prefab dump for `[MiniGameSystem]` was **prefab-level**, so scene overrides (for example where `MinigameCharacterView` sits) were not checked.

| id | Assumption |
|---|---|
| U1 | `ShakingMinigame.amc` points at a shaking-side animator, or is null. Only one `AnimationMinigameController` was found in the prefab (on `Minigame_Stirring`). The scene may override this. |
| U2 | The minigame canvases' RawImages reference RT **assets** directly, so the texture survives the move and only the cameras need handles. |
| U3 | `MinigameCharacterView` and `RT_CharacterCameraView` are a scene object and an asset under `[MiniGameSystem]` (CLAUDE.md says so; not parsed). |
| U4 | A separate "bar glass" that should receive `GarnishResult` **does not exist today**. No listener was found, and `ClearGlass` runs at Serve exit. So parity = the decoration stays on the GarnishTable glass. T14 is optional. |
| U5 | Unity 6 `Awaitable.FromAsyncOperation` and `AwaitableCompletionSource` behave as documented (single await). Confirm in G1's R1 spike. |
| U6 | Yarn Spinner 3.2.5 rejects or warns on duplicate command names across attributes. If so, X2 is mandatory, not optional. |
| U7 | A minigame type with no registered game should count as `Completed` (owner decision; the old code hung). |
| U8 | `Canvas.enabled` toggling plus the preload warm-up keeps the first entry under 300 ms. Measure in T12. |
| U9 | Virtual-cursor clicks reach `WorldView.OnPointerClick` through `InputSystemUIInputModule` (R6 spike, G4). |
| U10 | The body sprite pivot makes a scale pulse look like breathing rather than jumping (check by eye). |
| U12 | `PauseScene` may contain its own EventSystem. Not checked. It decides whether "one EventSystem" can hold while paused. |
| U11 | The Unity 6 prefab-instance "removed GameObject" override would let Main_V2 reuse `[GameLoop].prefab` without the two canvases. Otherwise T11 uses an unpacked copy or a `[GameLoop_V2]` variant. |

---

## (e) Cross-group interfaces

**Expected from G1 (core flow / Stations / GameRoot)**
- **X1 (decision):**
  - Where `GameRoot` compiles. It must be Assembly-CSharp, or it must receive the adapters from an Assembly-CSharp bootstrap (§1).
  - It must own `UiSceneLoader` and publish it through `UiSceneRegistry.Set` (an asmdef cannot call `GameRoot.Current` if GameRoot is in Assembly-CSharp).
- R2 Domain asmdef move with the exact type list in §1, including all 4 `E_Cocktail` partials.
- `FlowAwait.Wait(AwaitableCompletionSource<T>, ct)` and `FlowAwait.FromCoroutine(MonoBehaviour, IEnumerator, ct)` helpers.
- Station-side duties listed under each host: camera shots, Hub phase, look free/fixed, `SetShakerGrabbable` after Cancelled, ice on Finished, `ResetDrink` on Remake, `garnishTable.Clear()` at Prepare entry and Serve exit, `AimCustomerInset` before the minigame, and a warning (not a random order) when there is no order.
- `PreloadAsync` called at BeforeOpen.
- Day count (`$day` advance on Close→BeforeOpen) and `SO_DaySchedule` lookup. These are not in G3.

- **X4 (needs confirmation):** Garnish is two calls, `IGarnishHost.RunAsync` (step 3) plus `ServeAsync` (step 4), not the single locked `RunAsync`. This follows the separate Garnish/Serve stations and the Hub Garnish→Serve switch. If G1 prefers one call, merge them into `RunAsync` with a `stageChanged` callback; the DTOs stay the same.
- **X1 corollary:** any asmdef code that needs the single global (G2 `YarnVerbs` included) cannot see an Assembly-CSharp `GameRoot`. Use the same pattern as `UiSceneRegistry`: a static handle inside the asmdef, set by GameRoot.

**Expected from G2 (dialogue / yarn / voice / localization)**
- **X3:** remove the second EventSystem inside the `Text Animator Dialogue System` prefab (the code map's "2 EventSystems" bug), or T11's single-instance check cannot pass.
- **X2:** the compat shim must not re-declare names that old `[YarnCommand]`/`[YarnFunction]` attributes already register (F13). Either route the old statics through `GameRoot.Current` facades, or remove the old attributes when the shim lands (one-line edits per command, coordinated with the old scene).
- The Yarn result writer subscribed to `IOrderDesk.Scored` writes `$satisfaction`, `$type_of_cocktail`, `$task_done` and `ApplyRelationshipDelta`, under the Journal policy. It replaces `TalkToCustomerManager.CommitOrderResult`/`ResetVariables`.
- `StartStory` replaces `TalkToCustomerManager.StartStory`.
- `StageTiming.Instant` while skipping or replaying.
- Voice `Started(CharacterName)` / `Ended(CharacterName)` events, which GameRoot wires to `ICustomerStage.SetSpeaking`.
- A `Func<string,string>` (or `ILocalizer`) for popups and the chapter title, with identity until R7.
- `SkipScan` must learn the new verb names.

**Expected from G4 (save + input)**
- An `ISnapshotSource` wrapper over `ICustomerStage.Capture/Restore`, `IIngredientUnlocks.Capture/Restore` (with notified), and the order state if needed.
- The "waiting for serve" flag read from G1 session state, **not** the `CocktailSystemManager.IsWaitingForTask` static.
- Input: `IInputProvider` backed by the action map for minigames (injected through the existing `BaseMiniGame.Initialize(context, input)`), a pointer source for `CameraController`, and virtual-mouse confirmation for `WorldView`.

**G3 provides:** every type in §2 (Contracts interfaces and DTOs, WorldView, UiScenes, adapters and hosts), the Main_V2/UI-scene wiring (T11), and the R3 perf/verification numbers (T12).

---

## Summary

G3 wraps the six managers behind ten small interfaces in a new `Bar410.Contracts` asmdef:
- `IOrderDesk` and `IBarCounter`
- `IMinigameHost` and `IGarnishHost`
- `ICameraDirector` and `ICustomerStage`
- `IIngredientUnlocks`, `IHub`, `IEventPopups`, `IChapterTitle`

Plus the `IUiScenes` loader. Most managers are kept 95–100% and get one-line or overload changes. The exception is `GarnishManager`, which must be split (about 75% kept): its world half (glass spawn, pour) becomes `GarnishTable` in the main scene, its UI rules stay in the UI scene, and the grids are untouched.

The key verified constraint is that `[02]Script` has no asmdef. Asmdef code cannot see any manager, so adapters and `GameRoot` must live in Assembly-CSharp, and R2 must move the Domain types first, including all four `E_Cocktail` partials and `MinigameOutcome`.

Cross-scene breaks found:
- `BaseMiniGame.amc` points to a world animator → it is injected.
- The inset panel inside `Canvas_MiniGame` → `CameraManager` keeps only the camera.
- The garnish world fields → `IGarnishTable`.

`WorldView` fixes raycast blocking and wrong-object hits (LayerMask).

Contract rules:
- Scoring happens only through an idempotent `CommitServe`.
- Minigames return only Completed or Cancelled; the host waits for Idle to avoid the outro race.
- Ice is applied before scoring.
- UI-scene roots stay active, so registration works and the 300 ms gate is achievable.

There are 14 ordered tasks. Opus is recommended for T3, T8 and T9.

File: `/tmp/claude-1000/-home-icebutmelted-WorkSpace-Git-ProjectCocktail-renew/5d41d2d1-898d-4666-8fdf-68fea549af8c/scratchpad/api_G3.md`
