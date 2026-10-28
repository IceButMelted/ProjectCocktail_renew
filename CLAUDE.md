# Bar410 (ProjectCocktail)

<!-- ร่างโดย Claude 2026-10-25 จาก GDD, CONTEXT.md, ADR และงานที่ผ่านมา · ส่วนที่มี TODO ให้เจ้าของโปรเจคเติม -->

## เกมคืออะไร
Visual Novel แนวบาร์เทนเดอร์ 2.5D ตัวละคร เครื่องดื่ม และวัตถุดิบเป็น 2D วาดมือ ส่วนฉากเป็น 3D ผู้เล่นเป็นบาร์เทนเดอร์หน้าใหม่ในเมืองชนบทแถบยุโรป ฟังเรื่องของ NPC แล้วช่วยเขาผ่านเครื่องดื่มที่ชง

Loop ของหนึ่งวัน: คุยกับลูกค้า → รับออเดอร์ (จาก Yarn) → ชง → เสิร์ฟ → ดู reaction → คุยต่อ → จบวัน

- **เอกสาร design หลัก:** `Docs/GDD_Bar410_Master.md` (ส่วนที่ 2 คือ source of truth ของตัวเลขและกติกา)
- **ศัพท์ในโปรเจค:** `CONTEXT.md` ใช้คำตามไฟล์นี้ และเลี่ยงคำที่ไฟล์ระบุว่า _Avoid_
- **การตัดสินใจที่ทับ GDD:** `Docs/adr/`
  - **0001:** ไม่มี Phase เตรียมร้าน (Prepare) แล้ว วัตถุดิบมีครบทุกวัน GDD §6 Phase 1 และ §23 `phase1Layout` จึงล้าสมัย
  - **0002:** แก้วและการเทไม่ใช้การลากแล้ว แก้วอยู่ตำแหน่งตายตัว เลือกด้วยปุ่ม และกล้อง fix มุมเดียว GDD §11 (ลากไปเสิร์ฟ) และ §13/§20 (กล้องหันตามเมาส์) จึงล้าสมัย

## Tech
- Unity 6, URP, Yarn Spinner Plus, Text Animator (Febucci), Cinemachine, Input System, Yarn Spinner, Text Animator for Yarn Spinner, Text Animator Extras for Yarn Spinner, DOTween (Demigiant), Odin Inspector (Sirenix), Oklab.cs, Plugins/Febucci, TableForge
- หลักการโค้ด: SOLID, composition มากกว่า inheritance, Keep it simple สำหรับโค้ด
- Unity MCP (server ชื่อ `UnityMCP`) ใช้สั่ง Editor ระหว่าง Unity recompile bridge จะหลุดประมาณ 2 นาที
- ไม่มี `gh` CLI

## ฉากที่ใช้จริง
- `MainMenu_V2` → `LoadingScene` → `New Drag Drop System` (ฉากเกมหลัก)
- `PauseScene` โหลดซ้อน (additive) โดย `[PauseManager]`
- `Out/MainMenu.unity` คือเมนูเก่า (ยังเป็น Build index 0)
- ฉากอื่นใน `[05]Scenes/Deverlopment/` และฉากทดสอบใช้สำหรับทดลอง ไม่ใช่ของจริง

## สถาปัตยกรรม (ต้องรักษา)
- **Game flow:** `GameLoopFSM` (HSM 3 ระดับใน `[02]Script/Hierarchical State Machine/`)
  - state เป็น C# ล้วน ไม่รู้จัก Unity object
  - Level 1: BeforeOpen → Open → Close → BeforeOpen (เกมเริ่มที่ BeforeOpen)
  - **BeforeOpen (`BeforeOpenPhase`) มีไว้สำหรับ Event ก่อนเริ่มวัน** เช่น ปลดล็อควัตถุดิบใหม่ หรือเนื้อเรื่องก่อนเปิดร้าน ไปได้แค่ Open ผ่านปุ่ม (`GameFlowCommands.OpenBar` ผูก OnClick) หรือ Yarn `<<flow_open_bar>>` yarn ของแต่ละวันต้องเรียกคำสั่งนี้ ไม่งั้นค้างอยู่ที่ BeforeOpen
  - BeforeOpen มี Level 2: **Event → StoryBeforeOpen** (ไฟล์ใน `Level 2 - Before Open/`) เข้า BeforeOpen แล้วเริ่มที่ Event เสมอ Event แสดง popup ใน `EventPopupQueue` และรอจนปิดครบ (คิวว่างผ่านทันที ตอนนี้ยังไม่มีระบบไหนใส่คิว) Story เริ่มบทเฉพาะตอนเข้าครั้งแรกของวัน กลับไป Event กลางบทได้ด้วย `<<flow_check_events>>` (บทรอจน Event จบแล้วเล่นต่อ) และออกไป Open ได้จาก Story เท่านั้น
  - **วันเริ่มหลัง `Panel_Transition` เล่นจบ:** `FlowWiring.Start` รอ `PanelTransition.WaitUntilFinished()` แล้วค่อย `StartDay()` → BeforeOpen → Story เข้าครั้งแรก → `TalkToCustomerManager.StartStory(node)` โดย node มาจาก `SO_DaySchedule` (`DaySchedule_Demo.asset` ตาม `$day`) Start Node ของ `DialogueRunner` ไม่ถูกใช้ในฉากหลักแล้ว (ฉากหลักปิด auto start ของ `GameLoopFSM` และ `DialogueRunner` แล้ว) **`Panel_Transition` อาจเปลี่ยนรูปแบบ transition ได้ แต่ต้องรักษา behavior เดิม:** `WaitUntilFinished()` ต้องคืนเมื่อ transition เปิดฉากเล่นจบ
  - Step 1 Talking → 2 Prepare Drinks (2.1 AddIngredient ⇄ 2.2 Minigame) → 3 Garnish → 4 Serve → 1
  - Close (ClosingBarPhase) ยังไม่มี UI
- **`FlowWiring` คือที่เดียวที่ flow กับ manager มาเจอกัน:**
  - ฟัง event ของ state แล้วเรียก verb ของ manager
  - แปลง event ของ manager กลับเป็นคำสั่ง flow
  - manager ไม่รู้จัก flow และไม่รู้จักกันเอง
- **Manager แยกตามระบบ ต่างคนต่างมี Canvas ของตัวเอง:** Cocktail, Minigame, Garnish, TalkToCustomer, Camera, Placement (อยู่ใต้ `[GameLoop].prefab`)
- **Yarn สั่ง flow ผ่าน `GameFlowCommands`** (`<<flow_*>>`) ส่วนออเดอร์สั่งผ่าน `CocktailSystemManager` (`Order_Cocktail_*`, `order_by_type`)
- **ให้คะแนนที่เดียว:** ตอน Serve → Talking (`ServeDrink()` เป็น idempotent) ส่วน remake ไม่ให้คะแนน
- **มินิเกมแพ้ไม่ได้:** จบได้แค่ `Completed` หรือ `Cancelled`
- **ความสัมพันธ์อยู่ในตัวแปร Yarn `$rel_<id>` ที่เดียว** ห้าม mirror ลง ScriptableObject
- **Hub UI (เปิด/ปิดตาม Phase อย่างเดียว) ผ่าน `HubManager` บน `Canvas_HUD`** โดย `FlowWiring` เป็นคนส่ง Phase ให้ ส่วน System UI (Garnish, Minigame, Post-it ฯลฯ) ยังคุมโดย Manager ของมันเอง
- **Singleton เป็นแบบ scene-scoped** (เช่น `PauseManager`, `SoundManager`): ซ้ำแล้วทำลายตัวเอง และไม่ใช้ DontDestroyOnLoad
- **สูตรเครื่องดื่ม:** วัตถุดิบรวม = 10 หน่วยเสมอ ไม่มีสูตรที่ส่วนผสมซ้ำกันเป๊ะ ตรวจด้วย `Bar410 > Validate Cocktail Data`

## งานที่กำลังทำ
ตรงนี้ให้ Update ตัวเองในการทำทุกครั้งว่ากำลังทำอะไร
- branch `UiManager`: วางโครงสร้าง UI (merge เข้า `main` แล้ว 2026-10-28)
- แผนผัง UI ปัจจุบันอยู่ที่ FigJam board "Project Cocktails" › section "UI List" › "Table C" (อัปเดต 2026-10-28)
- เสร็จแล้วในรอบนี้: Event Log, AutoPlay + Skip-to-order, ความเร็ว/แบบการแสดงตัวอักษรใน Pause › Setting, แท็บหน้า Setting, `HubManager`
- ยังไม่ได้ทดสอบด้วยการเล่นจริง: คลิกเมาส์/controller, save/load, `*Silent*` (ยังไม่มี yarn ไหนใส่ `#timeout`), Phase Close (ยังไม่มีทางเข้า)
- เพิ่ม Phase BeforeOpen ใน Level 1 แล้ว (2026-10-28) commit `da93422`
- BeforeOpen รอ `Panel_Transition` เล่นจบก่อนเริ่ม (2026-10-28 ยังไม่ commit) แผน `Docs/Bar410_BeforeOpen_Plan.md`: Stage 1 (Level 2 Event / StoryBeforeOpen) , Stage 2 (ตารางวัน `SO_DaySchedule` เลือก node ตาม `$day`) , Stage 3 (Close → BeforeOpen บวก `$day`) , Stage 4 (ชื่อ Chapter ขึ้นตอนเข้า Open ด้วย `ChapterTitleUI`) , Stage 5 (popup ของ Event ด้วย `EventPopupQueue` ยังไม่ต่อระบบปลดล็อค) และ Stage 6 (Play test 2 วันด้วย `PLAYTEST_Demo.yarn` + `DaySchedule_PlayTest.asset`) เสร็จ compile + Play test ผ่านแล้ว ยังไม่ commit
- ไฟล์ test คำสั่ง Yarn ทั้งหมด `TEST_AllCommands.yarn` + `Docs/YarnCommands/` (2026-10-28) รันใน Play ผ่านแล้ว ยกเว้น `TEST_RealOrder` (ต้องชงจริง) ยังไม่ commit
- แผนปลดล็อควัตถุดิบ: `Docs/Bar410_IngredientUnlock_Plan.md` + ร่าง `Docs/adr/0003-ingredient-unlock.md` รออนุมัติ ยังไม่มีโค้ด
- Button Feedback (Hover/Hold/Release/Click scale ด้วย DOTween + SO preset) เสร็จ Stage 1-4 แล้ว (2026-10-28) ติดให้ปุ่มใต้ `Canvas_HUD` 13 ตัว + เพิ่ม event `Fired` และ `ButtonFeedbackSound` (Sound ID ใน preset ยังว่างทุกช่อง) และแปลงทั้ง 13 ปุ่มเป็น instance ของ `PF_Button_Hub` แล้ว (sprite/ขนาด/onClick ของแต่ละปุ่มเป็น override แก้ prefab แล้วค่าที่ไม่ได้ override จะเปลี่ยนตามทุกปุ่ม) แผน: `Docs/Bar410_ButtonFeedback_Plan.md` ยังไม่ได้ทดสอบ: เมาส์/controller จริง (ทดสอบด้วย event สังเคราะห์เท่านั้น), ค่า scale ช่วง Click (~1.12), ปุ่มที่ซ่อนอยู่ (Book, SkipWarning, EndShift, Thx) commit `db03a9e` (2026-10-28 เพิ่ม: ปุ่ม Cocktail/Garnish/Minigame 33 ตัวเป็น instance ของ `PF_Button_Hub` + `BTN_CloseEventLog` commit `0a6c503` ข้าม `BTN - OpenOrder` กับ `Panel - Note` เพราะ `Post_It_Order` scale อยู่แล้ว)
- TODO(เจ้าของ): ในตอนนี้จะให้วางแผนการจัดการงาน Structure Ui เพื่อให้ง่ายต่อการใช้งานและโครงสร้างที่ชัดเจน

## สิ่งที่ต้องแก้
- Input ซ้ำกัน: กด Pause แล้ว Yarn Spinner เลื่อนบทพูดไปพร้อมกัน (น่าจะแก้แล้ว: เอา Esc ออกจาก Next line ใน `PauseAwareKeyCodes` รอทดสอบด้วยการกดจริง)
- มี EventSystem 2 ตัวในฉาก
- Save/Load ในฉากหลักไม่กลับมาที่บรรทัดเดิม: `LinePresenterCocktailProject` ไม่ตั้ง `SceneLoaderBridge.CurrentLineId` / ไม่อ่าน `#save_checkpoint` / ไม่ทำ silent replay และ `OptionPresenterCocktailProject` ไม่เก็บคำตอบลง `SessionOptionChoices` (logic เดิมอยู่ใน `BubblePresenter` / `OptionPresenterType2` ที่ไม่ได้อยู่ในฉากแล้ว)
- **Save/Load ต้องแก้และ refactor อย่างหนัก (พักไว้ก่อน 2026-10-28)** สภาพตอนนี้ในฉากหลัก: load = เริ่มวันใหม่จาก `startNode` ตายตัวของ `DialogueRunner` (คืนค่าแค่ตัวแปร Yarn) ไม่มีใครอ่าน `ChapterName` ที่ save ไว้ ปัญหาที่ต้องตอบตอน refactor:
  - Phase ของ FSM ไม่ถูก save → load แล้วเริ่มที่ BeforeOpen เสมอ ถ้า save กลาง Open แล้วบทไปต่อที่ node เดิมจะค้าง (`<<flow_*>>` ผิด transition)
  - ระหว่าง silent replay `GameFlowCommands` ข้ามคำสั่ง flow ทุกตัว พึ่ง replay พา FSM ไปถึง Phase เดิมไม่ได้
  - popup ปลดล็อควัตถุดิบ (Event) ต้องเก็บรายการ "แจ้งแล้ว" ลง save ไม่งั้นขึ้นซ้ำ
  - save ระหว่าง Close ได้ `$day` ของวันเดิม (บวกตอน Close → BeforeOpen) load แล้วจะเล่นวันเดิมซ้ำ
  - `[SaveLoadSystem]` ถูกปิด (inactive) ในฉากหลัก `<<save_chapter_node "[SaveLoadSystem]">>` จึงหา target ไม่เจอและทำให้บทพูดค้าง (พบตอนทดสอบ 2026-10-28)
  - ทางเลือกที่คุยไว้: (ก) save Phase แล้ว load ข้าม Event/Story ไป Open ตรงๆ (ข) save ได้แค่ตอนเริ่มวัน (ค) ทำ (ข) ก่อนแล้วขยายเป็น (ก)
- **Localization ยังไม่ได้ทำ (พักไว้ 2026-10-28)** ต้องรองรับหลายภาษา ข้อความที่จะต้องย้ายไป String Table ตอนทำ: ชื่อ Chapter ใน `SO_DaySchedule` (แผน `Docs/Bar410_BeforeOpen_Plan.md`)
- `MainMenu_V2.unity` มี component Volume บน Global Volume ถูกลบค้างอยู่ (ยังไม่ commit รอเจ้าของยืนยัน)

## Request พิเศษจาก Designer
- รองรับการเล่นด้วย controller (ใช้ตอนออกงาน event ต่างๆ)

## วิธีทำงานกับผม
- คุยภาษาไทย ส่วนโค้ด comment และ commit message ใช้ภาษาอังกฤษตามของเดิม
- **เรื่อง design และโครงสร้าง ให้ถามทิศทางก่อนเสนอแผน** อย่าเดาสถาปัตยกรรมเอง
- แก้เป็น stage และ compile-check ทุก stage (`read_console` ต้องไม่มี error) ก่อนไป stage ถัดไป
- หลังแก้ data หรือ cocktail ให้รัน `Bar410 > Validate Cocktail Data`
- ทดสอบใน Play mode ผ่าน Unity MCP เมื่อทำได้ และบอกตรงๆ ว่าอะไรยังไม่ได้ทดสอบ
- commit / push / PR เฉพาะเมื่อสั่ง แยก commit ตามเรื่อง
- งานใหญ่ให้เขียน HANDOFF / decision log ไว้ใน `Docs/`
- **ทุกครั้งที่แก้หรือสร้างไฟล์ ให้บอกผมในข้อความตอนจบ:** path ของไฟล์, ไฟล์นั้นทำอะไร และถ้าอยู่ใน Scene/Prefab ให้บอกว่าอยู่ที่ GameObject ไหน
- **ถ้าเป็น Component (MonoBehaviour) ใหม่:** เพิ่มลงหัวข้อ "Component ที่เพิ่มเข้ามา" ด้านล่างด้วย และอัปเดตเมื่อย้ายหรือลบ
- TODO(เจ้าของ): กติกาเพิ่มเติม (เช่น ชื่อ branch, รูปแบบ commit, สิ่งที่ห้ามแตะ)

## Component ที่เพิ่มเข้ามา
| Component | ไฟล์ | อยู่ที่ (Scene › GameObject) | ทำอะไร |
|---|---|---|---|
| `EventLogPresenter` | `Assets/[06]Dialogue/EventLog/EventLogPresenter.cs` | `New Drag Drop System` › `[EventLogSystem]` | เก็บบทพูดและคำตอบลง Event Log, บันทึกลง save, ล้างด้วย `<<eventlog_clear>>` |
| `PauseAwareKeyCodes` | `Assets/[06]Dialogue/PauseAwareKeyCodes.cs` | prefab `Assets/[06]Dialogue/Text Animator Dialogue System.prefab` › `Line Advancer` | ปุ่มเร่ง/ไปบทถัดไป (ลิสต์ปุ่ม + cooldown) ไม่รับตอน Pause |
| `DialogueControls` | `Assets/[06]Dialogue/DialogueControls.cs` | `New Drag Drop System` › `Canvas_HUD` | ปุ่ม AutoPlay, Skip และ `Panel_SkipWarning` การแสดง/ซ่อนปุ่มตาม Phase ไม่อยู่ที่นี่แล้ว ย้ายไป `HubManager` |
| `HubManager` | `Assets/[02]Script/UI Script/HubManager.cs` | `New Drag Drop System` › `[GameLoop]` › `Canvas_HUD` | ตาราง Odin ใน Inspector: แต่ละแถวคือ Hub UI หนึ่งตัว พร้อมปุ่มเลือก Phase ที่แสดง (Talking / AddIngredient / Minigame / Garnish / Serve / Close / BeforeOpen) นอก Phase จะ fade หายและกดไม่ได้ (CanvasGroup ไม่เรียก `SetActive`) UI ที่ไม่อยู่ในตารางแสดงตลอด `FlowWiring` เรียก `SetPhase` ทุก Phase มีปุ่ม Preview ตอน edit mode (Undo ได้) |
| `DialogueSettingStepper` | `Assets/[06]Dialogue/Settings/DialogueSettingStepper.cs` | `PauseScene` › `Panel - Setting` › `Panel_DialougeSetting` › `Setting TypeWriteSpeed` (Text Speed) และ `Setting TypeWriterStyle` (Display Type) | แถว `◀ ค่า ▶` เลือก Text Speed (Slow/Normal/Fast) / Display Type (Character/Word/Instant) เก็บใน PlayerPrefs `dlg.speed` / `dlg.display` |
| `SettingWindowTabs` | `Assets/[02]Script/Pause/SettingWindowTabs.cs` | `PauseScene` › `Panel - Setting` › `Panel_BTN_ChangeSettingWindow` | แท็บของหน้า Settings: กดปุ่มแล้วเปิด panel ของปุ่มนั้น ปิดตัวอื่น (Sound / Dialogue / Language) เปิด Settings ใหม่จะกลับไปแท็บแรก |
| `ButtonScaleFeedback` | `Assets/[02]Script/UI Script/ButtonFeedback/ButtonScaleFeedback.cs` | prefab `Assets/[04]Prefab/UI/PF_Button_Hub.prefab` และ `New Drag Drop System` › `[GameLoop]` › `Canvas_HUD` › `BTN_Skip`, `BTN_Clock`, `BTN_OpenEventLog`, `BTN_Toggle_AutoPlay`, `BTN_Setting`, `BTN_Open_CloseBook`, `EndShiftButton`, `Panel_BookUI/Book_Cover/` (`BTN_NextPage`, `BTN_PreviousPage`, `BTN_Close`), `Panel_SkipWarning/.../Panel_BTN/` (`Button_ConfirmSkip`, `Button_CancelSkip`), `Panel_Thx/Image/Button` และปุ่มใน Manager อื่น (เป็น instance ของ `PF_Button_Hub` เหมือนกัน): `[CocktailSystem]` › `Canvas_Cocktail` › `Panel - Method` (`BTN_Mixing`, `BTN_Shaking`, `BTN_Reset`), `[GarnishSystem]` › `Canvas_Garnish` ทุกปุ่ม (28 ตัว), `[MiniGameSystem]` › `Canvas_MiniGame` › `BTN - Cancel Minigame` ×2 และ prefab `Text Animator Dialogue System` › `Panel_EventLog` › `BTN_CloseEventLog` (ใส่ component ตรงๆ ไม่ใช่ instance) | scale ปุ่มตอน Hover / Hold / Release / Click ด้วย DOTween ค่าอยู่ใน `SO_ButtonFeedback` (preset Default) ปิดเมื่อ `Button.IsInteractable()` เป็น false (รวม CanvasGroup ของ `HubManager`) ใช้ unscaled time จึงขยับตอน Pause ปุ่มที่กดไม่ได้จะจางลงเหลือ `disabledAlpha` (2/3) ของ alpha เดิม ทั้งตัวปุ่มและ child |
| `ButtonFeedbackSound` | `Assets/[02]Script/UI Script/ButtonFeedback/ButtonFeedbackSound.cs` | ทุกที่ที่มี `ButtonScaleFeedback` (prefab `PF_Button_Hub` และ 13 ปุ่มใต้ `Canvas_HUD`) | ฟัง event `ButtonScaleFeedback.Fired` แล้วเล่น UiSFX ตาม Sound ID ใน preset (`hoverSound` / `holdSound` / `releaseSound` / `clickSound` ว่าง = เงียบ) ระบบอื่นในอนาคต (VFX ฯลฯ) ให้ทำ component แบบเดียวกันที่ฟัง `Fired` |
| `ChapterTitleUI` | `Assets/[02]Script/Day Schedule/ChapterTitleUI.cs` | `New Drag Drop System` › `[GameLoop]` › `Canvas_HUD` › `Panel_ChapterTitle` | ข้อความชื่อ Chapter กลางจอ (แบบชั่วคราว) fade เข้า-ค้าง-ออก ตอนเข้า Open `FlowWiring` เรียก `Show(Chapter Name ของ $day)` ไม่บังการคลิก ไม่อยู่ในตาราง `HubManager` เปลี่ยนหน้าตาได้อิสระ |
| `EventPopupQueue` | `Assets/[02]Script/UI Script/EventPopupQueue.cs` | `New Drag Drop System` › `[GameLoop]` › `Canvas_HUD` › `Panel_EventPopup` | คิว popup ของ Event (หัวข้อ / ข้อความ / รูป + ปุ่มปิด `BTN_ClosePopup`) ระบบอื่นเรียก `Enqueue` `FlowWiring` เรียก `ShowAll()` ตอนเข้า Event และจบ Event เมื่อ `AllClosed` แสดงทีละอัน บังการคลิกด้านหลัง focus ปุ่มปิดให้ controller ไม่อยู่ในตาราง `HubManager` |

ไฟล์ที่ไม่ใช่ Component แต่เกี่ยวข้อง: `SO_DaySchedule` (`Assets/[02]Script/Day Schedule/SO_DaySchedule.cs`, asset `DaySchedule_Demo.asset` ในโฟลเดอร์เดียวกัน ผูกที่ `[GameLoop]` › `FlowWiring`) คือตาราง วัน / Start Node / ชื่อ Chapter, `SO_DialogueSettings` + `DialogueSettings` (`Assets/[06]Dialogue/Settings/SO_DialogueSettings.cs`, asset `DialogueSettings.asset`) คือค่าตั้งต้นของบทพูด `SO_ButtonFeedback` (`Assets/[02]Script/UI Script/ButtonFeedback/SO_ButtonFeedback.cs`, asset `ButtonFeedback_Default.asset` ในโฟลเดอร์เดียวกัน) คือค่า scale ของปุ่ม และ `SkipScan` (`Assets/[06]Dialogue/SkipScan.cs`) ใช้ดูบทล่วงหน้าตอน Skip

## เอกสารที่ควรอ่านก่อนเริ่มงานแต่ละระบบ
- Flow / manager: `Docs/Bar410_Managers_Restructure_HANDOFF.md`, `Docs/Bar410_StateMachine_Implementation.md`
- Pause: `Docs/Bar410_PauseSystem_Plan.md`, `Docs/Bar410_PauseSystem_Setup.md`
- Cocktail: `Docs/Bar410_CocktailSystem_Architecture.md`
- Garnish: `Docs/Bar410_GarnishRestructure_HANDOFF.md`
- Minigame: `Docs/Bar410_Minigame_Integration_Report.md`
- Event Log (ประวัติบทสนทนา): `Docs/Bar410_EventLog_Plan.md`
- AutoPlay / Skip: `Docs/Bar410_DialogueSkipAutoPlay_Plan.md`
- ความเร็ว / แบบการแสดงตัวอักษร: `Docs/Bar410_DialogueTextSettings_Plan.md`
- Hub UI ตาม Phase: `Docs/Bar410_HubManager_Plan.md`
- **เขียนบทวันใหม่:** ดูตัวอย่าง `Assets/[06]Dialogue/YarnScript/Demo/PLAYTEST_Demo.yarn` (หัวไฟล์คือโครงของหนึ่งวัน) แล้วเพิ่มแถวใน `SO_DaySchedule` (วัน / Start Node / ชื่อ Chapter) `<<flow_next_day>>` ควรเป็นคำสั่งท้ายวัน วันใหม่เริ่มเมื่อบทเดิมจบ
- คำสั่ง/ฟังก์ชัน Yarn ทั้งหมด (หนึ่งไฟล์ต่อคำสั่ง): `Docs/YarnCommands/README.md` ทดสอบด้วย `Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` (Start Node = `TEST_Start`) เพิ่ม/แก้คำสั่ง Yarn แล้วให้อัปเดตทั้งสองที่
- ภาพรวมล่าสุด: `Docs/Bar410_Status_Report_2026-10-24.html`
