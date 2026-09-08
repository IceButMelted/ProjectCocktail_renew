# Bar410 — Cocktail System Refactor Report

**Date:** 2026-08-21
**Branch:** `GameLoop/main`
**Plan:** `Bar410_CocktailSystem_Refactor_Plan.md`
**Scope executed:** Phase 0–8 (โค้ดทั้งหมด) — **ไม่มีการแก้ซีน prefab หรือ asset ใด ๆ**
**งานมือที่เหลือ:** `Bar410_CocktailSystem_Manual_Setup.md`
**หน้าที่ของแต่ละไฟล์:** `Bar410_CocktailSystem_Architecture.md`
**Compile status:** ✅ ผ่าน ไม่มี error (ยืนยันผ่าน Unity MCP)
> 📍 **เริ่มที่นี่ถ้าเพิ่งรับงานต่อ:** `Bar410_CocktailSystem_HANDOFF.md`

> ⚠️ **บันทึกสถานะ ณ 2026-08-21** — ทุกจุดที่พูดถึง `CompatibleGlass`/`GlassType.NotFix`/
> `SO_GlassVisualTable`/S13 เป็นสถานะวันนั้น **ระบบแก้วถูกรื้อและสร้างใหม่ทั้งชุดวันที่ 2026-08-22**
> (ผู้เล่นเลือกแก้วเสิร์ฟเอง ไม่ผูกกับสูตร) ดูสถานะจริงที่ `Docs/Bar410_GlassFreedom_ManualSetup.md`
> และ `GDD_Bar410_Master.md` §21 — อย่าอ้างอิงส่วนแก้วด้านล่างเป็นสถานะปัจจุบัน

---

## 1. สรุปสั้น

ทำ 4 ส่วนจากแผน:

| Phase | งาน | สถานะ |
|---|---|---|
| 0 | ลบโค้ดตาย + แก้บั๊ก B1, B3, B6, B7, B8, B11 | ✅ เสร็จ |
| 1 | แยก `UtilityDrink.cs` → `Cocktail/Domain/` 11 ไฟล์ + generic `IngredientMath` | ✅ เสร็จ |
| 2 | ทำให้ตรง GDD — S1, S2, S3, S4, S5, S7 เต็ม · S6, S8 บางส่วน | ⚠️ เสร็จบางส่วน |
| §4.7 | `CompositeDrinkRepository` (D1) | ✅ เสร็จ |
| 3 | แยก `CocktailShakerData` → 5 component + `InteractableToggle` (B4) | ✅ โค้ดเสร็จ · ย้ายข้อมูลเป็นงานมือ |
| 4 | `DrinkOrderContext` / `OrderService` / `SO_Customer` / scoring (B2, S10, S11) | ✅ โค้ดเสร็จ · สร้าง asset เป็นงานมือ |
| 5 | แยก Yarn adapter 4 ไฟล์ + B5 + โหมดสั่งที่ 5 (S9) | ✅ เสร็จ |
| 6 | `BarSetupBridge` + `CocktailFlowBridge` | ✅ โค้ดเสร็จ · ใส่ในซีนเป็นงานมือ |
| 7 | ย้ายไฟล์ 4 ไฟล์ + แยก `Enum_Class` เป็น partial 4 ไฟล์ | ✅ เสร็จ |
| 8 | งาน data — เขียน validator แทน (ข้อมูลจริงเป็นการตัดสินใจของ design) | ⚠️ validator เสร็จ · กรอกข้อมูลเป็นงานมือ |

**Phase 3–8 ถูกเขียนโดยยึดหลักเดียว: ของเดิมต้องไม่พัง** ทุก class ที่ซีนอ้างถึงยังอยู่ครบและยังมี
เมธอดเดิมทุกตัวที่ UnityEvent ผูกไว้ ส่วนที่ต้องทำมือรวมไว้ใน `Bar410_CocktailSystem_Manual_Setup.md`

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

## 3. บั๊กที่แก้ทั้งหมด 9 ตัว

ชื่อไฟล์ในคอลัมน์ "ที่" คือชื่อ **ก่อน** refactor

| # | เฟส | ที่ | อาการเดิม | ที่แก้ |
|---|---|---|---|---|
| **B1** | 0 | `YarnInterface.cs` | `_cocktailType` ถูกกำหนดค่าแค่ `None` เท่านั้นทั้งโปรเจกต์ → `$type_of_cocktail` เขียน `0` เสมอ ทุก `<<if $type_of_cocktail ...>>` ใน Yarn เป็น branch ตาย | เปลี่ยนเป็น `_servedType` ที่ `CalculateSatisfaction()` เขียนค่าจริงลงไป |
| **B3** | 0 | `YarnInterface.cs:183` | `ResolvePostItText` เรียก `FindFirstObjectByType<Post_It_Order>()` ทุกครั้งที่สั่งเครื่องดื่ม ทั้งที่มี `_postItOrder` เป็น `[SerializeField]` อยู่แล้ว | ใช้ฟิลด์ที่ผูกไว้ + warning ถ้าไม่ได้ผูก |
| **B6** | 0 | `UtilityDrink.cs:48` | `IsValidRatio` เช็กแค่ว่ายอด **ปัจจุบัน** < 10 → เติม 5 หน่วยตอนมี 9 ได้แก้ว 14 หน่วย | `DrinkQuery.CanAdd(d, amount)` นับ amount ที่กำลังเติมด้วย |
| **B7** | 0 | `DebugCocktail.cs:22` | `"Shaker:\n" + _shaker.CurrentCocktail` ต่อ string กับ ScriptableObject → พิมพ์ชื่อ object ไม่ใช่ข้อมูลเครื่องดื่ม | ใช้ `DrinkFormatter.GetCocktailInfo()` |
| **B8** | 0 | `CocktailSystemManager.cs:37` | `Awake` เขียน `_characterData` ที่ประกาศอยู่ในไฟล์ partial อีกไฟล์ — coupling ที่มองไม่เห็น | ย้ายการประกาศมาไว้ไฟล์เดียวกับที่เขียนค่า |
| **B11** | 0 | `SystemGame.prefab` | `_normalCocktailRepository` เป็น null → `Start()` โยน NullReferenceException | `CompositeDrinkRepository` ข้าม source ที่เป็น null และ log error ที่ระบุ GameObject แทน |
| **B2** | 4 | `YarnInterface.cs:57-94` | `Order_Cocktail_OutName` กับ `OutDescription` **สุ่มใหม่ทั้งคู่** เรียกติดกันในโหนดเดียวได้คนละแก้ว — ชื่อกับคำอธิบายไม่ตรงกัน | `DrinkOrderContext` เก็บออเดอร์ไว้ ถ้ามีของลูกค้าคนเดิมที่ยังไม่ถูกให้คะแนน จะใช้ตัวเดิม |
| **B4** | 3 | `YarnInterface.cs:226-235` vs `CocktailShakerData.cs:151-163` | `EnableButtonInYarn` เรียก `SetIngredientActive` แล้ววนลูป component เดิมซ้ำอีกรอบด้วยชุดที่ครอบคลุมน้อยกว่า — ปุ่มถูกเซ็ตสองครั้งด้วยนิยามต่างกัน | `InteractableToggle.Apply` จุดเดียว ยุบลูป 3 ก๊อป |
| **B5** | 5 | `YarnInterface.cs:282` | `UpdateVariableInYarn()` เป็นทั้ง predicate ของ `WaitUntil` (โพลทุกเฟรม) และ command ที่เขียนตัวแปร 3 ตัว ปิดปุ่ม ซ่อน Post-It ทุกครั้งที่คืน true | แยกเป็น `IsTaskComplete` (query บริสุทธิ์) กับ `CommitTaskResult()` (เรียกครั้งเดียว) |

### โค้ดตายที่ลบ

| ที่ | ลบอะไร |
|---|---|
| `CocktailSystemManager.cs` | `_failCocktailSprite` (อยู่แต่ในคอมเมนต์), `Update()` ว่าง, คอมเมนต์โค้ดเก่า ~25 บรรทัด, `_normalDrinks` |
| `YarnInterface.cs` | `SetPostItText` ที่คอมเมนต์ทิ้ง, `using System.Linq` และ `using System.Runtime.CompilerServices` ที่ไม่ได้ใช้ |
| `CocktailShakerData.cs` | `Update()` ว่าง |
| `IngredientButtonUI.cs` | `ActionBehavior()` (ก๊อปของ `Invoke()` ทุกไบต์), `AddIce()` ที่ body ว่าง |
| `UtilityDrink.cs` | **ลบทั้งไฟล์** (412 บรรทัด) — ย้ายไป `Domain/` |

> ตรวจแล้วว่า `ActionBehavior` **ไม่ได้ถูกผูกใน UnityEvent ของซีนหรือ prefab ไหนเลย** จึงลบได้ปลอดภัย
> ส่วน `AddIce` ที่ผูกอยู่ใน `CocktailSystem.prefab` ดู §6.1 ข้อ 3 และคู่มืองานมือ §2.1

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

## 5b. Phase 3–8

### 5b.1 Phase 3 — ผ่า `CocktailShakerData` ออกเป็น 5 component

`Cocktail/Shaker/` :

| ไฟล์ | หน้าที่เดียวของมัน |
|---|---|
| `ShakerContents.cs` | เครื่องดื่มที่อยู่ในแก้ว — วงจรชีวิต, เติมวัตถุดิบ, method/ice, reset |
| `ShakerVisualPresenter.cs` | สี + sprite แก้ว + `WaterSlosh` |
| `IngredientButtonGroup.cs` | roster ของ object ที่เปิด/ปิดพร้อมกัน (ใช้ 2 ตัว: วัตถุดิบ / หนังสือ) |
| `ShakerPanelController.cs` | สิทธิ์เปิดแผง Method / AddIce / Serve |
| `ShakerTooltip.cs` | ข้อความ tooltip |
| `InteractableToggle.cs` | จุดเดียวที่รู้ว่า "เปิดให้ผู้เล่นกดได้" แปลว่าต้องแตะ component ไหนบ้าง |
| `SO_GlassVisualTable.cs` | ตารางลุคแก้วเป็น asset (D5) |

**B4 หายไปจริง** — เดิมมีลูป `TryGetComponent` 3 ก๊อปที่แตะ component คนละชุด
(`SetIngredientActive` 6 ชนิด, `SetBookUiActive` 7 ชนิด, `EnableButtonInYarn` 4 ชนิด และตัวหลัง
รันต่อจากตัวแรกทันที ทำให้ปุ่มถูกเซ็ตสองรอบด้วยนิยามที่ต่างกัน) ตอนนี้เหลือ `InteractableToggle.Apply`
ตัวเดียว

**`CocktailShakerData` ยังอยู่ แต่ไม่มี logic เหลือแล้ว** — เป็น shim ที่ถือข้อมูล serialize เดิมของ
ซีนไว้ ส่งต่อให้ component จริงตอน `Awake` (สร้างให้อัตโนมัติถ้ายังไม่มี พร้อมป้อนค่าจากฟิลด์เดิม)
แล้ว forward ทุกเมธอด **จึงไม่มีซีนไหนพัง และไม่มีข้อมูลไหนหาย**
ตารางลุคแก้วแบบเก่าถูกแปลงเป็นตารางชั่วคราวตอนรัน พร้อม warning ชี้ไปคู่มือ

`CocktailShaker` ก็ทำแบบเดียวกัน — flag แผง UI ย้ายไป `ShakerPanelController`
ส่วน `SetCanShow*` / `ToggleUI` ที่ซีนผูกไว้ 19 จุดยังอยู่ครบและ forward ต่อ

### 5b.2 Phase 4 — `DrinkOrderContext` และ session layer

`Cocktail/Session/` :

| ไฟล์ | หน้าที่ |
|---|---|
| `DrinkOrderContext.cs` | สถานะออเดอร์ทั้งวงจร — ใครสั่ง, สั่งอะไร, ผลเป็นยังไง, ได้เงินเท่าไร |
| `OrderService.cs` | GDD §12 ทั้ง 5 โหมด + การสุ่มตาม §19.2 |
| `DrinkScoringService.cs` | รัน §17–18 ครั้งเดียวตอนเสิร์ฟ แล้วเขียนผลลง context |
| `ICustomerPreferences.cs` | abstraction ของ "ลูกค้าคนนี้ชอบอะไร" |
| `SO_Customer.cs` | `SO_Customer` + `SO_CustomerRoster` ตาม GDD §19.1 |

**นี่คือ `DrinkOrderContext` ที่ `Bar410_StateMachine_Implementation.md` §8 คำถามข้อ 4 ขอไว้** และ
มันดูดเอา state 5 ตัวที่เคยกระจายอยู่บน `CocktailSystemManager` เข้ามารวมกัน (`_targetCocktail`,
`_TaskDone`, `_satisfaction`, `_cocktailType`, `IsWaitingForTask`)

| แก้ | อะไร |
|---|---|
| **B2** | `Order_Cocktail_OutName` กับ `OutDescription` เคยสุ่มใหม่ทั้งคู่ — เรียกติดกันได้คนละแก้ว ตอนนี้ถ้ามีออเดอร์ค้างของลูกค้าคนเดิมที่ยังไม่ถูกให้คะแนน จะใช้ตัวเดิม |
| **S10** | §19.2 — รวมสูตรที่เข้าเกณฑ์ทั้งหมดก่อนแล้วค่อยสุ่ม uniform แทนการสุ่มประเภทก่อน |
| **S11** | `SO_Customer` / `SO_CustomerRoster` · `CharacterData` implement `ICustomerPreferences` ต่อไปได้ |
| **§18.2** | `PricingRules.RelationshipDelta` + `YarnVariableSync.ApplyRelationshipDelta` — เดิมไม่มีเลย |

### 5b.3 Phase 5 — Yarn adapter

`CocktailSystemManager.YarnInterface.cs` (391 บรรทัด) → 4 ไฟล์:

| ไฟล์ | เนื้อหา |
|---|---|
| `Yarn/YarnVariableSync.cs` | **plain class** — จุดเดียวที่รู้ชื่อตัวแปร Yarn ทั้ง 3 ตัว |
| `Yarn/CocktailSystemManager.YarnTask.cs` | commands: `wait_for_task`, `wait_scene`, `Can_End_Shift`, `Enable_InteractableObject`, `Reset_Variable` |
| `Yarn/CocktailSystemManager.YarnOrders.cs` | functions สั่งเครื่องดื่ม + โหมด 5 (S9) |
| `Yarn/CocktailSystemManager.YarnDebug.cs` | ContextMenu + ตาราง snapshot, `#if UNITY_EDITOR` |

**ยังเป็น `partial class` เดียวกัน** — Yarn resolve instance command ด้วยชื่อ GameObject ดังนั้น
การแยกเป็น MonoBehaviour คนละตัวจะทำให้ `<<wait_for_task SystemGame>>` เสี่ยงพัง แยกเป็นไฟล์
partial ได้ประโยชน์เรื่องขนาดไฟล์ครบโดยไม่มีความเสี่ยงเลย

**B5 แก้แล้ว** — เดิม `UpdateVariableInYarn()` เป็นทั้ง predicate ของ `WaitUntil` และ command
ที่เขียนตัวแปร 3 ตัว ปิดปุ่ม และซ่อน Post-It ทุกครั้งที่คืน true ตอนนี้แยกเป็น
`IsTaskComplete` (query บริสุทธิ์) กับ `CommitTaskResult()` (เรียกครั้งเดียว)

**S9 เพิ่มแล้ว** — `<<order_by_type>>`, `<<order_customer>>` และ function
`order_name()` / `order_flavor()` / `order_type()` / `order_satisfaction()`
**ชื่อ command/function เดิมทั้ง 4 ตัวไม่ถูกแตะ** `.yarn` ที่มีอยู่ไม่ต้องแก้

### 5b.4 Phase 6 — HSM bridges

| ไฟล์ | เกาะกับ | ทำอะไร |
|---|---|---|
| `Level 1 - Game Loop/BarSetupBridge.cs` | `PrepareBarPhase` | Enter: ล็อกวัตถุดิบ เปิดหนังสือ ล้างแก้ว ปลดล็อกการวาง · Exit: ล็อกการวาง แล้ว**ยึด roster ของวันนั้นจากของที่ผู้เล่นวางจริง** |
| `Level 3 - Prepare Drinks/CocktailFlowBridge.cs` | `PrepareDrinksPhase`, `AddIngredientState`, `ServeState` | `PrepareDrinks.Entered` ล้างแก้ว (บังคับกติกา HSM §3.1) · `AddIngredient` เปิด/ปิดปุ่ม · `Serve.Exited` คิดคะแนน |

**ไม่ต้องแก้ไฟล์ใดใน `Hierarchical State Machine/Base/` หรือ state class เลยแม้แต่บรรทัดเดียว**
เพราะ `StateBase` เปิด `Entered`/`Exited` ไว้อยู่แล้ว — เป็นไปตามที่แผน §6 คาดไว้

`Serve.Exited` ปิด TODO ที่ `ServeState.cs:29` และ `Bar410_StateMachine_Implementation.md` §3.2
ทิ้งไว้ · มี guard `if (Order.IsScored) return;` กันการคิดคะแนนซ้ำกับปุ่ม Serve เดิม

`CocktailFlowBridge` **จงใจไม่แตะ minigame** — เป็นงานของ `MinigameFlowBridge` ตาม
`Bar410_Minigame_Integration_Plan.md` §3.3 ทำทั้งคู่จะกลายเป็นมีเจ้าของสองคน
เตรียม `ShakerContents.RequiredMinigame` ไว้ให้ bridge นั้นเรียกแล้ว

### 5b.5 Phase 7 — ย้ายไฟล์

ย้ายด้วย `git mv` พร้อม `.meta` ทุกครั้ง → git บันทึกเป็น pure rename, GUID ไม่เปลี่ยน,
prefab/scene ที่อ้างถึงไม่กระทบ

| จาก | ไป |
|---|---|
| `Cocktail System/BTN_2_5D.cs` | `BaseInteractable/` |
| `Cocktail System/IInputProvider.cs` | `Minigame/` |
| `Cocktail System/NPC_Base.cs` | `NPC/` |
| `Cocktail System/CharacterData.cs` | `NPC/` |

`Enum_Class.cs` แยกเป็น 4 ไฟล์ตามโดเมน แต่ **ยังเป็น `partial class E_Cocktail` เดียวกัน** —
`using static E_Cocktail;` ใน 20 ไฟล์ไม่ต้องแตะ

### 5b.6 Phase 8 — validator แทนการกรอกข้อมูล

`Assets/Editor/CocktailDataValidator.cs` → เมนู **`Bar410 > Validate Cocktail Data`**
ไล่ทุก `S_Drink` และทุก `SO_GlassVisualTable` แล้วรายงานว่าอะไรขาด **ไม่แต่งข้อมูลให้เอง**
เพราะแก้วไหนคู่กับสูตรไหนเป็นการตัดสินใจของ design

ผลรันจริง:

```
assets=26   Σ ingredients != 10 -> 0   CompatibleGlass=None -> 20
glass: Hi_ball x3, Rocks x2, Magrita x1, None x20
สูตรซ้ำกันเป๊ะ: ไม่มี
```

**ข่าวดี: S12 ผ่านอยู่แล้ว** — ทั้ง 26 สูตรรวมได้ 10 หน่วยพอดีตาม GDD §15 และไม่มีสูตรซ้ำ
เหลือแค่ G2 (20 ใบไม่มี `CompatibleGlass`) ซึ่งกู้อัตโนมัติไม่ได้เพราะข้อมูลเดิมหายไปแล้วจริง ๆ

### 5b.7 ผลข้างเคียงของ S3 ที่วัดได้จริง

ไล่ดูทั้ง 26 สูตรแล้ว มี **2 ใบที่มีแอลกอฮอล์ 5 หน่วยพอดี** คือ `23_JungleBird` และ `44_Siesta`
ทั้งสองใบ **design กำหนดไว้เป็น `LowAlcohol` อยู่แล้ว** ในขณะที่โค้ดเดิมคำนวณได้ `HighAlcohol`

→ การแก้ S3 ตาม GDD ทำให้ค่าที่คำนวณตรงกับที่ design เขียนไว้ **ยืนยันว่า GDD §15.2 ถูก และโค้ดเดิมผิด**

---


## 6. สิ่งที่ยังเหลือ

### 6.1 งานมือใน Unity — `Bar410_CocktailSystem_Manual_Setup.md`

โค้ดครบแล้ว **เกมเล่นได้เหมือนเดิมโดยไม่ต้องทำอะไรเลย** เพราะทุกจุดมี compatibility shim
ที่เหลือคือเก็บกวาดให้สถาปัตยกรรมใหม่ทำงานเต็มตัว เรียงตามความสำคัญ:

| # | งาน | จำเป็นแค่ไหน |
|---|---|---|
| 1 | สลับซีนจาก `Demo_Normal_Cocktail` (6 สูตร) → `Normal_Cocktail` (26) | **ก่อนเล่นทดสอบ D7** |
| 2 | ยืนยันค่า `MaxTolerance` (D7) | **ก่อนส่งงาน** |
| 3 | ลบ component Missing Script ของ `BookUI` 5 จุด | เก็บกวาด — ไม่กระทบบิลด์ |
| 4 | กรอก `CompatibleGlass` 20 สูตร (G2) | แก้วไม่เปลี่ยนลุคถ้าไม่ทำ |
| 5 | สร้าง `SO_GlassVisualTable` แล้วผูกทุกซีน (D5) | shim รองรับไปก่อนได้ |
| 6 | ใส่ component ใหม่ 5 ตัวด้วยมือ แล้วลบ `CocktailShakerData` | shim รองรับไปก่อนได้ |
| 7 | สร้าง `SO_Customer` + roster (S11) | `CharacterData` ใช้ต่อได้ |
| 8 | ใส่ `BarSetupBridge` + `CocktailFlowBridge` ใน `[GameLoop]` | **flow ยังไม่ขับ Cocktail System จนกว่าจะทำ** |

### 6.2 ช่องว่าง GDD ที่ยังเปิด

| # | GDD | ติดอะไร |
|---|---|---|
| **S6** | §17.3 Fail(b) → ชื่อสุ่ม | ยังไม่มีคลังชื่อที่ designer เขียน ตอนนี้ใช้ `"???"` |
| **S8** | §21.1 Fail(b) → `BlendIngredientColors` | ไม่มีข้อมูลสีต่อวัตถุดิบใน data model เลย |
| **S13** | §10/§21 ผู้เล่นเลือกแก้วเอง | ✅ ปิดจริงแล้ว 2026-08-22 ด้วยแนวทางอื่นจากที่ระบุไว้ตอนแรก — ดูหมายเหตุตอนต้นไฟล์ |
| S14 | §16 `MixMethod.Build` | เลื่อนตาม D4 — รอ Building minigame |
| S15 | §16 `unlockedByDefault` | เลื่อนตาม D1 — ใช้ระบบ 2 asset แทน |

S12 **ปิดแล้วโดยไม่ต้องแก้อะไร** — validator ยืนยันว่าทั้ง 26 สูตรรวมได้ 10 พอดีอยู่แล้ว

ทั้งหมดมี `TODO(design, ...)` กำกับไว้ในโค้ด

### 6.3 ⚠️ เกมจะยากขึ้นทันทีที่รัน

การแก้ S1 เปลี่ยนจาก "นับชนิดที่ผิด" เป็น "รวมส่วนต่าง" — เครื่องดื่มจำนวนมากที่เคยได้
`Acceptable` จะกลายเป็น `Fail` ค่า `MaxTolerance = 3` ยังไม่ผ่านการเล่นทดสอบ (D7)

---


## 7. ตัวเลขจริง (ไม่ใช่ตัวเลขที่แผนประมาณไว้)

| ตัวชี้วัด | ก่อน | หลัง |
|---|---:|---:|
| ตระกูลเมธอดที่เขียนซ้ำ 3 ก๊อป | 3 ตระกูล (9 เมธอด) | 0 (3 generic) |
| การสแกนสูตรต่อการอัปเดต shaker 1 ครั้ง | 5 | **1** |
| จุดที่ hardcode `errors <= 2` | 6 | **1** const |
| จุดที่ hardcode เพดาน `10` | 2 | **1** const |
| ไฟล์ที่ยาวเกิน 200 บรรทัด | 3 | **1** (`CocktailShakerData.cs` — shim ที่จะถูกลบหลังงานมือ) |
| ไฟล์ที่ยาวเกิน 150 บรรทัด | 4 | **3** |
| จำนวนไฟล์ใน Cocktail System | 17 | 40 |
| ลูป TryGetComponent เปิด/ปิด interactable | 3 ก๊อป (คนละชุด component) | **1** (`InteractableToggle`) |
| `FindFirstObjectByType` ต่อการสั่ง 1 ครั้ง | 1 | **0** |
| กติกา GDD ที่ implement ไม่ตรง | 15 | **5** (S6, S8, S13 บางส่วน/ค้าง · S14, S15 เลื่อนโดยเจตนา) |

### บรรทัดโค้ด — เพิ่มขึ้น ไม่ได้ลดลง

| | ก่อน | หลัง |
|---|---:|---:|
| จำนวนไฟล์ | 17 | 40 |
| บรรทัดรวม (รวมคอมเมนต์) | 2,104 | 3,270 |
| ไฟล์ยาวสุด | 412 (`UtilityDrink.cs`) | 239 (`CocktailShakerData.cs` — shim ชั่วคราว) |

**แผนประมาณไว้ว่าจะลดลงเหลือ ~1,550 บรรทัด — ไม่ถึง และไม่ควรถึง** สามเหตุผล:

1. **เพิ่มกติกา GDD ที่โค้ดไม่เคยมี** — `PricingRules` (§18.1 + §18.2), `SatisfactionEvaluator`
   เคส 3/4, `RecipeMatch`, `DrinkColorBlender`, `OrderService` โหมด 5, `DrinkOrderContext`
   บรรทัดพวกนี้คือฟีเจอร์ที่ขาด ไม่ใช่ความอ้วน
2. **`CocktailShakerData` และ `CocktailShaker` ยังอยู่ในฐานะ shim** — บรรทัดที่แผนคิดว่าจะหาย
   จะหายจริงหลังงานมือใน §6.1 ข้อ 6 เสร็จ
3. **คอมเมนต์อธิบายเหตุผล** — ทุกที่ที่โค้ดต่างจาก GDD หรือแก้บั๊กเก่ามีคำอธิบายกำกับว่าทำไม
   เพื่อไม่ให้คนถัดไป "แก้กลับ" โดยไม่รู้

ตัวเลขที่ดีขึ้นจริงคือ **ความซ้ำ (9 เมธอด → 3, ลูป 3 ก๊อป → 1), จำนวนการสแกน (5 → 1),
ขนาดไฟล์สูงสุด (412 → 239) และความตรงกับ GDD (ไม่ตรง 15 → 5)** ไม่ใช่จำนวนบรรทัด

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

รายละเอียดว่าแต่ละไฟล์ทำอะไร อยู่ใน `Bar410_CocktailSystem_Architecture.md`

**สร้างใหม่ 30 ไฟล์**

```
Cocktail/Domain/            11 ไฟล์  กติกาเกมทั้งหมด (GDD §15, §17, §18, §21.1)
Cocktail/Shaker/             7 ไฟล์  ของในซีนที่ผ่าออกจาก CocktailShakerData
Session/                     5 ไฟล์  DrinkOrderContext, OrderService, scoring, SO_Customer
Yarn/                        4 ไฟล์  adapter + YarnVariableSync
Cocktail/CompositeDrinkRepository.cs
Hierarchical State Machine/  2 ไฟล์  BarSetupBridge, CocktailFlowBridge
Assets/Editor/CocktailDataValidator.cs
```

**แยกไฟล์ (เนื้อหาเดิม โครงใหม่)**

| เดิม | เป็น |
|---|---|
| `UtilityDrink.cs` (412) | `Cocktail/Domain/` 11 ไฟล์ |
| `CocktailSystemManager.YarnInterface.cs` (391) | `Yarn/` 4 ไฟล์ |
| `Enum_Class.cs` (126) | `E_Cocktail.*.cs` 4 ไฟล์ (partial class เดียวกัน) |

**ย้ายที่อยู่ (GUID คงเดิม, git บันทึกเป็น pure rename)**

`BTN_2_5D.cs` → `BaseInteractable/` · `IInputProvider.cs` → `Minigame/` ·
`NPC_Base.cs`, `CharacterData.cs` → `NPC/`

**แก้ไข 10 ไฟล์** — `CocktailSystemManager.cs`, `CocktailShakerData.cs`, `CocktailShaker.cs`,
`DrinkIngredients.cs`, `IDrinkInterfaces.cs`, `SO_CocktailList.cs`, `S_Drink.cs`,
`DebugCocktail.cs`, `IngredientButtonUI.cs`, `VisualizeCocktail.cs`

**ลบ 2 ไฟล์** — `UtilityDrink.cs`, `CocktailSystemManager.YarnInterface.cs`

**ไม่แตะเลย** — ทุกไฟล์ `.unity`, `.prefab`, `.asset` และ `.meta` ของไฟล์เดิม

---

## 10. เอกสารชุดนี้

| ไฟล์ | ใช้เมื่อ |
|---|---|
| `Bar410_CocktailSystem_Refactor_Plan.md` | อยากรู้ว่าตัดสินใจอะไรไปบ้างและเพราะอะไร (§10, §13 Decision Log) |
| `Bar410_CocktailSystem_Refactor_Report.md` | เอกสารนี้ — ทำอะไรไปบ้าง ผลตรวจสอบ ตัวเลข |
| `Bar410_CocktailSystem_Refactor_Summary.md` | อ่านสรุปสั้น |
| `Bar410_CocktailSystem_Architecture.md` | **หาว่าไฟล์ไหนทำอะไร / จะแก้อะไรต้องเปิดไฟล์ไหน** |
| `Bar410_CocktailSystem_Manual_Setup.md` | ลงมือทำงานมือใน Unity |
