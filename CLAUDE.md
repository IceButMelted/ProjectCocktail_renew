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
- **Singleton เป็นแบบ scene-scoped** (เช่น `PauseManager`, `SoundManager`): ซ้ำแล้วทำลายตัวเอง และไม่ใช้ DontDestroyOnLoad
- **สูตรเครื่องดื่ม:** วัตถุดิบรวม = 10 หน่วยเสมอ ไม่มีสูตรที่ส่วนผสมซ้ำกันเป๊ะ ตรวจด้วย `Bar410 > Validate Cocktail Data`

## งานที่กำลังทำ
ตรงนี้ให้ Update ตัวเองในการทำทุกครั้งว่ากำลังทำอะไร
- branch `UiManager`: วางโครงสร้าง UiManager
- แผนผัง UI ปัจจุบันอยู่ที่ FigJam board "Project Cocktails" › section "Ui Manager"
- TODO(เจ้าของ): ในตอนนี้จะให้วางแผนการจัดการงาน Structure Ui เพื่อให้ง่ายต่อการใช้งานและโครงสร้างที่ชัดเจน

## สิ่งที่ต้องแก้
- Input ซ้ำกัน: กด Pause แล้ว Yarn Spinner เลื่อนบทพูดไปพร้อมกัน
- มี EventSystem 2 ตัวในฉาก

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
- TODO(เจ้าของ): กติกาเพิ่มเติม (เช่น ชื่อ branch, รูปแบบ commit, สิ่งที่ห้ามแตะ)

## เอกสารที่ควรอ่านก่อนเริ่มงานแต่ละระบบ
- Flow / manager: `Docs/Bar410_Managers_Restructure_HANDOFF.md`, `Docs/Bar410_StateMachine_Implementation.md`
- Pause: `Docs/Bar410_PauseSystem_Plan.md`, `Docs/Bar410_PauseSystem_Setup.md`
- Cocktail: `Docs/Bar410_CocktailSystem_Architecture.md`
- Garnish: `Docs/Bar410_GarnishRestructure_HANDOFF.md`
- Minigame: `Docs/Bar410_Minigame_Integration_Report.md`
- ภาพรวมล่าสุด: `Docs/Bar410_Status_Report_2026-10-24.html`
