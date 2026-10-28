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
- เพิ่ม Phase BeforeOpen ใน Level 1 แล้ว (2026-10-28) ยังไม่ commit
- แผนปลดล็อควัตถุดิบ: `Docs/Bar410_IngredientUnlock_Plan.md` + ร่าง `Docs/adr/0003-ingredient-unlock.md` รออนุมัติ ยังไม่มีโค้ด
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
  - ทางเลือกที่คุยไว้: (ก) save Phase แล้ว load ข้าม Event/Story ไป Open ตรงๆ (ข) save ได้แค่ตอนเริ่มวัน (ค) ทำ (ข) ก่อนแล้วขยายเป็น (ก)
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

ไฟล์ที่ไม่ใช่ Component แต่เกี่ยวข้อง: `SO_DialogueSettings` + `DialogueSettings` (`Assets/[06]Dialogue/Settings/SO_DialogueSettings.cs`, asset `DialogueSettings.asset`) คือค่าตั้งต้นของบทพูด และ `SkipScan` (`Assets/[06]Dialogue/SkipScan.cs`) ใช้ดูบทล่วงหน้าตอน Skip

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
- ภาพรวมล่าสุด: `Docs/Bar410_Status_Report_2026-10-24.html`
