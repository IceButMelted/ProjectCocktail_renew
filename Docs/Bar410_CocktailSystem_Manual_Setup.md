# Bar410 — Cocktail System: งานที่ต้องทำมือใน Unity

**Date:** 2026-08-21 · **Branch:** `GameLoop/main`
**คู่กับ:** `Bar410_CocktailSystem_Refactor_Plan.md` · `Bar410_CocktailSystem_Refactor_Report.md`
**หน้าที่ของแต่ละไฟล์:** `Bar410_CocktailSystem_Architecture.md`
> 📍 **เริ่มที่นี่ถ้าเพิ่งรับงานต่อ:** `Bar410_CocktailSystem_HANDOFF.md`

โค้ดของ Phase 3–8 เขียนครบแล้วและคอมไพล์ผ่าน **เกมยังเล่นได้เหมือนเดิมโดยไม่ต้องทำอะไรในเอกสารนี้เลย**
— ทุกอย่างมี compatibility shim รองรับอยู่ ที่เหลือคือการเก็บกวาดให้สถาปัตยกรรมใหม่ทำงานเต็มตัว

เรียงตามความสำคัญ: **§1 ทำก่อนเล่นทดสอบ** → **§2–4 เก็บกวาด** → **§5–6 เปิดใช้ของใหม่**

> 🔧 ตรวจงานตัวเองได้ตลอดด้วยเมนู **`Bar410 > Validate Cocktail Data`**
> มันจะไล่ทุก `S_Drink` และทุก `SO_GlassVisualTable` แล้วรายงานว่าอะไรขาด

---

## 1. ⚠️ ทำก่อนเล่นทดสอบ — ✅ เสร็จแล้ว

### 1.1 สลับซีนไปใช้ชุดสูตรเต็ม — ✅ ทำแล้วในซีน `New Drag Drop System`

ตอนนี้ซีนชี้ไปคนละชุด:

| ซีน | ชี้ไป | จำนวนสูตร |
|---|---|---:|
| `[05]Scenes/Deverlopment/New Drag Drop System.unity` | `Demo_Normal_Cocktail` | **6** |
| `[05]Scenes/MainScene/GamePlayScene.unity` | `Demo_Normal_Cocktail` | **6** |
| `[05]Scenes/MainScene/GamePlayScene 1.unity` | `Normal_Cocktail` | 26 |

**ทำ:** เลือก GameObject `SystemGame` → `CocktailSystemManager` → ช่อง **Normal Cocktail Repository**
→ เปลี่ยนเป็น `Normal_Cocktail.asset`

**ทำไม:** จำนวนสูตรเปลี่ยนโอกาสเจอสูตรใกล้เคียงอย่างมีนัยสำคัญ ค่า `MaxTolerance` ที่ยืนยันจากซีน
6 สูตรใช้กับเกมจริง 26 สูตรไม่ได้

### 1.2 ค่า `MaxTolerance` (D7) — ✅ ยืนยันแล้ว = 3

design ยืนยันเมื่อ 2026-08-21 ว่าใช้ `3` ตาม GDD §17.3 หลังทดสอบกับชุด 26 สูตร
เป็นการตัดสินใจแล้ว ไม่ใช่ค่าตั้งชั่วคราว

```csharp
// Domain/DrinkDeviation.cs — ถ้าจะปรับทีหลัง แก้ที่นี่ที่เดียว
public const int MaxTolerance = 3;
```

---

## 2. เก็บกวาด Missing Script จาก `BookUI`

commit `99f9cef` ลบ `BookUI.cs` ทิ้ง (ถูกแทนที่ด้วย `BookUI_V2`) เหลือ component ค้าง 5 จุด
**ไม่มีจุดไหนกระทบบิลด์** เพราะซีนที่ลงบิลด์ใช้ `BookUI_V2` อยู่แล้ว

| ที่ | สภาพ | สถานะ |
|---|---|---|
| `New Drag Drop System.unity` | มี component ไม่มี UnityEvent ผูก | ✅ **ลบแล้ว** |
| `GamePlayScene.unity` | มี component ไม่มี UnityEvent ผูก | ยังไม่ทำ — นอกขอบเขตรอบนี้ |
| `GamePlayScene 1.unity` | มี 4 binding ที่ยังทำงาน · ไม่อยู่ใน Build Settings | ยังไม่ทำ |
| `SystemGame.prefab` | มี 4 binding · ไม่มีซีนไหนอ้างถึง | ยังไม่ทำ |
| `CocktailSystem.prefab` | มี 4 binding · ไม่มีซีนไหนอ้างถึง | ยังไม่ทำ |

**ทำ (สำหรับที่เหลือ):** เปิดแต่ละที่ → หา `Canvas_Overlay-Book` → ลบ component ที่ขึ้น
`Missing (Mono Script)` → ถ้ายังไม่มี `BookUI_V2` ให้เพิ่มแล้วผูก `_pages` ใหม่

> ในซีน `New Drag Drop System` ยังเหลือ missing script อีก 2 ตัวที่
> `Dialogue System .../Current Text Line` — **มีมาก่อนงาน refactor นี้ ไม่เกี่ยวกับ Cocktail System**
> จงใจไม่แตะ

### 2.1 ปุ่ม `AddIce` ที่ชี้ไปเมธอดที่ถูกลบ

`CocktailSystem.prefab` มี OnClick เรียก `IngredientButtonUI.AddIce()` แบบไม่มี argument
เมธอดนั้นถูกลบเพราะ body ว่างมาแต่ไหนแต่ไร

**ทำ:** เปลี่ยนไปเรียก `AddIce(bool)` · **ผลกระทบตอนนี้: ไม่มี** — prefab นี้ orphan

---

## 3. งานข้อมูลสูตรเครื่องดื่ม — ✅ เสร็จแล้ว

รันเมนู `Bar410 > Validate Cocktail Data` ตอนนี้ขึ้น **"validation passed"**

| ตรวจ | ผล |
|---|---|
| สูตรทั้งหมด | 26 |
| `Σ ingredients == 10` (GDD §15) | ✅ ผ่านครบ |
| สูตรซ้ำกันเป๊ะ (GDD §16) | ✅ ไม่มี |
| `CompatibleGlass` ถูกกำหนด | ✅ ครบทั้ง 26 ใบ |

**ที่ทำไป:** 20 ใบที่เคยเป็น `None` (ข้อมูลเดิมหายไปเพราะคีย์เก่า `CompatibleGlasses:`)
ถูกตั้งเป็น **`Hi_ball` ทั้งหมดตามที่ตกลง** — เป็นค่าชั่วคราวเพื่อให้ระบบภาพทำงานได้ก่อน

การใช้แก้วตอนนี้: `Hi_ball` ×23 · `Rocks` ×2 · `Magrita` ×1

> 🔖 **งานที่เหลือของ design:** ไล่ตั้งแก้วให้ตรงกับเครื่องดื่มจริง เช่น Martini ควรเป็น
> `Martini`, Old Fashioned ควรเป็น `Rocks` · หรือตั้งเป็น **`NotFix`** ถ้าอยากให้ผู้เล่นเลือกเอง (ดู §4.1)

---

## 4. ตารางลุคแก้ว (D5) — ✅ เสร็จแล้ว

`Cocktail Config/GlassVisualTable.asset` มีครบ **7 entry** ทุกค่าของ `GlassType` แล้ว
(`Cocktail`, `LongDrink`, `NotFix` ที่ยังขาดถูกเติมด้วย visual ของ `Hi_ball` ไว้ก่อนตามที่ตกลง)

ในซีน `New Drag Drop System` ผูก asset นี้เข้ากับ `CocktailShakerData` และ
`ShakerVisualPresenter` เรียบร้อย — ไม่ใช้ตารางแยกรายซีนอีกต่อไป

> 🔖 **งานที่เหลือของ art:** วาด/ผูก sprite จริงของ `Cocktail`, `LongDrink`, `NotFix`
> ตอนนี้ทั้งสามใช้ภาพเดียวกับ Hi_ball

### 4.1 `GlassType.NotFix` = ผู้เล่นเลือกแก้วเอง

กติกานี้ implement ในโค้ดแล้ว (GDD §21):

| สูตรตั้งไว้เป็น | ผลตอนชง |
|---|---|
| แก้วเจาะจง เช่น `Martini` | สูตรกำหนด — **ทับ**สิ่งที่ผู้เล่นเลือก |
| **`NotFix`** | **สิ่งที่ผู้เล่นเลือกคงอยู่** ไม่ถูกทับ |
| `NotFix` แต่ผู้เล่นยังไม่เลือก | ใช้ `DrinkBuilder.DefaultGlass` (= `Hi_ball`) |
| ไม่ตรงสูตรใดเลย (Fail b) | `DrinkBuilder.UnmatchedGlass` (= `Rocks`) — ให้ดูออกว่าไม่ใช่เครื่องดื่มจริง |

ทดสอบแล้ว:

```
recipe=Martini  player=Rocks  -> Martini   (สูตรกำหนด ทับของผู้เล่น)
recipe=NotFix   player=Rocks  -> Rocks     (ผู้เล่นเลือกเอง คงไว้)
recipe=NotFix   player=None   -> Hi_ball   (ยังไม่เลือก -> DefaultGlass)
```

**ยังขาด UI ให้ผู้เล่นเลือก (S13)** — เมื่อทำ UI แล้วให้เรียก `ShakerContents.SetGlass(glass)`
จุดเดียว ค่าจะไม่ถูกเขียนทับเองแล้ว เพราะ `DrinkBuilder.ApplyGlass` กันไว้ให้

---

## 5. ย้ายออกจาก `CocktailShakerData` (Phase 3)

### สถานะต่อซีน

| ที่ | สถานะ |
|---|---|
| **`New Drag Drop System.unity`** | ✅ **ย้ายครบแล้ว — ลบ shim ออกจากซีนเรียบร้อย** |
| `GamePlayScene.unity` · `GamePlayScene 1.unity` · 2 prefab | ยังใช้ shim อยู่ — ทำงานได้ปกติ |

`CocktailShakerData.cs` **ยังต้องอยู่ในโปรเจกต์** จนกว่าทั้ง 4 ที่ที่เหลือจะย้ายครบ

### สิ่งที่ทำไปในซีนนี้

1. `ShakerContents`, `ShakerVisualPresenter`, `IngredientButtonGroup` ×2, `ShakerPanelController`,
   `ShakerTooltip` อยู่บน `SystemGame/CocktailSystem/CocktailShaker` และผูกค่าครบ
2. `CocktailSystemManager` เลิกพึ่ง shim — ใช้ฟิลด์ใหม่ **`_shakerContents`** และ
   **`_ingredientButtons`** แทน (ฟิลด์ `_cocktailShakerData` ยังอยู่เป็น fallback ให้ซีนอื่น)
3. ซ่อม UnityEvent ที่ target หลุดตอนลบ shim **15 จุด** ตามตารางนี้

| เดิม | ตอนนี้ |
|---|---|
| `CocktailShakerData.ResetShaker` / `.ResetCocktailData` | `ShakerContents.Clear` |
| `CocktailShakerData.SetIngredientActive` | `IngredientButtonGroup.SetInteractable` |
| `CocktailShakerData.StopFill` | `ShakerVisualPresenter.StopFill` |
| `CocktailShaker.SetCanShow*UI` | `ShakerPanelController.SetCanShow*UI` |

### ⚠️ เหลือ 3 จุดที่ต้องตัดสินใจเอง

| ที่ | binding ที่หลุด | ทำไมถึงเดาให้ไม่ได้ |
|---|---|---|
| `SystemGame/MiniGameSystem` ×2 | `CocktailShaker.set_Interactable` | `CocktailShaker` ไม่มีในซีนนี้แล้ว · ตัวที่ทำหน้าที่แทนน่าจะเป็น `DragableObject.Interactable` บน `CocktailShaker` GameObject แต่ต้องยืนยันว่าตั้งใจล็อก shaker ตอนเล่น minigame |
| `Panel - Serve/BTN_Serving` | `GameObject.SetActive` | GameObject ที่เคยชี้ไปถูกลบ — ไม่มีทางรู้ว่าตัวไหน |

เปิด Inspector ของสามปุ่มนั้นแล้วเลือก target ใหม่ หรือลบ entry ทิ้งถ้าไม่ต้องการแล้ว

---

## 6. เปิดใช้ของใหม่

### 6.1 `SO_Customer` แทน `CharacterData` (S11)

`CharacterData` ยังทำงานได้ปกติ (implement `ICustomerPreferences` แล้ว) แต่ GDD §19.1 อยากให้
ข้อมูลลูกค้าเป็น ScriptableObject หนึ่งใบต่อหนึ่งตัวละคร — แก้ได้โดยไม่ต้องเปิดซีน และอ่าน git diff รู้เรื่อง

**ทำ:**

**สถานะ:** สร้างแล้ว 3 ใบ (`Isla`, `Owen`, `Walter`) + `Demo_CustomerRoster` และผูกเข้า
`CocktailSystemManager` ในซีนนี้แล้ว

⚠️ **ยังขาด `Cole` กับ `Freya`** — เมื่อสองคนนี้สั่งเครื่องดื่ม `OrderService` จะ log
`"No preferences for X; using every type"` แล้วสุ่มจากทุกประเภท

⚠️ **ค่าใน roster ไม่ตรงกับ `CharacterData` เดิม** และ roster ชนะเพราะถูกผูกไว้:

| | CharacterData (เดิม) | Roster (ที่ใช้อยู่) |
|---|---|---|
| Cole | HighAlcohol | **ไม่มี** |
| Owen | HighAlcohol | NoneAlcohol, LowAlcohol |
| Walter | HighAlcohol | HighAlcohol, LowAlcohol |
| Freya | HighAlcohol, LowAlcohol | **ไม่มี** |
| Isla | NoneAlcohol | NoneAlcohol |

ถ้าตั้งใจเปลี่ยนค่าก็ไม่ต้องทำอะไร ถ้ายังทำไม่ครบ ให้สร้างอีก 2 ใบแล้วใส่เข้า roster
(ค่าความชอบเป็น design data — ไม่ได้เดาให้)

**เมื่อครบแล้ว** จึงลบ component `CharacterData` ออกจากซีนได้

> `SO_Customer` **ไม่มีฟิลด์ `relationshipValue`** โดยเจตนา (D8) — ค่าความสัมพันธ์อยู่ที่
> Yarn `$rel_<id>` แหล่งเดียวตาม GDD §22 อ่านผ่าน `YarnVariableSync.ReadRelationship`

### 6.2 ต่อ HSM เข้ากับ Cocktail System (Phase 6)

สอง bridge เขียนเสร็จแล้วแต่ยังไม่ได้อยู่ในซีน จนกว่าจะเพิ่มเอง — flow จะยังไม่ขับ Cocktail System

**ทำ:** เลือก GameObject `[GameLoop]` แล้ว `Add Component` ทั้งสองตัว

**`BarSetupBridge`** (Level 1 · PrepareBarPhase)

| ช่อง | ผูกกับ |
|---|---|
| Game Loop | `GameLoopFSM` บน object เดียวกัน |
| Ingredients | `IngredientButtonGroup` ของชั้นวางวัตถุดิบ |
| Book UI | `IngredientButtonGroup` ของหนังสือ |
| Shaker Panels | `ShakerPanelController` |
| Shaker Contents | `ShakerContents` |
| On Placement Unlocked / Locked | UnityEvent → เปิด/ปิดระบบ drag-drop และเซฟ layout |

**`CocktailFlowBridge`** (Level 2–3)

| ช่อง | ผูกกับ |
|---|---|
| Game Loop | `GameLoopFSM` |
| Cocktail | `CocktailSystemManager` |
| Shaker Contents / Shaker Panels / Ingredients | component ที่เกี่ยวข้อง |

**ผลที่ได้:** `PrepareDrinks.Entered` จะรีเซ็ตแก้วอัตโนมัติตามกติกา HSM §3.1 (backtrack แล้วเครื่องดื่ม
รีเซ็ต) และ `Serve.Exited` จะคิดคะแนนให้ ปิด TODO ที่ `ServeState.cs:29` ค้างไว้

> `MinigameFlowBridge` **ยังไม่ได้เขียน** — อยู่ใน `Bar410_Minigame_Integration_Plan.md` §3.3
> ซึ่งเป็นแผนแยก `CocktailFlowBridge` จงใจไม่แตะ minigame เพื่อไม่ให้มีเจ้าของสองคน
> `ShakerContents.RequiredMinigame` เตรียมไว้ให้ bridge นั้นเรียกแล้ว

### 6.3 โหมดสั่งเครื่องดื่มที่ 5 (S9)

เพิ่ม Yarn command/function ใหม่โดยไม่แตะของเดิม — `.yarn` ที่มีอยู่ไม่ต้องแก้

```
<<order_customer SystemGame Cole>>          # ระบุลูกค้า (ทำก่อนก็ได้ ไม่ทำก็ได้)
<<order_by_type SystemGame HighAlcohol>>    # GDD §12 โหมด 5 — สั่งแค่ประเภท
{order_name()}        {order_flavor()}
{order_type()}        {order_satisfaction()}
```

โหมด 5 ไม่มีสูตรเป้าหมาย ความพึงพอใจจึงตัดสินจาก `servedType == orderedType` อย่างเดียว

---

## 7. สรุปสิ่งที่ยังไม่ได้ทำในโค้ด

| # | เรื่อง | ติดอะไร |
|---|---|---|
| S6 | GDD §17.3 — Fail(b) ต้องสุ่มชื่อจากคลังที่ designer เขียน | ยังไม่มีคลังชื่อ ตอนนี้ใช้ `"???"` (`DrinkBuilder.UnmatchedName`) |
| S8 | GDD §21.1 — Fail(b) ต้อง `BlendIngredientColors(poured)` | ไม่มีข้อมูลสีต่อวัตถุดิบใน data model ตอนนี้ใช้สีน้ำตาลขุ่นแทนสีดำ |
| S13 | GDD §10/§21 — ผู้เล่นเลือกแก้วเอง | **กติกา `NotFix` ทำแล้ว** (§4.1) เหลือแค่ UI ให้กดเลือก → เรียก `ShakerContents.SetGlass` |
| S14 | GDD §16 — `MixMethod.Build` | เลื่อนตาม D4 รอ Building minigame |
| S15 | GDD §16 — `unlockedByDefault` | เลื่อนตาม D1 ใช้ระบบ 2 asset (`CompositeDrinkRepository`) แทน |

ทั้งหมดมี `TODO(design, ...)` กำกับไว้ในโค้ดตรงจุดที่เกี่ยวข้อง

## 8. GDD — ✅ อัปเดตแล้ว

แก้ที่ `~/WorkSpace/Bar410/GDD_Bar410_Master.md` เรียบร้อย:

| # | หัวข้อ | แก้อะไร |
|---|---|---|
| **E1** | §15.1 / §16 | เปลี่ยนจาก `IngredientType` แบน + `IngredientCategory` เป็น **enum แยกต่อหมวด** (`BaseSpirit` / `Liqueur` / `Mixer`) พร้อม struct 3 ตัวและเงื่อนไข `Σ = 10` ข้ามสามลิสต์ · เขียนกำกับต้นทุนของการเพิ่มหมวดใหม่ไว้ให้ design รู้ |
| **E2** | §19.1 | ตัด `relationshipValue` ออกจาก `CustomerSO` + เพิ่ม `id` · อธิบายว่าทำไมห้าม mirror (§22 เป็นแหล่งความจริงเดียว) |
| **ใหม่** | §21.0 | บันทึกกติกา **`glassType = NotFix` = ผู้เล่นเลือกแก้วเอง** พร้อมตารางพฤติกรรมครบ 4 กรณี |

---

## 9. เรื่องที่ควรถาม design

**GDD §18 วัด deviation เทียบกับสูตรไหน?** อ่านตามตัวอักษร ถ้าผู้เล่นชง Negroni ได้เป๊ะทั้งที่ลูกค้า
สั่ง Martini → best-match deviation = 0 → เคส 1 → `Perfect` ซึ่งไม่น่าใช่เจตนา

โค้ดเลือกวัดเทียบ**สูตรที่ลูกค้าสั่ง** ส่วน best-match ใช้กำหนดตัวตนของเครื่องดื่มอย่างเดียว
เขียน `NOTE(design)` กำกับไว้ที่ `DrinkDeviation.MatchAgainst` แล้ว
