# Bar410 — ถ้าจะเขียนระบบใหม่ทั้งหมด (ไม่เอา HSM) ทำได้กี่แบบ

เขียนเมื่อ 2026-11-06 อัปเดต 2026-11-07: **ล็อกการตัดสินใจแล้ว (§9)** ตามคำตอบใน `Bar410_Rewrite_Questions.md` และเพิ่ม §11–§15 (หลายภาษา, เสียง Expression, Save, Controller, scene แยกของ Minigame/Garnish) ยังไม่ได้ลงมือ
คู่กับแผน refactor แบบค่อยๆ แก้ใน `Bar410_Refactor_Plan_Remove_HSM.md` (เก็บไว้ก่อน ไม่ได้ยกเลิก)

> **2026-11-07:** ส่วนที่ออกแบบ API จริงและลำดับงานอยู่ใน `Bar410_Rewrite_ExecutionPlan.md` — ที่นั่นแก้บางข้อของเอกสารนี้ (ชั้น asmdef §3.1 กลับด้านเพราะโค้ดเดิมไม่มี asmdef, Garnish 2 call, กล้องสลับมุมไม่ใช่หมุนอิสระ ฯลฯ) ถ้าขัดกันให้ถือไฟล์นั้น

ฐานข้อมูลของเอกสารนี้คือ `Bar410_CodeMap_MainScene.md` (อ่านโค้ดเต็มเฉพาะส่วน flow / customers / cocktail core ที่เหลืออ่านระดับ header)
ตัวเลขขนาดงานเป็นขนาดสัมพัทธ์ (S/M/L/XL) ไม่ใช่จำนวนวัน เพราะผมไม่รู้ความเร็วทีมและเวลาที่คุณมี
API ของ Unity ที่อ้างถึง (เช่น `Awaitable`) ต้องยืนยันใน spike ก่อนใช้จริง

---

## 0. สรุปสั้น (อ่านตรงนี้ก่อน)

1. **คำว่า "เขียนใหม่ทั้งหมด" มีสามระดับ** (§2) ระดับ A แก้เฉพาะ flow (คือแผน refactor เดิม), ระดับ B สร้างชั้นกำกับเกมใหม่ทั้งชั้น แต่เก็บ domain และ manager,
   ระดับ C เขียนโปรเจกต์ใหม่หมด ผมเห็นว่า **ปัญหาที่เจอจริงอยู่ที่ชั้น flow, save/replay และโครงสร้างโปรเจกต์** ไม่ได้อยู่ที่ domain หรือ manager (§1) ระดับ C จึงแพงกว่าที่ได้
2. **ตัวกำกับ flow ที่ไม่ใช่ HSM มี 3 แบบ** (§4): ① Async Stations (โค้ดอ่านจากบนลงล่าง) ② Data-driven Step list (ลำดับขั้นเป็นข้อมูลใน Inspector) ③ Yarn เป็นผู้กำกับทั้งหมด
   ผมแนะนำ **① เป็นแกน + ใช้ลิสต์ใน Inspector จาก ② เฉพาะลำดับขั้นการชง** (§4.4) ส่วน ③ เหมาะเป็นส่วนเสริมไม่ใช่แกน
3. **สิ่งที่ทำให้ "ใช้งานง่ายและเพิ่ม feature ง่าย" ไม่ใช่ตัวกำกับ flow แต่เป็นรากฐานร่วม** (§3): ชั้น assembly ที่ห้ามพึ่งข้ามชั้น, composition root เดียว,
   Yarn verb ที่ประกาศนโยบาย replay ไว้ที่ตัวคำสั่ง, save/replay ที่ออกแบบมาตั้งแต่ต้น, Input System รองรับ controller ตั้งแต่วันแรก, localization ตั้งแต่ต้น, และ test ที่รันได้
4. **ถ้าเขียนใหม่ ให้ทำ side-by-side** (§6) สร้างระบบใหม่ในโฟลเดอร์/ฉากใหม่ ใช้ checklist เส้นทางหลักเดียวกับของเดิมเป็นเกณฑ์ผ่าน แล้วค่อยสลับ ห้ามแก้ของเดิมไปพร้อมกัน
5. **ล็อกแล้ว:** ระดับ B, flow ① + ลิสต์ `steps`, side-by-side, verb ใหม่ + shim, uGUI + TextMeshPro, ภาษา EN + TH (สลับระหว่างเล่นได้), เสียง Expression ชุดเดียวทุกภาษา, controller ทุกหน้าจอ (Xbox/PlayStation) พร้อม virtual cursor, save หลาย slot + autosave (§9) เรื่องที่ยังเปิดอยู่รวมไว้ท้าย §9

---

## 1. ทำไมถึงคิดจะเขียนใหม่ และอะไรที่ไม่ใช่ปัญหา

### ปัญหาที่เห็นจากโค้ดจริง (จาก code map §8)
| ปัญหา | อยู่ที่ชั้นไหน |
|---|---|
| เพิ่มขั้นตอนใหม่ต้องแก้ enum + ตาราง transition + state class + hook ใน `FlowWiring` พร้อมกัน | Flow |
| `FlowWiring` 31 KB ทำ 5 หน้าที่ มีโค้ดซ้ำ/null-unsafe | Flow |
| `Entered` ของ container ยิงหลังลูก ลำดับเดายาก | Flow |
| Save/Load ไม่รู้จัก flow, replay เดิมมีช่องโหว่ 4 จุด (ดู Refactor Plan Phase 4) | Save |
| static singleton 8 ตัว + `FindFirstObjectByType` หลายจุด + `IsWaitingForTask` static | โครงสร้าง |
| `SaveLoadManager` / `SoundManager` ใช้ `DontDestroyOnLoad` ขัดกับ CLAUDE.md, มี `SoundManager` สองตัวในฉาก | โครงสร้าง |
| ใช้ `Input.*` เก่าใน 8 ไฟล์ ทั้งที่มี Input System, ต้องรองรับ controller | Input |
| ไม่มี automated test, debug object ปนในฉากจริง | เครื่องมือ |
| LEGACY VISUAL, ของตายใน `[06]Dialogue` / `[04]Prefab`, ชื่อพิมพ์ผิดเป็น identifier | ความสะอาด |

### สิ่งที่ **ไม่ใช่** ปัญหา (เก็บได้ ไม่ต้องเขียนใหม่)
- **Domain ของ Cocktail** (`Cocktail/Domain/*`, `Session/*`): pure C# แยกชัด มี GDD อ้างอิงรายกฎ มี Validate Cocktail Data
- **Manager แต่ละระบบ** (Cocktail / Minigame / Garnish / Camera / Customers / Hub): ไม่รู้จัก flow และไม่รู้จักกันเอง ใช้ event ออกอยู่แล้ว
- **ข้อมูล**: `SO_Customer`, `SO_CocktailList`, `SO_DaySchedule`, `SO_IngredientUnlockTable`, ฯลฯ, prefab ขวด/แก้ว, ศิลป์ทั้งหมด
- **สคริปต์ Yarn** (ถ้ารักษา command contract ไว้ หรือทำ compat shim ตาม §3.4)

> ข้อสังเกตตรงไปตรงมา: ถ้าเขียน domain และ manager ใหม่ทั้งหมด จะต้องพิสูจน์กฎ GDD §15–18 ซ้ำทีละข้อ (ผลคะแนน, ราคา, สูตร) โดยได้ประโยชน์น้อย
> เขียนใหม่คุ้มก็ต่อเมื่อมีเป้าหมายใหม่ที่โครงสร้างปัจจุบันรับไม่ได้ (เช่น แพลตฟอร์มใหม่, multiplayer, ระบบที่ไม่ใช่ visual novel) ถ้ามี ขอให้บอกใน §9

---

## 2. "เขียนใหม่ทั้งหมด" มีสามระดับ

| ระดับ | เขียนใหม่ | เก็บไว้ | ขนาด | ความเสี่ยง | ผลที่ได้ |
|---|---|---|---|---|---|
| **A — Flow อย่างเดียว** | `GameLoopFSM`, `FlowWiring`, state ทั้งหมด → `DayRunner` + `BartendingSession` + Station (แผน refactor เดิม) | ทุกอย่างที่เหลือ | M–L | ต่ำ–กลาง | เพิ่มขั้นตอนง่าย, ลำดับชัด, ไม่มี HSM |
| **B — ชั้นกำกับใหม่ทั้งชั้น** | A + รากฐานใหม่ (§3): assembly, composition root แทน static, Yarn verb layer + replay policy, save/replay ใหม่, Input System, localization, test harness | Domain, manager (หลัง wrap ด้วย interface), ข้อมูล, ศิลป์, `.yarn` | L–XL | กลาง | ทุกข้อใน §1 ถูกตอบ โดยไม่แตะกฎเกมที่พิสูจน์แล้ว |
| **C — Greenfield** | ทุกอย่างรวม domain, manager, UI, presenter | เฉพาะศิลป์/เสียง/บท | XL+ | สูง | โครงสร้างสะอาดทั้งโปรเจกต์ แต่ต้องพิสูจน์กฎเกมใหม่ และพอร์ตทุก feature (CLAUDE.md ระบุมี ~10 ระบบใหญ่ที่เพิ่งทำเสร็จ) |

**คำแนะนำ:** ระดับ **B** ถ้าต้องการ "ใช้งานง่ายและเพิ่ม feature ง่าย" จริงจัง; ระดับ **A** ถ้าต้องการแก้ปัญหา flow อย่างเร็ว; ระดับ **C** ไม่แนะนำเว้นแต่มีเหตุผลใน §1 ท้ายข้อสังเกต
ส่วนที่เหลือของเอกสารเขียนสำหรับระดับ B (ซึ่งครอบ A อยู่แล้ว) ส่วนที่เป็นของระดับ C บอกไว้ตรงที่เกี่ยวข้อง

---

## 3. รากฐานร่วม (ใช้กับทุกแบบของตัวกำกับ flow)

เรื่องที่ทำให้เกมเพิ่ม feature ง่ายจริงอยู่ที่นี่ ไม่ใช่ที่เลือก HSM หรือ async

### 3.1 ชั้นและ assembly (บังคับด้วยคอมไพเลอร์ ไม่ใช่ด้วยความจำ)

```
Bar410.Domain        pure C# ไม่มี UnityEngine   กฎเครื่องดื่ม, คะแนน, ราคา, คำสั่งซื้อ        ← มีอยู่แล้ว ย้ายเข้า asmdef
Bar410.Data          ScriptableObject             recipe, customer, schedule, unlock table
Bar410.Features.*    หนึ่ง asmdef ต่อหนึ่งระบบ     Cocktail, Minigame, Garnish, Customers, Camera, Hub, Unlock, EventPopup
Bar410.Flow          DayRunner, BartendingSession, Station, GameFlowCommands   รู้จัก Features ผ่าน interface
Bar410.Dialogue      presenter, DialogueDirector (replay), Yarn verb layer     รู้จัก Flow ผ่าน interface เท่านั้น
Bar410.Save          SaveGame, SaveSlot, snapshot contract                      รู้จัก "ผู้ให้ snapshot" ผ่าน interface
Bar410.Host          GameRoot (composition root)  รู้จักทุกชั้น เป็นที่เดียวที่ new / ผูกกัน
```

กฎ: ลูกศรชี้ลงเท่านั้น `Domain` ไม่รู้จักใคร `Features.*` ไม่รู้จักกันเอง `Flow` ไม่รู้จัก presenter `Dialogue` ไม่รู้จัก feature เฉพาะ
ได้ผล: เพิ่ม feature ใหม่ = เพิ่ม asmdef ใหม่ ไม่มีทางไปแก้ของคนอื่นโดยไม่ตั้งใจ และ Domain test รันได้โดยไม่เปิดฉาก

### 3.2 Composition root เดียว แทน static singleton

- มี `GameRoot` เดียวในฉาก ถือ reference ที่ลากใน Inspector (Odin `[Required]`) และ **ส่ง dependency เข้า constructor/`Init()`** ของแต่ละ feature
- ไม่ใช้ DI framework (VContainer ฯลฯ): โปรเจกต์ขนาดนี้ผูกด้วยมือใน `GameRoot.Awake()` สั้นกว่าและอ่านง่ายกว่า (ตัดสินใหม่ได้ใน §9)
- Yarn command แบบ static ยังต้องการทางเข้า: ให้ `YarnVerbs` (static, ชั้นบางๆ) ถือ reference `GameRoot.Current` ตัวเดียวทั้งโปรเจกต์ แทน 8 ตัวที่กระจายอยู่
- ผลพลอยได้: ปัญหา `[SoundManager] (1)`, `DontDestroyOnLoad` ขัดกฎ, `FindFirstObjectByType` หมดไปเพราะทุกอย่างถูกส่งมาให้

### 3.3 สามกลุ่มผู้ใช้ และพื้นที่ที่แต่ละกลุ่มแตะ

| ผู้ใช้ | แตะอะไร | ต้องไม่ต้องรู้ |
|---|---|---|
| **Writer** | ไฟล์ `.yarn` และ verb ของ Yarn (§3.4) | C#, scene, state ภายใน |
| **Designer** | ScriptableObject + Inspector (Odin): สูตร, ลูกค้า, ตารางวัน, unlock, ลำดับขั้นการชง, ค่าของมินิเกม | โค้ด, Yarn |
| **Programmer** | interface ของ feature, Station, test | รายละเอียดการ replay ของ Yarn |

เกณฑ์ที่ใช้ตัดสินว่า "เพิ่ม feature ง่าย": feature ใหม่หนึ่งชิ้นควรแตะ **พื้นที่ของผู้ใช้กลุ่มเดียวให้มากที่สุด** ไม่ต้องไล่แก้หลายกลุ่มพร้อมกัน

### 3.4 Yarn verb layer ที่ประกาศนโยบาย replay ไว้ที่ตัวคำสั่ง

ปัญหาของ silent replay (Refactor Plan Phase 4) คือ "คำสั่งไหน ข้าม / รัน / จดลง journal" กระจายอยู่ใน 7 ไฟล์ ถ้าเขียนใหม่ ให้ประกาศที่คำสั่งเอง:

```csharp
public enum ReplayPolicy
{
    Run,      // ทำงานจริงตอน replay (เช่น เขียนตัวแปรที่อยู่ใน Yarn เอง)
    Skip,     // ข้าม เพราะเป็นภาพ/เสียง/กล้อง ถูกคืนจาก snapshot ตอนจบ replay
    Journal   // ผลที่ C# เขียนเข้าตัวแปร Yarn: จดตอนเล่นจริง เขียนซ้ำตอน replay (เช่น ผลให้คะแนน)
}

[YarnVerb("serve_order", Replay = ReplayPolicy.Journal)]
public static async Awaitable ServeOrder() { ... }
```

- ตัว `YarnVerbs` เป็นคนห่อ: ตรวจ replay, ตรวจว่าเรียกถูกจังหวะ (warning ไม่เงียบ), log, แล้วค่อยเรียก feature
- เขียนคำสั่งใหม่โดยไม่ระบุนโยบาย = compile ไม่ผ่าน (attribute บังคับ) จึงไม่มีคำสั่งที่ "ลืมคิดเรื่อง replay"
- **ชื่อคำสั่งเดิมทั้งหมดอยู่ต่อได้** ผ่าน compat shim (alias ชี้ไปคำสั่งใหม่) จึงไม่ต้องแก้ `.yarn` ทั้งหมดทันที
- **เรื่องที่เขียนใหม่ได้ดีขึ้นมาก:** รวม `flow_prepare_drinks` + `wait_for_task` เป็นคำสั่งเดียว `<<serve_order>>` ที่ **รอจนเสิร์ฟและให้คะแนนเสร็จ**
  writer จึงเขียนว่า "สั่ง → ชง → ได้ผล" ในบรรทัดเดียว ไม่ต้องจำคู่คำสั่ง และ replay จัดการ journal ให้ที่จุดเดียว

### 3.5 Save / Replay ที่ออกแบบมาตั้งแต่ต้น

ตัดสินใจไว้แล้ว (Refactor Plan §10): save ที่ line ปัจจุบัน + silent replay รองรับ `<<detour>>` ในการเขียนใหม่ ทำให้เป็นส่วนหนึ่งของ `DialogueDirector` ไม่ใช่ของแถมที่ผูกกับ presenter:

- `DialogueDirector` ถือ: root node, `RootStartVariables`, `LineIndex`, คิวคำตอบ, journal, สถานะ replay และเป็น **ที่เดียว** ที่ presenter ถามว่า "ตอนนี้ replay อยู่ไหม"
- presenter (`Line`, `Option`) ไม่มี logic replay เลย แค่ถาม `Director.ShouldPresent(line)`
- ทุก feature ที่มีสถานะต้องคืน (ตัวละคร, unlocks, order, กล้อง, event log) implement `ISnapshotSource { Capture(); Restore(); }` แล้ว `GameRoot` ลงทะเบียน
  → เพิ่ม feature ใหม่ที่มี state = implement interface เดียว ไม่ต้องไปแก้ `SaveLoadManager`
- ข้อกำหนดกับ writer (ห้าม `random()` ในเงื่อนไข ฯลฯ) ย้ายไปเป็น **validator ตอน build** (§3.8) แทนที่จะเป็นเอกสาร

### 3.6 Input

- ใช้ Input System ทั้งหมดผ่าน Action Map เดียว (`Gameplay`, `Dialogue`, `UI`) ไม่มี `Input.GetKey` เหลือ
- ทุกปุ่มที่ผู้เล่นกดมี binding สำหรับ controller ตั้งแต่ต้น (ตรงกับ Request พิเศษของ Designer) และมี focus ของ UI (เปิด popup → focus ปุ่มปิด) เป็นกฎของทุกหน้าจอ
- ปรับ `IInputProvider` ของมินิเกมให้อ่านจาก action เดียวกัน

### 3.7 Localization ตั้งแต่ต้น (CLAUDE.md ระบุว่ายังไม่ได้ทำ)

- ข้อความทุกอย่างที่ผู้เล่นเห็นเป็น key: บทพูดใช้ระบบ localization ของ Yarn, ข้อความ UI/ชื่อ chapter/ชื่อวัตถุดิบใช้ String Table
- ห้าม hard-code string ใน `.cs` หรือ SO (ชื่อ chapter ใน `SO_DaySchedule` ที่ตอนนี้เป็น string ธรรมดาเปลี่ยนเป็น key)
- เหตุผลที่ต้องทำตอนเขียนใหม่: ย้ายทีหลังต้องไล่แก้ทุกที่ที่มี string

### 3.8 Test และเครื่องมือ

| เครื่องมือ | ทำอะไร |
|---|---|
| EditMode tests บน `Bar410.Domain` | กฎเครื่องดื่ม/คะแนน/ราคา รันโดยไม่เปิดฉาก |
| Smoke scripts (`TEST_*.yarn`) + รันผ่าน Unity MCP | `TEST_AllCommands`, `PLAYTEST`, `TEST_SaveLoad` เป็น regression ทุก milestone |
| **Yarn validator** (เมนู `Bar410 > Validate Yarn`) | ตรวจ: command ที่ไม่มีอยู่, `random()` ในเงื่อนไข, node ที่ไม่มี `flow_open_bar`, `Order_Cocktail_*` ที่ไม่มี `order_customer`, ชื่อตัวละครผิด |
| Validate Cocktail Data (มีอยู่) | สูตรรวม 10 หน่วย ไม่มีสูตรซ้ำ |
| Flow debug overlay (เฉพาะ editor) | แสดง `DayPart` / `BarStep` / order / journal ปัจจุบัน แทนที่ `[Debug]` ที่ปนในฉาก |
| Odin `[Required]` + validator ของ GameRoot | reference หลุดแล้วบอกตอนเปิดฉาก ไม่ใช่ตอนเล่น |

### 3.9 โครงโฟลเดอร์ (ระดับ B/C)

```
Assets/Bar410/
  Domain/            (asmdef)  ← ย้ายจาก Cocktail System/Cocktail/Domain + Session
  Data/              (asmdef)  SO + asset
  Features/<ชื่อ>/   (asmdef ต่อระบบ)
  Flow/              (asmdef)
  Dialogue/          (asmdef)  presenter + Director + YarnVerbs
  Save/              (asmdef)
  Host/              (asmdef)  GameRoot
  Content/           ศิลป์ เสียง prefab scene .yarn
  Tests/             EditMode + PlayMode
```
ศิลป์และ prefab ไม่ย้ายใน R1 (ย้ายโฟลเดอร์แล้ว GUID เดิมยังอยู่ แต่เสี่ยง reference เสีย ให้ย้ายเป็นขั้นแยกท้ายโครงการ)

---

## 4. ตัวกำกับ flow แบบไม่ใช้ HSM — สามแบบ

ทั้งสามแบบใช้รากฐาน §3 ร่วมกัน ต่างกันที่ "ใครเป็นคนรู้ลำดับของวันและลำดับของการชง"

### 4.1 แบบ ① Async Stations (ลำดับเป็นโค้ดอ่านจากบนลงล่าง)

คือแผน refactor เดิม แต่เขียนใหม่ในโครง §3

```csharp
// BartendingSession: ลำดับการชงอยู่ในที่เดียว อ่านได้ทั้งเรื่อง
while (true)
{
    await prepare.RunAsync(ctx);                       // ใส่ส่วนผสม ⇄ มินิเกม
    if (await garnish.RunAsync(ctx) == Next.Remake) continue;
    if (await serve.RunAsync(ctx)   == Next.Remake) continue;
    break;
}
```
```csharp
// Station = หนึ่งขั้น ตอนเข้า/ออกอยู่ที่เดียว ปิดทุกอย่างใน finally
public async Awaitable<Next> RunAsync(StepContext ctx)
{
    OpenUi(); Subscribe();
    try   { return await WaitForPlayerAsync(ctx.Cancel); }
    finally { Unsubscribe(); CloseUi(); }
}
```

วันหนึ่ง (Yarn ยังเป็นคนสั่ง):
```yarn
<<open_bar>>                    // DayRunner.OpenBarAsync (รอ chapter title)
Walter: One gin and tonic, please.
<<order_customer Walter>>
<<serve_order gin_tonic>>       // รอจนชง-เสิร์ฟ-ให้คะแนนเสร็จ
<<if $satisfaction == "Perfect">>
    Walter: Exactly right.
<<endif>>
<<close_bar>>
```

| เพิ่ม | ทำอย่างไร |
|---|---|
| ขั้นตอนใหม่ "ชิมก่อนเสิร์ฟ" | สร้าง `TastingStation` เพิ่มหนึ่งบรรทัดใน loop |
| มินิเกมใหม่ | subclass `BaseMiniGame` ใส่ใน `[MiniGameSystem]` (ไม่แตะ flow) |
| หน้าจอใหม่ เช่นร้านค้า | verb ใหม่ `<<open_shop>>` รอจนปิด |
| บทสอน (tutorial) | verb `<<wait_step Garnish>>` รอจน station นั้นเริ่ม (Station ยิง `StepEntered`) |

- **ข้อดี:** อ่านง่ายที่สุด, debugger เดินตามได้, `try/finally` ปิด UI ให้, ไม่มีตาราง/enum ให้ไล่แก้
- **ข้อเสีย:** ลำดับขั้นอยู่ในโค้ด designer เปลี่ยนเองไม่ได้; ต้องระวังเรื่อง `Awaitable` await ได้ครั้งเดียว/ไม่มี cancellation ในตัว (ต้องมี helper)
- **ใครแก้ลำดับ:** programmer
- **Replay:** `serve_order` เป็น `Journal` (ผลให้คะแนนถูกจดและเขียนซ้ำ)

### 4.2 แบบ ② Data-driven Step list (ลำดับเป็นข้อมูลใน Inspector)

ลำดับขั้นการชงเป็นลิสต์ใน asset ไม่ใช่ `while` ในโค้ด

```
SO_BartendingFlow (Odin list)
  1. Prepare      ← Station id "prepare"
  2. Garnish      ← "garnish"
  3. Serve        ← "serve"
  Remake → กลับไป step: Prepare   (ตั้งในช่อง "On Remake Go To")
```
- Station ยังเป็น MonoBehaviour ในฉาก; SO เก็บแค่ id (SO ถือ reference ของ scene object ไม่ได้) `StationRegistry` ใน `GameRoot` แปลง id → Station
- Runner อ่านลิสต์แล้ววนเรียก `RunAsync` ทีละตัว

| เพิ่ม | ทำอย่างไร |
|---|---|
| ขั้นใหม่ | เขียน Station ใหม่ (โค้ด) แล้ว designer **วางตำแหน่งในลิสต์ได้เอง** |
| สลับลำดับ / ข้ามขั้นสำหรับวันสอน | designer เปลี่ยนลิสต์ (หรือมีหลาย asset ต่อวัน) |

- **ข้อดี:** designer ปรับลำดับ/ทำหลาย flow ได้โดยไม่แตะโค้ด
- **ข้อเสีย:** มีชั้นอ้อมเพิ่ม (id, registry), เงื่อนไขซับซ้อน (ข้ามขั้นเมื่อ…) ทำให้ลิสต์กลายเป็นภาษาโปรแกรมที่ debug ยาก; ค่าเริ่มต้นของเกมนี้ลำดับตายตัว จึงอาจเป็น over-engineering
- **ใครแก้ลำดับ:** designer
- **Replay:** เหมือน ① (replay อยู่ที่ชั้น Yarn ไม่เกี่ยวกับลำดับขั้นการชง)

### 4.3 แบบ ③ Yarn เป็นผู้กำกับทั้งหมด

ไม่มี `BartendingSession`; writer สั่งทีละขั้นผ่าน verb ที่รอได้:
```yarn
<<bar_prepare>>          // รอจนใส่ส่วนผสม+มินิเกมเสร็จ
<<bar_garnish>>
<<bar_serve>>
```
remake ต้องเขียนเป็น `<<jump>>` ย้อนกลับใน Yarn

- **ข้อดี:** ลำดับทั้งหมดอยู่ในมือ writer, เล่นกลเปลี่ยนลำดับในบทพิเศษได้ทันที
- **ข้อเสีย (หนัก):** writer ต้องรู้กติกาการชงและจัดการ remake/ยกเลิกเอง; ถ้าลืม verb เกมค้าง; save/replay ซับซ้อนที่สุดเพราะ replay ต้องข้ามขั้นการชงที่ผ่านไปแล้วกลางบท; ตรวจความถูกต้องทำได้ยาก (validator ต้องเข้าใจกราฟของ Yarn)
- **ใครแก้ลำดับ:** writer
- **เหมาะเป็น:** ส่วนเสริมสำหรับบทพิเศษ ไม่เหมาะเป็นแกน

### 4.4 เปรียบเทียบและคำแนะนำ

| เกณฑ์ | ① Async Stations | ② Data-driven list | ③ Yarn กำกับ |
|---|---|---|---|
| อ่านง่ายสำหรับ programmer | ดีมาก | ดี | ปานกลาง |
| เพิ่มขั้นตอนใหม่ | โค้ด 1 class + 1 บรรทัด | โค้ด 1 class + วางในลิสต์ | โค้ด 1 verb + writer เขียน |
| ปรับลำดับโดยไม่แตะโค้ด | ไม่ได้ | ได้ (designer) | ได้ (writer) |
| ความเสี่ยงเกมค้าง | ต่ำ (`finally`) | ต่ำ | **สูง** |
| ความซับซ้อนของ save/replay | ต่ำ | ต่ำ | **สูง** |
| ขนาดงานตั้งต้น | M | M+ (registry, SO) | M แต่ validator XL |
| เหมาะกับเกมนี้ที่ลำดับตายตัว | **เหมาะ** | เกินความจำเป็น | ไม่เหมาะ |

**คำแนะนำ:** เริ่มด้วย **①** แต่ให้ `BartendingSession` อ่านลำดับจาก `Station[] steps` ที่ลากใน Inspector (แทนการเรียกสามตัวตรงๆ) จะได้ประโยชน์หลักของ ② (designer สลับ/ข้ามขั้นสำหรับวันสอนได้)
โดยไม่ต้องมี registry หรือ SO เพิ่ม; ถ้าวันหนึ่งต้องการหลาย flow ต่อวันจริง ค่อยยกลิสต์ออกเป็น SO

**แบบที่ผมไม่เสนอ:** event bus / blackboard ที่ทุกระบบฟังกันเอง ลำดับของเกมกระจายอยู่ทุกที่จนอ่านไม่ได้ ซึ่งเป็นปัญหาเดียวกับที่ทำให้ `FlowWiring` ยากอยู่แล้ว

---

## 5. เพิ่ม feature ง่ายขึ้นแค่ไหน (เทียบวันนี้กับหลังเขียนใหม่)

| อยากเพิ่ม | วันนี้ (HSM) | หลังเขียนใหม่ (ระดับ B + แบบ ①) |
|---|---|---|
| ขั้นตอนการชงใหม่ | enum + ตาราง transition (2 ระดับ) + state class + event ใน phase + enter/exit hook ใน `FlowWiring` + command | Station 1 class + ลากเข้า `steps` |
| คำสั่ง Yarn ใหม่ | `[YarnCommand]` static + `CommandTarget()` + จำว่าต้อง no-op ตอน replay (กระจาย 7 ไฟล์) | `[YarnVerb(Replay=...)]` 1 เมธอด compile บังคับระบุนโยบาย |
| ระบบที่มี state ต้อง save | แก้ `SaveLoadManager` (collect/apply) + `SaveData` | implement `ISnapshotSource` 1 class |
| มินิเกมใหม่ | subclass `BaseMiniGame` (เหมือนเดิม) | เหมือนเดิม |
| ตัวละครใหม่ | enum + `CharacterView` + `SO_Customer` + บรรทัด short-form ใน `TalkToCustomerManager.Characters` | SO_Customer + ตัวละครใน roster (short-form สร้างจาก roster อัตโนมัติ) |
| ภาษาใหม่ | ไม่มีระบบ | เพิ่มคอลัมน์ใน String Table / Yarn localization |
| ปุ่มบังคับด้วย controller | แก้ทีละไฟล์ที่ใช้ `Input.*` | เพิ่ม binding ใน Action Map |
| กล้องช็อตใหม่ | เพิ่ม `CameraEntry` + method ใน `CameraManager` | เพิ่ม `CameraEntry` (เรียกด้วย id) |
| บทสอนที่ข้ามขั้น | ทำไม่ได้โดยไม่แก้ state table | ใช้ `steps` อีกชุด หรือ `<<wait_step X>>` |

ข้อที่เปลี่ยนมากที่สุดคือสามแถวแรก ที่ปัจจุบันแต่ละงานแตะหลายไฟล์พร้อมกัน

---

## 6. แผนทำงาน (ถ้าเลือกระดับ B) — side-by-side

หลักการ: สร้างระบบใหม่ **ข้างๆ** ของเดิมในโฟลเดอร์ `Assets/Bar410/` และฉากใหม่ `Main_V2` ของเดิมยังเล่นได้ตลอด
ไม่มี "ช่วงที่เกมพัง" และสลับกลับได้ตราบใดที่ยังไม่ลบของเก่า

| ระยะ | งาน | ขนาด | เสร็จเมื่อ |
|---|---|---|---|
| **R0 สเปกและเกณฑ์ผ่าน** | ตรึงพฤติกรรมเดิม: วิดีโอเล่นเดโม 3 วัน, ไฟล์ค่าอ้างอิง `flow_phase/step`, checklist เส้นทางหลัก (ใช้ของ Refactor Plan §9), commit งานค้างและ tag `pre-rewrite` | S | มี baseline + tree สะอาด |
| **R1 รากฐาน** | asmdef ทั้งชั้น, `GameRoot`, Action Map ของ Input System (ยังไม่ต่อ gameplay), `YarnVerbs` + `ReplayPolicy` (+ validator ตัวแรก), spike `Awaitable`, โครง test | M | เปิดฉากเปล่า `Main_V2` ได้ root ผูกครบ, validator ทำงาน |
| **R2 พอร์ต Domain และข้อมูล** | ย้าย `Domain/*` และ `Session/*` เข้า asmdef + เขียน EditMode test ครอบกฎ GDD §15–18 (ใช้ Validate Cocktail Data เป็นตัวช่วย) | M | test ผ่านเท่าผลของเดิมกับสูตรทุกตัว |
| **R3 Vertical slice: ชงหนึ่งแก้ว** | feature Cocktail/Garnish/Minigame/Camera ผ่าน interface + Station ทั้งสาม + `BartendingSession` ฮาร์ดโค้ด order หนึ่งอัน ไม่มี Yarn | L | ชง → เสิร์ฟ → ได้คะแนนครบวงจรในฉากใหม่ (checklist §9 ข้อ 3–10) |
| **R4 วันและบท** | `DayRunner`, `DialogueDirector`, presenter ใหม่ (ใช้ Text Animator เดิม), verb ชุดเต็ม + compat shim ชื่อเดิม, Customers/Hub/Unlock/EventPopup/ChapterTitle | L | เดโม 3 วันเล่นจบใน `Main_V2` ผ่าน `.yarn` เดิม |
| **R5 Save / Replay** | `ISnapshotSource` ทุก feature, journal, `LineIndex`, watchdog, ปุ่ม Save ตามกฎ (ห้ามใน Event ฯลฯ), `TEST_SaveLoad.yarn` | L–XL | ผ่านเกณฑ์ "เสร็จเมื่อ" ของ Refactor Plan Phase 4 |
| **R6 Input และ controller** | ต่อ gameplay/UI/มินิเกมกับ Action Map, focus ของ popup | M | เล่นจบเดโมด้วย controller อย่างเดียว |
| **R7 Localization** | ย้ายข้อความทุกอย่างเป็น key, ภาษาตัวอย่างอย่างน้อยหนึ่งภาษาที่สอง | M | สลับภาษาแล้วไม่มี string ดิบ |
| **R8 สลับและลบ** | เทียบวิดีโอ/ checklist ทั้งหมด, ย้าย `Main_V2` เป็นฉากหลัก, ลบของเก่า (HSM, `FlowWiring`, presenter เก่า, ของตายใน §8.10 code map), ปรับ CLAUDE.md / ADR 0004 / Systems Guide | M | ฉากเก่าไม่มีใครอ้างถึง |

ลำดับที่แนะนำ: R0 → R1 → R2 → R3 เป็นก้อนแรกที่ **ตัดสินได้ว่าแนวทางใช้ได้ไหม** (vertical slice) ก่อนลงทุน R4–R8
จุดตัดสิน (kill criteria): ถ้า R3 จบแล้วการเพิ่ม Station ใหม่หรือ verb ใหม่ไม่ง่ายกว่าเดิมอย่างเห็นได้ชัด ให้หยุดและกลับไปแผน refactor ระดับ A

ระดับ A (แผนเดิม) เทียบเท่า R0 + ส่วนของ R1 + R3–R4 บางส่วน โดยทำบนฉากเดิม; ระดับ C เพิ่มงานพอร์ต manager/UI ทุกตัวเข้าระหว่าง R3–R4 (เพิ่มขนาดเป็น XL+)

### การใช้ของเดิมให้คุ้ม
| ของเดิม | เก็บ / ย้าย / ทิ้ง |
|---|---|
| `Cocktail/Domain/*`, `Session/*`, `YarnVariableSync` | ย้ายเข้า asmdef (R2) |
| `CocktailSystemManager`, `GarnishManager`, `MinigameSystemManager`, `CameraManager`, `TalkToCustomerManager`, `HubManager` | wrap ด้วย interface ตอน R3–R4 แล้วเก็บเนื้อใน (ระดับ B) |
| `BaseMiniGame` + มินิเกม | เก็บ |
| `CharacterView`, `SO_Customer` | เก็บ (ตัด LEGACY VISUAL เมื่อ art ครบ) |
| `ButtonScaleFeedback`, `PF_Button_Hub` และปุ่ม instance ทั้งหมด | เก็บ |
| `EventLogPresenter`, `DialogueControls`, `SkipScan`, `PauseAwareKeyCodes` | ย้ายเข้า `Bar410.Dialogue` ปรับให้ถาม `DialogueDirector` |
| `LinePresenterCocktailProject`, `OptionPresenterCocktailProject` | เขียนใหม่ส่วน replay |
| `GameLoopFSM`, state ทั้งหมด, `FlowWiring`, `SceneLoaderBridge` (ส่วน replay), `SaveLoadManager` | ทิ้ง (แทนด้วย §3–4) |
| `BubblePresenter/*`, `[00]Testing Yarn And Learning/*`, prefab ตัวละครเก่า | ทิ้งหลังตรวจ reference |
| `.yarn` ทั้งหมด | เก็บ ใช้ compat shim; ค่อยย้ายเป็น `<<serve_order>>` ทีหลัง |

---

## 7. เขียนใหม่ vs refactor ทีละขั้น

| | Refactor ทีละขั้น (ระดับ A บนฉากเดิม) | เขียนใหม่ side-by-side (ระดับ B) |
|---|---|---|
| ผลต่อเกมที่เล่นได้ระหว่างทำ | แก้ฉากเดิม ถ้าพังต้อง revert | ของเดิมไม่แตะ |
| ขอบเขตที่ได้ | flow + save | flow + save + โครงสร้าง + input + localization + test |
| ขนาด | M–L | L–XL |
| ความเสี่ยงหลัก | พฤติกรรมเปลี่ยนเงียบๆ จากลำดับเดิม | ทำเสร็จช้า / ไม่ถึง parity / เกิดสองระบบค้างนาน |
| ต้องมีวินัย | ทดสอบทุก Phase | เกณฑ์ผ่าน (R0) และ kill criteria (R3) |
| เหมาะเมื่อ | อยากแก้ปัญหา flow เร็ว | อยากเปลี่ยนโครงสร้างโปรเจกต์จริงจังและมีเวลา |

ทางประนีประนอมที่เป็นไปได้: ทำ **A ก่อน** (ได้ flow ใหม่ + save ที่ใช้ได้) แล้วค่อยทำ R1/R6/R7 (asmdef, Input, localization) เป็นโครงการแยกบนโค้ดที่สะอาดแล้ว
ข้อเสีย: ย้ายโฟลเดอร์/asmdef ทีหลังแตะไฟล์ทั้งโปรเจกต์ ข้อดี: ไม่มีช่วงสองระบบคู่กัน

---

## 8. ความเสี่ยงของการเขียนใหม่ และวิธีรับมือ

| ความเสี่ยง | รับมือ |
|---|---|
| ไม่ถึง parity (ลืมพฤติกรรมเล็กๆ ที่ `FlowWiring` ทำอยู่) | R0 จดพฤติกรรมเดิมจาก code map §3.3 และ Refactor Plan §4 ตาราง B; checklist เดียวกัน; อัดวิดีโอ |
| สองระบบค้างนานจนไม่มีใครกล้าลบของเก่า | กำหนดเส้นตายที่ R8 และ kill criteria ที่ R3 |
| asmdef แตกเป็นชิ้นเล็กเกินจน reference วุ่น | เริ่มจากชั้นใหญ่ (Domain, Data, Features รวมกลุ่ม, Flow, Dialogue, Host) แยกย่อยทีหลังเมื่อจำเป็น |
| ย้ายโฟลเดอร์แล้ว GUID/reference เสีย | ย้ายใน Unity Editor เท่านั้น ไม่ย้ายด้วย shell; ย้ายศิลป์/prefab เป็นขั้นแยกท้ายสุด |
| `Awaitable` มีข้อจำกัด (await ครั้งเดียว ฯลฯ) | spike ใน R1; มี helper กลาง; ทางสำรองคือ coroutine ที่ห่อด้วย completion source |
| Yarn Spinner API ที่ใช้ replay (node tracking, หยุดที่ line) อาจไม่ครบ | spike ใน R1 ก่อนตัดสินใจ R5 |
| Odin / Text Animator / Yarn plugin อัปเดตแล้ว API เปลี่ยน | ล็อกเวอร์ชันใน manifest ตลอดโครงการ |
| ทีมเล็ก ทำคนเดียวจนเสียโฟกัสกับ feature เกม | ตั้ง budget: ถ้า R0–R3 เกินกรอบเวลาที่ตั้ง ให้หยุดตามจุดตัดสิน |

---

## 9. การตัดสินใจ (ล็อกแล้ว 2026-11-07) และเรื่องที่ยังเปิดอยู่

### ล็อกแล้ว
| # | เรื่อง | ผล |
|---|---|---|
| 1 | ขอบเขต | **B** — ชั้นกำกับใหม่ เก็บ domain และ manager |
| 2 | ตัวกำกับ flow | **① Async Stations + ลิสต์ `steps` ใน Inspector** |
| 3 | วิธีทำ | side-by-side ในโปรเจกต์เดิม เก็บ asset/GUID และ package |
| 4 | Yarn contract | verb ใหม่ + shim ชื่อเดิม ลบ shim หลังย้ายบทครบ |
| 5 | UI | uGUI + TextMeshPro ต่อไป (UI Toolkit เฉพาะหน้าฟอร์มในอนาคตถ้าต้องการ) |
| 6 | Git | commit งานค้างแยกตามเรื่อง → merge `IngredientUnlock` เข้า `main` → แตกงานนี้เป็น branch `RefactorCode` |
| 7 | ภาษา | EN (ต้นฉบับ) + TH, สลับระหว่างเล่น, outsource แปล (§11) |
| 8 | เสียง | เสียง Expression ไม่ใช่ voice over ทั้งบรรทัด ชุดเดียวทุกภาษา เล่นผ่านคำสั่ง Yarn ด้วย Sound ID (§12) |
| 9 | Save | ตามเดิม (ที่บรรทัดปัจจุบัน + silent replay, ไม่ save ใน Event) เพิ่มหลาย slot + autosave ทิ้ง save เก่า (§13) |
| 10 | Controller | ทุกหน้าจอ เป้าหมาย Xbox/PlayStation, ไอคอนปุ่มเปลี่ยนตามอุปกรณ์, joystick ควบคุมเคอร์เซอร์ (§14) |
| 11 | แพลตฟอร์ม | Windows/Linux จริง, console แค่เตรียม input ไว้ |
| 12 | ผู้ทำงาน | Programmer คนเดียว (เจ้าของ) เป็นคนเพิ่ม/แก้ Designer สั่งงานมา → ไม่ต้องทำเครื่องมือสำหรับ writer ที่ไม่เขียนโค้ด ลดน้ำหนัก validator/Inspector ลง |
| 13 | เวอร์ชันสำรอง | มี build สำรองสำหรับออกงานอยู่แล้ว 1 ตัว → ไม่ต้องรักษาให้เล่นได้ตลอดเวลาแบบเข้มงวด แต่ยังใช้ side-by-side เพราะเป็นวิธีลดความเสี่ยง |
| 14 | DI | ผูกมือใน `GameRoot` (ไม่ได้ตอบ ใช้ค่าแนะนำ ถ้าไม่เห็นด้วยบอกได้) |
| 15 | สลับภาษา (O2) | **สลับสดทั้งหมดโดยไม่เกี่ยวกับ save/replay** (วิธี C ใน `Bar410_LanguageSwitch_Options.md`; แทนการตัดสินใจ reload+replay เดิม) ดู §11.4 |
| 20 | บรรทัดที่แสดงอยู่ตอนสลับภาษา | พิมพ์ใหม่ตั้งแต่ต้นด้วย typewriter |
| 21 | เสียง Expression ที่เล่นอยู่ตอนสลับภาษา | เล่นต่อ ไม่เล่นซ้ำ |
| 22 | เมนูภาษา | Pause › Settings (แท็บ Language เดิม) ภาษาเริ่มต้นครั้งแรกไม่ได้ระบุ ใช้ EN จนกว่าจะบอกเป็นอย่างอื่น |
| 23 | ประวัติบทสนทนา / ข้อความขาด / มินิเกม | ประวัติเปลี่ยนภาษาตาม (เก็บ line ID), ไม่มีคำแปลให้ใช้ EN พร้อมเตือน, สลับในมินิเกม/Garnish ได้ |
| 24 | ระบบ localization (O1) | **Unity Localization + Yarn (`UnityLocalisedLineProvider`) ตัดสินแล้ว ไม่ต้อง spike เป็นเกณฑ์ตัดสิน** รายการตรวจใน R1 ยังต้องทำเป็นการยืนยันการใช้งาน (§11.2) ผู้แปลรับ-ส่งเป็น **ไฟล์เท่านั้น** (ไม่ซิงก์ Google Sheets) เวลาโหลดที่เพิ่ม: ต้อง < 30 วิ (เป้า < 10 วิ) |
| 25 | Scene แยก (O4) | **UI ของ Minigame/Garnish เป็น additive scene ส่วน 3D และแหล่งภาพของ RawImage อยู่ฉากหลัก** สื่อสารด้วย contract เล็กๆ ดู §15.6 ยืนยันที่จุดตัดสิน R3 |
| 16 | Sync เสียงกับตัวละคร (O3) | ขยับตัว (bob/scale pulse) ตามช่วงเสียง ไม่ต้องมี art เพิ่ม (§12.3) |
| 17 | กล้องด้วย controller (O5) | stick ซ้ายเคอร์เซอร์ + stick ขวาหมุนกล้อง (§14.3) |
| 18 | Autosave (O6) | ต้นวัน (หลัง Event จบ), หลัง Serve แต่ละแก้ว, จบวัน หมุน 2–3 ชุด (§13) |
| 19 | รายการรูปที่มีตัวอักษร และฟอนต์ไทย (O7) | Designer และ Artist เป็นผู้ส่งให้ (§11.6) |

ขนาดเนื้อหาที่ใช้ประเมิน: 21 วัน, 8 ตัวละคร, 28 สูตรปกติ + 8 สูตรพิเศษ (36 สูตร, แต่ละสูตรมี Description สำหรับสั่งแบบ Detailed), วัตถุดิบ 17 ชนิด (Base Spirit 5, Liqueur 4, Mixer 8)

### ยังเปิดอยู่ (ต้องการคำตอบหรือ spike)
(O1–O7 และ O2b ปิดแล้วเป็นข้อ 15–25 ด้านบน ตอนนี้ไม่มีเรื่องเปิดค้างระดับการตัดสินใจ เหลือแต่รายการตรวจ/ความเสี่ยง)

| # | เรื่อง | ตัวเลือก / สถานะ |
|---|---|---|

---

## 10. ที่ยังไม่ได้ตรวจ / ข้อจำกัดของเอกสารนี้

- ไม่ได้อ่านโค้ดเต็มของ Garnish, Minigame, Camera, Sound, Pause, BookUI, BaseInteractable จึงยังไม่รู้ว่า wrap ด้วย interface ยากแค่ไหน (ประเมินใน R3)
- ไม่ได้ตรวจ API replay ของ Yarn Spinner 3 ในเวอร์ชันที่ติดตั้งว่าหยุดที่ line ได้ตรงตามที่ออกแบบหรือไม่ (spike ใน R1)
- ขนาดงานเป็นขนาดสัมพัทธ์ ไม่มีการประเมินเวลา
- ไม่ได้ตรวจว่า asmdef แยกชั้นเข้ากับ plugin ที่ใช้ (Odin, Yarn, Text Animator, CocktailCrafter_V2) ได้ทั้งหมด บาง plugin ที่อยู่ใน `Assembly-CSharp` อาจต้องมี asmdef ครอบก่อน
- ข้อเสนอเรื่อง `<<serve_order>>` ยังไม่ได้ทดลองกับ `SkipScan` (ที่ค้นหาชื่อ `flow_prepare_drinks`) ต้องแก้ `SkipScan` ให้ค้นด้วย verb ใหม่

---

## 11. หลายภาษา (EN + TH) — ออกแบบ

### 11.1 ข้อเท็จจริงที่ตรวจแล้ว
- `manifest.json` **ยังไม่มี** Unity Localization package; มี Input System 1.19.0
- Yarn Spinner 3.2.5 มี `Runtime/Localisation/` และ `UnityLocalisedLineProvider` (ใช้ได้เมื่อมี Unity Localization) จึงมีทางเลือกในข้อ 11.2
- โค้ดที่เป็น string ดิบ: `SO_DaySchedule` (ชื่อ chapter), `SO_IngredientUnlockTable` (Display Name) และ SO ของสูตร/ลูกค้าที่ถือคำอธิบาย (ยังไม่ได้ไล่รายการเต็ม)

### 11.2 ทางเลือกระบบ
| | (a) Unity Localization + Yarn provider | (b) Yarn อย่างเดียว + ตาราง CSV เอง |
|---|---|---|
| บทพูด | Yarn line ID → String Table | Yarn CSV ต่อภาษา |
| UI / ชื่อ / Description | String Table | ตาราง CSV เอง + component เอง |
| รูปที่มีตัวอักษร (C4) | Asset Table (sprite ต่อภาษา) | ต้องทำเอง |
| ฟอนต์ต่อภาษา | ผูกกับ Locale ได้ | ทำเอง |
| สลับภาษา live | มี event ให้ฟัง | ทำเอง |
| ความเสี่ยง | เพิ่ม package, ต้อง spike ให้เข้ากับ Text Animator + Yarn presenter | โค้ดเองมากขึ้น แต่ไม่มี dependency เพิ่ม |

**ตัดสินแล้ว: (a)** เหตุผลเดิม: ต้องมี sprite ต่อภาษา, ฟอนต์ต่อภาษา และสลับ live ซึ่งการทำเองจะกลายเป็นการสร้างระบบเดียวกันซ้ำ

**รายการตรวจใน R1 (ไม่ใช่ตัวตัดสิน แต่ถ้าล้มเหลวต้องกลับมาคุยกัน):** (1) บทพูด EN/TH ผ่าน `UnityLocalisedLineProvider` (2) พิมพ์บรรทัดที่แสดงอยู่ใหม่ด้วย Text Animator โดย tag ครบ (3) ภาษาไทยในกล่องข้อความจริง (4) SO หนึ่งตัวด้วย `LocalizedString` + รูป 1 ใบด้วย Asset Table (5) เวลาเริ่มเกมที่เพิ่ม ต้อง < 30 วิ (เป้า < 10 วิ) และขนาด build ที่เปลี่ยน ทางถอยถ้าล้ม: Yarn อย่างเดียว (line ID ไม่เปลี่ยน)

**ขั้นตอนเตรียมก่อนเริ่ม (Q1.4):** ใช้ปุ่ม **"Add Line Tags to Yarn Scripts"** ที่ Yarn มีใน Inspector ของ `.yarnproject` (พบใน `YarnProjectImporterEditor`) ไม่ต้องเขียน tool เอง เสนอขั้นตอน: commit งานค้าง → กดปุ่ม → ตรวจว่าทุกบรรทัดบทพูดได้ `#line:` (นับก่อน/หลัง) → commit แยก "Yarn: add line tags" ผลคือ `.yarn` เปลี่ยนทุกไฟล์ ต้องทำก่อนส่งไฟล์ให้ผู้แปลและก่อนแก้ต้นฉบับต่อ ข้อควรระวัง: ปุ่มทำงานไม่ได้ถ้าโปรเจกต์มีการเปลี่ยนที่ยังไม่ save หรือคอมไพล์ไม่ผ่าน

**การรับ-ส่งผู้แปล (ไฟล์เท่านั้น):** ไม่ใช้ส่วนขยาย Google Sheets ใช้ Export CSV ของ Yarn/Unity Localization ส่งเป็นไฟล์ (xlsx/csv ตามที่ผู้แปลสะดวก) และรับไฟล์กลับมา import แล้วรัน validator (ครบทุก key, tag markup เปิด/ปิดครบ, ตัวแปร `{$x}` ครบ) ถ้าผู้แปลคืนเป็น xlsx ต้องมีสคริปต์แปลง xlsx ↔ CSV (ยังไม่ได้เขียน เป็นงานเล็กใน R7)

### 11.3 กติกาข้อมูล
- SO ทุกตัวเก็บ **key** ไม่เก็บข้อความ: `recipe.<id>.name`, `recipe.<id>.desc` (36 สูตร ใช้กับสั่งแบบ Detailed), `ingredient.<id>`, `character.<id>`, `chapter.<day>`, `event.<id>.title/body`, `ui.<screen>.<item>`
- ตอนนี้ออเดอร์แบบ Detailed ดึงข้อความจาก SO_ ที่มีแต่ภาษาอังกฤษ ให้ `OrderDescriptionProvider` เดียวเป็นคนแปล key → ข้อความ ตามภาษาปัจจุบัน
- ต้อง tag line ID (`#line:`) ให้ครบในทุก `.yarn` และ commit ก่อนส่งแปล ไม่งั้นแก้ต้นฉบับแล้วจับคู่คำแปลไม่ได้
- ขั้นตอน outsource: export CSV จาก Yarn → แปลงเป็น Spreadsheet ส่งให้ผู้แปล → รับกลับ → แปลงเป็น CSV → import แล้วรัน validator ตรวจ (ครบทุก key, tag markup `[wave]…[/wave]` เปิด/ปิดครบ, ตัวแปร `{$x}` ครบ) ใส่คอลัมน์ context (ตัวละคร, node, ตัวแปรที่ใช้) ให้ผู้แปล
- เนื้อหาใหญ่เทียบกับ 21 วัน × 8 ตัวละคร จึงควรสร้าง script แปลง CSV ↔ Spreadsheet ตั้งแต่ต้น

### 11.4 การสลับภาษาระหว่างเล่น
- ทุก component ที่แสดงข้อความ (`LocalizedText`) ฟัง `LocaleChanged` แล้วอัปเดตตัวเอง; UI ที่สร้างตอนรันต้องผูกด้วยกติกาเดียวกัน
- **ตัดสินแล้ว (O2):** สลับสดทั้งหมด **แยกจาก save/replay** (วิธี C; เหตุผลและทางเลือกอื่นอยู่ใน `Bar410_LanguageSwitch_Options.md`) แทนแนวทาง reload scene ที่เคยเขียนไว้ที่นี่
- **UI:** ทุก component ที่แสดงข้อความ (`LocalizedText`) ฟังเหตุการณ์เปลี่ยนภาษาแล้วอัปเดตตัวเอง รวมฟอนต์และ layout สลับได้ทุกสถานะรวมมินิเกม/Garnish
- **บทพูดที่แสดงอยู่:** `DialogueDirector` เก็บ `Yarn.Line` ของบรรทัด (และตัวเลือก) ที่แสดงอยู่ เมื่อเปลี่ยนภาษาขอข้อความใหม่จาก line provider (`GetLocalizedLineAsync`) แล้ว **พิมพ์บรรทัดใหม่ตั้งแต่ต้นด้วย typewriter** (ตัดสินแล้ว) เสียง Expression ที่เล่นอยู่เล่นต่อ ไม่เล่นซ้ำ และบรรทัดที่พิมพ์ใหม่ไม่นับเป็น "อ่านแล้ว" เพิ่มและไม่ยิงคำสั่ง/effect ซ้ำ
- **กติกาที่ต้องรักษา:** ทุกที่เก็บ key/line ID ห้ามเก็บข้อความที่แปลแล้ว (จุดเสี่ยงที่ทราบ: `EventLogPresenter`, คำอธิบายออเดอร์แบบ Detailed ใน `Order`, คิว `EventPopupQueue`)
- **ข้อความที่ยังไม่มีคำแปล:** แสดง EN และเตือนใน console/validator
- **ภาษาเริ่มต้นและเมนู:** เมนูอยู่ใน Pause › Settings ภาษาเก็บใน settings ไม่ใช่ใน save
- **ปุ่ม debug:** เพิ่มปุ่มสลับ EN↔TH ใน `GameFlowDebugHotkeys` ใช้ไล่ตรวจทุกหน้าจอ
- **spike ใน R1 (ยังไม่ได้ทดลอง):** (1) presenter เข้าถึง `Yarn.Line` และขอข้อความใหม่ได้ไหม (2) Text Animator parse tag ใหม่และพิมพ์ใหม่ได้ถูกต้องไหม ถ้าไม่ผ่านให้ถอยไปแบบ "UI สด / บทพูดเปลี่ยนบรรทัดถัดไป"
- Event Log (ประวัติบทสนทนา) ต้องเก็บ line ID ไม่เก็บข้อความ ไม่งั้นสลับภาษาแล้วประวัติจะค้างภาษาเก่า (ตัดสินแล้วว่าประวัติเปลี่ยนตามภาษา) (ตอนนี้ `EventLogPresenter` เก็บข้อความและบันทึกลง save ต้องแก้)

### 11.5 ภาษาไทย — ความเสี่ยงที่ต้อง spike (ยังไม่ได้ทดลอง)
1. **ตัดคำ:** ภาษาไทยไม่มีเว้นวรรคระหว่างคำ TextMeshPro ไม่ตัดคำไทยเอง บรรทัดอาจขึ้นบรรทัดใหม่กลางคำ แนวทางทั่วไปคือแทรก zero-width space (U+200B) ตามคำ (ผู้แปลหรือ tool ตอน import) ต้องทดสอบกับกล่องข้อความจริง
2. **Text Animator + ตัวอักษรไทย:** effect ต่อตัวอักษร (wave/shake) อาจแยกสระบน/วรรณยุกต์ออกจากพยัญชนะ และโหมด typewriter แบบ "Word" จะถือทั้งประโยคเป็นคำเดียวถ้าไม่มีเว้นวรรค (ข้อ 1 ช่วยได้) ต้องดูในเกมจริง
3. **ฟอนต์:** ต้องมี TMP Font Asset ไทย (Dynamic + fallback) ต่อ Locale ตรวจว่ามีอักขระครบ รวมถึงตัวเลข/เครื่องหมายที่เกมใช้ ขนาดและ line spacing ของฟอนต์ไทยมักต้องต่างจากละติน
4. **ความยาวข้อความ:** ไทยยาวกว่า/สูงกว่าอังกฤษ ต้องเผื่อพื้นที่ใน UI (ปุ่ม, post-it ออเดอร์, หนังสือสูตร, popup)

### 11.6 ข้อความในรูป (C4: มี)
- ต้องสำรวจว่ารูปไหนมีตัวอักษร (ป้าย, โน้ต, หนังสือ ฯลฯ) แล้วทำเป็น sprite ต่อภาษา (Asset Table ถ้าเลือก O1-a) หรือเปลี่ยนเป็นข้อความจริงซ้อนบนรูปพื้นหลัง วิธีหลังถูกกว่า ใช้ได้ถ้า art รองรับ Designer และ Artist เป็นผู้ส่งรายการให้ (ตัดสินแล้ว) ผมจะตั้งช่องรับรายการและตรวจกับโค้ดว่าครบ

### 11.7 ขนาดงาน
M–L รวมระบบ, สำรวจข้อความ, ฟอนต์ไทย, tool แปลง CSV และ validator อยู่ในเฟส R7 (ดู §6)

---

## 12. เสียง Expression — ออกแบบ

หมายเหตุ: ตามที่ตอบ นี่ไม่ใช่ voice over ทั้งบรรทัด แต่เป็นเสียงสั้นๆ เช่น *sigh* *HUH!?* ตามอารมณ์ จึงไม่ต้องมีเครื่องมือตรวจ "บรรทัดไหนไม่มีเสียง" และไม่ต้องแยกเสียงตามภาษา

### 12.1 การเล่น
- คำสั่งใน Yarn ระบุ Sound ID ตรงๆ เช่น `<<voice Isla huh>>` (ชื่อ verb ยังไม่ตัดสิน) ใช้ในบรรทัดที่ต้องการ เพิ่มไว้ในชั้น Yarn verb เป็น `ReplayPolicy.Skip` (ไม่เล่นตอน silent replay ตอนโหลด save)
- เสียงมาจากหลายแหล่ง (บันทึกเอง, นักพากย์, AI) ไม่กระทบโค้ด ผูกกับ ID เท่านั้น
- ใช้ `SoundManager` เดิม (เสียงผ่าน Sound ID ใหม่ต้องตรวจว่าคืน handle ที่หยุด/รอจบได้หรือไม่ ยังไม่ได้อ่านโค้ดส่วนนี้ครบ)

### 12.2 พฤติกรรมที่ล็อกแล้ว → สิ่งที่ต้องทำ
| พฤติกรรม | ต้องทำ |
|---|---|
| หยุดเสียงเมื่อกดข้ามบรรทัด | เสียงที่เริ่มในบรรทัดนั้นถูกผูกกับ "ขอบเขตบรรทัด" (`LineVoiceScope`) `DialogueDirector` สั่งหยุดทั้งหมดเมื่อบรรทัดจบ/ถูกข้าม |
| AutoPlay รอเสียงจบ | `DialogueDirector` มี `IsLineBusy` = พิมพ์จบ **และ** เสียงของบรรทัดจบ `DialogueControls` ใช้ค่านี้ ใส่เวลารอสูงสุดกันเสียงลูป/ยาวผิดปกติ |
| Skip ข้ามเสียงของบทที่เล่นแล้ว | โหมด Skip ใช้ข้อมูล "เคยอ่านแล้ว" ของ `SkipScan` เดิมเพื่อไม่เล่นเสียง (ต้องแยกจากกรณีกด Skip บรรทัดที่ยังไม่เคยอ่าน ซึ่งทำงานตามกติกา Skip เดิม) |
| Text Animator ไม่ต้องปรับตามความยาวเสียง | ไม่ต้องทำอะไร |

### 12.3 Sync ท่าทางกับเสียง (ตัดสินแล้ว: ขยับตัว)
ตัวละครเป็น sprite แผ่นเดียวต่อสีหน้า (`CharacterView`) จึงไม่ทำปากขยับ แต่ให้ตัวละครขยับตัวเล็กน้อย (bob/scale pulse ด้วย DOTween) ตลอดช่วงที่เสียงของตัวละครนั้นเล่น ไม่ต้องมี art เพิ่ม
จุดเชื่อม: handle ของเสียงมี event `Started/Ended` พร้อมชื่อตัวละคร `CharacterView` ฟังแล้วตั้ง `IsSpeaking` (ต่อ Animation v2 2D bone ทีหลังได้โดยฟัง event เดียวกัน) ท่านี้ต้องไม่ชนกับ `_idleBob`/`_walkBob` เดิม (ตรวจตอนทำ)

### 12.4 ขนาดงาน
S–M ทำต่อจาก R4 (verb ชุดเต็ม) และ `DialogueDirector`

---

## 13. Save / Load — ส่วนที่เพิ่มจาก Refactor Plan Phase 4

ยืนยันแล้ว: save ที่บรรทัดปัจจุบัน + silent replay รองรับ `<<detour>>`, ไม่ save ใน Event (รายละเอียดตาม `Bar410_Refactor_Plan_Remove_HSM.md` Phase 4) ที่เพิ่มคือ:

- **หลาย slot:** ไฟล์ต่อ slot (`slot_<n>.json`) + ไฟล์ metadata เล็ก (วัน, chapter key, เวลาเล่น, วันที่ save, รูปย่อถ้าต้องการ) เพื่อให้หน้าเลือก slot โหลดเร็วโดยไม่เปิดไฟล์เต็ม เขียนแบบ atomic (เขียนไฟล์ชั่วคราวแล้ว rename) กันไฟล์พังตอนปิดเกมกลางคัน
- **Autosave (ตัดสินแล้ว):** slot แยกที่ไม่ปนกับ slot ผู้เล่น เซฟตามกฎเดียวกับ save ปกติ (ไม่เซฟใน Event / ระหว่าง replay / Skip) จังหวะ: ต้นวัน (หลัง Event จบ), หลัง Serve แต่ละแก้ว และตอนจบวัน เก็บแบบหมุนเวียน 2–3 ชุด
- **Version:** ใส่ `SaveData.Version` แต่ **ไม่ทำ migration** (save เก่าทิ้งได้) ถ้าเวอร์ชันไม่ตรงให้แจ้งผู้เล่นและไม่โหลด
- **ภาษา/ตั้งค่า:** ไม่เก็บใน save slot เก็บใน settings (PlayerPrefs หรือไฟล์ settings) เพื่อให้ผู้เล่นสลับภาษาโดยไม่ผูกกับ slot
- **ชื่อ chapter ใน slot:** เก็บ key ไม่เก็บข้อความ (ให้เปลี่ยนภาษาแล้วหน้าเลือก slot เปลี่ยนตาม)
- **UI เลือก slot:** ต้องใช้ controller ได้ (§14)

---

## 14. Controller ทุกหน้าจอ (Xbox / PlayStation)

### 14.1 ข้อเท็จจริงที่ตรวจแล้ว
- Input System 1.19.0 ติดตั้งอยู่ และ `activeInputHandler: 2` (เปิดทั้งระบบเก่าและใหม่)
- `Input.GetKey/GetMouse/mousePosition/GetAxis` ยังถูกเรียก **31 จุด** ใน `[02]Script` + `[06]Dialogue`; `Mouse.current` 2 จุด
- ไฟล์ที่อ่านตำแหน่งเมาส์ตรงๆ: `CameraController`, `HoverTooltip`, `DebugPhysicsRaycaster`, `MenuParallaxLook`, `UIParallaxMouse`
- ยังไม่ได้อ่านว่ามินิเกมแต่ละตัวใช้ input อะไร (เขย่า/คน ใช้การลากเมาส์หรือปุ่ม) ต้องไล่ใน R6

### 14.2 โครงสร้าง
- **แหล่งตำแหน่งชี้เดียว** (`IPointerSource.ScreenPosition`) ทุกโค้ดที่เคยอ่านเมาส์ (raycast วัตถุดิบ, tooltip, parallax, กล้อง) อ่านจากที่นี่ ไม่อ่าน `Mouse.current` ตรงๆ เมาส์จริงและ virtual cursor ป้อนค่าเข้าที่เดียวกัน
- **โหมดชี้ต่อหน้าจอ:** แต่ละ Station/หน้าจอประกาศโหมด `Cursor` (stick ควบคุมเคอร์เซอร์) หรือ `Focus` (D-pad/stick ย้าย focus ปุ่ม) เมนู/Pause/Settings/เลือก slot ใช้ Focus ส่วนบาร์ (เลือกวัตถุดิบ), Garnish ใช้ Cursor
- **Virtual cursor:** Input System มีคอมโพเนนต์ virtual mouse สำหรับขับเคอร์เซอร์ด้วย stick (ต้องยืนยัน API และพฤติกรรมกับ `InputSystemUIInputModule` ใน spike) เพราะ uGUI รับ pointer event จากอุปกรณ์เมาส์เสมือนนี้ ปุ่ม UI และ `RenderTextureClickRelay` (Garnish) จึงน่าจะทำงานได้โดยไม่ต้องแก้ แต่ยังไม่ได้ทดสอบ
- **ไอคอนปุ่มตามอุปกรณ์:** `InputGlyphs` (SO แมป control → sprite สำหรับ Xbox / PlayStation / คีย์บอร์ด) ฟังอุปกรณ์ที่ใช้ล่าสุด แล้วอัปเดต `ButtonPrompt` ข้างปุ่ม ("ปุ่มนี้เปิดอะไร") ข้อความ prompt ใช้ localization (§11)
- **ชุด Action Map:** `Gameplay`, `Dialogue`, `UI` ตาม §3.6; ปรับ `IInputProvider` ของมินิเกมให้อ่าน action เดียวกัน
- ปิดระบบ Input เก่า (`activeInputHandler` → 1) ตอนท้าย R6 เมื่อ grep `Input.` ไม่เหลือ

### 14.3 กล้อง (ตัดสินแล้ว)
ADR 0002 Amendment: หมุนกล้องได้เฉพาะตอนเลือกวัตถุดิบ ใช้ stick ซ้ายขยับเคอร์เซอร์ + stick ขวาหมุนกล้อง (เทียบเท่าเมาส์ที่ทำสองอย่างพร้อมกัน) ต้องปรับความไวและ dead zone ด้วยการเล่นจริง

### 14.4 ขนาดงาน
M–L (31 จุดของ `Input.*` + มินิเกม + glyph + ทดสอบด้วยจอยจริง ซึ่งผมทดสอบเองไม่ได้ ต้องให้เจ้าของลองเล่น)

---

## 15. ทำ Minigame / Garnish เป็น scene แยก (additive) ได้ไหม (O4)

### 15.1 สิ่งที่เห็นในโค้ด (ตรวจแล้ว)
- **Garnish:** `GarnishManager` ใช้ `RawImage` แสดงภาพที่กล้องเรนเดอร์ (`_glassView`) และ `RenderTextureClickRelay` แปลงตำแหน่งคลิกบนรูปเป็น ray ยิงเข้ากล้อง เพื่อให้วัตถุ 3D (ช่องเครื่องตกแต่งบนแก้ว) ยังคลิกได้
- **ภาพลูกค้าข้างจอตอนมินิเกม:** `CameraManager.ShowCustomerInset/HideCustomerInset` เปิด/ปิดกล้องที่เรนเดอร์ลง `RT_CharacterCameraView`
- ยังไม่ได้อ่านโครงของวัตถุภายในมินิเกมแต่ละตัว และยังไม่รู้ว่าวัตถุแก้วที่ Garnish ใช้อยู่ฉากเดียวกับที่ Cocktail ใช้ชงหรือไม่

ตอบตรงๆ: **ทำได้** ในทางเทคนิค ข้อดีข้อเสียต่อไปนี้

### 15.2 ข้อดี
- **แยกกล้อง/แสง/ฟิสิกส์:** raycast ของ Garnish ไม่ชนวัตถุในฉากบาร์ (ใช้ Physics Scene แยกได้) ไม่ต้องใช้เลเยอร์/Culling Mask/Rendering Layer Mask คัดกรอง (เงาของวัตถุดิบตอนนี้ต้องใช้ Rendering Layer Mask 3 กันแสงรั่ว)
- **ไฟล์ฉากหลักเล็กลง:** แก้ไข `New Drag Drop System` น้อยลง merge conflict ใน scene ยากน้อยลง (คุณคนเดียวแต่ยังช่วยเรื่อง diff)
- **ทดสอบแยกได้:** เปิดฉากมินิเกมเดี่ยวๆ เล่นได้โดยไม่ต้องผ่าน flow ทั้งหมด (ตรงกับเป้าหมายใช้งานและเพิ่มฟีเจอร์ง่าย)
- **โหลด/ปลดความจำได้** ถ้าเลือกไม่ค้างไว้
- ตรงกับแนวทาง `GameRoot` + interface ใหม่ เพราะบังคับให้ฉากแยกคุยกันผ่าน interface

### 15.3 ข้อเสีย
- **อ้างอิงข้ามฉากใน Inspector ไม่ได้:** ทุกจุดที่ผูกกันตรงๆ ต้องเปลี่ยนเป็นการลงทะเบียนกับ `GameRoot` ตอนโหลดฉาก (ต้องมี handshake) เพิ่มสถานะ "กำลังโหลด" ให้ Station
- **Prefab `[GameLoop].prefab` ที่ถือ manager อยู่ตอนนี้:** การย้าย manager ออกไปฉากอื่นเปลี่ยนการผูก prefab ทั้งหมด ขัดกับการให้ฉากเดิมยังเล่นได้ระหว่าง side-by-side (ใน R3 ต้องสร้างของใหม่คู่กัน ไม่ย้ายของเดิม)
- **ของที่ต้องมีเพียงชิ้นเดียว:** EventSystem (ตอนนี้ฉากมี 2 ตัวอยู่แล้วเป็นบั๊ก), AudioListener, Cinemachine Brain ฉากเสริมต้องไม่มี และเพิ่มจุดที่ลืมแล้วพังเงียบๆ
- **แสง:** Ambient/light probes/skybox มาจากฉาก active; realtime light จากฉากเสริมมีผลกับทุกฉากถ้าไม่ใช้ layer mask ต้องออกแบบใหม่ ไม่ใช่แค่ย้าย; lightmap เป็นรายฉาก (ผล bake ตอนนี้อยู่ในฉากหลัก)
- **เวลาโหลด:** โหลดแบบ async แล้วอาจสะดุดถ้าโหลดตอนเข้า step; แก้ด้วยโหลดล่วงหน้าตอน BeforeOpen แล้วค้างไว้ (ใช้ความจำ)
- **Save/replay:** state ที่อยู่ในฉากเสริมต้องส่งผ่าน `ISnapshotSource` และ silent replay ต้องไม่โหลดฉากเหล่านี้
- **ปัญหาหลักของ RawImage ไม่หายไปเอง:** การแยกฉากไม่ได้ลดโค้ด "กล้อง + RT + RawImage + click relay" ที่ซ้ำกัน และ RT ยังเปลืองหน่วยความจำภาพเท่าเดิม

### 15.4 ทางเลือก
| | เป็นอย่างไร | ได้ | เสีย |
|---|---|---|---|
| 1. คงไว้ในฉากหลัก | ตอนนี้ | ไม่ต้องทำอะไร | ปัญหาข้างบนยังอยู่ |
| 2. **`WorldView` component ก่อน** (แนะนำ) | รวมกล้อง + RT + RawImage + click relay + เปิด/ปิด เป็นคอมโพเนนต์เดียวใช้ซ้ำกับ Garnish และภาพลูกค้า | ลดโค้ดซ้ำทันที เสี่ยงต่ำ ทำเป็นส่วนของ R3 | ไม่ได้แยกฟิสิกส์/แสง |
| 3. Prefab ที่สร้างตอนเข้า Station และทำลายตอนออก | ได้การแยกพื้นที่ใกล้ฉากแยก ไม่ต้องมีเครื่องจักรโหลดฉาก | ง่ายกว่า additive | แสง/ฟิสิกส์ยังอยู่ฉากหลัก |
| 4. Additive scene | ตามคำถาม | แยกครบ | ข้อเสียข้อ 15.3 ทั้งหมด |

### 15.5 ข้อเสนอ
ทำข้อ 2 ใน R3 ก่อน แล้ววัดผล ถ้า Minigame ยังติดปัญหาแสงรั่ว/ raycast ชนวัตถุอื่น ให้ลองข้อ 4 **เฉพาะ Minigame** (พึ่งฉากหลักน้อยกว่า) ส่วน Garnish ผูกกับแก้วที่ชงอยู่ จึงเสี่ยงกว่า ยังไม่แนะนำให้แยกก่อนตรวจว่าแก้วอยู่ที่ไหน
ตัดสินที่จุดตัดสินของ R3 (ไม่ตัดสินล่วงหน้า)

---

### 15.6 ตัดสินแล้ว (2026-11-07): UI เป็น additive scene, 3D ค้างอยู่ฉากหลัก

คำตอบของคุณ: ให้ฉากหลักเป็นผู้ฟัง API แล้วรับข้อมูลกลับน้อยๆ (Minigame ส่งแค่ "ชงเสร็จ/ยกเลิก", Garnish ส่งตัวตกแต่งและการเสิร์ฟกลับมา) เพราะทั้งสองส่วนใหญ่เป็น UI; แก้วของ Garnish เป็นชุดแยกที่ส่งผลกลับให้บาร์หลังเสร็จ; แหล่งภาพของ `RawImage` (กล้อง/วัตถุ/lightmap) ยังอยู่ฉากหลัก; แยกเฉพาะชุด UI เป็นคนละ scene

**ความเข้าใจของคุณถูกต้อง** และเป็นรูปแบบที่ปลอดภัยกว่า additive เต็มรูปแบบใน §15.2–15.3 เพราะ:
- แสง/lightmap/light probe ยังอยู่ฉากเดียว จึงไม่ต้องแก้ปัญหาแสงข้ามฉาก
- ข้อต่อระหว่างสองฉากมีแค่ contract เล็กๆ (ด้านล่าง) ตรงกับเป้า "ลดความซับซ้อน แก้ง่ายโดยไม่กระทบฉากหลัก"

**ข้อที่ต้องรู้ (ไม่ใช่ข้อผิดพลาดของคุณ แต่ผลลัพธ์ต่างจากที่คาด):**
1. รูปแบบนี้ **ไม่ได้แก้แสงรั่วหรือ raycast ชนผิดตัว** (Q2.2, Q2.3 ที่คุณเคยเจอ) เพราะวัตถุ 3D ยังอยู่ฉากหลัก ต้องแก้แยก: raycast ที่ชน/บังเกิดจาก `blocksRaycasts` ของ UI ที่ไม่ปิด → ให้ `WorldView`/Station เป็นคนปิด-เปิดกลุ่ม UI ตาม step ส่วนแสงรั่วแก้ด้วย Rendering Layer Mask เดิม จนกว่าจะมีเหตุให้ย้าย 3D ออกด้วย
2. `RenderTextureClickRelay` ต้องรู้กล้องของฉากหลัก (`_camera`) แต่ฉาก UI อ้างอิงข้ามฉากใน Inspector ไม่ได้ → ฉากหลักส่งกล้อง/RT ให้ UI ผ่าน contract ตอนเริ่ม (`WorldViewHandle`)
3. EventSystem/AudioListener/Brain ห้ามมีในฉาก UI (มี EventSystem 2 ตัวอยู่ในฉากหลักแล้วเป็นบั๊กที่ต้องแก้)

**Contract (ร่าง ยังไม่ได้คอมไพล์):**
```csharp
public interface IMinigameHost  { Awaitable<MinigameOutcome> RunAsync(MinigameRequest r, CancellationToken ct); } // Completed | Cancelled
public interface IGarnishHost   { Awaitable<GarnishResult>  RunAsync(GarnishRequest r,  CancellationToken ct); } // decoration + serve
// ฉาก UI ลงทะเบียนตัวเองกับ GameRoot ตอนโหลด ฉากหลักเรียกผ่าน interface และส่ง WorldViewHandle (กล้อง/RT) ให้
```
`GarnishRequest` ส่งสถานะแก้วที่ต้องใช้ตกแต่ง (ไม่ใช่ object ของแก้ว) และ `GarnishResult` คืนข้อมูลการตกแต่ง ให้ฉากหลักสร้างผลบนแก้วในบาร์เอง

**เกณฑ์ผ่านที่จุดตัดสิน R3 (ตามที่คุณกำหนด):** ห้ามตกต่ำกว่า 60 fps และสะดุดตอนเข้า/ออกไม่เกิน 300 ms (ใช้การโหลดล่วงหน้าตอน BeforeOpen แล้วค้างไว้แบบ inactive ถ้าโหลดตอนเข้า step ไม่ทัน); คลิกผ่าน RT ใช้ได้ทั้งเมาส์และ virtual cursor; มินิเกมตัวใหม่เพิ่มได้โดยไม่แก้ฉากหลักเกิน 1 จุด
**Q2.7 (พอใจ `WorldView` อย่างเดียวไหม):** คุณตอบ "ต้องดูอีกครั้งก่อน" จึงทำ `WorldView` (ตัวรวมกล้อง+RT+RawImage+click relay) ใน R3 อยู่ดี และใช้เป็นส่วนของ contract นี้ แล้วดูผลที่จุดตัดสินว่าจะแยก UI เป็นฉาก additive จริงหรือไม่ ถ้าผลชี้ว่าไม่คุ้ม ถอยไปให้ UI อยู่ฉากหลักได้ (contract ยังใช้ได้เหมือนเดิม)

---

## 16. ผลต่อแผนงาน R0–R8 (§6)

| เฟส | ปรับ |
|---|---|
| R0 | ตามคำตอบ: commit แยกตามเรื่อง → merge `IngredientUnlock` → branch `RefactorCode` และ tag `pre-rewrite` ก่อนเริ่ม (ผมไม่ได้ทำให้ ต้องสั่ง) |
| R1 | กดปุ่ม "Add Line Tags to Yarn Scripts" แล้ว commit แยก; ติดตั้ง Unity Localization (ตัดสินแล้ว) และรายการตรวจ (§11.2): Yarn provider, ขอข้อความใหม่จาก `Yarn.Line` + Text Animator พิมพ์ใหม่, virtual mouse + `InputSystemUIInputModule`, Thai + Text Animator |
| R3 | เพิ่ม `WorldView` (§15.4 ข้อ 2) และ contract `IMinigameHost`/`IGarnishHost` + ฉาก UI additive ของ Minigame เป็นตัวทดลองแรก (§15.6) จุดตัดสินวัด 60 fps / 300 ms |
| R4 | เพิ่ม verb เสียง Expression + `LineVoiceScope` + `IsLineBusy` (§12) และให้ `DialogueDirector` เก็บ `Yarn.Line` ที่แสดงอยู่เพื่อสลับภาษา (§11.4) |
| R5 | เพิ่ม multi-slot + autosave + metadata (§13) |
| R6 | ขยายเป็น M–L: pointer source เดียว, virtual cursor, glyph, Focus/Cursor ต่อหน้าจอ, ไล่ input ของมินิเกม (§14) |
| R7 | ขยายเป็น M–L (สลับภาษาไม่ต้องรอ R5 แล้ว): ภาษา EN + TH, ฟอนต์ไทย, tool CSV ↔ Spreadsheet, รูปที่มีตัวอักษร (§11) |

เพราะมีคนทำคนเดียว ให้ถือว่าลำดับ R3 → R4 → R5 คือเส้นทางวิกฤต R6/R7 ทำได้ขนานบางส่วนกับ R4–R5 เพราะเป็นเรื่องคนละชั้น แต่คนเดียวทำพร้อมกันได้จำกัด จึงเรียงทีละเฟสจะคาดการณ์ง่ายกว่า
