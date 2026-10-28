# แผน: ลดคำสั่งที่ Writer ต้องเขียน (กล้อง/ล็อคบาร์ย้ายไปหลังบ้าน)

**สถานะ:** แผน รออนุมัติจากเจ้าของ เขียนเมื่อ 2026-10-28 ให้ Sonnet 5.5 ทำตาม
**อ่านก่อน:** `CLAUDE.md` (กติกาการทำงาน), `Assets/[02]Script/Hierarchical State Machine/FlowWiring.cs`, `Docs/YarnCommands/README.md`

## เป้าหมาย
Writer เขียนแค่ว่าตอนนี้อยู่ช่วงไหนของวัน (`<<flow_open_bar>>`, `<<flow_prepare_drinks>>`, `<<flow_close_bar>>`, `<<flow_next_day>>` …) ส่วนการตั้งกล้องและการล็อค/ปลดล็อคบาร์ให้ `FlowWiring` ทำเองตาม Phase

ตัวอย่างบรรทัดที่ writer ไม่ต้องเขียนอีกแล้ว:
```
<<Enable_InteractableObject SystemGame false>>
<<Fix_Camera CamController true>>
<<Reset_Camera CamController>>
<<Cut_Camera CamController MainCam>>      // ตอนเริ่มวันเท่านั้น
```

## การตัดสินใจของเจ้าของ (2026-10-28)
| เรื่อง | ตัดสินใจ |
|---|---|
| หมุนกล้องด้วยเมาส์ตอนชง | **หมุนได้เฉพาะตอนเลือกวัตถุดิบ** (2.1 AddIngredient บนช็อตบาร์ ขณะ shaker ยังไม่เต็ม) **ช็อต shaker และมินิเกม 2.2 ขยับไม่ได้** ช่วงอื่นทั้งหมด fix (ทับ ADR 0002 ต้องเขียน amendment) |
| `<<flow_before_open_bar>>` | **ไม่ต้องมี** BeforeOpen เริ่มเองตอนต้นวันอยู่แล้ว (หลัง `Panel_Transition` หรือหลัง `<<flow_next_day>>`) |
| คำสั่งเดิม `Enable_InteractableObject` / `Fix_Camera` / `Reset_Camera` | **ลบคำสั่งทิ้ง** ทั้งจากโค้ดและจาก yarn |
| `<<Reset_Camera CamController>>` โดยเฉพาะ | **ลบ** การหันกล้องกลับหน้าตรงเป็นงานของหลังบ้านทั้งหมด: เริ่มวัน (`OnBeforeOpenEntered` → `ResetLook`), เข้า Prepare (`ResetLook` ที่มีอยู่), ออกจาก Prepare (`SetLookFixed(true)` หันหน้าตรงเอง) จึงไม่มีจุดไหนที่ writer ต้องสั่ง |
| `Switch_Camera` / `Cut_Camera` | **เก็บไว้** เป็นเครื่องมือกำกับภาพของ writer (CenterCam, NearLeftCam ฯลฯ) ลบเฉพาะบรรทัดตั้งกล้องตอนเริ่มวัน |

## สภาพตอนนี้ (สำรวจแล้ว)
หลังบ้านทำส่วนใหญ่อยู่แล้ว บรรทัดใน yarn จึงซ้ำ:
| ตอน | `FlowWiring` ทำอยู่แล้ว | ที่ขาด |
|---|---|---|
| Event (เริ่มวัน + `flow_check_events`) | `LockBar()` = ล็อควัตถุดิบ, fix กล้อง, ปิดหนังสือ | ยังไม่ตัดกล้องไป MainCam ตอนเริ่มวัน |
| 1 Talking | ล็อควัตถุดิบ, fix กล้อง (fix = หันกลับหน้าตรงด้วย), ปิดหนังสือ | – |
| 2 Prepare Drinks | `ResetLook()`, `ShowBar()` (MainCam) | ยังไม่ปลด fix (ตอนนี้ yarn สั่ง `<<Fix_Camera false>>`) |
| 2.1 AddIngredient | เปิดวัตถุดิบ | – |
| 3 Garnish | `ShowGarnish()`, ล็อควัตถุดิบ | – |
| ออกจาก 4 Serve | `ShowBar()` (MainCam) | **เสิร์ฟแล้วกลับ MainCam อัตโนมัติอยู่แล้ว** แค่ต้องยืนยันใน Play |
| เสิร์ฟเสร็จ (`CommitTaskResult`) | `EnableButtonInYarn(false)` ล็อควัตถุดิบ | – |

ปัญหาที่ต้องแก้ด้วย: `CocktailSystemManager.WaitForTask()` เรียก `EnableButtonInYarn(true)` ถ้า writer เรียก `<<wait_for_task>>` ตอนยังอยู่ Talking บาร์จะปลดล็อคทั้งที่ยังไม่ได้ชง AddIngredient เปิดวัตถุดิบเองอยู่แล้ว จึงลบบรรทัดนี้ได้

---

## Stage 1: หลังบ้านรับงานไป (ยังไม่ลบคำสั่ง yarn เดิมยังทำงาน)

1. `Assets/[02]Script/CameraControl/CameraManager.cs`
   - เพิ่ม `public void CutToBar() => CutToCamera(_barShot);` ไว้ใต้ `ShowBar()` พร้อม `/// <summary>` สั้นๆ ว่า "The bar shot with no blend: the start of a day."
2. `Assets/[02]Script/Hierarchical State Machine/FlowWiring.cs`
   - Awake: `_gameLoop.BeforeOpen.Entered += OnBeforeOpenEntered;` และถอดใน OnDestroy (ในบล็อก `_gameLoop.BeforeOpen != null` ที่มีอยู่แล้ว)
   - handler ใหม่ (วางในหมวด Before Open / Close):
     ```csharp
     // A new day always opens on the bar, look fixed and forward. Not on Event: flow_check_events
     // re-enters Event mid-story and must not move the writer's shot.
     private void OnBeforeOpenEntered()
     {
         _cameras?.SetLookFixed(true);
         _cameras?.ResetLook();
         _cameras?.CutToBar();
     }
     ```
     ตรวจก่อนว่า `BeforeOpenPhase` ยิง `Entered` ก่อน Event ลูกของมัน (StateBase) ถ้าลำดับกลับกันก็ยังใช้ได้ เพราะ Event ไม่ยุ่งกับกล้อง
   - กฎ mouse look ที่เดียว: helper ใหม่ (วางใต้ `InMinigameStep()` และเขียน `InAddIngredientStep()` แบบเดียวกับ `InMinigameStep()`)
     ```csharp
     // Mouse look only while picking ingredients on the bar shot. Fixed on the shaker shot
     // (shaker full), in the minigame and everywhere else; fixing also snaps the view forward.
     private void RefreshLook()
     {
         bool free = InAddIngredientStep() && _shakerContents != null && _shakerContents.HasRoom;
         _cameras?.SetLookFixed(!free);
     }
     ```
     เรียก `RefreshLook()` ที่:
     - ท้าย `OnAddIngredientEntered`: เข้าครั้งแรกจะหมุนได้ ถ้ากลับจากมินิเกมขณะ shaker เต็ม ยังคง fix
     - `OnMinigameEntered`: fix
     - `OnShakerFull`: fix ก่อน `ShowShaker()`
     - `OnShakerCleared`: ปลดอีกครั้งเฉพาะเมื่ออยู่ 2.1 helper เช็คให้เอง จึงปลอดภัยแม้ Cleared ยิงจากที่อื่น
     - `OnMethodResetClicked` (หลัง `ShowBar()`)
   - `OnPrepareDrinksExited`: เพิ่ม `_cameras?.SetLookFixed(true);` (fix แล้วหันหน้าตรงเองก่อนไป Garnish)
   - ลำดับ: `OnShakerFull` ยิงระหว่างอยู่ 2.1 ก่อน state เปลี่ยน `HasRoom` เป็น false แล้ว จึงได้ fix ถ้า `HasRoom` ยังไม่อัปเดตตอน event ยิง ให้ใช้ `SetLookFixed(true)` ตรงๆ ใน `OnShakerFull` แทน
   - แก้ header comment ของ class:
     - BeforeOpen: "cuts to the bar shot, look fixed"
     - 2.1: "mouse look only here, while the shaker has room"
     - 2.2 และช็อต shaker: "look fixed"
     - Serve: "leaving it returns to the bar shot"
   - **ไม่ต้อง** สลับกล้องตอน Talking Entered: ลูกค้าคนแรกหลัง `flow_open_bar` ต้องเก็บช็อตที่ writer ตั้งไว้ ส่วนหลังเสิร์ฟ `OnServeExited` → `ShowBar()` ทำให้แล้ว
3. `Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnTask.cs`
   - `WaitForTask()`: ลบ `EnableButtonInYarn(true);` เพิ่ม comment หนึ่งบรรทัด: "The flow opens the bottles in step 2.1; waiting must not unlock the bar from Talking."

**ตรวจ:** compile (`read_console` ไม่มี error) แล้ว Play `PLAYTEST_Demo` หนึ่งรอบ (ดูวิธีรันใน `Docs/YarnCommands/README.md`) yarn ยังมีคำสั่งเดิม ผลต้องเหมือนเดิม

## Stage 2: ลบคำสั่งทิ้งและเก็บกวาด yarn

1. โค้ด
   - `CameraManager.cs`: ลบ `Yarn_FixCamera` และ `Yarn_ResetCamera` (รวม attribute) แก้ class summary ให้เหลือแค่ `Switch_Camera` / `Cut_Camera` และบอกว่าการ fix/reset กล้องเป็นของ game flow
   - `CocktailSystemManager.YarnTask.cs`: ลบ `Yarn_EnableInteractable` (รวม attribute และ doc comment) แก้ header comment ของไฟล์ (บรรทัด 4–7 ที่พูดถึง `<<Enable_InteractableObject ...>>`) **เก็บ** `EnableButtonInYarn` ไว้ เพราะ `CanEndShift` และ `CommitTaskResult` ยังใช้
2. yarn ทุกไฟล์ใน `Assets/[06]Dialogue/YarnScript/Demo/` (มีแค่ไฟล์ในโฟลเดอร์นี้ที่อยู่ใน `Bar_410Demo.yarnproject`)
   - ลบทุกบรรทัด `<<Enable_InteractableObject ...>>`, `<<Fix_Camera ...>>`, `<<Reset_Camera ...>>`
   - ลบ `<<Cut_Camera ... MainCam>>` / `<<Switch_Camera ... MainCam>>` / `<<Switch_Camera ... "MainCam">>` **เฉพาะ** ที่อยู่ในบล็อกตั้งกล้องตอนต้น node เริ่มวัน (ก่อนบรรทัดพูดแรก) และที่อยู่ติดกับ `wait_for_task` ซึ่งหลังบ้านทำแทนแล้ว ส่วน `Switch_Camera` อื่นๆ (CenterCam, Near*, FarRightCam, SummaryCam และ MainCam กลางเรื่อง) เป็นการกำกับภาพ **ห้ามลบ**
   - ไฟล์และจุดที่เจอ (ตรวจซ้ำด้วย grep ก่อนแก้):
     - `Day1_Demo_EventNabLab.yarn`: `D1_NL_00_Start` บรรทัดตั้งกล้อง 4 บรรทัดแรก และบล็อกก่อน/หลัง `wait_for_task` 3 จุด (Isla, Owen, Owen ครั้งที่สอง)
     - `Day1_Demo.yarn` (ใน `Demo/`): แบบเดียวกัน
     - `PLAYTEST_Demo.yarn`:
       - `PT_D1_Start` และ `PT_D2_Start` มีบล็อกตั้งกล้องตอนต้น node และ comment "The bar is locked and the camera fixed…" ให้ลบทั้งหมด
       - `PT_MakeDrink`: `Fix_Camera false` และบล็อกหลัง `wait_for_task`
       - `PT_D2_Owen`: แบบเดียวกัน
       - หัวไฟล์ "shape of a day" เพิ่มบรรทัด: กล้องและการล็อคบาร์เป็นงานของหลังบ้าน (เริ่มวันตัดไป MainCam, ชงหมุนได้, เสิร์ฟแล้วกลับ MainCam) writer ใช้แค่ `Switch_Camera` / `Cut_Camera` กำกับภาพ
       - "Every command … is used once" ยังจริงอยู่ เพราะคำสั่งที่ลบไม่อยู่ใน README แล้ว
     - `TEST_AllCommands.yarn`: `TEST_Camera` เหลือแค่ Switch/Cut, `TEST_Misc` ลบส่วน Enable_InteractableObject 3 บรรทัดและ `(TEST)` ที่คู่กัน
   - **ไม่แตะ** `Assets/[06]Dialogue/YarnScript/Day1_Demo.yarn` (นอก `Demo/`) ไฟล์นี้ไม่อยู่ใน yarn project ใด เป็นสำเนาเก่า ให้รายงานเจ้าของว่ายังมีคำสั่งเก่าอยู่ในนั้น
   - หลังแก้: `grep -rnE "Enable_InteractableObject|Fix_Camera|Reset_Camera" "Assets/[06]Dialogue/YarnScript/Demo" "Assets/[02]Script"` ต้องไม่เจออะไร (ยกเว้น comment ที่ตั้งใจเขียน)
3. Docs
   - ลบ `Docs/YarnCommands/Enable_InteractableObject.md`, `Fix_Camera.md`, `Reset_Camera.md`
   - `Docs/YarnCommands/README.md`: ลบสามแถวออกจากตาราง และเพิ่มหัวข้อสั้นๆ **"หลังบ้านทำให้เอง (ไม่ต้องเขียน)"** เป็นตารางตาม Phase: เริ่มวัน → ตัด MainCam + fix + ล็อคบาร์ / Talking → fix + ล็อค / เลือกวัตถุดิบ → MainCam + หมุนได้ / shaker เต็ม + มินิเกม → PrepareCam + fix / Garnish → GranishCam + fix / เสิร์ฟเสร็จ → MainCam + ล็อค
   - `Switch_Camera.md` / `Cut_Camera.md`: หมายเหตุว่าเริ่มวันและหลังเสิร์ฟกล้องกลับ MainCam เอง
   - เอกสารเก่าใน `Docs/` (refactor plans, `Docs/baseline/`) เป็นประวัติ **ไม่ต้องแก้**
4. `ProjectSettings/Packages/dev.yarnspinner/Assembly-CSharp-generated.ysls.json` จะสร้างใหม่เองหลัง compile ตรวจว่าไม่มีสามคำสั่งนี้แล้ว

**ตรวจ:** compile และ import yarn project ต้องไม่มี error/warning ใหม่ใน `read_console` (Yarn ฟ้องคำสั่งที่ไม่รู้จัก)

## Stage 3: Play test (Unity MCP)
รัน `PLAYTEST_Demo` ครบ 2 วัน โดยตั้ง `FlowWiring` Day Schedule = `DaySchedule_PlayTest` แล้วเปลี่ยนกลับเป็น `DaySchedule_Demo` เมื่อเสร็จ ใช้ driver แบบ Stage 6 ใน `Docs/Bar410_BeforeOpen_Plan.md`

ตรวจ `CameraManager.ActiveCameraId`, `CameraController.IsFixedCamera` และสถานะ interactable ของ `IngredientButtonGroup`:

| จุด | กล้อง | fix | วัตถุดิบ |
|---|---|---|---|
| หลัง transition (ต้นวัน 1) | MainCam | true | ล็อค |
| `flow_check_events` กลางบท | ไม่เปลี่ยนจากที่ writer ตั้ง | true | ล็อค |
| Talking (หลัง `flow_open_bar`) | ช็อตของ writer (CenterCam) | true | ล็อค |
| `flow_prepare_drinks` → AddIngredient | MainCam | **false** | ใช้ได้ |
| shaker เต็ม (ช็อต shaker) | PrepareCam | true | ล็อค |
| มินิเกม 2.2 | PrepareCam | true | – |
| ยกเลิกมินิเกมกลับ 2.1 (shaker ยังเต็ม) | PrepareCam | true | – |
| Reset ใน Method panel | MainCam | false | ใช้ได้ |
| Garnish | GranishCam | true | ล็อค |
| เสิร์ฟ → Talking (ก่อน yarn สั่งบรรทัดถัดไป) | MainCam | true | ล็อค |
| ต้นวัน 2 | MainCam (cut) | true | ล็อค |

ทดสอบเพิ่ม:
- `<<wait_for_task>>` ตอนยังอยู่ Talking ต้องไม่ปลดล็อควัตถุดิบ
- remake จาก Garnish ต้องกลับไปเป็น fix false
- เปิด `DaySchedule_Demo` (วันจริง `D1_NL_00_Start`) เล่นจนถึง `wait_for_task` ของ Isla แล้วตรวจว่าไม่มี error

ยังไม่ได้ทดสอบ (ให้เขียนไว้ในรายงาน):
- ภาพจริงบนจอ
- เมาส์หมุนจริง

## Stage 4: เอกสาร
- `Docs/adr/0002-*.md`: เพิ่มหัวข้อ `## Amendment (2026-10-28)` บอกว่ากล้องหมุนด้วยเมาส์ได้เฉพาะตอนเลือกวัตถุดิบ (2.1 บนช็อตบาร์ ขณะ shaker ยังไม่เต็ม) ช็อต shaker และมินิเกมยัง fix และ game flow (`FlowWiring`) เป็นคนเปิด/ปิด Yarn สั่งไม่ได้แล้ว
- `CLAUDE.md`:
  - หมวดสถาปัตยกรรม เพิ่มบรรทัด: "**Writer เขียนแค่คำสั่ง flow** กล้องเริ่มต้นและการล็อคบาร์เป็นงานของ `FlowWiring` ตาม Phase yarn ใช้ `Switch_Camera` / `Cut_Camera` กำกับภาพเท่านั้น"
  - แก้ 0002 ในหมวด ADR ให้บอกว่ามี amendment
  - หมวดงานที่กำลังทำ เพิ่มสถานะของงานนี้
  - แก้บรรทัด BeforeOpen กับ TEST yarn ที่ยังเขียนว่า "ยังไม่ commit" (commit แล้วใน `72d607a`, `3b2f37f`)
- ไฟล์นี้: เพิ่มหัวข้อ "ผลทดสอบ" ท้ายไฟล์

## กติกาสำหรับคนทำ (จาก CLAUDE.md)
- คุยกับเจ้าของเป็นภาษาไทย ส่วนโค้ด comment และ commit message เป็นภาษาอังกฤษ
- ทำทีละ Stage และ compile-check ทุก Stage ก่อนไป Stage ถัดไป
- **ห้าม commit** จนกว่าเจ้าของสั่ง และถ้าสั่งให้แยก commit ตามเรื่อง ไม่รวม `MainMenu_V2.unity` และรูป `SPR_*` ที่ไม่เกี่ยวข้อง
- ตอนจบให้บอกทุกไฟล์ที่แก้: path, ทำอะไร, และ GameObject (ถ้าอยู่ใน Scene/Prefab) งานนี้ไม่ควรต้องแก้ Scene หรือ Prefab ถ้าต้องแก้ ให้หยุดถามก่อน
- งานนี้ไม่มี Component ใหม่

## ผลทดสอบ (2026-10-28)
ทำครบ Stage 1-4 compile ผ่านทุก Stage (`read_console` ไม่มี error เหลือแต่ warning เดิมของโปรเจค) Play test ด้วยโค้ด: ตั้ง `FlowWiring` Day Schedule เป็น `DaySchedule_PlayTest` ตอน Play (ผ่าน reflection ไม่แตะ Scene) เดินบท `PLAYTEST_Demo` ด้วย coroutine (`DialogueControls.RequestNextLine()` + เลือก option ด้วย reflection) ส่วน step ชงสั่งด้วย `GameFlowCommands` และ `ShakerContents` ตรงๆ หลังเสร็จหยุด Play Day Schedule กลับเป็น `DaySchedule_Demo` Scene ไม่ dirty

| จุด | กล้อง | fix | วัตถุดิบ (bottle เปิด/ทั้งหมด) | ผล |
|---|---|---|---|---|
| หลัง transition (ต้นวัน 1) | MainCam | true | 0/11 | ผ่าน |
| `flow_check_events` กลางบท (writer ตั้ง CenterCam ก่อน) | CenterCam | true | 0/11 | ผ่าน |
| Talking หลัง `flow_open_bar` (Isla, CenterCam) | CenterCam | true | 0/11 | ผ่าน |
| `wait_for_task` ตอนอยู่ Talking | CenterCam | true | 0/11 | ผ่าน ไม่ปลดล็อค |
| `flow_prepare_drinks` → AddIngredient | MainCam | false | 11/11 | ผ่าน |
| ใส่ 1 หน่วย (ยังมีที่ว่าง) | MainCam | false | – | ผ่าน |
| shaker เต็ม | PrepareCam | true | 0/11 | ผ่าน |
| มินิเกม 2.2 | PrepareCam | true | – | ผ่าน |
| ยกเลิกมินิเกมกลับ 2.1 (shaker ยังเต็ม) | PrepareCam | true | – | ผ่าน |
| Reset ใน Method panel | MainCam | false | 11/11 | ผ่าน |
| Garnish | GranishCam | true | 0/11 | ผ่าน |
| remake จาก Garnish | MainCam | false | 11/11 | ผ่าน |
| เสิร์ฟ → Talking | MainCam | true | 0/11 | ผ่าน |
| ต้นวัน 2 | MainCam | true | 0/11 | ผ่าน |
| Close → BeforeOpen (ตั้ง CenterCam + ปลด fix ก่อน) | MainCam | true | – | ผ่าน (cut ทำงานจริง) |

- `DaySchedule_Demo` (วันจริง `D1_NL_00_Start`) เล่นจนถึง `wait_for_task` ของ Isla แล้วเสิร์ฟ ไม่มี error ใหม่
- ข้อสังเกต: การเปลี่ยนวันตอนทดสอบ cut ใช้ `GameFlowCommands.NextDay()` ตรงๆ จึงมี Error "DaySchedule_PlayTest has no row for day 3" (คาดไว้ ไม่เกี่ยวกับงานนี้)
- warning ที่เห็น (เดิมทั้งหมด): `BackCam` ลงทะเบียนซ้ำ, ไม่พบ UiSFX `Book_Close`, มี EventSystem 2 ตัว, `Canvas_ShakingMinigame` AnimationEvent ไม่มีชื่อฟังก์ชัน, "Leaving the conversation with no order placed", `MixingMinigame` StartGame ignored (เดินเร็ว)
- ยังไม่ได้ทดสอบ: ภาพจริงบนจอ, เมาส์หมุนจริง, ชงด้วยมือจริง (ใช้คำสั่งสังเคราะห์), remake จาก Serve, controller
- `Assets/[06]Dialogue/YarnScript/Day1_Demo.yarn` (นอก `Demo/`, ไม่อยู่ใน yarn project) ยังมีคำสั่งเก่าอยู่ ไม่ได้แตะ
