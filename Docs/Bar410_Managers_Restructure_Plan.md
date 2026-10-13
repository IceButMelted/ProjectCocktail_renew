# Bar410 · Managers Restructure Plan

วันที่ 2026-10-13 · สถานะ proposal (ตัดสินใจหลักแล้ว ดู §8) ยังไม่แก้โค้ด · branch ตอน audit `GameFlow/Main`

เอกสารนี้แทนที่ส่วนแผน (§5) ของ `Bar410_GameLoop_Decoupling_Plan.md` ส่วน audit (§1–§3) ของเอกสารนั้นยังใช้ได้

ตัวเลขและข้อสรุปมาจากการอ่านโค้ดและ dump scene hierarchy ไม่ได้รันเกม ข้อที่เป็นการอนุมานจะระบุว่า "inferred"

---

## 0. Stage 0 baseline และสิ่งที่เปลี่ยนจากตอน audit

baseline อยู่ที่ `Docs/baseline/managers/` (README ในโฟลเดอร์อธิบายวิธีเช็กทุก stage) สรุปที่มีผลต่อแผนนี้

- **`[GameLoop]` ใน `New Drag Drop System.unity` เป็นสำเนาที่ unpack แล้ว ไม่ได้ผูกกับ prefab** (scene นี้อยู่ใน Build Settings) `[04]Prefab/GameSystemPrefab/[GameLoop].prefab` จึงเก่ากว่า scene และถูกใช้เฉพาะ dev scene `New Cocktail System` กับ `Garnish Mockup` (ไม่อยู่ใน build) ทุก stage แก้ที่ object ใน scene จริง ไม่แก้ prefab แล้วคาดว่า scene จะตาม
- **ชื่อ child เปลี่ยนแล้ว** `SystemGame` → `[CocktailSystem]`, `MiniGameSystem` → `[MiniGameSystem]`, `PlacmentSystem` → `[PlacmentSystem]` เอกสารนี้ยังใช้ชื่อเดิมในบางที่ ให้อ่านตามชื่อใหม่
- **`[CocktailSystem]/Canvas_Cocktail` มีอยู่แล้ว** ตอนนี้มีแค่ post-it (`Panel - OrderDirnks`) ดังนั้นงาน post-it ใน Stage 4c ย้ายเสร็จแล้ว เหลือ book, Method panel, `VisualizeCocktail` (screen-space)
- `RecipeBook` และ `Architexturebook` ถูกลบจาก prefab แล้ว ตัดข้อ `RecipeBook` ออกจากรายการที่ยังเปิดอยู่
- **ตัวเลขข้าม child ตอนนี้คือ 76** (จาก root 58) ไม่ใช่ 78
- **`flow_close_bar` และ `flow_next_day` ไม่มี `.yarn` ไฟล์ไหนเรียก** ใช้เฉพาะ `flow_prepare_drinks` (9 ครั้ง) ข้อความใน §5 ที่ว่า Yarn คุม L1 ผ่าน 2 command นี้ยังไม่เป็นจริงในเดโม ต้องเพิ่มการเรียกใน `.yarn` (งานของ writer) หรือคง hotkey/ปุ่มเป็นตัวขับ
- driver ที่ใช้เทียบทุก stage คือ `flow_trace.snippet.cs` (15 step, console error 0) ไม่ครอบคลุม Yarn dialogue, save/load และ NPC ซึ่งต้องทดสอบมือหรือใช้ `Bar410DialogueOrderTest` เสริมใน stage ที่แตะส่วนนั้น (Stage 4f, Stage 7)

---

## 1. เป้าหมาย

แยก `[GameLoop]` เป็น Manager ที่เป็นอิสระต่อกัน แต่ละตัวมี Canvas ของตัวเอง และข้อมูลที่ใช้ร่วมกันมีเจ้าของเดียว

| Manager | ดูแล | Canvas |
|---|---|---|
| Cocktail | drink, ingredient ทั้งหมด, **ice**, order, การคำนวณและ scoring, recipe, post-it, book, `VisualizeCocktail` ทั้ง 2 ตัว | `Canvas_Cocktail` |
| Minigame | เลือก/เริ่ม/ยกเลิก minigame, ผลลัพธ์ | `Canvas_MiniGame` (มีอยู่แล้ว) |
| Garnish | glass, garnish item/rim, pour, **Serve panel**, `GlassPlacementZone` | `Canvas_Garnish` |
| TalkToCustomer | ลูกค้าเดินเข้า/ออก, expression, look direction (Yarn command), **`CharacterData` + roster**, **Yarn variable writer + `DialogueRunner` ref** | dialogue canvas (อยู่นอก prefab ตอนนี้) |
| Camera | camera ID ทั้งหมด, `Switch_Camera` | ไม่มี |
| Placement | drag/drop, placement zone, ray ของ mouse (`N_PlacementSystem` + `N_InputManager`) | ไม่มี |
| Flow | FSM Level 2–3 + ประตู Yarn + `FlowWiring` | ไม่มี |

## 2. กฎ

1. Manager ไม่รู้จัก FSM และไม่รู้จัก Manager ตัวอื่น มีแค่ verb เข้า (public method) กับ event ออก (C# event)
2. จุดเดียวที่รู้จักทั้ง FSM และ Manager คือ `FlowWiring` (ไฟล์เดียว) subscribe state event และ manager event แล้วเรียก verb
3. ข้อมูลที่ใช้ร่วมกันมีเจ้าของเดียว คนอื่นรับเป็น argument หรืออ่านผ่าน property ของเจ้าของ ห้ามเก็บสำเนา
4. Manager เป็นเจ้าของ UI ใน Canvas ของตัวเอง ปุ่มผูกใน panel ของมันเอง ไม่ถูกผูกจาก root
5. ชื่อ Yarn command/function ที่มีอยู่ห้ามเปลี่ยน (contract กับ `.yarn`)
6. Singleton ใช้เฉพาะ service ข้าม scene (`SoundManager`, `SaveLoadManager`, `PauseManager`) และ static entry ที่ Yarn ต้องการ **ข้อยกเว้นเดียว** คือ Placement เป็น scene-scoped (ดู §6.1) เพราะมี consumer 14+ ตัวกระจายหลาย subtree และตอนนี้เป็น `public static` อยู่แล้ว

## 3. โครงสร้างเป้าหมาย

```
[GameLoop]
 ├ Flow            GameLoopFSM (L2-3), GameFlowCommands, FlowWiring
 ├ Cocktail        CocktailManager + Canvas_Cocktail
 │                 (shaker, bottles, IngredientShelf, Method panel, Post-it, Book, VisualizeCocktail x2)
 ├ Minigame        MinigameManager + Canvas_MiniGame
 ├ Garnish         GarnishManager + Canvas_Garnish
 │                 (glass/garnish UI, Serve panel, GlassPlacementZone)
 ├ Customer        TalkToCustomerManager (NPCs_And_Set + CharacterData + roster + YarnVariableSync)
 ├ Camera          CameraManager
 ├ Placement       PlacementManager (N_PlacementSystem + N_InputManager + MouseIndicator)
 └ Canvas_Shell    Hub, EndShift, Thx, Transition (panel ดูแลตัวเอง ไม่มี manager)
```

หมายเหตุ `NPCs_And_Set`, `Text Animator Dialogue System`, `[Debug]` อยู่นอก prefab `[GameLoop]` ตอนนี้ การย้าย Customer เข้ามาเป็น stage แยก

## 4. Single source of truth

| ข้อมูล | เจ้าของ | คนอื่นอ่านอย่างไร | สำเนาที่ต้องเลิก |
|---|---|---|---|
| Drink กำลังทำ (ingredient, method) | Cocktail (`ShakerContents.CurrentCocktail`) | Garnish รับเป็น argument ตอน `Pour(drink)` | `GarnishFlowBridge` ถือ `_shakerContents` เอง |
| **Ice** | **Cocktail** (`CurrentCocktail.AddIce`) เพราะเป็นเงื่อนไขตอนเสิร์ฟ | Garnish ส่ง event `IceToggled(bool)` ขึ้น, FlowWiring เรียก `Cocktail.SetIce`, Cocktail ส่ง `IceChanged(bool)` กลับ, FlowWiring เรียก `Garnish.ShowIce(b)` ให้ glass + ปุ่มสลับ (ทั้งหมด synchronous) | `PlacedGlassInstance` ไม่เก็บ flag ของตัวเอง เป็น view ที่ derive จาก Cocktail |
| Order (target, mode, result, payout) | Cocktail (`DrinkOrderContext`) | event `OrderScored(order)` | writer ที่ดึงจาก manager ตรงๆ |
| Glass ที่วาง / garnish item / rim | Garnish | `GarnishChoice` (plain C#: Glass, Items, Rim ไม่รวม ice) | `PlacedGlassInstance.Current` แบบ static, `DestroyCurrent()` ที่ถูกเรียกจาก Cocktail bridge |
| Minigame ที่จะเล่น | Minigame (`RequestedType`) | FlowWiring ส่ง `drink.RequiredMinigame` เป็น argument ตอนเริ่ม | `MinigameState.PendingType`, `ShakerContents.RequiredMinigame` (เป็นแค่ query), `_defaultType` ใน bridge |
| ผลลัพธ์ minigame ล่าสุด | Minigame (`LastOutcome`) | `flow_minigame_result` | `MinigameState.LastOutcome` |
| ลูกค้าที่อยู่ที่ bar | TalkToCustomer (`CurrentCustomer`, ตั้งตอน `MoveIn`) | Order เก็บ customer ณ ตอนสั่งเป็น record ของ order | `Order.Customer` ที่มาจาก Yarn argument ล้วน |
| Customer preferences (`CharacterData`, `SO_CustomerRoster`) | TalkToCustomer | Cocktail นิยาม `ICustomerPreferences` (มีอยู่แล้ว) FlowWiring ส่งตัวจริงเข้า `Cocktail.UsePreferences(...)` ครั้งเดียวตอนเริ่ม | `_customerRoster` และ `GetComponent<CharacterData>()` ใน `CocktailSystemManager.Awake` |
| Yarn variable + `DialogueRunner` | TalkToCustomer (`YarnVariableSync`) | Cocktail ส่ง `OrderScored(order)` FlowWiring เรียก `Customer.CommitOrderResult(order)` ทันที (synchronous) | `_dialogueRunner` + `Variables.*` ใน `CocktailSystemManager.YarnTask` |
| relationship | Yarn `$rel_<id>` เท่านั้น (locked D8) | `YarnVariableSync` | ห้ามสำเนา |
| Drag state / hovered object / zone | Placement | `PlacementManager.Current` (scene-scoped) | `DragableObject._placementSystem` (`public static`), `IngredientHoverDetector._inputManager` (static) |
| ตำแหน่ง flow (step) | Flow (FSM) | `Flow.Step` | ไม่มี manager ไหนเก็บ step เอง |
| camera ปัจจุบัน + camera ID | Camera | `Camera.Show(id)` | ID 3 ที่ (`MainCam`, `GranishCam`, `PrepareCam`) |
| ingredient interactable | Cocktail (`IngredientShelf`) | verb `Lock/Unlock` | 6 writer ตอนนี้ (3 bridge, `CocktailSystemManager`, `ShakerContents` events, Yarn) |

## 5. สัญญา FSM ↔ Yarn ↔ Manager

**FSM** ถือแค่ Level 2–3 (non-linear, UI-driven, enforce กฎ) Level 1 (Open ⇄ Close) เป็นเชิงเส้น Yarn คุมผ่าน `flow_close_bar` / `flow_next_day`

**Yarn → Flow** ใช้ 3 verb `flow_prepare_drinks`, `flow_close_bar`, `flow_next_day` ครอบทุก `flow_*` ด้วยเช็ก `SceneLoaderBridge.IsSilentReplay` (ตอนนี้ `GameFlowCommands.cs` ไม่มีการเช็ก ระหว่าง silent replay `flow_prepare_drinks` อาจดัน FSM ไป step 2 ทั้งที่ dialogue ยังเล่นอยู่ inferred)

**Flow → Yarn** ใช้ `wait_for_task` กับ `flow_step()`

**Manager → Yarn** มีทางเดียวคือ command/function ที่ manager ตัวนั้นเป็นเจ้าของ

### Yarn surface แยกตามเจ้าของ (ชื่อห้ามเปลี่ยน)

| เจ้าของเป้าหมาย | Command / Function |
|---|---|
| Flow | `flow_*` ทั้ง 10 command, `flow_phase`, `flow_step`, `flow_minigame_type`, `flow_minigame_result` |
| Cocktail | `order_by_type`, `order_customer`, `Order_Cocktail_*` ×4, `order_name`, `order_flavor`, `order_type`, `order_satisfaction`, `wait_for_task`, `Enable_InteractableObject`, `Reset_Variable` |
| Camera | `Switch_Camera`, `Cut_Camera`, `Fix_Camera`, `Reset_Camera` |
| TalkToCustomer | `SetEmotion`, `SetLookDirection`, `MoveIn`, `MoveOut` |
| Shell | `Panel_FadeIn`, `Panel_FadeOut`, `Fade_To_Summmary`, `DisplayThx`, `Can_End_Shift`, `wait_scene` |
| Save | `save_chapter_node` |

`wait_scene` กับ `Can_End_Shift` อยู่ใน `CocktailSystemManager` ตอนนี้ ทั้งที่ไม่เกี่ยวกับ cocktail

## 6. Analysis

### 6.1 Gap ต่อ Manager (ปัจจุบัน → เป้าหมาย)

**Cocktail**
- มีแล้ว `CocktailSystemManager` (4 partial), `ShakerContents`, `ShakerPanelController`, `IngredientButtonGroup`, `IngredientButtonUI` ×19, `BottleIngredientSource`, fruit tray, `VisualizeCocktail` ×2, `Post_It_Order`, `BookUI_V2`, `DrinkOrderContext`/`OrderService`/`DrinkScoringService` (plain C#)
- ย้ายเข้า Method panel + 3 ปุ่ม (ตอนนี้ผูกจาก `CocktailFlowBridge`), post-it, book + ปุ่ม Hub, scoring ทางเดียว, `SetIce`/`IceChanged` (แยกจาก `Changed` ตามเดิม เพราะ `ShakerContents.SetIce` ตั้งใจไม่ยิง `Changed` กัน UI toggle ของ Garnish เด้ง)
- ย้ายออก `_endShiftBTN` (ไป Shell), `_dialogueRunner` + Yarn writes และ `_customerRoster` (ไป TalkToCustomer), `wait_scene`, `Can_End_Shift`
- ลบ `ShakerContents` UnityEvents 8 ตัว แล้ว manager subscribe ในโค้ด
- `VisualizeCocktail` ทั้งสองตัว (screen-space `Canvas/Panel - VisurlCocktail` และ world-space บน shaker) คงไว้ใน Cocktail ตอนนี้ bridge อัปเดตตัว world-space แต่ toggle ตัว screen-space ต้องดูใน play ก่อนว่าตัวไหนแสดงจริง แล้วค่อยตัดสินว่าจะรวมหรือไม่ ยังไม่ลบ

**Minigame**
- มีแล้ว `MinigameSystemManager` (0 refs, discover `BaseMiniGame` ด้วย `GetComponents` เป็นต้นแบบที่ดี), `Canvas_MiniGame`
- ย้ายเข้า การเลือก type (`explicit > drink method > default`), 4 ปุ่ม cancel/give-up, `LastOutcome`
- ลบ `_cocktailCamera` (null อยู่แล้ว, `ResetCamera` เป็น no-op), `OnStartedMinigame`/`OnEndedGame` (ไม่มี listener), logic ล็อก shaker หลัง cancel ใน `MinigameFlowBridge` (comment อ้างถึง listener ที่ไม่มีแล้ว)

**Garnish** (ใหม่)
- มาจาก `GarnishFlowBridge` (330 บรรทัด, 20 slots) + `PlacedGlassInstance` + 3 `*ChoiceGrid` + `Panel - Granish UI` (5 children) + **Serve panel** (ย้ายจาก `Canvas/Panel - Serve` เข้า `Canvas_Garnish`)
- Serve flow ใหม่ Garnish ส่ง `ServeClicked` / `RemakeClicked` ขึ้น FlowWiring เรียก `Cocktail.Serve()` (score + commit) แล้วจึง `Flow.ServeDone()` ตอน `Serve.Exited` เรียก `Garnish.ClearGlass()` และ `Cocktail.HideOrder()`
- `ShakerPanelController._serveUI` ยังอ้าง Serve panel เดิม ต้องตัด (legacy ตรวจว่ามีใครเรียก `SetActiveServe` ก่อน)
- ตัดวงกลม grid ↔ bridge ด้วย event `PourStarted`
- `GlassPlacementZone` (บน `SystemGame/Garnish_Plate`) คงไว้ใต้ Garnish ตอนนี้ไม่มี script ไหนอ้าง จึงไม่ทำอะไรและไม่แก้ ตรวจว่า derive จาก `PlacementZoneBase` ไหม ถ้าใช่ Garnish จะผูก type กับ Placement (compile-time ไม่ใช่ instance)

**TalkToCustomer** (ใหม่)
- มาจาก `NPC_Base` (static `_registry`, static Yarn command ×4, NPC ในฉาก Walter, Owen, Isla), `TalkingWithCustomerBridge`, `CharacterData` (อยู่บน `NPCs_And_Set` อยู่แล้ว ซึ่งเป็น GameObject เดียวกับที่ manager จะอยู่)
- `SaveLoadManager.CollectNPCData/ApplyNPCData` ใช้ `FindObjectsByType<NPC_Base>` และแตะ `CurrentWaypointIndex`, `CurrentLookDirection` ตรงๆ เปลี่ยนเป็น `Snapshot()`/`Restore()` ของ manager
- รับ `YarnVariableSync` + `DialogueRunner` ref + `_customerRoster` จาก `CocktailSystemManager`
- ปัจจุบัน `CocktailSystemManager` เรียก `GetComponent<CharacterData>()` บน GameObject ของตัวเองซึ่งไม่มี และ `_customerRoster` ถูก assign แล้ว (inferred) จึงเป็นการย้ายเจ้าของ ไม่ใช่การเปลี่ยนพฤติกรรม
- roster ยังขาด Cole กับ Freya (design data อย่าเดา)
- `Demo_KeysShortCut` มี 2 ตัว (บน `[GameLoop]` และ `NPCs_And_Set`)

**Camera**
- มี `CameraController` (381 บรรทัด) + `CinimachineCameraSwitcher` (singleton) + camera ID 3 ที่ + Yarn เรียกตรง
- 3 manager ต้องการเปลี่ยน camera (Cocktail, Garnish, TalkToCustomer) ไม่ควรให้ตัวใดเป็นเจ้าของ

**Placement**
- `N_PlacementSystem` (93 บรรทัด, drag state, Update ทุกเฟรม) + `N_InputManager` (75 บรรทัด, ray/zone query, ผูก `_sceneCamera`) + `MouseIndicator` รวมเป็น `PlacementManager`
- consumer ที่พบ `DragableObject` ×14 (`public static _placementSystem` เขียนทุก `OnEnable`), `IngredientHoverDetector` (static `_inputManager`, ใช้โดย `FruitPieceInstance`, `BottleIngredientSource`), และ `PlacementZoneBase` (`VolumePlacementZone`, `SurfacePlacementZone`)
- กลไกเข้าถึง เสนอ `PlacementManager.Current` scene-scoped (set ใน `Awake`, ล้างใน `OnDestroy`, ไม่ `DontDestroyOnLoad`) แบบเดียวกับ `PauseManager` ดีกว่า inject ให้ 14 instance ผ่าน FlowWiring และไม่แย่กว่าสถานะ static ปัจจุบัน
- `N_InputManager._sceneCamera` ชี้ `CamController/Main Camera` ต้องรับจาก Camera manager (เป็น `Camera` object ไม่ใช่ camera ID)

**Flow**
- `GameLoopFSM`, `GameFlowCommands` คงไว้ State classes ไม่แตะ
- bridge ทั้ง 6 ตัวถูกแทนที่ด้วย `FlowWiring` ไฟล์เดียว ราว 100 บรรทัด อ่านแล้วเห็นทั้งหมดว่า state ไหนเรียก verb ไหน

### 6.2 ผลกระทบและความเสี่ยง

| ความเสี่ยง | ทำไม | วิธีกัน |
|---|---|---|
| UI เพี้ยนเมื่อย้าย panel ข้าม Canvas | ต่าง `CanvasScaler`, sorting order, anchor, `UIOnPosition3D` อ้าง camera ของ canvas | ตั้ง `CanvasScaler` และ sorting order ให้เหมือนกัน screenshot ก่อน/หลังทุก stage |
| Serialized reference หลุด | ลบ field แล้วค่าหาย | log ทุก field ที่ลบ ใช้ `[FormerlySerializedAs]` เมื่อเปลี่ยนชื่อ field, rename class ผ่าน Unity |
| Yarn contract พัง | ชื่อ command ผูกกับ `.yarn` | เก็บรายการ `[YarnCommand]`/`[YarnFunction]` เป็น baseline เทียบทุก stage |
| Save/load | `SaveLoadManager` แตะ NPC โดยตรง, silent replay | แก้ใน stage TalkToCustomer ทดสอบ save → load → ดู NPC และ flow step |
| ลำดับเขียน Yarn variable กับ `IsScored` | `wait_for_task` รอ `IsScored` ตอนนี้ score แล้วเขียน var ใน call เดียว | `OrderScored` ต้องเป็น event แบบ synchronous ห้าม defer ข้ามเฟรม |
| Ice round-trip ผ่าน event | `ToggleIce` ตอนนี้ตั้ง flag + visual พร้อมกัน | event synchronous ทั้งเส้นและตรวจว่ากดซ้ำเร็วๆ ไม่ desync |
| Nested prefab | `CamController`, `Panel - BookUI` เป็น nested prefab | เช็กว่ามี scene อื่นใช้ไหมก่อนแก้ |
| Execution order | ตัด `EnsureBuilt` | `[DefaultExecutionOrder(-100)]` บน `GameLoopFSM` |
| Preferences ไม่ถูก inject ทัน | `Orders` สร้างใน `Awake` ด้วย preferences ตั้งแต่ต้น | `OrderService` อ่าน preferences ผ่าน property ที่ตั้งทีหลังได้ และ log error ถ้า Yarn เรียกก่อน inject |

### 6.3 สิ่งที่ยังไม่รู้ (ต้องตรวจก่อนลงมือ)

- `Day1_Demo_EventNabLab.yarn` มี flow/camera command 23 จุดที่ยังไม่ได้อ่านรายละเอียด
- F6 (Give-up ค้างใน 2.2) ยังไม่ได้ยืนยันด้วยการรัน
- `Canvas/Panel - VisurlCocktail` กับตัว world-space ตัวไหน render จริง
- `RecipeBook` (sprite null ทั้งหมด) ใช้อยู่ไหม ยังไม่ได้ตัดสิน ไม่แตะ
- `GlassPlacementZone` derive จาก `PlacementZoneBase` ไหม
- dialogue canvas อยู่นอก prefab ย้ายเข้ามาแล้วกระทบ `SaveLoadManager` (`FindFirstObjectByType<DialogueRunner>`) หรือไม่

## 7. แผนงาน (1 stage = 1 commit, ทุก stage เทียบ baseline)

**Stage 0 · Baseline** สร้าง branch `GameFlow/Managers` จัดเก็บ (1) wiring dump (2) รายการ Yarn command/function (3) play-mode run เรียก flow command ตามลำดับ log `flow_phase`/`flow_step`/console error (4) screenshot ทุก panel

**Stage 1 · Subtract** ย้าย dev scripts ไป `[Debug]`, แก้ hotkey ชน, `DefaultExecutionOrder` + ลบ `EnsureBuilt`, ครอบ `flow_*` ด้วย `IsSilentReplay`, ลบ slot/field ที่ null และไม่มีใครใช้ (`MinigameSystemManager._cocktailCamera`, `ShakerPanelController` serve/ice flags), แก้ comment เก่า **ไม่ลบ object ใดในรายการ object ตาย** เพราะตัดสินให้คงไว้ทั้งหมดแล้ว

**Stage 2 · (ข้าม)** `FlowWiring` ที่ยังไม่มี wiring จริงเป็นแค่ class ว่าง ไม่มีอะไรให้เทียบ baseline จึงย้ายการสร้างไปรวมกับ Stage 4 ที่เป็น manager ตัวแรกซึ่งต้องผูก state เข้ากับ verb

**Stage 3 · Placement Manager** (เสร็จ) ไม่รวม `N_PlacementSystem` กับ `N_InputManager` เป็น class เดียว เพราะอยู่ GameObject เดียวกันและ ref กันเองอยู่แล้ว การรวมแก้ข้อมูล serialized โดยไม่ลด wiring และ trace กับ drag simulation จับความต่างไม่ครบ จึงทำเท่าที่ได้ผล: `N_PlacementSystem.Current` แบบ scene-scoped (set ใน `Awake`, ล้างใน `OnDestroy`) พร้อม `.InputManager` เป็นทางเข้าเดียว แทน `public static DragableObject._placementSystem` กับ `FindFirstObjectByType` ใน `OnEnable` และ static cache ใน `IngredientHoverDetector` (ตัวเลขเดิม "ลบ Find ×14+1" ผิด static ตัวเดียวถูกเติมครั้งเดียว Find จริงเกิดแค่ครั้งแรก) ถ้าต้องการรวม class ค่อยทำทีหลังได้

**Stage 4 · Cocktail Manager** (ทำเป็น commit ย่อย)
- 4f (เสร็จ) scoring ทางเดียว: ให้คะแนนตอนเปลี่ยน Serve → Talking (`OpenBar.Sub.OnTransition`) ไม่ใช่ตอน Exited เพราะ Exited เกิดตอน remake ด้วย และ `ServeDrink()` กันเรียกซ้ำด้วย `IsScored` ผลคือ `ServeDone()` ตรงๆ (`<<flow_serve_done>>`, hotkey 7) เขียน `$task_done` ลง Yarn ถูกต้องแล้ว (เดิมได้แค่ `IsScored`)
- 4d (เสร็จ) `ShakerContents` UnityEvent 9 call (Changed 2, Cleared 3, OnFulled 4) ย้ายเป็น listener ในโค้ดที่ `CocktailFlowBridge` ลำดับเดิม ส่วน `IsCocktailFull` เรียกใน `ShakerContents.RaiseChanged()` ก่อน `Changed` เสมอ ปุ่ม book ต้องมี ref ใหม่ `_btnBook` ชั่วคราวจนกว่า BookPanel จะเป็นเจ้าของปุ่ม
- 4a (เสร็จ) ไม่ตั้งชื่อ `IngredientShelf` ใหม่ ใช้ `IngredientButtonGroup` เดิม: เก็บขวดจาก `IngredientButtonUI` ใต้ตัวมันเองอัตโนมัติ (รายการใน scene จาก 20 เหลือ 2 คือ shaker กับปุ่ม book ที่อยู่นอกกลุ่มขวด) และ `CocktailSystemManager.Awake` ส่ง `ShakerContents` ให้ทุกขวดผ่าน `BindShaker` ขวดที่ถือ shaker ตั้งแต่เริ่มจาก 3 เป็น 19 จาก 19 เลิกค้นหาตอนเทครั้งแรก ไม่รวม writer เป็นตัวเดียวในรอบนี้: `SetInteractable` กับ `EnableInteractablePrepareDrinksPhase` ต่างกันที่ fruit tray hijack จึงเปลี่ยนเป็น API เดียวไม่ได้โดยไม่เปลี่ยนพฤติกรรม ปล่อยให้เป็นงานของ FlowStep/presenter ใน Stage 8
- 4b `Canvas_Cocktail` + Method panel เป็นเจ้าของปุ่มตัวเอง ยังไม่ทำ
- 4c book ยังไม่ทำ (post-it ย้ายเข้า `Canvas_Cocktail` แล้ว) ส่วน `VisualizeCocktail` ตัดสินแล้ว: `Canvas/Panel - VisurlCocktail` (screen-space) ไม่มีโค้ดหรือ event ใดเปิดมัน และเมื่อบังคับเปิดมันเล็กจนถูกปุ่ม Shake บัง (ดู `screens_baseline/visualize_*.jpg`) จึง**ลบทิ้ง** พร้อม `CocktailFlowBridge._panelVisualCocktail` เหลือตัว world-space บน shaker ตัวเดียว (เสร็จ)
- 4e `SetIce`/`IceChanged` เป็น API ของ Cocktail ย้ายไปทำพร้อม Stage 6 เพราะผู้เรียกคือ Garnish
- `FlowWiring` ไม่จำเป็นใน stage นี้ bridge เดิมทำหน้าที่ seam ได้ จะรวมเป็นไฟล์เดียวตอนล้าง bridge (Stage 9)

**Stage 5 · Minigame Manager** ย้ายการเลือก type, cancel buttons, `LastOutcome` เข้ามา แก้ F6 หลังจำลองให้เห็นก่อน

**Stage 6 · Garnish Manager** แยก `GarnishManager` + `Canvas_Garnish` (tabs, ice toggle เป็น component ของ panel เอง, **Serve panel ย้ายเข้า**) + `GarnishChoice`, `PlacedGlassInstance` เลิก static, grid หา manager ด้วย `GetComponentInParent`, `GlassPlacementZone` อยู่ใต้ Garnish, ตัด `ShakerPanelController._serveUI`

**Stage 7 · TalkToCustomer Manager** instance registry แทน static dict, ชื่อ Yarn command เดิม, `Snapshot()`/`Restore()` ให้ `SaveLoadManager`, รับ `YarnVariableSync` + `DialogueRunner` + roster, `Cocktail.UsePreferences(...)` ผ่าน FlowWiring, `OrderScored` → `Customer.CommitOrderResult` ทดสอบ save → load

**Stage 8 · Camera Manager** รวม ID table เดียว, Yarn camera command ผ่าน manager, `Instance` เหลือเฉพาะที่ Yarn ใช้, ส่ง `Camera` ให้ Placement

**Stage 9 · Shell + cleanup bridge** `LoadSceneButton`, `Canvas_Shell`, ลบ bridge ที่ว่างแล้วทีละตัว (`TalkingWithCustomerBridge`, `ServeFlowBridge`, `UIFlowBridge`, `CocktailFlowBridge`, `MinigameFlowBridge`, `GarnishFlowBridge`)

**Stage 10 · (optional) Level 1 เป็น enum** ลดโค้ดราว 130 บรรทัด แต่ L1 ถูก Yarn คุมอยู่แล้วโดยพฤตินัย มีค่าน้อย

### ตัวชี้วัดหลังจบ (ประมาณการ ต้องนับซ้ำด้วย dump เดิม)

- script บน `[GameLoop]` root: 10 → 3 (`GameLoopFSM`, `GameFlowCommands`, `FlowWiring`)
- slot จาก root ไป child: 58 → ต่ำกว่า 10
- writer ต่อ object ที่ใช้ร่วม: 3–6 → 1
- Inspector UnityEvent: 28 → ~18 (เหลือเฉพาะ bottle click ใน prefab เดียว)
- runtime `Find` บน hot path และ public static ที่ซ่อนอยู่: ~30 → 0

## 8. Decision log (2026-10-13)

| # | ตัดสินใจ | หมายเหตุ |
|---|---|---|
| D1 | FSM = Level 2–3, Level 1 ให้ Yarn | ยืนยันแล้ว |
| D2 | Serve panel อยู่ `Canvas_Garnish` | ยืนยันแล้ว |
| D3 | Camera เป็น Manager แยก (ไม่มี canvas) | ยืนยันแล้ว |
| D4 | Yarn variable writer + `DialogueRunner` ref ย้ายไป TalkToCustomer | ยืนยันแล้ว |
| D5 | **Ice อยู่ที่ Cocktail** ไม่ใช่ Garnish เพราะเป็นเงื่อนไขตอนเสิร์ฟ | ยืนยันแล้ว แทนข้อเสนอเดิม |
| D6 | `CharacterData` อยู่ใน TalkToCustomer (คงไว้ ไม่ลบ) | ยืนยันแล้ว |
| D7 | `PlacementSystem` คงไว้ แยกเป็น Manager ของตัวเอง | ยืนยันแล้ว |
| D8 | `Canvas/Panel - VisurlCocktail` อยู่กับ Cocktail Manager (คงไว้ ไม่ลบ) | ยืนยันแล้ว **แทนที่ด้วย D10** |
| D9 | `GlassPlacementZone` อยู่กับ Garnish (คงไว้ ไม่ลบ) | **ตีความจากบรรทัดที่ไม่มีประธาน** ต้องยืนยัน |
| D10 | ลบ `Canvas/Panel - VisurlCocktail` (screen-space) เหลือ world-space ตัวเดียว ลบ `_panelVisualCocktail` ด้วย | ยืนยันแล้ว 2026-10-13 หลังตรวจว่าไม่มีอะไรเปิดมัน |

### ยังเปิดอยู่ (มีค่า default)

- **Shell** (Hub, EndShift, Thx, Transition) เป็น `Canvas_Shell` ไม่มี manager (default ยังไม่ได้ยืนยัน)
- **Placement เข้าถึงผ่าน `PlacementManager.Current`** แบบ scene-scoped (default)
- **`RecipeBook`** ไม่แตะจนกว่าจะตัดสินใจ

## 9. Non-goals

- HSM state classes, `StateMachine`, transition table L2–3
- ชื่อ `[YarnCommand]`/`[YarnFunction]`
- rename API ที่สะกดผิด (`CinimachineCameraSwitcher`, `ResetRotaionAndMovement`, `SelectStiring`)
- DI container / event bus
- bottle internals (`ScaleOnHover`, `HoverTooltip`, `UIPointerSound`)
- งาน Glass Freedom / Garnish restructure ที่ล็อกไปแล้ว
