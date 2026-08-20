# Bar410 — Cocktail System: งานที่ต้องทำมือใน Unity

**Date:** 2026-08-21 · **Branch:** `GameLoop/main`
**คู่กับ:** `Bar410_CocktailSystem_Refactor_Plan.md` · `Bar410_CocktailSystem_Refactor_Report.md`
**หน้าที่ของแต่ละไฟล์:** `Bar410_CocktailSystem_Architecture.md`

โค้ดของ Phase 3–8 เขียนครบแล้วและคอมไพล์ผ่าน **เกมยังเล่นได้เหมือนเดิมโดยไม่ต้องทำอะไรในเอกสารนี้เลย**
— ทุกอย่างมี compatibility shim รองรับอยู่ ที่เหลือคือการเก็บกวาดให้สถาปัตยกรรมใหม่ทำงานเต็มตัว

เรียงตามความสำคัญ: **§1 ทำก่อนเล่นทดสอบ** → **§2–4 เก็บกวาด** → **§5–6 เปิดใช้ของใหม่**

> 🔧 ตรวจงานตัวเองได้ตลอดด้วยเมนู **`Bar410 > Validate Cocktail Data`**
> มันจะไล่ทุก `S_Drink` และทุก `SO_GlassVisualTable` แล้วรายงานว่าอะไรขาด

---

## 1. ⚠️ ทำก่อนเล่นทดสอบ

### 1.1 สลับซีนไปใช้ชุดสูตรเต็ม (จำเป็นสำหรับ D7)

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

### 1.2 ยืนยันค่า `MaxTolerance` (D7)

การแก้ S1 ทำให้เกมยากขึ้นชัดเจน เครื่องดื่มที่เคยได้ `Acceptable` จำนวนมากจะกลายเป็น `Fail`

**ทำ:** เล่นทดสอบแล้วลองค่า `2`, `3`, `4`, `5` แก้ที่ `Domain/DrinkDeviation.cs` **จุดเดียว**

```csharp
public const int MaxTolerance = 3;   // GDD §17.3
```

**เกณฑ์ตัดสิน:** ผู้เล่นที่ตั้งใจชงถูกแล้วพลาดเล็กน้อยควรได้ `Acceptable` · ชงมั่วต้องได้ `Fail`
(ก่อนแก้ S1 ชงมั่วก็ยังได้ `Acceptable`)

---

## 2. เก็บกวาด Missing Script จาก `BookUI`

commit `99f9cef` ลบ `BookUI.cs` ทิ้ง (ถูกแทนที่ด้วย `BookUI_V2` แล้ว) เหลือ component ค้าง 5 จุด
**ไม่มีจุดไหนกระทบบิลด์** เพราะซีนที่ลงบิลด์ใช้ `BookUI_V2` อยู่แล้ว

| ที่ | สภาพ |
|---|---|
| `GamePlayScene.unity` | มี component แต่ไม่มี UnityEvent ผูก → ลบ component ทิ้งได้เลย |
| `New Drag Drop System.unity` | เหมือนกัน |
| `GamePlayScene 1.unity` | มี 4 binding ที่ยังทำงาน — ซีนนี้ไม่อยู่ใน Build Settings |
| `SystemGame.prefab` | มี 4 binding — prefab นี้ไม่มีซีนไหนอ้างถึง |
| `CocktailSystem.prefab` | มี 4 binding — prefab นี้ไม่มีซีนไหนอ้างถึง |

**ทำ:** เปิดแต่ละที่ → หา GameObject `Canvas_Overlay-Book` → ลบ component ที่ขึ้น
`Missing (Mono Script)` → ถ้าซีนนั้นยังไม่มี `BookUI_V2` ให้เพิ่มแล้วผูก `_pages` ใหม่

### 2.1 ปุ่ม `AddIce` ที่ชี้ไปเมธอดที่ถูกลบ

`CocktailSystem.prefab` มี OnClick เรียก `IngredientButtonUI.AddIce()` แบบไม่มี argument
เมธอดนั้นถูกลบเพราะ body ว่างมาแต่ไหนแต่ไร (ไม่เคยทำอะไร)

**ทำ:** เปลี่ยนไปเรียก `AddIce(bool)` แล้วติ๊กค่าตามต้องการ
**ผลกระทบตอนนี้:** ไม่มี — prefab นี้ orphan และซีนจริงใช้ `AddIce(bool)` อยู่แล้ว

---

## 3. งานข้อมูลสูตรเครื่องดื่ม

รันเมนู `Bar410 > Validate Cocktail Data` เพื่อดูรายการล่าสุดเสมอ ตัวเลข ณ วันที่เขียน:

| ตรวจ | ผล |
|---|---|
| สูตรทั้งหมด | 26 |
| `Σ ingredients == 10` (GDD §15) | ✅ **ผ่านครบทั้ง 26 ใบ** |
| สูตรซ้ำกันเป๊ะ (GDD §16) | ✅ ไม่มี |
| `CompatibleGlass` ถูกกำหนด | ❌ **มีแค่ 6 ใบ — อีก 20 ใบเป็น `None`** |

### 3.1 กรอก `CompatibleGlass` 20 ใบ (gap G2)

ไฟล์ `.asset` ของ 20 ใบนั้นยังเก็บคีย์เก่าชื่อ `CompatibleGlasses:` (พหูพจน์) ซึ่ง `S_Drink`
ไม่มีฟิลด์นี้แล้ว Unity จึงทิ้งค่าและ `CompatibleGlass` กลายเป็น `None`
**กู้อัตโนมัติไม่ได้ เพราะข้อมูลเดิมหายไปแล้วจริง ๆ**

ที่มีค่าอยู่แล้ว 6 ใบ: `01_JohnCollins`, `04_GinFizz`, `11_SeaBreeze`, `13_Greyhound`,
`53_CranberryFizz`, `54_Grapefruit Spritz` (Hi_ball ×3, Rocks ×2, Magrita ×1)

**ทำ:** เปิด `[02]Script/Cocktail System/Cocktail Config/Cocktail/` แล้วตั้ง **Compatible Glass**
ของอีก 20 ใบ · รัน validator ซ้ำจนไม่มีรายการเหลือ

**ถ้าไม่ทำจะเป็นยังไง:** `DrinkBuilder.ApplyRecipeIdentity` จะใส่ `GlassType.Rocks` ให้แทน
และ `ShakerVisualPresenter` จะ log warning ทุกครั้งที่หา entry ไม่เจอ — แก้วไม่เปลี่ยนลุค

---

## 4. รวมตารางลุคแก้วเป็น asset เดียว (D5)

ตอนนี้ตาราง `GlassType → sprite` ถูกก๊อปไว้ 5 ที่ และ 3 ใน 5 ไม่ตรงกัน:

| ที่ | สภาพ |
|---|---|
| `GamePlayScene.unity` | 4 entry, sprite 12 ใบ |
| `New Drag Drop System.unity` | 4 entry — GUID เหมือนกันเป๊ะ (ก๊อปกันมา) |
| `GamePlayScene 1.unity` | **ว่างเปล่า** |
| `CocktailSystem.prefab` / `SystemGame.prefab` | **ไม่มีคีย์นี้เลย** |

**ทำ:**

1. `Assets > Create > Bar410 > Cocktails > Glass Visual Table` → ตั้งชื่อ `GlassVisualTable`
2. คัดลอก 4 entry จาก `GamePlayScene.unity` มาใส่ (Hi_ball, Martini, Rocks, Magrita)
3. **เติมให้ครบทุก `GlassType` ที่ใช้จริง** — gap G1: enum มี 8 ค่า แต่ตารางเดิมมีแค่ 4
   ที่ขาดคือ `Cocktail`, `LongDrink`, `NotFix`
4. ทุกซีน/prefab: `CocktailShakerData` → ช่อง **Glass Visual Table** → ผูก asset ที่สร้าง

**ระหว่างที่ยังไม่ทำ:** shim จะแปลงตารางเดิมของแต่ละซีนเป็นตารางชั่วคราวตอนรัน พร้อม log warning
ให้ทราบ — ภาพยังถูกต้องเหมือนเดิม

---

## 5. ย้าย `CocktailShakerData` ไปใช้ 5 component ใหม่ (Phase 3)

`CocktailShakerData` ตอนนี้ **ไม่มี logic เหลือแล้ว** เป็น shim ที่เก็บข้อมูลเดิมของซีนไว้
แล้วส่งต่อให้ component จริง ถ้ายังไม่มี component เหล่านั้นมันจะ `AddComponent` ให้ตอน `Awake`
พร้อมป้อนข้อมูลจากฟิลด์เดิม — **ทุกซีนจึงทำงานได้ทันทีโดยไม่ต้องแก้อะไร**

| Component ใหม่ | รับผิดชอบ | ข้อมูลที่ต้องย้ายไป |
|---|---|---|
| `ShakerContents` | เครื่องดื่มที่อยู่ในแก้ว | — (สร้างเอง) |
| `ShakerVisualPresenter` | สี + แก้ว + `WaterSlosh` | `glassWaterSlosh`, ตารางลุคแก้ว (§4) |
| `IngredientButtonGroup` ×2 | roster ปุ่มวัตถุดิบ / หนังสือ | `ingredientButtons`, `bookUi` |
| `ShakerPanelController` | แผง Method / AddIce / Serve | `MethodUI`, `AddIceUI`, `ServeUI` จาก `CocktailShaker` |
| `ShakerTooltip` | ข้อความ tooltip | — |

**ทำ (ต่อหนึ่งซีน/prefab — มี 5 ที่):**

1. เลือก GameObject ที่มี `CocktailShakerData`
2. `Add Component` ทั้ง 5 ตัวด้วยมือ (จะได้เห็นและแก้ค่าใน Inspector ได้)
3. ผูกค่าตามตารางข้างบน
4. `IngredientButtonGroup` **2 ตัวบน GameObject เดียวกันเป็นเรื่องชั่วคราว** —
   ที่ถูกคือย้ายไปไว้บนชั้นวางวัตถุดิบและบนหนังสือแยกกัน
5. เมื่อยืนยันว่าทำงานครบแล้ว จึงลบ `CocktailShakerData` และไล่แก้ UnityEvent ที่ผูกไว้
   (`ResetShaker` 12 จุด, `TryToAdd*` 14 จุด, `SetIngredientActive` 4, `CanIngredientActive` 4,
   `ResetCocktailData` 3, `StopFill` 2) ให้ชี้ไป component ตัวใหม่

> ⚠️ ข้อ 5 คือจุดที่พังง่ายที่สุดในทั้งเอกสาร — **อย่าลบ `CocktailShakerData` จนกว่าจะย้าย
> binding ครบทุกจุด** ทำทีละซีนแล้วกดเล่นทดสอบ

---

## 6. เปิดใช้ของใหม่

### 6.1 `SO_Customer` แทน `CharacterData` (S11)

`CharacterData` ยังทำงานได้ปกติ (implement `ICustomerPreferences` แล้ว) แต่ GDD §19.1 อยากให้
ข้อมูลลูกค้าเป็น ScriptableObject หนึ่งใบต่อหนึ่งตัวละคร — แก้ได้โดยไม่ต้องเปิดซีน และอ่าน git diff รู้เรื่อง

**ทำ:**

1. `Assets > Create > Bar410 > Customer` ทีละตัว (Cole, Owen, Walter, Freya, Isla)
   ตั้ง `Id` ให้ตรง `NPC_Name` และใส่ `PreferredDrinkTypes` ตามที่อยู่ใน `CharacterData` ปัจจุบัน
2. `Assets > Create > Bar410 > Customer Roster` แล้วใส่ทั้ง 5 ใบ
3. `CocktailSystemManager` → ช่อง **Customer Roster** → ผูก roster
4. ยืนยันว่าออเดอร์ยังสุ่มถูก แล้วจึงลบ component `CharacterData` ออกจากซีน

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
| S13 | GDD §10/§21 — ผู้เล่นเลือกแก้วเอง | ยังไม่มี UI เลือกแก้ว ตอนนี้ใช้แก้วของสูตรที่ match |
| S14 | GDD §16 — `MixMethod.Build` | เลื่อนตาม D4 รอ Building minigame |
| S15 | GDD §16 — `unlockedByDefault` | เลื่อนตาม D1 ใช้ระบบ 2 asset (`CompositeDrinkRepository`) แทน |

ทั้งหมดมี `TODO(design, ...)` กำกับไว้ในโค้ดตรงจุดที่เกี่ยวข้อง

## 8. GDD ที่ต้องแก้เอกสาร

อยู่นอก repo นี้ — `~/WorkSpace/Bar410/GDD_Bar410_Master.md` (รายละเอียดในแผน §12)

- **E1** §15.1/§16 — บันทึกว่าโปรเจกต์ใช้ 3 ลิสต์แยกตามหมวด ไม่ใช่ลิสต์แบน (D6)
- **E2** §19.1 — ตัด `relationshipValue` ออกจาก `CustomerSO` (D8)

## 9. เรื่องที่ควรถาม design

**GDD §18 วัด deviation เทียบกับสูตรไหน?** อ่านตามตัวอักษร ถ้าผู้เล่นชง Negroni ได้เป๊ะทั้งที่ลูกค้า
สั่ง Martini → best-match deviation = 0 → เคส 1 → `Perfect` ซึ่งไม่น่าใช่เจตนา

โค้ดเลือกวัดเทียบ**สูตรที่ลูกค้าสั่ง** ส่วน best-match ใช้กำหนดตัวตนของเครื่องดื่มอย่างเดียว
เขียน `NOTE(design)` กำกับไว้ที่ `DrinkDeviation.MatchAgainst` แล้ว
