# Bar410 — Cocktail System: เอกสารส่งต่องาน / HANDOFF

**Date เริ่มต้น:** 2026-08-21 · **อัปเดตล่าสุด:** 2026-08-23 · **Branch:** `GameLoop/main` · **HEAD:** `b505b53` (push แล้วหรือยังไม่แน่ใจ เช็ค `git status` ก่อน)
**สถานะ:** โค้ดคอมไพล์ผ่านไม่มี error · `Bar410 > Validate Cocktail Data` ผ่าน · **working tree มีของค้างไม่ได้ commit** (ดู §0 — งาน "ผสมขวด/tray" ของ `Mixer-LemonJuice (1)` รอบใหม่ล่าสุดหลัง `b505b53`)

> อ่านไฟล์นี้ก่อนไฟล์อื่นทั้งหมด แล้วค่อยเจาะรายละเอียดจากเอกสารที่อ้างถึงใน §2
> 🆕 **ถ้ามาต่องาน "Glass Freedom" (ผู้เล่นเลือกแก้วเสิร์ฟเอง + ลาก-วางวัตถุดิบ) โดยเฉพาะ อ่าน §1b ก่อน**

---

## 0. ⚠️ สิ่งที่ต้องรู้ก่อนอื่นใด — สองซีน ไม่ใช่ซีนเดียว

งานตอนนี้กระจายอยู่ **2 ซีน** ที่ทำงานคนละช่วงเวลากัน อย่าสับสน:

| ซีน | สถานะ Git | ใช้ทำอะไร |
|---|---|---|
| `Assets/[05]Scenes/Deverlopment/New Drag Drop System.unity` | tracked, commit แล้ว | ซีนของรอบ refactor เดิม (§1) — ของค้างเดิมทั้งหมดใน §8.1/§8.3 อยู่ที่ซีนนี้ |
| `Assets/[05]Scenes/Deverlopment/New Cocktail System.unity` | **ยังไม่ track ใน git** (`git status` ขึ้น `??` ตลอด) | ซีนทดสอบแยกของผู้ใช้เอง ใช้พัฒนา/ทดสอบฟีเจอร์ **Glass Freedom** (§1b) ทั้งหมด — `GameFlowHooks`/`GlassPlacementZone`/`BottleIngredientSource` ที่เทสแล้วอยู่ในซีนนี้ |

**นัยสำคัญ:** ถ้า clone repo ใหม่หรือเช็คเอาต์ branch ใหม่ ซีน `New Cocktail System.unity` **จะหายไปเลยถ้าไม่ได้ถูก commit ไว้ก่อน** — ก่อนทำงานต่อบนเครื่องอื่น ให้เช็คก่อนว่าไฟล์นี้ยังอยู่ในเครื่อง หรือถามเจ้าของโปรเจกต์ว่าตั้งใจ commit มันหรือยัง

**ของค้างไม่ได้ commit ตอนนี้** (โค้ดแก้ไปแล้วแต่ยังไม่ลง git หลัง `b505b53`):
- `DragableObject.cs` — เพิ่ม hook `OnThresholdCrossed` (protected virtual, default no-op) +
  เมธอด public `BeginRedirectedDrag`/`FinishRedirectedDrag` ให้ subclass ใช้ยกมือ (hand off) การลาก
  ไปให้ object อื่นได้ ไม่กระทบพฤติกรรมเดิมของ object ไหนที่ไม่ override
- `DragableFruitTraySlot.cs` (ใหม่) — `: DragableObject` สำหรับวัตถุดิบที่เป็น**ทั้งของกดเทน้ำ
  (`Interactable_2_5DObject`/`IngredientButtonUI`) และ tray ผลไม้ในตัวเดียวกัน** (ตอนนี้มีแค่
  `Mixer-LemonJuice (1)`) — ดู §5 กับดักใหม่ #14/#15 และ §6 D13
- `InteractableToggle.cs` — เพิ่ม `ApplyOnlyFruitTraySlot` มองหา `DragableFruitTraySlot` ผูกเข้า
  `ApplyPrepareBarPhase`(false)/`ApplyPrepareDrinksPhase`(true)
- Scene `New Cocktail System.unity`: `Mixer-LemonJuice (1)` เปลี่ยนจาก `DragableObject`+`FruitTraySlot`
  (สองคอมโพเนนต์แยก) เป็น `DragableFruitTraySlot` ตัวเดียว, ลบ null-reference ที่ค้างใน
  `FruitTrayGroup._members` จากการสลับคอมโพเนนต์นี้ด้วย

---

## 1. งานนี้คืออะไร (ต้นฉบับ — Phase 0–8, ปิดแล้ว)

Refactor `Assets/[02]Script/Cocktail System/` ทั้งระบบ เพื่อ 4 เป้าหมาย:

1. กระชับโค้ด ลบโค้ดตาย ยุบโค้ดซ้ำ
2. แยกหน้าที่ตาม SOLID
3. **ทำให้ตรงกับ GDD** — พบว่ากติกาที่โค้ดใช้ไม่ตรงสเปก 15 จุด
4. เตรียมเชื่อมกับ Hierarchical State Machine (HSM) ที่มีอยู่แล้ว

ทำครบ Phase 0–8 ฝั่งโค้ด และเซ็ตซีน `New Drag Drop System` ให้ใช้สถาปัตยกรรมใหม่เต็มตัว

---

## 1b. 🆕 งานต่อยอด: "Glass Freedom" (2026-08-22)

เจ้าของโปรเจกต์ตัดสินใจให้ **ผู้เล่นเลือกแก้วเสิร์ฟเองทั้งหมด** แทนระบบ `CompatibleGlass`/
`GlassType.NotFix` เดิม (ที่ §6 D5 และ §8.2 S13 ของเดิมพูดถึง) — **ฟิลด์ `CompatibleGlass` ถูก
ลบออกจาก `S_Drink` จริง ไม่ใช่แค่เลิกใช้** ทุกจุดในเอกสารเก่าที่พูดถึงมันตอนนี้ล้าสมัยแล้ว
(มีคำเตือนกำกับไว้ในแต่ละไฟล์แล้ว)

ระบบใหม่แบ่งเป็น 2 track อิสระต่อกัน ที่มาบรรจบกันตอน "เท" ใน Garnish state:

- **Track A — เลือกแก้ว**: ลากแก้ว (`SO_GlassOption` — sprite + garnish look มัดรวมกัน) จากชั้นวาง
  ไปวางบนโต๊ะ ทำเมื่อไหร่ก็ได้ (ตอนใส่วัตถุดิบ หรือตอน Garnish) มีแก้วได้แค่ 1 ใบทั้งซีน
  (`GlassPlacementZone._occupant` เป็น `static` โดยตั้งใจ) ลากใบใหม่มา = แทนที่ใบเก่า (ไม่ใช่ปฏิเสธ)
- **Track B — ใส่วัตถุดิบ**: ลากขวด/ชิ้นผลไม้เข้าภาชนะชง ตรวจจับด้วย raycast ธรรมดา
  (`IngredientHoverDetector`) ไม่ใช่ placement zone — ลองใช้ zone มาก่อนแล้วมีปัญหาสั่น/jitter
  (ดู §5 กับดักใหม่)

**สถานะจริง ณ ตอนนี้ (อัปเดต 2026-08-23):**

| ส่วน | สถานะ |
|---|---|
| Track A (เลือกแก้ว) — หยิบ/วาง/สลับแก้ว | ✅ **เทสแล้ว ใช้งานได้จริง** |
| Track B — หมวด **ขวด** (`BottleIngredientSource`) | 🟡 **เทสแล้ว ใช้งานได้จริง แต่ติดแค่ `Alchohol-Vodka`** — อีก 10 ขวดที่เหลือ (Gin/Rum/Whiskey/Tequila/TripleSec/SweetVermouth/DryVermouth/Campari/Soda/Syrup) ยังไม่มี `BottleIngredientSource` |
| Track B — หมวด **Fruit** (`FruitTraySlot`/`FruitPieceInstance`) | ✅ **เทสแล้ว ใช้งานได้จริง ครบทั้ง 6 ชนิด** — sprite ยังเป็น placeholder (ยืมสไปรต์ขวดเดิมมาใช้ ไม่ใช่ art จริง) |
| วัตถุดิบที่กดเทได้ **และ** ดึงผลไม้ออกได้ในตัวเดียวกัน (`Mixer-LemonJuice (1)`) | ✅ **เทสแล้ว ใช้งานได้จริง** ผ่าน `DragableFruitTraySlot` (ดู §5 #14/#15, §6 D13) |
| การเทจากภาชนะชงลงแก้ว (`GarnishFlowBridge`) | 🟡 ผูกในซีนแล้ว ยังไม่มีปุ่ม UI เรียก `TryFinishGarnish()` — ดู §8.2 |
| Phase-gated interactable (`EnableInteractablePrepareBarPhase`/`Drinks`) | ✅ ผูกแล้วใน `GameFlowHooks` (ซีน `New Cocktail System`), เทสแล้ว |

**เอกสารของฟีเจอร์นี้โดยเฉพาะ:** `Docs/Bar410_GlassFreedom_ManualSetup.md` · แผนต้นฉบับอยู่ใน plan
file `robust-watching-lark` (อนุมัติและทำครบ 6 ขั้นแล้ว) · GDD อัปเดตแล้วที่ §21/§21.0

**สิ่งที่ต้องทำต่อ เรียงตามลำดับ:**
1. สร้างปุ่ม/panel UI "ตกแต่งเสร็จแล้ว" ผูกเข้า `GarnishFlowBridge.TryFinishGarnish()` (ไม่ใช่
   `GameFlowCommands.GarnishDone()` ตรงๆ) — ตอนนี้เทแล้วไม่มีทางกดจบไป Serve ได้เลย
2. เพิ่ม `BottleIngredientSource` ให้ขวดที่เหลืออีก 10 อัน + ปรับ Hover Offset ทีละอัน (§3.5 ของ
   `Bar410_GlassFreedom_ManualSetup.md`)
3. Art จริงของหมวด Fruit (sprite ชิ้นผลไม้แทนของที่ยืมมาจากขวด) + ตัดสินใจ `E_GarnishLook`/กลไก
   ตกแต่งแก้วจริง — ยังเป็น placeholder ทั้งคู่
4. ทดสอบวงจรเต็ม: สั่งเครื่องดื่ม → ใส่วัตถุดิบ (ขวด+ผลไม้ผสมกัน) → มินิเกม → วางแก้ว → เท → กด
   ตกแต่งเสร็จ (รอข้อ 1) → เสิร์ฟ
5. **commit งานค้างที่ระบุใน §0** เข้า git

---

## 2. แผนที่เอกสาร — เปิดอันไหนเมื่อไหร่

| ไฟล์ | ใช้เมื่อ |
|---|---|
| **`Bar410_CocktailSystem_HANDOFF.md`** | ไฟล์นี้ — ภาพรวมและสถานะปัจจุบัน |
| **`Bar410_GlassFreedom_ManualSetup.md`** | 🆕 **งานที่เหลือของฟีเจอร์ Glass Freedom ทั้งหมด** — ใช้บ่อยสุดตอนนี้ |
| `Bar410_CocktailSystem_Architecture.md` | **ไฟล์ไหนทำอะไร · จะแก้อะไรต้องเปิดไฟล์ไหน** (อัปเดตแล้ว มี §4b/§4c ของ Glass/Ingredients) |
| `Bar410_CocktailSystem_Manual_Setup.md` | งานมือรอบ refactor เดิมที่ยังไม่ปิด (§4/§4.1 เรื่องแก้วถูก mark ว่าเลิกใช้แล้ว — พับเก็บไว้เป็นประวัติ) |
| `Bar410_CocktailSystem_Refactor_Plan.md` / `_Report.md` / `_Summary.md` | เหตุผล/รายละเอียด/สรุปของรอบ refactor เดิม — **ทุกจุดที่พูดถึงแก้ว (`CompatibleGlass`/`NotFix`/`SO_GlassVisualTable`/S13) เป็นบันทึกประวัติ ณ 2026-08-21 เท่านั้น มีคำเตือนกำกับหัวไฟล์แล้ว อย่าเชื่อว่าเป็นสถานะปัจจุบัน** |
| `GDD_Bar410_Master.md` | **source of truth ด้านกติกาเกม** — §21/§21.0 อัปเดตแล้วตาม Glass Freedom |
| `Bar410_StateMachine_Implementation.md` | โครง HSM ที่มีอยู่ก่อน |
| `Bar410_Minigame_Integration_Plan.md` / `_Report.md` / `_Summary.md` | งานเชื่อม minigame — ทำแล้ว ไม่เกี่ยวกับ Glass Freedom |

---

## 3. ประวัติ commit

| Commit | เนื้อหา |
|---|---|
| `99f9cef` | เคลียร์ของค้าง — ลบ `BookUI.cs` / `GameLoopManager.cs`, ย้าย `NPC_Base.cs` |
| `9f91ed8` | Phase 0–2 + §4.7 — สร้าง `Domain/` layer และทำให้ตรง GDD |
| `3ac5b1b` | Phase 3–8 — ผ่า shaker, `DrinkOrderContext`, HSM bridges, ย้ายไฟล์, validator |
| `663c15f` | เอกสาร — Architecture reference |
| `a88c3f8` | เซ็ตซีน + ข้อมูลลูกค้า + อัปเดต GDD |
| `87ab9a5` | เอกสารส่งต่องานฉบับแรก |
| `86a7fd8` | ขับ minigame ผ่าน game loop, จบงาน migrate cocktail |
| `44664af` | อัปเดตซีน `New Drag Drop System.unity` |
| `bbdaccd` | **Glass Freedom** — ลบ `CompatibleGlass`, เพิ่ม `Cocktail/Glass/` + `Cocktail/Ingredients/`, `GarnishFlowBridge`, `GameFlowDebugHotkeys` (55 ไฟล์, +1763/−127) |
| *(ยังไม่ commit)* | Bug fix ตาม §0 + เอกสารรอบนี้ |

---

## 4. สถาปัตยกรรมโดยย่อ

```
Flow bridges ──── Hierarchical State Machine/   BarSetupBridge · CocktailFlowBridge · GarnishFlowBridge (ใหม่)
Yarn adapters ─── Cocktail System/Yarn/         4 ไฟล์ (partial class เดียวกัน)
Session ───────── Cocktail System/Session/      DrinkOrderContext · OrderService · Scoring
Scene runtime ─── Cocktail System/Cocktail/Shaker/   ShakerContents · VisualPresenter · ...
Serving glass ─── Cocktail System/Cocktail/Glass/       (ใหม่) SO_GlassOption · GlassPlacementZone · ...
Ingredient drag ─ Cocktail System/Cocktail/Ingredients/ (ใหม่) BottleIngredientSource · FruitTraySlot · ...
Domain ────────── Cocktail System/Cocktail/Domain/   กติกาเกมทั้งหมด (static, ไม่มี Unity)
```

**กฎเดียว:** ลูกศรชี้ลงเสมอ · `Domain/` ไม่รู้จัก `MonoBehaviour` · state class ใน HSM ไม่รู้จัก `UnityEngine`

**จุดเข้าออกสำคัญ**

- กติกาเปลี่ยน → แก้ใน `Domain/` ไฟล์เดียวต่อหนึ่งหัวข้อ GDD
- HSM สั่งอะไรเพิ่ม → แก้ที่ bridge **ห้ามแก้ state class**
- Yarn เพิ่มคำสั่ง → `Yarn/CocktailSystemManager.Yarn*.cs`
- แก้วเปลี่ยน → `Cocktail/Glass/` เท่านั้น ไม่มีจุดไหนใน `Domain/`/`S_Drink` รู้จักแก้วอีกแล้ว

---

## 5. ⚠️ กับดักที่ต้องรู้ก่อนแตะโค้ด

| # | เรื่อง | ทำไม |
|---|---|---|
| 1 | **ห้ามเปลี่ยนชื่อคลาส `S_Drink` / `SO_CocktailList`** | มี asset อ้างอยู่ 27 ไฟล์ จะกลายเป็น Missing Script |
| 2 | **ห้ามสลับลำดับสมาชิก enum `Satisfaction`** | ค่า int ถูกเขียนลง Yarn `$satisfaction` และ `.yarn` เทียบเลขอยู่ |
| 3 | **Yarn instance command ผูกกับชื่อ GameObject** | `.yarn` เรียก `<<wait_for_task SystemGame>>` — component ที่ถือคำสั่งต้องอยู่บน object ชื่อ `SystemGame` เท่านั้น |
| 4 | **ย้ายไฟล์ต้องพา `.meta` ไปด้วยเสมอ** | ไม่งั้น GUID เปลี่ยน → scene/prefab พัง |
| 5 | **`CocktailShakerData.cs` เป็น shim ที่ยังลบไม่ได้** | ซีน `GamePlayScene`, `GamePlayScene 1` และ 2 prefab ยังใช้อยู่ · ยังอ้าง `SO_GlassVisualTable`/`GlassVisual` เดิมอยู่ — **ห้ามลบ 2 คลาสนั้นทิ้ง** จนกว่าจะย้าย shim ครบ |
| 6 | **`MaxTolerance = 3` เป็นการตัดสินใจแล้ว** | แก้ที่ const เดียวใน `DrinkDeviation.cs` ห้าม hardcode ที่อื่น |
| 7 | **แต่ละซีนใช้ชุดสูตรคนละชุด** | `New Drag Drop System` = `Normal_Cocktail` (26 สูตร) · `GamePlayScene` ยังเป็น `Demo_Normal_Cocktail` (6 สูตร) |
| 8 | **missing script 2 ตัวใน `Dialogue System .../Current Text Line`** | มีมาก่อนงานนี้ ไม่เกี่ยวข้อง **อย่าไปแก้แล้วนับเป็นผลงาน** |
| 9 🆕 | **`S_Drink` ไม่มีฟิลด์ `CompatibleGlass` แล้ว** | ลบออกจริง 2026-08-22 — เอกสารเก่า (Refactor_Plan/Report/Summary, Manual_Setup §4) ยังพูดถึงมันเป็นสถานะปัจจุบัน **อย่าเชื่อ** มีคำเตือนกำกับไว้ในแต่ละไฟล์แล้ว |
| 10 🆕 | **`GlassPlacementZone._occupant` เป็น `static` โดยตั้งใจ** | ต้องการแก้วแค่ 1 ใบทั้งซีน ไม่ใช่ 1 ใบต่อโซน — ยืนยันจากผู้ใช้แล้ว ไม่ใช่บั๊ก |
| 11 🆕 | **`DragableObject.Interactable` ต้องเป็น `true` ทั้งช่วง Prepare และ AddIngredient สำหรับขวด** | มันไม่ใช่สวิตช์ bar-layout อย่างเดียว — เป็น input listener จริงที่ `BottleIngredientSource` เกาะอยู่ (`OnPointerDown`/`OnDrag` เช็คค่านี้) ปิดมันตอน AddIngredient จะทำให้ลากเทไม่ได้เลย ไม่ใช่แค่ล็อกการย้าย |
| 12 🆕 | **อย่าใช้ `PlacementZoneBase` ตรวจจับ "ลากทับภาชนะชง"** | เคยลองแล้ว (`IngredientDropTarget`) แต่ระบบ clamp ตำแหน่งทุกเฟรมทำให้ของสั่น/jitter — ใช้ raycast ตรงผ่าน `IngredientHoverDetector` แทน |
| 13 🆕 | **layer `Ignore Raycast` ถูกใช้ชั่วคราวระหว่างลากขวด/ผลไม้** | `BottleIngredientSource`/`FruitPieceInstance` สลับ layer ตัวเองไปเป็น `Ignore Raycast` ระหว่างลากเพื่อกัน self-occlusion แล้วคืนค่าเดิมตอนปล่อย — **อย่าเอา layer นี้ไปใช้ gameplay logic อื่นในซีนนี้** |
| 14 🆕 | **`Interactable_2_5DObject` + `FruitTraySlot`/`DragableFruitTraySlot` บน object เดียวกัน = collider ชนกันแบบไม่ deterministic ถ้าไม่ใช้ `DragableFruitTraySlot`** | piece spawn ที่ตำแหน่งเดียวกับ collider ของปุ่มพอดี raycast จะสุ่มว่าโดนอันไหนตามความแม่นยำของ float ทุกเฟรม — วัตถุดิบที่ต้องกดเทได้ *และ* ดึงผลไม้ได้ในตัวเดียวกันต้องใช้ `DragableFruitTraySlot` เท่านั้น (ดู #15, D13) ห้ามใช้ `FruitTraySlot` (sibling ธรรมดา) คู่กับ `Interactable_2_5DObject` |
| 15 🆕 | **`OnDrag` ของ `DragableObject`/subclass ถูกเรียกทุกเฟรมที่ลากเกิน threshold ไม่ใช่แค่ครั้งเดียว** | ถ้า override `OnThresholdCrossed` แล้วไม่กันการเรียกซ้ำเอง (เช่นเช็ค `_activePiece != null` ก่อน) จะ spawn object ใหม่ทุกเฟรมตลอดที่ลากอยู่ (เจอจริงแล้วใน `DragableFruitTraySlot` รอบพัฒนานี้ ก่อนแก้ค้างไปสูงสุด 29 instance ตอนทดสอบ) — ฟีเจอร์ต่อไปที่ override hook นี้ต้องกันเคสนี้เองเสมอ เพราะ base class ไม่ได้กันให้ |

---

## 6. การตัดสินใจที่ปิดแล้ว — ห้ามรื้อโดยไม่คุยกับเจ้าของโปรเจกต์

ทั้งหมดมีเหตุผลบันทึกไว้ใน `Refactor_Plan.md` §13 Decision Log (ยกเว้นที่ทำเครื่องหมาย 🆕 ซึ่งมาจากงาน Glass Freedom)

| # | เรื่อง | ผล |
|---|---|---|
| D1 | คลังสูตรพิเศษ | เขียน `CompositeDrinkRepository` รวม normal + special |
| D2 | `wait_for_task` | อยู่ต่อ · เปลี่ยนเงื่อนไขภายในเป็น `Order.IsScored` |
| D3 | `IsWaitingForTask` static | คงไว้ก่อน · แยกเป็น task ต่างหากภายหลัง |
| D4 | `Method.Build` | เลื่อน — รอ Building minigame |
| ~~D5~~ | ~~ตารางลุคแก้วเป็น ScriptableObject~~ | **ถูกแทนที่ทั้งหมดโดย Glass Freedom (2026-08-22)** — ดู D9 |
| **D6** | โครงสร้างวัตถุดิบ | **คง 3 ลิสต์แยกตามหมวด** (ไม่ใช้ flat model แบบ GDD เดิม) — เคยตัดสินสลับกันมาก่อนหนึ่งรอบ **อย่าพลิกกลับ** |
| D7 | `MaxTolerance` | **3** ยืนยันแล้ว |
| D8 | `relationshipValue` | **ไม่ mirror ลง SO** — อ่านจาก Yarn `$rel_<id>` แหล่งเดียว |
| **S3** | เกณฑ์แอลกอฮอล์ | **ยึด GDD** `1..5 → Low`, `>=6 → High` — เคยตัดสินสลับกันมาก่อนหนึ่งรอบ **อย่าพลิกกลับ** |
| **D9** 🆕 | ผู้เล่นเลือกแก้วเสิร์ฟเอง | `CompatibleGlass`/`NotFix` **ลบออกจาก `S_Drink` ทั้งหมด** แทนด้วยระบบลาก-วาง (`SO_GlassOption` + `GlassPlacementZone`) ยืนยัน 2026-08-22 |
| **D10** 🆕 | จำนวนแก้วที่วางได้ | **แค่ 1 ใบทั้งซีน ไม่ใช่ 1 ใบต่อโซน** — `GlassPlacementZone._occupant` เป็น `static` โดยตั้งใจ ยืนยัน 2026-08-22 |
| **D11** 🆕 | Fruit vs Bottle | **เป็นแค่ interaction/visual ต่างกัน ไม่ใช่ ingredient category ใหม่** — ทั้งคู่ยังเป็น `Mixer` enum เดิม ยืนยัน 2026-08-22 |
| **D12** 🆕 | กลไกตรวจจับ "ลากทับภาชนะชง" | **raycast ตรง (`IngredientHoverDetector`) ไม่ใช่ `PlacementZoneBase`** — ลองแบบ zone มาก่อนแล้วมีปัญหาสั่น ยืนยัน 2026-08-22 |
| **D13** 🆕 | คลิก vs ลาก บน `Mixer-LemonJuice (1)` | **คลิก = เทน้ำผ่าน `Interactable_2_5DObject`/`IngredientButtonUI` เดิม ลาก = ดึงผลไม้ออกมาเสมอ ไม่มีทางลากทั้งขวดไปเทที่ shaker แบบ `BottleIngredientSource`** — ยืนยันแล้วว่าตั้งใจ ไม่ต้องมี `BottleIngredientSource` บน object นี้ พิจารณาสองทางก่อนตัดสินแบบนี้: (1) แยก collider ไม่ให้ทับกัน (ไม่ต้องเขียนโค้ด แต่พึ่งวินัยเรื่อง geometry) กับ (2) hijack การลากไปที่ piece ที่เพิ่ง spawn ผ่าน `DragableFruitTraySlot`/`OnThresholdCrossed` (เขียนโค้ดเพิ่ม แต่ geometry ขยับยังไงก็ไม่พัง) — เลือกทาง (2) ยืนยัน 2026-08-23 |

GDD ถูกแก้ตามการตัดสินใจ D6 (§15.1/§16), D8 (§19.1), D9/D10 (§21/§21.0 เขียนใหม่ทั้งหมด) แล้ว

---

## 7. เสร็จแล้ว

### 7.1 บั๊กที่แก้ 9 ตัว (รอบ refactor เดิม)

`B1` `$type_of_cocktail` เขียน 0 เสมอ · `B2` สั่งเครื่องดื่มได้คนละแก้วระหว่างชื่อกับคำอธิบาย ·
`B3` `FindFirstObjectByType` ทุกครั้งที่สั่ง · `B4` ลูปเปิด/ปิดปุ่ม 3 ก๊อปที่แตะ component คนละชุด ·
`B5` predicate ที่มี side effect ถูกโพลทุกเฟรม · `B6` เพดาน 10 หน่วยทะลุได้ ·
`B7` debug overlay พิมพ์ชื่อ object แทนข้อมูล · `B8` field เขียนคนละไฟล์กับที่ประกาศ ·
`B11` NullReferenceException จาก repository ที่เป็น null

### 7.2 ช่องว่าง GDD ที่ปิดแล้ว 10 ข้อ (รอบ refactor เดิม)

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
| **S13** | ผู้เล่นเลือกแก้วเอง — **ปิดจริงแล้ว 2026-08-22 ด้วย Glass Freedom** (ไม่ใช่ทางที่เคยระบุไว้แต่แรก) |

**S1 คือเรื่องใหญ่ที่สุดของรอบ refactor เดิม** — สูตร `Gin 7, Vodka 3` เท `Gin 1, Vodka 9` เดิมได้ 2
(Acceptable) ตอนนี้ได้ 12 (Fail) **เกณฑ์ Fail เกือบไม่เคยทำงานมาตลอด**

### 7.3 ซีน `New Drag Drop System` เซ็ตครบแล้ว (รอบ refactor เดิม)

- ใช้ `Normal_Cocktail` (26 สูตร) · component ใหม่ 5 ตัวบน `SystemGame/CocktailSystem/CocktailShaker` ผูกค่าครบ
- **`CocktailShakerData` shim ถูกลบออกจากซีนนี้แล้ว**
- `BarSetupBridge` + `CocktailFlowBridge` อยู่ใน `[GameLoop]` ผูกครบ

### 7.4 🆕 Glass Freedom — Track A + Track B (ขวด) เทสแล้ว ใช้งานได้จริง

ดู §1b สำหรับรายละเอียดเต็ม สรุป: เลือก/สลับแก้วเสิร์ฟ + ลากขวดเทวัตถุดิบ ทำงานถูกต้องในซีน
`New Cocktail System.unity` (คนละซีนกับ §7.3) รวมถึงแก้บั๊กสั่น (self-occlusion) และระบบ
phase-gated interactable (`EnableInteractablePrepareBarPhase`/`PrepareDrinksPhase`) แล้ว

### 7.5 🆕 Glass Freedom — หมวด Fruit ครบทั้ง 6 ชนิด + วัตถุดิบสองบทบาทในตัวเดียว (2026-08-23)

- Tray ครบ 6 ชนิด (`Cranberry`/`Lemon`/`Lime`/`Grapefruit`/`PepperMint`/`Orange`) แต่ละชนิดมี
  piece prefab แยกของตัวเอง (`FruitPiece_<Type>.prefab`) sprite ยังยืมของขวดเดิมมาใช้เป็น
  placeholder — ไม่ใช่ art จริง
- `FruitTrayGroup` — spawn/despawn piece ของ 5 tray สะอาดพร้อมกันตอนเข้า/ออก AddIngredient
  (ผูกกับ `GameFlowHooks.AddIngredient.OnEnter/OnExit`) ไม่มี piece ค้างอยู่นอกช่วงนี้เลย
- `Mixer-LemonJuice (1)` เป็นวัตถุดิบที่ **กดเทน้ำได้ปกติ และลากดึงผลไม้ออกมาได้ในตัวเดียวกัน** —
  ใช้ `DragableFruitTraySlot` (ดู §5 #14/#15, §6 D13) แทนที่ `DragableObject`+`FruitTraySlot`
  แยกกันแบบเดิม
- `CocktailFlowBridge._glassZone` และ `GarnishFlowBridge` (ใหม่บน `[GameLoop]`) ผูกครบกับ
  `GlassPlacementZone` (object ชื่อ `Plane`) แล้ว — ปิดช่องว่างเดิมจาก §8.2 ของรอบก่อน (ยกเว้นปุ่ม
  UI ที่ยังไม่มี ดู §8.2 ใหม่)

---

## 8. ยังไม่เสร็จ — เรียงตามความสำคัญ

### 8.1 🔴 Glass Freedom — ไม่มีทางจบ Garnish state (สำคัญสุดตอนนี้)

`GarnishFlowBridge` ผูกในซีนครบแล้ว (ดู §7.5) แต่**ยังไม่มีปุ่ม/panel UI ไหนเรียก
`GarnishFlowBridge.TryFinishGarnish()` เลย** — เทเสร็จแล้วก็ค้าง ไปต่อ Serve ไม่ได้ ต้องสร้าง UI
ใหม่ (ดู `Bar410_GlassFreedom_ManualSetup.md` §5.2 — **ห้ามผูกเข้า `GameFlowCommands.GarnishDone()`
ตรงๆ** เพราะจะข้ามการเช็คว่าเทแล้วหรือยัง)

### 8.2 🟡 Glass Freedom — งานที่เหลือของหมวดขวด/decoration

- ขวดที่เหลือ 10 อัน (Gin/Rum/Whiskey/Tequila/TripleSec/SweetVermouth/DryVermouth/Campari/Soda/Syrup)
  ยังไม่มี `BottleIngredientSource` — มีแค่ `Alchohol-Vodka` ตัวเดียวที่ทดสอบแล้ว
- กลไกตกแต่งแก้วหลังเท (decoration) — ยังไม่ตัดสินใจกลไก มี `TODO(design)` กำกับไว้ใน `GarnishFlowBridge.cs`
- `E_GarnishLook` enum — ยังเป็น placeholder รอ design ตัดสินใจรายการจริง
- sprite ของหมวด Fruit ทั้ง 6 ชนิด (tray + piece) ยังยืมสไปรต์ขวดเดิมมาใช้ ไม่ใช่ art จริง
- object เศษๆ ที่พบระหว่างพัฒนา ยังไม่ได้ตัดสินใจ: `Cube` (มี `GlassPlacementZone` ซ้ำกับ `Plane`
  บน layer ผิด — ผู้ใช้ขอให้เก็บไว้เฉยๆ ไม่ต้องแตะ) และ `BaseFruitPieceInstance` (raw prefab ลอยอยู่
  root-level ในซีน ไม่ได้เป็นลูกของ tray ไหน หน้าตาเหมือนของทดสอบค้าง ยังไม่ได้ถาม/ลบ)

### 8.3 🔴 ของค้างจากรอบ refactor เดิม (ซีน `New Drag Drop System` เท่านั้น)

**1 UnityEvent binding ที่ target หลุด** (ไม่มีใครเดาแทนได้)

| ที่ | binding | ต้องตัดสินใจอะไร |
|---|---|---|
| `Panel - Serve/BTN_Serving` OnClick[10] | `GameObject.SetActive` | GameObject ที่เคยชี้ไปถูกลบ ไม่มีทางรู้ว่าตัวไหน |

**Roster ขาด 2 คน** — `Demo_CustomerRoster` มีแค่ `Isla`, `Owen`, `Walter` · `Cole` กับ `Freya` จะเข้า
path `"No preferences for X; using every type"` · **ค่าความชอบเป็น design data — อย่าเดาเอง**

### 8.4 🟡 ช่องว่าง GDD ที่ยังเปิด (รอบ refactor เดิม)

| # | GDD | ติดอะไร |
|---|---|---|
| **S6** | §17.3 Fail(b) ต้องสุ่มชื่อจากคลังที่ designer เขียน | ยังไม่มีคลังชื่อ · ตอนนี้ใช้ `"???"` (`DrinkBuilder.UnmatchedName`) |
| **S8** | §21.1 Fail(b) ต้อง `BlendIngredientColors(poured)` | **ไม่มีข้อมูลสีต่อวัตถุดิบใน data model เลย** · ตอนนี้ใช้สีน้ำตาลขุ่นแทนสีดำ |
| S14 | §16 `MixMethod.Build` | เลื่อนตาม D4 |
| S15 | §16 `unlockedByDefault` | เลื่อนตาม D1 |

### 8.5 🟡 งานมือที่เหลือของรอบ refactor เดิม

- ย้ายอีก 4 ที่ออกจาก shim: `GamePlayScene`, `GamePlayScene 1`, `SystemGame.prefab`, `CocktailSystem.prefab`
- ลบ missing script `BookUI` ใน 4 ที่นั้น

### 8.6 🟢 งานถัดไปที่เป็นก้อนใหญ่

**ไม่มีใครเรียก `flow_open_bar` / `flow_prepare_drinks`** ทั้งในซีนและใน `.yarn` flow จึงหยุดที่
Level 1 — ต้องตัดสินใจว่าจะให้บทสนทนาเป็นคนขับ (ตรงเจตนา GDD §12) หรือผูกปุ่มไว้ทดสอบก่อน

---

## 9. ❓ คำถามค้างที่ควรถาม design

**GDD §18 วัด deviation เทียบกับสูตรไหน?**

§17 คำนวณ best-match ทั้งฐานข้อมูลเพื่อกำหนด *ตัวตน* ของเครื่องดื่ม (ชื่อ สี ราคา)
ส่วน §18 พูดถึงแค่คำว่า "deviation" เฉย ๆ

**โค้ดเลือกวัดเทียบสูตรที่ลูกค้าสั่ง** (`DrinkDeviation.MatchAgainst`) ส่วน best-match ใช้กำหนด
ตัวตนอย่างเดียว — ตรงกับที่โค้ดเดิมทำ · เขียน `NOTE(design)` กำกับไว้ที่เมธอดแล้ว

**🆕 กลไกตกแต่งแก้วหลังเท (decoration) ควรเป็นแบบไหน?** ยังไม่มีคำตอบ — `GarnishFlowBridge`
ปล่อยให้กด "เสร็จ" ได้เลยหลังเทเสร็จ ไม่มีขั้นตอนตกแต่งจริง (ดู §8.2)

---

## 10. วิธีตรวจสอบงาน

โปรเจกต์นี้ต่อกับ **Unity MCP** อยู่ ใช้ตรวจได้จริงไม่ต้องเดา

**คอมไพล์**

```
mcp__unityMCP__refresh_unity(mode=force, scope=all, compile=request, wait_for_ready=true)
mcp__unityMCP__read_console(action=get, types=["Error"])
```

**ตรวจข้อมูลสูตร** — เมนู `Bar410 > Validate Cocktail Data`
(ไล่ทุก `S_Drink` แล้วรายงานว่าอะไรขาด — **เลิกเช็ค `CompatibleGlass`/`SO_GlassVisualTable` แล้ว
ตั้งแต่ 2026-08-22** · ตอนนี้ขึ้น "validation passed")

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
```

> หมายเหตุ: ตัวอย่างเดิม `recipe=NotFix player=Rocks -> Rocks` ถูกลบออกจากชุดนี้แล้ว —
> โค้ดพาธนั้น (`GlassType.NotFix`/`DrinkBuilder.ApplyGlass`) ไม่มีอยู่จริงอีกต่อไป

---

## 11. ตัวเลขสรุป (รอบ refactor เดิม — ก่อน Glass Freedom)

| | ก่อน | หลัง |
|---|---:|---:|
| ตระกูลเมธอดเขียนซ้ำ 3 ก๊อป | 9 เมธอด | **3 generic** |
| ลูป TryGetComponent เปิด/ปิด interactable | 3 ก๊อป | **1** |
| สแกนสูตรต่อการอัปเดต shaker 1 ครั้ง | 5 | **1** |
| จุด hardcode `errors <= 2` | 6 | **1** |
| ไฟล์ยาวสุด | 412 | **~240** |
| กติกา GDD ที่ไม่ตรง | 15 | **5** |
| จำนวนไฟล์ / บรรทัด | 17 / 2,104 | 40+ / ~3,300 |

⚠️ ตัวเลขนี้เป็นของรอบ refactor เดิม (ก่อน 2026-08-21) **ไม่รวมงาน Glass Freedom** — commit
`bbdaccd` เพิ่มอีก 55 ไฟล์ (+1763/−127 บรรทัด) ทับไปอีกชั้น รายละเอียดใน `Refactor_Report.md` §7
(เฉพาะส่วนที่ไม่เกี่ยวกับแก้ว — ส่วนแก้วในไฟล์นั้นล้าสมัยแล้ว)
