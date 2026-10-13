# Bar410 — Cocktail System: หน้าที่ของแต่ละไฟล์

**Date:** 2026-08-21 · **Branch:** `GameLoop/main`
**คู่กับ:** `Bar410_CocktailSystem_Refactor_Report.md` · `Bar410_CocktailSystem_Manual_Setup.md`
> 📍 **เริ่มที่นี่ถ้าเพิ่งรับงานต่อ:** `Bar410_CocktailSystem_HANDOFF.md`

เอกสารอ้างอิงว่าไฟล์ไหนทำอะไร และ**ควรแก้ไฟล์ไหนเมื่ออยากเปลี่ยนอะไร**
ทุกไฟล์อยู่ใต้ `Assets/[02]Script/` ยกเว้นที่ระบุ

---

## 1. แผนผังชั้น

```
┌─ Flow bridges ─────────── Hierarchical State Machine/  (Bar410.GameFlow)
│    BarSetupBridge          CocktailFlowBridge          GarnishFlowBridge (ใหม่)
│         ↓ เรียกลงมา · ห้ามมีใครเรียกขึ้นไป
├─ Yarn adapters ────────── Cocktail System/Yarn/
│    YarnTask   YarnOrders   YarnDebug   YarnVariableSync
├─ Session ──────────────── Cocktail System/Session/         (plain C#)
│    DrinkOrderContext   OrderService   DrinkScoringService   SO_Customer
├─ Scene runtime ────────── Cocktail System/Cocktail/Shaker/ (MonoBehaviour)
│    ShakerContents   ShakerVisualPresenter   ShakerPanelController
│    IngredientButtonGroup   ShakerTooltip   InteractableToggle
├─ Serving glass (ใหม่) ─── Cocktail System/Cocktail/Glass/  (MonoBehaviour + SO)
│    SO_GlassOption   E_GarnishLook   GlassShelfSlot
│    PlacedGlassInstance   GlassPlacementZone   PourSource
├─ Ingredient drag (ใหม่) ─ Cocktail System/Cocktail/Ingredients/ (MonoBehaviour)
│    IngredientHoverDetector   BottleIngredientSource
│    FruitTraySlot   FruitPieceInstance
└─ Domain ───────────────── Cocktail System/Cocktail/Domain/ (static, ไม่มี Unity lifecycle)
     DrinkDeviation   DrinkFlagResolver   SatisfactionEvaluator   PricingRules
     AlcoholClassifier   DrinkColorBlender   DrinkQuery   DrinkBuilder
     DrinkFormatter   IngredientMath   RecipeMatch
```

> **Glass/Ingredients (ใหม่ 2026-08-22):** ระบบผู้เล่นเลือกแก้วเสิร์ฟเอง + ลาก-วางวัตถุดิบ
> แทนที่ `CompatibleGlass`/`NotFix` เดิมทั้งหมด — รายละเอียดเต็มอยู่ที่
> `Docs/Bar410_GlassFreedom_ManualSetup.md` ไม่ใช่เอกสารนี้ (เอกสารนี้แค่ให้เห็นตำแหน่งในสถาปัตยกรรม)

**กฎเดียวที่ต้องจำ:** ลูกศรชี้ลงเสมอ — ชั้นล่างห้ามรู้จักชั้นบน
`Domain/` ไม่รู้จัก `MonoBehaviour` เลย และ state class ใน HSM ไม่รู้จัก `UnityEngine` เลย

---

## 2. Domain — กติกาเกม (`Cocktail System/Cocktail/Domain/`)

คลาส static ล้วน ไม่มี state ไม่มี Unity lifecycle **ทดสอบได้โดยไม่ต้องเปิดซีน**
แต่ละไฟล์ตรงกับหนึ่งหัวข้อ GDD — อยากแก้กติกาข้อไหน เปิดไฟล์นั้น

| ไฟล์ | บรรทัด | GDD § | หน้าที่ |
|---|---:|---|---|
| `DrinkDeviation.cs` | 112 | §17.1, §17.2 | **หัวใจของการตรวจเครื่องดื่ม** — `Compute` = `Σ\|r−p\|`, `FindBestMatch` สแกนทั้งฐาน, `MatchAgainst` เทียบสูตรเดียว, `MaxTolerance` |
| `RecipeMatch.cs` | 59 | §17 | struct ผลการเทียบ + enum `DrinkFlag` (`Perfect` / `Seem_Like` / `Fail`) |
| `DrinkFlagResolver.cs` | 21 | §17.3 | deviation + method + ice → `DrinkFlag` |
| `SatisfactionEvaluator.cs` | 48 | §18 | บันไดความพึงพอใจ 5 เคส · แยก Fail(a) จาก Fail(b) |
| `PricingRules.cs` | 55 | §18.1, §18.2 | ราคา `×1.5 / ×1.0 / ×0.5 / 50` + ค่าความสัมพันธ์ `+0.5 / +0.25 / 0` |
| `AlcoholClassifier.cs` | 38 | §15.2 | หน่วยแอลกอฮอล์ → `TypeOfCocktail` (`1..5` Low, `>=6` High) |
| `DrinkColorBlender.cs` | 46 | §21.1 | สีน้ำในแก้ว |
| `DrinkQuery.cs` | 42 | §15 | ยอดรวมต่อหมวด, เพดาน 10 หน่วย, `CanAdd` |
| `DrinkBuilder.cs` | 145 | §10, §17.3, §21 | **ที่เดียวที่เขียนทับ `S_Drink`** — `TryAdd*`, `ApplyRecipeIdentity`, `ApplyGlass`, `Clear` |
| `DrinkFormatter.cs` | 49 | — | ข้อความ debug และ tooltip |
| `IngredientMath.cs` | 96 | — | generic helper ที่ทำให้ 3 ลิสต์วัตถุดิบไม่ต้องมีโค้ดคนละก๊อป |

### จุดที่มักต้องแก้

| อยากทำอะไร | แก้ที่ |
|---|---|
| ปรับความยาก (เกณฑ์ Fail) | `DrinkDeviation.MaxTolerance` — **const เดียว ห้าม hardcode ที่อื่น** |
| เปลี่ยนราคา / ค่าความสัมพันธ์ | `PricingRules` |
| เปลี่ยนเกณฑ์ High/Low alcohol | `AlcoholClassifier.FromUnits` |
| เปลี่ยนเพดาน 10 หน่วย | `DrinkQuery.MaxTotalParts` |
| เปลี่ยนกติกาความพึงพอใจ | `SatisfactionEvaluator.Evaluate` |

### ⚠️ เพิ่มหมวดวัตถุดิบใหม่ (เช่น Bitters, Garnish)

`IngredientMath` ไม่รู้จักหมวดใดเป็นพิเศษ **ไม่ต้องเขียนอัลกอริทึมใหม่** แต่ต้องแก้ **จุดรวมผล 4 จุด**
ซึ่งมีคอมเมนต์ `เพิ่มหมวดใหม่: แก้ที่นี่` กำกับไว้:

`DrinkDeviation.Compute` · `DrinkQuery.GetTotalIngredient` · `DrinkBuilder.Clear` · `DrinkFormatter.GetCocktailIngredient`

บวกกับเพิ่ม enum + struct ใน `DrinkIngredients.cs` และลิสต์ใน `S_Drink.cs`

---

## 3. Data — ข้อมูลสูตรและวัตถุดิบ (`Cocktail System/Cocktail/`)

| ไฟล์ | บรรทัด | หน้าที่ |
|---|---:|---|
| `S_Drink.cs` | 73 | ScriptableObject ของเครื่องดื่ม 1 แก้ว — ใช้ทั้งเป็น**สูตร**บนดิสก์และเป็น**แก้วที่กำลังชง**ในหน่วยความจำ |
| `DrinkIngredients.cs` | 73 | struct 3 ตัว (`AlcoholIngredient` / `LiqueurIngredient` / `MixerIngredient`) + interface `IIngredientEntry<TKey>` |
| `IDrinkInterfaces.cs` | 60 | `IDrinkRepository` (แหล่งสูตร) และ `IIngredientReceiver` (อะไรที่รับวัตถุดิบได้) |
| `SO_CocktailList.cs` | 83 | คลังสูตรแบบ asset — implement `IDrinkRepository` |
| `CompositeDrinkRepository.cs` | 95 | รวมคลัง normal + special เป็นตัวเดียว · สุ่ม uniform บน union |
| `E_Cocktail.Drink.cs` | 105 | enum ฝั่งเครื่องดื่ม — `BaseSpirit`, `Liqueur`, `Mixer`, `GlassType`, `Method`, `TypeOfCocktail`, `Satisfaction` |

> ⚠️ **ห้ามเปลี่ยนชื่อคลาส `S_Drink` และ `SO_CocktailList`** — มี asset อ้างอยู่ 27 ไฟล์
> ⚠️ **ห้ามสลับลำดับสมาชิกใน `Satisfaction`** — ค่าตัวเลขถูกเขียนลง Yarn `$satisfaction`

---

## 4. Shaker — ของในซีน (`Cocktail System/Cocktail/Shaker/`)

เดิมทั้งหมดนี้อัดอยู่ใน `CocktailShakerData` คลาสเดียว

| ไฟล์ | บรรทัด | หน้าที่เดียวของมัน |
|---|---:|---|
| `ShakerContents.cs` | 140 | **เครื่องดื่มที่อยู่ในแก้ว** — สร้าง/ทำลาย instance, เติมวัตถุดิบ, ตั้ง method/ice, `Clear`, `UpdateIdentity` · ยิง **UnityEvent** `Changed` / `Cleared` / `IdentityResolved` — designer ผูกเองได้ใน Inspector · โค้ดใช้ `AddListener` / `RemoveListener` |
| `ShakerVisualPresenter.cs` | 92 | **ภาพของแก้ว** — สีน้ำ, sprite แก้ว/น้ำแข็ง, `WaterSlosh` · ฟัง event จาก `ShakerContents` |
| `ShakerPanelController.cs` | 74 | **สิทธิ์เปิดแผง** Method / AddIce / Serve · `LockAll` / `ResetPermissions` ให้ HSM สั่งได้ |
| `IngredientButtonGroup.cs` | 67 | **roster ของ object ที่เปิด/ปิดพร้อมกัน** · ใช้ 2 ตัว (วัตถุดิบ / หนังสือ) · `SetRoster` ให้ `BarSetupBridge` เปลี่ยนรายการได้ตอนรัน |
| `InteractableToggle.cs` | 37 | **จุดเดียวที่รู้ว่า "เปิดให้กดได้" แปลว่าต้องแตะ component ไหน** — เพิ่ม component ชนิดใหม่ แก้ที่นี่ที่เดียว · ตั้งแต่ 2026-08-22 มี `ApplyPrepareBarPhase`/`ApplyPrepareDrinksPhase` เป็นชุดค่าสุดท้ายต่อเฟส HSM ด้วย (แทน "เปิดหมดแล้วปิดย้อนหลังบางตัว") |
| `ShakerTooltip.cs` | 17 | ข้อความ tooltip ตอน hover |

> ⚠️ `SO_GlassVisualTable.cs` ยังอยู่ในโฟลเดอร์นี้แต่**ไม่ใช่ระบบแก้วปัจจุบันแล้ว** — เก็บไว้เฉพาะให้
> `CocktailShakerData` (legacy shim) เรียกใช้ได้ ระบบแก้วจริงตอนนี้คือ `SO_GlassOption` ใน §4b

## 4b. Glass — ผู้เล่นเลือกแก้วเสิร์ฟเอง (`Cocktail System/Cocktail/Glass/`) — ใหม่ 2026-08-22

แทนที่ `S_Drink.CompatibleGlass`/`NotFix` เดิมทั้งหมด (ลบฟิลด์ออกจาก `S_Drink` แล้ว) รายละเอียด
setup เต็มอยู่ที่ `Docs/Bar410_GlassFreedom_ManualSetup.md`

| ไฟล์ | หน้าที่เดียวของมัน |
|---|---|
| `SO_GlassOption.cs` | asset หนึ่งใบ = แก้วหนึ่งแบบ (sprite + garnish look + placed prefab) |
| `E_GarnishLook.cs` | enum ลายตกแต่ง — ยังเป็น placeholder รอ design |
| `GlassShelfSlot.cs` | ตำแหน่งชั้นวาง สร้างแก้วใหม่แทนที่ทุกครั้งที่ใบเก่าถูกหยิบไป |
| `PlacedGlassInstance.cs` | แก้วที่วางอยู่บนโต๊ะจริง — ทำลายทิ้งหลังเสิร์ฟทุกครั้ง |
| `GlassPlacementZone.cs` | โซนบนโต๊ะ รับแก้วได้ใบเดียว**ทั้งซีน** (`_occupant` เป็น `static` โดยตั้งใจ) |
| `PourSource.cs` | marker บน `CocktailShaker` ให้ `GlassPlacementZone` แยกออกจากแก้ว |

## 4c. Ingredients — ลาก-วางวัตถุดิบเข้าภาชนะชง (`Cocktail System/Cocktail/Ingredients/`) — ใหม่ 2026-08-22

| ไฟล์ | หน้าที่เดียวของมัน |
|---|---|
| `IngredientHoverDetector.cs` | helper ราคาศ (ไม่ใช่ placement zone) เช็คว่าเมาส์ชี้ทับภาชนะชงอยู่ไหม |
| `BottleIngredientSource.cs` | ติดคู่ขวด — ลาก hover ทับภาชนะชงแล้วปล่อย = เท, ดีดกลับที่เดิมเสมอ — **เทสแล้ว ใช้งานได้จริง** |
| `FruitTraySlot.cs` / `FruitPieceInstance.cs` | ถาดผลไม้ — ชิ้นผลไม้เป็น child ของถาด มองไม่เห็นจนกว่าจะลาก ใช้แล้วหายเสมอ — **ยังไม่ได้ทำ/เทสในซีนจริง** |

---

## 5. Session — ออเดอร์และการให้คะแนน (`Cocktail System/Session/`)

plain C# ล้วน ไม่มี `MonoBehaviour`

| ไฟล์ | บรรทัด | หน้าที่ |
|---|---:|---|
| `DrinkOrderContext.cs` | 129 | **แหล่งความจริงเดียวของออเดอร์ปัจจุบัน** — ใครสั่ง, สั่งอะไร, โหมดไหน, ผลเป็นยังไง, ได้เงินเท่าไร, ความสัมพันธ์ขยับเท่าไร · มี enum `OrderMode` 5 โหมดตาม GDD §12 |
| `OrderService.cs` | 135 | เลือกว่าลูกค้าจะสั่งอะไร — 5 โหมด + การสุ่มตาม §19.2 |
| `DrinkScoringService.cs` | 64 | รัน §17–18 ครั้งเดียวตอนเสิร์ฟ แล้วเขียนผลลง context |
| `ICustomerPreferences.cs` | 21 | abstraction ของ "ลูกค้าคนนี้ชอบดื่มอะไร" |
| `SO_Customer.cs` | 54 | `SO_Customer` (1 ใบต่อ 1 ตัวละคร) + `SO_CustomerRoster` |

### `DrinkOrderContext` คือของสำคัญ

มันคือสิ่งที่ `Bar410_StateMachine_Implementation.md` §8 คำถามข้อ 4 ขอไว้ และเป็นตัวที่ทำให้
mini-FSM เดิมบน `CocktailSystemManager` หายไป:

| state เดิม (กระจายอยู่ 2 ไฟล์ partial) | ตอนนี้ |
|---|---|
| `_targetCocktail` | `Order.Target` |
| `_TaskDone` | `Order.IsScored` |
| `_satisfaction` | `Order.Result` |
| `_cocktailType` (ไม่เคยถูกเซ็ต — บั๊ก B1) | `Order.ServedType` |
| — (ไม่มีเลย) | `Order.OrderedType` ← ตัวที่ทำให้ GDD §18 เคส 4 ทำงานได้ |

---

## 6. Yarn adapters (`Cocktail System/Yarn/`)

| ไฟล์ | บรรทัด | หน้าที่ |
|---|---:|---|
| `YarnVariableSync.cs` | 109 | **จุดเดียวที่รู้ชื่อตัวแปร Yarn** (`$task_done`, `$satisfaction`, `$type_of_cocktail`, `$rel_<id>`) · plain class ไม่ใช่ MonoBehaviour |
| `CocktailSystemManager.YarnTask.cs` | 145 | commands: `wait_for_task`, `wait_scene`, `Can_End_Shift`, `Enable_InteractableObject`, `Reset_Variable` + `IsWaitingForTask` |
| `CocktailSystemManager.YarnOrders.cs` | 174 | functions สั่งเครื่องดื่ม 4 ตัวเดิม + `order_by_type` (โหมด 5) + function อ่านค่า |
| `CocktailSystemManager.YarnDebug.cs` | 86 | ContextMenu + ตาราง snapshot ตัวแปร · `#if UNITY_EDITOR` |

### ⚠️ สามไฟล์นี้เป็น `partial class CocktailSystemManager` เดียวกันโดยตั้งใจ

Yarn resolve **instance command** ด้วยชื่อ GameObject — `.yarn` เขียน `<<wait_for_task SystemGame>>`
ดังนั้น **component ที่ถือ command เหล่านี้ต้องอยู่บน GameObject ชื่อ `SystemGame`**
การแยกเป็น MonoBehaviour คนละตัวคือความเสี่ยงที่ไม่ได้อะไรกลับมา แยกเป็นไฟล์ partial ได้ประโยชน์
เรื่องขนาดไฟล์ครบโดยไม่เสี่ยงเลย

### Yarn API ทั้งหมด

| Command | ทำอะไร |
|---|---|
| `<<wait_for_task SystemGame>>` | ค้างบทสนทนาไว้จนเครื่องดื่มถูกเสิร์ฟและให้คะแนนแล้ว |
| `<<wait_scene 1>>` | รอแบบข้ามได้ตอน silent replay |
| `<<Can_End_Shift SystemGame>>` | เปิดปุ่มจบกะ |
| `<<Enable_InteractableObject SystemGame false>>` | เปิด/ปิดการชงทั้งหมด |
| `<<Reset_Variable SystemGame>>` | ล้างออเดอร์ + ตัวแปร Yarn |
| `<<order_customer SystemGame Cole>>` | ระบุลูกค้าของออเดอร์ถัดไป |
| `<<order_by_type SystemGame HighAlcohol>>` | **ใหม่** — GDD §12 โหมด 5 สั่งแค่ประเภท |

| Function | คืนอะไร |
|---|---|
| `Order_Cocktail_OutName(npc)` | ชื่อเครื่องดื่มที่สุ่มตามรสนิยม |
| `Order_Cocktail_OutDescription(npc)` | คำอธิบายรสชาติของ**แก้วเดียวกัน** (แก้ B2) |
| `Order_Cocktail_ByName_OutName(name)` | ชื่อ — ระบุสูตรตรง ๆ |
| `Order_Cocktail_ByName_OutDescription(name)` | คำอธิบาย — ระบุสูตรตรง ๆ |
| `order_name()` `order_flavor()` `order_type()` `order_satisfaction()` | **ใหม่** — อ่านออเดอร์ที่ตั้งไว้แล้ว |

---

## 7. ตัวเชื่อมกับซีน (`Cocktail System/Cocktail/`)

| ไฟล์ | บรรทัด | หน้าที่ |
|---|---:|---|
| `CocktailSystemManager.cs` | 164 | ผูก repository + services เข้ากับซีน · ถือ `Order`, `Orders`, `Scoring` · **ไม่มีกติกาเกมอยู่ในนี้** |
| `CocktailShakerData.cs` | 239 | ⚠️ **shim ชั่วคราว** — ไม่มี logic เหลือ ถือข้อมูลเดิมของซีนแล้ว forward |
| `CocktailShaker.cs` | 93 | ⚠️ **shim บางส่วน** — เป็น interactable จริง แต่ flag แผง UI forward ไป `ShakerPanelController` |

> shim สองตัวมีไว้เพราะซีนและ prefab 5 ที่ผูก UnityEvent ไว้กับมัน 40+ จุด
> จะลบได้หลังทำงานมือใน `Bar410_CocktailSystem_Manual_Setup.md` §5 เสร็จ

---

## 8. UI และ debug (`Cocktail System/`)

| ไฟล์ | บรรทัด | หน้าที่ |
|---|---:|---|
| `IngredientButtonUI.cs` | ~150 | ปุ่มวัตถุดิบ/วิธีชง/น้ำแข็ง · `ButtonAction` เลือกว่าปุ่มนี้ทำอะไร · คุยกับ `ShakerContents` โดยตรง (ถอยไปใช้ shim อัตโนมัติถ้าซีนยังมี) · UnityEvent `OnPoured` / `OnRejected` ให้ designer ผูกเอฟเฟกต์เอง |
| `VisualizeCocktail.cs` | ~72 | แถบวัดสัดส่วนแอลกอฮอล์/mixer · อ่าน `ShakerContents` |
| `DebugCocktail.cs` | 31 | overlay แสดงเป้าหมาย vs แก้วปัจจุบัน · `#if UNITY_EDITOR` |
| `E_Cocktail.Dialogue.cs` | 24 | enum ฝั่งบทสนทนา — `TextType`, `ConversationPhase` |

---

## 9. ตัวเชื่อม HSM (`Hierarchical State Machine/`)

| ไฟล์ | เกาะกับ state | ทำอะไร |
|---|---|---|
| `Level 1 - Game Loop/BarSetupBridge.cs` | `PrepareBarPhase` | **Enter:** ล็อกวัตถุดิบ เปิดหนังสือ ล้างแก้ว ปลดล็อกการวางของ · **Exit:** ล็อกการวาง แล้วยึด roster ของวันนั้นจากของที่ผู้เล่นวางจริง |
| `Level 3 - Prepare Drinks/CocktailFlowBridge.cs` | `PrepareDrinksPhase`, `AddIngredientState`, `ServeState` | **PrepareDrinks.Entered:** ล้างแก้ว (บังคับกติกา HSM §3.1) · **AddIngredient:** เปิด/ปิดปุ่ม · **Serve.Exited:** คิดคะแนน |

**ทั้งสองไฟล์ไม่ได้แก้ state class ใด ๆ เลย** — `StateBase` เปิด event `Entered` / `Exited` ไว้อยู่แล้ว
นี่คือแพตเทิร์นเดียวกับที่ `Bar410_Minigame_Integration_Plan.md` §3.3 วางไว้สำหรับ `MinigameFlowBridge`
(ซึ่ง**ยังไม่ได้เขียน** และ `CocktailFlowBridge` จงใจไม่แตะ minigame เพื่อไม่ให้มีเจ้าของสองคน)

---

## 10. Editor tools (`Assets/Editor/`)

| ไฟล์ | เมนู | หน้าที่ |
|---|---|---|
| `CocktailDataValidator.cs` | `Bar410 > Validate Cocktail Data` | ไล่ทุก `S_Drink` แล้วรายงานว่าอะไรขาด — `Σ != 10`, สูตรซ้ำ, ไม่มีชื่อ · เลิกเช็ค `CompatibleGlass` แล้ว (ฟิลด์ถูกลบ 2026-08-22) — `ValidateGlassTables` ที่เหลืออยู่เช็คแค่ `SO_GlassVisualTable` ของ legacy shim เท่านั้น |
| `IngredientButtonUIEditor.cs` | — | Inspector ของ `IngredientButtonUI` ซ่อนช่องที่ไม่เกี่ยวกับ action ที่เลือก |

---

## 11. ไฟล์ที่ย้ายออกจาก Cocktail System

| ไฟล์ | ไปอยู่ที่ | เพราะ |
|---|---|---|
| `BTN_2_5D.cs` | `BaseInteractable/` | ปุ่ม 2.5D ทั่วไป ไม่รู้จักค็อกเทลเลย |
| `IInputProvider.cs` | `Minigame/` | ผู้ใช้เดียวคือ `BaseMiniGame` |
| `NPC_Base.cs` | `NPC/` | การเคลื่อนที่และสีหน้าของ NPC |
| `CharacterData.cs` | `NPC/` | ข้อมูลตัวละคร (implement `ICustomerPreferences`) |
| `E_Cocktail.Character.cs` | `NPC/` | enum `Direction`, `NPC_Name` |
| `E_Cocktail.Minigame.cs` | `Minigame/` | enum `Enum_MiniGameType` |

`E_Cocktail` ยังเป็น **partial class เดียวกัน** ทั้ง 4 ไฟล์ — `using static E_Cocktail;` ใน 20 ไฟล์ยังทำงานปกติ

---

## 12. เส้นทางเดินของข้อมูลหนึ่งรอบ

```
1  ลูกค้าสั่ง       .yarn เรียก Order_Cocktail_* / order_by_type
                    → OrderService เลือกสูตร → DrinkOrderContext.BeginOrder
                    → Post-It แสดงข้อความ

2  ผู้เล่นชง        IngredientButtonUI → ShakerContents
                    → DrinkBuilder.TryAdd* (เช็กเพดาน 10 หน่วย)
                    → OnPoured / OnRejected (UnityEvent สำหรับ designer)
                    ซีนที่ยังมี CocktailShakerData จะเดินทางเก่าผ่าน shim เหมือนเดิม

3  อัปเดตตัวตน      ShakerContents.UpdateIdentity
                    → DrinkDeviation.FindBestMatch  (สแกนสูตร 1 รอบ)
                    → DrinkBuilder.ApplyRecipeIdentity  (ชื่อ ราคา ประเภท แก้ว สี)
                    → ShakerVisualPresenter.Apply

4  เสิร์ฟ           ปุ่ม Serve → CocktailSystemManager.ServeDrink
                    หรือ CocktailFlowBridge เมื่อออกจาก ServeState
                    → DrinkScoringService.Score
                        ├ DrinkDeviation.MatchAgainst  (เทียบกับสูตรที่ "สั่ง")
                        ├ SatisfactionEvaluator.Evaluate  (GDD §18)
                        └ PricingRules.Payout + RelationshipDelta
                    → DrinkOrderContext.Score

5  กลับเข้าบทสนทนา  CommitTaskResult → YarnVariableSync.WriteResult
                    → $satisfaction, $type_of_cocktail, $task_done, $rel_<id>
                    → <<wait_for_task>> ปล่อยบทสนทนาไปต่อ
```

---

## 13. คำถามที่พบบ่อย

**อยากปรับความยากของเกม** → `Domain/DrinkDeviation.cs` ค่า `MaxTolerance` (ตอนนี้ 3, ยังรอ design ยืนยัน)

**อยากเพิ่มวัตถุดิบชนิดใหม่** → เพิ่มค่าใน enum ที่ `E_Cocktail.Drink.cs` แล้วสร้างปุ่มในซีน · ไม่ต้องแตะโค้ดอื่น

**อยากเพิ่มหมวดวัตถุดิบใหม่** → ดู §2 หัวข้อ "เพิ่มหมวดวัตถุดิบใหม่" — มี 4 จุดที่มีคอมเมนต์กำกับ

**อยากเพิ่ม Yarn command** → `Yarn/CocktailSystemManager.YarnTask.cs` หรือ `YarnOrders.cs`
· ต้องอยู่บน GameObject ชื่อ `SystemGame` ถ้าเป็น instance command

**อยากให้ HSM สั่งอะไรเพิ่ม** → เพิ่มใน bridge ทั้งสองตัว **ห้ามแก้ state class**
· state ยิง event, bridge แปลเป็นผลในซีน

**เครื่องดื่มไม่เปลี่ยนสีในแก้ว** → เช็ก `ShakerContents.CurrentCocktail.waterColorTop/Bottom` และ
`ShakerVisualPresenter.Apply` — ไม่เกี่ยวกับ `CompatibleGlass` แล้ว (ฟิลด์นี้ถูกลบออกจาก `S_Drink`)

**อยากให้ผู้เล่นเลือกแก้วเสิร์ฟเอง** → ไม่ต้องตั้งอะไรที่สูตรอีกต่อไป — **ทุกสูตรเป็นแบบนั้นอยู่แล้ว
โดยอัตโนมัติ** ผู้เล่นลากแก้วจาก `GlassShelfSlot` มาวางที่ `GlassPlacementZone` เอง ดู §4b และ
`Docs/Bar410_GlassFreedom_ManualSetup.md` (ระบบ `NotFix`/`DrinkBuilder.ApplyGlass`/
`ShakerContents.SetGlass` เดิมถูกลบทั้งหมดแล้ว 2026-08-22)

**เพิ่ม component ที่ต้องเปิด/ปิดตอนล็อกการชง** → `Shaker/InteractableToggle.cs` **ที่เดียว**
