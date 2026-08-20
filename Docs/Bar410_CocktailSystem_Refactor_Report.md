# Bar410 — Cocktail System Refactor Report

**Date:** 2026-08-21
**Branch:** `GameLoop/main`
**Plan:** `Bar410_CocktailSystem_Refactor_Plan.md`
**Scope executed:** Phase 0, Phase 1, Phase 2, §4.7 — **งาน pure C# ทั้งหมด ไม่มีการแก้ซีน prefab หรือ asset**
**Compile status:** ✅ ผ่าน ไม่มี error (ยืนยันผ่าน Unity MCP)

---

## 1. สรุปสั้น

ทำ 4 ส่วนจากแผน:

| Phase | งาน | สถานะ |
|---|---|---|
| 0 | ลบโค้ดตาย + แก้บั๊ก B1, B3, B6, B7, B8, B11 | ✅ เสร็จ |
| 1 | แยก `UtilityDrink.cs` → `Cocktail/Domain/` 11 ไฟล์ + generic `IngredientMath` | ✅ เสร็จ |
| 2 | ทำให้ตรง GDD — S1, S2, S3, S5, S7 เต็ม · S6, S8 บางส่วน | ⚠️ เสร็จบางส่วน |
| §4.7 | `CompositeDrinkRepository` (D1) | ✅ เสร็จ |
| 3–8 | แยก component, `DrinkOrderContext`, Yarn adapter, bridges, ย้ายไฟล์, งาน data | ❌ ยังไม่ทำ — ดู §6 |

**บั๊กที่แก้แล้ว 6 ตัว · ช่องว่าง GDD ที่ปิดแล้ว 5 ข้อเต็ม + 2 ข้อบางส่วน · ไฟล์ที่ยาวเกิน 200 บรรทัดเหลือ 1 ไฟล์**

---

## 2. ผลการตรวจสอบ (รันจริงใน Unity Editor)

รันโค้ดทดสอบผ่าน Unity MCP โดยใช้ตัวอย่างจาก GDD โดยตรง ผลลัพธ์:

```
GDD 17.1 example  expect 2  got 2
S1 case Gin1/Vodka9 expect 12 got 12 | flag=Fail | satisfaction=Fail
15.2  0->NoneAlcohol  1->LowAlcohol  5->LowAlcohol  6->HighAlcohol
case1 exact+method+ice   dev=0  -> Perfect     (pay 150)
case2 exact, wrong ice   dev=0  -> Acceptable  (pay 100)
case3 near, type match   dev=1  -> Acceptable  (pay 100)
case4 near, type WRONG   dev=1  -> Fail        (pay 50)
case5 far                dev=12 -> Fail        (pay 50)
B6 add 5 onto 9 -> allowed=False total=9 (expect False / 9)
```

| ตรวจ | อ้างอิง | ผล |
|---|---|---|
| สูตร deviation | GDD §17.1 ตัวอย่าง `Gin7 Vodka3` vs `Gin7 Vodka2 Syrup1` = 2 | ✅ ได้ 2 |
| S1 regression | `Gin1 Vodka9` vs `Gin7 Vodka3` — GDD ได้ 12 (Fail), โค้ดเดิมได้ 2 (Acceptable) | ✅ ได้ 12 → Fail |
| เกณฑ์แอลกอฮอล์ | GDD §15.2 — 5 หน่วยต้องเป็น Low, 6 เป็น High | ✅ ตรง |
| บันไดความพึงพอใจ | GDD §18 ทั้ง 5 เคส | ✅ ตรงทุกเคส |
| ราคา | GDD §18.1 — `×1.5 / ×1.0 / ×0.5 / 50` (ราคาฐาน 100) | ✅ `150 / 100 / 50 / 50` |
| เพดาน 10 หน่วย | B6 — เติม 5 หน่วยลงบนแก้วที่มี 9 ต้องถูกปฏิเสธ | ✅ ปฏิเสธ, ยังคง 9 |

**เคส 4 คือสิ่งที่เกิดขึ้นไม่ได้เลยก่อนหน้านี้** — `Fail (a)` ต้องเทียบ `servedType` กับ `orderedType`
แต่โค้ดเดิมไม่เคยเก็บ "ประเภทที่ลูกค้าสั่ง" ไว้ที่ไหน จึงเทียบไม่ได้ และราคา ×0.5 ของ GDD §18.1
เข้าถึงไม่ได้ตามไปด้วย

---

## 3. Phase 0 — บั๊กที่แก้

| # | ที่ | อาการเดิม | ที่แก้ |
|---|---|---|---|
| **B1** | `YarnInterface.cs` | `_cocktailType` ถูกกำหนดค่าแค่ `None` เท่านั้นทั้งโปรเจกต์ → `$type_of_cocktail` เขียน `0` เสมอ ทุก `<<if $type_of_cocktail ...>>` ใน Yarn เป็น branch ตาย | เปลี่ยนเป็น `_servedType` ที่ `CalculateSatisfaction()` เขียนค่าจริงลงไป |
| **B3** | `YarnInterface.cs:183` | `ResolvePostItText` เรียก `FindFirstObjectByType<Post_It_Order>()` ทุกครั้งที่สั่งเครื่องดื่ม ทั้งที่มี `_postItOrder` เป็น `[SerializeField]` อยู่แล้ว | ใช้ฟิลด์ที่ผูกไว้ + warning ถ้าไม่ได้ผูก |
| **B6** | `UtilityDrink.cs:48` | `IsValidRatio` เช็กแค่ว่ายอด **ปัจจุบัน** < 10 → เติม 5 หน่วยตอนมี 9 ได้แก้ว 14 หน่วย | `DrinkQuery.CanAdd(d, amount)` นับ amount ที่กำลังเติมด้วย |
| **B7** | `DebugCocktail.cs:22` | `"Shaker:\n" + _shaker.CurrentCocktail` ต่อ string กับ ScriptableObject → พิมพ์ชื่อ object ไม่ใช่ข้อมูลเครื่องดื่ม | ใช้ `DrinkFormatter.GetCocktailInfo()` |
| **B8** | `CocktailSystemManager.cs:37` | `Awake` เขียน `_characterData` ที่ประกาศอยู่ในไฟล์ partial อีกไฟล์ — coupling ที่มองไม่เห็น | ย้ายการประกาศมาไว้ไฟล์เดียวกับที่เขียนค่า |
| **B11** | `SystemGame.prefab` | `_normalCocktailRepository` เป็น null → `Start()` โยน NullReferenceException | `CompositeDrinkRepository` ข้าม source ที่เป็น null และ log error ที่ระบุ GameObject แทน |

### โค้ดตายที่ลบ

| ที่ | ลบอะไร |
|---|---|
| `CocktailSystemManager.cs` | `_failCocktailSprite` (อยู่แต่ในคอมเมนต์), `Update()` ว่าง, คอมเมนต์โค้ดเก่า ~25 บรรทัด, `_normalDrinks` |
| `YarnInterface.cs` | `SetPostItText` ที่คอมเมนต์ทิ้ง, `using System.Linq` และ `using System.Runtime.CompilerServices` ที่ไม่ได้ใช้ |
| `CocktailShakerData.cs` | `Update()` ว่าง |
| `IngredientButtonUI.cs` | `ActionBehavior()` (ก๊อปของ `Invoke()` ทุกไบต์), `AddIce()` ที่ body ว่าง |
| `UtilityDrink.cs` | **ลบทั้งไฟล์** (412 บรรทัด) — ย้ายไป `Domain/` |

> ตรวจแล้วว่า `ActionBehavior` **ไม่ได้ถูกผูกใน UnityEvent ของซีนหรือ prefab ไหนเลย** จึงลบได้ปลอดภัย
> ส่วน `AddIce` ดู §5 ข้อ 1

---

## 4. Phase 1 + 2 — `Domain/` layer

`UtilityDrink.cs` (412 บรรทัด / 4 หน้าที่) → `Cocktail/Domain/` 11 ไฟล์ แต่ละไฟล์ trace กลับหัวข้อ GDD ได้:

| ไฟล์ | บรรทัด | GDD § | หน้าที่ |
|---|---:|---|---|
| `DrinkFlagResolver.cs` | 21 | §17.3 | deviation → `Perfect` / `Seem_Like` / `Fail` |
| `AlcoholClassifier.cs` | 38 | §15.2 | หน่วยแอลกอฮอล์ → `TypeOfCocktail` |
| `DrinkQuery.cs` | 42 | §15 | ยอดรวมต่อหมวด, เพดาน 10 หน่วย |
| `DrinkColorBlender.cs` | 46 | §21.1 | สีน้ำในแก้ว |
| `SatisfactionEvaluator.cs` | 48 | §18 | บันไดความพึงพอใจ 5 เคส |
| `DrinkFormatter.cs` | 49 | — | ข้อความ debug / tooltip |
| `PricingRules.cs` | 55 | §18.1, §18.2 | ราคา + ค่าความสัมพันธ์ |
| `RecipeMatch.cs` | 59 | §17 | ผลการเทียบ + `DrinkFlag` |
| `IngredientMath.cs` | 96 | — | generic helper ที่ยุบโค้ดซ้ำ |
| `DrinkBuilder.cs` | 110 | §10, §17.3 | เติมวัตถุดิบ, `ApplyRecipeIdentity`, `Clear` |
| `DrinkDeviation.cs` | 112 | §17.1, §17.2 | สูตร `Σ\|r−p\|`, `FindBestMatch`, `MatchAgainst` |

### 4.1 โค้ดซ้ำที่ยุบได้ (D6 — คง 3 ลิสต์)

`AlcoholIngredient` / `LiqueurIngredient` / `MixerIngredient` implement `IIngredientEntry<TKey>`
โดยชื่อฟิลด์เดิม (`Type` / `Amount`) ไม่เปลี่ยน → **ข้อมูลที่ serialize ไว้ทั้ง 26 asset และปุ่มในซีนไม่ถูกแตะเลย**

| เมธอดเดิม | ยุบเหลือ |
|---|---|
| `AlcoholListEquals` + `LiqueurListEquals` + `MixerListEquals` | `IngredientMath.ListEquals<,>` |
| `CountAlcoholErrors` + `CountLiqueurErrors` + `CountMixerErrors` | `IngredientMath.Deviation<,>` |
| `TryToAddAlcohol` + `TryToAddLiqueur` + `TryToAddMixer` (ตัวคำนวณ) | `IngredientMath.Add<,>` |

**9 เมธอด → 3 generic** และ `IngredientMath` ไม่รู้จักหมวดใดเป็นพิเศษ — การเพิ่มหมวดที่ 4
(Bitters, Garnish) **ไม่ต้องเขียนอัลกอริทึมใหม่แม้แต่บรรทัดเดียว** เหลือแค่แก้จุดรวมผล 4 จุด
ซึ่งมีคอมเมนต์ `เพิ่มหมวดใหม่: แก้ที่นี่` กำกับไว้ครบแล้วที่:

- `DrinkDeviation.Compute`
- `DrinkQuery.GetTotalIngredient`
- `DrinkBuilder.Clear`
- `DrinkFormatter.GetCocktailIngredient`

### 4.2 การสแกนสูตร 5 รอบ → 1 รอบ

เดิม `UpdateCocktailInShaker` เรียก `UpdateTypeOfAlcohol` → `UpdateName` → `UpdatePrice` →
`UpdateColorInGlass` → `UpdateGlassType` และแต่ละตัวเรียก `FindBestIngredientMatch` ของตัวเอง =
สแกนสูตรทั้งฐาน 5 รอบต่อการเติมวัตถุดิบหนึ่งครั้ง

ตอนนี้:

```csharp
LastMatch = DrinkDeviation.FindBestMatch(CurrentCocktail, recipes);
DrinkBuilder.ApplyRecipeIdentity(CurrentCocktail, LastMatch);
```

**และแก้บั๊กเชิงลำดับที่ซ่อนอยู่ด้วย** — `UpdateTypeOfAlcohol` เดิมค้นหาสูตรจาก `d.Name` ที่
`UpdateName` เพิ่งเขียนลงไป ถ้าสลับลำดับการเรียกเมื่อไหร่ ประเภทแอลกอฮอล์จะผิดทันที
ตอนนี้ match เดียวเข้า identity เดียวออก ไม่มีลำดับให้ผิด

### 4.3 ช่องว่าง GDD ที่ปิดแล้ว

| # | GDD | ก่อน | หลัง |
|---|---|---|---|
| **S1** | §17.1 `Σ\|r−p\|` | นับ *จำนวนชนิด* ที่ปริมาณต่าง | `IngredientMath.Deviation` รวมส่วนต่างจริง |
| **S2** | §17.3 เกณฑ์ `<= 3` | `errors <= 2` hardcode 6 จุด | `DrinkDeviation.MaxTolerance` const เดียว |
| **S3** | §15.2 `1..5 → Low` | `>= 5 → High` (off-by-one) | `AlcoholClassifier.FromUnits` ตาม GDD |
| **S4** | §18 เคส 3/4 | ไม่เคยเทียบประเภท → `Fail (a)` เกิดไม่ได้ | `SatisfactionEvaluator.Evaluate(match, servedType, orderedType)` |
| **S5** | §18.1 ราคา | ไม่มีตัวคูณเลย, fallback `5f` | `PricingRules.Payout` — `×1.5 / ×1.0 / ×0.5 / 50` |
| **S7** | §17.3 Fail(b) drinkType | ไม่ได้เซ็ต | `AlcoholClassifier.Compute(runtime)` ใน `ApplyRecipeIdentity` |
| **§17.2** | tie-break ตัวแรกชนะ | ถูกอยู่แล้วโดยบังเอิญ | เขียนคอมเมนต์กำกับว่าตั้งใจ (`if (deviation >= bestDeviation) continue;`) |
| **§18.2** | ค่าความสัมพันธ์ | ไม่มีเลย | `PricingRules.RelationshipDelta` — `+0.5 / +0.25 / 0` (ยังไม่มีใครเรียก ดู §6) |

### 4.4 ⚠️ ปิดได้แค่บางส่วน

| # | GDD | ทำอะไรไป | ทำไมไม่จบ |
|---|---|---|---|
| **S6** | §17.3 Fail(b) → ชื่อสุ่ม | เปลี่ยน `"NOT MATCH ANY"` → `"???"` และรวมไว้ที่ `DrinkBuilder.UnmatchedName` | GDD ต้องการ **สุ่มจากคลังชื่อที่ designer เขียน** — คลังนั้นยังไม่มี การแต่งชื่อเองคือการสร้าง content ไม่ใช่งาน refactor |
| **S8** | §21.1 Fail(b) → `BlendIngredientColors` | แยก `DrinkColorBlender` ออกมา, ฝั่ง Perfect/Seem_Like ใช้สีจากสูตรที่ match ตาม GDD, ฝั่ง Fail(b) เปลี่ยนจาก **สีดำ** เป็นสีน้ำตาลขุ่น | **ไม่มีข้อมูลสีต่อวัตถุดิบใน data model เลย** — ไม่มีอะไรให้ blend ต้องเพิ่มตาราง `IngredientType → Color` ก่อน |

ทั้งสองข้อมี `TODO(design, plan S6/S8)` กำกับไว้ในโค้ดพร้อมอธิบายว่าติดอะไร

---

## 5. §4.7 — `CompositeDrinkRepository` (D1)

`Cocktail/CompositeDrinkRepository.cs` (95 บรรทัด) รวม normal + special ให้เป็น `IDrinkRepository` เดียว

**สถานะข้อมูลจริงตอนนี้:** `Specia_Cocktail.asset` มี `cocktails: []` ว่างเปล่า และ
`_specialCocktailRepository` เป็น null ในทุกซีน — **พฤติกรรมจึงเหมือนเดิมทุกประการ** งานนี้คือ
การเปิดรอยต่อไว้ให้พร้อม ไม่ใช่เปลี่ยนพฤติกรรม

สองจุดที่ออกแบบไว้ตามแผน:

1. **`GetRandom()` สุ่ม uniform บน union** ไม่ใช่สุ่ม repository ก่อน — ถ้าสุ่ม repo ก่อน
   สูตรพิเศษ 2 ใบจะมีโอกาสออก 50% แทนที่จะเป็น 2/28 (รูปแบบบั๊กเดียวกับ S10)
2. **แยก pool การค้นหาออกจาก pool การสุ่ม** — `CocktailSystemManager` ถือ 2 ตัว:
   - `_lookup` = composite → ใช้ค้นชื่อ (โหมดสั่ง 1/2) สูตรพิเศษต้องหาเจอ
   - `_randomPool` = normal เท่านั้น → สูตรพิเศษไม่หลุดออกมาก่อนถึงจังหวะในเนื้อเรื่อง

เพิ่ม `TryGetByName` เข้า `IDrinkRepository` → ชั้น Yarn เลิกทำ LINQ ค้นชื่อเอง

---

## 6. สิ่งที่ **ยังไม่ได้ทำ**

### 6.1 Phase ที่เหลือ

| Phase | งาน | ทำไมยังไม่ทำ |
|---|---|---|
| 3 | แยก `CocktailShakerData` → 5 component | ต้องแก้ซีน/prefab **5 ที่** และผูก inspector คืนด้วยมือ ทำจากที่นี่แล้วยืนยันไม่ได้ว่าไม่พัง |
| 4 | `DrinkOrderContext` / `OrderService` / `SO_Customer` (S10, S11) | ต้องผูก reference ในซีน |
| 5 | แยก Yarn adapter 4 ไฟล์ + B5 + โหมดสั่งที่ 5 (S9) | ต้องแก้ซีน และ `CocktailYarnCommands` ต้องอยู่บน GameObject ชื่อ `SystemGame` เป๊ะ |
| 6 | `BarSetupBridge` + `CocktailFlowBridge` | ต้องผูกใน `[GameLoop]` |
| 7 | ย้ายไฟล์ + แยก `Enum_Class` เป็น partial | ต้องย้ายผ่าน Unity `MoveAsset` เพื่อรักษา `.meta` GUID |
| 8 | งาน data (G1, G2, S12) | เป็นงานกรอกข้อมูลใน Inspector ไม่ใช่งานโค้ด |

**ช่องว่าง GDD ที่ยังเปิดอยู่:** S9, S10, S11, S12, S13 · S14/S15 เลื่อนโดยเจตนา (D4/D1)

### 6.2 ⚠️ สิ่งที่ต้องทำด้วยมือใน Unity

1. **`CocktailSystem.prefab` มี OnClick ที่ชี้ไปเมธอดที่ถูกลบ** — ปุ่มหนึ่งเรียก `AddIce()`
   แบบไม่มี argument (`m_Mode: 1`) ซึ่งถูกลบเพราะ body ว่าง (ไม่เคยทำอะไรอยู่แล้ว)
   ให้เปลี่ยนไปเรียก `AddIce(bool)` แทน
   *ผลกระทบตอนนี้: ไม่มี* — prefab นี้ไม่ถูกอ้างถึงจากซีนไหนเลย (orphan)
   ในซีนจริงทั้งหมดใช้ `m_Mode: 6` ซึ่งชี้ไป `AddIce(bool)` อยู่แล้ว จึงไม่กระทบ

2. **ซีนใช้ชุดสูตรคนละชุด** — `New Drag Drop System.unity` และ `GamePlayScene.unity` ชี้ไป
   `Demo_Normal_Cocktail` (6 สูตร) ส่วน `GamePlayScene 1.unity` ใช้ `Normal_Cocktail` (26 สูตร)
   **ต้องสลับเป็น `Normal_Cocktail` ก่อนเล่นทดสอบ D7** ไม่งั้นค่าที่ยืนยันจะใช้ไม่ได้

3. **`_specialCocktailRepository` ยังว่าง** — ผูก `Specia_Cocktail.asset` เข้าไปได้เลยถ้าต้องการ
   (ตอนนี้ asset เองก็ยังไม่มีสูตร จึงยังไม่ต่างกัน)

### 6.3 ⚠️ เกมจะยากขึ้นทันทีที่รัน

การแก้ S1 เปลี่ยนจาก "นับชนิดที่ผิด" เป็น "รวมส่วนต่าง" — **เครื่องดื่มจำนวนมากที่เคยได้
`Acceptable` จะกลายเป็น `Fail` ทันที** ตัวอย่างที่รันไว้ใน §2: แก้วที่เคยได้ 2 คะแนน (ผ่าน)
ตอนนี้ได้ 12 (ตก) ค่า `MaxTolerance = 3` ยังไม่ผ่านการเล่นทดสอบ — ดู D7 ในแผน §10.3

---

## 7. ตัวเลขจริง (ไม่ใช่ตัวเลขที่แผนประมาณไว้)

| ตัวชี้วัด | ก่อน | หลัง |
|---|---:|---:|
| ตระกูลเมธอดที่เขียนซ้ำ 3 ก๊อป | 3 ตระกูล (9 เมธอด) | 0 (3 generic) |
| การสแกนสูตรต่อการอัปเดต shaker 1 ครั้ง | 5 | **1** |
| จุดที่ hardcode `errors <= 2` | 6 | **1** const |
| จุดที่ hardcode เพดาน `10` | 2 | **1** const |
| ไฟล์ที่ยาวเกิน 200 บรรทัด | 3 | **1** (`YarnInterface.cs` — Phase 5) |
| `FindFirstObjectByType` ต่อการสั่ง 1 ครั้ง | 1 | **0** |
| กติกา GDD ที่ implement ไม่ตรง | 14 | **7** (5 ปิดเต็ม, 2 ปิดบางส่วน) |

### บรรทัดโค้ด — เพิ่มขึ้น ไม่ได้ลดลง

| | code-only |
|---|---:|
| `UtilityDrink.cs` (เดิม) | 262 |
| `Domain/` 11 ไฟล์ (ใหม่) | **329** |

**แผนประมาณไว้ว่าจะลดลงเหลือ ~1,550 บรรทัด — รอบนี้ไม่ถึง และไม่ควรถึง** เหตุผล 2 ข้อ:

1. **Phase 2 เพิ่มกติกาที่ไม่เคยมีในโค้ด** — `PricingRules` (§18.1 + §18.2),
   `SatisfactionEvaluator` เคส 3/4, `RecipeMatch`, `DrinkColorBlender`, `MatchAgainst`
   ทั้งหมดนี้คือ GDD ที่โค้ดไม่เคย implement บรรทัดที่เพิ่มคือฟีเจอร์ที่ขาด ไม่ใช่ความอ้วน
2. **การลดบรรทัดก้อนใหญ่อยู่ใน Phase 3–5** ซึ่งยังไม่ได้ทำ — `CocktailSystemManager` หายทั้งคลาส,
   ย้าย debug block ~90 บรรทัดออกไป `#if UNITY_EDITOR`, แยก `CocktailShakerData`

ตัวเลขที่ดีขึ้นจริงในรอบนี้คือ **ความซ้ำ, จำนวนการสแกน และความตรงกับ GDD** ไม่ใช่จำนวนบรรทัด

---

## 8. หมายเหตุ / เรื่องที่ต้องตัดสินเพิ่ม

### 8.1 ความกำกวมใน GDD ที่เจอตอนเขียนโค้ด

**§18 วัด deviation เทียบกับสูตรไหน?** §17 คำนวณ best-match ทั้งฐานข้อมูลเพื่อกำหนด*ตัวตน*
ของเครื่องดื่ม (ชื่อ สี ราคา) ส่วน §18 พูดถึงแค่คำว่า "deviation" เฉย ๆ

อ่านตามตัวอักษร: ถ้าผู้เล่นชง Negroni ได้เป๊ะทั้งที่ลูกค้าสั่ง Martini → best-match deviation = 0
→ §18 เคส 1 → **Perfect** ซึ่งไม่น่าใช่เจตนา

**แก้ไปแบบนี้:** §18 วัดเทียบ**สูตรที่ลูกค้าสั่ง** (`DrinkDeviation.MatchAgainst`) ส่วน best-match
ใช้กำหนดตัวตนอย่างเดียว — ตรงกับที่โค้ดเดิมทำ (`CalculateSatisfaction(served, target)`)
เขียน `NOTE(design)` กำกับไว้ที่เมธอดแล้ว **ควรยืนยันกับ design**

### 8.2 GDD errata ที่ยังต้องแก้เอกสาร

ตามแผน §12 — ยังไม่ได้แก้ `GDD_Bar410_Master.md` เพราะอยู่นอก repo นี้:

- **E1** §15.1/§16 — บันทึกโครงสร้าง 3 ลิสต์ตามจริง (D6) พร้อมกำกับต้นทุนการเพิ่มหมวดใหม่
- **E2** §19.1 — ตัด `relationshipValue` ออกจาก `CustomerSO` (D8)

### 8.3 working tree

ตอนเริ่มงาน working tree ยังค้างการลบ `BookUI.cs` / `GameLoopManager.cs` และการย้าย
`NPC_Base.cs` อยู่ (แผน §9.4 แนะนำให้เคลียร์ก่อน) — **ไม่ได้เคลียร์ให้ เพราะเป็นงานที่ค้างไว้ก่อนหน้า
และไม่ใช่ของ refactor นี้** git diff ยังแยกออกจากกันได้เพราะคนละไฟล์ แต่ตอน commit ควรแยก commit

---

## 9. ไฟล์ที่เปลี่ยน

**สร้างใหม่ 12 ไฟล์**

```
Cocktail/CompositeDrinkRepository.cs
Cocktail/Domain/AlcoholClassifier.cs      Cocktail/Domain/DrinkFlagResolver.cs
Cocktail/Domain/DrinkBuilder.cs           Cocktail/Domain/DrinkFormatter.cs
Cocktail/Domain/DrinkColorBlender.cs      Cocktail/Domain/DrinkQuery.cs
Cocktail/Domain/DrinkDeviation.cs         Cocktail/Domain/IngredientMath.cs
Cocktail/Domain/PricingRules.cs           Cocktail/Domain/RecipeMatch.cs
Cocktail/Domain/SatisfactionEvaluator.cs
```

**แก้ 11 ไฟล์** — `CocktailSystemManager.cs`, `CocktailSystemManager.YarnInterface.cs`,
`CocktailShakerData.cs`, `CocktailShaker.cs`, `DrinkIngredients.cs`, `IDrinkInterfaces.cs`,
`SO_CocktailList.cs`, `S_Drink.cs`, `DebugCocktail.cs`, `Enum_Class.cs`,
`IngredientButtonUI.cs`, `VisualizeCocktail.cs`

**ลบ 1 ไฟล์** — `UtilityDrink.cs` (+ `.meta`)

**ไม่แตะเลย** — ทุกไฟล์ `.unity`, `.prefab`, `.asset` และ `.meta` ของไฟล์เดิม
