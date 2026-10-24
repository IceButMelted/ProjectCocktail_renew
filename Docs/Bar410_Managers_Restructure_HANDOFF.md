# Bar410 · Managers Restructure · HANDOFF

เขียนเมื่อ 2026-10-13 หลังจบ **Stage 9** · branch `GameFlow/Managers` (แตกจาก `GameFlow/Main` ที่ `1962402`, **ยังไม่ push, ไม่มี PR**)

> **อัปเดต 2026-10-24:** branch push แล้ว (ยังไม่มี PR) · user เล่น Day 1 ผ่าน → §8 ข้อ 1 และ 10 ปิด · ภาพรวมล่าสุด `Docs/Bar410_Status_Report_2026-10-24.html`
>
> **คำตอบ user 2026-10-24 (รายงาน §6 ข้อ A):** 1) สีเครื่องดื่มที่ไม่ตรงสูตรย้ายไป `UnmatchedDrinkColor.asset` (`SO_UnmatchedDrinkColor`, `db426b8`) · 2) dialogue ที่รอเสิร์ฟจริงและกล้องตอนคุยหลัง 7d **ตรงกับที่ writer ตั้งใจ** (คง `e7bc9ad`) · 3) เปิด PR เข้า `GameFlow/Main` · 4) ไฟล์ค้างแยก commit ตามเรื่อง (ทำแล้ว)
>
> **เรื่องที่ต้องรู้ก่อนทำอะไรต่อ (สั้นที่สุด)**
> 1. Stage 0–9 เสร็จและ commit แล้วทั้งหมด งานที่เหลือมีแค่ Stage 10 (optional) และการตัดสินใจเรื่อง push / PR
> 2. **Stage 7d (`e7bc9ad`) เปลี่ยนสิ่งที่ผู้เล่นเห็น**: `.yarn` เรียก `SystemGame` ซึ่งไม่มีใน scene แล้ว ทุก command ของ cocktail เคยล้ม ตอนนี้ทำงาน (dialogue รอการเสิร์ฟจริง) **user ยังไม่ได้เล่นจริง** (§8 ข้อ 1) revert ได้ด้วย commit เดียว
> 3. ตัวตรวจ `flow_trace` เรียก `onClick.Invoke()` ซึ่งไม่ดู `interactable` เคยพลาด regression ของ 4b จึงมี lever ที่กดจริง (§6.1) ใช้ให้ตรงกับสิ่งที่แก้

เอกสารนี้ให้คนที่รับงานต่อ (หรือ session ใหม่) ทำงานต่อได้โดยไม่ต้องอ่าน chat เดิม อ่านตามลำดับ §1 → §2 → §6 แล้วเริ่มที่ §7

เอกสารที่เกี่ยวข้อง
- `Docs/Bar410_Managers_Restructure_Plan.md` แผนเต็ม, สถาปัตยกรรมเป้าหมาย, single source of truth, **decision log (§8)**
- `Docs/Bar410_GameLoop_Decoupling_Plan.md` audit ต้นทาง (§1–§3 ยังใช้ได้, §5 ถูกแทนที่)
- `Docs/baseline/managers/README.md` วิธีเช็กหลังแต่ละ stage และ stage log

---

## 1. สถานะโดยสรุป

| | |
|---|---|
| เป้าหมาย | แยก `[GameLoop]` เป็น Manager อิสระต่อกัน (Cocktail, Minigame, Garnish, TalkToCustomer, Camera, Placement) แต่ละตัวมี Canvas ของตัวเอง ข้อมูลที่ใช้ร่วมกันมีเจ้าของเดียว FSM คุมแค่ Level 2–3 |
| ทำเสร็จ (commit แล้ว) | Stage 0, 1, 3, 4a, 4b, 4c, 4d, 4f, **5**, **6**, **7**, **8**, **9** (9a–9c) |
| ข้าม | Stage 2 (`FlowWiring` ว่างเปล่าในตอนนั้น; มันถูกสร้างจริงใน 9b) |
| เหลือ | Stage 10 (optional, ไม่จำเป็นต่อเป้าหมาย), push / PR (รอ user) |
| working tree | สะอาด ยกเว้น `ProjectSettings/Packages/dev.yarnspinner/Assembly-CSharp-generated.ysls.json` ซึ่ง Yarn สร้างเอง **ไม่ใช่งานของเรา ไม่ต้อง stage** |
| ตรวจล่าสุด | หลัง 9c: flow trace, lock fingerprint, `serve_commit` (หลัง 9b), `back_button` (หลัง 9b), `method_buttons_click`, `customer_yarn`, `shell_ui` เหมือน baseline ทุกไบต์; `camera_yarn` เหมือน Stage 8; Yarn contract exit 0; console error 0 ไม่ได้รัน `garnish_ui`/`dialogue_wait`/`placement_drag` ซ้ำใน Stage 9 (ไม่ได้แตะ) |

ตัวเลขเทียบ (`wiring_dump.snippet.cs`, scene `New Drag Drop System`, นับใต้ `[GameLoop]`)

| | Stage 0 | หลัง 4b | หลัง 5 | หลัง 6 | หลัง 7 | หลัง 8 | หลัง 9 |
|---|---|---|---|---|---|---|---|
| components | 933 | 916 | 885 | 891 | 891 | 891 | 888 |
| MonoBehaviour | 410 | 402 | 386 | 389 | 389 | 389 | 386 |
| scene ref (ไม่นับ UI built-in) | 246 | 215 | 211 | 207 | 208 | 208 | 185 |
| **ref ข้าม child** | **76** | 65 | 61 | 40 | **41** | **40** | **17** |
| ref ใน child เดียวกัน | 170 | 150 | 150 | 167 | 167 | 168 | 168 |
| Inspector UnityEvent call | 28 | 19 | 19 | 19 | 19 | 19 | 19 |
| ref ออกนอก prefab | 1 | 1 | 1 | 1 | 1 (ย้ายจาก Cocktail ไป bridge) | 1 | 1 |
| script บน `[GameLoop]` root | 10 | 8 | 8 | 8 | 8 | 8 | **3** |

ตอนจบ Stage 9: ไม่มี bridge เหลือ (`FlowWiring` ตัวเดียว) เดิม 6 bridge serialize ตัวจัดการ/object ที่ใช้ร่วมรวม ~38 slot (`_cocktail` ×4, `_ingredients` ×4, `_bookUI` ×3, `_cameras` ×3 ...) ตอนนี้ serialize ครั้งเดียว slot จาก root ไป child 36 → 13 ไฟล์: `wiring_dump.txt` (Stage 0), `wiring_dump.after_stage{4b,5,6,7,8,9}.txt`
ตัวเลขของ Stage 7 ฝั่ง NPC วัดจากต้นไม้ `[GameLoop]` ไม่ได้ (NPC อยู่นอกต้นไม้) ดูผลจริงที่ `customer_yarn`

---

## 2. Commit ที่ทำไปแล้ว (เรียงตามเวลา)

| commit | Stage | ทำอะไร | ทำไม |
|---|---|---|---|
| `0b6b3f7` | 0 | แผน + baseline (wiring dump, Yarn contract, flow trace, screenshot) | จุดเทียบของทุก stage |
| `fe06935` | 1 | `GameLoopFSM` build ใน `Awake` + `[DefaultExecutionOrder(-100)]` ลบ `EnsureBuilt()` 6 จุด; `flow_*` Yarn command ข้ามเมื่อ `SceneLoaderBridge.IsSilentReplay`; hotkey ไม่ชน (minigame ใช้ Shift+1/2/B/V); ย้าย `GameFlowDebugHotkeys` ไป `[Debug]`; ลบ `Demo_KeysShortCut` บน `[GameLoop]` (listener target null); แก้ comment เก่า | ลด hack, กัน `flow_prepare_drinks` ดัน FSM ตอน silent replay |
| `dbbef25` | 3 | `N_PlacementSystem.Current` (scene-scoped) + `.InputManager` แทน `public static DragableObject._placementSystem` และ Find ใน `IngredientHoverDetector` | ลบ global ที่ใครก็เขียนได้ |
| `b794bdf` | 4f | ให้คะแนนที่เดียว: `CocktailFlowBridge` ฟัง `OpenBar.Sub.OnTransition` (Serve → Talking) เรียก `ServeDrink()` ซึ่ง idempotent ด้วย `IsScored`; ปุ่ม Serve เรียกแค่ `ServeDone()`; ลบ fallback ใน `OnServeExited` | `ServeDone()` ตรงๆ (`<<flow_serve_done>>`, hotkey 7) ให้ `IsScored` แต่ไม่เขียน `$task_done`/`$satisfaction` ลง Yarn และ fallback เดิมทำงานตอน remake ด้วย |
| `f5ca4c9` | 4d | `ShakerContents` Inspector UnityEvent 9 call (Changed 2, Cleared 3, OnFulled 4) → listener ในโค้ดที่ `CocktailFlowBridge`; `IsCocktailFull` ย้ายเข้า `ShakerContents.RaiseChanged()` | logic ซ่อนใน Inspector, อยู่นอก HSM |
| `7bfdba4` | docs | บันทึกหลักฐานว่า screen-space `VisualizeCocktail` ไม่เคยถูกเปิด | |
| `91ef84e` | 4c-Vis | **ลบ** `Canvas/Panel - VisurlCocktail` (screen-space) + `_panelVisualCocktail` (decision D10) | ไม่มีโค้ด/event ใดเปิดมัน; ผู้เล่นเห็นแต่ตัว world-space |
| `c4865fe` | 4a | `IngredientButtonGroup` เก็บขวดจาก `IngredientButtonUI` ใต้ตัวเอง (list ใน scene 20 → 2: shaker, ปุ่ม book) + `BindShaker` จาก `CocktailSystemManager.Awake` | ขวดถือ `ShakerContents` ตั้งแต่เริ่ม 3 → 19 จาก 19 |
| `3fe82b2` | 4b | `MethodPanel` (component ใหม่) เป็นเจ้าของปุ่ม Shake/Stir/Reset ส่ง event; bridge เหลือ 1 slot (เดิม 4); ย้าย panel ไป `[CocktailSystem]/Canvas_Cocktail`; `Canvas_Cocktail` CanvasScaler match 0 → 0.5; prefab เก่าได้ component เดียวกัน | panel ดูแลตัวเอง |
| `e61fc99` | 4b-fix | ลบ `IngredientButtonUI` (`action=None`, ไม่มี call) ออกจาก `BTN_Shaking`/`BTN_Mixing` ใน scene + prefab; `IngredientButtonGroup.Members` กลับเป็น `GetComponentsInChildren<IngredientButtonUI>` ล้วน (ไม่มี special-case) | 4b ย้าย panel เข้าใต้ `[CocktailSystem]` สองปุ่มนี้เลยกลายเป็นสมาชิกของกลุ่มที่ `OnFulled` lock → ผู้เล่นกด Shake/Stir ตอน drink เต็มไม่ได้ trace มองไม่เห็นเพราะเรียก `onClick.Invoke()` |
| `e61fc99` | 5 | `MinigameSystemManager` ถือปุ่ม back ×2 + `Resolve()` + `_defaultType` (ส่ง `BackRequested`); bridge เหลือ slot `_gameLoop`/`_commands`/`_minigames`/`_shakerContents`; ลบ `OnStartedMinigame`/`OnEndedGame`; **ลบ `BG_BTN` ทั้งกลุ่ม** (user สั่ง) จึงไม่มี give-up/cancel-direct/F6 อีก | ปุ่ม cancel อยู่ใน bridge ที่ root; ปุ่ม dev ที่ปิดไว้และตายทั้งกลุ่ม |
| `6621bd2` | 6a | สร้าง `[GarnishSystem]/Canvas_Garnish` (scaler เท่า `Canvas_Cocktail`) ย้าย `Panel - Granish UI`, `Panel - Serve`, `Garnish_Plate` (พร้อม `GlassPlacementZone`, `GranishCam`, `Ref - Panel Serve`) เข้าไป ไม่เปลี่ยนโค้ด; ตัวตรวจใหม่ `garnish_ui`; `flow_trace`/`serve_commit` หา panel ด้วยชื่อ | D2/D9 — Garnish มี canvas และ root ของตัวเอง |
| `754daee` | 6b | `GarnishManager` (ใหม่) ถือ glass/ice/ตกแต่ง/pour + ปุ่ม tab ของ canvas ตัวเอง ส่ง event; `GarnishFlowBridge` 20 → 7 slot; grid ×3 หา manager ด้วย `GetComponentInParent`, glass grid ฟัง `Opened`/`PourStarted` (ตัดวงกลม bridge ↔ grid); `IceToggled` → bridge → `ShakerContents.SetIce` | bridge ถือ UI + logic ทั้งหมด, 20 slot, lambda ไม่ unsubscribe |
| `eb63d71` | 6c | Serve panel + 2 ปุ่มเป็นของ `GarnishManager` (`OpenServe`/`CloseServe`, `ServeRequested`/`RemakeRequested`); `ServeFlowBridge` เหลือฝั่ง flow; ลบ `CocktailFlowBridge._panelServe` และ `ShakerPanelController._serveUI` กับ flag serve | panel เดียวมี 3 writer |
| `e5601e2` | 6d | glass เป็นของ `GarnishManager` (`Glass`, `ClearGlass()`); ลบ `PlacedGlassInstance.Current`/`Spawn`/`DestroyCurrent` (static); การเคลียร์ glass ย้ายจาก `CocktailFlowBridge` ไป `GarnishFlowBridge` | static ที่ใครก็เข้าถึง glass ได้ |
| `be40a89` | 7a | ตัวตรวจ `customer_yarn` (Yarn dispatcher จริง, save → load ของ NPC, การเขียนตัวแปร Yarn) บันทึก baseline ไม่แก้โค้ด; พบ 3 อย่างที่พังอยู่แล้ว | ไม่มีตัวตรวจ Yarn/save ก่อนหน้านี้ |
| `01e7e2d` | 7b | `TalkToCustomerManager` (ใหม่, บน `NPCs_And_Set`) ถือ NPC lookup + Yarn command `SetEmotion`/`SetLookDirection`/`MoveIn`/`MoveOut` (static, ชื่อเดิม, ผ่าน `Current`); `NPC_Base` เหลือพฤติกรรมของตัวเอง; `SaveLoadManager` ใช้ `Snapshot()`/`Restore()` | static `_registry` + save แตะ property ตรง |
| `a931465` | 7c | manager เป็นผู้เขียนตัวแปร Yarn คนเดียว ถือ `DialogueRunner`/roster/`CharacterData`; Cocktail ส่ง `OrderScored`/`OrderCleared` + รับ `UsePreferences`; `TalkingWithCustomerBridge` ต่อสองฝั่ง | Cocktail ถือ DialogueRunner + roster + `CharacterData` (ซึ่ง `GetComponent` ได้ null) |
| `e7bc9ad` | 7d | `wait_for_task`/`Enable_InteractableObject`/`Reset_Variable`/`Can_End_Shift` เป็น static ไม่สนชื่อ object (ชื่อ command เดิม) | `.yarn` เรียก `SystemGame` ซึ่งไม่มีใน scene แล้ว ทุกคำสั่งล้ม `wait_for_task` ไม่เคยรอ |
| `6a92f8a` | 8a | lever `camera_yarn` + baseline (ไม่แก้โค้ด) | ไม่มีตัวตรวจคำสั่งกล้องของ Yarn; baseline แสดงว่าเปลี่ยนชื่อ `CamController` แล้วทุกคำสั่งล้ม |
| `89b76e8` | 8b | `CinimachineCameraSwitcher` → `CameraManager` (GUID เดิม) เก็บ `CameraEntry` เอง, shot ชื่อ `ShowBar`/`ShowShaker`/`ShowGarnish`, ถือ look (`SetLookFixed`/`ResetLook`); `Switch_Camera`/`Cut_Camera`/`Fix_Camera`/`Reset_Camera` static ชื่อเดิม; 3 bridge เหลือ slot `_cameras`; sync prefab เก่า | id กล้อง 3 ที่ + instance command ผูกชื่อ object (กับดักเดียวกับ 7d) |
| `9f8278f` | 8c | ลบ `MinigameSystemManager._cocktailCamera` + `IMinigameContext.ResetCamera()` + call ใน `BaseMiniGame` | null ทั้ง scene และ prefab ไม่เคยทำอะไร; มุมมองถูก reset ตอนเข้า step 2 อยู่แล้ว |
| `4ae4329` | 9a-0 | lever `shell_ui` + baseline (ไม่แก้โค้ด) | ไม่มีตัวตรวจปุ่ม shell; พบว่าปุ่ม toggle ของ post-it ไม่ interactable |
| `2d4f4b2` | 9a | ลบ `UIFlowBridge`: `BookUI_V2`/`Post_It_Order` ผูกปุ่มเอง, `LoadSceneButton` ×2, ปุ่มเปิด book ที่ `CocktailFlowBridge`; `Canvas` → `Canvas_Shell` | panel ผูกปุ่มของตัวเอง (กฎ 4) |
| `793b54b` | 9b | `FlowWiring` ใหม่ รับ Minigame + Garnish + Serve bridge | bridge 3 ตัวถือ slot ซ้ำ + `_gameLoop`/`_commands` ซ้ำ |
| `e295690` | 9c | `FlowWiring` รับ Cocktail + Talking bridge; ไม่มี bridge เหลือ | กฎ step → UI อยู่ในไฟล์เดียว (D12) |
| `c3cc94b` `29aa520` `7a34927` `66cf0a3` | docs | บันทึก decision (D9, D11), HANDOFF และ baseline log ของ Stage 6/7 | |

---

## 3. สิ่งที่ตัดสินใจ**ต่างจากแผนเดิม** (และเหตุผล)

| แผนเดิม | ที่ทำจริง | เหตุผล |
|---|---|---|
| Stage 2 สร้าง `FlowWiring` ว่าง | ข้าม | class ว่างไม่มีอะไรให้เทียบ baseline; bridge เดิมทำหน้าที่ seam ได้ จะรวมเป็นไฟล์เดียวตอน Stage 9 |
| Stage 3 รวม `N_PlacementSystem` + `N_InputManager` | ไม่รวม ทำ `Current` แทน | อยู่ GameObject เดียวและ ref กันเองอยู่แล้ว การรวมแก้ข้อมูล serialized โดยไม่ลด wiring และ trace จับไม่ครบ ตัวเลข "ลบ Find ×14+1" ผิด (static ตัวเดียว, Find เกิดครั้งแรกครั้งเดียว) |
| 4f ให้คะแนนตอน `Serve.Exited` | ให้คะแนนตอน Serve → Talking | `Exited` ยิงตอน remake ด้วย จะเขียน relationship ผิด |
| 4a เปลี่ยนชื่อเป็น `IngredientShelf`, writer เดียว, รวม API | คงชื่อ `IngredientButtonGroup`; ไม่รวม `SetInteractable` กับ `EnableInteractablePrepareDrinksPhase` | rename ไม่ลด wiring; สองตัวต่างกันที่ `DragableFruitTraySlot` hijack (`Apply` ไม่เปิด, `ApplyPrepareDrinksPhase` เปิด) รวมแล้วพฤติกรรมเปลี่ยน — รอ presenter Stage 8 |
| 4c ตรวจ `VisualizeCocktail` แล้วคงไว้ทั้งสอง (D8) | ลบ screen-space (D10) | user ตัดสินหลังเห็นหลักฐาน (2 ภาพใน `screens_baseline/visualize_*.jpg`) |
| 4e ice API ใน Stage 4 | ย้ายไป Stage 6 | ผู้เรียกคือ Garnish |
| baseline trace มีฟิลด์ `visScreen=` | ถอดออกจาก snippet + ไฟล์ baseline | object ไม่มีแล้ว |
| ตั้ง `Canvas_Cocktail` CanvasScaler | match 0.5 (เดิม 0) | ให้เท่า `Canvas` เดิม ไม่งั้น Method panel/post-it สเกลต่างเมื่อจอไม่ใช่ 16:9 ถ้า user ตั้ง 0 ไว้จริงให้คืน |
| Stage 5 ย้าย `PendingType`/`LastOutcome` เข้า manager | ไม่ย้าย | เป็น state ระดับ flow ที่ Yarn อ่าน (`flow_minigame_type`/`flow_minigame_result`) และ `LastOutcome` ถูกล้างทุกครั้งที่เข้า 2.2 การย้ายต้องให้ `GameFlowCommands` ถือ ref ไป manager ซึ่งข้ามชั้น; ย้ายเฉพาะส่วนที่เป็นของ manager จริง (ปุ่ม, ลำดับเลือก, default) |
| Stage 5 ลบ `_cocktailCamera`/`ResetCamera()` | เลื่อนไป Stage 8 | null ทั้ง scene และ prefab (no-op) แต่เป็น seam ที่ออกแบบไว้ "snap กล้องตอนเริ่มเกม" เป็นเรื่องของ Camera manager ให้ตัดสินตอนนั้นว่าต่อหรือลบ |
| Stage 5 แก้ F6 ให้ Give-up/Cancel-direct ออกผ่าน HSM | ลบปุ่มทั้งคู่ (`BG_BTN`) | หลักฐานใน `f6_giveup.txt`: ค้างที่ 2.2 แล้ว `IngredientAdded()` ถูกปฏิเสธ ปุ่มอยู่ใน `BG_BTN` ที่ปิดไว้และไม่มีอะไรเปิด ผมแก้ให้ออกผ่าน HSM ก่อน (ผ่าน `f6_giveup.after_stage5.txt`) แล้ว user สั่งลบทั้งกลุ่ม จึงถอดโค้ด give-up ออกด้วย |
| 6 ice: Garnish ส่ง `IceToggled` → `Cocktail.SetIce` → `IceChanged` กลับ | `IceToggled` → bridge → `ShakerContents.SetIce` ไม่มี echo | writer ของ ice มีทางเดียว (Garnish) และ icon ของ Garnish เป็นสถานะของตัวเองที่รีเซ็ตตอน `Close()` echo จะไม่ได้ทำอะไร (YAGNI) flag ยังอยู่ที่ `CurrentCocktail.AddIce` (D5) |
| 6 `GarnishChoice` (plain C#: glass + item + rim) | ไม่ทำ | ไม่มี consumer: ไม่มีอะไรอ่านสามอย่างนี้รวมกัน (scoring ไม่ใช้) ทำตอนมีผู้ใช้จริง |
| 6 Serve ส่ง `ServeClicked` ให้ FlowWiring | `ServeRequested` ไป `ServeFlowBridge` | FlowWiring ยังไม่มี (Stage 2 ข้าม) bridge ทำหน้าที่เดียวกัน จะรวมตอน Stage 9 |
| 6 `Canvas_Garnish` ลำดับวาด | อยู่ก่อน `Canvas` (sortingOrder 0 เท่ากัน) เหมือน `Canvas_Cocktail` | Hub/BookUI/Transition ที่อยู่ใน `Canvas` จึงวาดทับ Garnish UI (ก่อนย้าย Garnish UI อยู่เหนือ Hub) ภาพ garnish step เทียบด้วยตาแล้วเหมือนเดิม ถ้า Hub ทับ panel ในอนาคตให้ปรับ sortingOrder |
| 7 `Cocktail.UsePreferences(...)` ผ่าน FlowWiring | ผ่าน `TalkingWithCustomerBridge` | FlowWiring ยังไม่มี bridge ทำหน้าที่เดียวกันและเป็น seam ของ "ลูกค้า" อยู่แล้ว |
| 7 `CharacterData` (legacy) ย้ายหรือลบ | คงไว้บน `NPCs_And_Set` เป็น fallback ของ roster | roster ถูกต่อไว้ จึงไม่ถูกใช้จริง แต่ scene ยังมีข้อมูลอยู่ ตัดสินตอน Stage 9 |
| 7 look direction / emotion ใน save | ไม่แก้ | `_currentLookDirection` ไม่เคยถูกเซ็ตโดย `LookDirection()` (บันทึกเป็น `None` เสมอ) และ emotion ไม่ถูกบันทึกเลย เป็นบั๊กเดิมที่ `customer_yarn` (B2) บันทึกไว้ ต้องให้ user ตัดสินว่าจะให้ save เก็บอะไร |
| 7 `YarnVariableSync` | คงเป็น plain class ถือโดย manager | ไฟล์เดิม ไม่ต้องแตะ แค่เปลี่ยนเจ้าของ |
| 7d (นอกแผน) แก้ command ที่ target ชื่อ `SystemGame` | static + ไม่สนชื่อ | พบจาก `customer_yarn`: Yarn หา target ด้วย `GameObject.Find` object ถูกเปลี่ยนชื่อเป็น `[CocktailSystem]` ทุก command ล้ม **เปลี่ยนพฤติกรรมที่ผู้เล่นเห็น** (dialogue รอการเสิร์ฟจริง และ dialogue เริ่มวิ่งเลย `Enable_InteractableObject` บรรทัดแรก) แยก commit เดียวเพื่อ revert ได้ |
| Stage 5 เลื่อน `_cocktailCamera`/`ResetCamera()` มาตัดสินที่ Stage 8 | **ลบ** (8c) | no-op มาตลอด; ต่อกับ manager จะเปลี่ยนพฤติกรรม (reset มุมมองตอนเริ่ม minigame) ซึ่งไม่มีใครเคยเห็น และ step 2 reset ให้อยู่แล้ว |
| Stage 8 `FlowStep` flat enum + presenter (กล้อง, ingredient policy, book, post-it) | **ไม่ทำ** กฎไปรวมที่ `FlowWiring` ใน Stage 9 (D12) | presenter ต้องรู้ FSM เหมือน bridge จึงเป็น bridge ตัวที่ 6; กฎมีแค่ ~15 บรรทัด; Stage 9 ต้องรวม bridge อยู่แล้ว; ผลต่อผู้เล่นเป็นศูนย์ ถ้า user อยากได้ enum ให้บอก |
| Stage 8 ส่ง `Camera` ให้ Placement ผ่าน Camera manager | คง `N_InputManager._sceneCamera` เป็น ref | ต้องมีใครสักคนเรียก `SetCamera` (wiring เพิ่ม) เพื่อแลกกับ ref ตัวเดียวที่ไม่เปลี่ยนชื่อได้ (D12) |
| Stage 8 `CameraManager` เป็น class ใหม่ + รวม `CameraController` | เปลี่ยนชื่อ `CinimachineCameraSwitcher` (GUID เดิม) ให้ถือ `CameraController` | `CameraController` เป็นพฤติกรรมเมาส์ล้วน (381 บรรทัด) รวมไฟล์เดียวเปลี่ยน diff เยอะโดยไม่ลด wiring; GUID เดิมทำให้ scene/prefab เก็บค่าที่ serialize ไว้ |
| Stage 9 เริ่มด้วย `LoadSceneButton` บน EndShift/Thanks (ตามที่ HANDOFF เดิมเขียนว่ามี) | สร้างใหม่ (ไม่เคยมีมาก่อน) | HANDOFF/plan เขียนเหมือนมีอยู่แล้ว `SceneLoader` มีแค่ `LoadScene(string)` ปุ่มสองปุ่มถูกผูกจาก `UIFlowBridge` |
| Stage 9 ทุก panel ผูกปุ่มเอง (รวมปุ่มเปิด book) | ปุ่มเปิด book ผูกที่ `FlowWiring` | `Panel - BookUI` ปิดตัวเองใน `Awake` และปุ่มเปิดอยู่ใน Hub นอก panel จึงฟังเองไม่ได้ ส่วน next/previous/close เป็นลูกของ panel ผูกเองได้ |
| Stage 9 รวม bridge ทีละตัว (6 commit) | รวม 5 ตัวเป็น 2 commit (9b, 9c) | แต่ละตัวมี verify ตรงกัน (flow trace + lever เฉพาะจุด) และ `FlowWiring` ต้องมี slot ร่วม จึงแยกเป็น 6 ไม่ได้ผลเพิ่ม |
| Stage 9 `CharacterData` ตัดสินว่าจะลบไหม | คงไว้ (D6 ที่ user ยืนยันไว้) | ไม่มีใครใช้นอกจาก fallback ของ roster; user ตัดสิน D6 ไว้แล้ว |
| Stage 9 `_autoAdvanceOnWin` / `_driveIngredientButtons` (Inspector toggle) | ลบ | เป็น `true` ทั้งใน scene และ prefab; ปิดได้ก็ทำให้ flow ค้าง (tooltip เดิมเขียนว่า) |

---

## 4. Decision log (ย่อ — ฉบับเต็มใน plan §8)

| # | ตัดสินใจ | สถานะ |
|---|---|---|
| D1 | FSM = Level 2–3; Level 1 ให้ Yarn คุม | ยืนยัน ส่วน Level 1 **แทนที่ด้วย D11** |
| D2 | Serve panel อยู่ `Canvas_Garnish` | ยืนยัน |
| D3 | Camera เป็น Manager แยก (ไม่มี canvas) | ยืนยัน |
| D4 | Yarn variable writer + `DialogueRunner` ref ไป TalkToCustomer | ยืนยัน |
| D5 | **Ice อยู่ที่ Cocktail** (เป็นเงื่อนไขตอนเสิร์ฟ) ไม่ใช่ Garnish | ยืนยัน |
| D6 | `CharacterData` + roster อยู่ TalkToCustomer | ยืนยัน |
| D7 | `PlacementSystem` คงไว้เป็น Manager แยก | ยืนยัน (ทำแบบ `Current`) |
| D8 | `Canvas/Panel - VisurlCocktail` อยู่กับ Cocktail | **แทนที่ด้วย D10** |
| D9 | `GlassPlacementZone` อยู่กับ Garnish (คงไว้ ไม่ลบ) | ยืนยัน 2026-10-13 |
| D10 | ลบ screen-space `VisualizeCocktail` เหลือ world-space ตัวเดียว | ยืนยัน |
| D11 | Level 1 (Open ⇄ Close) ขับด้วยปุ่ม/hotkey ตามเดิม ไม่เพิ่ม `flow_close_bar`/`flow_next_day` ใน `.yarn` | ยืนยัน 2026-10-13 ไม่มีงานแก้ L1 |
| D12 | ไม่สร้าง presenter/`FlowStep` ใน Stage 8 (กฎ step → UI ไปที่ `FlowWiring` ตอน Stage 9); `N_InputManager._sceneCamera` คงเป็น ref | **ค่า default ของ Claude รอ user ยืนยัน** (ดู §3) |
| D13 | Stage 9: bridge ทั้งหมดรวมเป็น `FlowWiring`, panel ผูกปุ่มเอง, `LoadSceneButton`, `Canvas` → `Canvas_Shell` | ค่า default ของ Claude ตามแผน (ดู §3) |

ค่า default ที่ยังไม่ยืนยัน: Shell (Hub, EndShift, Thx, Transition) เป็น `Canvas_Shell` ไม่มี manager; Placement เข้าถึงผ่าน `Current`; `RecipeBook` หายไปจาก scene แล้ว (ไม่ต้องตัดสิน)

---

## 5. สถาปัตยกรรมเป้าหมาย (ย่อ)

```
[GameLoop]
 ├ Flow            GameLoopFSM (L2-3), GameFlowCommands, (FlowWiring ตอน Stage 9)
 ├ Cocktail        CocktailSystemManager + Canvas_Cocktail  (post-it, Method panel อยู่แล้ว; book รอ Stage 8)
 ├ Minigame        MinigameSystemManager + Canvas_MiniGame
 ├ Garnish         GarnishManager + Canvas_Garnish          (+ Serve panel)
 ├ Customer        TalkToCustomerManager (NPCs_And_Set + CharacterData + roster + YarnVariableSync)
 ├ Camera          CameraManager
 ├ Placement       N_PlacementSystem + N_InputManager (Current)
 └ Canvas_Shell    Hub, EndShift, Thx, Transition
```

กฎ: Manager ไม่รู้จัก FSM และไม่รู้จักกัน (verb เข้า, event ออก); ข้อมูลร่วมมีเจ้าของเดียว คนอื่นรับเป็น argument; ชื่อ `[YarnCommand]`/`[YarnFunction]` ห้ามเปลี่ยน; singleton เฉพาะ service ข้าม scene + static ที่ Yarn ต้องการ (+ `N_PlacementSystem.Current`)

---

## 6. วิธีทำงานต่อ (ต้องทำตามนี้ทุก stage)

**Principle:** 1 stage (หรือ 1 commit ย่อย) = 1 commit, ตรวจกับ baseline ก่อน commit, ไม่รวมหลายอย่างในคอมมิตเดียว

### 6.1 ตัวตรวจ (อยู่ใน `Docs/baseline/managers/`)

| ตัวตรวจ | ไฟล์ | ครอบคลุม | วิธีรัน |
|---|---|---|---|
| Yarn contract | `check_yarn_contract.py` | 42 ชื่อที่ลงทะเบียนต้องไม่หาย; command ที่ `.yarn` ใช้ต้องมี | `python Docs/baseline/managers/check_yarn_contract.py --check` ต้อง exit 0 |
| flow trace | `flow_trace.snippet.cs` → `flow_trace.txt` | 15 step ผ่าน HSM + ปุ่มจริง: ชนะ minigame, ยกเลิก, remake จาก Garnish/Serve, Serve, close bar, next day | Play ใหม่ → วาง snippet ใน `execute_code` → รอบรรทัด `DONE` → `git diff Docs/baseline/managers/flow_trace.txt` ต้องว่าง |
| serve commit | `serve_commit.snippet.cs` → `serve_commit.txt` | ออกจาก Serve 3 ทาง แล้วดู `$task_done`/`$satisfaction` | เหมือนข้างบน |
| placement drag | `placement_drag.snippet.cs` → `placement_drag.txt` | ลากขวด 2 ตัวผ่าน `PointerEventData` + `IngredientHoverDetector` | เหมือนข้างบน |
| lock fingerprint | `lock_fingerprint.snippet.cs` → `lock_fingerprint.txt` | สถานะ lock ของ 19 object (ขวด 17, shaker, ปุ่ม book) × 9 จุด (ครบทุก writer) | เหมือนข้างบน |
| wiring dump | `wiring_dump.snippet.cs` | จำนวน ref/UnityEvent/ข้าม child | รันใน Edit mode, ตั้ง `tag` ให้ชื่อไฟล์ |
| F6 give-up / cancel-direct (**obsolete**: `BG_BTN` ถูกลบแล้ว snippet รันไม่ได้ เก็บเป็นหลักฐาน) | `f6_giveup.snippet.cs` → `f6_giveup.after_stage5.txt` (`f6_giveup.txt` = ก่อนแก้) | ปุ่ม `BG_BTN/BTN_Reset`/`BTN_Next` ใน 2.2 แล้วลอง `IngredientAdded()` ต่อ | เหมือนข้างบน |
| minigame type resolution | `resolve_priority.snippet.cs` → `resolve_priority.txt` | default / method ของ drink / เลือกเอง (trace อื่นทุกตัวเรียก `SelectShaking` ก่อนจึงไม่ครอบคลุม) | เหมือนข้างบน |
| ปุ่ม back ของ minigame | `back_button.snippet.cs` → `back_button.txt` | กดปุ่ม `BTN - Cancel Minigame` จริง → 2.1 (drink ถูกเก็บ, shaker ปลด lock) → เข้า 2.2 ซ้ำได้ | เหมือนข้างบน |
| กดปุ่ม Method จริง | `method_buttons_click.snippet.cs` → `method_buttons_click.txt` | Shake/Stir ด้วย pointer click ผ่าน EventSystem ตอน drink เต็ม (ดู `interactable`) | เหมือนข้างบน; ต้องรันเมื่อย้าย object ใต้ `[CocktailSystem]` |
| Garnish UI | `garnish_ui.snippet.cs` → `garnish_ui.txt` | tab, glass swap, ice round trip, slot/item/rim, finish/pour gating, remove ice/swap glass หลัง pour, Reset, default ตอนเข้าใหม่, ปุ่ม Serve และตำแหน่ง ด้วย pointer click จริง หา object ด้วยชื่อ | เหมือนข้างบน; `diff` กับ `garnish_ui.txt` ต้องเหมือนทุกไบต์ |
| customer / Yarn | `customer_yarn.snippet.cs` → `customer_yarn.txt` | คำสั่ง NPC และคำสั่ง cocktail ผ่าน dispatcher จริง, save → load ของ NPC, ตัวแปร Yarn ตอนให้คะแนน (ตั้ง NPC กลับจุดเริ่มก่อน เพราะ chapter dialogue วิ่งก่อนที่ snippet จะหยุดมัน) | เหมือนข้างบน |
| dialogue จริง | `dialogue_wait.snippet.cs` → `dialogue_wait.txt` | node `D1_02_Walter_Order` จอดรอ `wait_for_task` จนให้คะแนน แล้วเดินต่อ | เหมือนข้างบน |
| คำสั่งกล้องของ Yarn | `camera_yarn.snippet.cs` → `camera_yarn.txt` (ก่อน 8b) / `camera_yarn.after_stage8.txt` (หลัง) | ผ่าน dispatcher จริง: ปกติ, เปลี่ยนชื่อ `CamController`, และสิ่งที่ flow ทำกับกล้อง/มุมมอง; ตั้ง `Fix_Camera true` กลับก่อนช่วงที่ไม่ได้ดู เพราะมุมมองที่ไม่ fixed ตามเมาส์จริง | เหมือนข้างบน ค่าที่ต้องเทียบคือ `cam=`/`fixed=`/`dir=` ต่อบรรทัด |
| ปุ่ม shell (book, post-it, โหลด scene) | `shell_ui.snippet.cs` → `shell_ui.{endshift,thanks}.txt` | pointer click จริงที่ book next/previous/close, post-it, และปุ่มโหลด scene (อ่าน `SceneLoaderBridge.TargetScene` แล้วปิดไฟล์ก่อน Unity ออก scene) | ตั้ง `which` เป็น `endshift` แล้ว `thanks` ใน Play ใหม่ทั้งสองครั้ง; ผลต้องเหมือนกันทุกบรรทัด |

ทุก snippet รันเป็น body ของ `mcp__unityMCP__execute_code` ใน Play mode **เริ่มจาก Play session ใหม่** (stop → play) และทุกตัวหยุด chapter dialogue ของฉากก่อน

ถ้า stage แตะ**ส่วนที่ trace ไม่ครอบคลุม** ให้สร้างตัวตรวจใหม่ก่อนแก้ แล้วรันทั้งก่อน/หลัง (ทำแบบนี้มาแล้วใน 3, 4f, 4a, 4b) — ช่องว่างที่รู้ตอนนี้: Yarn dialogue จริง, save → load / silent replay, NPC `MoveIn/MoveOut/SetEmotion`, fruit tray drag, mouse จริง

### 6.2 ขั้นตอนต่อ stage
1. อ่านแผน stage นั้นใน plan §7 และ gap ใน §6.1
2. สร้าง/รันตัวตรวจ "ก่อน" ถ้าจุดนั้นยังไม่มีตัวตรวจ
3. แก้โค้ด → `refresh_unity` (ดู §6.3) → ตรวจ console error
4. แก้ scene/prefab ผ่าน `execute_code` (SerializedObject) แล้ว save
5. **กรอง scene diff** (§6.3 ข้อ 2)
6. Play: รัน flow trace + ตัวตรวจเฉพาะ stage + `check_yarn_contract.py --check`
7. อัปเดต README stage log + plan §7 → `git add` **ระบุ path** → commit (ข้อความอังกฤษ ลงท้าย `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`)

### 6.3 กับดัก (เจอมาแล้ว)

1. **`[GameLoop]` ใน `New Drag Drop System.unity` เป็นสำเนา unpack ไม่ผูกกับ prefab** `[04]Prefab/GameSystemPrefab/[GameLoop].prefab` เก่ากว่า scene และถูกใช้เฉพาะ dev scene `New Cocktail System`, `Garnish Mockup` (ไม่อยู่ใน build) ทุก stage แก้ที่ object ใน scene จริง แตะ prefab เฉพาะกรณีที่ถ้าไม่แตะ dev scene จะพัง (ทำแล้วใน 4b)
2. **save scene จาก editor สร้าง noise ของ Cinemachine** (FOV, far clip, ตำแหน่ง/การหมุน camera) เจอทุกครั้ง 3 hunk ขั้นตอน: `git diff -U3 -- <scene> > full.patch` → ใช้ python แยก hunk (`re.split(r"(?m)^(?=@@ )", text)`), ทิ้ง hunk ที่มี `m_LocalRotation`/`m_LocalPosition`/`value:` ของ camera → `git checkout HEAD -- <scene>` → `git apply --recount --whitespace=nowarn filtered.patch` → ใน Unity `EditorSceneManager.OpenScene(path)` ให้ memory ตรงกับไฟล์
3. **`refresh_unity` ที่ `wait_for_ready=true` timeout ได้** ใช้ `wait_for_ready=false` แล้วรอ ~30 วินาที ตรวจ `EditorApplication.isCompiling`/ชนิดที่เพิ่ม ก่อนใช้ type ใหม่ใน `execute_code`
4. **`execute_code`**: `Object` กำกวม ใช้ `UnityEngine.Object`; local iterator function (`System.Collections.IEnumerator Run() { yield ... }`) ใช้เป็น coroutine ผ่าน `cmds.StartCoroutine(Run())` ได้; ผลต้องเขียนไฟล์แล้วโพล `DONE`
5. **minigame ที่ถูก cancel เล่น outro นาน (>15 วินาที)** เริ่มใหม่ไม่ได้จนกว่า phase = `Idle` — รอ `Idle` ก่อน `IngredientAdded()` (ใช้ใน `serve_commit` และ `lock_fingerprint`)
6. **order สุ่ม**: `InitState(410)` ต้องเรียก**ทันทีก่อน** `cmds.PrepareDrinks()` (consumer อื่นของ `UnityEngine.Random` ทำให้ seed ต้นไม่พอ)
7. **`git add` ระบุ path เท่านั้น ห้าม `-A`** (มี `ysls.json` ที่ไม่ใช่ของเรา); ข้อความเตือน LF/CRLF ไม่เป็นไร
8. **hotkey ใน editor**: `2`–`0` = flow command (`GameFlowDebugHotkeys` บน `[Debug]`), Shift+`1`/`2`/`B`/`V` = minigame (`MinigameSystemManager`), `R` = reload scene (`Demo_KeysShortCut` บน `NPCs_And_Set`)
9. **ชื่อ child เปลี่ยนไปแล้ว**: `SystemGame` → `[CocktailSystem]`, `MiniGameSystem` → `[MiniGameSystem]`, `PlacmentSystem` → `[PlacmentSystem]` เอกสารเก่าบางที่ยังใช้ชื่อเดิม
10. Unity MCP อาจหลุด (`MCP server disconnected`) → ตรวจ editor state ก่อนแก้ต่อ
11. **หลัง `manage_editor play` ต้องรอให้พร้อมก่อนรัน snippet** ถ้ารันทันที snippet อาจติดอยู่ก่อน domain reload ของ Play mode: `GameFlowCommands.Instance` เป็น null ภายหลัง, `BaseMiniGame.Input` เป็น null (NRE ใน `ProcessedGame`), trace ค้างกลางทาง (เจอตอน Stage 5 ครั้งแรก) เช็กก่อนว่า `Instance != null` และ `GetComponent<ShakingMinigame>()` มี `Input` แล้ว (snippet ทุกตัวเริ่มต้นด้วยเช็กนี้ได้) และ **ห้ามแก้ .cs ระหว่างอยู่ใน Play**
12. **`Button.onClick.Invoke()` ไม่ดู `interactable`** trace/snippet ที่เรียกแบบนี้พิสูจน์ได้แค่ว่า handler ทำงาน ไม่ใช่ว่าผู้เล่นกดได้ สำหรับปุ่มที่ lock ได้ให้ใช้ `ExecuteEvents.Execute(go, PointerEventData, pointerClickHandler)` (ดู `method_buttons_click.snippet.cs`)
13. **ย้าย object เข้าใต้ `[CocktailSystem]` ต้องรัน lock fingerprint ซ้ำ** `IngredientButtonGroup` เก็บสมาชิกด้วย `GetComponentsInChildren<IngredientButtonUI>` ตามตำแหน่ง จึงเก็บของที่ย้ายเข้ามาเงียบๆ (4b พลาดตรงนี้: README ระบุว่า fingerprint "เหมือนก่อนแก้" แต่เป็นของ 4a) 4b-fix แก้ด้วยการลบ `IngredientButtonUI` ซากออกจากปุ่ม Shake/Stir ดังนั้นตอนนี้ **ทุก `IngredientButtonUI` ใต้ `[CocktailSystem]` คือสมาชิกที่ถูก lock** ถ้าจะเติม component นี้บนปุ่มที่ไม่ใช่ขวดต้องรู้ว่ามันจะถูก lock ตอน drink เต็ม
14. **chapter dialogue วิ่งก่อน snippet จะหยุดมัน** ระยะที่มันไปถึง (กล้อง, NPC เดิน/เปลี่ยนหน้า) ขึ้นกับว่า editor ใช้เวลาเข้า Play นานแค่ไหน ตั้งแต่ 7d คำสั่งในบรรทัดแรกๆ ทำงานจริง จึงเห็นผลชัดขึ้น snippet ที่อ่านสถานะที่ dialogue แตะได้ (กล้อง, ตำแหน่ง/หน้า NPC) ต้องตั้งค่ากลับก่อน (`customer_yarn`) หรือไม่เอาค่านั้นมาเทียบ (`flow_trace` S00 ไม่พิมพ์ `cam=`)
15. **heredoc ใน bash ที่มีข้อความไทย/อัญประกาศปนกันพังได้** (`unexpected EOF while looking for matching`) แก้ไฟล์ด้วยสคริปต์ python ที่เขียนลงไฟล์ด้วย Write tool แล้วค่อยรัน (สคริปต์ที่ใช้ทั้งหมดเป็นแบบ `edit(path, [(old, new), ...])` ที่ assert ว่า `old` มีอยู่) และเก็บ CRLF/LF ของไฟล์เดิม
16. **Yarn instance command หา target ด้วย `GameObject.Find(ชื่อ)`** `<<cmd Name ...>>` จึงผูกกับชื่อ object ใน scene เปลี่ยนชื่อ object แล้ว command ล้ม (`TargetMissingComponent`) แต่ dialogue **ไม่หยุดทันที** ยังต่อได้ ถ้าจะเพิ่ม/ย้าย command ให้ทำเป็น static (+ รับ target เป็น `string target = ""` ที่ไม่ใช้) แบบ `CocktailSystemManager.Yarn_*` ตรวจด้วย `customer_yarn` (A4) ว่า status เป็น `Succeeded`
17. **`DialogueRunner.CommandDispatcher` เป็น internal** lever เรียกผ่าน reflection: property `CommandDispatcher` → interface `ICommandDispatcher.DispatchCommand(string, MonoBehaviour)` (explicit implementation) → field `Status` ของ struct ผลลัพธ์ (ดู `customer_yarn.snippet.cs`)
18. **`git add` ที่มี path ผิดเพียงตัวเดียวไม่ stage อะไรเลย แต่ `git commit` ต่อท้ายยังทำงาน** และ commit สิ่งที่ stage ไว้ก่อนหน้า (เช่น rename จาก `git mv`) ให้ตรวจ `git show --stat HEAD` ทุกครั้ง; ถ้าพลาดและยังไม่ push ใช้ `git add <ที่เหลือ>` แล้ว `git commit --amend --no-edit`
19. **ไฟล์ .cs ในโปรเจกต์มีทั้ง LF และ CRLF ปนกัน** และ `Write` tool ตัดช่องว่างท้ายบรรทัดออก pattern ที่มี `{ ` + ขึ้นบรรทัดใหม่จึงไม่ match ใช้ `Edit` tool ตรงๆ สำหรับจุดที่มีช่องว่างท้ายบรรทัด และเรียงลำดับ: เปลี่ยนชื่อไฟล์ด้วย `git mv` ทั้ง `.cs` และ `.meta` (GUID คงเดิม) แล้วค่อย `refresh_unity`
20. **flow trace ขึ้นกับเวลา** snippet เดิมถ่าย screenshot ทุก step ซึ่งช้าพอให้ outro ของ minigame ที่ถูก cancel (>15 วินาที) จบก่อน `IngredientAdded()` ถัดไป พอตัด screenshot ออก (หรือเครื่องเร็วขึ้น) จะขึ้น `StartGame ignored — already in Outro` และ `TIMEOUT waiting for minigame Play` snippet ตอนนี้รอ `mgPhase == Idle` ก่อนทุก `IngredientAdded()` และก่อน `NextDay()` (S14 รายงานผลของ run สุดท้าย) ถ้า trace ต่างจาก baseline แค่ท้ายไฟล์ ให้ดูตรงนี้ก่อน
21. **`BookUI_V2.Awake` ปิด panel ตัวเองตอนโหลด scene** panel จึงปิดอยู่เสมอ ปุ่มที่เป็นลูกของมัน (next/previous/close) ผูกใน `Awake` ได้ (รันตอนโหลด ขณะ active) แต่ปุ่มเปิดที่อยู่ใน Hub ผูกจาก panel ไม่ได้ ใช้ `Button.onClick.AddListener` ใน `Awake` ไม่ใช่ `OnEnable` เพราะ `Awake` ของ object ที่ inactive ตั้งแต่ต้นจะไม่รันจนกว่าจะ active
22. **ปุ่มที่ปิด `interactable` ไว้ใน scene จะไม่รับ `ExecuteEvents` click** (post-it toggle เป็นแบบนี้) lever ที่ต้องการให้ปุ่มทำงานต้องเข้า step ที่เปิดมันก่อน ปุ่ม book ล็อกตอนลูกค้าคุย (ต้อง `PrepareDrinks()` ก่อน)

---

## 7. งานที่เหลือ (ละเอียดต่อ stage)

### Stage 4c · book (**เสร็จใน Stage 9**)
ปุ่ม `Canvas/Panel - Hub/BTN - Open_CloseBook` ถูกเขียนจาก 4 จุด: `IngredientButtonGroup` (lock), `CocktailFlowBridge._btnBook` (เปิดกลับตอน `OnFulled`), `UIFlowBridge` (onClick → `BookUI_V2`), `TalkingWithCustomerBridge`/`CocktailFlowBridge` (ปิด book) กฎ "step ไหนกดได้" กระจายอยู่ จึงเลื่อนไปทำพร้อม presenter/`FlowStep` ใน Stage 8

### Stage 5 · Minigame Manager (**เสร็จ**)

ที่ทำจริง
- `MinigameSystemManager` ถือ `_btnBack[2]` (`BTN - Cancel Minigame` ของ Shaking/Mixing canvas) ผูก listener ใน `OnEnable`/`OnDisable` ส่ง event `BackRequested`; `Resolve(selected, fromDrink)` + `_defaultType` ย้ายมาจาก bridge (ค่า serialized 1 = Shaking เท่าเดิม); ลบ `OnStartedMinigame`/`OnEndedGame` (UnityEvent ว่าง ไม่มี listener ทั้ง scene, prefab, dev scene)
- `MinigameFlowBridge`: `OnBackRequested` → `AnotherIngredient()`; ลบ warning "no minigame to run" (ซ้ำกับ `StartMinigame` และ default คือ Shaking)
- F6 (`f6_giveup.txt` ก่อน → `.after_stage5.txt` หลัง): ก่อนแก้ Give-up/Cancel-direct ทิ้ง flow ค้างที่ 2.2; ผมแก้ให้ออกผ่าน HSM แล้ว user สั่งลบ `BG_BTN` ทั้งกลุ่ม (`BTN_Reset`, `BTN_Next`, `BTN_AddIce`: ปิดไว้ทั้ง scene + prefab, ไม่มีอะไรเปิด, 7 object) F6 จึงไม่มีให้เกิดอีกทาง UI เหลือแค่ Shift+B ใน editor ที่ตั้งใจให้ยกเลิกตรง ผู้เล่นรีเซ็ต drink ได้จากปุ่ม Reset ของ Method panel เหมือนเดิม
- ตัวตรวจทางออกที่เหลือ: `back_button` (กดปุ่ม back จริง → 2.1 → เข้า 2.2 ซ้ำได้, drink ถูกเก็บ, shaker ปลด lock)

### Stage 6 · Garnish Manager (**เสร็จ**, 4 commit)

ที่ทำจริง
- **6a** `[GarnishSystem]/Canvas_Garnish` ใหม่ (อยู่ก่อน `Canvas` ใน `[GameLoop]`) รับ `Panel - Granish UI`, `Panel - Serve` และ `Garnish_Plate` (world position คงเดิม) ทั้ง scene และ prefab
- **6b** `GarnishManager` (`Assets/[02]Script/Garnish/`, บน `[GarnishSystem]`) ถือ glass/ice/ตกแต่ง/pour และปุ่ม tab/Pour/Finish/Reset/ice ของ canvas ตัวเอง; รับ drink เป็น argument (`Pour(drink)`); events `Opened`, `PourRequested`, `PourStarted`, `IceToggled`, `FinishRequested`, `ResetRequested`; `GarnishFlowBridge` เหลือ 7 slot: เข้า step → `UpdateCocktailInShaker`, กล้อง, ล็อก ingredient, `Open()`; event ของ manager → flow/cocktail
- **6c** Serve panel + ปุ่ม Serve/Remake เป็นของ `GarnishManager` (D2); `ServeFlowBridge` ถือฝั่ง flow (เปิด/ปิดตาม step, ซ่อน post-it, ส่ง `ServeDone()`/`RemakeDrink()`); ตัด writer อีก 2 ตัว (`CocktailFlowBridge._panelServe`, `ShakerPanelController._serveUI` + flag serve)
- **6d** `PlacedGlassInstance` ไม่มี static; `GarnishManager.Glass`/`ClearGlass()`; `GarnishFlowBridge` เคลียร์ glass ตอน `PrepareDrinks.Entered` และ `Serve.Exited` (เดิมอยู่ `CocktailFlowBridge`)

ค้าง/สังเกต
- `CanFinish` ยังเป็น True หลัง `ClearGlass()` จนกว่า `Open()` ถัดไป (พฤติกรรมเดิม baseline ล็อกไว้ ไม่มีผลเพราะ panel ปิดอยู่) ถ้าจะแก้ให้ reset ใน `ClearGlass()` แล้ว trace จะเปลี่ยน `canFinish=` ที่ S07/S10 ต้องบันทึกในคอมมิต
- `GlassPlacementZone` คงไว้ใต้ `[GarnishSystem]/Garnish_Plate` (D9) ไม่มีใครเรียก (`SetGlass`, static `_occupant`) ถ้าจะลบเป็นการตัดสินใจ design
- `Bar410DialogueOrderTest` ใช้ `GarnishManager` แล้ว (ต่อ `_garnish` ใน scene แล้ว) ยังไม่ได้รันจริง (ต้องมี dialogue)
- `IngredientButtonUI._action` ยังมีค่า `SetShaking`/`SetMixing`/`AddIce`/`ResetShaker` ที่ไม่มีใครใช้ (ปุ่ม method มี `MethodPanel` แล้ว) ถ้าจะตัดเป็นงานเล็กแยก

### Stage 7 · TalkToCustomer Manager (**เสร็จ**, 4 commit)

ที่ทำจริง
- **7a** ตัวตรวจ `customer_yarn` (ผ่าน `ICommandDispatcher` จริงด้วย reflection, save → load, ตัวแปร Yarn) บันทึก baseline
- **7b** `TalkToCustomerManager` (`Assets/[02]Script/Customer/`, บน `NPCs_And_Set`): NPC lookup แบบ instance (NPC ใต้มันที่ `Name != None`), Yarn command ×4 static ผ่าน `Current` (ชื่อเดิม, `IsSilentReplay` guard เดิม), `Snapshot()`/`Restore()`; `SaveLoadManager` เรียกสองตัวนี้; restore ข้าม teleport เมื่อ index = -1 (ตัด warning เท็จ)
- **7c** manager ถือ `DialogueRunner`, `SO_CustomerRoster` (fallback `CharacterData`), `YarnVariableSync` และเป็นผู้เขียน `$task_done`/`$satisfaction`/`$type_of_cocktail`/`$rel_*` คนเดียว (`CommitOrderResult`, `ResetVariables`, ContextMenu `Debug Yarn Variables`); `CocktailSystemManager` ไม่รู้จักสิ่งเหล่านี้ ส่ง `OrderScored`/`OrderCleared` (synchronous) และรับ `UsePreferences`; `TalkingWithCustomerBridge` ต่อ
- **7d** command ของ cocktail เป็น static ไม่สนชื่อ object (ดู §3)

ค้าง/สังเกต
- **save/load จริง (เปลี่ยน scene + silent replay) ยังไม่ได้ทดสอบ** `customer_yarn` ครอบคลุมเฉพาะ `CollectNPCData`/`ApplyNPCData` ในฉากเดียว (`[SaveLoadSystem]` ปิดอยู่ใน dev scene จึงต้องเรียกตรง)
- look direction ไม่ถูกบันทึก (`look=None`) และ emotion ไม่ถูกบันทึก: บั๊กเดิม ยังไม่แก้ (ดู §3)
- NPC สามตัวใต้ `TMP_NPC` (Name = None) เป็น placeholder ปิดอยู่ manager ข้ามพวกมัน
- dev scene `New Cocktail System` / `Garnish Mockup` (ใช้ prefab เก่า) ไม่มี `TalkToCustomerManager` จึงไม่มี preferences/การเขียนตัวแปร Yarn (bridge เตือนครั้งเดียว) ไม่อยู่ใน build
- `Demo_KeysShortCut` บน `NPCs_And_Set` (R → โหลด scene ใหม่) ยังอยู่ เป็นปุ่ม dev
- `.yarn` ยังมี `Fix_Camera`/`Reset_Camera`/`Switch_Camera CamController ...` ซึ่งยังเป็น instance command ผูกกับชื่อ object `CamController` (ยังพอดีเพราะชื่อไม่ถูกเปลี่ยน) เป็นของ Stage 8 ให้ทำแบบเดียวกับ 7d

### Stage 8 · Camera Manager (**เสร็จ**, 3 commit)

ที่ทำจริง
- **8a** lever `camera_yarn` (คำสั่งกล้องของ Yarn ผ่าน dispatcher จริง, ซ้ำเมื่อเปลี่ยนชื่อ `CamController`, สิ่งที่ flow ทำกับกล้องและมุมมอง) baseline: ทุกคำสั่งได้ `TargetMissingComponent` เมื่อเปลี่ยนชื่อ
- **8b** `CinimachineCameraSwitcher` → `CameraManager` (ไฟล์และ GUID เดิม scene จึงเก็บค่าเดิม): เก็บ `CameraEntry` เองตอน `Awake` (`CameraEntry` เหลือเป็นข้อมูล ไม่มี `Instance`/`Start`/`OnDestroy`), ตั้งชื่อ shot ที่ flow ขอ (`ShowBar` = `MainCam`, `ShowShaker` = `PrepareCam`, `ShowGarnish` = `GranishCam`; id อยู่ใน manager ที่เดียว), ถือ `CameraController` (`SetLookFixed`, `ResetLook`); `Switch_Camera`/`Cut_Camera`/`Fix_Camera`/`Reset_Camera` เป็น static ชื่อเดิม รับ argument แรก (`CamController`) แล้วไม่สนใจ ผ่าน `CameraManager.Current`; 3 bridge เหลือ slot `_cameras` อย่างละหนึ่ง (เดิม `CameraController` + switcher + id string); `CameraController` ไม่มี Yarn attribute, `ResetRotaionAndMovement` → `ResetView` (ลบ `ResetCamera` private ที่ซ้ำ), `SetFixedCamera` ถูกลบ (ใช้ property `IsFixedCamera`)
- **8c** ลบ `MinigameSystemManager._cocktailCamera` + `IMinigameContext.ResetCamera()` + การเรียกใน `BaseMiniGame.StartGame`
- ตัวตรวจ: `camera_yarn` (ก่อน/หลัง), flow trace (ทุกไบต์เหมือน baseline), `back_button`, `check_yarn_contract.py --check`

ค้าง/สังเกต
- คำสั่งของ NPC (7b), cocktail task (7d) และกล้อง (8b) เป็น static แล้ว ถ้าเพิ่ม command ใหม่ให้ทำเป็น static ตั้งแต่ต้น (§6.3 ข้อ 16)
- `Switch_Camera` ที่ id ไม่มีใน scene ยังได้ status `Succeeded` และ log error (พฤติกรรมเดิม)
- `ApplyTransition` ยังทำตามเดิม: กล้องเดิมถูก `enabled = true` + priority 10 (comment เดิมบอกว่า disable แต่โค้ดไม่ได้ทำ) อย่าแก้โดยไม่ทดสอบ เพราะเปลี่ยน blend
- `CameraController.SetCanRotateCamera` ไม่มีใครเรียก และเขียนลง `CameraControllerSettings` (SO ที่ใช้ร่วม) ตอน runtime ปล่อยไว้
- `N_InputManager._sceneCamera` คงเป็น ref ที่ serialize (D12) `DebugPhysicsRaycaster` บน Main Camera ใน nested prefab `CamController` ไม่แตะ
- presenter/`FlowStep` ไม่ทำ (D12) กฎอยู่ที่ Stage 9
- prefab `[GameLoop].prefab` (dev scene) ถูก sync เรื่องกล้อง และได้ field ที่ค้างจาก Stage 7 (`_cocktail`/`_customers` บน `TalkingWithCustomerBridge`, ลบ `_customerRoster`/`_dialogueRunner`)
- `.yarn` ใช้ `Switch_Camera` 36, `Fix_Camera` 29, `Reset_Camera` 21 ครั้ง (id ที่ใช้: `MainCam`, `CenterCam`, `NearLeftCam`, `NearRightCam`, `FarRightCam`, `SummaryCam`); `Cut_Camera` ไม่มีใครใช้ แต่ชื่อคงไว้

### Stage 9 · Shell + ลบ bridge (**เสร็จ**, 4 commit)

ที่ทำจริง
- **9a-0** lever `shell_ui` (ปุ่ม book, post-it, โหลด scene ด้วย pointer click จริง) baseline
- **9a** `UIFlowBridge` หายไป: `BookUI_V2` ผูกปุ่ม next/previous/close (ลูกของ panel) เอง, `Post_It_Order` ผูกปุ่ม toggle เอง, `LoadSceneButton` (ใหม่, `Assets/[02]Script/SceneLoader/`) บน `EndShiftButton` และ `Panel_Thx/Image/Button`, ปุ่มเปิด book ผูกที่ bridge ที่ถือกฎล็อกของมัน; `[GameLoop]/Canvas` → `Canvas_Shell` (มีแต่ Hub, book, EndShift, Thx, Transition อยู่แล้ว)
- **9b** `FlowWiring` (`Assets/[02]Script/Hierarchical State Machine/FlowWiring.cs`) รับ Minigame + Garnish + Serve bridge: subscribe event ของ step, เรียก verb ของ manager, แปลง event ของ manager กลับเป็น flow command; ถือ manager/object ที่ใช้ร่วมครั้งเดียว หา `GameLoopFSM`/`GameFlowCommands` ด้วย `GetComponent`
- **9c** `FlowWiring` รับ Cocktail + Talking bridge; ตอนนี้ `[GameLoop]` root มี `GameLoopFSM` + `GameFlowCommands` + `FlowWiring` (เดิม 8 script) ไฟล์เดียวอ่านกฎของทุก step ได้ ไล่ตาม §: 1 Talking, 2 Prepare, 2.1 AddIngredient, 2.2 Minigame, 3 Garnish, 4 Serve
- ตัวตรวจ: ดู README stage log (9a-0, 9a, 9b, 9c, 9 รวม)

ค้าง/สังเกต
- **ปุ่ม toggle ของ post-it ไม่ interactable ใน scene** กดแล้วไม่เกิดอะไร (มี listener แต่ผู้เล่นกดไม่ได้) พฤติกรรมเดิม ถ้าตั้งใจให้ post-it เปิด/ปิดเองโดย flow ก็ลบปุ่มกับ `_btnToggle` ได้ ถ้าไม่ใช่ต้องเปิด `interactable` (การตัดสินใจ design)
- `CharacterData` คงไว้ตาม D6 (fallback ของ roster ไม่ถูกใช้) และ `Demo_KeysShortCut` (R → โหลด scene ใหม่) ยังอยู่บน `NPCs_And_Set`
- `FlowWiring` ไม่ได้ถูก unit test ในโหมด Edit (ต้องมี scene); ตรวจผ่าน lever ใน Play เท่านั้น
- ไม่ได้แตะ `SceneLoaderBridge` จึงไม่ต้องมีตัวตรวจ save → load ข้าม scene สำหรับ stage นี้ (§8 ข้อ 3 ยังค้างไว้ถ้าจะแตะในอนาคต)
- `Bar410DialogueOrderTest` ยังไม่ได้รันจริง

### Stage 10 (optional) · Level 1 เป็น enum
ลดโค้ดราว 130 บรรทัด มีค่าน้อย ไม่จำเป็นต่อเป้าหมาย: ไม่มี `.yarn` ไฟล์ไหนเรียก `flow_close_bar`/`flow_next_day` (ใช้แค่ `flow_prepare_drinks` 9 ครั้ง) และ user ตัดสินแล้ว (D11) ให้ L1 ขับด้วยปุ่ม/hotkey ตามเดิม ไม่ต้องให้ writer เพิ่ม command

---

## 8. คำถามที่ยังเปิด / ต้องการคำตอบจาก user

ตัดสินแล้ว (ไม่ต้องถามอีก): L1 ขับด้วยปุ่ม/hotkey (D11), `GlassPlacementZone` อยู่ Garnish (D9), ลบ `BG_BTN`, แก้ 4b ด้วยวิธี B, commit Stage 5 เป็น commit เดียว

1. ✅ **ปิด 2026-10-24: user เล่น Day 1 จริงแล้ว ปกติดี** (เดิม: เล่น Day 1 จริงหนึ่งรอบเพื่อตรวจ 7d) dialogue ตอนนี้รอการเสิร์ฟที่ `wait_for_task` และคำสั่งบรรทัดแรก (`Enable_InteractableObject`, `Fix_Camera`, `Switch_Camera`) ทำงานจริง ถ้าไม่ตรงกับที่ writer ออกแบบให้ revert `e7bc9ad` (ต้นเหตุ: ชื่อ object ใน scene ไม่ตรงกับ `.yarn`)
2. **save ควรเก็บอะไรของ NPC** ตอนนี้เก็บตำแหน่ง/waypoint แต่ไม่เก็บหน้า (emotion) และทิศมอง (`_currentLookDirection` ไม่เคยถูกเซ็ต) ต้องให้ user ตัดสิน เพราะต้องปรับ save format และ restore
3. **save → load จริงข้าม scene + silent replay** ยังไม่เคยทดสอบ (`customer_yarn` ครอบคลุมแค่ `Collect`/`Apply` ในฉากเดียว) ถ้า Stage 9 จะแตะ `SceneLoaderBridge` ต้องมีตัวตรวจนี้ก่อน
4. **`Canvas_Cocktail` CanvasScaler match 0.5** ผมเปลี่ยนจาก 0 ใน 4b ตั้งใจเดิมเป็น 0 หรือเปล่า (ยังไม่ได้คำตอบ)
5. **Shell เป็น `Canvas_Shell` ไม่มี manager**: ทำแล้วใน 9a (default); ถ้าอยากได้ manager สำหรับ Hub/Transition บอกได้
6. **ลำดับวาดของ `Canvas_Garnish`/`Canvas_Cocktail`** อยู่ก่อน `Canvas` จึงถูก Hub/BookUI/Transition วาดทับ ถ้าเห็นว่า Hub บัง panel ให้ปรับ `sortingOrder`
7. **ทดสอบด้วยเมาส์จริงที่ MCP ทำไม่ได้**: ลากขวดลง shaker, ลากผลไม้จาก tray, Shift+1/2 hotkey ขอให้ user ลองสั้นๆ
8. **`CharacterData` (legacy)** คงไว้ตาม D6 แต่ไม่ถูกใช้ (roster ต่อไว้) ถ้าจะลบให้ลบ component บน `NPCs_And_Set` + fallback ใน `TalkToCustomerManager` และ `Demo_KeysShortCut` (R → โหลด scene ใหม่) ยังอยู่
9. **เปิด PR** branch push ขึ้น origin แล้ว ยังไม่มี PR (branch นำ `GameFlow/Main` อยู่ ~30 commit)
10. ✅ **ปิด 2026-10-24 พร้อมข้อ 1** (เดิม: เล่นจริงดูกล้อง) chapter dialogue ใช้ `Switch_Camera`/`Fix_Camera`/`Reset_Camera` ผ่าน static command ใหม่ ผมตรวจผ่าน dispatcher แต่ยังไม่เห็นด้วยตาว่า shot/blend ตอนคุยตรงกับที่ writer ต้องการ (ถ้าไม่ตรง คือ 7d ทำให้คำสั่งทำงานจริงเป็นครั้งแรก ไม่ใช่ 8b)
11. **ยืนยัน D12**: ไม่ทำ presenter/`FlowStep` และไม่ส่ง `Camera` ให้ Placement ถ้าต้องการ enum ให้ทำที่ Stage 9
12. **ปุ่ม toggle ของ post-it** ไม่ interactable (ดู §7 Stage 9 ค้าง/สังเกต) ตั้งใจหรือเปล่า
13. ✅ **user สั่งเปิด PR 2026-10-24** (เดิม: รวมเข้า `GameFlow/Main`): branch นำอยู่ ~45 commit ยังไม่ push ถ้าจะเปิด PR ให้บอก (ผมจะไม่ push เอง)

---

## 9. ไฟล์สำคัญ

| ไฟล์ | ใช้ทำอะไร |
|---|---|
| `Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs` | Yarn/UI entry; `CommandTarget()` guard |
| `Assets/[02]Script/Hierarchical State Machine/FlowWiring.cs` | **ที่เดียวที่ flow กับ manager เจอกัน** (Stage 9): กฎ step → book/post-it/ingredient/camera, minigame, garnish, serve, ส่งผลให้คะแนนเข้าตัวแปร Yarn |
| `Assets/[02]Script/Garnish/GarnishManager.cs` | Garnish manager: glass, ice, ตกแต่ง, pour, Serve panel (Stage 6) |
| `Assets/[02]Script/SceneLoader/LoadSceneButton.cs` | ปุ่มที่โหลด scene ผ่าน `SceneLoader` (EndShift, Thanks) |
| `Assets/[02]Script/UI Script/BookUI_V2.cs` / `Post_It_Order.cs` | panel ที่ผูกปุ่มของตัวเอง (Stage 9) |
| `Assets/[02]Script/Cocktail System/Cocktail/CocktailSystemManager.cs` (+ `Yarn/*.cs` partial) | Cocktail manager; `ServeDrink()` idempotent; `BindShaker` |
| `Assets/[02]Script/Cocktail System/Cocktail/Shaker/IngredientButtonGroup.cs` | auto-collect ขวด |
| `Assets/[02]Script/Cocktail System/MethodPanel.cs` | panel ที่ดูแลปุ่มตัวเอง (pattern ที่จะใช้ต่อกับ Serve/Garnish panel) |
| `Assets/[02]Script/PlacementSystem/N_PlacementSystem.cs` | `Current`, `InputManager` |
| `Assets/[02]Script/Customer/TalkToCustomerManager.cs` | NPC lookup + Yarn command ของ NPC, ตัวแปร Yarn, preferences, snapshot/restore (Stage 7) |
| `Assets/[02]Script/NPC/NPC_Base.cs` | พฤติกรรมของ NPC หนึ่งตัว (ไม่รู้จัก dialogue) |
| `Assets/[02]Script/CameraControl/CameraManager.cs` | Camera manager: shot (`ShowBar`/`ShowShaker`/`ShowGarnish`), `SwitchCamera`/`CutToCamera`, look (`SetLookFixed`/`ResetLook`), Yarn static command (Stage 8) |
| `Assets/[02]Script/CameraControl/CameraController.cs` / `CameraEntry.cs` | มุมมองตามเมาส์ (ไม่มี Yarn attribute แล้ว) / ข้อมูล id + blend ของกล้องหนึ่งตัว |
| `Assets/[05]Scenes/Deverlopment/New Drag Drop System.unity` | scene จริง (ใน Build Settings) |

---

## 10. คำสั่งเริ่ม session ใหม่

> อ่าน `Docs/Bar410_Managers_Restructure_HANDOFF.md` ก่อน Stage 0–9 เสร็จหมดแล้ว งานที่เหลือต้องให้ user เลือก: (ก) Stage 10 (Level 1 เป็น enum, optional, ลดโค้ด ~130 บรรทัด), (ข) ตอบคำถามค้างใน §8 (โดยเฉพาะ 1, 10, 12 ที่ต้องเล่นจริง), (ค) push / เปิด PR `GameFlow/Managers` → `GameFlow/Main` ก่อนแก้อะไร ให้ตรวจว่าอยู่ branch `GameFlow/Managers` และ working tree สะอาด (ยกเว้น `ysls.json`) จากนั้นรัน `check_yarn_contract.py --check`, flow trace, `shell_ui`, `camera_yarn`, `customer_yarn` และ lock fingerprint เพื่อยืนยันว่า baseline ยังผ่าน (รอให้ Play พร้อมก่อนรัน snippet ดู §6.3 ข้อ 11, 14, 20)

ผู้ใช้เป็นคนไทย ตอบไทยกระชับ ชื่อเทคนิคเป็นอังกฤษ; ตัดสินใจทุกอย่างผ่าน decision log; คุ้นกับ Unity MCP; ไม่ push/ไม่เปิด PR จนกว่าจะบอก
