# Minigame Customer View: Plan

Status: implemented (2026-10-31), see the results log at the end. Plan written 2026-10-30.

## Goal

While a minigame runs, `CharacterCameraView` (a RawImage in `Canvas_MiniGame`) slides in from the left and shows
the customer who ordered the drink. When it has arrived, its left edge touches the left edge of the screen.

## Decisions (owner, 2026-10-30)

| Question | Decision |
|---|---|
| Who ordered | `Order.Customer`, set through `<<order_customer>>`. Yarn is updated wherever it is missing |
| When it slides in | From the start of the Intro, not with the panels at 3.5-4.8 s |
| What the picture shows | The character together with the bar scene, one camera that moves to the customer |

## Current state

- **The picture is empty.** The RawImage uses `CamViewCharacter/RT_ShakingAnimation.renderTexture`, and no camera
  renders to it.
- **The picture would stretch.** The texture is 964x400 and the RawImage is 682.8x1080.
- **The texture name is misleading.** It has the same name as `Shaking/RT_ShakingAnimation`.
- **The RawImage is the last child of the canvas** and has `Raycast Target` on, so it sits on top and blocks clicks.
- **The rest position is already correct.** `Anchored Position X` 341.4 with width 682.8 and a centre pivot puts the
  left edge at x = 0. The panels are 1237.2 wide, so the two fill 1920 exactly.
- **Both minigames share one Animator.** `Canvas_MiniGame` has the `Panel - MinigameStage` controller, and both
  `MinigamePanelAnimator` components list it.
- **`Order.Customer` is often wrong or empty today.** `Order_Cocktail_ByName_*` and `order_by_type` keep whatever was
  there before. `Order_Cocktail_OutName(n)` sets the customer by itself.
- **The object exists only in the scene.** It is not in `[GameLoop].prefab`. The scene file names it
  `CharacterCameraView`, not `RawImage_CharacterCameraView`. Animation paths must use the name saved at that time.

## Design

No new component.

```
MinigameState.OnStartRequested
  -> FlowWiring.OnMinigameStartRequested
       -> view = _customers.Find(_cocktail.Order.Customer)
       -> _cameras.ShowCustomerInset(view?.transform)
       -> _minigames.StartMinigame(...)

MinigameFinished (after the outro)
  -> FlowWiring.OnMinigameFinished
       -> _cameras.HideCustomerInset()
```

- `FlowWiring` is the only place that connects the three managers, which keeps the architecture rule.
- `CameraManager` owns the extra camera, because it is already the one owner of the camera.
- The slide lives in the stage clips, so both minigames get it with no code, and its timing can be edited in the
  Animation window.
- The camera is enabled only while a minigame runs, so it costs nothing the rest of the time.
- `<<order_customer>>` missing: log a warning and hide the RawImage, rather than showing an old frame or the wrong
  person.

## Stages

Compile-check after every stage (`read_console` must show no error).

### Stage 1: Picture

- Rename the texture to `RT_CharacterCameraView` (keeps its GUID) and set its size to 683x1080.
- Add a `Camera` named `CustomerInsetCam` to the scene: disabled, Target Texture set to this texture, no
  `CinemachineBrain`, no `CameraEntry`, no `AudioListener`.
- Check that its layers exclude anything that should not appear, then tune the framing by hand on one character.
- Turn off `Raycast Target` on the RawImage.

### Stage 2: Animation

Add a curve for path `CharacterCameraView`, `m_AnchoredPosition.x`, to all four stage clips. The controller uses
Write Defaults, so a clip with no curve would snap the object back to its scene value.

| Clip (`Assets/[08]Animation/MinigamePanel/`) | Time | X |
|---|---|---|
| `MinigameStage_Hidden` | constant | -341.4 |
| `MinigameStage_Intro` | 0 to 1.3 s | -341.4 to 341.4 |
| `MinigameStage_Shown` | constant | 341.4 |
| `MinigameStage_Outro` | 0 to 1.2 s | 341.4 to -341.4 |

The 1.3 s matches the slide length of the panels and can be changed on the clip.

### Stage 3: Code

- `Assets/[02]Script/CameraControl/CameraManager.cs`: add serialized `_customerInsetCamera`, `_customerInsetImage`
  and `_customerInsetOffset`, plus these two methods:

  ```csharp
  /// <summary>Points the inset camera at the customer. Null hides the inset picture.</summary>
  public void ShowCustomerInset(Transform customer)
  {
      _customerInsetImage.enabled = customer != null;
      _customerInsetCamera.enabled = customer != null;
      if (customer != null) _customerInsetCamera.transform.position = customer.position + _customerInsetOffset;
  }

  public void HideCustomerInset() => _customerInsetCamera.enabled = false;
  ```

- `Assets/[02]Script/Hierarchical State Machine/FlowWiring.cs`: call them from `OnMinigameStartRequested` and
  `OnMinigameFinished`, with the warning for a missing customer.
- `CameraManager.cs` already has uncommitted changes and the `using Mono.Cecil.Cil` line. Leave both alone.

### Stage 4: Yarn

Add `<<order_customer "[CocktailSystem]" Name>>` to every order that uses `Order_Cocktail_ByName_*`, `$order_note`
with a by-name order, or `order_by_type`.

`SetOrderCustomer` keeps the target, so the line can go before or after the order, as long as it runs before the
minigame.

- Real game: `DEMO_3Days.yarn`, `VDO_Rec.yarn`, `PLAYTEST_Demo.yarn`, `Day1_Demo_EventNabLab.yarn`
- Skip: `VDO_Rec_Conversation.yarn` (no orders), `TEST_AllCommands.yarn` (its order tests stay as they are)
- Also update `Docs/YarnCommands/` if the description of `order_customer` changes: it was optional and is now needed
  for the picture. `Writer` documentation must say so.

### Stage 5: Test in Play mode, then docs

Test through Unity MCP:

1. Run Shaking and Stirring with a customer standing on each spot used by the demo (1, 2, 3).
2. Run an order with no `order_customer`: the warning appears and the RawImage is hidden.
3. Press Cancel during the Intro and during play: the view slides out and the camera turns off.
4. Remake a drink: the same customer is shown again.

Update `Docs/Bar410_Systems_Guide.md` and the "งานที่กำลังทำ" section of `CLAUDE.md`.

## Known limits

- **Cancel during the Intro jumps.** The Outro starts from the rest position. The panels already do the same, so it
  stays consistent.
- **The camera does not follow** the idle bob or a character moving during a minigame. The position is set once, at
  the start.
- **Art001 overlaps by about 30 px.** It spans x 653-1287 and the view ends at 682.8. The view is drawn on top. Check
  by eye and move it if it looks wrong.
- **Save/Load:** `Order.Customer` is not considered here, because Save/Load is on hold.

## Needs the owner's eye

Framing, offset and the look of the slide cannot be judged from code. Unity MCP was not connected when this plan was
written, so nothing here has been checked in the Editor.

## Results log (2026-10-31)

Status: implemented, compile clean, Play-tested by code (not by mouse). Framing needs the owner's eye.

### What differed from the plan
- The scene object is named `RawImage_CharacterCameraView`, not `CharacterCameraView`. The stage clips were first
  written with the plan's name, so their curve path was changed to `RawImage_CharacterCameraView` (all four clips).
  If the object is renamed, the clip paths must follow.
- A camera `[GameLoop]/[MiniGameSystem]/MinigameCharacterView` already existed in the scene (a copy of
  `GarnishStageCam`, still pointing at `GarnishStage_RT` and enabled). It was reused instead of adding
  `CustomerInsetCam`: Target Texture -> `RT_CharacterCameraView`, disabled, rotation reset to 0, Culling Mask excludes
  Ignore Raycast, UI, PlacementZone, Draggable, 2.5D_UI, SpriteAnimationToUI and PostProcessing. No Brain, no
  `CameraEntry`, no AudioListener.
- The offset in the plan (`0, 1.2, 2`) puts the camera behind the customer. The starting value is `0, 0.6, -2.2`
  (in front, 2.2 units away). It is a starting value only.
- `Order_Cocktail_ByName_*` orders in `Day1_Demo_EventNabLab.yarn` and `DEMO_3Days.yarn` (Walter / Isla / Owen, day 1)
  were missing `<<order_customer>>`: added 3 lines to each (6 total). `VDO_Rec.yarn` and `PLAYTEST_Demo.yarn` already had them.
- `FlowWiring.cs` and `CameraManager.cs` also contain unrelated uncommitted owner edits (`ToggleShakerView`,
  `IsOnShakerView`, `_ingredients.Disable()`, `using Mono.Cecil.Cil`); left alone.

### Play mode tests (driven by code through `GameFlowCommands` + `CocktailSystemManager`; dialogue stopped; customers placed with `TalkToCustomerManager.Enter` on spots 1, 2, 3)
1. Walter on spot 1 + Shaking, Isla on spot 2 + Stirring, Owen on spot 3 + Shaking: the camera moved to each spot
   (x 6.2 / 7.2 / 8.1), the view arrived at x = 341.4, and screenshots show Walter and Isla in the picture with its left
   edge at the screen edge. Owen's picture was not screenshotted (cancelled in the Intro, test 3).
2. No `<<order_customer>>` (Order cleared): the warning `[FlowWiring] The order has no customer` was logged, the RawImage was
   disabled and the camera stayed off.
3. Cancel during play (Walter, Isla): the view slid out to -341.4 and the camera turned off. Cancel during the Intro (Owen,
   x was 246 at that moment): the view slid out the same way and the camera turned off; the jump to the rest position
   noted in "Known limits" was not looked at by eye.
4. Remake: customer Isla, Stirring, `DrinkComplete` (camera off), `RemakeDrink`, Stirring again: `Order.Customer` was still
   Isla and the camera was repositioned to her spot; screenshot shows Isla.

### Not tested
- Real mouse clicks on the Cancel button; controller.
- Framing by eye: neighbours (Owen next to Isla) show at the edge of the picture at the current offset. Tune
  `Customer Inset Offset` and the camera rotation.
- Overlap with Art001 (about 30 px), look and speed of the slide.
- A customer that is not standing on a spot (picture shows the empty bar).
- Whole 3-day demo played through.

### Panel_CharacterView (2026-10-31, owner's structure change)
The owner added `Canvas_MiniGame/Panel_CharacterView` as the parent of `RawImage_CharacterCameraView`, because transparent
parts of the picture showed whatever was behind it.

- Cause, measured: the windows in the bar render with alpha 0 into `RT_CharacterCameraView` (with Isla on spot 2, 44,624
  pixels at alpha 0 and 47,402 partial, out of 737,640). The panel is the backdrop seen through them.
- The clips now animate `Panel_CharacterView` (`m_AnchoredPosition.x`: hidden -1301.4, rest -618.6), since the old path no
  longer resolved. The RawImage stays at 341.4 inside the panel.
- `CameraManager._customerInsetImage` (RawImage) became `_customerInsetPanel` (GameObject): no customer now turns the whole
  panel off, otherwise the backdrop would stay on screen.
- `Raycast Target` is off on the panel's Image.
- Play test by code: the panel arrives with its left edge at x = 0, `ShowCustomerInset(null)` turns the panel and camera off,
  showing again restores both, and Cancel during play slides to -1301.4 and turns the camera off.
- Not tested: Stirring, Remake and Cancel during the Intro were not repeated after this change. The missing-customer warning
  was not repeated either, because `CameraManager.ShowCustomerInset(null)` was called directly.
- The panel's green and the RawImage's red tint were left as the owner set them.

### Panel width 652.8 (2026-10-31, owner's size change)
- The stage clips now move `Panel_CharacterView` between X -1286.4 (hidden) and -633.6 (rest).
- `RawImage_CharacterCameraView` now stretches to fill the panel, so its old 30 px overflow is gone.
  `RT_CharacterCameraView` is now 653x1080.
- Play test by code, Shaking and Stirring: at rest the panel spans 0 to 652.8 and Art002 starts at 652.8. After the Outro
  the panel's right edge is at 0. Screenshots of Shaking show no gap at rest and no strip when hidden.
- Not tested: the slide by eye, and the full flow through `FlowWiring` and Yarn.
