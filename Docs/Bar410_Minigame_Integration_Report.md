# Bar410 — Minigame ↔ Game Loop Integration · รายงานผล

**Date:** 2026-08-21 · **Branch:** `GameLoop/main` · **ซีนที่ทำ:** `New Drag Drop System.unity`
**แผนต้นทาง:** [`Bar410_Minigame_Integration_Plan.md`](Bar410_Minigame_Integration_Plan.md)
**สรุปสั้น:** [`Bar410_Minigame_Integration_Summary.md`](Bar410_Minigame_Integration_Summary.md)

> เอกสารนี้ปิดงาน §3 "ต่อ HSM เข้ากับ minigame" และงานโยก Cocktail System ที่ค้างอยู่ใน
> `Bar410_CocktailSystem_HANDOFF.md` §8.4

---

## 1. สิ่งที่พบก่อนเริ่ม — แผนล้าสมัยไปครึ่งหนึ่ง

`Bar410_Minigame_Integration_Plan.md` ลงวันที่ 2026-08-20 แต่ **FSM Simplification ลงหลังจากนั้น**
และรื้อสิ่งที่แผนอ้างถึงไปแล้ว ตรวจกับโค้ดจริงได้ผลนี้

| แผนบอกให้ทำ | สภาพจริง |
|---|---|
| item 1 — เพิ่ม `PendingType` / `LastResult` / event ให้ `MinigameState` | **ทำแล้ว** ครบทุกตัว (ชื่อจริงคือ `LastOutcome`) |
| item 8 — ทำ `SetSlidePhase` chokepoint + legality table | **ไม่ต้องทำ** `SlidePhase` ถูกลบทั้งชุด เหลือ `MinigamePhase` เส้นตรง + `SetPhase` จุดเดียว |
| item 9 — ลบ `InitBackground` / field ตาย | **ไม่ต้องทำ** ถูกลบไปพร้อมกัน |
| §5.1 `Processing → Standby` warning | **หายแล้ว** `SwitchTo` เรียก `Cancel()` แทนการยัด state |
| §5.2 ทางออกสองทางยิง event ไม่เท่ากัน | **หายแล้ว** ทุกทางลงที่ `EnterPhase(Idle)` จุดเดียว |
| §5.3 `OnEndedGame` ยิงซ้ำ | **หายแล้ว** ยิงครั้งเดียวจาก `NotifyGameEnded` |

จึงเหลือของจริงแค่ item 3 → 2 → 5 → 6/7 · เขียนหมายเหตุเตือนไว้หัว §6 ของแผนแล้ว

---

## 2. งานฝั่งโค้ด

### 2.1 `GameFlowCommands.cs` — เลือกมินิเกมได้ (item 3)

| เมธอด | ใช้จาก |
|---|---|
| `SelectMinigame(Enum_MiniGameType)` | โค้ด / Inspector dropdown |
| `SelectMinigame(string)` | Yarn |
| `SelectShaking()` · `SelectStiring()` | `Button.OnClick` ซึ่งส่ง enum ไม่ได้ |
| `IngredientAdded(string)` | เลือก + เดินหน้า 2.1 → 2.2 ในคลิกเดียว |

Yarn ที่เพิ่ม

```
<<flow_minigame Shaking>>              # เลือกอย่างเดียว ไม่ย้าย state
<<flow_ingredient_added Shaking>>      # เลือก + 2.1 -> 2.2  (param เป็น optional)
{flow_minigame_type()}                 # "Shaking" / "Stiring" / "Building" / "None"
{flow_minigame_result()}               # "Completed" / "Cancelled" / "" (ยังไม่เคยเล่น)
```

`<<flow_ingredient_added>>` แบบไม่มี argument ยังทำงานเหมือนเดิม — `.yarn` ที่มีอยู่ไม่ต้องแก้

ตัวแปลชื่อรับ 3 สะกด: `Stiring` (ตาม enum), `Stirring` (อังกฤษถูก), `Mixing` (ชื่อ component)
ชื่อที่ไม่รู้จัก **เตือนแล้วไม่แตะค่าเดิม** ไม่ตกไปเป็น `None` เงียบ ๆ ซึ่งจะแยกไม่ออกจาก "ไม่มีมินิเกม"

`flow_minigame_result()` คืนคำเดียวกับ `MinigameOutcome` ไม่ใช่ `"win"/"lose"` ตามแผนเดิม —
เพราะกติกาที่ยืนยันวันนี้คือแพ้ไม่ได้ คำว่า lose จึงไม่มีความหมายในระบบนี้

### 2.2 `MinigameFlowBridge.cs` — ไฟล์ใหม่ (item 2)

`Assets/[02]Script/Hierarchical State Machine/Level 3 - Prepare Drinks/MinigameFlowBridge.cs`
`MonoBehaviour` บน `[GameLoop]` เป็นที่เดียวที่สองระบบเจอกัน

| ทิศ | เหตุการณ์ | สิ่งที่ทำ |
|---|---|---|
| flow → game | `MinigameState.OnStartRequested(type)` | `Resolve(type)` แล้ว `StartMinigame` |
| flow → game | `MinigameState.OnStopRequested` | `CancelMinigame()` (ข้ามถ้า `ActivePhase == Idle`) |
| game → flow | `MinigameSystemManager.MinigameFinished(type, outcome)` | `ReportResult(outcome)` · ปลดล็อก shaker ถ้า Cancelled · auto-advance ถ้า Completed |

`Resolve` = สิ่งที่เลือกไว้ → `ShakerContents.RequiredMinigame` (จาก `PreparationMethod` ของสูตร) → `_defaultType`
ลงเอยที่ `None` เมื่อไหร่ เตือนแล้วไม่เริ่มอะไร

`MinigameState` ยังเป็น plain C# ไม่รู้จัก `UnityEngine` และไม่รู้จัก `MinigameSystemManager`
ชั้น layering ตาม `Bar410_StateMachine_Implementation.md` ไม่ถูกแตะ

### 2.3 `VisualizeCocktail.cs` — โยกมาอ่านของใหม่

เดิม `FindFirstObjectByType<CocktailShaker>()` แล้วอ่าน `.CurrentCocktail`

ซีนนี้ลบ `CocktailShaker` ทิ้งไปตั้งแต่รอบ refactor **ตัวแปรจึงเป็น null และโยน
NullReferenceException ทันทีที่ปุ่ม `BTN_Reset` เรียก `UpdateCocktailBars()`** — บั๊กที่ยังไม่มีใครเจอ
เพราะยังไม่มีใครเล่นซีนนี้จนถึงจุดนั้น

ตอนนี้อ่าน `ShakerContents` แทน · มีช่อง `_shaker` ให้ผูกใน Inspector (ผูกไว้แล้ว) และหาเองถ้าเว้นว่าง ·
เพิ่ม null guard ให้ทั้ง `_shaker` และ `Image` ทั้งสองใบ — พลาดผูกช่องไหนก็แค่บาร์ไม่ขยับ ไม่ล้ม

---

### 2.4 `ShakerContents` — event ให้ designer ผูกเองได้

`event Action Changed / Cleared` + `event Action<RecipeMatch> IdentityResolved` เป็น C# event
ซึ่ง Unity ไม่ serialize → ไม่ขึ้น Inspector → designer ผูกเสียงหรือ particle เองไม่ได้ ต้องรอโปรแกรมเมอร์

เปลี่ยนเป็น **UnityEvent field** ชื่อเดิมทั้งสามตัว

```csharp
[Serializable] public class RecipeMatchEvent : UnityEvent<RecipeMatch> { }

public UnityEvent Changed;                   // วัตถุดิบ / method / ice / แก้ว เปลี่ยน
public UnityEvent Cleared;                   // เทแก้วทิ้ง
public RecipeMatchEvent IdentityResolved;    // UpdateIdentity ได้ผลลัพธ์
```

`UnityEvent<T>` เปล่า ๆ Unity serialize ไม่ได้ ต้องมี subclass ที่ติด `[Serializable]` ก่อน

ฝั่งโค้ดที่ subscribe มีที่เดียวคือ `ShakerVisualPresenter` เปลี่ยนเป็น `AddListener` / `RemoveListener`
ส่วน `Changed` ไม่มีใครใน subscribe เลย — เป็น hook ของ designer ล้วน ๆ

### 2.5 `IngredientButtonUI` — โยกมาคุยกับ `ShakerContents`

เดิมถือ `CocktailShakerData` (shim) แล้วยิง UnityEvent ของ shim ทุกครั้งที่เท
ซีนนี้ไม่มี shim แล้ว → `_shaker` เป็น null → **NullReferenceException ทุกครั้งที่คลิกวัตถุดิบ**
(`Interactable_2_5DObject.OnClicked` ผูกไป `Invoke()` อยู่ทั้ง 20 ปุ่ม) บั๊กแบบเดียวกับ `VisualizeCocktail`

ตอนนี้:

- คุยกับ `ShakerContents` ตรง ๆ ผ่าน `IIngredientReceiver.TryToAdd*`
- **ซีนที่ยังมี shim ไม่เปลี่ยนพฤติกรรม** — ถ้าเจอ `CocktailShakerData` ในซีน จะยิง UnityEvent ของ shim
  เหมือนเดิม เพราะ event พวกนั้นถูก author ไว้รายซีน (แอนิเมชันเท เสียง และตัวการเทเอง)
  ถ้าเรียกทั้งสองทางเครื่องดื่มจะถูกเทซ้ำสองหน
- หา shaker แบบ lazy ตอนใช้ครั้งแรก ไม่ใช่ใน `Awake` — ในซีนที่ยังมี shim `ShakerContents`
  ถูกสร้างตอน `Awake` ของ shim ซึ่งลำดับไม่แน่นอน
- เพิ่ม UnityEvent **`OnPoured` / `OnRejected`** — แยกกรณีเทติดกับกรณีแก้วเต็มแล้ว (เพดาน 10 หน่วย GDD §15)
  ซึ่ง `TryToAdd*` คืนค่า void จึงเทียบ `TotalParts` ก่อน/หลังเอา
- `ResetShaker()` ไม่มี shim = `ShakerContents.Clear()` ซึ่งยิง `Cleared` ต่อให้เอง

`Assets/Editor/IngredientButtonUIEditor.cs` วาดเฉพาะ field ที่มันรู้จัก — ถ้าไม่แก้ ช่องใหม่ทั้งสาม
จะมองไม่เห็นใน Inspector · เพิ่มการวาด `Shaker Contents` + สอง event (โชว์เฉพาะ action ที่เทจริง)
และแก้ข้อความอธิบายให้ตรงกับพฤติกรรมใหม่

## 3. งานฝั่งซีน

### 3.1 ผูก bridge (item 5)

`MinigameFlowBridge` บน `[GameLoop]`

```
_gameLoop        = [GameLoop] (GameLoopFSM)
_commands        = [GameLoop] (GameFlowCommands)
_minigames       = SystemGame/MiniGameSystem (MinigameSystemManager)
_shakerContents  = SystemGame/CocktailSystem/CocktailShaker (ShakerContents)
_defaultType     = Shaking
_autoAdvanceOnWin = true
```

### 3.2 ปุ่มวิธีชง (item 6)

FSM เป็นคนสั่งเริ่มมินิเกมแล้ว ปุ่มแค่บอกว่าจะใช้วิธีไหน

| ปุ่ม | ก่อน | หลัง |
|---|---|---|
| `BTN_Shaking` | `SetShaking` → `MinigameSystemManager.StartShakingMinigame` | `SetShaking` → `SelectShaking` → `IngredientAdded` |
| `BTN_Mixing` | `SetMixing` → `MinigameSystemManager.StartMixingMinigame` | `SetMixing` → `SelectStiring` → `IngredientAdded` |

### 3.3 ปุ่มปิดมินิเกม (item 7) — ไม่ต้องแก้

`BTN_Next` และ `BTN_Reset` ใน `Canvas_MiniGame` เรียก `CancelMinigame` อยู่แล้วตั้งแต่รอบ FSM Simplification
ตรงกับกติกา "ยกเลิกได้ทุกเมื่อ" พอดี

### 3.4 โยก BaseInteractable — `enabled` → `Interactable`

พบ 5 จุดที่ยังปิดการโต้ตอบด้วยการปิด **component** (`DragableObject.enabled = false`)
ซึ่งเป็นวิธีเดิมก่อน refactor · `PointerInteractableBase.Interactable` คือช่องทางที่ระบบใหม่ใช้ทั้งหมด
(รวมถึง `InteractableToggle.Apply`) แก้ครบทั้ง 5 จุด ค่า argument เดิมทุกตัว

| ที่ | เดิม | ตอนนี้ |
|---|---|---|
| `Panel - Method/BTN_Reset` [10] | `enabled(true)` | `Interactable(true)` |
| `Panel - AddIce/BTN_Reset (1)` [8] | `enabled(true)` | `Interactable(true)` |
| `Panel - Serve/BTN_Reset (2)` [8] | `enabled(true)` | `Interactable(true)` |
| `Panel - Serve/BTN_Serving` [11] | `enabled(true)` | `Interactable(true)` |
| `MiniGameSystem.OnStartedMinigame` [5] | `enabled(false)` | `Interactable(false)` |

ต่างกันตรงไหน: `enabled = false` ตัด `Update` และ pointer callback ทั้งก้อน ถ้าตอนนั้นกำลังลากอยู่
การลากจะค้าง · `Interactable = false` วิ่งผ่าน `OnInteractableChanged` ซึ่ง `DragableObject` override ไว้
ให้ `CancelDrag()` ก่อน — กดเริ่มมินิเกมกลางลากแล้วไม่มีสถานะค้าง

### 3.5 ปลดล็อก shaker เมื่อยกเลิก

ซีนล็อก shaker ตอนมินิเกมเปิด (`OnStartedMinigame[5]`) แต่คนปลดล็อกมีแค่ปุ่ม Reset/Serving
ซึ่งการกดยกเลิกไม่ได้ผ่าน → **ยกเลิกแล้ว shaker จะจับไม่ได้อีกเลย ตัน**

แก้ที่ bridge: `MinigameFinished` ที่เป็น `Cancelled` เรียก `InteractableToggle.Apply(shaker, true)`
ใช้ chokepoint ของ BaseInteractable ตัวเดียวกับที่ระบบใหม่ใช้ ไม่เพิ่ม UnityEvent ในซีน
ส่วน `Completed` ปล่อยล็อกไว้ตามเดิม เพราะเครื่องดื่มเสร็จแล้วและซีนสลับไปโชว์แก้วแทน shaker

---

## 4. คำถาม design ที่ปิดไปวันนี้

| # | คำถาม (Plan §6) | คำตอบ |
|---|---|---|
| Q1 | ใครจบขั้น 2.2 | **auto-advance** ชนะ = เครื่องดื่มเสร็จ ไปข้อ 3 Garnish · `_autoAdvanceOnWin = true` |
| Q2 | แพ้แล้วบล็อกไหม | **แพ้ไม่ได้** จบได้แค่ `Completed` หรือ `Cancelled` |

ผลข้างเคียงของ Q1: ลูป 2.1 ↔ 2.2 ไม่ถูกใช้ในซีนนี้ ซึ่งตรงกับ UI จริง — ผู้เล่นเทวัตถุดิบให้ครบก่อน
แล้วกดวิธีชงครั้งเดียวปิดท้าย · เขียนลง GDD §10.1 แล้ว

Q3 (`Building`) และ Q4 (เก็บผลลง `DrinkOrderContext`) ยังเปิดอยู่ · Q3 เพิ่มเข้า GDD §24 แล้ว

---

## 5. ผลตรวจสอบ

**คอมไพล์** — ไม่มี error ใหม่ (เหลือ warning เก่า 3 ตัวใน `BubblePresenter` / `HoverTooltip` / `CustomLineAdvancer`)

**Yarn registration** — `Assembly-CSharp-generated.ysls.json` ที่ Yarn generate เองมีครบ

```
flow_minigame  flow_minigame_type  flow_minigame_result
```

**ชั้น flow ล้วน** (`execute_code` ไม่ต้องมีซีน)

```
bridge type: Bar410.GameFlow.MinigameFlowBridge : MonoBehaviour
default pending  : None            start fired with : Stiring
outcome on enter : null            after report     : Completed
stop fired       : 1 time(s)       re-entry outcome : null
parse 'Stirring' -> Stiring   'Mixing' -> Stiring   'shaking' -> Shaking
parse 'Nonsense' -> false (เตือน ไม่เปลี่ยนค่าเดิม)
```

**ตรวจซีนหลังแก้**

```
VisualizeCocktail._shaker            = ShakerContents @ SystemGame/CocktailSystem/CocktailShaker
legacy CocktailShaker in scene       = 0
legacy CocktailShakerData in scene   = 0
DragableObject.set_enabled ที่เหลือ   = 0
set_Interactable bindings            = 5
binding ที่ target หาย                = 1  (ของเดิม ไม่เกี่ยวกับงานรอบนี้)
```

**ยังไม่ได้ทำ:** play-test จริง — ทำไม่ได้จนกว่าจะมีคนเรียก `flow_open_bar` (ดู §6)

---

## 6. ⚠️ ค้างอยู่ ต้องแก้ก่อนเล่นทดสอบได้

**ไม่มีใครขับ flow เข้าสู่ Open Bar** — สแกน UnityEvent ทุกตัวในซีนและ `.yarn` ทุกไฟล์แล้ว
ไม่มีที่ไหนเรียก `flow_open_bar` หรือ `flow_prepare_drinks` เลย

`_autoStart` พาไปได้แค่ Level 1 Prepare · ไม่เข้า Open → ไม่เข้า 2.1 → กดปุ่มวิธีชงแล้ว
`TryTransition` เตือนแล้วไม่มีอะไรเกิดขึ้น

นี่คือต้นทุนของ option A ที่แผน §2 เขียนเตือนไว้ตั้งแต่แรก (FSM เป็นเจ้าของ ปุ่มเป็นแค่ผู้ร้องขอ)
ก่อนหน้านี้ปุ่มเปิดมินิเกมได้ตรง ๆ โดยไม่สนใจ flow

**ทางเลือก:** ใส่ `<<flow_open_bar>>` / `<<flow_prepare_drinks>>` ในโหนด `.yarn` ตอนลูกค้าสั่งเสร็จ
(ตรงเจตนา GDD §12 ที่สุด) หรือผูกกับปุ่มในซีนไว้ก่อนเพื่อทดสอบ

**อีกอย่างที่ยังค้าง (ของเดิม):** `Panel - Serve/BTN_Serving` OnClick[10] `GameObject.SetActive`
target หายไปตั้งแต่รอบ refactor — เดาแทนไม่ได้ว่าเคยชี้ไปที่ไหน

---

## 7. ไฟล์ที่แตะ

| ไฟล์ | อะไร |
|---|---|
| `Hierarchical State Machine/GameFlowCommands.cs` | +6 เมธอด · +2 Yarn command · +2 Yarn function · ตัวแปลชื่อ |
| `Hierarchical State Machine/Level 3 - Prepare Drinks/MinigameFlowBridge.cs` | **ไฟล์ใหม่** |
| `Cocktail System/VisualizeCocktail.cs` | อ่าน `ShakerContents` แทน `CocktailShaker` + null guard |
| `Cocktail System/IngredientButtonUI.cs` | คุยกับ `ShakerContents` · ถอยไป shim อัตโนมัติ · +`OnPoured` / `OnRejected` |
| `Cocktail System/Cocktail/Shaker/ShakerContents.cs` | 3 event เปลี่ยนเป็น UnityEvent |
| `Cocktail System/Cocktail/Shaker/ShakerVisualPresenter.cs` | subscribe ด้วย `AddListener` / `RemoveListener` |
| `Assets/Editor/IngredientButtonUIEditor.cs` | วาดช่อง shaker + event ใหม่ · แก้ข้อความอธิบาย |
| `[05]Scenes/Deverlopment/New Drag Drop System.unity` | bridge 1 ตัว · ปุ่ม 2 ปุ่ม · binding 5 จุด · `VisualizeCocktail._shaker` |
| `Docs/GDD_Bar410_Master.md` | +§10.1 กติกา minigame · +1 แถวใน §24 |
| `Docs/Bar410_Minigame_Integration_Plan.md` | หมายเหตุว่าเอกสารเก่ากว่าโค้ด + คำตอบ Q1/Q2 |
| `ProjectSettings/.../Assembly-CSharp-generated.ysls.json` | Yarn generate เอง |
