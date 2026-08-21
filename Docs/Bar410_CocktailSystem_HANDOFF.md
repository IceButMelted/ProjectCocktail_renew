# Bar410 — Cocktail System: เอกสารส่งต่องาน / HANDOFF

**Date:** 2026-08-21 · **Branch:** `GameLoop/main` · **HEAD:** `a88c3f8` (push แล้ว)
**สถานะ:** working tree สะอาด · คอมไพล์ผ่านไม่มี error · `Bar410 > Validate Cocktail Data` ผ่าน

> อ่านไฟล์นี้ก่อนไฟล์อื่นทั้งหมด แล้วค่อยเจาะรายละเอียดจากเอกสารที่อ้างถึงใน §2

---

## 1. งานนี้คืออะไร

Refactor `Assets/[02]Script/Cocktail System/` ทั้งระบบ เพื่อ 4 เป้าหมาย:

1. กระชับโค้ด ลบโค้ดตาย ยุบโค้ดซ้ำ
2. แยกหน้าที่ตาม SOLID
3. **ทำให้ตรงกับ GDD** — พบว่ากติกาที่โค้ดใช้ไม่ตรงสเปก 15 จุด
4. เตรียมเชื่อมกับ Hierarchical State Machine (HSM) ที่มีอยู่แล้ว

ทำครบ Phase 0–8 ฝั่งโค้ด และเซ็ตซีน `New Drag Drop System` ให้ใช้สถาปัตยกรรมใหม่เต็มตัว

---

## 2. แผนที่เอกสาร — เปิดอันไหนเมื่อไหร่

| ไฟล์ | ใช้เมื่อ |
|---|---|
| **`Bar410_CocktailSystem_HANDOFF.md`** | ไฟล์นี้ — ภาพรวมและสถานะปัจจุบัน |
| `Bar410_CocktailSystem_Architecture.md` | **ไฟล์ไหนทำอะไร · จะแก้อะไรต้องเปิดไฟล์ไหน** ← ใช้บ่อยสุด |
| `Bar410_CocktailSystem_Manual_Setup.md` | งานที่ต้องทำมือใน Unity + สถานะรายซีน |
| `Bar410_CocktailSystem_Refactor_Plan.md` | เหตุผลเบื้องหลังทุกการตัดสินใจ (§10, §13 Decision Log) |
| `Bar410_CocktailSystem_Refactor_Report.md` | รายละเอียดเต็ม บั๊กที่แก้ ผลตรวจสอบ ตัวเลข |
| `Bar410_CocktailSystem_Refactor_Summary.md` | สรุปสั้น |
| `GDD_Bar410_Master.md` | **source of truth ด้านกติกาเกม** (มีสำเนาใน `Docs/` แล้ว) |
| `Bar410_StateMachine_Implementation.md` | โครง HSM ที่มีอยู่ก่อน |
| `Bar410_Minigame_Integration_Plan.md` | แผนเชื่อม minigame — **ยังไม่ได้ทำ** |

---

## 3. ประวัติ commit

| Commit | เนื้อหา |
|---|---|
| `99f9cef` | เคลียร์ของค้าง — ลบ `BookUI.cs` / `GameLoopManager.cs`, ย้าย `NPC_Base.cs` |
| `9f91ed8` | Phase 0–2 + §4.7 — สร้าง `Domain/` layer และทำให้ตรง GDD |
| `3ac5b1b` | Phase 3–8 — ผ่า shaker, `DrinkOrderContext`, HSM bridges, ย้ายไฟล์, validator |
| `663c15f` | เอกสาร — Architecture reference |
| `a88c3f8` | เซ็ตซีน + ข้อมูลลูกค้า + อัปเดต GDD |

---

## 4. สถาปัตยกรรมโดยย่อ

```
Flow bridges ──── Hierarchical State Machine/   BarSetupBridge · CocktailFlowBridge
Yarn adapters ─── Cocktail System/Yarn/         4 ไฟล์ (partial class เดียวกัน)
Session ───────── Cocktail System/Session/      DrinkOrderContext · OrderService · Scoring
Scene runtime ─── Cocktail System/Cocktail/Shaker/   ShakerContents · VisualPresenter · ...
Domain ────────── Cocktail System/Cocktail/Domain/   กติกาเกมทั้งหมด (static, ไม่มี Unity)
```

**กฎเดียว:** ลูกศรชี้ลงเสมอ · `Domain/` ไม่รู้จัก `MonoBehaviour` · state class ใน HSM ไม่รู้จัก `UnityEngine`

**จุดเข้าออกสำคัญ**

- กติกาเปลี่ยน → แก้ใน `Domain/` ไฟล์เดียวต่อหนึ่งหัวข้อ GDD
- HSM สั่งอะไรเพิ่ม → แก้ที่ bridge **ห้ามแก้ state class**
- Yarn เพิ่มคำสั่ง → `Yarn/CocktailSystemManager.Yarn*.cs`

---

## 5. ⚠️ กับดักที่ต้องรู้ก่อนแตะโค้ด

| # | เรื่อง | ทำไม |
|---|---|---|
| 1 | **ห้ามเปลี่ยนชื่อคลาส `S_Drink` / `SO_CocktailList`** | มี asset อ้างอยู่ 27 ไฟล์ จะกลายเป็น Missing Script |
| 2 | **ห้ามสลับลำดับสมาชิก enum `Satisfaction`** | ค่า int ถูกเขียนลง Yarn `$satisfaction` และ `.yarn` เทียบเลขอยู่ |
| 3 | **Yarn instance command ผูกกับชื่อ GameObject** | `.yarn` เรียก `<<wait_for_task SystemGame>>` — component ที่ถือคำสั่งต้องอยู่บน object ชื่อ `SystemGame` เท่านั้น · นี่คือเหตุผลที่ Yarn adapter ยังเป็น `partial class` เดียวกัน ไม่แยกเป็น MonoBehaviour |
| 4 | **ย้ายไฟล์ต้องพา `.meta` ไปด้วยเสมอ** | ไม่งั้น GUID เปลี่ยน → scene/prefab พัง |
| 5 | **`CocktailShakerData.cs` เป็น shim ที่ยังลบไม่ได้** | ซีน `GamePlayScene`, `GamePlayScene 1` และ 2 prefab ยังใช้อยู่ · ลบได้เมื่อย้ายครบทั้ง 4 ที่ |
| 6 | **`MaxTolerance = 3` เป็นการตัดสินใจแล้ว** | design ยืนยัน 2026-08-21 · ถ้าจะปรับ แก้ที่ const เดียวใน `DrinkDeviation.cs` ห้าม hardcode ที่อื่น |
| 7 | **แต่ละซีนใช้ชุดสูตรคนละชุด** | `New Drag Drop System` = `Normal_Cocktail` (26 สูตร) · `GamePlayScene` ยังเป็น `Demo_Normal_Cocktail` (6 สูตร) |
| 8 | **missing script 2 ตัวใน `Dialogue System .../Current Text Line`** | มีมาก่อนงานนี้ ไม่เกี่ยวกับ Cocktail System **อย่าไปแก้แล้วนับเป็นผลงาน** |

---

## 6. การตัดสินใจที่ปิดแล้ว — ห้ามรื้อโดยไม่คุยกับเจ้าของโปรเจกต์

ทั้งหมดมีเหตุผลบันทึกไว้ใน `Refactor_Plan.md` §13 Decision Log

| # | เรื่อง | ผล |
|---|---|---|
| D1 | คลังสูตรพิเศษ | เขียน `CompositeDrinkRepository` รวม normal + special |
| D2 | `wait_for_task` | อยู่ต่อ · เปลี่ยนเงื่อนไขภายในเป็น `Order.IsScored` |
| D3 | `IsWaitingForTask` static | คงไว้ก่อน · แยกเป็น task ต่างหากภายหลัง |
| D4 | `Method.Build` | เลื่อน — รอ Building minigame |
| D5 | ตารางลุคแก้ว | เป็น ScriptableObject (`GlassVisualTable.asset`) |
| **D6** | โครงสร้างวัตถุดิบ | **คง 3 ลิสต์แยกตามหมวด** (ไม่ใช้ flat model แบบ GDD เดิม) — เคยตัดสินสลับกันมาก่อนหนึ่งรอบ **อย่าพลิกกลับ** |
| D7 | `MaxTolerance` | **3** ยืนยันแล้ว |
| D8 | `relationshipValue` | **ไม่ mirror ลง SO** — อ่านจาก Yarn `$rel_<id>` แหล่งเดียว |
| **S3** | เกณฑ์แอลกอฮอล์ | **ยึด GDD** `1..5 → Low`, `>=6 → High` — เคยตัดสินสลับกันมาก่อนหนึ่งรอบ **อย่าพลิกกลับ** |

GDD ถูกแก้ตามการตัดสินใจ D6 (§15.1/§16), D8 (§19.1) และเพิ่ม §21.0 (กติกา `NotFix`) แล้ว

---

## 7. เสร็จแล้ว

### 7.1 บั๊กที่แก้ 9 ตัว

`B1` `$type_of_cocktail` เขียน 0 เสมอ · `B2` สั่งเครื่องดื่มได้คนละแก้วระหว่างชื่อกับคำอธิบาย ·
`B3` `FindFirstObjectByType` ทุกครั้งที่สั่ง · `B4` ลูปเปิด/ปิดปุ่ม 3 ก๊อปที่แตะ component คนละชุด ·
`B5` predicate ที่มี side effect ถูกโพลทุกเฟรม · `B6` เพดาน 10 หน่วยทะลุได้ ·
`B7` debug overlay พิมพ์ชื่อ object แทนข้อมูล · `B8` field เขียนคนละไฟล์กับที่ประกาศ ·
`B11` NullReferenceException จาก repository ที่เป็น null

### 7.2 ช่องว่าง GDD ที่ปิดแล้ว 10 ข้อ

| # | เรื่อง |
|---|---|
| **S1** | สูตร deviation — เดิม *นับจำนวนชนิดที่ต่าง* GDD §17.1 ต้องการ *ผลรวมส่วนต่าง* `Σ\|r−p\|` |
| S2 | เกณฑ์ tolerance เป็น const เดียว |
| S3 | เกณฑ์แอลกอฮอล์ตาม §15.2 |
| **S4** | เทียบ `servedType` กับ `orderedType` — ทำให้ `Fail (a)` เกิดได้เป็นครั้งแรก |
| S5 | ราคาตาม §18.1 (`×1.5 / ×1.0 / ×0.5 / 50`) |
| S7 | Fail(b) คำนวณประเภทจากแก้วจริง |
| S9 | เพิ่มโหมดสั่งที่ 5 (`order_by_type`) |
| S10 | การสุ่มตาม §19.2 (รวมผู้สมัครก่อนแล้วสุ่ม uniform) |
| S11 | `SO_Customer` / `SO_CustomerRoster` |
| S12 | validator ยืนยันว่าทั้ง 26 สูตรผ่านอยู่แล้ว |

**S1 คือเรื่องใหญ่ที่สุด** — สูตร `Gin 7, Vodka 3` เท `Gin 1, Vodka 9` เดิมได้ 2 (Acceptable)
ตอนนี้ได้ 12 (Fail) · เพราะทุกสูตรมีวัตถุดิบแค่ 3–4 ชนิด ค่าเดิมแทบไม่มีทางเกิน 3–4
**เกณฑ์ Fail เกือบไม่เคยทำงานมาตลอด**

### 7.3 ซีน `New Drag Drop System` เซ็ตครบแล้ว

- ใช้ `Normal_Cocktail` (26 สูตร)
- component ใหม่ 5 ตัวบน `SystemGame/CocktailSystem/CocktailShaker` ผูกค่าครบ
- **`CocktailShakerData` shim ถูกลบออกจากซีนนี้แล้ว** — `CocktailSystemManager` ใช้ฟิลด์
  `_shakerContents` + `_ingredientButtons` แทน
- ซ่อม UnityEvent ที่หลุดตอนลบ shim 15 จุด
- `BarSetupBridge` + `CocktailFlowBridge` อยู่ใน `[GameLoop]` ผูกครบ
- `GlassVisualTable.asset` ครบ 7 entry · สูตรทั้ง 26 มี `CompatibleGlass` แล้ว

---

## 8. ยังไม่เสร็จ — เรียงตามความสำคัญ

### 8.1 🔴 ทำก่อน — ของที่ค้างอยู่จริงในซีน

**3 UnityEvent binding ที่ target หลุด** (ไม่มีใครเดาแทนได้)

| ที่ | binding | ต้องตัดสินใจอะไร |
|---|---|---|
| `SystemGame/MiniGameSystem` ×2 | `CocktailShaker.set_Interactable` | `CocktailShaker` ไม่มีในซีนนี้แล้ว · ตัวแทนน่าจะเป็น `DragableObject.Interactable` บน `CocktailShaker` GameObject — แต่ต้องยืนยันว่าตั้งใจล็อก shaker ตอนเล่น minigame |
| `Panel - Serve/BTN_Serving` | `GameObject.SetActive` | GameObject ที่เคยชี้ไปถูกลบ ไม่มีทางรู้ว่าตัวไหน |

**Roster ขาด 2 คน** — `Demo_CustomerRoster` มีแค่ `Isla`, `Owen`, `Walter`
`Cole` กับ `Freya` จะเข้า path `"No preferences for X; using every type"`
· ค่าใน roster ยังต่างจาก `CharacterData` เดิมด้วย (ตารางเทียบใน `Manual_Setup.md` §6.1)
· **ค่าความชอบเป็น design data — อย่าเดาเอง**

### 8.2 🟡 ช่องว่าง GDD ที่ยังเปิด

| # | GDD | ติดอะไร |
|---|---|---|
| **S6** | §17.3 Fail(b) ต้องสุ่มชื่อจากคลังที่ designer เขียน | ยังไม่มีคลังชื่อ · ตอนนี้ใช้ `"???"` (`DrinkBuilder.UnmatchedName`) |
| **S8** | §21.1 Fail(b) ต้อง `BlendIngredientColors(poured)` | **ไม่มีข้อมูลสีต่อวัตถุดิบใน data model เลย** · ตอนนี้ใช้สีน้ำตาลขุ่นแทนสีดำ |
| **S13** | §10/§21 ผู้เล่นเลือกแก้วเอง | **กติกาทำแล้ว** — สูตรที่ตั้ง `CompatibleGlass = NotFix` จะไม่ทับแก้วที่ผู้เล่นเลือก · เหลือแค่ UI ให้กดเลือกแล้วเรียก `ShakerContents.SetGlass(glass)` |
| S14 | §16 `MixMethod.Build` | เลื่อนตาม D4 |
| S15 | §16 `unlockedByDefault` | เลื่อนตาม D1 |

ทั้งหมดมี `TODO(design, ...)` กำกับไว้ในโค้ดตรงจุดที่เกี่ยวข้อง

### 8.3 🟡 งานมือที่เหลือ (`Manual_Setup.md`)

- ย้ายอีก 4 ที่ออกจาก shim: `GamePlayScene`, `GamePlayScene 1`, `SystemGame.prefab`, `CocktailSystem.prefab`
- ลบ missing script `BookUI` ใน 4 ที่นั้น
- ไล่ตั้ง `CompatibleGlass` ให้ตรงเครื่องดื่มจริง (ตอนนี้ 23/26 เป็น `Hi_ball` ชั่วคราว)
- art วาด sprite จริงของ `Cocktail` / `LongDrink` / `NotFix` (ตอนนี้ยืมของ `Hi_ball`)

### 8.4 🟢 งานถัดไปที่เป็นก้อนใหญ่

**`MinigameFlowBridge` ยังไม่ได้เขียน** — สเปกอยู่ใน `Bar410_Minigame_Integration_Plan.md` §3.3
`CocktailFlowBridge` **จงใจไม่แตะ minigame** เพื่อไม่ให้มีเจ้าของสองคน
`ShakerContents.RequiredMinigame` เตรียมไว้ให้ bridge นั้นเรียกแล้ว

---

## 9. ❓ คำถามค้างที่ควรถาม design

**GDD §18 วัด deviation เทียบกับสูตรไหน?**

§17 คำนวณ best-match ทั้งฐานข้อมูลเพื่อกำหนด *ตัวตน* ของเครื่องดื่ม (ชื่อ สี ราคา)
ส่วน §18 พูดถึงแค่คำว่า "deviation" เฉย ๆ

อ่านตามตัวอักษร: ชง Negroni ได้เป๊ะทั้งที่ลูกค้าสั่ง Martini → best-match deviation = 0 →
เคส 1 → **Perfect** ซึ่งไม่น่าใช่เจตนา

**โค้ดเลือกวัดเทียบสูตรที่ลูกค้าสั่ง** (`DrinkDeviation.MatchAgainst`) ส่วน best-match ใช้กำหนด
ตัวตนอย่างเดียว — ตรงกับที่โค้ดเดิมทำ · เขียน `NOTE(design)` กำกับไว้ที่เมธอดแล้ว

---

## 10. วิธีตรวจสอบงาน

โปรเจกต์นี้ต่อกับ **Unity MCP** อยู่ ใช้ตรวจได้จริงไม่ต้องเดา

**คอมไพล์**

```
mcp__unityMCP__refresh_unity(mode=force, scope=all, compile=request, wait_for_ready=true)
mcp__unityMCP__read_console(action=get, types=["Error"])
```

**ตรวจข้อมูลสูตร** — เมนู `Bar410 > Validate Cocktail Data`
(ไล่ทุก `S_Drink` + `SO_GlassVisualTable` แล้วรายงานว่าอะไรขาด · ตอนนี้ขึ้น "validation passed")

**ทดสอบกติกาจริง** — `mcp__unityMCP__execute_code` รันได้เลย ตัวอย่างที่ใช้ยืนยัน GDD:

```csharp
// GDD §17.1 ตัวอย่างในสเปก: Gin7 Vodka3 vs Gin7 Vodka2 Syrup1 = 2
DrinkDeviation.Compute(poured, recipe);
// GDD §15.2: 5 -> LowAlcohol, 6 -> HighAlcohol
AlcoholClassifier.FromUnits(5);
// GDD §18 + §18.1 ครบวงผ่าน context
new DrinkScoringService(repo).Score(ctx, served);   // -> Satisfaction + ctx.Payout + ctx.RelationshipDelta
```

ผลที่ควรได้ (ยืนยันแล้ว):

```
GDD 17.1 expect 2 -> 2            S1 expect 12 -> 12
15.2  5->LowAlcohol  6->HighAlcohol
ctx case1 Perfect    pay=150 rel=0.5
ctx case2 Acceptable pay=100 rel=0.25
ctx case3 Acceptable pay=100 rel=0.25
ctx case5 Fail       pay=50  rel=0
B6 add 5 onto 9 -> allowed=False total=9
recipe=NotFix player=Rocks -> Rocks   (ผู้เล่นเลือกเอง คงไว้)
```

---

## 11. ตัวเลขสรุป

| | ก่อน | หลัง |
|---|---:|---:|
| ตระกูลเมธอดเขียนซ้ำ 3 ก๊อป | 9 เมธอด | **3 generic** |
| ลูป TryGetComponent เปิด/ปิด interactable | 3 ก๊อป | **1** |
| สแกนสูตรต่อการอัปเดต shaker 1 ครั้ง | 5 | **1** |
| จุด hardcode `errors <= 2` | 6 | **1** |
| ไฟล์ยาวสุด | 412 | **~240** |
| กติกา GDD ที่ไม่ตรง | 15 | **5** |
| จำนวนไฟล์ / บรรทัด | 17 / 2,104 | 40+ / ~3,300 |

⚠️ **บรรทัดเพิ่มขึ้น ไม่ได้ลดลง** — เพราะเพิ่มกติกา GDD ที่โค้ดไม่เคยมี (`PricingRules`,
`SatisfactionEvaluator` เคส 3/4, `DrinkOrderContext`), shim ยังอยู่รอย้ายอีก 4 ที่
และคอมเมนต์อธิบายเหตุผลทุกจุดที่ต่างจาก GDD **อย่าเอาตัวเลขนี้ไปตีความว่างานล้มเหลว**
รายละเอียดใน `Refactor_Report.md` §7
