# Bar410 — Cocktail System Refactor & HSM Merge Plan

**Date:** 2026-08-21
**Branch:** `GameLoop/main`
**Scope:** `Assets/[02]Script/Cocktail System/` (17 ไฟล์ / 2,104 บรรทัด)
**เอกสารพี่น้อง:** `Bar410_StateMachine_Implementation.md`, `Bar410_Minigame_Integration_Plan.md`
**Source of truth ด้านกติกา:** `~/WorkSpace/Bar410/GDD_Bar410_Master.md` (§7, §12, §17, §18, §19, §21, §24)

เป้าหมาย 4 ข้อ:

1. **กระชับโค้ด** — ลบโค้ดตาย, ยุบโค้ดซ้ำ 3 ชุด, แก้บั๊กที่เจอระหว่างอ่าน
2. **แยกหน้าที่ตาม SOLID** — `CocktailSystemManager` (518 บรรทัด / 13 หน้าที่) และ `CocktailShakerData` (212 บรรทัด / 5 หน้าที่) ถูกซอยเป็นคลาสเล็กที่มีหน้าที่เดียว
3. **ทำให้ตรง GDD** — อัลกอริทึมตรวจเครื่องดื่มและระบบความพึงพอใจที่ใช้อยู่ **ไม่ตรงกับ GDD §17–18** ดู §2.5
4. **เตรียม merge เข้า HSM** — สร้าง seam ให้ `PrepareBarPhase` (Level 1) และ `PrepareDrinksPhase` / `ServeState` (Level 2–3) เกาะได้ โดยไม่ให้ flow layer รู้จัก `UnityEngine`

> เอกสารนี้เป็น **แผนอย่างเดียว** ยังไม่มีการแก้โค้ดจริง

> ⚠️ **ข้อค้นพบสำคัญ:** ตอนแรกงานนี้ถูกมองว่าเป็น refactor เชิงโครงสร้าง แต่พอเทียบกับ GDD แล้วพบ
> ช่องว่างเชิงกติกา **14 จุด** (§2.5) ซึ่งบางจุดทำให้เครื่องดื่มที่ผิดสูตรอย่างหนักได้ผลเป็น `Acceptable`
> ช่องว่างเหล่านี้อยู่ในไฟล์เดียวกับที่แผนจะผ่าอยู่แล้ว — ควรแก้พร้อมกัน ไม่ใช่แยก task

---

## 1. สภาพปัจจุบัน

### 1.1 Inventory

| ไฟล์ | บรรทัด | หน้าที่ที่แท้จริง | อยู่ถูกโฟลเดอร์? |
|---|---:|---|---|
| `Cocktail/CocktailSystemManager.cs` | 143 | repository + target cocktail + debug | ✔ |
| `Cocktail/CocktailSystemManager.YarnInterface.cs` | 375 | Yarn functions/commands + task FSM + var sync + debug UI | ✔ |
| `Cocktail/CocktailShakerData.cs` | 212 | drink runtime + events + button roster + glass visual + tooltip | ✔ |
| `Cocktail/CocktailShaker.cs` | 88 | interactable + UI panel flags | ✔ |
| `Cocktail/S_Drink.cs` | 73 | ScriptableObject data | ✔ |
| `Cocktail/SO_CocktailList.cs` | 59 | `IDrinkRepository` | ✔ |
| `Cocktail/IDrinkInterfaces.cs` | 53 | 2 interfaces | ✔ |
| `Cocktail/DrinkIngredients.cs` | 34 | 3 structs | ✔ |
| `UtilityDrink.cs` | 412 | query + mutate + compare + format (4 หน้าที่) | ✔ |
| `Enum_Class.cs` | 125 | enum 11 ตัว (cocktail + NPC + minigame + dialogue) | ✔ (ดู §5.6) |
| `IngredientButtonUI.cs` | 113 | ปุ่มวัตถุดิบ | ✔ |
| `VisualizeCocktail.cs` | 54 | fill bar UI | ✔ |
| `DebugCocktail.cs` | 28 | debug overlay | ✔ |
| `BTN_2_5D.cs` | 122 | ปุ่ม 2.5D generic — **ไม่เกี่ยวกับค็อกเทล** | ✘ → `BaseInteractable/` |
| `IInputProvider.cs` | 49 | input ของ **minigame** | ✘ → `Minigame/` |
| `NPC_Base.cs` | 152 | NPC movement/emotion | ✘ → `NPC/` |
| `CharacterData.cs` | 12 | NPC favorite drinks | ✘ → `NPC/` |

### 1.2 ความรับผิดชอบที่อัดอยู่ใน `CocktailSystemManager` (partial 2 ไฟล์)

1. ถือ repository ของสูตร  2. ถือ target cocktail (ออเดอร์)  3. สุ่มออเดอร์ตามรสนิยม NPC
4. อัปเดตข้อความ Post-It (UI)  5. Yarn **functions** 4 ตัว  6. Yarn **commands** 5 ตัว
7. sync ตัวแปร Yarn 3 ตัว  8. task/satisfaction mini-FSM  9. เปิด/ปิด interactable ของปุ่มวัตถุดิบ
10. เปิดปุ่ม End Shift  11. คำนวณ satisfaction  12. debug menu ~90 บรรทัด  13. `Update()` ว่าง

ข้อ 8 คือ **state machine ตัวที่สอง** ที่ทำงานคู่ขนานกับ HSM — เป็นสาเหตุหลักที่ merge ยาก

---

## 2. ปัญหาที่เจอระหว่างอ่าน

### 2.1 บั๊กจริง (แก้ก่อน refactor)

| # | ที่ | อาการ |
|---|---|---|
| B1 | `CocktailSystemManager.YarnInterface.cs:33,248,288` | `_cocktailType` ถูกกำหนดค่าแค่ `None` เท่านั้นตลอดทั้งโปรเจกต์ → `$type_of_cocktail` เขียน `0` เสมอ ทำให้ `<<if $type_of_cocktail == ...>>` ใน `.yarn` ใช้ไม่ได้ |
| B2 | `YarnInterface.cs:57–94` | `Order_Cocktail_OutName` และ `Order_Cocktail_OutDescription` **สุ่มใหม่ทั้งคู่** ถ้า `.yarn` เรียกสองบรรทัดติดกัน จะได้คนละแก้ว — ชื่อกับคำอธิบายไม่ตรงกัน |
| B3 | `YarnInterface.cs:183` | `ResolvePostItText` ใช้ `FindFirstObjectByType<Post_It_Order>()` ทั้งที่มี `_postItOrder` เป็น `[SerializeField]` อยู่แล้ว (บรรทัด 28) — สอง path ไปหาวัตถุเดียวกัน |
| B4 | `YarnInterface.cs:226–235` vs `CocktailShakerData.cs:151–163` | `EnableButtonInYarn` เรียก `SetIngredientActive(enable)` แล้ว **วนลูป component เดิมซ้ำอีกรอบ** ด้วยชุด component ที่ครอบคลุมน้อยกว่า (ขาด `Interactable_3DObject`, `HoverTooltip`) |
| B5 | `YarnInterface.cs:282` | `UpdateVariableInYarn()` เป็นทั้ง predicate ของ `WaitUntil` และ command ที่มี side effect (เขียนตัวแปร Yarn, ปิดปุ่ม, ซ่อน Post-It) — ละเมิด CQS และถูกเรียกทุกเฟรม |
| B6 | `UtilityDrink.cs:48` | `IsValidRatio` เช็ก `< 10` **ก่อน** เติม แล้วเติมกี่หน่วยก็ได้ → เติมตอนมี 9 หน่วยด้วย amount=5 ได้ 14 หน่วย ทะลุเพดาน |
| B7 | `DebugCocktail.cs:22` | `"Shaker:\n" + _shaker.CurrentCocktail` ต่อ string กับ ScriptableObject → ได้ชื่อ object ไม่ใช่ข้อมูลค็อกเทล (ควรเรียก `DrinkUtility.GetCocktailInfo`) |
| B8 | `CocktailSystemManager.cs:37` | `Awake` เขียน `_characterData` ซึ่งประกาศอยู่ในไฟล์ partial **อีกไฟล์หนึ่ง** — coupling ที่มองไม่เห็นระหว่าง 2 partial |
| B9 | `CocktailShakerData.cs:187–196` | `GlassVisualData` มี 4 entry จาก `GlassType` 8 ค่า, lookup ไม่เจอแล้วแค่ warning → sprite แก้วค้างของเดิม (ดู §10.1 G1) |
| B10 | `Cocktail Config/Cocktail/*.asset` | 20 จาก 26 สูตรมี `CompatibleGlass = None` เพราะ `.asset` ยังเก็บคีย์เก่าชื่อ `CompatibleGlasses:` — รวมกับ B9 แปลว่าแก้วไม่เคยเปลี่ยนลุค (ดู §10.1 G2) |
| B11 | `SystemGame.prefab:2645` | `_normalCocktailRepository` เป็น `{fileID: 0}` (null) → `Start()` เรียก `_normalRepository.GetDrinks()` **NullReferenceException ทันที** ถ้า instantiate prefab นี้ ดู §4.7 |

#### ⚠️ ซีนใช้ชุดสูตรคนละชุดกัน

| ที่ | `_normalCocktailRepository` | จำนวนสูตร |
|---|---|---:|
| `New Drag Drop System.unity` (ซีนพัฒนา) | `Demo_Normal_Cocktail` | **6** |
| `GamePlayScene.unity` | `Demo_Normal_Cocktail` | **6** |
| `GamePlayScene 1.unity` | `Normal_Cocktail` | 26 |
| `SystemGame.prefab` | *(null)* | — → B11 |

**สำคัญต่อ D7** — ถ้า design เล่นทดสอบเพื่อยืนยันค่า `MaxTolerance` ในซีนพัฒนา จะทดสอบกับสูตร
เพียง 6 ใบ ไม่ใช่ 26 ใบ โอกาสเจอสูตรใกล้เคียงต่างกันมหาศาล **ต้องสลับเป็น `Normal_Cocktail`
ก่อนทดสอบ** ไม่งั้นตัวเลขที่ยืนยันมาจะใช้กับเกมจริงไม่ได้

### 2.2 โค้ดตาย / ยังไม่ได้ใช้

| ที่ | สิ่งที่ตาย |
|---|---|
| `CocktailSystemManager.cs:12` | `_specialCocktailRepository` — ประกาศแล้วไม่มีใครอ่าน และเป็น `null` ในทุกซีน/prefab · `Specia_Cocktail.asset` เองก็ `cocktails: []` ว่างเปล่า → **D1 ตัดสินให้ต่อใช้งานจริง** ดู §4.7 |
| `CocktailSystemManager.cs:15` | `_failCocktailSprite` — อยู่แต่ในคอมเมนต์ |
| `CocktailSystemManager.cs:52–65, 122–132` | `Update()` ว่าง + โค้ดคอมเมนต์ 10 บรรทัด |
| `YarnInterface.cs:372–374` | `SetPostItText` ที่คอมเมนต์ทิ้ง |
| `CocktailShakerData.cs:54–57` | `Update()` ว่าง |
| `IngredientButtonUI.cs:100–104` | `AddIce()` body ว่าง แต่ยังอยู่ใน switch |
| `UtilityDrink.cs:19` | `using Unity.VisualScripting;` ไม่ได้ใช้ — ลาก assembly reference มาเปล่า ๆ |
| `IDrinkInterfaces.cs:48` | `IIngredientReceiver` ถูก implement แต่ **ไม่มีใครประกาศตัวแปรเป็นชนิดนี้เลย** — DIP แบบตกแต่ง |

### 2.3 โค้ดซ้ำ

| ที่ | ซ้ำกี่ชุด | ยุบเหลือ |
|---|---:|---|
| `UtilityDrink` — `AlcoholListEquals` / `LiqueurListEquals` / `MixerListEquals` | 3 (อัลกอริทึมเดียวกันเป๊ะ) | 1 generic |
| `UtilityDrink` — `CountAlcoholErrors` / `CountLiqueurErrors` / `CountMixerErrors` | 3 | 1 generic |
| `UtilityDrink` — `TryToAddAlcohol` / `TryToAddLiqueur` / `TryToAddMixer` | 3 | 1 generic |
| `UtilityDrink` — `GetTypeOfAlcohol` vs `ComputeTypeOfAlcohol` | 2 (สูตร `>=5 / >0` เขียนซ้ำ) | 1 |
| ลูป `TryGetComponent` เปิด/ปิด interactable | 3 (`SetIngredientActive`, `SetBookUiActive`, `EnableButtonInYarn`) | 1 helper |
| `IngredientButtonUI.Invoke()` / `ActionBehavior()` | 2 (เหมือนกันทุกไบต์) | 1 |
| ค่า tolerance `errors <= 2` | 6 จุด | 1 const |
| เพดาน 10 หน่วย (`MAX_TOTAL_PARTS` vs ตัวเลข `10` ดิบใน `CocktailShakerData:148`) | 2 | 1 const |

### 2.4 ปัญหาเชิงประสิทธิภาพ

`CocktailShakerData.UpdateCocktailInShaker()` เรียก `UpdateTypeOfAlcohol` → `UpdateName` → `UpdatePrice` → `UpdateColorInGlass` → `UpdateGlassType` และ **แต่ละตัวเรียก `FindBestIngredientMatch` ของตัวเอง** = สแกนสูตรทั้ง 26 ใบ **5 รอบ** ต่อการอัปเดตหนึ่งครั้ง (บวก `GetCocktailSprite` อีกรอบถ้าเปิดใช้) ควรสแกนรอบเดียวแล้วส่งผลลัพธ์ต่อ

### 2.5 ช่องว่างระหว่างโค้ดกับ GDD

GDD §165 ระบุว่า Part 2 คือ "single source of truth for implementation" — ด้านล่างคือจุดที่โค้ดปัจจุบันไม่ตรง

#### S1 — สูตรคำนวณความผิดเพี้ยนผิดชนิด ⚠️ ร้ายแรงที่สุด

GDD §17.1 นิยาม deviation เป็น **ผลรวมของส่วนต่างสัมบูรณ์**:

```
deviation(poured, recipe) = Σ |recipe[i] − poured[i]|      // รวมทุกชนิดที่ปรากฏฝั่งใดฝั่งหนึ่ง
```

โค้ดปัจจุบัน (`UtilityDrink.cs:348–382`, `CountAlcoholErrors` และพี่น้องอีก 2 ตัว) ทำคนละอย่าง —
มันนับ **"จำนวนชนิดวัตถุดิบที่ปริมาณไม่ตรง"** ไม่ใช่ขนาดของความต่าง:

```csharp
return keys.Count(k => { int p = ...; int r = ...; return p != r; });   // นับชนิด ไม่ใช่รวมส่วนต่าง
```

ผลต่างในทางปฏิบัติ — สูตร `Gin 7, Vodka 3` ผู้เล่นเท `Gin 1, Vodka 9`:

| | ค่าที่ได้ | ผลลัพธ์ |
|---|---:|---|
| GDD §17.1 | `\|7−1\| + \|3−9\| = 12` | `> 3` → **Fail (b)** |
| โค้ดปัจจุบัน | `2` (มี 2 ชนิดที่ต่าง) | `<= 2` → **Acceptable** |

เครื่องดื่มที่ผิดสูตรแทบทั้งแก้วได้ผลเป็น "พอรับได้" — และเพราะทุกสูตรมีวัตถุดิบไม่เกิน 3–4 ชนิด
(ดูจาก `Cocktail Config/`) ค่าที่โค้ดคำนวณจึงแทบไม่มีทางเกิน 3–4 เลย **เกณฑ์ Fail จึงเกือบไม่เคยทำงาน**

#### S2–S14 — ที่เหลือ

| # | GDD | โค้ดปัจจุบัน | ผลกระทบ |
|---|---|---|---|
| S2 | §17.3 เกณฑ์ `deviation <= 3` | `errors <= 2` (`UtilityDrink.cs:209`) | เกณฑ์ต่างกัน — แต่ต้องแก้ S1 ก่อนถึงจะเทียบได้ |
| S3 | §15.2 `1..5 → Low`, `>=6 → High` | `>= 5 → High` (`UtilityDrink.cs:64`) | **off-by-one — ✅ ตัดสินแล้ว: ยึด GDD** โค้ดต้องแก้เป็น `1..5 → Low`, `>=6 → High` |
| S4 | §18 เคส 3/4 แยกด้วย `servedDrinkType == orderedDrinkType` | ไม่เคยเทียบประเภทเลย | **`Fail (a)` เกิดไม่ได้** — ขาดครึ่งหนึ่งของบันไดความพึงพอใจ |
| S5 | §18.1 ราคา `×1.5 / ×1.0 / ×0.5 / 50 คงที่` | `match.Price` หรือ `DEFAULT_PRICE = 5f` | ไม่มีตัวคูณเลย และค่า fallback 5 ≠ 50 |
| S6 | §17.3 Fail(b) → `drinkName = สุ่ม()` | `NO_MATCH_NAME = "NOT MATCH ANY"` | ผู้เล่นเห็นข้อความ debug แทนชื่อเครื่องดื่ม |
| S7 | §17.3 Fail(b) → `drinkType` คำนวณจาก `AlcoholUnits(poured)` | ไม่ได้เซ็ต | ต่อยอด B1 — `$type_of_cocktail` ไม่มีค่าให้เขียนอยู่แล้ว |
| S8 | §21.1 Fail(b) → `BlendIngredientColors(poured)` | `Color.black` (`UtilityDrink.cs:108–109`) | แก้วดำ; และ **ยังไม่มีข้อมูลสีต่อวัตถุดิบใน data model เลย** |
| S9 | §12/§19 โหมดสั่งที่ 5 `OrderFixedByType(DrinkType)` | ไม่มี | นักเขียนใช้โหมดนี้ใน `.yarn` ไม่ได้ |
| S10 | §19.2 `candidates = ทุกสูตรที่ type อยู่ใน preferred` แล้วสุ่ม uniform | สุ่ม **ประเภท** ก่อน แล้วค่อยสุ่มสูตรในประเภทนั้น (`YarnInterface.cs:156`) | การกระจายต่างกัน — ถ้า High มี 20 สูตร None มี 4 สูตร GDD ให้ทุกสูตรโอกาสเท่ากัน โค้ดให้ทุก**ประเภท**เท่ากัน |
| S11 | §7/§19.1 `CustomerSO : ScriptableObject` ต่อตัวละคร + `relationshipValue` | `CharacterData : MonoBehaviour` ตัวเดียวถือ dictionary ทั้งหมด, ไม่มี `relationshipValue` | ข้อมูลลูกค้าผูกกับซีน แก้ทีต้องเปิดซีน |
| S12 | §15/§16 `Σ quantity == 10` เป๊ะ + editor validation + ห้ามสูตรซ้ำ | `IsValidRatio` เช็ก `< 10` ก่อนเติม (B6), ไม่มี validation ใด ๆ | สูตรผิดกติกาหลุดเข้า build ได้ |
| S13 | §10/§21 ผู้เล่น **เลือกแก้วเอง** (cosmetic) | `UpdateGlassType` เขียนทับด้วยแก้วของสูตรที่ match | ไม่มีระบบเลือกแก้ว — ต่อยอดกับ G1/G2 ใน §10.1 |
| S14 | §16 `MixMethod { Shake, Stir, Build }` | `Method { None, Shaking, Stirring }` — ไม่มี `Build` | **✅ D4 ตัดสินแล้ว: เลื่อนออกไป** — `Enum_MiniGameType.Building` มีแล้วแต่ยังไม่มี minigame รองรับ (Integration Plan §1) เพิ่ม `Method.Build` เมื่อ minigame พร้อม |
| S15 | §16 `public bool unlockedByDefault;` | **ไม่มีใน `S_Drink` เลย** (grep ทั้ง `[02]Script` ไม่เจอคำว่า unlock) | ระบบปลดล็อกสูตรยังไม่มี — ทับซ้อนกับ D1 ดู §4.7 |

#### สิ่งที่ GDD ยืนยันว่าโค้ดทำถูกแล้ว

- §17.2 tie-break ใช้ลำดับ index ตัวแรกชนะ — `FindBestIngredientMatch` ใช้ `if (errors >= bestErrors) continue;` (strictly-less) จึงได้ตัวแรกชนะพอดี ✔ **ให้เขียนคอมเมนต์กำกับว่าตั้งใจ** ไม่ใช่บังเอิญ
- §21 แก้วเป็น cosmetic ไม่มีผลต่อคะแนน — อัลกอริทึม matching ไม่อ่าน `CompatibleGlass` จริง ✔
- §18 เคส 2 (`deviation == 0` แต่ method/ice ไม่ตรง → Acceptable) — โค้ดตกลงมาที่ `errors <= 2` ได้ผลถูกโดยบังเอิญ ✔

---

## 3. สถาปัตยกรรมเป้าหมาย

```
┌─ Layer 4 · Flow bridges ────────────── (Bar410.GameFlow, MonoBehaviour)
│   BarSetupBridge          ← PrepareBarPhase        (Level 1)
│   CocktailFlowBridge      ← PrepareDrinksPhase / ServeState (Level 2–3)
│   MinigameFlowBridge      ← MinigameState          (มีแผนแล้วใน Integration Plan §3.3)
├─ Layer 3 · Yarn adapters ───────────── (MonoBehaviour, บาง ๆ ไม่มี logic)
│   CocktailYarnFunctions   CocktailYarnCommands
│   YarnTaskGate            YarnVariableSync
├─ Layer 2 · Session / Order ─────────── (plain C#)
│   DrinkOrderContext       OrderService       DrinkScoringService
├─ Layer 1 · Scene runtime ───────────── (MonoBehaviour)
│   ShakerContents          ShakerVisualPresenter   ShakerPanelController
│   IngredientButtonGroup   ShakerTooltip           CocktailShaker (คงเดิม, ผอมลง)
└─ Layer 0 · Domain ──────────────────── (static / SO, ไม่มี Unity lifecycle)
    S_Drink   DrinkIngredients   IDrinkRepository / SO_CocktailList
    DrinkQuery   DrinkMatcher   DrinkBuilder   DrinkFormatter
```

**กฎการพึ่งพา:** ลูกศรชี้ลงเสมอ Layer 0–2 ห้ามรู้จัก `MonoBehaviour` ของ Layer 1 ขึ้นไป และ **flow layer (`Bar410.GameFlow` state classes) ห้ามรู้จักอะไรใน Layer 0–3 เลย** — ทุกอย่างผ่าน bridge ตามแบบที่ `Bar410_Minigame_Integration_Plan.md §3.2` วางไว้

---

## 4. รายละเอียดการแยกไฟล์

### 4.1 `UtilityDrink.cs` (412 บรรทัด) → 8 ไฟล์ **ตามหัวข้อ GDD**

โฟลเดอร์ใหม่: `Cocktail System/Cocktail/Domain/`

จุดที่ควรแยกไม่ใช่เรื่องรสนิยม — **หัวข้อใน GDD คือรอยแยกอยู่แล้ว** แต่ละไฟล์ trace กลับไปหนึ่งหัวข้อได้:

| ไฟล์ใหม่ | GDD § | เนื้อหา | ประมาณ |
|---|---|---|---:|
| `DrinkDeviation.cs` | §17.1 | `Compute(poured, recipe)` → `Σ\|r−p\|` (**แก้ S1**), `FindBestMatch` → `RecipeMatch` | ~55 |
| `DrinkFlagResolver.cs` | §17.3 | `Perfect` / `Seem_Like` / `Fail` จาก deviation + methodMatch + iceMatch | ~35 |
| `SatisfactionEvaluator.cs` | §18 | บันไดลำดับ 5 เคส (**แก้ S4** — เพิ่มการเทียบ `servedType == orderedType`) | ~45 |
| `PricingRules.cs` | §18.1 | `×1.5 / ×1.0 / ×0.5 / 50` (**แก้ S5**) | ~25 |
| `AlcoholClassifier.cs` | §15.2 | `0 → None`, `1..5 → Low`, `>=6 → High` (**แก้ S3**) + ยุบสูตรที่เขียนซ้ำ 2 ที่เหลือ 1 | ~25 |
| `DrinkColorBlender.cs` | §21.1 | สีจากสูตรที่ match, หรือ `BlendIngredientColors(poured)` ตอน Fail(b) (**แก้ S8**) | ~40 |
| `DrinkQuery.cs` | §15 | `GetTotalAlcohol/Liqueur/Mixer/Ingredient`, `IsValidRatio` | ~45 |
| `DrinkBuilder.cs` | §10 | `TryAdd<T>`, `ApplyRecipeIdentity`, `Clear` | ~70 |
| `DrinkFormatter.cs` | — | `GetCocktailInfo`, `GetCocktailIngredient` — debug, `#if UNITY_EDITOR` | ~35 |
| `IngredientMath.cs` | — | generic helper ยุบโค้ดซ้ำ 3 ชุด (`internal`) — จาก D6 คง 3 ลิสต์ ดู §4.1.1 | ~45 |

ไฟล์ละ ≤ 70 บรรทัด และเมื่อ design แก้กติกาข้อไหนใน GDD จะรู้ทันทีว่าต้องเปิดไฟล์ไหน — นี่คือ
Single Responsibility ที่วัดได้ ไม่ใช่แค่ "แยกเพราะไฟล์ยาว"

**โครงสร้างผลลัพธ์กลาง** — แทนที่จะให้แต่ละเมธอดสแกนสูตรเอง (§2.4):

```csharp
public readonly struct RecipeMatch          // GDD §17
{
    public readonly S_Drink Recipe;
    public readonly int     Deviation;      // Σ|r−p|  ตาม §17.1
    public readonly bool    MethodMatch;
    public readonly bool    IceMatch;
    public readonly DrinkFlag Flag;         // Perfect / Seem_Like / Fail  ตาม §17.3
    public bool IsFailB => Recipe == null || Deviation > DrinkDeviation.MaxTolerance;  // 3
}

public enum DrinkFlag { Perfect, Seem_Like, Fail }   // ใหม่ — GDD §17.3 ไม่มีใน enum ปัจจุบัน
```

> ⚠️ **ห้ามเพิ่มสมาชิกใน `Satisfaction`** — Yarn เขียนค่าเป็น `(int)` ลง `$satisfaction` การเพิ่ม/สลับ
> ลำดับจะทำให้ `.yarn` ที่เทียบเลขอยู่พังเงียบ Fail(a)/Fail(b) จึงต้องแยกด้วย `RecipeMatch.IsFailB`
> ไม่ใช่ด้วย enum ใหม่ใน `Satisfaction`

#### 4.1.1 โครงสร้างวัตถุดิบ (D6 — ตัดสินแล้ว: ยึดโค้ด, คง 3 ลิสต์)

**ตัดสินให้คง `AlcoholList` / `LiqueurList` / `MixerList` แยกกันตามโค้ดเดิม** ไม่ย้ายไป
`List<IngredientAmount>` แบบ GDD §15.1 เหตุผล: รองรับการเพิ่มวัตถุดิบในอนาคตโดยยังคง type safety
ต่อหมวด — ปุ่ม Mixer จะถูกเผลอตั้งเป็น Gin ไม่ได้ และ dropdown ใน Inspector ยังสั้นและอยู่ในบริบท
แม้จำนวนวัตถุดิบจะโตขึ้น (ต่างจาก enum แบนที่ dropdown จะยาวขึ้นเรื่อย ๆ ทุกครั้งที่เพิ่มของ)

> ⚠️ **ข้อแลกเปลี่ยนที่ต้องรู้:** การเพิ่มวัตถุดิบ *ชนิดใหม่ในหมวดเดิม* ถูกมากทั้งสองแบบ (เพิ่มค่า
> enum ค่าเดียว) แต่การเพิ่ม *หมวดใหม่* (เช่น Bitters, Garnish, Foam) แบบ 3 ลิสต์แพงกว่า —
> ต้องเพิ่ม enum + struct + ลิสต์ + จุดเรียกอีกหลายที่ เทียบกับแบบแบนที่เพิ่มค่าใน
> `IngredientCategory` ค่าเดียว **§4.1.1 ด้านล่างจึงออกแบบให้ต้นทุนนี้ต่ำที่สุดเท่าที่ทำได้**

**กุญแจ** — ให้ 3 struct ใน `DrinkIngredients.cs` implement interface ร่วม (ยังเป็น struct ไม่มี
boxing เพราะผูกด้วย generic constraint):

```csharp
public interface IIngredientEntry<TKey> where TKey : struct
{
    TKey Key   { get; }
    int  Parts { get; }
}

[System.Serializable]
public struct AlcoholIngredient : IIngredientEntry<BaseSpirit>
{
    public BaseSpirit Type;      // ชื่อฟิลด์เดิม → ข้อมูลที่ serialize ไว้ไม่พัง
    public int Amount;
    public readonly BaseSpirit Key   => Type;
    public readonly int        Parts => Amount;
}
// LiqueurIngredient : IIngredientEntry<Liqueur>  และ  MixerIngredient : IIngredientEntry<Mixer> เหมือนกัน
```

จากนั้นยุบ 9 เมธอด (3×equals + 3×count + 3×add) เหลือ 3 ตัวที่ไม่รู้จักหมวดใดเป็นพิเศษ:

```csharp
internal static class IngredientMath
{
    public static bool ListEquals<TItem, TKey>(List<TItem> a, List<TItem> b)
        where TItem : struct, IIngredientEntry<TKey> where TKey : struct;

    /// GDD §17.1 — Σ|recipe − poured| ของหมวดเดียว ผู้เรียกเอาไปบวกกันเอง
    public static int Deviation<TItem, TKey>(List<TItem> poured, List<TItem> recipe)
        where TItem : struct, IIngredientEntry<TKey> where TKey : struct;

    public static void Add<TItem, TKey>(List<TItem> list, TKey key, int amount, Func<TKey,int,TItem> make)
        where TItem : struct, IIngredientEntry<TKey> where TKey : struct;
}
```

**ต้นทุนของการเพิ่มหมวดที่ 4 หลังทำแบบนี้แล้ว** — เพราะ `IngredientMath` ไม่รู้จักหมวดใดเลย
การเพิ่ม `BittersIngredient` จึงไม่ต้องเขียนอัลกอริทึมใหม่แม้แต่บรรทัดเดียว มีแค่ **จุดรวมผล 4 จุด**:

```csharp
// Domain/DrinkDeviation.cs — จุดเดียวที่รู้ว่ามีกี่หมวด
public static int Compute(S_Drink poured, S_Drink recipe)
    => IngredientMath.Deviation<AlcoholIngredient, BaseSpirit>(poured.AlcoholList, recipe.AlcoholList)
     + IngredientMath.Deviation<LiqueurIngredient, Liqueur>   (poured.LiqueurList, recipe.LiqueurList)
     + IngredientMath.Deviation<MixerIngredient,   Mixer>     (poured.MixerList,   recipe.MixerList);
     // + บรรทัดเดียวต่อหมวดใหม่
```

จุดรวมผลทั้ง 4: `DrinkDeviation.Compute`, `DrinkQuery.GetTotalIngredient`,
`DrinkBuilder.Clear`, `DrinkFormatter.GetCocktailIngredient` — **ให้เขียนคอมเมนต์ `// เพิ่มหมวดใหม่:
แก้ที่นี่` กำกับทั้ง 4 จุด** เพื่อให้คนที่มาเพิ่มทีหลังหาเจอครบ ไม่ตกหล่นจุดใดจุดหนึ่งแล้วบั๊กเงียบ

**ไม่ต้องแปลงข้อมูลใด ๆ** — ชื่อฟิลด์และโครงสร้างที่ serialize ไว้เหมือนเดิมทุกประการ
26 `.asset` และปุ่ม `IngredientButtonUI` ~20 ตัว × 6 ที่ ไม่ถูกแตะเลย (นี่คือข้อได้เปรียบใหญ่สุด
ของการเลือกทางนี้ — ไม่มี migration script, ไม่มีความเสี่ยงข้อมูลหาย)

**GDD §15.1/§16 จึงเป็นฝ่ายที่ไม่ตรง** — ต้องแก้เอกสาร ดู §12 E1 และให้เขียนคอมเมนต์ที่หัวไฟล์
`DrinkIngredients.cs` ระบุว่าโครงสร้างนี้ต่างจาก GDD โดยเจตนา ไม่งั้นคนที่อ่าน GDD แล้วมาอ่านโค้ด
จะคิดว่าเจอบั๊ก

**ของแถมที่ได้ฟรี** — `CanberryJuice` ใน `Enum_Class.cs:33` สะกดตกตัว `r` (GDD §194 สะกด
`CranberryJuice`) การเปลี่ยนชื่อสมาชิก enum ไม่กระทบข้อมูลที่ serialize ไว้ (เก็บเป็น int) และมี
ผู้อ้างอิงในโค้ดแค่จุดเดียว — แก้ได้เลยโดยไม่มีความเสี่ยง


**ยุบการสแกนสูตร 5 รอบเหลือ 1 รอบ** — `RecipeMatch` (ด้านบน) ถูกคำนวณครั้งเดียวแล้วส่งต่อ:

```csharp
// เดิม: 5 เมธอด × สแกน 26 สูตร  →  ใหม่: 1 สแกน + 1 apply
public static void ApplyRecipeIdentity(S_Drink runtime, in RecipeMatch match);
```

`CocktailShakerData.UpdateCocktailInShaker` กลายเป็น:

```csharp
var match = DrinkDeviation.FindBestMatch(CurrentCocktail, recipes);
DrinkBuilder.ApplyRecipeIdentity(CurrentCocktail, match);
```

`ApplyRecipeIdentity` เป็นที่เดียวที่รวมกติกา GDD §17.3 + §18.1 + §21.1 เข้าด้วยกัน — ชื่อ, ราคา,
สี, ประเภท ถูกกำหนดพร้อมกันจาก `Flag` เดียว แทนที่จะให้ 5 เมธอดตัดสินใจแยกกันแล้วอาจไม่ตรงกัน

### 4.2 `CocktailShakerData.cs` (212 บรรทัด / 5 หน้าที่) → 5 component

| Component ใหม่ | รับผิดชอบ | สิ่งที่ย้ายมา |
|---|---|---|
| `ShakerContents` | วงจรชีวิตของ `CurrentCocktail`, เติมวัตถุดิบ, ตั้ง method/ice, reset | `CurrentCocktail`, `Awake/OnDestroy`, `TryToAdd*`, `SetMethod*`, `ToggleIce`, `ResetCocktailData` |
| `ShakerVisualPresenter` | สีน้ำ + แก้ว + `WaterSlosh` | `GlassVisualData`, `VisualCocktailGlass`, `SetColorAndGlass`, `Start/Stop/FinishFill`, `glassWaterSlosh` |
| `IngredientButtonGroup` | roster ของ GameObject + `SetInteractable(bool)` | `ingredientButtons`, `bookUi`, `SetIngredientActive`, `SetBookUiActive`, `CanIngredientActive` |
| `ShakerPanelController` | แผง Method / AddIce / Serve และ flag `_canShow*` | ย้ายมาจาก `CocktailShaker.cs:13–16, 20–23, 34–37, 56–76` |
| `ShakerTooltip` | `ITooltipProvider` | `GetTooltipText()` |

**`IngredientButtonGroup` วางในซีน 2 ตัว** (Ingredients / BookUI) — จบปัญหา `bookUi` เป็นลิสต์แยกที่มีโค้ดวนลูปคนละก๊อป และจบ B4 ไปในตัว ภายในใช้ helper เดียว:

```csharp
public static class InteractableToggle
{
    // จุดเดียวที่รู้ว่า "เปิด/ปิด interactable" แปลว่าต้องแตะ component ตัวไหนบ้าง
    public static void Apply(GameObject go, bool active);
}
```

**ลบ `AlcoholEvent` / `LiqueurEvent` / `MixerEvent`** — สาม `UnityEvent<T,int>` นี้มีอยู่เพื่อให้ `IngredientButtonUI` ยิงกลับเข้า shaker เท่านั้น (มี 3 จุดผูกในซีน `New Drag Drop System.unity`) เปลี่ยนเป็นให้ `IngredientButtonUI` ถือ `IIngredientReceiver` แล้วเรียกตรง — interface ที่ตอนนี้เป็นของตกแต่ง (§2.2) จะได้ถูกใช้จริง และลด indirection ที่พังเงียบถ้าลืมผูก inspector

> ⚠️ ข้อนี้ต้องแก้ซีน — ดู §8 Phase 2

### 4.3 `CocktailSystemManager` (518 บรรทัด) → 8 ไฟล์

| ไฟล์ใหม่ | Layer | เนื้อหา |
|---|---|---|
| `Session/DrinkOrderContext.cs` | 2 | สถานะออเดอร์ปัจจุบัน (ดู §5) |
| `Session/OrderService.cs` | 2 | 5 โหมดสั่งตาม GDD §19 — สุ่มครั้งเดียว แก้ B2, S9, S10 |
| `Session/DrinkScoringService.cs` | 2 | ห่อ `SatisfactionEvaluator` + `PricingRules` |
| `Session/ICustomerPreferences.cs` | 2 | interface |
| `Session/SO_Customer.cs` | 0 | ScriptableObject ต่อตัวละคร ตาม GDD §19.1 — แทน `CharacterData` (S11) |
| `Yarn/CocktailYarnFunctions.cs` | 3 | `Order_Cocktail_*` 4 ตัว |
| `Yarn/CocktailYarnCommands.cs` | 3 | `wait_for_task`, `wait_scene`, `Can_End_Shift`, `Enable_InteractableObject`, `Reset_Variable` |
| `Yarn/YarnVariableSync.cs` | 3 | const 3 ตัว + read/write + แก้ B1 |
| `Yarn/YarnTaskGate.cs` | 3 | `IsWaitingForTask` + gate; แยก query/command แก้ B5 |
| `Editor/CocktailDebugMenu.cs` | — | ~90 บรรทัดของ `[ContextMenu]` + `DebugVariableFromYarn` ทั้งหมด ใต้ `#if UNITY_EDITOR` |

`CocktailSystemManager` **หายไปทั้งคลาส** ไม่เหลือ facade ให้เป็นที่กองของใหม่

แก้ B5 ให้ชัดที่ `YarnTaskGate`:

```csharp
public bool IsSatisfied => _context.IsScored;    // pure query — ใช้ใน WaitUntil ได้ทุกเฟรม
public void Commit()  { /* เขียนตัวแปร Yarn, ปิดปุ่ม, ซ่อน Post-It */ }   // เรียกครั้งเดียว
```

### 4.4 `IngredientButtonUI.cs`

- ลบ `ActionBehavior()` (ก๊อปของ `Invoke()`)
- ลบ `AddIce()` ตัว body ว่าง และเคสใน switch — เหลือ `AddIce(bool)`
- เปลี่ยน `_shaker.OnAddMixer?.Invoke(...)` → `_receiver.TryToAddMixer(_mixer, 1)`
- เปลี่ยน `FindFirstObjectByType` ใน `Awake` → `[SerializeField]` reference (หรือรับจาก `IngredientButtonGroup` ตอน bind)

**ฟิลด์ `_mixer` / `_alcohol` / `_liqueur` และ `ButtonAction` ทั้ง 7 เคสคงเดิม** ตาม D6 —
`Assets/Editor/IngredientButtonUIEditor.cs` จึงไม่ต้องแก้ และปุ่มในซีน ~20 ตัว × 6 ที่ ไม่ถูกแตะ

### 4.5 ย้ายไฟล์ที่อยู่ผิดโฟลเดอร์

| จาก | ไป | เหตุผล |
|---|---|---|
| `Cocktail System/BTN_2_5D.cs` | `BaseInteractable/` | เป็นปุ่ม 2.5D generic ไม่รู้จักค็อกเทลเลย |
| `Cocktail System/IInputProvider.cs` | `Minigame/` | ผู้ใช้เดียวคือ `BaseMiniGame` |
| `Cocktail System/NPC_Base.cs` | `NPC/` | (หมายเหตุ: working tree ปัจจุบัน**เพิ่งย้ายเข้ามา** — ดู §9.4) |
| `Cocktail System/CharacterData.cs` | `NPC/` | ข้อมูล NPC ไม่ใช่ข้อมูลค็อกเทล |

### 4.6 `Enum_Class.cs` — แยกไฟล์ **แต่ไม่แยกคลาส**

`E_Cocktail` ถูก `using static E_Cocktail;` ใน **20 ไฟล์** การแยกเป็นคลาสใหม่จะพังทั้งหมดโดยไม่ได้อะไรกลับมา ทำแบบถูกที่สุดคือใช้ `partial class` แยกเป็น 4 ไฟล์ตามโดเมน โดยชื่อคลาสยังเป็น `E_Cocktail` เหมือนเดิม:

```
Cocktail System/Cocktail/E_Cocktail.Drink.cs      BaseSpirit, Liqueur, Mixer, GlassType, Method,
                                                   TypeOfCocktail, Satisfaction, DrinkFlag
NPC/E_Cocktail.Character.cs                        NPC_Name, Direction
Minigame/E_Cocktail.Minigame.cs                    Enum_MiniGameType
[06]Dialogue/E_Cocktail.Dialogue.cs                TextType, ConversationPhase
```

`using static E_Cocktail;` เดิมยังทำงานทั้ง 20 ไฟล์ ไม่ต้องแตะ และลบ `using UnityEngine;` ที่ไม่ได้ใช้ทิ้ง

### 4.7 `CompositeDrinkRepository` (D1 — ตัดสินแล้ว: ต่อใช้งานจริง)

**สถานะปัจจุบันของข้อมูล** (ตรวจจากไฟล์จริง):

- `Specia_Cocktail.asset` → `cocktails: []` **ว่างเปล่า** ยังไม่มีสูตรพิเศษสักใบ
- `_specialCocktailRepository` → `null` ในทุกซีนและ prefab
- ไม่มีระบบปลดล็อกสูตรในโค้ดเลย (S15 — GDD §16 มี `unlockedByDefault` แต่ `S_Drink` ไม่มี)

ดังนั้น **composite จะให้ผลเหมือนวันนี้ทุกประการจนกว่าจะมี content** — คุณค่าของงานนี้คือ
*เปิดรอยต่อไว้ให้พร้อม* ไม่ใช่เปลี่ยนพฤติกรรม ต้นทุนต่ำ (~40 บรรทัด) และตัด B11 ไปด้วย

```csharp
// Cocktail/CompositeDrinkRepository.cs — plain C#, ไม่ใช่ ScriptableObject
public class CompositeDrinkRepository : IDrinkRepository
{
    private readonly List<S_Drink> _union;          // cache ตอนสร้าง ไม่สแกนซ้ำทุกครั้ง

    public CompositeDrinkRepository(params IDrinkRepository[] sources);   // ข้าม source ที่เป็น null

    public IReadOnlyList<S_Drink> GetDrinks() => _union;
    public S_Drink GetRandom();
    public S_Drink GetRandom(TypeOfCocktail type);
    public bool TryGetByName(string name, out S_Drink drink);   // ใหม่ — เลิก LINQ ใน Yarn layer
}
```

> ⚠️ **`GetRandom()` ต้องสุ่ม uniform บน union — ห้ามสุ่ม repository ก่อนแล้วค่อยสุ่มสูตร**
> นั่นคือรูปแบบบั๊กเดียวกับ S10 เป๊ะ ๆ ถ้า normal มี 26 สูตรและ special มี 2 สูตร การสุ่ม
> repository ก่อนจะทำให้สูตรพิเศษมีโอกาสออก 50% แทนที่จะเป็น 2/28

**คำถามออกแบบที่ composite บังคับให้ตอบ** — สูตรพิเศษควรถูก **สุ่ม** เจอไหม?

| ใช้กับ | ควรใช้ pool ไหน |
|---|---|
| โหมด 1/2 (`OrderFixedByName` / `ByFlavor`) — นักเขียนระบุชื่อเอง | **union** — ต้องหาสูตรพิเศษเจอ |
| โหมด 3/4/5 (สุ่มตามความชอบ / ตามประเภท) | **normal เท่านั้น** — ไม่งั้นสูตรพิเศษหลุดออกมาก่อนถึงจังหวะในเนื้อเรื่อง |

แก้ด้วยการให้ `OrderService` รับ **2 ตัว** ไม่ต้องสร้าง interface ใหม่:

```csharp
public OrderService(IDrinkRepository lookup,      // composite — ใช้ค้นหาชื่อ
                    IDrinkRepository randomPool)  // normal อย่างเดียว — ใช้สุ่ม
```

`CocktailSystemManager` เดิมประกอบให้ตอน `Awake`:

```csharp
var lookup = new CompositeDrinkRepository(_normalCocktailRepository, _specialCocktailRepository);
_orderService = new OrderService(lookup, _normalCocktailRepository);
```

**แก้ B11 ไปในตัว** — constructor ข้าม source ที่เป็น `null` และถ้า union ว่างให้
`Debug.LogError` ระบุชื่อ GameObject แทนที่จะโยน `NullReferenceException` เปล่า ๆ ตอน `Start`

> 📌 **S15 ทับซ้อนกับข้อนี้ — ต้องเลือกกลไกเดียว** GDD §16 ออกแบบการปลดล็อกเป็น `bool
> unlockedByDefault` **ต่อสูตร** ส่วนโค้ดใช้ **แยกเป็น 2 asset** สองอย่างนี้แก้ปัญหาเดียวกัน
> ถ้าทำทั้งคู่จะมีแหล่งความจริง 2 ที่ที่ขัดกันได้ **แผนนี้เดินตามโค้ด (2 asset) ไปก่อนตาม D1**
> และ **ไม่** เพิ่ม `unlockedByDefault` — เมื่อ design เริ่มทำ content สูตรพิเศษจริงค่อยตัดสิน
> ว่าจะอยู่กับ 2 asset ต่อ หรือย้ายไป flag ต่อสูตร (ถ้าย้าย ให้ยุบ composite ทิ้งไปเลย)

---

## 5. `DrinkOrderContext` — หัวใจของการ merge

`Bar410_StateMachine_Implementation.md §8 คำถามเปิดข้อ 4` ระบุว่าต้องมี "`DrinkOrderContext` ที่สร้างขึ้นข้ามสเต็ปแล้วคิดคะแนนตอนเสิร์ฟ" — คลาสนี้คือคำตอบ และเป็นตัวที่ทำให้ mini-FSM ข้อ 8 ใน §1.2 หายไป

```csharp
// Session/DrinkOrderContext.cs  — plain C#, ไม่มี UnityEngine
public class DrinkOrderContext
{
    // ── ฝั่งออเดอร์ (เขียนตอน TalkingWithCustomer) ──
    public NPC_Name      Customer      { get; private set; }
    public OrderMode     Mode          { get; private set; }   // GDD §12 — 5 โหมด
    public S_Drink       Target        { get; private set; }   // สุ่มครั้งเดียว แก้ B2
    public TypeOfCocktail OrderedType  { get; private set; }   // GDD §18 เคส 3/4 ต้องใช้ — แก้ S4
                                                               // โหมด 5 มีแค่ค่านี้ Target = null

    // ── ฝั่งผลลัพธ์ (เขียนตอน ServeState.Exited) ──
    public RecipeMatch   Match         { get; private set; }   // GDD §17 — deviation + Flag
    public Satisfaction  Result        { get; private set; } = Satisfaction.None;
    public TypeOfCocktail ServedType   { get; private set; } = TypeOfCocktail.None;  // แก้ B1 + S7
    public float         Payout        { get; private set; }   // GDD §18.1 — แก้ S5
    public float         RelationshipDelta { get; private set; }  // GDD §18.2
    public bool          IsScored      { get; private set; }

    public IReadOnlyList<MinigameOutcome> MinigameResults => _minigameResults;

    public void BeginOrder(NPC_Name customer, OrderMode mode, S_Drink target, TypeOfCocktail orderedType);
    public void RecordMinigame(MinigameOutcome outcome);
    public void Score(S_Drink served, in RecipeMatch match);   // เรียกจาก ServeState.Exited
    public void Clear();
}
```

`Score()` คือจุดเดียวที่รันบันได GDD §18 ทั้ง 5 เคส แล้วเขียน `Result` / `Payout` /
`RelationshipDelta` พร้อมกัน — **แก้ S4 และ S5 ไปพร้อมกันโดยธรรมชาติ** เพราะเคส 3/4 ต้องใช้
`OrderedType` ซึ่งมีอยู่ใน context อยู่แล้ว (ปัจจุบันข้อมูลนี้ไม่เคยถูกเก็บไว้ที่ไหนเลย จึงเทียบไม่ได้)

**`OrderMode` สำคัญกว่าที่คิด** — GDD §12 โหมด 5 (`OrderFixedByType`) สั่งแค่ "ประเภท" ไม่มีสูตรเป้าหมาย
ดังนั้น `Target` เป็น `null` ได้โดยชอบธรรม และ §18 เคส 1–2 (`deviation == 0`) ใช้ไม่ได้กับโหมดนี้ —
ต้องตัดสินจาก `ServedType == OrderedType` อย่างเดียว ถ้าไม่แยก `Mode` ไว้ โค้ดจะแยกไม่ออกระหว่าง
"ไม่มีเป้าหมาย" กับ "หาเป้าหมายไม่เจอ"

**สิ่งที่ตัวนี้ทดแทน** — `_TaskDone`, `_satisfaction`, `_cocktailType`, `_targetCocktail`, `IsWaitingForTask` ทั้ง 5 ตัวที่ตอนนี้กระจายอยู่ใน 2 partial

| เจ้าของเดิม | ย้ายไป |
|---|---|
| `_targetCocktail` + `RandomCocktail()` | `OrderService.PickForCustomer` → `context.BeginOrder` |
| `_TaskDone` | `context.IsScored` |
| `_satisfaction` | `context.Result` |
| `_cocktailType` (ตายอยู่) | `context.ServedType` — เขียนจริงจาก `DrinkQuery.GetTypeOfAlcohol(served)` |
| `IsWaitingForTask` (public static mutable) | property บน `YarnTaskGate` + accessor คงชื่อเดิมไว้ให้ `SaveLoadManager.cs:197` |

---

## 6. จุดเชื่อม HSM (merge readiness)

ยึดแพตเทิร์นเดียวกับ `MinigameFlowBridge` ใน Integration Plan §3.3: **state ธรรมดาไม่รู้จักซีน — bridge MonoBehaviour ตัวเดียวต่อ seam** และเนื่องจาก `StateBase` เปิด `Entered` / `Exited` / `Ticked` อยู่แล้ว **ไม่ต้องแก้ไฟล์ใน `Hierarchical State Machine/` เลยแม้แต่ไฟล์เดียว** ยกเว้นที่ Integration Plan วางไว้แล้ว

### 6.1 `BarSetupBridge` → `PrepareBarPhase` (Level 1)

ไฟล์: `Hierarchical State Machine/Level 1 - Game Loop/BarSetupBridge.cs`, namespace `Bar410.GameFlow`

```csharp
[SerializeField] GameLoopFSM            _gameLoop;
[SerializeField] IngredientButtonGroup  _ingredients;
[SerializeField] IngredientButtonGroup  _bookUI;
[SerializeField] ShakerPanelController  _panels;
[SerializeField] ShakerContents         _contents;
[SerializeField] N_PlacementSystem      _placement;      // optional
```

| Event | สิ่งที่ทำ |
|---|---|
| `PrepareBar.Entered` | ปลดล็อกระบบวาง (`_placement`), เปิด `_bookUI`, **ปิด** `_ingredients` และ `_panels`, `_contents.Clear()` |
| `PrepareBar.Exited` | ล็อกการวาง, **ยึด roster ของวันนั้น** — `_ingredients.Rebuild(_placement.PlacedObjects)` |

**นี่คือคุณค่าจริงของการ merge:** ตอนนี้ `ingredientButtons` เป็นลิสต์ที่ผูกมือใน inspector คงที่ตลอดเกม หลัง merge มันจะกลายเป็น "วัตถุดิบที่ผู้เล่นวางบนบาร์ในวันนั้น" ซึ่งเป็นสิ่งที่ `PrepareBarPhase` ("drag-and-drop bar setup") มีไว้ทำ

`PrepareBarPhase.RequestOpenBar()` มีอยู่แล้ว → ปุ่ม "Open Bar" ต่อกับ `GameFlowCommands.OpenBar()` ได้ทันที ไม่ต้องเพิ่มอะไร

### 6.2 `CocktailFlowBridge` → Level 2–3

ไฟล์: `Hierarchical State Machine/Level 3 - Prepare Drinks/CocktailFlowBridge.cs`

| Event | สิ่งที่ทำ | แทนที่ของเดิม |
|---|---|---|
| `TalkingWithCustomer.Entered` | `OrderService.PickForCustomer` → `context.BeginOrder` → ดัน Post-It | `Order_Cocktail_*` ที่สุ่มซ้ำ (B2) |
| `PrepareDrinks.Entered` | `_contents.Clear()`, `_panels.ResetFlags()` | ปุ่ม / `<<Reset_Variable>>` — และบังคับใช้กติกา §3.1 ของ HSM doc ("backtrack แล้วเครื่องดื่มรีเซ็ต") ให้เป็นโครงสร้าง ไม่ใช่ข้อตกลง |
| `AddIngredient.Entered` / `Exited` | `_ingredients.SetInteractable(true / false)` | `Enable_InteractableObject` ที่เรียกจาก `.yarn` |
| `Minigame.OnStartRequested` | **ไม่ทำที่นี่** — เป็นงานของ `MinigameFlowBridge` ตาม Integration Plan | — |
| `Serve.Exited` | `DrinkScoringService.Score(context, _contents.Current)` → `YarnVariableSync.Write(context)` | `ServeDrink()` → `UpdateVariableInYarnTrigger()` |

`Serve.Exited` ปิด TODO ที่ `ServeState.cs:29` และ HSM doc §3.2 ("Scoring moved to `ServeState.OnExit`") ทิ้งไว้

**ป้อน minigame type:** Integration Plan §3.1 ลำดับที่ 2 บอกให้อ่านจาก `CocktailShakerData.CurrentCocktail.PreparationMethod` → หลัง refactor คือ `ShakerContents.Current.PreparationMethod` เปลี่ยนแค่ชื่อ type ใน `MinigameFlowBridge`

### 6.3 การอยู่ร่วมกันของ `wait_for_task` กับ HSM

ปัญหาที่ต้องแก้ตอน merge: วันนี้ `<<wait_for_task SystemGame>>` ค้าง Yarn ไว้จนผู้เล่นกดเสิร์ฟ ส่วน HSM ต้องการให้ `<<flow_prepare_drinks>>` เดินเครื่องแล้วบทสนทนาไหลต่อ — สอง mechanism แย่งกันเป็นเจ้าของ "จังหวะ"

**แนวทางที่แนะนำ (migration ราบรื่นที่สุด):** เก็บ `wait_for_task` ไว้ตามเดิม แต่เปลี่ยนเงื่อนไขข้างในจาก `_TaskDone && _satisfaction != None` เป็น `_context.IsScored` — `.yarn` ทั้ง 3 จุดใน `Day1_Demo.yarn` ไม่ต้องแก้เลย แต่แหล่งความจริงย้ายไปอยู่ที่ `DrinkOrderContext` ที่ HSM เขียนแล้ว เมื่อ flow เดินครบวงแล้วค่อยพิจารณาถอด `wait_for_task` ทิ้งทีหลัง

### 6.4 ระบบสั่งเครื่องดื่ม 5 โหมด (GDD §12 / §19) — แก้ S9, S10

GDD กำหนด 5 โหมด โดย **นักเขียนเลือกโหมดเองใน Yarn** ไม่มีการสุ่มโหมดอัตโนมัติ ปัจจุบันมี 4 ครึ่ง:

| GDD §19 | โหมด | โค้ดปัจจุบัน | สถานะ |
|---|---|---|---|
| `OrderFixedByName(string)` | 1 ระบุชื่อเมนู | `Order_Cocktail_ByName_OutName(string)` | ✔ มี |
| `OrderFixedByFlavorDescription(string)` | 2 ระบุคำอธิบายรสชาติ | `Order_Cocktail_ByName_OutDescription(string)` | ✔ มี |
| `OrderRandomByPreferenceType(string id)` | 3 สุ่มตามความชอบ → ชื่อ | `Order_Cocktail_OutName(int NPC)` | ⚠ มี แต่ **S10** + รับ `int` |
| `OrderRandomByPreferenceFlavor(string id)` | 4 สุ่มตามความชอบ → รสชาติ | `Order_Cocktail_OutDescription(int NPC)` | ⚠ เหมือนกัน |
| `OrderFixedByType(DrinkType)` | 5 ระบุแค่ประเภท | — | ✘ **ไม่มี (S9)** |

**ข้อสังเกตสำคัญ:** GDD เขียน signature เป็น *Command* (`void`) แต่โค้ดทำเป็น *YarnFunction* ที่คืน `string`
ให้นักเขียนเอาไปแทรกในบรรทัดพูดได้ — **รูปแบบของโค้ดใช้งานจริงกว่า** และแก้ B2 ได้ตรงกว่าด้วย
เพราะ "เลือกสูตร" กับ "แสดงข้อความ" เป็นคนละจังหวะ ข้อเสนอ: แยกให้ชัดเป็น 2 ชั้น

```csharp
// ชั้นเลือก — command, เขียนลง DrinkOrderContext อย่างเดียว ไม่คืนค่า (ตรง GDD §19)
[YarnCommand("order_by_name")]        void OrderFixedByName(string recipeName);
[YarnCommand("order_by_flavor")]      void OrderFixedByFlavor(string recipeName);
[YarnCommand("order_random_name")]    void OrderRandomByPreference(string characterId);
[YarnCommand("order_random_flavor")]  void OrderRandomByPreferenceFlavor(string characterId);
[YarnCommand("order_by_type")]        void OrderFixedByType(string drinkType);      // แก้ S9

// ชั้นอ่าน — function, อ่านจาก context ที่เลือกไว้แล้ว จึงได้แก้เดียวกันเสมอ (แก้ B2)
[YarnFunction("order_name")]          static string OrderName();
[YarnFunction("order_flavor")]        static string OrderFlavor();
[YarnFunction("order_type")]          static string OrderType();
```

⚠️ ชื่อ `[YarnCommand]`/`[YarnFunction]` เดิม 4 ตัว (`Order_Cocktail_*`) **ต้องคงไว้เป็น wrapper**
จนกว่าจะได้ตรวจ `.yarn` ครบทุกไฟล์ — ดู §9.3

**แก้ S10 ที่ `OrderService`** ให้ตรง GDD §19.2:

```csharp
// ผิด (ปัจจุบัน): สุ่มประเภทก่อน แล้วค่อยสุ่มสูตรในประเภทนั้น
var type = favorites[Random.Range(0, favorites.Count)];
return repo.GetRandom(type);

// ถูก (GDD §19.2): รวมผู้สมัครทั้งหมดก่อน แล้วสุ่ม uniform ครั้งเดียว
var candidates = repo.GetDrinks().Where(r => favorites.Contains(r.AlcoholStrength)).ToList();
return candidates[Random.Range(0, candidates.Count)];
```

GDD §24 ระบุว่า weighted random เป็นงาน **เลื่อนออกไป** — v1 ใช้ uniform เท่านั้น ให้เขียนคอมเมนต์
กำกับไว้ที่เมธอดนี้ว่า uniform เป็นการตัดสินใจ ไม่ใช่ของค้าง

### 6.5 ข้อมูลลูกค้า (GDD §7 / §19.1) — แก้ S11

```csharp
// ปัจจุบัน: MonoBehaviour ตัวเดียวในซีน ถือ dictionary ของทุกตัวละคร
public class CharacterData : MonoBehaviour {
    public SerializedDictionary<NPC_Name, List<TypeOfCocktail>> NPC_Favorite_Drink;
}

// GDD §19.1: ScriptableObject หนึ่งใบต่อหนึ่งตัวละคร
[CreateAssetMenu(menuName = "Bar410/Customer")]
public class SO_Customer : ScriptableObject {
    public NPC_Name             Id;
    public string               CustomerName;
    public List<TypeOfCocktail> PreferredDrinkTypes;
    // relationshipValue ไม่เก็บที่นี่ — GDD §22 บอกว่าแหล่งความจริงคือ Yarn $rel_<id>
    // ถ้าต้องอ่านในโค้ด ให้ดึงจาก VariableStorage ไม่ใช่ mirror ไว้สองที่
}
```

ได้อะไร: แก้ข้อมูลตัวละครโดยไม่ต้องเปิดซีน, diff ใน git อ่านออก, และ `ICustomerPreferences`
(§4.3) มี implementation จริงที่ทดสอบได้โดยไม่ต้องมีซีน

> **`relationshipValue` — D8 ตัดสินแล้ว: อ่านจาก `VariableStorage` แหล่งเดียว**
> GDD ขัดกันเองระหว่าง §19.1 (ให้ mirror ลง `CustomerSO`) กับ §22 (แหล่งความจริงคือ `$rel_<id>`
> ที่ลง save data) — ตัดสินให้ **§22 ชนะ** `SO_Customer` จึงไม่มีฟิลด์ `relationshipValue`
>
> เหตุผล: ScriptableObject ที่ถูกเขียนตอน runtime จะติดค้างข้ามรอบเล่นใน Editor (ค่าไม่รีเซ็ตตอน
> ออก Play Mode) ทำให้ทดสอบไม่ตรง และการมีค่าเดียวกันอยู่ 2 ที่ต้องเขียนโค้ด sync ที่พังเงียบได้
> **GDD §19.1 ต้องแก้ให้ตัดฟิลด์นี้ออก** — ดู §12

```csharp
// อ่านค่าความสัมพันธ์: จุดเดียว ไม่ mirror
public float GetRelationship(NPC_Name id)
    => _dialogueRunner.VariableStorage.TryGetValue($"$rel_{id}", out float v) ? v : 0f;
```

## 7. ผลลัพธ์ที่คาดหวัง

| ตัวชี้วัด | ก่อน | หลัง |
|---|---:|---:|
| บรรทัดรวมใน Cocktail System | 2,104 | ~1,550 |
| ไฟล์ที่ยาวเกิน 200 บรรทัด | 3 | 0 |
| ไฟล์ที่ยาวเกิน 150 บรรทัด | 4 | 0 |
| คลาสที่มีมากกว่า 3 หน้าที่ | 3 | 0 |
| การสแกนสูตรต่อการอัปเดต shaker 1 ครั้ง | 5 | 1 |
| ลูป TryGetComponent เปิด/ปิด interactable | 3 ก๊อป | 1 |
| จุดที่ hardcode `errors <= 2` | 6 | 1 |
| `FindAnyObjectByType` / `FindFirstObjectByType` ใน hot path | 6 | 0 |

---

## 8. ลำดับงาน

แต่ละเฟสคอมไพล์ผ่านและเล่นทดสอบได้ด้วยตัวเอง **ห้ามทำคร่อมเฟส** — ถ้า regression โผล่ จะแยกไม่ออกว่ามาจากอะไร

| Phase | งาน | แตะซีน/asset? | เปลี่ยนพฤติกรรม? | ความเสี่ยง |
|---:|---|:---:|:---:|---|
| **0** | ลบโค้ดตาย §2.2 + แก้บั๊ก B1, B3, B6, B7, B8 | ✘ | เล็กน้อย | ต่ำ |
| **1** | แยก `UtilityDrink` → `Domain/` ตาม §4.1 + generic `IngredientMath` (§4.1.1) — **ย้ายโค้ดอย่างเดียว ผลลัพธ์ห้ามเปลี่ยน** | ✘ | ✘ | ต่ำ — pure C# |
| **2** | **ทำให้ตรง GDD** — S1, S2, S3, S5, S6, S7, S8 ภายใน `Domain/` | ✘ | ✔ **มาก** | กลาง — ดูด้านล่าง |
| **3** | แยก `CocktailShakerData` → 5 component, ลบ 3 UnityEvent, ยุบ B4 | ✔ **5 ที่** | ✘ | **สูงสุด** — ดู §9.2 |
| **4** | ดึง `DrinkOrderContext` / `OrderService` ออกมา + แก้ B2, S4, S10, S11 | ✔ เล็กน้อย | ✔ | กลาง |
| **5** | แยก Yarn adapter 4 ไฟล์ + แก้ B5 + เพิ่มโหมดสั่งที่ 5 (S9) — **ชื่อคำสั่งเดิมทุกตัว** | ✔ เล็กน้อย | ✔ | กลาง — ดู §9.3 |
| **6** | เขียน `BarSetupBridge` + `CocktailFlowBridge` แล้วผูกใน `[GameLoop]` | ✔ | ✔ | กลาง |
| **7** | ย้ายไฟล์ §4.5 + แยก `Enum_Class` เป็น partial §4.6 | ✘ (แต่แตะ `.meta`) | ✘ | ต่ำ ถ้าย้ายผ่าน Unity |
| **8** | งาน data: กรอก `CompatibleGlass` 20 ใบ (G2), เติม `GlassVisualData` ให้ครบ (G1), ตรวจ `Σ = 10` ทุกสูตร (S12) | ✔ | ✔ | ต่ำ แต่กินเวลา |

> **D6 ทำให้แผนเบาลงหนึ่งเฟส** — เพราะคง 3 ลิสต์ตามโค้ดเดิม จึง **ไม่มี migration script และไม่แตะ
> asset/prefab/scene ใด ๆ ในช่วงต้น** เฟสที่เคยเป็น "ย้ายโครงสร้างวัตถุดิบ" หายไปทั้งเฟส
> Phase 1–2 กลายเป็นงาน pure C# ล้วน เริ่มได้ทันทีโดยไม่ต้องรอใครเคลียร์ซีน

**Phase 1 กับ 2 ต้องแยกกันเด็ดขาด** — Phase 1 ย้ายโค้ดโดยผลลัพธ์ห้ามเปลี่ยน (คง logic "นับจำนวน
ชนิดที่ต่าง" ไว้ก่อน แม้จะผิด GDD) Phase 2 เปลี่ยนกติกาโดยไม่ย้ายไฟล์ ถ้ารวมกันแล้วคะแนนเพี้ยน
จะแยกไม่ออกว่าเพราะย้ายผิดหรือกติกาใหม่ทำงานถูกแล้ว **Phase 1 จบแล้วเกมต้องเล่นได้เหมือนเดิมทุกประการ**

ก่อนเริ่ม Phase 1 ให้เขียนตารางเคสทดสอบจาก **ตัวอย่างใน GDD §17.1 และบันได §18 ทั้ง 5 เคส**
ไว้เป็นตัววัด — Phase 1 ต้องผ่านตารางพฤติกรรมเดิม, Phase 2 ต้องผ่านตารางตาม GDD

**Phase 2 จะทำให้เกมยากขึ้นอย่างเห็นได้ชัด** — S1 เปลี่ยนจาก "นับชนิดที่ผิด" เป็น "รวมส่วนต่าง"
เครื่องดื่มที่เคยได้ `Acceptable` จำนวนมากจะกลายเป็น `Fail` ทันที **ต้องให้ design เล่นทดสอบและยืนยัน
ค่า `MaxTolerance = 3` ก่อนไปต่อ (D7)** ถ้าต้องปรับ ปรับที่ const เดียวใน `DrinkDeviation.cs`

Phase 0–1 ทำได้ทันที Phase 3 ควรทำหลัง Integration Plan item 8–9 (`BaseMiniGame` cleanup) เสร็จ
เพื่อไม่ให้ regression ของ panel animation ปนกับ regression ของ shaker

---

## 9. ความเสี่ยงเฉพาะ Unity

### 9.1 อย่าเปลี่ยนชื่อคลาส ScriptableObject
`S_Drink` และ `SO_CocktailList` มี asset อ้างอยู่ **27 ไฟล์** (`Cocktail Config/`) ชื่อคลาสอ่านยากก็จริง แต่การเปลี่ยนชื่อจะทำให้ทุก `.asset` หา `m_Script` ไม่เจอ **แผนนี้ไม่แตะชื่อทั้งสอง**

### 9.2 การผ่า MonoBehaviour = ค่าใน inspector หาย
`[FormerlySerializedAs]` ช่วยได้เฉพาะเมื่อ **ฟิลด์ยังอยู่ในคลาสเดิม** — การย้ายฟิลด์ข้ามคลาสไม่มีทางลัด ดังนั้นก่อน Phase 2:
1. บันทึกค่าปัจจุบันของ `CocktailShakerData` ในซีน (screenshot / คัดลอกบล็อก YAML)
2. เพิ่ม component ใหม่ทั้ง 5 ตัวลง GameObject เดิม แล้วผูกค่าคืน
3. **อย่าลบ `CocktailShakerData` จนกว่าจะยืนยันว่าทุก reference ย้ายครบ** — 3 จุดผูก `OnAddMixer/OnAddAlcohol/OnAddLiqueur` ในซีนต้องถูกเปลี่ยนก่อน

**ขอบเขตงานจริงของ Phase 2 ใหญ่กว่าที่คิด** — `CocktailShakerData` ไม่ได้อยู่แค่ซีนเดียว แต่มี **5 ก๊อป
อิสระ**: `GamePlayScene.unity`, `GamePlayScene 1.unity`, `New Drag Drop System.unity`,
`CocktailSystem.prefab`, `SystemGame.prefab` (ไม่ใช่ prefab instance ที่แชร์ต้นทาง — fileID ต่างกันหมด)
ต้องทำซ้ำทั้ง 5 ที่ ตัดสินใจก่อนเริ่มว่าจะยุบให้เหลือ prefab เดียวก่อนหรือยอมทำ 5 รอบ

**ทำ D5 (§10.1) ให้เสร็จก่อน Phase 2** — `GlassVisualData` เป็นฟิลด์ที่กรอกคืนยากที่สุด (sprite 12 ใบ)
ถ้าย้ายไป `SO_GlassVisualTable` ก่อน จะเหลือแค่ผูก reference ใบเดียวต่อที่

### 9.3 ชื่อ `[YarnCommand]` เป็นสัญญาสาธารณะ
`Day1_Demo.yarn` เรียก `<<wait_for_task SystemGame>>` และ `<<Enable_InteractableObject SystemGame false>>` **7 จุด** — ทั้งคู่เป็น *instance command* ที่ผูกกับชื่อ GameObject `SystemGame` ดังนั้น component ที่ถือคำสั่งเหล่านี้ (`CocktailYarnCommands`) **ต้องอยู่บน GameObject ชื่อ `SystemGame` เท่านั้น** ห้ามย้าย ห้ามเปลี่ยนชื่อคำสั่ง

`ProjectSettings/Packages/dev.yarnspinner/Assembly-CSharp-generated.ysls.json` regenerate เองตอนคอมไพล์ — ตอนนี้มัน dirty อยู่ใน working tree แล้ว (ตาม HSM doc §8) commit แยกให้ชัด

### 9.4 เคลียร์ working tree ก่อนเริ่ม
สถานะปัจจุบันมี `BookUI.cs` / `GameLoopManager.cs` ถูกลบ และ `NPC_Base.cs` ถูกย้ายเข้า `Cocktail System/` (ซึ่ง §4.5 จะย้ายออกอีกที) — **commit หรือ stash ให้เรียบก่อน** ไม่งั้นแยกไม่ออกว่า diff ไหนเป็นของ refactor

### 9.5 ย้ายไฟล์ต้องพา `.meta` ไปด้วย
ใช้ Unity `Move Asset` หรือ `git mv` **ทั้งคู่พร้อมกัน** ทุกครั้ง ถ้า `.meta` หลุด GUID จะเปลี่ยน แล้ว component ทุกตัวในซีน/prefab จะกลายเป็น `Missing Script` — HSM doc §9 บันทึกไว้ว่าการย้าย 28 ไฟล์รอบก่อนรอดเพราะทำผ่าน `MoveAsset`

---

## 10. บันทึกการตัดสินใจเชิงออกแบบ

**ทุกข้อปิดหมดแล้ว** — ไม่มีคำถามค้าง

| # | คำถาม | ผล | ผลต่อแผน |
|---|---|---|---|
| ~~D1~~ | `_specialCocktailRepository` จะใช้จริงไหม? | **(ข) เขียน `CompositeDrinkRepository` รวมสองแหล่ง** | §4.7 · แก้ B11 ไปในตัว · S15 ค้างไว้โดยเจตนา |
| ~~D2~~ | `wait_for_task` จะอยู่ต่อหรือถอด? | **(ก) อยู่ต่อ** เปลี่ยนเงื่อนไขภายในเป็น `context.IsScored` | §6.3 · `.yarn` ทั้ง 3 จุดใน `Day1_Demo.yarn` ไม่ต้องแก้ · ถอดทีหลังได้ |
| ~~D3~~ | `IsWaitingForTask` เป็น `public static` | **(ก) คง static accessor ไว้ก่อน** | §4.3 `YarnTaskGate` เก็บ property เดิมไว้ให้ `SaveLoadManager.cs:197` · แยกเป็น task ต่างหากภายหลัง |
| ~~D4~~ | `Method.Build` | **(ข) ปล่อยไว้** — Integration Plan §1 บันทึกแล้วว่ายังไม่มี minigame รองรับ | S14 เลื่อนออกไป · เพิ่มเมื่อ Building minigame พร้อม |
| ~~D5~~ | `GlassVisualData` อยู่ในซีนหรือเป็น SO? | **(ข) `SO_GlassVisualTable`** | §10.1 · prerequisite ของ Phase 3 |
| ~~D6~~ | โครงสร้างวัตถุดิบ 3 ลิสต์ หรือลิสต์เดียว? | **ยึดโค้ด — คง 3 ลิสต์** + generic `IngredientMath` | §4.1.1 · ไม่ต้อง migrate ข้อมูล · §12 E1 |
| ~~D7~~ | `MaxTolerance` ของ deviation หลังแก้ S1 | **✅ ตัดสินแล้ว: `3`** — design ยืนยัน 2026-08-21 หลังทดสอบกับชุด 26 สูตร | ดู §10.3 |
| ~~D8~~ | `relationshipValue` mirror ลง SO หรือไม่? | **ยึด GDD §22** — อ่านจาก `VariableStorage` แหล่งเดียว | §6.5 · §12 E2 |
| ~~S3~~ | เกณฑ์แอลกอฮอล์ | **ยึด GDD §15.2** `1..5 → Low`, `>=6 → High` | โค้ดต้องแก้ใน Phase 2 |

### 10.1 รายละเอียด D5 — `GlassVisualData`

**ฟิลด์นี้คืออะไร** — `CocktailShakerData.cs:39–40` เก็บ dictionary ที่ map `GlassType` → sprite 3 ใบ
(แก้ว / น้ำ / น้ำแข็ง) มีผู้อ่านจุดเดียวคือ `SetColorAndGlass()` (`:186–197`) ซึ่งเอา
`S_Drink.CompatibleGlass` ของเครื่องดื่มปัจจุบันมา lookup แล้วส่งเข้า `WaterSlosh.UpdateVisual()`
พูดง่าย ๆ คือ **ตารางลุคของแก้ว** — เป็น config ของเกม ไม่ใช่สถานะของ object ตัวใดตัวหนึ่งในซีน

**สภาพจริงในโปรเจกต์** (ตรวจจากไฟล์ `.unity` / `.prefab` โดยตรง):

| ที่อยู่ | `GlassVisualData` |
|---|---|
| `[05]Scenes/MainScene/GamePlayScene.unity` | 4 entry — keys `01 04 02 03` = `Hi_ball, Martini, Rocks, Magrita`, 12 sprite GUID |
| `[05]Scenes/Deverlopment/New Drag Drop System.unity` | 4 entry — **GUID เหมือนกันทุกใบ** กับข้างบน (ก๊อปกันมา) |
| `[05]Scenes/MainScene/GamePlayScene 1.unity` | **ว่างเปล่า** (`m_keys:` / `m_values: []`) |
| `[04]Prefab/GameSystemPrefab/CocktailSystem.prefab` | **ไม่มีคีย์นี้ใน YAML เลย** → ฟิลด์ถูกเพิ่มหลัง prefab ถูกเซฟ, deserialize เป็น dictionary ว่าง |
| `[04]Prefab/GameSystemPrefab/SystemGame.prefab` | เหมือนกัน — ไม่มีคีย์ |

`CocktailShakerData` ทั้ง 5 ตัวนี้เป็น **component คนละตัวจริง ๆ** ไม่ใช่ prefab instance ที่แชร์ต้นทางเดียวกัน
(fileID ของ UnityEvent target ต่างกันหมด) → แก้ตารางที่หนึ่ง อีกสี่ที่ไม่ตาม

**นี่คือเหตุผลว่าทำไมต้องเลือก (ข)** ไม่ใช่เพราะ "เผื่ออนาคต" แต่เพราะการก๊อปเกิดขึ้นไปแล้ว และ
สองในห้าที่เริ่มไม่ตรงกัน `SO_GlassVisualTable` ทำให้ทั้งห้าที่ชี้ไป asset ใบเดียว — แก้ที่เดียวจบ
และ **ตัดฟิลด์นี้ออกจาก §9.2** ด้วย (เป็นฟิลด์ที่หนักที่สุดตอนผ่า `CocktailShakerData` — ถ้าย้ายไป
SO ก่อน Phase 2 จะเหลือแค่ผูก reference ใบเดียว แทนที่จะต้องกรอก sprite 12 ใบคืน)

```csharp
[CreateAssetMenu(fileName = "GlassVisualTable", menuName = "Bar410/Cocktails/Glass Visual Table")]
public class SO_GlassVisualTable : ScriptableObject
{
    [SerializedDictionary("Glass Type", "Visual")]
    public SerializedDictionary<GlassType, VisualCocktailGlass> Table = new();

    public bool TryGet(GlassType type, out VisualCocktailGlass visual);
}
// VisualCocktailGlass ต้องเลื่อนจาก nested class ใน CocktailShakerData ออกมาเป็น top-level
```

`ShakerVisualPresenter` เหลือ `[SerializeField] SO_GlassVisualTable _glassVisuals;` แค่บรรทัดเดียว

**บั๊กข้างเคียง 2 ตัวที่เจอตอนตรวจ** (ควรแก้ตอนย้าย ไม่ใช่ทีหลัง):

- **G1 — `GlassType` 8 ค่า แต่ตารางมีแค่ 4** ขาด `Cocktail`, `LongDrink`, `NotFix`, `None`
  ถ้า lookup ไม่เจอ `SetColorAndGlass` แค่ `Debug.LogWarning` แล้วปล่อยผ่าน → sprite แก้วค้างของเดิม
- **G2 — สูตร 20 จาก 26 ใบมี `CompatibleGlass = None`** ไฟล์ `.asset` ของ 20 ใบนั้นยังเก็บคีย์เก่าชื่อ
  `CompatibleGlasses:` (พหูพจน์, ค่าว่าง) ซึ่ง `S_Drink` ไม่มีฟิลด์นี้แล้ว → Unity ทิ้งค่า, `CompatibleGlass`
  deserialize เป็น `0` = `None` มีแค่ 6 ใบ (`01_JohnCollins`, `04_GinFizz`, `11_SeaBreeze`,
  `13_Greyhound`, `53_CranberryFizz`, `54_Grapefruit Spritz`) ที่มีค่าจริง
  **รวมกับ G1 แปลว่า 20 จาก 26 สูตรวิ่งเข้า path warning เสมอ แก้วไม่เคยเปลี่ยนลุค**

G2 ต้องกรอกค่าคืนด้วยมือใน Inspector 20 ใบ — เป็นงาน data ไม่ใช่งานโค้ด แต่ทำหลังย้ายเป็น SO แล้ว
จะเห็นช่องโหว่ชัดกว่า เพราะตารางกลางจะมีครบทุก `GlassType`

### 10.2 D6 — ตัดสินแล้ว: ยึดโค้ด (คง 3 ลิสต์แยกตามหมวด)

**สเปกเต็มอยู่ที่ §4.1.1** — `IIngredientEntry<TKey>`, `IngredientMath`, และจุดรวมผล 4 จุดที่ต้อง
เขียนคอมเมนต์กำกับไว้สำหรับการเพิ่มหมวดในอนาคต

| | (ก) คง 3 ลิสต์ ← **เลือกข้อนี้** | (ข) ลิสต์เดียวแบบแบน (GDD §15.1) |
|---|---|---|
| Type safety ต่อหมวด | ✔ ปุ่ม Mixer ตั้งเป็น Gin ไม่ได้ | ✘ enum เดียวรวมทุกอย่าง |
| Dropdown ใน Inspector | สั้น อยู่ในบริบท | ยาวขึ้นทุกครั้งที่เพิ่มวัตถุดิบ |
| เพิ่มชนิดใหม่ในหมวดเดิม | เพิ่มค่า enum ค่าเดียว | เพิ่มค่า enum ค่าเดียว (เท่ากัน) |
| เพิ่ม**หมวด**ใหม่ | enum + struct + ลิสต์ + จุดรวมผล 4 จุด | เพิ่มค่าใน `IngredientCategory` ค่าเดียว |
| โค้ดซ้ำ 9 เมธอด | ยุบด้วย generic เหลือ 3 | ไม่มีตั้งแต่แรก |
| ต้องแปลงข้อมูล | **✘ ไม่ต้องเลย** | ✔ 26 asset + ปุ่ม ~20 ตัว × 6 ที่ |
| ตรง GDD | ✘ → §12 E1 | ✔ |

**เหตุผลที่เลือก (ก):** รองรับการเพิ่มวัตถุดิบในอนาคตโดยยังคง type safety ต่อหมวดและ dropdown
ที่ใช้งานได้จริงตอนจัด content — ยิ่งวัตถุดิบเยอะ ข้อได้เปรียบนี้ยิ่งชัด และได้ผลพลอยได้คือ
**ไม่ต้องแตะข้อมูลเลย** ตัดความเสี่ยง migration ทิ้งทั้งก้อน

**สิ่งที่ต้องทำเพื่อชดเชยข้อเสียเดียวของ (ก)** — การเพิ่มหมวดใหม่มีจุดที่ต้องแก้ 4 จุด
`IngredientMath` ถูกออกแบบให้ไม่รู้จักหมวดใดเป็นพิเศษ จึงไม่ต้องเขียนอัลกอริทึมใหม่ แต่ **ต้องเขียน
คอมเมนต์ `// เพิ่มหมวดใหม่: แก้ที่นี่` กำกับครบทั้ง 4 จุด** ไม่งั้นคนที่มาเพิ่มทีหลังจะตกหล่นแล้วบั๊กเงียบ
(รายละเอียดจุดทั้ง 4 อยู่ใน §4.1.1)
### 10.3 D7 — `MaxTolerance` = 3 ✅ ยืนยันแล้ว

**design ยืนยันเมื่อ 2026-08-21 ว่าใช้ `3` ตาม GDD §17.3** — ถือเป็นการตัดสินใจ ไม่ใช่ค่าตั้งชั่วคราว

```csharp
// Domain/DrinkDeviation.cs
public const int MaxTolerance = 3;
```

เงื่อนไขที่ครบก่อนยืนยัน:

| # | เงื่อนไข | สถานะ |
|---|---|---|
| 1 | ซีนทดสอบใช้ `Normal_Cocktail` (26 สูตร) ไม่ใช่ `Demo_Normal_Cocktail` (6) | ✅ สลับแล้วในซีน `New Drag Drop System` |
| 2 | Phase 2 เสร็จครบ (S1 + S2 + S3) | ✅ |
| 3 | ทุกสูตรรวมได้ 10 หน่วย (S12) | ✅ validator ยืนยัน 26/26 |

**ถ้าจะปรับทีหลัง** แก้ที่ const เดียวใน `DrinkDeviation.cs` — ห้าม hardcode เลข 3 ที่อื่น

---

## 11. ตารางอ้างอิงกลับ GDD / Traceability

| GDD § | หัวข้อ | ที่อยู่หลัง refactor | ช่องว่างที่ต้องแก้ |
|---|---|---|---|
| §7, §19.1 | Customer Data | `Session/SO_Customer.cs`, `ICustomerPreferences` | S11 · D8 ปิดแล้ว → **GDD ต้องแก้** |
| §12, §19 | Order Generation | `Yarn/CocktailYarnCommands.cs`, `Session/OrderService.cs` | S9, S10 |
| §15.1 | Ingredient Category / Type | `DrinkIngredients.cs`, `Domain/IngredientMath.cs` | ✅ D6 ปิดแล้ว — **ยึดโค้ด, GDD ต้องแก้** (§12 E1) |
| §15.2 | Alcohol → DrinkType | `Domain/AlcoholClassifier.cs` | S3 — ✅ ปิดแล้ว: **ยึด GDD, โค้ดต้องแก้** (Phase 2) |
| §16 | Recipe Data Model | `S_Drink.cs` + editor validation ใหม่ | S12 · S14 เลื่อน (D4) · S15 เลื่อน (D1) |
| §16 | คลังสูตร / Repository | `Cocktail/CompositeDrinkRepository.cs` | ✅ D1 ปิดแล้ว — §4.7, แก้ B11 |
| §17.1 | Deviation Formula | `Domain/DrinkDeviation.cs` | **S1** · `MaxTolerance` = D7 ⚠️ |
| §17.2 | Tie-break | `DrinkDeviation.FindBestMatch` | ✔ ถูกแล้ว — เพิ่มคอมเมนต์ |
| §17.3 | Recipe Flags | `Domain/DrinkFlagResolver.cs` | S2, S6, S7 |
| §18 | Satisfaction | `Domain/SatisfactionEvaluator.cs` | **S4** |
| §18.1 | Pricing | `Domain/PricingRules.cs` | S5 |
| §18.2 | Relationship Impact | `DrinkOrderContext.RelationshipDelta` → Yarn | ยังไม่มีเลย |
| §21 | Glass Selection | `ShakerVisualPresenter` + `SO_GlassVisualTable` | S13, G1, G2, D5 |
| §21.1 | Color Resolution | `Domain/DrinkColorBlender.cs` | S8 |
| §24 | Open Items | — | ดูด้านล่าง |

### 11.1 GDD §24 — สิ่งที่ **ห้ามสร้าง** ในรอบนี้

GDD §24 ระบุ 3 รายการที่เลื่อนออกไปแล้ว แผนนี้ตั้งใจ**ไม่**สร้างมัน แต่เว้นรอยต่อไว้:

| GDD §24 | สถานะ | รอยต่อที่แผนนี้เตรียมไว้ |
|---|---|---|
| UI ให้ผู้เล่นเลือกเองเมื่อหลายสูตร match เท่ากัน (§17.2) | เลื่อน — v1 ใช้ index-order | `FindBestMatch` คืน `RecipeMatch` ตัวเดียว; ถ้าจะทำ UI ค่อยเพิ่ม overload คืน `List<RecipeMatch>` — ผู้เรียกเดิมไม่พัง |
| ระบบตกแต่งแบบ free-placement (§21) | เลื่อน — v1 slot-based | ไม่อยู่ในขอบเขต refactor นี้เลย `GarnishState` ยังเป็น stub |
| สุ่มสูตรแบบมี weight (§19.2) | เลื่อน — v1 uniform | `OrderService.PickForCustomer` แยกเป็นเมธอดเดียว เปลี่ยนวิธีสุ่มภายหลังแตะที่เดียว |

**อย่าเผลอสร้างของพวกนี้ระหว่าง refactor** — การเห็นโค้ดที่ "ยังไม่รองรับหลาย match" แล้วอยาก
เติมให้ครบเป็นกับดักที่ทำให้ scope บาน GDD ตัดสินใจไปแล้วว่า v1 ไม่ทำ

---

## 12. GDD ที่ต้องแก้ตาม / GDD Errata — ✅ ใช้แล้ว 2026-08-21

> E1 และ E2 ถูกนำไปแก้ใน `GDD_Bar410_Master.md` เรียบร้อย พร้อมเพิ่ม §21.0 (กติกา `NotFix`)

การตัดสินใจ 2 ข้อทำให้ **GDD กลายเป็นฝ่ายที่ไม่ตรง** — ถ้าไม่แก้เอกสาร คนที่อ่าน GDD แล้วมาอ่านโค้ด
จะคิดว่าเจอบั๊กแล้ว "แก้" กลับไปเป็นของเดิม ต้องแก้ที่ `~/WorkSpace/Bar410/GDD_Bar410_Master.md`

### E1 — §15.1 / §16 โมเดลวัตถุดิบ (จากการตัดสิน D6)

GDD §15.1 กำหนด `IngredientType` แบบแบน + `IngredientCategory` และ §16 ใช้
`List<IngredientAmount> ingredients` ลิสต์เดียว — **ตัดสินแล้วว่าโค้ดยึดโครงสร้าง 3 ลิสต์แยกตามหมวด**
เพื่อคง type safety และ dropdown ที่ใช้งานได้จริงเมื่อวัตถุดิบเพิ่มขึ้น GDD จึงต้องบันทึกตามจริง

```diff
  ### 15.1 ชนิดวัตถุดิบ / Ingredient Type
- public enum IngredientCategory { BaseSpirit, Liqueur, Mixer }
- public enum IngredientType { Gin, Vodka, ..., TripleSec, ..., Syrup, Soda, ... }
+ // แยกเป็น enum ต่อหมวด เพื่อคง type safety ตอนจัด content
+ public enum BaseSpirit { None, Vodka, Gin, Whiskey, Rum, Tequila }
+ public enum Liqueur    { None, Triplesec, DryVermouth, SweetVermouth, Campari }
+ public enum Mixer      { None, Soda, CranberryJuice, LimeJuice, LemonJuice,
+                          GrapefruitJuice, Syrup, PepperMint, OrangeJuice }

  ## 16. โมเดลข้อมูลสูตรเครื่องดื่ม
- public List<IngredientAmount> ingredients;   // ต้องรวม = 10
+ public List<AlcoholIngredient> AlcoholList;  // รวมทั้ง 3 ลิสต์ = 10
+ public List<LiqueurIngredient> LiqueurList;
+ public List<MixerIngredient>   MixerList;
```

**เงื่อนไข `Σ = 10` ยังคงเดิมทุกประการ** — แค่รวมข้ามสามลิสต์แทนลิสต์เดียว
editor validation ใน §16 จึงยังใช้ได้ ไม่ต้องแก้เจตนา

⚠️ ผลข้างเคียงที่ต้องเขียนกำกับใน GDD ด้วย: **การเพิ่มหมวดวัตถุดิบใหม่** (เช่น Bitters, Garnish)
ในโครงสร้างนี้แพงกว่าแบบแบน — ต้องเพิ่ม enum + struct + ลิสต์ + แก้จุดรวมผล 4 จุดในโค้ด
ให้ design รู้ต้นทุนนี้ก่อนตัดสินใจเพิ่มหมวด (การเพิ่ม *ชนิด* ในหมวดเดิมยังถูกเหมือนเดิม)

### E2 — §19.1 `CustomerSO.relationshipValue` (จากการตัดสิน D8)

```diff
  public class CustomerSO : ScriptableObject
  {
      public string customerName;
      public List<DrinkType> preferredDrinkTypes;   // List<DrinkType> ไม่ใช่ List<Recipe>
-     public float relationshipValue;               // mirror จาก Yarn $rel_<id>, 0-10
  }
+ // ค่าความสัมพันธ์ไม่เก็บที่นี่ — แหล่งความจริงเดียวคือ Yarn $rel_<id> (ดู §22)
+ // อ่านผ่าน DialogueRunner.VariableStorage; ห้าม mirror ลง ScriptableObject
+ // เหตุผล: SO ที่เขียนตอน runtime ติดค้างข้ามรอบเล่นใน Editor และการมีค่าเดียวกัน 2 ที่
+ //          ต้องมีโค้ด sync ที่พังเงียบได้
```

เดิม §19.1 กับ §22 ขัดกันเองอยู่แล้ว — E2 แค่ทำให้ §19.1 ตามหลัง §22

### E3 — ยืนยันว่าสิ่งเหล่านี้ *ไม่* ต้องแก้ GDD

รายการที่โค้ดต่างจาก GDD **แต่ GDD ถูก** โค้ดจึงต้องเปลี่ยนตาม (ไม่ใช่ errata):
S1, S2, **S3**, S4, S5, S6, S7, S8, S9, S10, S11, S12, S13, S14 — ทั้งหมดอยู่ใน Phase 1–6

**S3 กลับมาอยู่ในรายการนี้** หลังทบทวนรอบสอง: เกณฑ์แอลกอฮอล์ยึด GDD §15.2
(`1..5 → Low`, `>=6 → High`) โค้ดที่เขียน `>= 5 → High` เป็น off-by-one ที่ต้องแก้ใน Phase 2

⚠️ ผลของการแก้ S3 ที่ต้องแจ้ง design: สูตรที่มี alcohol รวม **5 หน่วยพอดี** จะย้ายจาก
`HighAlcohol` ไป `LowAlcohol` ซึ่งกระทบ GDD §19.2 (การสุ่มตามความชอบของลูกค้า) และ §18 เคส 3/4
(เทียบ `servedType == orderedType`) — **ให้ไล่ดู 26 สูตรว่ามีใบไหนอยู่ตรงเส้น 5 พอดีบ้าง**
เพราะออเดอร์ของลูกค้าที่ชอบ High อาจเปลี่ยนชุดผู้สมัครไปเลย

---

## 13. บันทึกการตัดสินใจ / Decision Log

| # | รายการ | ผล | เหตุผล | ผลต่อแผน |
|---|---|---|---|---|
| 1 | เชื่อม Cocktail System เข้า HSM ที่ state ไหน | **ทั้ง `PrepareBarPhase` และ `PrepareDrinksPhase`/`ServeState`** | ระบบมี 2 หน้าที่จริง: จัดโต๊ะบาร์ กับ ชง/เสิร์ฟ | §6.1 + §6.2 — 2 bridge |
| 2 | D5 `GlassVisualData` | **ScriptableObject** (`SO_GlassVisualTable`) | ตารางถูกก๊อปไปแล้ว 5 ที่ และ 3 ใน 5 ไม่ตรงกัน | §10.1 — prerequisite ของ Phase 3 |
| 3 | S3 เกณฑ์แอลกอฮอล์ | **ยึด GDD** `1..5 → Low`, `>=6 → High` | GDD §15.2 เขียนกำกับชัดว่า 5 = Low ตั้งใจ | โค้ดต้องแก้ใน Phase 2 · ต้องไล่ดูสูตรที่อยู่ตรงเส้น 5 |
| 4 | D6 โครงสร้างวัตถุดิบ | **ยึดโค้ด** คง 3 ลิสต์แยกตามหมวด | คง type safety ต่อหมวด + dropdown ใช้งานได้จริงเมื่อวัตถุดิบเพิ่มขึ้น | §4.1.1 · ไม่ต้อง migrate ข้อมูล · §12 E1 GDD ต้องแก้ |
| 5 | D8 `relationshipValue` | **ยึด GDD §22** อ่านจาก `VariableStorage` แหล่งเดียว | SO ที่เขียนตอน runtime ติดค้างข้ามรอบเล่นใน Editor | §12 E2 — GDD §19.1 ต้องแก้ |
| 6 | D1 `_specialCocktailRepository` | **เขียน `CompositeDrinkRepository` รวมสองแหล่ง** | เปิดรอยต่อไว้ให้พร้อมก่อนมี content ต้นทุนต่ำ (~40 บรรทัด) | §4.7 · แก้ B11 · ยังไม่ทำ S15 (`unlockedByDefault`) |
| 7 | D2 `wait_for_task` | **อยู่ต่อ** เปลี่ยนเงื่อนไขภายในเป็น `context.IsScored` | `.yarn` ไม่ต้องแก้เลย แต่แหล่งความจริงย้ายไป HSM แล้ว | §6.3 · ถอดทีหลังได้เมื่อ flow เดินครบวง |
| 8 | D3 `IsWaitingForTask` static | **คง static accessor ไว้ก่อน** | ไม่ลาก save system เข้ามาในขอบเขต refactor นี้ | §4.3 · แยกเป็น task ต่างหากภายหลัง |
| 9 | D4 / S14 `Method.Build` | **เลื่อนออกไป** | Integration Plan §1 บันทึกแล้วว่ายังไม่มี Building minigame | เพิ่มเมื่อ minigame พร้อม — ไม่ใช่งานรอบนี้ |
| 10 | D7 `MaxTolerance` | **`3`** — design ยืนยัน 2026-08-21 | ทดสอบกับชุด 26 สูตรหลังแก้ S1 แล้ว | §10.3 · const เดียวใน `DrinkDeviation.cs` |

> **ข้อ 3 กับ 4 ถูกทบทวนกลับหนึ่งรอบ** — ครั้งแรกตัดสินสลับกัน (S3 ยึดโค้ด / D6 ยึด GDD)
> บันทึกไว้เพื่อไม่ให้มีใครย้อนกลับไปหาข้อสรุปเดิมโดยไม่รู้ว่าเคยพิจารณาแล้ว

**สถานะ: ปิดครบทุกข้อ** — ไม่มีการตัดสินใจค้าง
