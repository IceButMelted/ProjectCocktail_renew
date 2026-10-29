# Bar410: คู่มือระบบทั้งหมด (Systems Guide)

อัปเดตล่าสุด: 2026-10-30 (ID กล้องใหม่ Char1Cam ฯลฯ)

ไฟล์นี้รวมทุกระบบในฉากเกมหลัก `New Drag Drop System` ไว้ที่เดียว แต่ละระบบบอก 4 เรื่อง:
- หลักการทำงานแบบสั้น
- Script หลักอยู่บน Component ไหน และ GameObject ไหน
- อะไรที่แก้เองได้ใน Inspector / asset / Yarn โดยไม่ต้องแตะโค้ด
- ต้องการรายละเอียดเพิ่ม ไปอ่านเอกสารไหน

> **กติกา:** ทุกครั้งที่มีระบบใหม่หรือแก้ระบบเดิม ต้องอัปเดตไฟล์นี้ด้วย (เขียนไว้ใน `CLAUDE.md` แล้ว)

---

## แผนผังภาพรวม

```
[GameLoop]  (GameLoopFSM + FlowWiring + GameFlowCommands)
 │   FlowWiring = ที่เดียวที่ flow (Phase) เจอกับ manager  manager ไม่รู้จักกันเอง
 ├─ [CocktailSystem]        ชง: ขวด, shaker, ออเดอร์, ให้คะแนน
 ├─ [MiniGameSystem]        มินิเกม Shaking / Mixing
 ├─ [GarnishSystem]         แก้ว, น้ำแข็ง, ตกแต่ง, เสิร์ฟ
 ├─ [PlacmentSystem]        input + การวางของ
 ├─ [EventLogSystem]        ประวัติบทสนทนา
 ├─ [IngredientUnlocks]     วัตถุดิบไหนปลดล็อคแล้ว
 ├─ CamController           กล้อง (Cinemachine)
 ├─ Canvas_HUD              Hub UI ตาม Phase, ปุ่ม AutoPlay/Skip, popup Event, ชื่อ Chapter
 └─ Text Animator Dialogue System   DialogueRunner (Yarn)
NPCs_And_Set                TalkToCustomerManager + ตัวละคร 8 ตัว (CharacterView)
[PauseManager] [SoundManager] [SaveLoadSystem]   ระบบระดับฉาก (scene-scoped singleton)
PauseScene (โหลดซ้อน)        หน้า Pause / Setting
```

ลำดับฉาก: `MainMenu_V2` → `LoadingScene` → `New Drag Drop System`

---

## 1. Game Flow (Phase ของวัน)
- **หลักการ:** state machine 3 ระดับ (`GameLoopFSM`) เป็น C# ล้วน ไม่รู้จัก Unity object
  - BeforeOpen (Event → StoryBeforeOpen) → Open (Talking → Prepare (AddIngredient ⇄ Minigame) → Garnish → Serve → Talking) → Close → BeforeOpen
  - `FlowWiring` ฟังการเข้า/ออก Phase แล้วสั่ง manager แต่ละตัว และแปลง event ของ manager กลับเป็นคำสั่ง flow
  - Yarn สั่ง flow ผ่าน `GameFlowCommands` (`<<flow_*>>`)
- **Script หลัก:**
  - `GameLoopFSM`: `Assets/[02]Script/Hierarchical State Machine/Level 1 - Game Loop/GameLoopFSM.cs` บน `[GameLoop]`
  - `FlowWiring`: `Assets/[02]Script/Hierarchical State Machine/FlowWiring.cs` บน `[GameLoop]`
  - `GameFlowCommands`: `Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs` บน `[GameLoop]`
- **แก้เองได้:**
  - ช่องอ้างอิงใน `FlowWiring` (ลาก manager / UI เข้า)
  - ปุ่มเปิดร้าน: OnClick → `GameFlowCommands.OpenBar`
  - ใน Yarn: `<<flow_open_bar>>`, `<<flow_check_events>>`, `<<flow_next_day>>` ฯลฯ
- **อ่านเพิ่ม:** `Docs/Bar410_Managers_Restructure_HANDOFF.md`, `Docs/Bar410_StateMachine_Implementation.md`, `Docs/YarnCommands/README.md`

## 2. ตารางวัน + ก่อนเปิดร้าน (Day Schedule / BeforeOpen)
- **หลักการ:**
  - `FlowWiring.Start` รอ `Panel_Transition` เล่นจบแล้วค่อยเริ่มวัน
  - เข้า BeforeOpen แล้วเริ่มที่ Event ก่อนเสมอ (popup) แล้วจึงเข้า Story เล่น node ของวันตาม `$day`
  - Close → BeforeOpen บวก `$day` ให้เอง
  - ชื่อ Chapter ขึ้นตอนเข้า Open และ `<<flow_open_bar>>` รอจนชื่อ fade ออกครบก่อนไปบรรทัดถัดไป (ไม่รอตอน Skip)
  - `<<Panel_FadeIn/FadeOut Panel_Transition>>` รอ animation เล่นจบ
- **Script หลัก:**
  - `SO_DaySchedule`: `Assets/[02]Script/Day Schedule/SO_DaySchedule.cs` asset `DaySchedule_Demo.asset` ผูกที่ `FlowWiring`
  - `ChapterTitleUI` บน `Canvas_HUD` › `Panel_ChapterTitle`
- **แก้เองได้:**
  - `DaySchedule_Demo.asset`: แถว วัน / Start Node / ชื่อ Chapter
  - หน้าตา `Panel_ChapterTitle`
  - บทวันใหม่ ดูตัวอย่าง `Assets/[06]Dialogue/YarnScript/Demo/PLAYTEST_Demo.yarn`
  - เดโม 3 วันจริง: `DEMO_3Days.yarn` ผูกใน `DaySchedule_Demo.asset` ส่วน playtest ใช้ `DaySchedule_PlayTest.asset` (สลับที่ช่อง Day Schedule ของ `FlowWiring`)
- **อ่านเพิ่ม:** `Docs/Bar410_BeforeOpen_Plan.md`

## 3. Popup ของ Event
- **หลักการ:**
  - ระบบอื่นเรียก `Enqueue(หัวข้อ, ข้อความ, รูป)`
  - ตอนเข้า Event `FlowWiring` เรียก `ShowAll()` popup ขึ้นทีละอัน Event จบเมื่อปิดครบ**และ panel fade ออกจบ** (คิวว่าง = จบทันที) Yarn ที่รอ Event (`<<flow_check_events>>`) จึงไปต่อหลัง fade
  - ปุ่มปิดถูก focus ให้ใช้ controller ได้
- **Script หลัก:** `EventPopupQueue` (`Assets/[02]Script/UI Script/EventPopupQueue.cs`) บน `Canvas_HUD` › `Panel_EventPopup`
- **แก้เองได้:**
  - หน้าตา panel
  - **Show Sound Id** (เสียงตอน popup ขึ้น ว่าง = เงียบ)
  - **Fade Seconds** (เวลา fade เข้า/ออก 0 = ทันที)
  - รูป: `Window` › `Panel_Icons` (Horizontal Layout Group) › `IMG_Popup` popup ที่มีหลายรูปจะสร้างสำเนาของ `IMG_Popup` เรียงข้างกัน ไม่มีรูป = ซ่อน
- **ใครใส่คิวอยู่ตอนนี้:** ระบบปลดล็อควัตถุดิบ (ข้อ 4)

## 4. ปลดล็อควัตถุดิบ (Ingredient Unlock) *ใหม่ 2026-10-29*
- **หลักการ:**
  - ปลดล็อค = (`$day` ≥ Unlock Day) หรือบทสั่ง `<<unlock_ingredient X>>` ปลดล็อคแล้วถาวร
  - ทุกระบบถาม `IngredientUnlocks` ที่เดียว:
    - บาร์ซ่อนขวดที่ล็อค ขวดขึ้นทันทีที่ปลดล็อค (ทุก Phase) และตอนเริ่มวันใหม่
    - ออเดอร์สุ่มเลือกเฉพาะสูตรที่ทำได้
    - Event มี popup "Unlock New Ingredient" วันละอัน (วัน 1 ไม่มี)
- **Script หลัก:**
  - `IngredientUnlocks` (`Assets/[02]Script/Ingredient Unlock/IngredientUnlocks.cs`) บน `[GameLoop]` › `[IngredientUnlocks]`
  - ตาราง `SO_IngredientUnlockTable` asset `Assets/[02]Script/Ingredient Unlock/IngredientUnlockTable.asset`
- **แก้เองได้:**
  - ตาราง: เลือกวัตถุดิบด้วย Id (หมวด + enum, ไม่ใช่ข้อความ), Unlock Day (0 = รอบทสั่งเท่านั้น), Display Name, Icon (รูปใน popup)
  - ปุ่ม **Add Missing Ingredients** บน asset เติมแถวให้วัตถุดิบ enum ที่ยังไม่มีแถว (enum เพิ่มต่อท้ายเท่านั้น)
  - Yarn: `<<unlock_ingredient X>>`, `ingredient_unlocked("X")`
  - เสียง popup ที่ `EventPopupQueue`
  - หลังแก้ตาราง → `Bar410 > Validate Cocktail Data`
- **อ่านเพิ่ม (ละเอียด):** `Docs/Bar410_IngredientUnlock_HowItWorks.md`, แผน `Docs/Bar410_IngredientUnlock_Plan.md`

## 5. ชงเครื่องดื่ม + ออเดอร์ (Cocktail)
- **หลักการ:**
  - ขวดเทลง shaker
  - สูตรรวม 10 หน่วยเสมอ และไม่มีสูตรที่ส่วนผสมซ้ำกันเป๊ะ
  - ออเดอร์มาจาก Yarn (`Order_Cocktail_*`, `order_by_type`)
  - ให้คะแนนที่เดียวตอน Serve → Talking (remake ไม่ให้คะแนน)
- **Script หลัก:**
  - `CocktailSystemManager` + `IngredientButtonGroup` บน `[GameLoop]` › `[CocktailSystem]`
  - ขวด (`IngredientButtonUI`) อยู่ใต้ `Base Spirits` / `Liqueur` / `Mixer`
  - logic ออเดอร์อยู่ใน `OrderService` (C# ล้วน)
- **แก้เองได้:**
  - สูตร: `Assets/[02]Script/Cocktail System/Cocktail Config/Cocktail/*.asset` (แก้แล้วรัน Validate)
  - รสนิยมลูกค้า: `SO_Customer` › Drinks › Preferred Drink Types
- **อ่านเพิ่ม:** `Docs/Bar410_CocktailSystem_Architecture.md`, `Docs/Bar410_CocktailSystem_Manual_Setup.md`

## 6. มินิเกม (Minigame)
- **หลักการ:** Shaking / Mixing แพ้ไม่ได้ จบได้แค่ `Completed` หรือ `Cancelled` เข้า/ออกตาม Phase 2.2
- **Script หลัก:** `MinigameSystemManager` + `ShakingMinigame` + `MixingMinigame` บน `[GameLoop]` › `[MiniGameSystem]` (UI ใน `Canvas_MiniGame`)
- **แก้เองได้:** Default Type และปุ่ม Back ใน `MinigameSystemManager` รวมถึงค่าของมินิเกมแต่ละตัวใน Inspector
- **อ่านเพิ่ม:** `Docs/Bar410_Minigame_Integration_Report.md`, `Docs/Bar410_Minigame_FSM_Simplification_Summary.md`

## 7. ตกแต่งและเสิร์ฟ (Garnish / Serve)
- **หลักการ:** แก้ว น้ำแข็ง และของตกแต่งอยู่ใน Garnish เท่านั้น ตำแหน่งแก้ว/กล้องตายตัว เลือกด้วยปุ่ม (ADR 0002)
- **Script หลัก:** `GarnishManager` บน `[GameLoop]` › `[GarnishSystem]` (UI `Canvas_Garnish`)
- **แก้เองได้:** จุดวางแก้ว, ปุ่มทั้งหมด, `Slide Duration`, panel เสิร์ฟ
- **อ่านเพิ่ม:** `Docs/Bar410_GarnishRestructure_HANDOFF.md`, `Docs/Bar410_Garnish_Decoration_Plan.md`

## 8. กล้อง (Camera)
- **หลักการ:**
  - `FlowWiring` เลือกมุมกล้องตาม Phase
  - หมุนกล้องด้วยเมาส์ได้เฉพาะตอน AddIngredient ขณะ shaker ยังไม่เต็ม
  - Yarn กำกับภาพด้วย `Switch_Camera` / `Cut_Camera`
- **Script หลัก:** `CameraManager` + `CameraController` บน `[GameLoop]` › `CamController`
- **แก้เองได้:** Global Blend Time / Style และชื่อ shot (`Bar Shot` = MainCam, `Shaker Shot` = PrepareCam, `Garnish Shot` = GarnishCam)
  - เพิ่ม/แก้กล้อง: ใส่ `CameraEntry` บน virtual camera ใน prefab `Assets/[04]Prefab/GameSystemPrefab/CamController.prefab` ตั้ง `cameraId` (ID ห้ามซ้ำ ตั้งชื่อ GameObject ให้ตรงกัน) ID ตอนนี้: `MainCam`, `Char1Cam`, `Char2Cam`, `Char3Cam`, `Char12Cam`, `Char23Cam`, `WideCam`, `FrontCam`, `BackCam`, `PrepareCam`, `GarnishCam`, `SummaryCam` (ตารางความหมายใน `Docs/YarnCommands/Switch_Camera.md`)
- **อ่านเพิ่ม:** `Docs/Bar410_WriterCommands_Plan.md`, `Docs/adr/0002*`

## 9. ตัวละคร (Characters)
- **หลักการ:**
  - ตัวละครละ 1 `CharacterView` ถือ `SO_Customer` และ state สีหน้า / ท่าหัน / อยู่ในร้านไหม
  - `Apply()` คือที่เดียวที่เปลี่ยน sprite
  - เข้า-ออกร้านด้วย fade
  - ทั้ง 8 ตัวตอนนี้ใช้**โหมดภาพเก่า** (รูปทั้งตัวแผ่นเดียว คำสั่งล่าสุดชนะ)
- **Script หลัก:**
  - `CharacterView` บน `NPCs_And_Set` › `NPC_<ชื่อ>` (child `Body` / `Face`)
  - `TalkToCustomerManager` บน `NPCs_And_Set` เป็นคนสั่งตัวละครและเริ่มบท
- **แก้เองได้:**
  - `Assets/[02]Script/NPC/CustomerData/<ชื่อ>.asset`: ชื่อ, สีชื่อ, รูปทุกสีหน้า, Look F/L/R, Use Legacy Visual
  - `TalkToCustomerManager`: จุดยืน (Spots), เวลา fade, ท่าเดินตอน fade (`Walk Bob`: Height = ความสูงแต่ละก้าวเป็นเมตร, Steps = จำนวนก้าวต่อหนึ่ง fade, Height 0 = ไม่ขยับ), เสียงเข้า/ออกร้านของทุกตัวละคร (`Enter Sound Id` + `Enter Sound Lead` = เล่นก่อน fade เข้ากี่วินาที, `Exit Sound Id` + `Exit Sound Delay` = เล่นหลัง fade ออกจบกี่วินาที, ว่าง = เงียบ, ไม่เล่นตอน Skip/โหลด)
  - ปุ่ม Preview บน `CharacterView` ตอน edit mode
  - Yarn: `<<Isla Sad>>`, `<<Isla Look L>>`, `<<Isla Enter 2>>`, `<<Isla Exit>>`
- **อ่านเพิ่ม:** `Docs/Bar410_CharacterPresentation_Plan.md`, `Docs/Bar410_LegacyCharacterVisual_Plan.md`, `Docs/YarnCommands/CharacterShortForm.md`

## 10. บทสนทนา (Dialogue: AutoPlay / Skip / Event Log / ความเร็วตัวอักษร)
- **หลักการ:**
  - Yarn Spinner + Text Animator
  - AutoPlay / Skip-to-order อยู่บน HUD
  - Event Log เก็บบทพูดและคำตอบ
  - ความเร็ว/แบบแสดงตัวอักษรตั้งใน Pause › Setting (PlayerPrefs `dlg.speed` / `dlg.display`)
  - ปุ่มเลื่อนบทไม่รับตอน Pause
- **Script หลัก:**
  - `DialogueRunner`: `[GameLoop]` › `Text Animator Dialogue System`
  - `DialogueControls`: `[GameLoop]` › `Canvas_HUD`
  - `EventLogPresenter`: `[GameLoop]` › `[EventLogSystem]`
  - `PauseAwareKeyCodes`: prefab `Text Animator Dialogue System` › `Line Advancer`
  - `DialogueSettingStepper`: `PauseScene`
- **แก้เองได้:**
  - `DialogueSettings.asset` (ค่าตั้งต้นบทพูด)
  - สี AutoPlay บน `DialogueControls`
  - ลิสต์ปุ่มเลื่อนบท + cooldown ใน `PauseAwareKeyCodes`
- **อ่านเพิ่ม:** `Docs/Bar410_EventLog_Plan.md`, `Docs/Bar410_DialogueSkipAutoPlay_Plan.md`, `Docs/Bar410_DialogueTextSettings_Plan.md`

## 11. Hub UI ตาม Phase
- **หลักการ:**
  - `FlowWiring` ส่ง Phase ให้ `HubManager`
  - UI ในตารางจะ fade หายและกดไม่ได้นอก Phase ที่เลือก (ใช้ CanvasGroup ไม่ `SetActive`)
  - UI ที่ไม่อยู่ในตารางแสดงตลอด
- **Script หลัก:** `HubManager` บน `[GameLoop]` › `Canvas_HUD`
- **แก้เองได้:**
  - ตาราง Odin: เพิ่มแถว UI แล้วกดเลือก Phase ที่จะแสดง
  - ปุ่ม Preview ใน edit mode
- **อ่านเพิ่ม:** `Docs/Bar410_HubManager_Plan.md`

## 12. ปุ่ม (Button Feedback)
- **หลักการ:**
  - ปุ่มขยายตอน Hover / Hold / Release / Click ด้วย DOTween
  - ปุ่มที่กดไม่ได้จะจางลง
  - เสียงเป็น component แยก ฟัง event `Fired`
- **Script หลัก:**
  - `ButtonScaleFeedback` + `ButtonFeedbackSound` บน prefab `Assets/[04]Prefab/UI/PF_Button_Hub.prefab` (ปุ่มส่วนใหญ่เป็น instance ของ prefab นี้)
  - ค่าอยู่ใน `SO_ButtonFeedback`
- **แก้เองได้:**
  - `Assets/[02]Script/UI Script/ButtonFeedback/ButtonFeedback_Default.asset`: ค่า scale, เวลา, Sound ID (ตอนนี้ว่างทุกช่อง)
  - แก้ prefab `PF_Button_Hub` แล้วทุกปุ่มเปลี่ยนตาม ยกเว้นค่าที่ override ไว้
- **อ่านเพิ่ม:** `Docs/Bar410_ButtonFeedback_Plan.md`

## 13. เสียง (Sound)
- **หลักการ:** `SoundManager` เป็น singleton ของฉาก เล่น BGM / SFX / UiSFX ด้วย Sound ID ผ่าน AudioMixer ลดเสียงลงตอน Pause
- **Script หลัก:** `SoundManager` (`Assets/[02]Script/Sound/SoundManager.cs`) บน `[SoundManager]`
- **แก้เองได้:**
  - `Assets/[07]Sound/SoundData.asset`: รายการ Sound ID → ไฟล์เสียง
  - BGM Start, ระดับ Pause Duck, จำนวน SFX สูงสุด
- **ใครใช้ Sound ID:** ปุ่ม (`ButtonFeedback_Default.asset`), popup Event (`EventPopupQueue` › Show Sound Id)

## 14. Pause / Setting
- **หลักการ:** `PauseManager` โหลด `PauseScene` ซ้อน (additive) และหยุดเวลาได้ หน้า Setting มีแท็บ Sound / Dialogue / Language
- **Script หลัก:**
  - `PauseManager` บน `[PauseManager]`
  - `SettingWindowTabs` + `DialogueSettingStepper` ใน `PauseScene` › `Panel - Setting`
- **แก้เองได้:** Pause Time, ชื่อ Pause Scene, Esc เปิดเมนูไหม, หน้าตา `PauseScene`
- **อ่านเพิ่ม:** `Docs/Bar410_PauseSystem_Plan.md`, `Docs/Bar410_PauseSystem_Setup.md`

## 15. Save / Load ⚠️ ยังพังในฉากหลัก
- **สภาพตอนนี้:**
  - load = เริ่มวันใหม่ คืนค่าได้แค่ตัวแปร Yarn
  - Phase ของ FSM ไม่ถูก save
  - `[SaveLoadSystem]` ถูกปิดอยู่ในฉากหลัก
  - รายการปัญหาครบอยู่ใน `CLAUDE.md` › สิ่งที่ต้องแก้
- **Script หลัก:** `SaveLoadManager` (`Assets/[02]Script/SaveLoad/SaveLoadManager.cs`) บน `[SaveLoadSystem]`
- **สิ่งที่ save:** ตัวแปร Yarn, NPC, Event Log, Setting, `UnlockData` (ปลดล็อควัตถุดิบ)
- **แก้เองได้:** ยังไม่แนะนำ รอ refactor

---

## 16. ฉากร้านและแสง (Environment / Lighting) *2026-10-29*
- **หลักการ:**
  - ตัวร้านคือ prefab `BarInteriorV3` (instance ชื่อ `BarInteriorV2` ในฉาก) มีไฟผนัง `WallLight` ×7 และไฟเพดาน `CeilingLight` ×3 เป็นแบบ Baked
  - แสง bake ลง lightmap ของฉากหลัก (`New Drag Drop System/Lightmap-*`, `LightingData.asset`) ใช้ค่าเดียวกับฉาก `BarInteriorV2.unity`
  - ท้องฟ้าคือ Default-Skybox (Procedural) ดวงอาทิตย์คือ `Directional Light (BarInteriorV2)` (ความสว่าง 0 ใช้แค่ทิศของท้องฟ้า)
  - ร้านเก่า `BarVer_3` ยังอยู่ในฉากแต่ถูกปิดไว้ (สลับกลับได้)
  - ของที่ขยับได้ (ขวด/วัตถุดิบ, แก้ว, ตัวละคร) ไม่อยู่ใน lightmap จึงรับแสงที่ bake ผ่าน Light Probe `LightProbes_Bar` (184 จุด: ตารางทั้งห้องระยะ ~1 ม. + ถี่ 0.4 ม. รอบวัตถุดิบ) ถ้าไม่มี probe ของพวกนี้จะดำ
- **อยู่ที่:** `New Drag Drop System` › `BarInteriorV2` (prefab `Assets/[04]Prefab/BarInteriorV3.prefab`), `Directional Light (BarInteriorV2)`, `BarVer_3` (ปิด), `LightProbes_Bar`
- **แก้เองได้:**
  - ตำแหน่งร้าน แล้วจัดกล้อง / จุดยืนลูกค้า (`TalkToCustomerManager` › Spots) / ขวดให้ตรงกับร้านใหม่
  - Window › Rendering › Lighting: Skybox, Environment Lighting, Lighting Settings (ตอนนี้เป็นค่าเริ่มต้นของ Unity: Progressive GPU, Shadowmask, 40 texel, lightmap 1024)
  - `LightProbes_Bar` › Edit Light Probes: เพิ่ม/ย้ายจุด ถ้าย้ายร้านหรือวัตถุดิบไปที่อื่น ให้ย้าย probe ตามด้วย
  - **ย้ายร้านหรือไฟแล้วต้องกด Generate Lighting ใหม่** ไม่งั้นเงาและแสงที่ bake ไว้จะไม่ตรง
- **HDRI เดิม:** `M_Skybox` (`HDR_WarmBar_1k`) ไม่ได้ใช้ในฉากหลักแล้ว ถ้าจะกลับไปใช้ ใส่ใน Lighting › Skybox Material แล้ว bake ใหม่

## ระบบที่ยังไม่มีหรือยังไม่ครบ
- **Close (จบวัน):** ยังไม่มี UI
- **หน้าสูตรในหนังสือ:** มีแค่ API ของการปลดล็อค รอตัวอย่าง UI
- **Localization:** ยังไม่ทำ ข้อความที่ต้องย้ายไป String Table:
  - ชื่อ Chapter (`SO_DaySchedule`)
  - Display Name ของวัตถุดิบ (`SO_IngredientUnlockTable`)
  - หัวข้อ popup "Unlock New Ingredient" (`FlowWiring`)
- **Controller:** บางจุดรองรับแล้ว (popup) ยังไม่ได้ทดสอบทั้งเกม

## วิธีอัปเดตไฟล์นี้
เมื่อเพิ่มหรือแก้ระบบ:
1. เพิ่มหรือแก้หัวข้อของระบบนั้นด้วยหัวข้อย่อยเดียวกัน (หลักการ / Script หลัก / แก้เองได้ / อ่านเพิ่ม)
2. อัปเดตแผนผังภาพรวมถ้ามี GameObject ใหม่
3. แก้วันที่ "อัปเดตล่าสุด" ด้านบน
