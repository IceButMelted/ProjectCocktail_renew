# Bar410 — Cocktail System Refactor Summary

**Date:** 2026-08-21 · **Branch:** `GameLoop/main` · **Compile:** ✅ ไม่มี error
**เอกสารเต็ม:** `Bar410_CocktailSystem_Refactor_Report.md` · **แผน:** `Bar410_CocktailSystem_Refactor_Plan.md`

---

## ทำอะไรไป

ทำ **Phase 0, 1, 2 และ §4.7** จากแผน — งาน pure C# ทั้งหมด **ไม่แตะซีน prefab หรือ asset แม้แต่ไฟล์เดียว**

- แก้บั๊ก **6 ตัว** (B1, B3, B6, B7, B8, B11)
- แยก `UtilityDrink.cs` (412 บรรทัด) → `Cocktail/Domain/` **11 ไฟล์** แต่ละไฟล์ = หนึ่งหัวข้อ GDD
- ปิดช่องว่าง GDD **5 ข้อเต็ม** (S1, S2, S3, S4, S5, S7) + **2 ข้อบางส่วน** (S6, S8)
- เพิ่ม `CompositeDrinkRepository` (D1)

## เรื่องใหญ่ที่สุด: S1

โค้ดเดิมนับ *จำนวนชนิดวัตถุดิบที่ปริมาณต่าง* แต่ GDD §17.1 ต้องการ *ผลรวมส่วนต่าง* `Σ|r−p|`

สูตร `Gin 7, Vodka 3` ผู้เล่นเท `Gin 1, Vodka 9`:

| | ค่า | ผล |
|---|---:|---|
| ก่อน | 2 | **Acceptable** |
| หลัง | 12 | **Fail** |

เพราะทุกสูตรมีวัตถุดิบแค่ 3–4 ชนิด ค่าที่โค้ดเดิมคำนวณจึงแทบไม่มีทางเกิน 3–4 — **เกณฑ์ Fail
เกือบไม่เคยทำงานเลยตลอดที่ผ่านมา**

## ยืนยันด้วยการรันจริงใน Unity Editor

```
GDD 17.1 example  expect 2  got 2
S1 case Gin1/Vodka9 expect 12 got 12 -> Fail
15.2  0->None  1->Low  5->Low  6->High
case1 exact+method+ice   dev=0  -> Perfect     (pay 150)
case2 exact, wrong ice   dev=0  -> Acceptable  (pay 100)
case3 near, type match   dev=1  -> Acceptable  (pay 100)
case4 near, type WRONG   dev=1  -> Fail        (pay 50)
case5 far                dev=12 -> Fail        (pay 50)
B6 add 5 onto 9 -> allowed=False total=9
```

ครบทั้งตัวอย่างใน GDD §17.1, เกณฑ์ §15.2, บันได §18 ทั้ง 5 เคส, ราคา §18.1 และเพดาน 10 หน่วย

**เคส 4 คือสิ่งที่เกิดขึ้นไม่ได้เลยมาก่อน** — `Fail (a)` ต้องเทียบประเภทที่เสิร์ฟกับประเภทที่สั่ง
แต่โค้ดเดิมไม่เคยเก็บ "ประเภทที่ลูกค้าสั่ง" ไว้ที่ไหน ราคา ×0.5 ของ GDD §18.1 จึงเข้าไม่ถึงตามไปด้วย

## ตัวเลข

| | ก่อน | หลัง |
|---|---:|---:|
| ตระกูลเมธอดที่เขียนซ้ำ 3 ก๊อป | 9 เมธอด | **3 generic** |
| สแกนสูตรต่อการอัปเดต shaker 1 ครั้ง | 5 | **1** |
| จุด hardcode `errors <= 2` | 6 | **1** |
| ไฟล์เกิน 200 บรรทัด | 3 | **1** |
| กติกา GDD ที่ไม่ตรง | 14 | **7** |

⚠️ **จำนวนบรรทัดเพิ่มขึ้น ไม่ได้ลดลง** (262 → 329 code-only ในชั้น domain) เพราะ Phase 2 เพิ่มกติกา
GDD ที่โค้ดไม่เคยมี (`PricingRules`, `SatisfactionEvaluator` เคส 3/4, `RecipeMatch`) และการลดก้อนใหญ่
อยู่ใน Phase 3–5 ที่ยังไม่ได้ทำ — รายละเอียดใน Report §7

---

## ⚠️ ต้องรู้ก่อนรันเกม

**เกมจะยากขึ้นทันที** — เครื่องดื่มจำนวนมากที่เคยได้ `Acceptable` จะกลายเป็น `Fail`
ค่า `MaxTolerance = 3` ยังไม่ผ่านการเล่นทดสอบ (D7) แก้ที่ `Domain/DrinkDeviation.cs` จุดเดียว

**ก่อนเล่นทดสอบ D7 ต้องสลับซีนไปใช้ `Normal_Cocktail` (26 สูตร)** — ตอนนี้ซีนพัฒนาชี้ไป
`Demo_Normal_Cocktail` ที่มีแค่ 6 สูตร โอกาสเจอสูตรใกล้เคียงต่างกันมหาศาล ค่าที่ยืนยันจากซีนนั้นใช้ไม่ได้

## ยังไม่ได้ทำ

**Phase 3–8** — แยก `CocktailShakerData` เป็น 5 component, `DrinkOrderContext`, Yarn adapter,
`BarSetupBridge` + `CocktailFlowBridge`, ย้ายไฟล์, งานกรอกข้อมูล
ทั้งหมดต้องแก้ซีน/prefab และผูก inspector คืนด้วยมือ ทำจากนอก Unity แล้วยืนยันไม่ได้ว่าไม่พัง

**ช่องว่าง GDD ที่ยังเปิด:** S9, S10, S11, S12, S13 (S14/S15 เลื่อนโดยเจตนาตาม D4/D1)

**S6 / S8 ปิดได้แค่บางส่วน** — GDD ต้องการคลังชื่อสุ่มและตารางสีต่อวัตถุดิบ ซึ่งยังไม่มีใน data model
เขียน `TODO(design, ...)` กำกับไว้ในโค้ดแล้ว

## งานมือที่ค้างอยู่

1. `CocktailSystem.prefab` มี OnClick ชี้ไป `AddIce()` ที่ถูกลบ (body ว่าง ไม่เคยทำอะไร)
   → เปลี่ยนไปเรียก `AddIce(bool)` — **ไม่กระทบตอนนี้** เพราะ prefab นี้ไม่มีซีนไหนอ้างถึง
2. แก้ `GDD_Bar410_Master.md` ตาม errata E1 (§15.1/§16) และ E2 (§19.1) — อยู่นอก repo นี้
3. ยืนยันความกำกวมของ GDD §18: deviation วัดเทียบสูตรที่**สั่ง** (ที่ implement ไป) หรือ best-match
   — เขียน `NOTE(design)` กำกับไว้ที่ `DrinkDeviation.MatchAgainst` แล้ว

---

## ไฟล์

**ใหม่ 12** · **แก้ 12** · **ลบ 1** (`UtilityDrink.cs`) · **ซีน/prefab/asset: 0**
