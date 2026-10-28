# Bar410 — BeforeOpen Level 2 Plan

สถานะ: **Stage 1–6 เสร็จ ทดสอบใน Unity แล้ว (ยังไม่ commit)** เหลือแค่ต่อ popup กับระบบปลดล็อคตอนทำระบบนั้น · 2026-10-28
เกี่ยวกับ: [Bar410_IngredientUnlock_Plan.md](Bar410_IngredientUnlock_Plan.md) (popup ใน Event มาจากระบบนี้)

## ที่ตกลงแล้ว
| เรื่อง | คำตอบ |
|---|---|
| Level 2 ใต้ BeforeOpen | `Event` → `StoryBeforeOpen` |
| Event | เช็ค event ประจำวัน (ตอนนี้มีแค่ปลดล็อควัตถุดิบ) และ**มี popup ให้กดปิด** |
| Story | บทสั้นก่อนเปิดร้าน ไป Event ได้ (แล้วกลับมาเล่นต่อ) หรือออกไป Open |
| `$day` | ตัวแปร Yarn, **C# บวก 1 ตอน Close → BeforeOpen** |
| node ของวัน + ชื่อ Chapter | ตารางเดียวใน Inspector: วัน / node / ชื่อ Chapter |
| UI บอก Chapter/วัน | ขึ้นตอนเข้า Open ทำแบบง่าย (ข้อความกลางจอ fade) เจ้าของจะใส่หน้าตาจริงเอง |
| หลายภาษา | ต้องรองรับ แต่**พักไว้จนกว่าจะทำ Localization** ชื่อ Chapter ตอนนี้เป็น string ธรรมดา |
| เริ่มวัน | รอ `Panel_Transition` เล่นจบก่อน (ทำแล้ว ดูหัวข้อ "ทำไปแล้ว") |
| Save/Load | **พักไว้** ดู CLAUDE.md "สิ่งที่ต้องแก้" แผนนี้ไม่แตะ |

## โครง
```
BeforeOpen (L1)                        เข้า: SetInitial(Event)
 ├─ Event          เข้า: ขอคิว popup ใหม่ → คิวว่างไป Story ทันที / ปิดอันสุดท้าย → Story
 └─ StoryBeforeOpen
                   เข้าครั้งแรกของวัน: StartDialogue(node ของ $day)
                   เข้าจาก Event รอบหลัง: ไม่เริ่มบทใหม่ บทเดินต่อเอง
                   <<flow_check_events>> → Event (คำสั่งรอจน popup ปิดหมด)
                   ปุ่ม / <<flow_open_bar>> → ออกไป Open
Close → BeforeOpen : FlowWiring บวก $day ผ่าน YarnVariableSync
Open.Entered       : แสดง UI Chapter/วัน
```
state ยังเป็น C# ล้วนตามกติกาเดิม งานกับ Unity (popup, StartDialogue, UI) อยู่ที่ `FlowWiring`

## ข้อมูล (Inspector)
`SO_DaySchedule` ตาราง Odin แถวละหนึ่งวัน:

| Day | Start Node | Chapter Name |
|---|---|---|
| 1 | `D1_NL_00_Start` | (ชื่อ Chapter) |

- ไม่มีแถวของ `$day` → log error และไม่เริ่มบท (ไม่เดา node)
- **Localization (พักไว้):** `Chapter Name` เป็น string ธรรมดา ตอนทำ Localization ให้เปลี่ยนคอลัมน์นี้เป็น key ของ String Table แทน และ UI อ่านผ่าน Localization ไม่ต้องแก้ตรรกะของ FSM
- Validate: วันไม่ซ้ำ, node มีอยู่จริงใน Yarn project

## Popup ของ Event
- Event ไม่รู้ว่า popup มีอะไร มันแค่ถามว่ามีของใหม่ไหม แล้วรอจนกว่าจะปิด
- ระหว่างรอระบบปลดล็อค (แผนปลดล็อค Stage 1–3) คิวจะว่างตลอด Event จึงผ่านไปทันที
- popup เป็น System UI ของตัวเอง ไม่อยู่ในตาราง `HubManager`

## ทำไปแล้ว (2026-10-28 ยังไม่ commit)
- `FlowWiring.Start` รอ `PanelTransition.WaitUntilFinished()` แล้วค่อย `StartDay()` ระหว่างรอจะล็อคบาร์และซ่อน Hub UI
- auto start ของ `GameLoopFSM` และ `DialogueRunner` ในฉากหลักปิดแล้ว
- `TalkToCustomerManager.StartStory()` เริ่มบทจาก `startNode` ของ runner ตอนนี้เรียกตอนเข้า BeforeOpen
- `flow_phase()` / `flow_step()` คืนค่าว่างก่อนวันเริ่ม

## Stage 1 ทำแล้ว (2026-10-28)
- `Level 2 - Before Open/`: `BeforeOpenPhase` (ย้ายมาจาก Level 1 พร้อม `BeforeOpenSub`), `EventState`, `StoryBeforeOpenState`
- ออกไป Open ต้องอยู่ใน Story (ถ้าไม่ใช่จะ warning) `GameFlowCommands.OpenBar` → `Story.RequestOpen()`
- `GameFlowCommands.CheckEvents()` + `<<flow_check_events>>` (คำสั่งรอจนออกจาก Event)
- `flow_step()` ตอบ `Event` / `StoryBeforeOpen` ตอนอยู่ใน BeforeOpen
- `FlowWiring`: Event เข้า → ล็อคบาร์ + Hub BeforeOpen + `Finish()` ทันที (ยังไม่มี popup) / Story เข้าครั้งแรก → `StartStory()`
- `GameFlowDebugHotkeys`: ปุ่ม `1` = OpenBar
- ตรวจแล้ว: compile + test transition ของ FSM ล้วนนอก Unity (เข้า→Story, check events ไม่เริ่มบทซ้ำ, Open จาก Event ถูกปฏิเสธ, วันใหม่เริ่มบทใหม่)
- ตรวจใน Unity แล้ว (2026-10-28): compile ไม่มี error; Play: รอ transition แล้วเข้า Event → Story → เริ่มบท, `<<flow_check_events>>` กลางบทกลับมาเล่นบรรทัดถัดไปและ step ยังเป็น StoryBeforeOpen, `<<flow_open_bar>>` ไป Open/Talking, เรียกซ้ำจาก Open แค่ warning, `flow_next_day` จาก Close กลับมา BeforeOpen/StoryBeforeOpen (ผ่าน `TEST_AllCommands.yarn`)
- **ยังไม่ได้ตรวจ:** ปุ่มจริงที่ผูก `OpenBar` (ยังไม่มีปุ่มในฉาก)
- ข้อสังเกต: Event ที่จบทันทีจะ transition ซ้อนใน Enter ทำให้ `Sub.OnTransition` ของ BeforeOpen แจ้งลำดับกลับกัน ตอนนี้ไม่มีใครฟัง event นี้

## Stage 2 ทำแล้ว (2026-10-28)
- `Assets/[02]Script/Day Schedule/SO_DaySchedule.cs` + asset `DaySchedule_Demo.asset` (แถว Day 1 → `D1_NL_00_Start`, ชื่อ Chapter `Day 1` เป็นค่าชั่วคราว) Odin เตือนถ้าวันซ้ำ
- `FlowWiring._daySchedule` (ตั้งทั้งใน prefab `[GameLoop]` และฉากหลัก) Story เข้าครั้งแรก → อ่าน `$day` → `StartStory(node ของแถวนั้น)` ไม่มีแถว = log error ไม่เริ่มบท ช่องว่าง = ใช้ Start Node ของ runner (ฉากทดสอบอื่น)
- `StartStory(node)` เช็คว่า node มีอยู่จริงก่อนเริ่ม ไม่มี = log error
- `YarnVariableSync.ReadDay()` / `TalkToCustomerManager.CurrentDay`
- `<<declare $day = 1>>` ใน `Day1_Demo.yarn` (อยู่ใน `InitialValues` แล้ว SaveLoadManager จะเก็บได้)
- ตรวจแล้ว: compile ไม่มี error; Play: วัน 1 เริ่ม `D1_NL_00_Start` (Start Node ของ runner ถูกเมินจริง), ตั้ง `$day` = 2 → error "no row for day 2" และไม่เริ่มบท
- การทดสอบ `TEST_AllCommands.yarn` เปลี่ยนเป็นแก้แถว Day 1 ในตาราง (README อัปเดตแล้ว)

## Stage 3 ทำแล้ว (2026-10-28)
- `FlowWiring` ฟัง `ClosingBar.Exited` → `TalkToCustomerManager.AdvanceDay()` → `YarnVariableSync.WriteDay($day + 1)`
- Close ไปได้แค่ BeforeOpen และ Exit เกิดก่อน Enter ของ state ใหม่ Story ของวันใหม่จึงอ่าน `$day` ใหม่ได้ วันแรกมาจาก `StartDay()` ไม่ผ่าน Close จึงไม่ถูกบวก
- ตรวจแล้ว: compile ไม่มี error; Play: วัน 1 เริ่ม `D1_NL_00_Start` → OpenBar → CloseBar → NextDay ได้ `$day` = 2 และ error "no row for day 2" (ยังไม่มีแถว) → เพิ่มแถว Day 3 ชั่วคราวแล้วข้ามวันอีกรอบ ได้ `$day` = 3 และเริ่มบท node ของแถวนั้น (ลบแถวชั่วคราวออกแล้ว)
- **ยังไม่ได้ตรวจ:** ทางเข้า Close จริง (ยังไม่มี UI) และการ save/load `$day` (พักไว้)

## Stage 4 ทำแล้ว (2026-10-28)
- `Assets/[02]Script/Day Schedule/ChapterTitleUI.cs` (component): `Show(text)` fade เข้า 0.5s → ค้าง 1.5s → fade ออก 0.5s (ปรับใน Inspector) ไม่บังการคลิก
- ฉากหลัก: `[GameLoop]` › `Canvas_HUD` › `Panel_ChapterTitle` (CanvasGroup + `ChapterTitleUI`) › `TXT_ChapterTitle` (TMP กลางจอ) ไม่อยู่ในตาราง `HubManager`
- `FlowWiring._chapterTitle`: `OpenBar.Entered` → `Show(Chapter Name ของแถว $day)` ไม่มีแถว/ช่องว่าง = ไม่แสดง (error ขึ้นแล้วตอน Story) การหาแถวของวันรวมเป็น `TryGetToday()`
- ตรวจแล้ว: compile ไม่มี error; Play: OpenBar → ข้อความ "Day 1", alpha ขึ้นถึง 1 แล้วกลับเป็น 0, ไม่มี error
- **ยังไม่ได้ตรวจ:** ดูด้วยตา (ตรวจผ่านค่า alpha) และฟอนต์เป็น `LiberationSans SDF` ซึ่งไม่มีตัวอักษรไทย ถ้าชื่อ Chapter เป็นภาษาไทยต้องเปลี่ยนฟอนต์
- ตั้งเฉพาะในฉากหลัก ไม่ได้ใส่ใน prefab `[GameLoop]`

## Stage 5 ทำแล้ว (2026-10-28)
- เจ้าของเลือก (2026-10-28): ทำแค่ popup + คิว ยังไม่ต่อระบบปลดล็อค (แผนปลดล็อคยังรออนุมัติ) / popup = หัวข้อ + ข้อความ + รูป (ไม่มีรูปได้) + ปุ่มปิด
- `Assets/[02]Script/UI Script/EventPopupQueue.cs` (component): `Enqueue(title, body, sprite)`, `ShowAll()` แสดงทีละอัน, ปิดอันสุดท้าย → event `AllClosed` (คิวว่าง = `AllClosed` ทันที) ตอนแสดงจะ focus ปุ่มปิด (controller กด Submit ได้) และบังการคลิกด้านหลัง มี ContextMenu "Test: queue two popups"
- ฉากหลัก: `[GameLoop]` › `Canvas_HUD` › `Panel_EventPopup` (พื้นมืด + CanvasGroup + `EventPopupQueue`) › `Window` › `TXT_Title`, `IMG_Popup`, `TXT_Body`, `BTN_ClosePopup` (instance ของ `PF_Button_Hub`, sprite `SPR_Close_btn_Normal`) ไม่อยู่ในตาราง `HubManager`
- `FlowWiring._eventPopups`: Event เข้า → `ShowAll()`; `AllClosed` → `Event.Finish()` (เฉพาะตอนยังอยู่ใน Event) ช่องว่าง = Event ผ่านทันทีเหมือนเดิม
- **ระบบปลดล็อค (ตอนทำ):** เรียก `Enqueue` ก่อนเข้า Event (เช่นตอน Close → BeforeOpen หรือใน Yarn ก่อน `<<flow_check_events>>`)
- ตรวจแล้ว: compile ไม่มี error; Play: ใส่ 2 popup ก่อนวันเริ่ม → Event รอ บทยังไม่เริ่ม ปุ่มปิดถูก focus → ปิดอันแรกขึ้นอันที่สอง → ปิดอันที่สอง popup หาย เข้า Story บทเริ่ม; กลางบทใส่ popup + `CheckEvents()` → Event แสดง popup → ปิด → กลับ Story บทเดิมยังเล่นอยู่ (ไม่เริ่มใหม่); `CheckEvents()` ตอนคิวว่างผ่านทันที
- **ยังไม่ได้ตรวจ:** คลิกเมาส์/controller จริง (ทดสอบด้วย pointer click สังเคราะห์), หน้าตา, ฟอนต์ไทย (`LiberationSans SDF`), Pause ระหว่าง popup ข้อสังเกต: เรียก `onClick.Invoke()` สองครั้งใน frame เดียวกันจากสคริปต์ทดสอบ ครั้งที่สองไม่ปิด popup (กดทีละ frame ปกติ) ยังไม่ได้หาสาเหตุ ผู้เล่นกดสองครั้งใน frame เดียวไม่ได้
- ตั้งเฉพาะในฉากหลัก ไม่ได้ใส่ใน prefab `[GameLoop]`

## Stage 6 ทำแล้ว (2026-10-28)
- เจ้าของขอ: Yarn ไฟล์ใหม่สำหรับ Play test ที่เป็นทั้งตัวอย่างการเขียนวันถัดๆ ไป แบบเดียวกับไฟล์ test คำสั่ง ใช้ทุกคำสั่ง และเป็นเดโมเล็กๆ
- `Assets/[06]Dialogue/YarnScript/Demo/PLAYTEST_Demo.yarn`: 2 วัน (วัน 1 Isla สั่งตามชื่อ / วัน 2 Walter สุ่มตามความชอบ + Owen สั่งตามประเภท) หัวไฟล์อธิบายโครงของหนึ่งวัน บรรทัด `(PT)` บอกค่าที่ควรเห็น node `PT_MakeDrink` ใช้ร่วมกัน มีตัวเลือกให้ Yarn เดินทุก step แทนการชงเอง ใช้ทุกคำสั่งใน `Docs/YarnCommands/` ยกเว้น `save_chapter_node` (บทค้างในฉากหลัก)
- `Assets/[02]Script/Day Schedule/DaySchedule_PlayTest.asset`: Day 1 → `PT_D1_Start`, Day 2 → `PT_D2_Start` รัน: เปลี่ยน `FlowWiring` Day Schedule เป็น asset นี้
- **แก้โค้ด:** `TalkToCustomerManager.StartStory` เดิมไม่เริ่มบทถ้ามีบทเล่นอยู่ `<<flow_next_day>>` จาก Yarn จึงไม่เริ่มวันใหม่เลย ตอนนี้รอบทเดิมจบแล้วเริ่ม node ของวันใหม่ `flow_next_day` จึงควรเป็นคำสั่งท้ายวัน
- **แก้ไฟล์ test:** `[PT]` / `[TEST]` ถูก Yarn อ่านเป็น markup ทำให้ชื่อผู้พูดเพี้ยน (`[character name=...]` โผล่ใน Event Log) เปลี่ยนเป็น `(PT)` / `(TEST)`
- ตรวจแล้ว (Play, เดินบทด้วยโค้ด เลือกตัวเลือก "(Test) Let Yarn walk the steps" และชงของ Owen ด้วยคำสั่ง flow จาก C#): วัน 1 BeforeOpen/Story → check events → Open/Talking → ออเดอร์ Gin Fizz → เดินทุก step ตรงตาม `(PT)` → Fail → reaction → Close → `$day` = 2 → บทวันเดิมจบแล้ว `PT_D2_Start` เริ่มเอง (Event Log ถูกล้าง) → Walter สุ่มได้ Martini → Owen `order_by_type` HighAlcohol (`order_name` ว่าง) → End Shift, Close, Fade_To_Summmary, Panel_Thx แสดง ไม่มี error ใหม่ (warning ที่เห็น: "no order placed" ตอน `flow_close_bar` จาก Talking ของเดิม, `MixingMinigame` "already in Outro" เพราะเดินเร็ว)
- **ยังไม่ได้ตรวจ:** ชงจริงด้วยมือ (Perfect/Acceptable), ภาพ/กล้อง/การเดินของ NPC, ชื่อ Chapter ที่ขึ้นตอน Open ในรอบนี้ (ตรวจไว้แล้วใน Stage 4), การแสดงผลหลังเปลี่ยนเป็น `(PT)` (compile ผ่าน แต่ยังไม่ได้รันซ้ำ), popup ใน Event (ยังไม่มีอะไรใส่คิว)

## Stage (compile-check ทุก stage)
1. **FSM:** sub-machine ใน `BeforeOpenPhase` (`Event`, `StoryBeforeOpen`) + event ออก; `GameFlowCommands.OpenBar` ใช้ได้เฉพาะตอนอยู่ใน Story; `<<flow_check_events>>`; `flow_step` ตอบ sub-state ของ BeforeOpen; ย้ายการเรียก `StartStory()` จาก BeforeOpen.Entered ไป Story เข้าครั้งแรก
2. **ตารางวัน + เริ่มบท:** `SO_DaySchedule`; `StartStory(node)` รับ node จากตารางแทน `startNode`; `<<declare $day = 1>>`
3. **Close → BeforeOpen:** บวก `$day`; ทดสอบผ่าน `GameFlowDebugHotkeys` (Close ยังไม่มีทางเข้า)
4. **UI Chapter/วัน (แบบง่าย):** ตอน Open.Entered แสดงข้อความกลางจอจากตาราง แล้ว fade เข้า-ออก แยก component เล็กๆ ให้เจ้าของเปลี่ยนหน้าตาเองได้
5. **Popup:** UI popup + คิว; Event รอปิด; ต่อกับระบบปลดล็อคเมื่อแผนปลดล็อคถึง Stage 3
6. **ทดสอบ Play:** วัน 1 ครบรอบ, `<<flow_check_events>>` กลางบท, ข้ามวันด้วย hotkey

## คำถามที่ยังเปิด
ไม่มี (ตอบครบแล้ว 2026-10-28)
