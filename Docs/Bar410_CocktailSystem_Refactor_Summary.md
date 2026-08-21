# Bar410 — Cocktail System Refactor Summary

**Date:** 2026-08-21 · **Branch:** `GameLoop/main` · **Compile:** ✅ ไม่มี error
**เอกสารเต็ม:** `Bar410_CocktailSystem_Refactor_Report.md`
**งานมือใน Unity:** `Bar410_CocktailSystem_Manual_Setup.md`
**หน้าที่ของแต่ละไฟล์:** `Bar410_CocktailSystem_Architecture.md`
**แผน:** `Bar410_CocktailSystem_Refactor_Plan.md`

---

## ทำอะไรไป

**Phase 0–8 ครบทุกเฟส (ฝั่งโค้ด)** — และ **ไม่แตะซีน prefab หรือ asset แม้แต่ไฟล์เดียว**

- แก้บั๊ก **6 ตัว** (B1, B3, B6, B7, B8, B11) + **B2, B4, B5** ในเฟสหลัง = **9 ตัว**
- `UtilityDrink.cs` (412 บรรทัด) → `Cocktail/Domain/` **11 ไฟล์** แต่ละไฟล์ = หนึ่งหัวข้อ GDD
- `CocktailShakerData` (5 หน้าที่) → `Cocktail/Shaker/` **7 ไฟล์**
- `CocktailSystemManager.YarnInterface.cs` (391 บรรทัด) → **4 ไฟล์**
- เพิ่ม `Cocktail/Session/` — `DrinkOrderContext`, `OrderService`, `DrinkScoringService`, `SO_Customer`
- เพิ่ม HSM bridges 2 ตัว + `CompositeDrinkRepository` + validator
- ปิดช่องว่าง GDD **10 ข้อ** (S1–S5, S7, S9–S12) · ค้าง 3 (S6, S8, S13) · เลื่อนโดยเจตนา 2 (S14, S15)

## เรื่องใหญ่ที่สุด: S1

โค้ดเดิมนับ *จำนวนชนิดวัตถุดิบที่ต่าง* แต่ GDD §17.1 ต้องการ *ผลรวมส่วนต่าง* `Σ|r−p|`

สูตร `Gin 7, Vodka 3` ผู้เล่นเท `Gin 1, Vodka 9` → ก่อน: **2 (Acceptable)** · หลัง: **12 (Fail)**

เพราะทุกสูตรมีวัตถุดิบแค่ 3–4 ชนิด ค่าเดิมจึงแทบไม่มีทางเกิน 3–4 — **เกณฑ์ Fail เกือบไม่เคยทำงาน**

## ยืนยันด้วยการรันจริงใน Unity Editor

```
GDD 17.1 expect 2 -> 2          S1 expect 12 -> 12
15.2  5->LowAlcohol  6->HighAlcohol
ctx case1 Perfect    pay=150 rel=0.5   scored=True
ctx case2 Acceptable pay=100 rel=0.25  scored=True
ctx case3 Acceptable pay=100 rel=0.25  scored=True
ctx case5 Fail       pay=50  rel=0     scored=True
B6 add 5 onto 9 -> allowed=False total=9
```

ครบทั้งตัวอย่าง GDD §17.1, เกณฑ์ §15.2, บันได §18 ทั้ง 5 เคส, ราคา §18.1, ค่าความสัมพันธ์ §18.2
และเพดาน 10 หน่วย — ผ่านทั้งก่อนและหลัง Phase 3–7

## ข้อมูลจริงจาก validator (`Bar410 > Validate Cocktail Data`)

```
สูตรทั้งหมด 26  ·  Σ ingredients != 10 -> 0  ·  สูตรซ้ำกันเป๊ะ -> 0
CompatibleGlass = None -> 20
```

**S12 ผ่านอยู่แล้วโดยไม่ต้องแก้อะไร** ทั้ง 26 สูตรรวมได้ 10 พอดี · เหลือ G2 (20 ใบไม่มีแก้ว)

**ผลข้างเคียงของ S3 ที่วัดได้:** มี 2 สูตรที่มีแอลกอฮอล์ 5 หน่วยพอดี (`23_JungleBird`, `44_Siesta`)
ทั้งคู่ design เขียนไว้เป็น `LowAlcohol` อยู่แล้ว ส่วนโค้ดเดิมคำนวณได้ `HighAlcohol`
→ **ยืนยันว่า GDD §15.2 ถูก และโค้ดเดิมผิด**

## ตัวเลข

| | ก่อน | หลัง |
|---|---:|---:|
| ตระกูลเมธอดเขียนซ้ำ 3 ก๊อป | 9 เมธอด | **3 generic** |
| ลูป TryGetComponent เปิด/ปิด interactable | 3 ก๊อป (component คนละชุด) | **1** |
| สแกนสูตรต่อการอัปเดต shaker 1 ครั้ง | 5 | **1** |
| จุด hardcode `errors <= 2` | 6 | **1** |
| ไฟล์ยาวสุด | 412 | **239** (shim ชั่วคราว) |
| กติกา GDD ที่ไม่ตรง | 15 | **5** |
| จำนวนไฟล์ / บรรทัดรวม | 17 / 2,104 | 40 / 3,270 |

⚠️ **บรรทัดเพิ่มขึ้น ไม่ได้ลดลง** — เพราะเพิ่มกติกา GDD ที่โค้ดไม่เคยมี, shim ยังอยู่รอการย้ายข้อมูล
และคอมเมนต์อธิบายเหตุผลทุกจุดที่ต่างจาก GDD รายละเอียดใน Report §7

---

## ⚠️ ต้องรู้ก่อนรันเกม

**เกมจะยากขึ้นทันที** — เครื่องดื่มจำนวนมากที่เคยได้ `Acceptable` จะกลายเป็น `Fail`
ค่า `MaxTolerance = 3` ยังไม่ผ่านการเล่นทดสอบ (D7) แก้ที่ `Domain/DrinkDeviation.cs` จุดเดียว

**ก่อนเล่นทดสอบต้องสลับซีนไปใช้ `Normal_Cocktail` (26 สูตร)** — ตอนนี้ซีนพัฒนาชี้ไป
`Demo_Normal_Cocktail` ที่มีแค่ 6 สูตร ค่าที่ยืนยันจากซีนนั้นใช้กับเกมจริงไม่ได้

## เกมยังเล่นได้เหมือนเดิมโดยไม่ต้องทำอะไร

ทุก class ที่ซีนอ้างถึงยังอยู่ครบ และยังมีเมธอดเดิมทุกตัวที่ UnityEvent ผูกไว้
`CocktailShakerData` / `CocktailShaker` กลายเป็น shim ที่ไม่มี logic เหลือ — มันสร้าง component ใหม่
ให้อัตโนมัติตอน `Awake` พร้อมป้อนข้อมูลจากฟิลด์เดิม

**แต่ flow ยังไม่ขับ Cocktail System** จนกว่าจะใส่ `BarSetupBridge` + `CocktailFlowBridge`
ใน `[GameLoop]` ด้วยมือ — ดู Manual Setup §6.2

## งานมือ 8 ข้อ

อยู่ใน `Bar410_CocktailSystem_Manual_Setup.md` เรียงตามความสำคัญ
สองข้อแรก (สลับ repository, ยืนยัน `MaxTolerance`) ควรทำก่อนเล่นทดสอบ ที่เหลือเป็นการเก็บกวาด

## เอกสารชุดนี้

| ไฟล์ | ใช้เมื่อ |
|---|---|
| `..._Refactor_Plan.md` | อยากรู้ว่าตัดสินใจอะไรไปและเพราะอะไร (§13 Decision Log) |
| `..._Refactor_Report.md` | รายละเอียดเต็ม — ทำอะไรไป ผลตรวจสอบ ตัวเลข |
| `..._Refactor_Summary.md` | เอกสารนี้ |
| `..._Architecture.md` | **ไฟล์ไหนทำอะไร · จะแก้อะไรต้องเปิดไฟล์ไหน** |
| `..._Manual_Setup.md` | ลงมือทำงานมือใน Unity |

## ยังค้างในโค้ด

**S6** (คลังชื่อสุ่ม), **S8** (ตารางสีต่อวัตถุดิบ) — ติดที่ยังไม่มีข้อมูลให้ใช้ เขียน seam + `TODO(design, ...)` ไว้แล้ว

**S13** (เลือกแก้วเอง) — **กติกาทำแล้ว**: สูตรที่ตั้ง `CompatibleGlass = NotFix` จะไม่ทับแก้วที่ผู้เล่นเลือก
เหลือแค่ UI ให้กดเลือกแล้วเรียก `ShakerContents.SetGlass`

**S14 / S15** เลื่อนโดยเจตนาตาม D4 / D1
