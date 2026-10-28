# Bar410 — Ingredient Unlock Plan

สถานะ: **ร่าง รอเจ้าของอนุมัติ** (ยังไม่มีโค้ด) · 2026-10-28
ADR ที่เกี่ยว: [0003](adr/0003-ingredient-unlock.md) (ทับ ADR 0001 เรื่อง "วัตถุดิบมีครบทุกวัน")

## ทิศทางที่เจ้าของเลือกแล้ว
| เรื่อง | คำตอบ |
|---|---|
| เลขวันมาจากไหน | ตัวแปร Yarn `$day` (เนื้อเรื่องตั้งเองใน node `DayN`) |
| สถานะปลดล็อคเก็บที่ไหน | รายการใน C# แยกต่างหาก + save เอง |
| วัตถุดิบที่ล็อคบนบาร์ | ซ่อนปุ่ม |
| หน้าสูตรในหนังสือ | รอตัวอย่างจากเจ้าของ รอบนี้ทำแค่ API |

## หลักการ
- **Single source of truth = `IngredientUnlocks`** (component ใหม่ ตัวเดียวในฉาก) ทุกคน (บาร์, หนังสือ, Yarn) ถามที่นี่ที่เดียว
- "ปลดล็อคแล้ว" **คำนวณ** ไม่ได้เก็บ:
  `IsUnlocked(x) = (row.Day > 0 && $day >= row.Day) || storyUnlocked.Contains(x)`
  - ปลดล็อคตามวัน → ไม่ต้อง save เพราะ `$day` save ไปกับ Yarn อยู่แล้ว
  - ปลดล็อคตามเนื้อเรื่อง → `HashSet<string>` ใน C# ตัวเดียวที่ต้อง save
- **Key ของวัตถุดิบ = ชื่อ enum** (`Gin`, `LemonJuice`, …) ชื่อทั้ง 17 ตัวใน `BaseSpirit` / `Liqueur` / `Mixer` ไม่ซ้ำกัน จึงใช้เป็นชื่อใน Yarn ได้ตรงๆ
- วัตถุดิบที่**ไม่มีแถว**ในตาราง = ปลดล็อคตั้งแต่ต้น (ของเดิมไม่พัง)

## ข้อมูล (Inspector)
`SO_IngredientUnlockTable` (ScriptableObject, ตาราง Odin) แถวละหนึ่งวัตถุดิบ:

| คอลัมน์ | ความหมาย |
|---|---|
| Ingredient | dropdown รวม 17 ชื่อ (เก็บเป็น string ชื่อ enum) |
| Unlock Day | `0` = ปลดล็อคด้วย Yarn เท่านั้น, `N` = ปลดล็อคเองเมื่อ `$day >= N` |

ทั้งสองทางใช้ร่วมกันได้: ตั้ง Day 5 แต่เนื้อเรื่องสั่ง `<<unlock_ingredient X>>` ก่อนวัน 5 ก็ได้

## Yarn
```
<<declare $day = 1>>           // ประกาศครั้งเดียว ให้ SaveLoadManager เก็บได้ (มันเดินแค่ InitialValues)
<<set $day to 3>>              // ใน node Day3
<<unlock_ingredient LemonJuice>>
<<if ingredient_unlocked("LemonJuice")>> ... <<endif>>
```
- `unlock_ingredient` เรียกซ้ำได้ไม่มีผล (silent replay ตอน load จะรันซ้ำ)
- ชื่อผิด → `Debug.LogError` ไม่เงียบ

## หนังสือ (รอบนี้แค่ API)
- `IsUnlocked(string ingredient)`
- `IsRecipeComplete(S_Drink)` = วัตถุดิบทุกตัวใน Alcohol/Liqueur/Mixer list ปลดล็อคแล้ว
- กติกาที่เจ้าของกำหนด:
  - สูตรยังไม่ครบ → วัตถุดิบที่ปลดล็อคขึ้นชื่อ ที่ล็อคเป็น `???` **ไม่แสดงปริมาณเลย**
  - สูตรครบ → ขึ้นชื่อ + ปริมาณทุกตัว
- UI หนังสือรอตัวอย่างจากเจ้าของ (`BookUI_V2` ตอนนี้เป็นหน้าเขียนตายตัว)

## Stage (compile-check ทุก stage)
1. **ข้อมูล + ตัวกลาง:** `SO_IngredientUnlockTable`, `IngredientUnlocks` (scene-scoped, มี `Current`), `IsUnlocked`, `IsRecipeComplete`, อ่าน `$day` ผ่าน `YarnVariableSync`
2. **Yarn:** `<<unlock_ingredient>>`, function `ingredient_unlocked`, `<<declare $day>>` + `<<set $day>>` ใน `Day1..Day10`
3. **Save/Load:** เพิ่ม `UnlockData` ใน `SaveData`, เก็บตอน `SaveToFile`, ตอน load **แทนที่** set (ไม่ merge)
4. **บาร์:** ตอนเข้า AddIngredient `FlowWiring` สั่ง refresh → `IngredientButtonGroup` ซ่อนเฉพาะปุ่ม AddAlcohol/AddLiqueur/AddMixer ที่ล็อค (ปุ่ม Ice / Method / Reset ไม่แตะ)
5. **Validate:** เพิ่มใน `Bar410 > Validate Cocktail Data`: ชื่อในตารางไม่ซ้ำ/มีจริง, รายงานสูตรที่ใช้วัตถุดิบที่ล็อค
6. **ทดสอบ Play:** วัน 1 / หลัง unlock / save-load / ปุ่มที่ซ่อนไม่ทิ้งช่องว่างแปลกๆ

## คำถามที่ยังเปิด (ต้องให้เจ้าของตัดสิน)
1. **ออเดอร์:** `RandomCocktail` / `order_by_type` / `Order_Cocktail_*` อาจสั่งเครื่องดื่มที่ใช้วัตถุดิบที่ยังซ่อนอยู่ ผู้เล่นจะทำไม่ได้ จะ (ก) กรอง random ให้เลือกแค่สูตรที่ครบ (ข) ปล่อย ให้คนเขียน Yarn ระวังเอง หรือ (ค) อย่างอื่น?
2. **ปลดล็อคตั้งแต่แรก:** วัตถุดิบไหนบ้างที่มีตั้งแต่วัน 1 (หรือใช้ "ไม่มีแถว = ปลดล็อค" ตามแผน)?
3. **แจ้งผู้เล่น:** ตอนปลดล็อคต้องมี popup/เสียงไหม หรือเงียบๆ?
4. **ปลดล็อคย้อนกลับ (lock คืน):** ต้องมีไหม (แผนนี้ถือว่าไม่มี)
