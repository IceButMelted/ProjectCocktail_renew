# Bar410 Rewrite — แผนปฏิบัติการ (API + มอบหมายงาน + ขั้นตอนตรวจ)

เขียน 2026-11-07 **ยังไม่ได้ลงมืออะไรทั้งสิ้น** (ไม่มี commit, ไม่มีการแก้โค้ด/ฉาก/package) รอคุณสั่งรัน
ที่มา: การตัดสินใจใน `Bar410_Rewrite_Options.md` §9 + การออกแบบ API โดย Opus 4 ตัว (หนึ่งตัวต่อกลุ่มระบบ) อ่านโค้ดจริงของโปรเจกต์และซอร์ส Yarn/Input System
ข้อกำหนดเต็มของแต่ละกลุ่ม (signature, edge case, ขั้นตอนทดสอบ, checklist สำหรับ Opus ตรวจ) อยู่ในไฟล์แนบ:

| กลุ่ม | ไฟล์ | ขอบเขต |
|---|---|---|
| G1 | `rewrite/API_G1_CoreFlow.md` | GameRoot, DayRunner, BartendingSession, Station, FlowState/FlowCommands, asmdef |
| G2 | `rewrite/API_G2_Dialogue.md` | Yarn verb + ReplayPolicy, DialogueDirector, presenter, เสียง Expression, Localization, validator |
| G3 | `rewrite/API_G3_Features.md` | interface ของ Cocktail/Minigame/Garnish/Camera/Customers/Unlock/Hub, WorldView, additive UI scene |
| G4 | `rewrite/API_G4_SaveInput.md` | Save หลาย slot + autosave + replay, Input System, virtual cursor, glyph |

**ธรรมเนียมการอ่าน:** ทุกอย่างในไฟล์แนบเป็นร่างจากการอ่านโค้ด ไม่ได้คอมไพล์ ข้อที่ตรวจกับซอร์สจริงมีป้าย `[V]`/ตารางข้อเท็จจริง ส่วนที่ยังไม่ได้ตรวจอยู่ในหัวข้อ "UNVERIFIED" ของแต่ละไฟล์

---

## 1. R0 — Git (รอคำสั่งรัน)

สถานะที่ตรวจ: branch `IngredientUnlock`, `main` ตามหลัง 18 commit ไม่มี conflict (`git merge-tree` สะอาด) → fast-forward ได้ ไม่มี stash ไม่มี tag เดิม

| # | commit | ไฟล์ | สถานะ |
|---|---|---|---|
| 1 | `Docs: rewrite planning and API specs` | Docs ทั้ง 6 ไฟล์เดิม + `Bar410_Rewrite_ExecutionPlan.md` + `Docs/rewrite/*` | รอสั่ง |
| 2 | `Lighting: rebake main scene lightmaps` | lightmap, LightingData, ReflectionProbe (+meta) ~3.7 MB | คุณสั่ง **เก็บ** |
| 3 | `Packages: bump Linux toolchain and sdk to 1.1.1` | `manifest.json`, `packages-lock.json` | รอสั่ง |
| 4 | `Project: regenerate Yarn definitions` | `*-generated.ysls.json` 3 ไฟล์ | รอสั่ง |
| 5 | `Project: remove Rowlan Fullscreen tool` | `Assets/Rowlan/**`, `Assets/[09]ThirdPartyAsset/Rowlan/**` (~60 ไฟล์ รวม meta) | คุณสั่ง **ตั้งใจลบ** |
| — | `ProjectCocktail_renew.slnx` | — | คุณสั่ง **ไม่ commit** |

หลัง commit: `git checkout main` → `git merge --ff-only IngredientUnlock` (ไม่ push) → `git tag pre-rewrite` → `git checkout -b RefactorCode`
ผมจะไม่ push, ไม่ลบ/restore ไฟล์เอง

**งาน R0 ที่เหลือ (ยังไม่มีใครทำ):** ไฟล์ค่าอ้างอิงพฤติกรรมเดิม (ค่า `flow_phase()/flow_step()` ต่อขั้นของ `PLAYTEST_Demo` และ `TEST_AllCommands`, checklist เส้นทางหลัก) ใช้เป็นเกณฑ์ผ่านของ G1-15 → เป็นงาน N0 ใน §6

---

## 2. สิ่งที่การออกแบบพบ และเปลี่ยนแผนเดิม

ข้อเหล่านี้ **ขัดกับที่เขียนไว้ใน `Bar410_Rewrite_Options.md`** (ยังไม่ได้แก้ไฟล์นั้น ถือเอกสารนี้เป็นตัวใหม่)

| # | ข้อค้นพบ | ผล |
|---|---|---|
| F1 | โค้ดโปรเจกต์ไม่มี asmdef เลย ทุก manager อยู่ใน Assembly-CSharp ซึ่ง asmdef อ้างอิงไม่ได้ (ปลั๊กอินอย่าง DOTween Modules ก็อยู่ใน firstpass) | **ชั้นใน Options §3.1 ต้องกลับด้านบางส่วน:** `GameRoot` และ adapter ที่ครอบ manager เดิมอยู่ใน `Assets/Bar410/Host/` ซึ่ง **ไม่มี asmdef** ส่วน asmdef ใหม่ไม่รู้จัก manager เดิมเลย ต้องเรียกผ่าน interface; asmdef ใหม่ห้ามใช้ DOTween extension |
| F2 | Domain ไม่ใช่ C# ล้วน: `S_Drink` เป็น ScriptableObject และ Session ใช้ UnityEngine | `Bar410.Domain` อ้าง UnityEngine/Odin ได้; ก่อนย้ายต้องแยกไฟล์ `MinigameOutcome`, `HubPhase`, `Facing` ออกจากไฟล์ที่ปนกัน (G3-T0) |
| F3 | `GarnishManager` ต้องแยก (เก็บราว 75%): ส่วน 3D (วางแก้ว, เท) → `GarnishTable` ในฉากหลัก, ส่วนกติกา UI → ฉาก UI | ตรงกับการตัดสินใจ O4 แต่เป็นงานจริง ไม่ใช่แค่ย้ายฉาก |
| F4 | Garnish ต้องมี **2 await** (`RunAsync` ขั้น Garnish และ `ServeAsync` ขั้น Serve) ไม่ใช่ call เดียวที่เขียนไว้ใน Options §15.6 | ต้องให้คุณยืนยัน (คำถาม D7) |
| F5 | **ไม่พบ "แก้วในบาร์" ที่จะรับผล Garnish** (ไม่มี listener, `ClearGlass` รันตอนออกจาก Serve) ขัดกับคำตอบ Q2.5 ที่ว่าแก้วเป็นคนละชุดแล้วส่งกลับ | ต้องให้คุณชี้ว่าแก้วที่รับคือ object ไหน (คำถาม D6) ถ้าไม่มี ก็ให้ผลตกแต่งอยู่บนแก้วของ `GarnishTable` ตามของเดิม |
| F6 | จุดเรียก `Input.*` เก่าจริง **29 จุดใน 10 ไฟล์** (ไม่ใช่ 31) และ `N_InputManager.cs` ไม่ใช่ UTF-8 ต้องแปลงก่อนแก้ | grep ปกติพลาดไฟล์นี้ เกณฑ์ปิด input เก่า = `grep -a` + เล่นทุกฉาก (การเรียกเก่าพังตอนรัน ไม่พังตอน compile) |
| F7 | กล้อง **ไม่ได้หมุนอิสระ** แต่สลับมุมมองคงที่เมื่อเมาส์ชิดขอบจอ | ข้อ 17 ใน Options §9 (stick ขวาหมุนกล้อง) ต้องเปลี่ยนเป็น stick ขวา "สลับมุม" และปิด edge-hover เมื่อใช้ controller (คำถาม D8) |
| F8 | คำสั่ง Yarn เก่า `flow_*` ถูกลงทะเบียนอยู่แล้วทั่วโปรเจกต์ Yarn ปฏิเสธชื่อซ้ำ | verb ใหม่ต้อง "ลบแล้วเพิ่มใหม่" ที่ runner (ตรวจแล้วว่า dispatcher ไม่ถูกสร้างใหม่ ทุก `StartDialogue`) ไม่ใช่ประกาศชื่อซ้ำ |
| F9 | `TrySetResult` อาจไม่ resume โค้ดที่รออยู่ทันที แต่ Yarn อ่าน `flow_step()` ในบรรทัดถัดไปทันที | spike G1-0 ทดสอบก่อน พร้อมทางถอย F1 ใน G1-6 |
| F10 | `CameraManager.cs` มี `using Mono.Cecil.Cil;` ที่ไม่ได้ใช้ (ไลบรารี Editor) เสี่ยงทำ build พัง | ลบใน G3-T0 |
| F11 | `CameraController` เขียน settings asset ตอนรัน; มินิเกมนับการคลิกปุ่ม Cancel เป็น hit; `BTN_2_5D`/`InputSystemManager` ดูเหมือนไม่ถูกใช้ | แก้ใน I8/I9; ลบหลังตรวจ |
| F12 | ฉากหลักมี EventSystem 2 ตัว (ตัวที่สองอยู่ใน prefab `Text Animator Dialogue System`) | ต้องลบก่อนเช็ก "ฉาก UI ไม่มี EventSystem" ของ G3-T11 → งานของ G2 (N3) |
| F13 | save เก็บสถานะ ณ ตอนแสดง line ไม่ใช่ตอนกดปุ่ม; ทุกคำสั่งที่เปลี่ยน state ที่ save (`CharacterEnter`, `order_customer`, `unlock_ingredient`, `Switch_Camera`) ต้องรันซ้ำได้ปลอดภัย | เป็นกติกาของ G3 ทุก verb; autosave หลัง Serve รอ line ถัดไปที่ save ได้ |
| F14 | `SoundManager` ใช้ `PlayOneShot` จากแหล่งเดียว ไม่มี handle ต่อการเล่น | เสียง Expression ต้องมี handle (หยุดได้/รอจบ) → ไม่มีกลุ่มไหนรับงานนี้ (ช่องว่าง N2) |
| F15 | ภาษาไทย/Text Animator, ตัดคำ ฯลฯ ยังไม่ทดสอบ | spike G2-T02 ข้อ (f) พร้อมทางแก้ G2-T18 |

---

## 3. โครงสร้าง assembly ที่ตกลงกัน (รวมจาก G1/G3/G4)

| asmdef | ทำอะไร | อ้างอิง | เจ้าของ |
|---|---|---|---|
| `Bar410.Core` | `FlowAwait`, `Forcible<T>` | UnityEngine | G1 |
| `Bar410.Domain` | กติกาเครื่องดื่ม (`E_Cocktail`, `S_Drink`, Session ฯลฯ) | UnityEngine, Odin | G3 (งานย้าย N1) |
| `Bar410.Data` | `SO_DaySchedule` (GUID เดิม) และ SO อื่นที่ไม่ใช่ Domain | Core | G1 |
| `Bar410.Contracts` | interface/DTO ที่ Flow, Dialogue, Save, Features ใช้ร่วมกัน (ไม่มี MonoBehaviour) | UnityEngine (+Domain) | G1 เป็นเจ้าของ, G3/G4 เพิ่มไฟล์ |
| `Bar410.Flow` | `FlowState`, `DayRunner`, `BartendingSession`, `Station`, `FlowCommands` | Core, Data, Contracts | G1 |
| `Bar410.Features.*` / `WorldView` / `UiScenes` | โค้ดฟีเจอร์ใหม่, `WorldView`, loader ฉาก UI | Contracts | G3 |
| `Bar410.Dialogue` / `Verbs` / `Localization` | director, presenter, verb, ภาษา | Core, Contracts, Flow, Yarn | G2 |
| `Bar410.Save` / `Bar410.Input` | save/slot/replay, Input System | Contracts, Newtonsoft/InputSystem | G4 |
| *(ไม่มี asmdef)* `Assets/Bar410/Host/` | `GameRoot`, `MainWiring`, adapter ครอบ manager เดิม | ทุกอย่าง | G1 (+G3 adapter) |
| `*.Tests` | EditMode/PlayMode test | — | ทุกกลุ่ม |

asmdef ที่ต้องเห็น `GameRoot` (ซึ่งอยู่ใน Assembly-CSharp) ใช้ **static handle ภายใน asmdef** (เช่น `UiSceneRegistry`, `YarnVerbs.Bind`) ที่ `GameRoot` เป็นคนตั้งค่า

**ข้อขัดกันที่ยังไม่ปิด (ตั้งเป็นงาน N-RECON ที่ Opus ต้องเคลียร์ก่อนเขียนโค้ด):**
- `SO_DaySchedule`: G1 วางใน `Bar410.Data`, G3 วางใน `Bar410.Domain`
- `DayPart` และ `JournalEntry`/`YarnVars`: G1 (FlowTypes), G2 (A4), G4 (§1.5) ต่างนิยามของตัวเอง → ต้องมีที่เดียวใน Contracts
- ชื่อ/รูปร่างของ resume: G2 ขอ `flow snapshot + ResumeAt`, G4 ใช้ `IDayResume.ResumeAsync(DayPart, rootNode, ct)`, G1 เสนอ `DayRunner.ResumeAsync(ResumePoint)` → รวมเป็นสัญญาเดียว
- ports ของ Flow (G1) เทียบกับ interface ของ G3: ย้ายเข้า `Bar410.Contracts` ที่เดียว (G1 ระบุไว้แล้วว่าย้ายได้โดยไม่เปลี่ยน API)

---

## 4. API ที่ออกแบบ (ย่อ) — รายละเอียดอยู่ในไฟล์แนบ

### G1 — แกน flow (`API_G1_CoreFlow.md`)
| Type | ทำอะไร | แทนของเดิม |
|---|---|---|
| `FlowAwait`, `Forcible<T>` | รอแบบมี cancel; ปุ่ม Yarn "กดแทนผู้เล่น" จบ await ที่รออยู่ได้ | `FlowWiring` event-handlers |
| `FlowState` | แหล่งสถานะเดียว (`DayPart`, `BarStep`, session) + map ไปสตริง `flow_phase/flow_step` และคีย์ Hub | `GameLoopFSM` + states |
| `Station` (abstract MonoBehaviour), `StepContext`, `PrepareStation`/`GarnishStation`/`ServeStation` | แต่ละขั้นเป็นโค้ด async ลากใส่ `Station[] steps` ใน Inspector; Remake เริ่มที่ `steps[0]` | Level 3 states |
| `BartendingSession` | ชงหนึ่งแก้วครบวงจร (plain C#) มี `IsRunning`, `Cancel()` | `PrepareDrinksPhase` |
| `DayRunner` | วัน: Story → Open → Close → วันถัดไป, `Milestone`, `ResumeAsync` | Level 1/2 states |
| `FlowCommands` | ผิว Yarn: guard + คำเตือน + ตารางสตริง | `GameFlowCommands` |
| `GameRoot`, `MainWiring`, `Legacy*Adapter` | ผูกมือ, wiring ที่ทำงานตลอด, ครอบ manager เดิม | `FlowWiring` |

### G2 — บทสนทนา/Yarn/ภาษา (`API_G2_Dialogue.md`)
| Type | ทำอะไร |
|---|---|
| `ReplayPolicy`, `[YarnVerb(name, policy)]`, `YarnVerbRegistry` | verb ต้องระบุนโยบาย replay (ไม่ระบุ = compile error); เขียนทับคำสั่งเดิมที่ runner |
| `DialogueDirector` | เจ้าของสถานะบทสนทนาเดียว: mode (Live / SkipToOrder / Replay), line+options ที่แสดง, `LineIndex`, journal, `IsLineBusy`, **retype เมื่อเปลี่ยนภาษา** |
| `JournalEntry`/`YarnVars`/`DialogueJournal`, `DialogueSavePoint` | บันทึกตัวแปรที่ C# เขียน + ผลฟังก์ชัน + ค่าสุ่ม |
| `CocktailLinePresenter`, `CocktailOptionPresenter` | presenter บาง |
| `IVoicePlayer`/`IVoicePlayback`/`LineVoiceScope` + verb `<<voice Isla huh>>` | เสียง Expression: เข้าคิว, เล่นเฉพาะบรรทัดที่แสดงสด, หยุดเมื่อไปบรรทัดถัดไป, AutoPlay รอ |
| `EventLogView` | เก็บ line ID แปลข้อความใหม่เมื่อสลับภาษา |
| `Language`, `LocMarker`, `OrderDescriptionProvider`, `ISpeakerDirectory` | Unity Localization + token `[loc]` ในคำอธิบายออเดอร์ |
| `YarnValidator`, `TranslationValidator`, สคริปต์ CSV | ตรวจตอน build / ตรวจคำแปล |

### G3 — ฟีเจอร์ + host (`API_G3_Features.md`)
`IOrderDesk`, `IBarCounter` (ให้คะแนนผ่าน `CommitServe()` ที่ idempotent ครั้งเดียว), `IMinigameHost` (คืนแค่ `Completed|Cancelled`, Back คืนทันที), `IGarnishHost` (`RunAsync` + `ServeAsync`), `ICameraDirector`, `ICustomerStage`, `IIngredientUnlocks`, `IHub`, `IEventPopups`, `IChapterTitle`, `WorldView`/`WorldViewHandle`/`WorldViewSource` (ปิด `blocksRaycasts` เมื่อซ่อน, raycast ด้วย LayerMask), `UiSceneLoader`/`UiSceneEntry`/`UiSceneRegistry` (ฉาก UI additive ต้องไม่มี EventSystem/AudioListener/Brain; root ต้อง active ซ่อนด้วย Canvas)

### G4 — Save + Input (`API_G4_SaveInput.md`)
Save: `ISnapshotSource`, `IDialogueResumeSource`, `IReplayEngine`, `ReplayWatchdog`, `SaveData`/`SaveMeta`/`SlotId`, `SaveFileStore` (เขียน atomic), `SaveEligibility`, `SaveService`, `PendingLoad`, `ResumeCoordinator`, `AutosaveScheduler`, `SlotListView`
Input: `Bar410Controls.inputactions` (maps UI/Dialogue/Gameplay), `InputService` (mode stack Direct/Focus/Cursor), `IPointerSource`, `VirtualCursor` (software cursor), `InputGlyphs`+`ButtonPrompt`, `FocusOnEnable`, `ActionInputProvider` (มินิเกม), `DialogueAdvanceInput` (แทน `PauseAwareKeyCodes`)

---

## 5. ช่องว่างที่ไม่มีกลุ่มไหนรับ (ผมเพิ่มเป็นงานใหม่)

| id | งาน | ที่มา | model |
|---|---|---|---|
| N0 | ไฟล์ค่าอ้างอิงพฤติกรรมเดิม: รัน `PLAYTEST_Demo` และ `TEST_AllCommands` ในฉากเดิม บันทึก `flow_phase/step` ต่อขั้น + checklist | R0 ใน Options §6 | sonnet (รันผ่าน Unity MCP) |
| N-RECON | เคลียร์ข้อขัดกันใน §3 → `Docs/rewrite/Contracts_Final.md` | ข้อขัดกันระหว่าง G1–G4 | **opus** |
| N1 | ย้าย Domain เข้า `Bar410.Domain` + EditMode test กฎ GDD §15–18 | Options R2; G3 §1 ระบุรายการ type; ไม่มีใครรับ | sonnet (opus ตรวจ) |
| N2 | `SoundManager` ให้ handle ต่อการเล่น (`AudioSource` ต่อ call, `Stop`, `Ended`) | G2 ขอจาก G3; G3 ไม่มีงาน | sonnet |
| N3 | ลบ EventSystem ตัวที่สองใน prefab `Text Animator Dialogue System` | G3 X3 คาดหวังจาก G2; G2 ไม่มีงาน | haiku |
| N4 | `S_Drink` มี id คงที่ (RecipeKey) ใช้ใน save และ key ภาษา | G4 ขอจาก G3 data; G2 ขอ RecipeKey | haiku |
| N5 | ย้ายข้อความใน SO (36 สูตร, วัตถุดิบ 17, chapter 21, ชื่อตัวละคร 8, popup) เป็น `LocalizedString`/key | G2 คาดหวังจาก G3; Options §11.3 | sonnet |
| N6 | แปลง `N_InputManager.cs` เป็น UTF-8 (commit แยก) | G4 §0 | haiku |

---

## 6. รายการงานทั้งหมด จัดตามเฟสและ model ที่ใช้

สัญลักษณ์: ระดับยาก 1–5 · **O** = Opus, **S** = Sonnet, **H** = Haiku · ⚙ = ต้องทำใน Unity Editor ผ่าน MCP (ทำทีละงาน ห้ามขนาน)
รวม 71 งานจากสเปก (Opus 19 · Sonnet 35 · Haiku 17) + 8 งานเสริมใน §5 (Opus 1 · Sonnet 4 · Haiku 3) = 79 งาน (Opus 20 · Sonnet 39 · Haiku 20) งานที่ติดป้าย "เงื่อนไข" ทำเมื่อเข้าเงื่อนไขเท่านั้น

### R1 รากฐาน + spike (ผลของ spike ต้องกลับไปแก้สเปกก่อนไปเฟสถัดไป)
| id | งาน | ยาก | model | ขึ้นกับ |
|---|---|---|---|---|
| G1-0 | spike S1–S6: `Awaitable`/`TrySetResult` resume ทันทีไหม, คำสั่ง Yarn คืน `Awaitable`, cancel ตอน unload ฯลฯ | 2 | O | R0 |
| G2-T02 | spike: Unity Localization provider EN/TH, `Yarn.Line` re-fetch + retype, ลบ-เพิ่มคำสั่ง Yarn, ภาษาไทยใน Text Animator, เวลา boot ⚙ **ต้องติดตั้ง `com.unity.localization` (ต้องได้รับอนุญาต)** | 4 | O | R0 |
| G4-S0 | spike: ลำดับ event ของ detour, `GetAllVariables` round trip, `File.Replace` บน Linux | 2 | S | R0 |
| G4-I0 | spike: virtual cursor + `InputSystemUIInputModule` + RenderTexture relay + timeScale 0 (ต้องใช้จอยจริงของคุณ) | 3 | O | R0 |
| N-RECON | เคลียร์ข้อขัดกันของ contract | 4 | O | spike ทั้ง 4 |
| G1-1 | โครง asmdef + โฟลเดอร์ | 1 | H | N-RECON |
| G3-T0 | แยกไฟล์ type ที่ปนกัน, ลบ `using Mono.Cecil.Cil` | 1 | H | — |
| G2-T01 | asmdef ของ Dialogue/Verbs/Localization + ก๊อปปี้ settings | 1 | H | G1-1 |
| G2-T15 | ปุ่ม "Add Line Tags to Yarn Scripts" แล้ว commit แยก (เจ้าของกดเอง) | 1 | H | R0 |
| N6 | `N_InputManager` → UTF-8 | 1 | H | R0 |
| G3-T1 / G4-S1 | Contracts + DTO ทั้งหมด (รวมไฟล์เดียว) | 2 | S / H | N-RECON |

### R2 Domain + พื้นฐาน flow
| id | งาน | ยาก | model | ขึ้นกับ |
|---|---|---|---|---|
| N1 | ย้าย Domain + test กติกา ⚙ | 3 | S (O ตรวจ) | G3-T0 |
| G1-2 | `FlowAwait` + `Forcible<T>` + test | 3 | O | G1-0 |
| G1-3 | ย้าย `SO_DaySchedule` เข้า `Bar410.Data` (GUID เดิม) ⚙ | 1 | H | G1-1 |
| G1-4 | `FlowTypes` + `FlowState` + test ตารางสตริง | 2 | S | G1-2 |
| G1-5 | ports + fakes | 2 | S | N-RECON |

### R3 Vertical slice: ชงหนึ่งแก้วครบวงจร (ไม่มี Yarn) + จุดตัดสิน
| id | งาน | ยาก | model | ขึ้นกับ |
|---|---|---|---|---|
| G1-6 | `Station`, `StepContext`, `GarnishStation`, `ServeStation` | 3 | S | G1-2, G1-5 |
| G1-7 | `PrepareStation` | 4 | O | G1-6 |
| G1-8 | `BartendingSession` | 4 | O | G1-7 |
| G1-9 | `DayRunner` | 4 | O | G1-8 |
| G1-10 | `FlowCommands` | 2 | S | G1-9 |
| G3-T2 | `WorldView` | 3 | S | G3-T1 |
| G3-T3 | `UiSceneLoader`/`UiSceneEntry` | 4 | O | G3-T1 |
| G3-T4 | `CocktailFeature` (IOrderDesk+IBarCounter) | 3 | S | G3-T1, N1 |
| G3-T5 | `CameraDirector` | 2 | H | G3-T1 |
| G3-T6 | Unlocks/Hub/EventPopup/ChapterTitle facade | 2 | H | G3-T1 |
| G3-T7 | `CustomerStage` + `CharacterView.SetSpeaking` | 3 | S | G3-T1 |
| N2 | `SoundManager` handle | 2 | S | — |
| G1-11 | adapter ของ manager เดิม (Opus ตรวจ `LegacyMinigames`, `LegacyDayUi`) | 3 | S | G1-5 |
| G3-T8 | แยก Minigame: UI scene + world source ⚙ | 4 | O | G3-T3, G3-T2 |
| G3-T9 | แยก Garnish: `GarnishTable` + UI scene ⚙ | 4 | O | G3-T8 |
| G3-T10 | glue + preload ตอน BeforeOpen | 2 | S | G3-T9 |
| G1-12 | `GameRoot` + `MainWiring` | 3 | S | G1-10, G1-11 |
| G1-13 | ฉาก `Main_V2` ⚙ | 2 | S | G1-12 |
| G3-T11 | wiring ฉาก (ต้องหลัง G1-13; ห้ามแก้พร้อมกัน) ⚙ | 3 | S | G1-13, N3 |
| G4-I1 | action asset + class (G2-T09 ต้องใช้) | 2 | S | G4-I0 |
| G1-14 | debug hotkeys (Editor) | 1 | H | G1-10 |
| G3-T12 | วัด fps/hitch/raycast ที่จุดตัดสิน | 2 | S | G3-T11 |
| G1-15 | parity run เทียบไฟล์ N0 | 3 | S | G1-13, N0 |

**จุดตัดสิน R3 (หยุดรอคุณ):** (1) ผ่านเกณฑ์ ≥60 fps / สะดุด ≤300 ms และคลิกผ่าน RT (2) เพิ่ม Station หรือ verb ใหม่ง่ายกว่าเดิมจริงไหม ถ้าไม่ ย้อนไปแผนระดับ A (3) ตัดสินว่าจะคง UI scene แยกหรือให้ UI อยู่ฉากหลัก

### R4 วันและบทสนทนา (G2 เป็นหลัก)
| id | งาน | ยาก | model | ขึ้นกับ |
|---|---|---|---|---|
| G2-T03 | `Journal` + `YarnVars` + test | 2 | S | R3 |
| G2-T04 | verb layer (`ReplayPolicy`, registry, gate, test) | 5 | O | G2-T02 |
| G2-T05 | `DialogueDirector` (Live + skip) | 4 | O | G2-T04 |
| G2-T07 | `CocktailLinePresenter` + retype | 4 | O | G2-T02 |
| G2-T08 | `CocktailOptionPresenter` | 3 | S | G2-T05 |
| G2-T09 | controls / input / SkipScan | 2 | S | G4-I1 |
| G2-T10 | `EventLogView` | 3 | S | G2-T05 |
| G2-T06 | voice (`<<voice>>`) | 3 | S | G2-T05, N2 |
| N3 | ลบ EventSystem ตัวที่สอง ⚙ | 1 | H | — |
| N4 | RecipeKey ที่ `S_Drink` | 1 | H | N1 |
| G2-T11 | runtime ภาษา + retype wiring | 3 | S | G2-T05, G2-T07 |
| N5 | ข้อความใน SO → `LocalizedString` | 3 | S | G2-T11 |
| G2-T13 | shim ชื่อเดิม + ตารางใน `Docs/YarnCommands/README.md` | 2 | H | G2-T04 |
| G2-T14 | `YarnValidator` | 3 | S | G2-T04 |
| G2-T16 | `TranslationValidator` + สคริปต์ CSV | 2 | S | G2-T11 |

### R5 Save / Replay
| id | งาน | ยาก | model | ขึ้นกับ |
|---|---|---|---|---|
| G4-S2 | `ReplayWatchdog` + test | 3 | O | G4-S1 |
| G4-S3 | `SaveData`/`SaveFileStore` + test | 2 | S | G4-S1 |
| G4-S4 | `SaveEligibility`/`SaveService`/`PendingLoad` | 3 | S | G4-S3 |
| G2-T12 | replay ใน director (detour, loop, journal, options) | 5 | O | G2-T05, G4-S2 |
| G4-S5 | `ResumeCoordinator` | 5 | O | G4-S4, G2-T12, G1-9 |
| G4-S6 | `AutosaveScheduler` | 4 | O | G4-S4 |
| G4-S7 | UI เลือก slot (ใช้จอยได้) | 3 | S | G4-S4, G4-I4 |
| G4-S8 | ลบโค้ด save เก่า | 2 | H | G4-S5 |

### R6 Input + controller
| id | งาน | ยาก | model | ขึ้นกับ |
|---|---|---|---|---|
| G4-I2 | `InputService` + `IPointerSource` + test | 4 | O | G4-I0, G4-I1 |
| G4-I3 | `VirtualCursor` | 3 | O | G4-I2 |
| G4-I4 | `FocusOnEnable` + select feedback | 2 | S | G4-I2 |
| G4-I5 | `InputGlyphs` + `ButtonPrompt` | 3 | S | G4-I1 |
| G4-I6 | แทนจุดเรียกแบบกลไก | 1 | H | G4-I2 |
| G4-I7 | `N_InputManager`, `HoverTooltip`, `PauseManager`, `DialogueAdvanceInput` | 3 | S | G4-I2, N6 |
| G4-I8 | `CameraController` + จอย | 3 | S | G4-I2, D8 |
| G4-I9 | `ActionInputProvider` (มินิเกม) + Back | 2 | S | G4-I2 |
| G4-I10 | ปิด input เก่า (`grep -a` + เล่นทุกฉาก) | 1 | H | I6–I9 |

### R7 ภาษา / เนื้อหา
| id | งาน | ยาก | model | เงื่อนไข |
|---|---|---|---|---|
| G2-T17 | `SeenLines` | 1 | H | ถ้า D1 = ต้องการ |
| G2-T18 | แก้ภาษาไทยกับ Text Animator | 2 | S | ถ้า spike (f) ไม่ผ่าน |
| G3-T14 | ผล Garnish ลงแก้วอีกใบ | 2 | S | ถ้า D6 ยืนยัน object |
| — | ฟอนต์ไทย, รูปมีตัวอักษร, ทดสอบเนื้อหา 21 วัน | — | — | ต้องการงานจาก Designer/Artist/ผู้แปล |

### R8 สลับ ลบ เอกสาร
| id | งาน | model |
|---|---|---|
| G1-16 | ADR 0004 + Systems Guide + CLAUDE.md | H |
| G2-T19 | `Bar410_Dialogue_HowItWorks.md`, `Bar410_Localization_HowItWorks.md`, `Docs/YarnCommands/voice.md`, `serve_order.md` | H |
| G3-T13 | Systems Guide + ตาราง Component | H |
| — | เทียบ parity ทั้งชุด, ย้าย `Main_V2` เป็นฉากหลัก, ลบของเก่า (HSM, `FlowWiring`, presenter เก่า, `BTN_2_5D` ฯลฯ) | O ตรวจ |

---

## 7. ขั้นตอนตอนรัน (Opus ตรวจทุกงาน)

บทบาท: **ผู้ประสานงาน** (ผม/เซสชันนี้) แจกงานและรัน Unity MCP · **ผู้ลงมือ** (Sonnet/Haiku subagent ตามตาราง) เขียนไฟล์ · **ผู้ตรวจ** (Opus subagent) ตรวจตาม checklist ของงานนั้นในไฟล์แนบ

ต่องาน:
1. ผู้ประสานงานส่ง prompt ที่มี: ข้อความงาน + ส่วน spec ของ type ที่เกี่ยวข้อง + กฎโปรเจกต์ (CLAUDE.md: SOLID/KISS, comment+commit ภาษาอังกฤษ, ห้ามแก้ของเดิมนอกรายการที่ระบุ, ห้ามแตะ prefab `[GameLoop]` — ใช้ override บน instance) + definition of done
2. ผู้ลงมือเขียนไฟล์ เสร็จแล้ว **ไม่** รัน Unity (มี editor ตัวเดียว)
3. ผู้ประสานงานรัน `refresh_unity` → `read_console` ต้องไม่มี error → รัน EditMode test ผ่าน MCP
4. ผู้ตรวจ (Opus) อ่าน diff เทียบ spec + checklist ให้ผล `PASS` / `REVISE (ข้อ...)`
5. `REVISE` ส่งกลับผู้ลงมือได้ **สูงสุด 2 รอบ** ถ้ายังไม่ผ่านให้ Opus ลงมือเองหรือหยุดถามคุณ ไม่ทำต่อแบบเดา
6. `PASS` → commit ต่องาน (ภาษาอังกฤษ, แยกตามเรื่อง) บน `RefactorCode` ไม่ push

กฎความปลอดภัย:
- งานที่ติด ⚙ รันทีละงานโดยผู้ประสานงาน เพราะ Unity recompile ทำให้ MCP หลุดราว 2 นาที และแก้ฉากพร้อมกันไม่ได้
- งานคนละโฟลเดอร์ที่ไม่ติด ⚙ รันขนานกันได้ (เช่น G1 กับ G4-S) แต่ compile/test ทำรวมทีเดียว
- ห้ามย้าย/ลบไฟล์ Unity ด้วย shell (ต้องคง `.meta`/GUID) ใช้ Editor ผ่าน MCP หรือ `git mv` พร้อม `.meta`
- ฉากเดิม `New Drag Drop System` ต้องไม่เปลี่ยน: `git diff` ของ `.unity` นั้นต้องว่างตลอด จนถึง R8
- ทุกงานต้องรายงานสิ่งที่ **ไม่ได้ทดสอบ** (เช่น จอยจริง ภาพจริง) ตรงๆ ตาม CLAUDE.md
- ทุกเฟสจบด้วยการหยุดรายงานคุณ (พร้อมรายการ commit และผลทดสอบ) ก่อนเริ่มเฟสถัดไป

ข้อจำกัดที่ต้องรู้: ผมทดสอบด้วยจอยจริง ดูภาพ/ฟอนต์ไทยด้วยตา และตัดสินความรู้สึกของการเล่นไม่ได้ เฟส R3, R6, R7 จึงต้องมีคุณลองเล่น

---

## 8. คำถามที่ต้องการคำตอบ (เขียนใต้ `**คำตอบ:**`)

### D1. โหมด "ข้ามบทที่เคยอ่านแล้ว" (G2 Q1)
G2 ตีความว่าคุณอาจต้องการโหมด Skip ใหม่ที่จำบรรทัดที่อ่านแล้ว (ต้องมี `SeenLines`) ค่าเริ่มต้นคือ **ไม่ทำ** (Skip เดิมคือข้ามไปถึงออเดอร์) ที่คุณตอบก่อนหน้านี้ว่า "ข้ามเสียงของบทที่เล่นไปแล้ว" ตั้งใจให้หมายถึงอะไร
- [ ] แค่ตอนกด Skip (แบบเดิม) ไม่ต้องเล่นเสียง ไม่ต้องมีโหมดใหม่
- [ ] ต้องการโหมดข้ามบทที่เคยอ่านแล้วด้วย (ทำ G2-T17)

**คำตอบ:**

### D2. ชื่อผู้พูดในไฟล์แปลภาษาไทย (G2 Q2)
ให้คงชื่ออังกฤษนำหน้า (`Isla: ...`) เป็น ID ของผู้พูด แล้วแสดงชื่อไทยจากตาราง `character.<id>` (ค่าเริ่มต้น: ใช่)

**คำตอบ:**

### D3. ชื่อ verb เสียง
`<<voice Isla huh>>` (ค่าเริ่มต้น: ใช้ตามนี้)

**คำตอบ:**

### D4. ไฟล์แปล
ผู้แปลรับ-ส่งเป็นสเปรดชีต G2 ตั้งค่าเริ่มต้นเป็น CSV บันทึกแบบมี BOM (Excel เปิดไทยได้) ไม่มีสคริปต์ xlsx ยอมรับไหม หรือต้องมีสคริปต์แปลง `.xlsx`
- [ ] CSV (BOM) พอ
- [ ] ต้องมีสคริปต์ `.xlsx` ↔ CSV

**คำตอบ:**

### D5. มินิเกมที่ไม่มีเกมรองรับ (G3 U7)
โค้ดเดิมค้างตรงนี้ เสนอให้ log error แล้วนับเป็น `Completed` เพื่อไม่ให้ออเดอร์ค้าง ตกลงไหม

**คำตอบ:**

### D6. แก้วที่รับผล Garnish (G3 U4 / ขัดกับคำตอบ Q2.5)
คุณบอกว่าแก้วของ Garnish เป็นคนละชุดและส่งผลกลับให้บาร์ แต่ G3 หา object แก้วที่จะรับผลนี้ไม่พบ (ไม่มี listener และแก้วถูกล้างตอนออกจาก Serve) ใน Hierarchy จริง แก้วที่รับผลคือ object ไหน หรือไม่มีและให้ผลตกแต่งอยู่บนแก้วของ Garnish เหมือนของเดิม

**คำตอบ:**

### D7. Garnish เป็นสอง call
`IGarnishHost` แยก `RunAsync` (ขั้น Garnish) กับ `ServeAsync` (ขั้น Serve) เพื่อให้ตรงกับ Station แยกและ Hub ที่สลับ Garnish→Serve ยืนยันไหม (แทน call เดียวที่ตกลงไว้เดิม)

**คำตอบ:**

### D8. กล้องกับ controller (F7)
กล้องจริงสลับมุมคงที่เมื่อเมาส์ชิดขอบ ไม่ได้หมุนอิสระ ให้ controller ทำอย่างไร
- [ ] stick ขวาแตะแล้วสลับมุมถัดไป (แนะนำ) และปิด edge-hover เมื่อใช้จอย
- [ ] กดค้างเพื่อ "แอบดู" แล้วกลับ
- [ ] อื่นๆ

**คำตอบ:**

### D9. Save เมื่อแก้บท
G4 เสนอเก็บ hash ของโปรแกรม Yarn ใน save และปฏิเสธถ้าไม่ตรง แปลว่าแก้ `.yarn` เมื่อไรก็ตาม save ตอนพัฒนาใช้ไม่ได้ (มีสวิตช์ใน dev menu ให้ข้าม) หรือจะใช้ `SaveData.Version` ที่ยกเลขเองเมื่อแก้บท
- [ ] hash (แนะนำ)
- [ ] ยกเลขเอง

**คำตอบ:**

### D10. Autosave ตอนจบวัน
ปกติอยู่ห่างจาก autosave ต้นวันถัดไปแค่หนึ่ง line G4 เสนอตัดจังหวะ "จบวัน" ออก (เหลือ: ต้นวันหลัง Event, หลัง Serve) ตกลงไหม (ข้อ 18 ใน Options §9 ระบุ 3 จังหวะ)

**คำตอบ:**

### D11. ปุ่มของจอยที่ยังไม่กำหนด
(ดู `API_G4_SaveInput.md` §2.1 และส่วน (d) ข้อ 10) เช่น ปุ่มเดินบทสนทนา/Hub บน Xbox และ PlayStation, ในมินิเกมเขย่า = กดรัว หรือ stick แบบ analog, parallax ของ UI ตอนใช้จอย (นิ่ง หรือใช้ stick ขวา), ยังต้องใช้ `#save_checkpoint` ไหม
ถ้ายังไม่ตัดสิน ผมจะใช้ค่าเริ่มต้นของ G4 และแจ้งไว้ตอนรัน I1

**คำตอบ:**

### D12. อนุญาตให้ติดตั้ง `com.unity.localization`
ต้องแก้ `Packages/manifest.json` ตอน spike G2-T02 (บน branch `RefactorCode` ย้อนกลับได้) อนุญาตเมื่อถึงงานนั้นไหม

**คำตอบ:**

### D13. จุดตรวจให้คุณลองเล่น
เฟสที่ต้องหยุดรอคุณลองเล่นจริง: จบ R3 (ชงแก้วเดียว), จบ R4 (เดโม 3 วัน), R6 (จอย) ตกลงตามนี้ไหม หรืออยากเพิ่มจุดอื่น

**คำตอบ:**

### D14. ลำดับการรัน
ต้องการให้รันตามนี้ไหม: R0 (git) → R1 (spike + รากฐาน) → หยุดรายงาน → R2 → R3 → หยุดรอคุณลอง … หรืออยากให้รัน R0 อย่างเดียวก่อน

**คำตอบ:**

---

## 9. สิ่งที่ยังไม่ได้ตรวจ / ความเสี่ยงของเอกสารนี้

- ผมยังไม่ได้อ่านไฟล์แนบทั้ง 255 KB ทุกบรรทัด ส่วนที่ผมอ่านเต็มคือหัวข้อการตัดสินใจ, ตารางงาน, ข้อขัดกันและสมมติฐานของทั้งสี่ไฟล์ รายละเอียด signature ผมอาศัยการสรุปของ Opus แต่ละตัวและให้ N-RECON ตรวจความสอดคล้องก่อนเขียนโค้ด
- Opus แต่ละตัวทำงานแยกกัน จึงมีข้อขัดกันที่ระบุไว้ใน §3 อาจยังมีข้อที่ผมไม่เห็น
- ตัวเลขจำนวนงานและระดับ model เป็นของ Opus ต่อกลุ่ม ผมไม่ได้ปรับ นอกจากเพิ่มงานเสริมใน §5
- ไม่มีการประเมินเวลา (ขนาดงานเป็นระดับยาก 1–5) และไม่ได้ประเมินค่าใช้จ่ายของการรัน subagent ~80 งาน ซึ่งมากกว่าการทำเซสชันเดียวมาก ควรรันทีละเฟส
- โค้ดทั้งหมดในไฟล์แนบเป็นร่าง ไม่ได้คอมไพล์ ข้อสมมติเรื่อง `Awaitable`, ลำดับ event ของ Yarn detour, ภาษาไทยกับ Text Animator, virtual cursor กับ `InputSystemUIInputModule` ต้องผ่าน spike ใน R1 ก่อน
