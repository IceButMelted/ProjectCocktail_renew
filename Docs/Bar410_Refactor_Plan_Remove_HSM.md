# Bar410 — แผน Refactor: เลิกใช้ HSM/FSM

เขียนเมื่อ 2026-11-06 อ้างอิงจาก `Bar410_CodeMap_MainScene.md` (snapshot ก่อน refactor)
ขอบเขต: ฉากหลัก `New Drag Drop System` เท่านั้น

Stack ที่ใช้: Yarn Spinner Pro (3.x + Text Animator integration), Odin Inspector, DOTween (free),
**`Awaitable` ของ Unity 6 (ไม่ติดตั้ง package เพิ่ม)** ยังไม่ใช้ SOAP

สถานะ: **Draft v4** (2026-11-06) ตัดสินใจแล้ว: ใช้ `Awaitable` แทน UniTask, Save ที่ line ปัจจุบัน + Silent Replay รองรับ `<<detour>>`
(ยกเลิก checkpoint ต้นวันที่ตัดสินไว้ใน v3)

**v4 แก้อะไรจาก v3:** Phase 4 เขียนใหม่เป็น "Save ที่ line ปัจจุบัน + Silent Replay" หลังอ่านโค้ด `SaveLoadManager` / `SceneLoaderBridge` เจอช่องโหว่ 4 จุดของ replay เดิม
(ตัวแปร save ผิดจุด, หยุดด้วย `TextID` ซ้ำ, ตัวแปรที่ C# เขียนไม่เกิดตอน replay, presenter ในฉากไม่มี logic replay) เพิ่ม `DayRunner.ResumeAsync` (§6.4),
§10 ข้อ 1/4/5 ปิดแล้ว, เพิ่ม checklist/ความเสี่ยง/ไฟล์ใหม่ ถ้าวันหลังกลับไปใช้ checkpoint ต้นวัน Phase 4 จะเล็กลงมาก (ไม่ต้องมี root-start snapshot, journal, `LineIndex`, `ResumeAsync`)

**v2 แก้อะไรจาก v1** (จากการวิเคราะห์เทียบกับโค้ดจริง)
1. เปลี่ยน UniTask → `Awaitable` ทั้งแผน (§6 skeleton ใหม่ทั้งหมด, Phase 1 เป็น spike ตรวจ API แทนการติดตั้ง)
2. `flow_phase()` / `flow_step()` ต้องคืนค่าเดิมทุกตัว เพิ่มตารางแปลงใน §5.1 (v1 ทำไม่ได้เพราะ enum ใหม่คนละชุด)
3. §4 เพิ่มตาราง B: งานของ `FlowWiring` ที่ทำงานตลอดเวลา ไม่ผูกกับ phase (v1 ตกหล่น)
4. Phase 0 เพิ่มขั้น commit งานค้างก่อนเปิด branch และใช้ `PLAYTEST_Demo.yarn` / `TEST_AllCommands.yarn` เป็น regression
5. Command ที่เรียกผิดจังหวะต้อง log warning (ของเดิมมี "Illegal transition"), `DayRunner` ต้องมี guard เหมือนเดิม
6. `OpenBarAsync` เข้าโหมดคุยก่อนแล้วค่อยโชว์ chapter title (ตรงกับลำดับเดิม)
7. Phase 4 ต้องคืน `DayPart` และโหมดบาร์ตอน load (v3 เคยยกเลิกข้อนี้ v4 นำกลับมา เพราะ save กลางวันได้อีกครั้ง)
8. Phase 5 เพิ่ม ADR 0004

---

## 1. ปัญหา

- Flow ของเกมเป็น HSM 3 ชั้น (`GameLoopFSM` → `OpenBarPhase` / `BeforeOpenPhase` → `PrepareDrinksPhase`)
  การเพิ่มขั้นตอนใหม่ต้องแก้ enum, ตาราง transition, state class และ enter/exit hook ใน `FlowWiring` พร้อมกัน
- `FlowWiring` ใหญ่ 31 KB ทำ 5 หน้าที่ในไฟล์เดียว (เริ่มฉาก, กล้อง/ล็อก, hub, event popup, garnish/serve, minigame bridge)
- ลำดับ `Entered` ของ container เกิดหลัง state ลูก (code map §8.1) ทำให้ลำดับการทำงานเดายาก
- Save/Load แยกขาดจาก flow (§8.5) เพราะ phase ของ FSM ไม่ถูกบันทึก

สิ่งที่ **ไม่ใช่** ปัญหาและไม่ต้องแก้: manager ต่างๆ (Cocktail, Garnish, Minigame, Camera, Hub, Customers) แยกกันดีอยู่แล้ว
ไม่รู้จัก flow และไม่รู้จักกันเอง ชั้น Domain ของ Cocktail เป็น pure C# อยู่แล้ว

สิ่งที่ได้จริงจากการเปลี่ยน: โค้ดรวมไม่ได้น้อยลง (ตรรกะของ `FlowWiring` ย้ายไปอยู่ station) แต่ได้ตรรกะ "เข้า/ออก" ของแต่ละขั้นอยู่ที่เดียว,
`try/finally` ปิด UI ให้เอง, ลำดับอ่านจากบนลงล่าง, และ Save/Load ที่ไม่ต้อง save phase

## 2. การตัดสินใจ

แทน state machine ด้วย **โค้ด async ที่อ่านจากบนลงล่าง**

- **Yarn ยังเป็นผู้กำกับ** ลำดับของวันอยู่ใน `.yarn` เหมือนเดิม
- **วงจรชงเครื่องดื่ม** (Prepare → Garnish → Serve → Remake) กลายเป็น `while` loop เดียวใน `BartendingSession`
- **แต่ละขั้น** เป็น *Station* class เล็กๆ ที่มี `RunAsync()` ซึ่งรวม "ตอนเข้า" และ "ตอนออก" (`try/finally`) ไว้ในที่เดียว
- **วงจรวัน** (BeforeOpen / Event / Story / Open / Close) ย้ายไปอยู่ใน `DayRunner` ซึ่งถูกเรียกจาก Yarn command
- **ชื่อ Yarn command และค่าที่ Yarn function คืน คงเดิมทั้งหมด** ไม่ต้องแก้ไฟล์ `.yarn` เลย

### หลักที่ใช้ตลอดแผน

1. **ขอให้ทำ → เรียกตรง / await** · **แจ้งว่าทำแล้ว → C# event** (manager มี event อยู่แล้ว)
2. **Enum บอกขั้น = ป้ายเท่านั้น** (`CurrentStep`, `CurrentPart`) ไม่มีตาราง transition ไม่มีใครตรวจสิทธิ์จากมัน
3. **การป้องกันการกดผิดจังหวะ** เกิดเองจากการที่ station ที่ไม่ได้ทำงานจะไม่ได้ subscribe event ของปุ่ม
   **แต่ต้องไม่เงียบ:** command ที่ไม่มีใครฟัง (`flow_garnish_done` ตอนไม่ได้อยู่ใน Garnish) ต้อง `Debug.LogWarning` เหมือนที่ "Illegal transition" เคยเตือน
4. **ทุกอย่างที่เปิด ต้องปิดใน `finally`** เพื่อให้การยกเลิก (เปลี่ยนฉาก, remake) ไม่ทิ้ง UI ค้าง
5. **ลำดับการ subscribe สำคัญ:** subscribe ก่อนทำ action ที่ยิง event นั้น (เช่น subscribe `ShakerContents.Cleared` ก่อนเรียก `Clear()` ตอนเข้า Prepare)

### ทำไม `Awaitable` ไม่ใช่ UniTask

- ไม่เพิ่ม dependency และไม่เปลี่ยนชนิดของ `YarnTask`: Yarn Spinner 3 เลือก `YarnTask` จาก define `USE_UNITASK` ถ้ามี UniTask ก็จะ
  ใช้ UniTask แทน `Awaitable` ที่ใช้อยู่ตอนนี้ และ presenter ทุกตัวที่ใช้ `YarnTask` ถูก compile ใหม่บนชนิดใหม่
- Unity 6 มี `AwaitableCompletionSource<T>` ให้รอ event ได้ และ `async Awaitable` คืนค่าได้
- ข้อจำกัดที่ต้องระวัง (ดู §11): await ได้ครั้งเดียวต่อ instance, ไม่มี `Forget()` / `AttachExternalCancellation` ในตัว
  (แก้ด้วย helper ใน §6.0), `WaitForSecondsAsync` ใช้เวลาแบบ scaled (หยุดตอน Pause)

---

## 3. โครงสร้างใหม่

```
Yarn (.yarn)                  ← ไม่แก้
  │ <<flow_*>> ชื่อเดิมทุกตัว
GameFlowCommands              ← ประตูเดิม แต่ชี้ไปที่ DayRunner / BartendingSession แทน FSM
  ├─ DayRunner                ← เริ่มฉาก, เริ่มวัน, popup ปลดล็อก, เริ่มเรื่อง, เปิด/ปิดร้าน, วันถัดไป
  └─ BartendingSession        ← loop ชงเครื่องดื่ม
       ├─ PrepareStation      ← 2 / 2.1 / 2.2 (ใส่ส่วนผสม ⇄ มินิเกม)
       ├─ GarnishStation      ← 3
       └─ ServeStation        ← 4
Managers เดิม                 ← ไม่แก้ (หรือแก้น้อยมาก: เพิ่มเมธอด async ห่อของเดิม)
```

แนะนำให้วาง component ใหม่ไว้ที่ `[GameLoop]` (แทนที่ `GameLoopFSM` + `FlowWiring`)
และวางไฟล์ไว้ที่ `[02]Script/Game Flow/` (โฟลเดอร์ใหม่ แทน `Hierarchical State Machine/`)

หมายเหตุสถาปัตยกรรม: หลักเดิมใน CLAUDE.md ("`FlowWiring` คือที่เดียวที่ flow กับ manager มาเจอกัน", "state เป็น C# ล้วน")
จะไม่จริงอีก station แต่ละตัวถือ reference ของ manager 5–6 ตัวเอง manager ยังไม่รู้จัก station และไม่รู้จักกันเหมือนเดิม
ต้องเขียน ADR 0004 และแก้ CLAUDE.md (Phase 5)

---

## 4. งานใน `FlowWiring` ย้ายไปไหน

อ้างอิงตาราง §3.3 ของ code map

### ตาราง A — งานที่ผูกกับ phase (เข้า/ออก)

| งานเดิม (FlowWiring) | เจ้าของใหม่ |
|---|---|
| `Start()`: ล็อกบาร์, Hub = None, รอ `PanelTransition`, `StartDay()` | `DayRunner.Start()` |
| BeforeOpen enter: กล้อง look fixed + reset + cut ไปบาร์ | `DayRunner.StartDayAsync()` บรรทัดแรก |
| Event enter: ล็อกบาร์, Hub = BeforeOpen, `RefreshBottles`, popup ปลดล็อก, รอ `AllClosed` | `DayRunner.ShowDayEventsAsync()` |
| Story enter (ครั้งแรกของวัน): `StartStory(node ของ $day)` | `DayRunner.StartDayAsync()` บรรทัดสุดท้าย |
| Open enter: `ChapterTitleUI.Show`, กล้องไปบาร์ | `DayRunner.OpenBarAsync()` |
| 1 Talking enter: ปิด ingredient, look fixed, ปิดหนังสือ, Hub = Talking, กล้องไปบาร์ | `DayRunner.EnterTalkingMode()` เรียกจาก `OpenBarAsync()` (**ก่อน** chapter title ตามลำดับเดิม) และจาก `finally` ของ `BartendingSession` |
| 1 Talking exit: เตือนถ้าไม่มี order | `BartendingSession.Begin()` ตรวจ `Order.HasOrder` แล้ว warning |
| 2 Prepare enter: เคลียร์ shaker, reset panel permission, เคลียร์ผล order, กล้องไปบาร์, post-it | `PrepareStation.RunAsync()` ตอนเข้า (**subscribe ก่อน** `Clear()`) |
| 2 Prepare exit: ปิดหนังสือ, post-it ออก, look fixed | `PrepareStation.RunAsync()` `finally` |
| 2.1 AddIngredient enter/exit | `PrepareStation` ช่วงรอใส่ส่วนผสม |
| 2.2 Minigame enter/exit (inset camera, `StartMinigame`, `CancelMinigame`) | `PrepareStation` ช่วงมินิเกม |
| `ShakerContents.OnFulled` → ปิด ingredient, กล้องไป shaker, method panel | `PrepareStation` (subscribe เฉพาะตอนทำงาน) |
| `MethodPanel` Shaking/Mixing → `SetMethod`, `IngredientAdded()` | `PrepareStation` |
| `MinigameFinished` / `BackRequested` | `PrepareStation` |
| 3 Garnish enter/exit | `GarnishStation.RunAsync()` |
| 4 Serve enter/exit | `ServeStation.RunAsync()` |
| Prepare enter / Serve exit: `GarnishManager.ClearGlass()` | `BartendingSession` ต้น session และ `finally` |
| `GarnishManager` Reset/Remake → `ResetCocktail` + remake | `GarnishStation` / `ServeStation` คืนค่า `Next.Remake` |
| Serve → Talking: `ServeDrink()` (ให้คะแนนครั้งเดียว) | `BartendingSession` ท้าย loop |
| `OrderScored` → `CommitOrderResult`; `OrderCleared` → `ResetVariables` | `TalkToCustomerManager.OnEnable` (subscribe เอง) |
| Close enter: Hub = Close · exit: `AdvanceDay()` | `DayRunner.CloseBar()` / `DayRunner.NextDayAsync()` |
| `HubManager.SetPhase(...)` ทุก phase | แต่ละ station / เมธอดของ `DayRunner` เรียกเอง |

### ตาราง B — งานที่ทำงานตลอดเวลา (v1 ตกหล่น) ต้องเลือกที่อยู่ให้ชัด

| งานเดิม (subscribe ใน `FlowWiring.Awake` ตลอด) | ที่อยู่ใหม่ | หมายเหตุ |
|---|---|---|
| `ShakerContents.Changed` → `UpdateCocktailBars` | `PrepareStation` (ช่วง 2.1) | เหตุเกิดเฉพาะตอนใส่ส่วนผสม |
| `ShakerContents.Cleared` → `ResetVisualBars`, ซ่อน method panel, เปิดขวดกลับ, กล้องกลับบาร์, `RefreshLook` | `PrepareStation` | `Clear()` ตอนเข้า Prepare ยิง event นี้ครั้งแรก ต้อง subscribe ก่อน |
| `MethodPanel.ResetChosen` → `ResetCocktail`, ซ่อน panel, กล้องกลับบาร์ | `PrepareStation` ช่วง 2.1 | อยู่ใน 2.1 ต่อ ไม่ใช่การเปลี่ยนขั้น |
| ปุ่มหนังสือ (`_btnBook.onClick`) → `BookUI_V2.SetActive(true)` | `DayRunner` หรือ component เล็กเฉพาะ | ปุ่มอยู่ใน Hub ใช้ได้หลายขั้น ไม่ควรผูกกับ station |
| `IngredientUnlocks.Changed` → `RefreshBottles` | `DayRunner` | บาร์อยู่บนจอทุก phase ปลดล็อกแล้วต้องเห็นขวดทันที |
| `UsePreferences`, `OrderScored`/`OrderCleared` wiring, `UseRecipeFilter` ตอน `Awake` | `DayRunner.Awake()` (หรือ `TalkToCustomerManager`) | ตั้งครั้งเดียว |
| `RefreshLook()`: หมุนกล้องด้วยเมาส์ได้เฉพาะตอน 2.1 และ shaker ยังไม่เต็ม (ADR 0002 Amendment) | `PrepareStation` | เป็นกฎของ ADR ต้องมีใน checklist |
| มินิเกม `Cancelled` → เปิด shaker กลับ (`InteractableToggle.Apply`), `HideCustomerInset` | `PrepareStation.RunMinigameAsync()` `finally` | |
| มินิเกมจบ → `Minigame.ReportResult(outcome)` ให้ `flow_minigame_result()` / `flow_minigame_type()` | `PrepareStation` เก็บ `LastOutcome` / `PendingType` | |
| `InMinigameStep()` guard: มินิเกมที่ cancel ทีหลังต้องไม่ถูกนับเป็นชนะของรอบใหม่ | `PrepareStation` (token ของรอบ) | |
| `RandomCocktailIfNoOrder()` ตอนเข้า 2.1 (test fallback) | Phase 5 เปลี่ยนเป็น error | |

---

## 5. Yarn command contract (ชื่อคงเดิม)

| Command / Function | พฤติกรรมใหม่ |
|---|---|
| `flow_open_bar` | `await DayRunner.OpenBarAsync()` (รอ chapter title จางหายเหมือนเดิม, ไม่รอตอน Skip) **guard:** ใช้ได้เฉพาะ `CurrentPart == Story` ไม่งั้น warning |
| `flow_check_events` | `await DayRunner.ShowDayEventsAsync()` **guard:** ใช้ได้เฉพาะ `Story` ไม่งั้น warning |
| `flow_next_day` | `DayRunner.NextDayAsync()` **guard:** ใช้ได้เฉพาะ `Close` |
| `flow_close_bar` | `DayRunner.CloseBar()` **guard:** ใช้ได้เฉพาะ `Open` และไม่มี session ค้าง |
| `flow_prepare_drinks` | `BartendingSession.Begin()` (ไม่รอ) + `DialogueControls.StopSkip()` เหมือนเดิม **ชื่อนี้ห้ามเปลี่ยน** เพราะ `SkipScan` ค้นหาด้วยชื่อนี้ |
| `wait_for_task` | **ไม่แก้** ยังรอ `Order.IsScored` |
| `flow_ingredient_added [type]` | ยิงเหมือนกด Shake/Stir ใน `MethodPanel` |
| `flow_another_ingredient` | ยิงเหมือนกด Back ในมินิเกม |
| `flow_drink_complete` | สั่งมินิเกมปัจจุบัน `Complete()` |
| `flow_garnish_done` | ยิงเหมือนกด Finish |
| `flow_serve_done` | ยิงเหมือนกด Serve |
| `flow_remake_drink` | ยิงเหมือนกด Remake |
| `flow_minigame X` | เลือกมินิเกม (เหมือนเดิม ไม่เปลี่ยนขั้น) |
| `flow_phase()`, `flow_step()` | ดูตาราง §5.1 |
| `flow_minigame_type()`, `flow_minigame_result()` | อ่านจาก `PrepareStation` |

กฎร่วมของ command ที่ "ยิงเหมือนกดปุ่ม": ถ้าไม่มี station ที่กำลังฟังอยู่ ให้ `Debug.LogWarning("[GameFlowCommands] X ignored: not in <step>")`
ห้ามเงียบ เพราะของเดิมเตือนด้วย "Illegal transition"

Silent replay: ตราบใดที่ยังมี `SceneLoaderBridge.IsSilentReplay` ให้ `GameFlowCommands.CommandTarget()` คืน null เหมือนเดิม
ส่วนนี้ **ต้องอยู่ต่อ** เพราะเลือก save ที่ line ปัจจุบัน + silent replay (§10 ข้อ 1) Phase 4 จะรวมสถานะ replay ที่กระจายอยู่ให้เป็นคลาสเดียว

### 5.1 ตารางแปลง `flow_phase()` / `flow_step()` (ต้องคืนค่าเดิมเป๊ะ)

ค่าเดิม: `flow_phase` ∈ {`BeforeOpen`, `Open`, `Close`} และ `""` ก่อนเริ่มวัน
`flow_step` ∈ {`Event`, `StoryBeforeOpen`, `TalkingWithCustomer`, `AddIngredient`, `Minigame`, `Garnish`, `Serve`} และ `""` ตอน Close/ก่อนเริ่ม

`DayPart` / `BarStep` ใหม่เป็นป้ายภายใน ส่วน Yarn เห็นผ่านฟังก์ชันแปลงใน `GameFlowCommands` เท่านั้น:

| สถานะใหม่ | `flow_phase()` | `flow_step()` |
|---|---|---|
| ก่อน `StartDayAsync` | `""` | `""` |
| `DayPart.BeforeOpen` / `Event` | `BeforeOpen` | `Event` |
| `DayPart.Story` | `BeforeOpen` | `StoryBeforeOpen` |
| `DayPart.Open`, session ไม่ทำงาน | `Open` | `TalkingWithCustomer` |
| session ทำงาน, `BarStep.AddIngredient` / `Minigame` / `Garnish` / `Serve` | `Open` | ชื่อเดียวกับ `BarStep` |
| `DayPart.Close` | `Close` | `""` |

`PLAYTEST_Demo.yarn` พิมพ์ค่าเหล่านี้พร้อมค่าที่คาดไว้ (เช่น "expect StoryBeforeOpen", "expect AddIngredient") ใช้เป็นตัวตรวจ
กฎ: ตอน session ทำงาน `BarStep` ชนะ `DayPart`

---

## 6. โค้ดโครง (skeleton)

ชื่อเมธอดของ manager ใช้ตามที่ `FlowWiring` เรียกอยู่จริง (`CameraManager.ShowBar/ShowShaker/ShowGarnish/CutToBar`, `Post_It_Order.SetAvailable(bool)`,
`HubManager.SetPhase`) ถ้าไม่ตรงให้ดูจาก `FlowWiring.cs` ปัจจุบัน
API ของ `Awaitable` ที่ใช้ด้านล่างต้อง **ยืนยันใน Phase 1 spike** ก่อนเขียนจริง

### 6.0 ชนิดข้อมูลร่วมและ helper

```csharp
public enum Next { Continue, Remake }

public enum BarStep { Idle, AddIngredient, Minigame, Garnish, Serve }       // ป้ายเท่านั้น
public enum DayPart { None, BeforeOpen, Event, Story, Open, Close }         // ป้ายเท่านั้น

public static class FlowAwait
{
    /// <summary>รอ completion source พร้อมยกเลิกด้วย token (Awaitable ไม่มี AttachExternalCancellation)</summary>
    public static async Awaitable<T> Wait<T>(AwaitableCompletionSource<T> source, CancellationToken ct)
    {
        using (ct.Register(() => source.TrySetCanceled()))
            return await source.Awaitable;
    }

    /// <summary>Fire-and-forget ที่ไม่กลืน exception (Awaitable ไม่มี Forget)</summary>
    public static async void Run(Func<Awaitable> work, string label)
    {
        try { await work(); }
        catch (OperationCanceledException) { /* เปลี่ยนฉาก / ยกเลิก */ }
        catch (Exception e) { Debug.LogError($"[{label}] {e}"); }
    }
}
```

ข้อควรจำ: `Awaitable` await ได้ **ครั้งเดียว** ต่อ instance ห้ามเก็บไว้ await ซ้ำ ให้เรียกเมธอด async ใหม่ทุกครั้ง

### 6.1 BartendingSession

```csharp
public class BartendingSession : MonoBehaviour
{
    [SerializeField, Required] PrepareStation prepare;
    [SerializeField, Required] GarnishStation garnish;
    [SerializeField, Required] ServeStation serve;
    [SerializeField, Required] CocktailSystemManager cocktail;
    [SerializeField, Required] GarnishManager garnishManager;
    [SerializeField, Required] DayRunner day;

    public BarStep CurrentStep { get; internal set; }
    public bool IsRunning { get; private set; }

    CancellationTokenSource _cts;

    public void Begin()
    {
        if (IsRunning) { Debug.LogWarning("[Session] already running"); return; }
        if (!cocktail.Order.HasOrder)
            Debug.LogWarning("[Session] no order placed: the .yarn node should call an Order_Cocktail_* function.");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        FlowAwait.Run(() => RunAsync(_cts.Token), "Session");
    }

    public void Cancel() => _cts?.Cancel();

    async Awaitable RunAsync(CancellationToken ct)
    {
        IsRunning = true;
        garnishManager.ClearGlass();
        try
        {
            while (true)
            {
                await prepare.RunAsync(this, ct);
                if (await garnish.RunAsync(this, ct) == Next.Remake) continue;
                if (await serve.RunAsync(this, ct)   == Next.Remake) continue;

                cocktail.ServeDrink();          // ให้คะแนนครั้งเดียว → OrderScored → Yarn
                break;
            }
        }
        finally
        {
            garnishManager.ClearGlass();
            CurrentStep = BarStep.Idle;
            IsRunning = false;
            _cts?.Dispose(); _cts = null;
            day.EnterTalkingMode();
        }
    }
}
```

### 6.2 Station ตัวอย่าง (Garnish)

```csharp
public class GarnishStation : MonoBehaviour
{
    [SerializeField, Required] GarnishManager garnish;
    [SerializeField, Required] CocktailSystemManager cocktail;
    [SerializeField, Required] CameraManager cam;
    [SerializeField, Required] HubManager hub;
    [SerializeField, Required] IngredientButtonGroup ingredients;
    [SerializeField, Required] Post_It_Order postIt;
    [SerializeField, Required] ShakerContents shaker;

    public async Awaitable<Next> RunAsync(BartendingSession s, CancellationToken ct)
    {
        var done = new AwaitableCompletionSource<Next>();
        void OnFinish() => done.TrySetResult(Next.Continue);
        void OnRemake() { cocktail.ResetCocktail(); done.TrySetResult(Next.Remake); }
        void OnPour()   { if (!shaker.IsEmpty) garnish.Pour(shaker.CurrentCocktail); }
        void OnIce(bool on) => shaker.SetIce(on);

        // subscribe ก่อนเปิด UI (หลักข้อ 5)
        garnish.FinishRequested += OnFinish;
        garnish.ResetRequested  += OnRemake;
        garnish.RemakeRequested += OnRemake;
        garnish.PourRequested   += OnPour;
        garnish.IceToggled      += OnIce;
        try
        {
            s.CurrentStep = BarStep.Garnish;
            hub.SetPhase(HubPhase.Garnish);
            cocktail.UpdateCocktailInShaker();
            cam.ShowGarnish();
            ingredients.Disable();
            garnish.Open();
            postIt.SetAvailable(true);

            return await FlowAwait.Wait(done, ct);
        }
        finally
        {
            garnish.FinishRequested -= OnFinish;
            garnish.ResetRequested  -= OnRemake;
            garnish.RemakeRequested -= OnRemake;
            garnish.PourRequested   -= OnPour;
            garnish.IceToggled      -= OnIce;
            garnish.Close();
            postIt.SetAvailable(false);
        }
    }
}
```

### 6.3 PrepareStation (โครง loop ใส่ส่วนผสม ⇄ มินิเกม)

```csharp
public async Awaitable RunAsync(BartendingSession s, CancellationToken ct)
{
    SubscribeShaker();                      // Changed / Cleared / OnFulled / Reset ก่อน
    EnterPrepare();                         // เคลียร์ shaker (ยิง Cleared), permission, ผล order, กล้อง, post-it
    try
    {
        while (true)
        {
            s.CurrentStep = BarStep.AddIngredient;
            var method = await WaitForMethodAsync(ct);      // วนในตัวถ้ากด Reset ใน method panel
            s.CurrentStep = BarStep.Minigame;
            var outcome = await RunMinigameAsync(method, ct);

            if (outcome == MinigameOutcome.Completed) return;   // ไป Garnish
            // Back / Cancelled → กลับไปใส่ส่วนผสมต่อ
        }
    }
    finally { ExitPrepare(); UnsubscribeShaker(); }   // ปิดหนังสือ, post-it ออก, look fixed, ปิด method panel
}
```

`RunMinigameAsync` ห่อ `MinigameSystemManager.StartMinigame` + รอ `MinigameFinished` หรือ `BackRequested` ด้วย `AwaitableCompletionSource`
เรียก `CancelMinigame()` ใน `finally` ถ้ายังไม่ idle และจัดการตาราง B (ShowCustomerInset/Hide, เปิด shaker กลับเมื่อ Cancelled, เก็บ `LastOutcome`)
ผลจาก run ที่ถูก cancel ทีหลังต้องไม่ถูกนับเป็นของรอบใหม่ (ใช้ completion source ต่อรอบ)

### 6.4 DayRunner

```csharp
public class DayRunner : MonoBehaviour
{
    public DayPart CurrentPart { get; private set; }

    async void Start()       // ปลอดภัยเพราะ catch ข้างใน
    {
        try
        {
            LockBar();
            hub.SetPhase(HubPhase.None);
            await transition.WaitUntilFinishedAsync(destroyCancellationToken);
            await StartDayAsync();
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { Debug.LogException(e, this); }
    }

    public async Awaitable StartDayAsync()
    {
        CurrentPart = DayPart.BeforeOpen;
        cam.SetLookFixed(true); cam.ResetLook(); cam.CutToBar();

        await ShowDayEventsAsync();

        CurrentPart = DayPart.Story;
        customers.StartStory(schedule.NodeFor(customers.CurrentDay));
    }

    // เรียกจาก StartDayAsync (ต้นวัน) และจาก <<flow_check_events>> (กลางเรื่อง, ต้องอยู่ใน Story)
    public async Awaitable ShowDayEventsAsync()
    {
        CurrentPart = DayPart.Event;
        LockBar();
        hub.SetPhase(HubPhase.BeforeOpen);
        ingredients.RefreshBottles();
        foreach (var u in unlocks.TakeNewUnlocks()) popups.Enqueue(MakeUnlockPopup(u));
        await popups.ShowAllAsync(destroyCancellationToken);
        CurrentPart = DayPart.Story;            // จบ Event แล้วเป็น Story เสมอ (ทั้งต้นวันและกลางเรื่อง)
    }

    public async Awaitable OpenBarAsync()
    {
        CurrentPart = DayPart.Open;
        EnterTalkingMode();                     // ก่อน chapter title (ตรงกับลำดับเดิม: Talking enter ก่อน Open enter)
        cam.ShowBar();
        if (schedule.TryGet(customers.CurrentDay, out var row))
            await chapterTitle.ShowAsync(row.chapterName, destroyCancellationToken);   // ข้ามการรอเมื่อ DialogueControls.IsSkipping
    }

    public void EnterTalkingMode()
    {
        ingredients.Disable();
        cam.SetLookFixed(true);
        book.CloseBook();
        hub.SetPhase(HubPhase.Talking);
        cam.ShowBar();
    }

    public void CloseBar()
    {
        CurrentPart = DayPart.Close;
        hub.SetPhase(HubPhase.Close);
    }

    public async Awaitable NextDayAsync()
    {
        customers.AdvanceDay();                 // $day + 1 ก่อนเริ่มเรื่องใหม่ (เหมือนเดิม)
        await StartDayAsync();
    }

    // Load: แทน StartDayAsync (Phase 4) ตั้งโหมดตาม DayPart ที่ save แล้วให้ Yarn replay เงียบจาก rootNode ไปถึง line เป้าหมาย
    public async Awaitable ResumeAsync(DayPart part, string rootNode)
    {
        await transition.WaitUntilFinishedAsync(destroyCancellationToken);
        cam.SetLookFixed(true); cam.ResetLook(); cam.CutToBar();
        switch (part)
        {
            case DayPart.Story: CurrentPart = DayPart.Story; LockBar(); hub.SetPhase(HubPhase.BeforeOpen); break;
            case DayPart.Open:  CurrentPart = DayPart.Open;  EnterTalkingMode(); break;          // ไม่โชว์ chapter title ซ้ำ
            case DayPart.Close: CurrentPart = DayPart.Close; CloseBar(); break;
            default: Debug.LogError($"[DayRunner] cannot resume at {part}"); return;
        }
        customers.StartStory(rootNode);         // SilentReplay เปิดอยู่แล้ว ปิดเองเมื่อถึง line เป้าหมาย
    }
}
```

หมายเหตุ: `StartStory` เดิมรองรับกรณีบทเก่ายังไม่จบ (`StartWhenIdle`) จึงเรียกจาก `NextDayAsync` ได้เหมือนเดิม
`ShowDayEventsAsync` ข้างบนเขียนให้ง่ายไว้ก่อน ตอนลงมือให้ตรวจว่าเรียกซ้อนระหว่าง Event แล้วไม่ค้าง

### 6.5 การเรียกจาก Yarn

Yarn command เดิมคืน `IEnumerator` ให้คงรูปแบบนี้และ bridge ด้วย `yield return` (Unity รองรับ `yield return` กับ `Awaitable` ใน coroutine —
ยืนยันใน Phase 1 spike):

```csharp
[YarnCommand("flow_open_bar")]
public static IEnumerator Yarn_OpenBar()
{
    var flow = CommandTarget();
    if (flow == null) yield break;
    yield return flow._day.OpenBarAsync();
}
```

ถ้า spike พบว่า `yield return Awaitable` ใช้ไม่ได้ ทางสำรองคือคืน `YarnTask` (ชนิดเดียวกับที่ presenter ใช้อยู่)

---

## 7. แผนทำทีละขั้น

ทุก Phase ต้องจบด้วย **checklist เส้นทางหลัก (§9) ผ่านทั้งหมด**, รัน `PLAYTEST_Demo.yarn` + `TEST_AllCommands.yarn` แล้วค่าที่พิมพ์ตรงกับที่คาดไว้, และ commit แยก

### Phase 0 — ตาข่ายกันพลาด (≈ ครึ่งวัน)
- [ ] **จัดการงานค้างก่อน:** ตอนนี้อยู่ branch `IngredientUnlock` มีไฟล์เปลี่ยนที่ยังไม่ commit (งาน IngredientUnlock, `manifest.json` / `packages-lock.json`,
      lightmap ที่ bake เมื่อ 2026-11-06, `.meta` ของ Rowlan) commit เป็นเรื่องๆ หรือ stash ให้ working tree สะอาดก่อน
- [ ] แตก branch `refactor/remove-hsm` และ tag `pre-refactor` (หลัง tree สะอาดเท่านั้น)
- [ ] เล่น `DEMO_3Days` จบหนึ่งรอบ อัดวิดีโอเก็บไว้เทียบ
- [ ] รัน `PLAYTEST_Demo.yarn` และ `TEST_AllCommands.yarn` จดค่า `flow_phase()` / `flow_step()` ทุกจุดที่พิมพ์ออกมา เก็บเป็นไฟล์อ้างอิง
- [ ] ทดสอบข้อ 8.1 ของ code map ใน Play mode แล้วจดผล (ตอน Event คิวว่าง Story เริ่มก่อนกล้อง cut ไหม)
- [ ] `grep -rn "flow_phase\|flow_step\|flow_minigame_" Assets/[06]Dialogue` จดทุกที่ที่ใช้ (ตอนนี้: `PLAYTEST_Demo.yarn`;
      command ภายในใช้ใน `VDO_Rec.yarn`, `PLAYTEST_Demo.yarn`, `TEST_AllCommands.yarn`)
- [ ] แก้ของเล็ก: ลบ `[SoundManager] (1)`, ใส่ CinemachineBrain ให้ `CameraManager`, แก้บรรทัด null-unsafe ใน `OnShakerFull` และ `Disable()` ซ้ำใน `OnTalkingEntered`

**เสร็จเมื่อ:** tree สะอาด + มีวิดีโอ + ไฟล์ค่าอ้างอิง + รายการ command ที่ Yarn ใช้จริง

### Phase 1 — Spike `Awaitable` และเตรียมเครื่องมือ (ไม่เปลี่ยนพฤติกรรม ไม่ติดตั้ง package)
- [ ] เขียน script ทดลองชั่วคราว (ไม่ commit) ยืนยัน 5 อย่างใน Unity เวอร์ชันของโปรเจกต์:
  1. `AwaitableCompletionSource<T>` มี `TrySetResult` / `TrySetCanceled` และ property `Awaitable`
  2. `async Awaitable` / `async Awaitable<T>` เป็นชนิดคืนค่าของเมธอดได้
  3. `yield return` กับ `Awaitable` ใน coroutine ทำงาน (สำหรับ Yarn command ที่คืน `IEnumerator`)
  4. exception ใน `FlowAwait.Run` ถูก log และ `OperationCanceledException` ไม่ถูก log
  5. `destroyCancellationToken` ยกเลิกงานที่รออยู่เมื่อ object ถูกทำลาย (ลองเปลี่ยนฉากกลาง await)
- [ ] เพิ่ม `FlowAwait` (§6.0) และเมธอด async ห่อของเดิม (ของเดิมยังอยู่):
  - `PanelTransition.WaitUntilFinishedAsync(ct)`
  - `EventPopupQueue.ShowAllAsync(ct)` (รอ `AllClosed`)
  - `ChapterTitleUI.ShowAsync(name, ct)` (ถ้าใช้ DOTween ให้รอด้วย completion source ที่ `OnComplete`; อย่าใช้ `WaitForSecondsAsync` ถ้าต้องเดินตอน Pause)
- [ ] สร้างโฟลเดอร์ `[02]Script/Game Flow/` และ enum `Next`, `BarStep`, `DayPart`

**เสร็จเมื่อ:** spike ครบ 5 ข้อผ่าน (ข้อที่ไม่ผ่านให้แก้ §6 ตามทางสำรอง), compile ผ่าน, เกมเล่นได้เหมือนเดิม

### Phase 2 — วงจรชงเครื่องดื่ม (ส่วนที่คุ้มที่สุด)
- [ ] สร้าง `ServeStation` (เล็กสุด เริ่มที่นี่เพื่อลองแนวทาง)
- [ ] สร้าง `GarnishStation`
- [ ] สร้าง `PrepareStation` (ใหญ่สุด รวม 2 / 2.1 / 2.2, method panel, minigame bridge และงานตาราง B ที่เป็นของมัน)
- [ ] สร้าง `BartendingSession` และใส่ component ทั้งหมดใน `[GameLoop]` ลาก reference ใน Inspector
- [ ] เปลี่ยน `flow_prepare_drinks` และ command ภายใน (§5) ให้ชี้ไป session / ยิง event ปุ่ม พร้อม warning เมื่อไม่มีใครฟัง
- [ ] `GameLoopFSM` ชั้น 1 ยังอยู่ ค้างที่ Open ระหว่างชง ช่วงนี้เป็น hybrid:
  - `flow_step()` ใช้ตาราง §5.1 อยู่แล้ว (session ทำงาน → `BarStep`) ห้ามอ่านจาก FSM
  - hook ให้คะแนน (`OnOpenBarTransition` Serve → Talking) ย้ายไป `BartendingSession` พร้อมกัน ห้ามเหลือสองที่
  - `OnTalkingEntered` เดิมถูกแทนด้วย `EnterTalkingMode()` ใน `finally`
- [ ] ลบส่วน 2–4 ออกจาก `FlowWiring`
- [ ] ลบ `OpenBarPhase` (state ลูกยกเว้น Talking), `GarnishState`, `ServeState`, โฟลเดอร์ `Level 3 - Prepare Drinks/`

**เสร็จเมื่อ:** checklist §9 ข้อ 3–10 ผ่าน รวมถึง remake จาก Garnish และจาก Serve

### Phase 3 — วงจรวัน
- [ ] สร้าง `DayRunner` ย้ายส่วน `Start()`, BeforeOpen, Event, Story, Open, Close และตาราง B ที่เหลือ จาก `FlowWiring`
- [ ] เปลี่ยน `flow_open_bar`, `flow_check_events`, `flow_close_bar`, `flow_next_day`, `flow_phase()` พร้อม guard (§5)
- [ ] ย้ายการ subscribe `OrderScored` / `OrderCleared` ไปที่ `TalkToCustomerManager` เอง
- [ ] เขียน `GameFlowDebugHotkeys` ใหม่ให้เรียก `DayRunner` / ยิง event ปุ่ม
- [ ] ลบ `GameLoopFSM`, `ClosingBarPhase`, `BeforeOpenPhase` + state ลูก, `Base/` (`IState`, `StateBase`, `StateMachine`), `FlowWiring`
- [ ] ลบโฟลเดอร์ `Hierarchical State Machine/` เหลือแค่ `GameFlowCommands` (ย้ายไป `Game Flow/`)

**เสร็จเมื่อ:** checklist §9 ผ่านทั้งหมด เล่น 3 วันจบโดยไม่มี warning ใหม่ใน Console และค่า `flow_phase()` / `flow_step()` ตรงไฟล์อ้างอิงจาก Phase 0

### Phase 4 — Save ที่ line ปัจจุบัน + Silent Replay (รองรับ `<<detour>>`)
ตัดสินใหม่ 2026-11-06 (แทน checkpoint ต้นวัน): กด Save ได้ทุกที่ที่มีบทพูดอยู่ ตอน load เล่นบทซ้ำแบบเงียบจาก root node
ไปจนถึง line นั้น แล้วเล่นต่อตามปกติ ตรงกับแนวทาง (ก) ใน CLAUDE.md

**นี่คือ Phase ที่เสี่ยงสุดของทั้งแผน** อย่าเริ่มก่อน Phase 3 เสร็จ (ต้องมี `DayRunner`)

#### ของเดิมที่ใช้ต่อได้
- ตัวติดตาม root node แยก `<<jump>>` (root ใหม่) กับ `<<detour>>` (node ย่อย) ใน `SaveLoadManager` (`OnNodeStart/OnNodeComplete`)
- `SessionOptionChoices` (คำตอบที่เลือกตั้งแต่เริ่ม root, ไม่ล้างตอน detour), `TargetLineId`, `IsSilentReplay`
- ตัวข้ามผลข้างเคียงตอน replay ใน 7 ไฟล์ (`GameFlowCommands.CommandTarget`, `CocktailSystemManager.YarnTask`, `Post_It_Order`,
  `TalkToCustomerManager.Characters`, `EventLogPresenter` …)
- snapshot ของ NPC / unlocks / event log

#### ช่องโหว่ที่เจอในโค้ดเดิม (ต้องแก้ ไม่ใช่แค่ย้าย)
1. **ตัวแปรถูก save ตอนกด Save แล้วคืนค่าก่อน replay** → `<<set $x to $x + 1>>` ใน node รันซ้ำบนค่าที่เพิ่มแล้ว และ `<<if>>` แตกกิ่งผิด
   **แก้:** save ตัวแปรทั้งหมด ณ ตอนเริ่ม root (`RootStartVariables`) ด้วย ตอน load คืนค่านี้ก่อน replay แล้วค่อยเขียนทับด้วยตัวแปร ณ ตอน save เมื่อ replay ถึง line เป้าหมาย
2. **หา line เป้าหมายด้วย `TextID` แรกที่เจอ** → detour ที่ใช้ซ้ำ (`PT_MakeDrink` 9 จุด, `VDO_MakeDrink` 7 จุด) และลูปมี line id ซ้ำ จะหยุดผิดที่
   **แก้:** นับจำนวน line ที่แสดงตั้งแต่เริ่ม root (`LineIndex`) หยุดเมื่อนับถึงค่าที่ save ใช้ `TextID` เป็นตัวตรวจ (ไม่ตรง → log error แล้วถอยไปหยุดที่ `TextID` แรก)
3. **ตัวแปรที่ C# เขียนระหว่างบทไม่เกิดตอน replay เพราะคำสั่งถูกข้าม** (ให้คะแนน → `$satisfaction` `$type_of_cocktail` `$task_done` `$rel_*`,
   `Reset_Variable`, `AdvanceDay` → `$day`, `unlock_ingredient`) → `<<if $satisfaction …>>` หลัง `wait_for_task` แตกกิ่งผิด
   **แก้:** `YarnVariableSync` เป็นที่เดียวที่ C# เขียนตัวแปรอยู่แล้ว ให้จด **journal** (ชื่อคำสั่งที่ทำให้เขียน + รายการที่เขียน) ตั้งแต่เริ่ม root
   ตอน replay เมื่อ Yarn มาถึง `wait_for_task` / `Reset_Variable` / `flow_next_day` / `unlock_ingredient` ให้เอารายการถัดไปจาก journal มาเขียนแทนการทำงานจริง
4. **presenter ที่อยู่ในฉากไม่มี logic replay เลย** (`LinePresenterCocktailProject`, `OptionPresenterCocktailProject`; ของเดิมอยู่ใน
   `BubblePresenter` / `OptionPresenterType2` ที่ไม่อยู่ในฉาก) และตัวที่ `StartDialogue(ChapterNodeName)` ตอน load คือ `BubblePresenter` ที่ไม่อยู่แล้ว
   **แก้:** เขียน logic replay ใหม่ใน presenter สองตัวนี้ และให้ `DayRunner.ResumeAsync` เป็นคนเริ่มบท

#### ข้อมูลที่ save (เพิ่มจากเดิม)
| ข้อมูล | ที่มา | ใช้ทำอะไร |
|---|---|---|
| `RootNode` | ตัวติดตาม root (มีอยู่) | node ที่เริ่ม replay |
| `RootStartVariables` | ใหม่ ถ่ายตอน `OnNodeStart` ของ root | ค่าตั้งต้นของ replay (ช่องโหว่ 1) |
| `OptionChoices` | `SessionOptionChoices` (มีอยู่) | เลือกคำตอบอัตโนมัติ |
| `LineIndex` + `LastLineId` | ใหม่ (นับใน line presenter) + มีอยู่ | จุดหยุด (ช่องโหว่ 2) |
| `Journal` | ใหม่ ใน `YarnVariableSync` | เขียนซ้ำตัวแปรที่ C# เขียน (ช่องโหว่ 3) |
| `Variables` | มีอยู่ (ตอน save) | เขียนทับเมื่อ replay ถึงเป้าหมาย |
| `DayPart` | `DayRunner.CurrentPart` | โหมดบาร์หลัง load (Story / Open / Close) |
| `CameraId` | `CameraManager.ActiveCameraId` | `Switch_Camera` / `Cut_Camera` ใน replay ถูกข้าม ต้องคืนช็อตที่ writer สั่งไว้ |
| `Order` (ย่อ: ลูกค้า, โหมด, ชื่อเครื่องดื่มเป้าหมาย, ชนิดที่สั่ง, ผล, ชนิดที่เสิร์ฟ, `IsScored`) | `CocktailSystemManager.Order` | line หลังเสิร์ฟอ้าง `order_name()` / `order_satisfaction()`; ฟังก์ชัน `Order_Cocktail_*` ตอน replay ต้องไม่สุ่มใหม่ |
| NPC / unlocks (รวมรายการแจ้งแล้ว) / event log / setting / playtime | มีอยู่ | คืนค่าหลัง replay จบ |

#### ลำดับ load
1. `SaveLoadManager` โหลดไฟล์ → เปลี่ยนฉาก (เหมือนเดิม) → ใน `sceneLoaded` คืน unlocks / event log / NPC / `RootStartVariables` และตั้ง `SilentReplay` (เป้าหมาย, คิวคำตอบ, journal)
2. `DayRunner.Start()` รอ transition แล้วเรียก `ResumeAsync(part, rootNode)` แทน `StartDayAsync`:
   - `Story`: ไม่เล่น Event ซ้ำ (unlocks แจ้งแล้ว) เริ่มบทที่ `rootNode`
   - `Open`: `EnterTalkingMode()` โดยไม่โชว์ chapter title ซ้ำ แล้วเริ่มบทที่ `rootNode`
   - `Close`: `CloseBar()` แล้วเริ่มบทที่ `rootNode` (ถ้ามีบทเหลือ)
3. Yarn วิ่งจาก `rootNode` เงียบๆ: line presenter นับ `LineIndex` และคืนค่าทันที, option presenter เลือกจากคิว, คำสั่งที่มีผลข้างเคียงถูกข้าม,
   คำสั่งใน journal ถูกเขียนแทน, `<<detour>>` / `<<jump>>` ทำงานตามปกติเพราะ Yarn วิ่งเอง
4. ถึง `LineIndex` เป้าหมาย: ปิด `IsSilentReplay`, เขียนทับด้วย `Variables` ที่ save, คืน `Order` และ `CameraId`, แล้วแสดง line นั้นตามปกติ
5. Watchdog: ถ้าบทจบ / คิวคำตอบหมดก่อนถึงเป้าหมาย / replay เกินจำนวน line ที่กำหนด → ปิด replay, log error, ใช้ตัวแปรที่ save ไว้และปล่อยให้เล่นต่อ ห้ามค้างหน้าจอว่าง

#### ถ้า save ขณะชงเครื่องดื่ม
บทหยุดอยู่ที่ `wait_for_task` (ไม่มี line แสดงระหว่างชง) จึง save ที่ line ล่าสุดที่แสดง (ถ้ามี `#save_checkpoint` ใช้ line นั้น ตรงกับ GDD §23.1)
หลัง load ผู้เล่นกลับไปที่ line ก่อนสั่ง **เครื่องดื่มที่กำลังทำไม่ถูกบันทึก** ปุ่ม Save ต้องบอกผู้เล่น (UI) เมื่อ `BartendingSession.IsRunning`

#### ห้าม save เมื่อ
`DialogueControls.IsSkipping`, `IsSilentReplay`, `CurrentPart` เป็น `None` / `BeforeOpen` / `Event` (popup ปลดล็อกกำลังแสดง: รายการ "แจ้งแล้ว" ถูกเก็บก่อนผู้เล่นปิด popup),
หรือยังไม่มี line แรกของวัน (ยังไม่มีอะไรให้ replay; อนุญาตเมื่อบทแสดง line แรกแล้ว)

#### ข้อกำหนดกับ writer
- ห้ามใช้ `random()` / `dice()` ในเงื่อนไข `<<if>>` ปัจจุบันไม่มีใช้ (grep แล้ว 0 จุด) ถ้าจำเป็นต้องจดผลลง journal ด้วย
- เงื่อนไขที่อ่านสถานะนอก Yarn (`ingredient_unlocked()`, `flow_phase()`, `flow_step()`) ต้องไม่ขึ้นกับสิ่งที่เปลี่ยนกลางบทโดยไม่ผ่านคำสั่งใน journal
- คำสั่งใหม่ที่เขียนตัวแปรหรือเปลี่ยนสถานะ ต้องเลือกให้ชัดว่าตอน replay "ข้าม" หรือ "เขียนซ้ำจาก journal" (เพิ่มลง `Docs/YarnCommands/`)
- `<<save_chapter_node>>` **ไม่จำเป็นอีก** (root node ถูกติดตามอัตโนมัติ; ไม่มี `.yarn` ใดใช้จริง และสั่งตอน `[SaveLoadSystem]` inactive ทำให้บทค้าง)
  เก็บคำสั่งไว้เป็น static ที่ไม่ทำอะไรและ log เตือน เพื่อไม่ให้ `.yarn` เก่าพัง

#### งานย่อย
**4a — โมเดลข้อมูล**
- [ ] เปิด `[SaveLoadSystem]` ในฉาก และเลิกให้ `save_chapter_node` ผูกกับชื่อ GameObject
- [ ] ถ่าย `RootStartVariables` ตอน root node เริ่ม, เพิ่ม `Journal` ใน `YarnVariableSync`, เพิ่ม `LineIndex`, `DayPart`, `CameraId`, `Order` ลง `SaveData`
**4b — เครื่อง replay**
- [ ] รวมสถานะ replay ที่กระจายใน `SceneLoaderBridge` เป็นคลาสเดียว (`SilentReplay`: เป้าหมาย, คิวคำตอบ, journal, ตัวนับ) ลบตัวที่ไม่ใช้
- [ ] `LinePresenterCocktailProject`: นับ `LineIndex`, ตอน replay คืนค่าทันที, หยุดเมื่อถึงเป้าหมาย, ส่ง `OnLineStarted`
- [ ] `OptionPresenterCocktailProject`: เก็บคำตอบลง `SessionOptionChoices`, ตอน replay เลือกจากคิว
- [ ] ปรับ `wait_for_task` / `Reset_Variable` / `flow_next_day` / `unlock_ingredient` ให้อ่านจาก journal ตอน replay (ตอนนี้บางตัวแค่ `return`)
- [ ] watchdog ตามข้อ 5
**4c — ต่อเข้ากับ flow**
- [ ] `DayRunner.ResumeAsync(part, rootNode)` (§6.4) และคืน `CameraId` / `Order` ตอนจบ replay
- [ ] ห้าม save ตามรายการข้างบน แทน `CocktailSystemManager.IsWaitingForTask` (static) ด้วย `BartendingSession.IsRunning`
- [ ] ข้อความใน UI ปุ่ม Save ตอนกำลังชง
**4d — เก็บกวาดและทดสอบ**
- [ ] ลบสถานะ replay เก่าที่ไม่ใช้แล้วใน `SceneLoaderBridge` และ logic ซ้ำใน `BubblePresenter` / `OptionPresenterType2` ถ้าลบโฟลเดอร์ตาม §8.10
- [ ] เขียน `Assets/[06]Dialogue/YarnScript/Demo/TEST_SaveLoad.yarn` ครอบคลุม: `<<set>>` สะสมค่า, `<<jump>>` ต่อเนื่อง, `<<detour>>` ซ้อนและเรียก detour เดียวกันสองครั้งใน root เดียว, ลูป, option หลายชั้น,
      `wait_for_task` แล้วแตกกิ่งตาม `$satisfaction`, `flow_next_day`
- [ ] ตัดสิน `DontDestroyOnLoad` ของ `SaveLoadManager` / `SoundManager` ให้ตรงกับ CLAUDE.md (§8.6)

**เสร็จเมื่อ:** ใน `TEST_SaveLoad.yarn` และ `DEMO_3Days.yarn` ทดลอง save ที่ line ต่างๆ (ก่อน/หลังชง, ใน detour, หลัง option, ต้น/กลาง/ท้ายวัน, หลังเสิร์ฟก่อนปิดร้าน) →
ออกเกม → load → ตัวแปรทุกตัวเท่ากับตอน save, line ที่แสดงตรงกัน, ตัวละครและกล้องอยู่ที่เดิม, เล่นต่อจนจบวันได้, popup ปลดล็อกไม่ขึ้นซ้ำ และ load ซ้ำสองครั้งติดกันได้ผลเหมือนกัน

### Phase 5 — เก็บกวาด
- [ ] ลบ fallback `RandomCocktailIfNoOrder()` ให้ log error แทน (§8.15)
- [ ] รวม debug object ไว้ใต้ `[Debug]` ตัวเดียว ปิดใน build (§8.13)
- [ ] ลบของตายที่ยืนยันแล้วว่าไม่มีใครอ้างอิง (§8.10) — ใช้ "Find References In Scene" ก่อนลบทุกครั้ง
- [ ] เขียน `Docs/adr/0004-remove-hsm.md` (แทน HSM ด้วย DayRunner + BartendingSession + Station; `FlowWiring` ไม่ใช่จุดเดียวที่ flow กับ manager มาเจอกันอีก)
- [ ] อัปเดต `CLAUDE.md` (หัวข้อ "สถาปัตยกรรม (ต้องรักษา)" และ "Game flow"), `Bar410_Systems_Guide.md`, `CONTEXT.md`, `Docs/YarnCommands/README.md`
- [ ] (แยกงาน ทำทีหลังได้) LEGACY VISUAL (§8.11), ย้ายไป Input System (§8.8)

---

## 8. หลัง refactor เพิ่ม feature อย่างไร

| อยากเพิ่ม | ทำแค่ |
|---|---|
| มินิเกมใหม่ | subclass `BaseMiniGame` ใส่ใน `[MiniGameSystem]` (ไม่แตะ flow) |
| ขั้นตอนใหม่ เช่น "ชิมก่อนเสิร์ฟ" | สร้าง `TastingStation.RunAsync()` แล้วเพิ่ม 1 บรรทัดใน loop ของ `BartendingSession` (และเพิ่มค่าใน `BarStep` + ตาราง §5.1) |
| Event กลางวัน / ลูกค้าพิเศษ | Yarn command ใหม่ (async ถ้าต้องรอ) เรียกใน `.yarn` ตรงที่ต้องการ |
| ระบบที่แค่อยากรู้ว่าเกิดอะไรขึ้น (สถิติ, achievement, เสียง) | subscribe event ที่มีอยู่ เช่น `CocktailSystemManager.OrderScored` |
| หน้าจอระหว่างวัน เช่น ร้านค้า | Yarn command async `<<open_shop>>` ที่รอจนปิดหน้าจอ |

ถ้าวันหลังมีระบบประเภท "แค่ฟัง" เยอะจน subscribe ข้าม manager กันวุ่น ค่อยพิจารณา SOAP หรือ event channel ScriptableObject

---

## 9. Checklist เส้นทางหลัก (ใช้ทดสอบทุก Phase)

1. เปิดฉาก → transition จบ → popup ปลดล็อก (ถ้ามี) → เรื่องวันที่ 1 เริ่ม
2. `<<flow_open_bar>>` → Hub เป็นโหมดคุย, bar ล็อก → chapter title แสดงแล้วหาย → คุยกับลูกค้า
3. ลูกค้าสั่ง → `<<flow_prepare_drinks>>` → ขวดกดได้ ขวดที่ล็อกถูกซ่อน
4. ใส่ส่วนผสมจนเต็ม → กล้องไป shaker → method panel; **กด Reset ใน method panel** → shaker ว่าง กล้องกลับบาร์ ยังอยู่ในขั้นใส่ส่วนผสม
5. ระหว่างใส่ส่วนผสม (shaker ยังไม่เต็ม) หมุนกล้องด้วยเมาส์ได้; shaker เต็ม / ในมินิเกม / Garnish หมุนไม่ได้ (ADR 0002 Amendment)
6. เลือก Shake → มินิเกมเขย่า (ภาพลูกค้าข้างจอขึ้น) → กด Back → กลับมาใส่ส่วนผสมได้ และ shaker กดได้อีก
7. เลือก Stir → มินิเกมคน → สำเร็จ → ไป Garnish
8. Garnish: เลือกแก้ว, น้ำแข็ง, ของตกแต่ง, Pour → **Remake** → กลับไป Prepare แก้วว่าง shaker ว่าง
9. ทำใหม่จนถึง Serve → **Remake จาก Serve** → กลับไป Prepare
10. ทำใหม่ → Serve → ลูกค้าตอบสนองตามผล (`$satisfaction`, `$rel_*` เปลี่ยน) และ **ให้คะแนนครั้งเดียว**
11. ลูกค้าคนถัดไปได้แก้วว่างบนโต๊ะ
12. `<<flow_check_events>>` กลางเรื่อง → popup → กลับมาเล่าเรื่องต่อ
13. `<<flow_close_bar>>` → `<<flow_next_day>>` → `$day` เพิ่ม 1 → เรื่องของวันใหม่เริ่ม
14. Skip-to-order และ AutoPlay ทำงานเหมือนเดิม
15. Pause ระหว่างมินิเกมและระหว่าง Garnish แล้วกลับมาเล่นต่อได้
16. เรียก command ผิดจังหวะ (เช่น `flow_garnish_done` ตอนคุย, `flow_open_bar` ซ้ำ) → มี warning ไม่เงียบ ไม่ค้าง
17. `PLAYTEST_Demo.yarn` และ `TEST_AllCommands.yarn` พิมพ์ค่า `flow_phase()` / `flow_step()` ตรงไฟล์อ้างอิงจาก Phase 0
18. Console ไม่มี error / warning ใหม่
19. (Phase 4) Save ที่ line ต่างๆ → ออก → load → ตัวแปรเท่าเดิม, line ตรง, ตัวละคร/กล้องเดิม; ใน detour ที่ใช้ซ้ำ (`PT_MakeDrink`) ก็ถูกที่
20. (Phase 4) Save ขณะชง → load กลับไปที่ line ก่อนสั่ง ชงใหม่ได้ ไม่ค้าง
21. (Phase 4) Save หลังเสิร์ฟ (`$satisfaction` แตกกิ่ง) → load แล้วกิ่งตรงเดิม
22. (Phase 4) Save/Load ระหว่าง Skip, ระหว่าง replay, ใน Event → ถูกห้ามพร้อมข้อความ ไม่พัง

---

## 10. เรื่องที่ต้องตัดสินใจ

| # | คำถาม | สถานะ / ตัวเลือกที่แนะนำ | เหตุผล |
|---|---|---|---|
| 1 | Save แบบ checkpoint หรือรายบรรทัด? | **ตัดสินแล้ว: รายบรรทัด + silent replay** (2026-11-06 ยกเลิกคำตัดสิน checkpoint ก่อนหน้า) | ผู้เล่น save ได้ทุกที่ที่มีบท ข้อเสีย: ต้องมีเครื่อง replay ที่แม่น (Phase 4: root-start snapshot, `LineIndex`, journal) และต้องเขียนบท/คำสั่งให้ replay ได้ (ข้อกำหนดกับ writer) |
| 2 | UniTask หรือ `Awaitable` ของ Unity? | **ตัดสินแล้ว: `Awaitable`** (2026-11-06) | ไม่เพิ่ม dependency และไม่เปลี่ยนชนิด `YarnTask` ของ presenter ที่มีอยู่ ข้อจำกัดดู §11 |
| 3 | Command ภายใน (`flow_garnish_done` ฯลฯ) เก็บไว้ไหม? | **เก็บไว้** | `VDO_Rec.yarn`, `PLAYTEST_Demo.yarn`, `TEST_AllCommands.yarn` เรียกอยู่จริง ภาระดูแลต่ำเพราะแค่ยิง event ปุ่ม |
| 4 | วาง `<<save_chapter_node>>` ที่ไหนใน yarn? | **ยกเลิก (ไม่ต้องมี)** | root node ถูกติดตามอัตโนมัติ ไม่มี `.yarn` ใช้คำสั่งนี้จริง เก็บเป็นคำสั่งว่างไว้กัน `.yarn` เก่าพัง |
| 5 | ปุ่ม Save ใน Pause menu ระหว่างวันทำอะไร? | **ตัดสินแล้ว: save ที่ line ปัจจุบัน** | ระหว่างชงเครื่องดื่ม save ที่ line ล่าสุดก่อนสั่ง (เครื่องดื่มที่ทำอยู่ไม่ถูกบันทึก) และปุ่มต้องบอกผู้เล่น |
| 6 | เมื่อ save ขณะ popup ปลดล็อกแสดงอยู่ (Event) ทำอย่างไร? | **ตัดสินแล้ว: ห้าม save ใน Event** (2026-11-06) | รายการ "แจ้งแล้ว" ถูกเก็บตอน `TakeNewUnlocks()` ก่อนผู้เล่นปิด popup ถ้าอนุญาต save แล้ว load popup จะหายโดยไม่เคยเห็น ปุ่ม Save ต้องแสดงสถานะกดไม่ได้พร้อมเหตุผล (ไม่ใช่เงียบ) |

---

## 11. ความเสี่ยงและวิธีรับมือ

| ความเสี่ยง | รับมือ |
|---|---|
| ลำดับการทำงานเปลี่ยนจากเดิม (ข้อ 8.1) แล้วมีอะไรพึ่งพาลำดับเดิมอยู่ | Phase 0 ทดสอบและจดพฤติกรรมเดิมก่อน แล้วเทียบกับวิดีโอ |
| ลืมปิด UI / unsubscribe เมื่อถูกยกเลิก | ทุกอย่างที่เปิดต้องปิดใน `finally`; ทดสอบเปลี่ยนฉากกลางมินิเกม |
| `async void` / `Awaitable` ที่ไม่ได้ await กลืน exception | เรียกผ่าน `FlowAwait.Run` เท่านั้น (log exception ให้) ห้ามเรียก `async Awaitable` ทิ้งไว้เฉยๆ |
| `Awaitable` await ได้ครั้งเดียว เผลอเก็บไว้ await ซ้ำ | ห้ามเก็บ `Awaitable` ในตัวแปร เรียกเมธอด async ใหม่ทุกครั้ง; รอ event ใช้ `AwaitableCompletionSource` ใหม่ต่อรอบ |
| `Awaitable` ไม่รองรับ cancellation ในตัวสำหรับ completion source | ใช้ `FlowAwait.Wait` (ลงทะเบียน token แล้ว `TrySetCanceled`) |
| `WaitForSecondsAsync` ใช้เวลา scaled หยุดตอน Pause | งานรอ event ไม่ได้รับผลกระทบ; ส่วนที่ต้องเดินตอน Pause (fade UI) ใช้ DOTween unscaled แล้วรอด้วย completion source |
| Yarn command ที่คืน `IEnumerator` + `yield return Awaitable` ใช้ไม่ได้ | Phase 1 spike ข้อ 3; ทางสำรองคืน `YarnTask` |
| Yarn เรียก command ซ้ำ เช่น `flow_prepare_drinks` สองครั้ง | `Begin()` ตรวจ `IsRunning` แล้ว log warning |
| Yarn เรียก command ผิดจังหวะแล้วไม่มีอะไรเกิด (ของเดิมเตือน) | warning ใน `GameFlowCommands` และ guard ใน `DayRunner` (§5) |
| reference ใน Inspector หลุดตอนย้ายจาก `FlowWiring` | ใส่ `[Required]` ของ Odin ในทุกช่องของ station / runner |
| ค่า `flow_phase()` / `flow_step()` เปลี่ยน string แล้ว `.yarn` แตกเงียบ | ตาราง §5.1 + ไฟล์ค่าอ้างอิงจาก Phase 0 + checklist ข้อ 17 |
| งาน "ตลอดเวลา" ของ `FlowWiring` หายตอนย้าย (เช่น `Cleared` ครั้งแรก) | ตาราง B + หลักข้อ 5 (subscribe ก่อน action) |
| Phase 2 เป็น hybrid (FSM ชั้น 1 ยังอยู่) ทำให้สถานะรายงานสองที่ | `flow_step()` อ่านจาก §5.1 เท่านั้น; hook ให้คะแนนมีที่เดียว |
| Silent replay แตกกิ่งผิดเพราะตัวแปรที่ C# เขียนกลางบท (ให้คะแนน, `AdvanceDay`, unlock) ไม่เกิดตอน replay | journal ใน `YarnVariableSync` (Phase 4, ช่องโหว่ 3) + `TEST_SaveLoad.yarn` |
| replay หยุดผิด line เพราะ `TextID` ซ้ำ (detour ที่ใช้ซ้ำ, ลูป) | นับ `LineIndex` เป็นตัวหลัก `TextID` เป็นตัวตรวจ |
| replay ค้างหน้าจอว่าง (คิวคำตอบไม่พอ, บทจบก่อนถึงเป้าหมาย) | watchdog ปิด replay, log error, ใช้ตัวแปรที่ save |
| ตัวแปร save ตอนกด Save ถูกใช้เป็นค่าเริ่ม replay ทำให้ `<<set>>` รันซ้ำ | save `RootStartVariables` แยก (Phase 4, ช่องโหว่ 1) |
| writer ใส่ `random()` / เงื่อนไขที่อ่านสถานะนอก Yarn แล้ว replay ไม่ตรง | ข้อกำหนดกับ writer ใน Phase 4 + เพิ่มลง `Docs/YarnCommands/` |
| ไม่มี automated test | ใช้ `PLAYTEST_Demo` / `TEST_AllCommands` เป็น regression ทุก Phase และทดสอบใน Play ผ่าน Unity MCP เท่าที่ทำได้ |

---

## 12. ไฟล์ที่จะหายไป / เกิดใหม่

**หายไป** (สิ้น Phase 3)
- `Hierarchical State Machine/Base/IState.cs`, `StateBase.cs`, `StateMachine.cs`
- `Level 1 - Game Loop/GameLoopFSM.cs`, `ClosingBarPhase.cs`
- `Level 2 - Before Open/*`, `Level 2 - Open Bar/*`, `Level 3 - Prepare Drinks/*`
- `FlowWiring.cs`

**เกิดใหม่** (`[02]Script/Game Flow/`)
- `GameFlowCommands.cs` (ย้ายมา, เนื้อในเปลี่ยน, มีตารางแปลง §5.1)
- `DayRunner.cs`
- `BartendingSession.cs`
- `Stations/PrepareStation.cs`, `GarnishStation.cs`, `ServeStation.cs`
- `FlowTypes.cs` (`Next`, `BarStep`, `DayPart`) และ `FlowAwait.cs`
- `GameFlowDebugHotkeys.cs` (เขียนใหม่)
- `SilentReplay.cs` (Phase 4: รวมสถานะ replay จาก `SceneLoaderBridge`)
- `Assets/[06]Dialogue/YarnScript/Demo/TEST_SaveLoad.yarn` (Phase 4)
- `Docs/adr/0004-remove-hsm.md` (Phase 5)
