# Bar410 — Glass Freedom & Two-Track Building: งานที่ต้องทำมือใน Unity

**Date:** 2026-08-21 · **อัปเดตล่าสุด:** 2026-08-23 · **Branch:** `GameLoop/main`
**คู่กับ:** แผนงาน `robust-watching-lark` (ผู้เล่นเลือกแก้วเสิร์ฟเอง + แยกขั้นตอนชง/เลือกแก้ว)
**อ้างอิงเพิ่ม:** `Bar410_CocktailSystem_Manual_Setup.md` (งานมือของรอบ refactor ก่อนหน้า — ยังไม่ปิด, ส่วนแก้วในนั้นถูกแทนที่ด้วยเอกสารนี้แล้ว)
**สถานะรวม:** ✅ **ยืนยันแล้วว่าเล่นได้จริงในซีน** (`New Cocktail System.unity` — ซีนทดสอบแยกของผู้ใช้) ทั้งฝั่งวางแก้วเสิร์ฟ, หมวด **ขวด** (`BottleIngredientSource` — ทดสอบแล้ว 1 ใน 11 ขวด คือ `Alchohol-Vodka`, อีก 10 ยังไม่ได้ใส่ component) และหมวด **Fruit** (`FruitTraySlot`/`FruitPieceInstance`, `FruitTrayGroup`) **ครบทั้ง 6 ชนิดแล้ว 2026-08-23** — sprite ยังเป็น placeholder (ยืมของขวดเดิม) ไม่ใช่ art จริง — **เหลืองานหลักคือปุ่ม UI "ตกแต่งเสร็จแล้ว" ที่ยังไม่มี** (ดู §5.2) และของเก็บตกอื่นๆ ใน HANDOFF §8

🆕 **วัตถุดิบที่ต้องกดเทน้ำได้ปกติ *และ* ดึงผลไม้ออกมาได้ในตัวเดียวกัน** (ตอนนี้มีแค่
`Mixer-LemonJuice (1)`) ใช้คลาสใหม่ `DragableFruitTraySlot : DragableObject` แทนที่
`DragableObject`+`FruitTraySlot` แยกกัน — ดู HANDOFF §5 #14/#15 และ §6 D13 สำหรับเหตุผล/กับดัก

โค้ดทั้งหมดคอมไพล์ผ่าน (`refresh_unity` + `read_console` ไม่มี error, `Bar410 > Validate Cocktail Data` ผ่าน) งานที่เหลือส่วนใหญ่ตอนนี้เป็น **งานมือใน Unity Editor + งาน art/design** ไม่ใช่โค้ด (ยกเว้นปุ่ม UI §5.2 ที่ต้องผูก Inspector ใหม่)

เรียงตามลำดับที่ควรทำ: **§1 art/data ก่อน** (ไม่มีของพวกนี้ prefab สร้างไม่ได้) → **§2 prefab** → **§3 วางในซีน** → **§4 ผูก Inspector** → **§5 ปุ่ม UI** → **§6 ทดสอบทีละก้อนตามลำดับ**

---

## 0. ภาพรวมของที่เพิ่มเข้ามา

| ไฟล์ | อยู่ที่ | หน้าที่ | สถานะ |
|---|---|---|---|
| `SO_GlassOption` | `Cocktail/Glass/` | หนึ่ง asset = แก้วหนึ่งแบบที่เลือกได้ (sprite + garnish look + prefab) | ✅ ใช้งานจริงแล้ว |
| `E_GarnishLook` | `Cocktail/Glass/` | enum ลาย/สไตล์ตกแต่ง — **ตอนนี้เป็น placeholder เฉยๆ** (ดู §5.2) | 🔖 ค้าง (content) |
| `GlassShelfSlot` | `Cocktail/Glass/` | ตำแหน่งบนชั้นวาง หนึ่ง slot ต่อหนึ่ง `SO_GlassOption` | ✅ เทสแล้ว |
| `PlacedGlassInstance` | `Cocktail/Glass/` | ตัวแก้วที่ถูกลากมาวางจริง (spawn จาก `GlassShelfSlot`) | ✅ เทสแล้ว (หยิบ/วาง/สลับแก้วทำงานถูกต้อง) |
| `GlassPlacementZone` | `Cocktail/Glass/` | โซนบนโต๊ะ รับแก้วได้ 1 ใบ (แชร์ทั้งซีนผ่าน `static _occupant`) + รับการ "เท" จากภาชนะชง | ✅ เทสแล้ว |
| `PourSource` | `Cocktail/Glass/` | marker เฉยๆ ติดคู่กับ `CocktailShaker` | ✅ |
| `IngredientHoverDetector` | `Cocktail/Ingredients/` | helper ราคาศ (ไม่ใช่ zone) เช็คว่าเมาส์ชี้ทับภาชนะชงอยู่หรือไม่ — ใช้ `N_InputManager.GetObjectMouseHover()` เดิม | ✅ ใช้งานจริงแล้ว |
| `BottleIngredientSource` | `Cocktail/Ingredients/` | ติดคู่ขวดเดิม — ลากไป hover ทับภาชนะชง = สแนปไปหน้าแก้วชง ปล่อย = เท (ดีดกลับที่เดิมเสมอ) | ✅ **เทสแล้ว ใช้งานได้จริง** (รวมแก้บั๊กสั่น/self-occlusion แล้ว) |
| `FruitTraySlot` / `FruitPieceInstance` | `Cocktail/Ingredients/` | ถาดผลไม้ — ลากได้ทีละชิ้น กลไก hover เดียวกับขวด ใช้แล้วหายเสมอ, เป็น child ของถาด (ตามถาดได้ตอนย้าย) | ✅ **เทสแล้ว ใช้งานได้จริง ครบทั้ง 6 ชนิด** (2026-08-23) — sprite ยัง placeholder |
| `FruitTrayGroup` | `Cocktail/Ingredients/` | spawn/despawn piece ของทุก tray พร้อมกันตอนเข้า/ออก AddIngredient — ไม่มี piece ค้างนอกช่วงนี้ | ✅ ผูกกับ `GameFlowHooks.AddIngredient` แล้ว |
| `DragableFruitTraySlot` | `Cocktail/Ingredients/` | 🆕 สำหรับวัตถุดิบที่กดเทน้ำได้ *และ* ดึงผลไม้ได้ในตัวเดียวกัน (`: DragableObject`, ลากจะ hijack ไปที่ piece ที่เพิ่ง spawn) | ✅ เทสแล้ว บน `Mixer-LemonJuice (1)` |
| `GarnishFlowBridge` | `Hierarchical State Machine/Level 2 - Open Bar/` | คุมการเทตอน Garnish State (ใหม่) | 🔖 ยังไม่ได้ผูกในซีน/ยังไม่เทส |
| `CocktailFlowBridge` | (ของเดิม, แก้เพิ่ม) | ทำลายแก้วที่วางไว้ตอนกลับเข้า PrepareDrinks | 🔖 ยังไม่ได้ผูก `_glassZone`/ยังไม่เทส |
| `IngredientButtonGroup`/`InteractableToggle` phase methods | `Cocktail/Shaker/` (ของเดิม, แก้เพิ่ม) | `EnableInteractablePrepareBarPhase()`/`EnableInteractablePrepareDrinksPhase()` — สลับสิทธิ์ลาก bar-layout ↔ ลากเทตามเฟส HSM | ✅ ผูกแล้วใน `GameFlowHooks` (`New Cocktail System.unity`), เทสแล้ว |

---

## 1. ⚠️ Content/Art ที่ต้องมีก่อน — ไม่มีพวกนี้ prefab ทำไม่ได้

### 1.1 `GarnishLook` enum ยังเป็น placeholder

ตอนนี้ในโค้ด (`Cocktail/Glass/E_GarnishLook.cs`):

```csharp
public enum GarnishLook : byte { None, Lime, SaltRim, SugarRim, Umbrella, Olive, Twist }
```

รายการนี้เดาไว้ให้คอมไพล์ผ่านเฉยๆ **ไม่ใช่ค่าที่ยืนยันจาก design** — ต้องตัดสินใจว่าจริงๆ จะมีลายตกแต่งกี่แบบ
ชื่ออะไรบ้าง แล้วแก้ enum ตรงนี้ทีเดียว (โค้ดที่เหลือใช้ค่านี้แค่เก็บ/แสดงผล ไม่มี logic ผูกอยู่)

### 1.2 Sprite ของแต่ละแก้ว (`SO_GlassOption`)

ต้องมี sprite 3 ชุดต่อแก้วหนึ่งแบบ: `GlassSprite`, `WaterSprite`, `IceSprite` (โครงเดียวกับ
`GlassVisual` เดิมใน `SO_GlassVisualTable`) — ตัดสินใจว่าจะเปิดกี่แบบบนชั้นวาง (อย่างน้อย 1 แบบ
เพื่อทดสอบ §6 ได้)

### 1.3 Sprite ของผลไม้ (6 ชนิด)

`Mixer` ที่ต้องเป็น **ถาดผลไม้แทนขวด**: `CranberryJuice`, `LimeJuice`, `LemonJuice`,
`GrapefruitJuice`, `PepperMint`, `OrangeJuice` — ต้องมี sprite ชิ้นผลไม้ (ที่ลากออกจากถาดได้)
คนละแบบ ส่วน `Soda`/`Syrup` **ยังเป็นขวดเหมือนเดิม** ไม่ต้องทำใหม่

### 1.4 กลไกตกแต่งแก้ว (Garnish decoration)

`GarnishFlowBridge` มี `TODO(design, ...)` กำกับไว้ — ตอนนี้เทเสร็จแล้วกดปุ่ม "เสร็จ" ได้เลย
**ไม่มีขั้นตอนตกแต่งจริง** เพราะยังไม่มีคนตัดสินใจว่ากลไกควรเป็นยังไง (คลิกสลับ asset ตายตัวแบบ
GDD §21 เดิม หรือแบบอื่น) — เมื่อมี ให้เพิ่ม logic ใน `GarnishFlowBridge` ตรงคอมเมนต์นั้น

---

## 2. Prefab ที่ต้องสร้าง

### 2.1 `PlacedGlassInstance` prefab — หนึ่งอันต่อหนึ่ง `SO_GlassOption`

Component ที่ต้องมี:

| Component | หมายเหตุ |
|---|---|
| `Collider` | ให้ `DragableObject` ใช้ตรวจ overlap |
| `DragableObject` | ลากได้เหมือนขวด/แก้วอื่นในซีน |
| `SpriteRenderer` (หรือเทียบเท่า) | แสดง `GlassSprite` |
| `WaterSlosh` (แนะนำ) | ผูกใส่ช่อง **Water Slosh** ของ `PlacedGlassInstance` — ถ้าเว้นว่างไว้ แก้วจะไม่มีสีน้ำ/น้ำแข็งตอนเทเสร็จ (โค้ดเช็ค null ไว้แล้ว ไม่ error แต่จะไม่เห็นผล) |
| `PlacedGlassInstance` | สคริปต์หลัก — ไม่ต้องตั้งค่าอะไรในนี้ (`Option`/origin ถูกเซ็ตตอน spawn จาก `GlassShelfSlot`) |

แล้วเอา prefab นี้ไปใส่ในช่อง **Placed Prefab** ของ `SO_GlassOption` ที่ตรงกัน

### 2.2 `FruitPieceInstance` prefab — หนึ่งอันต่อผลไม้หนึ่งชนิด (6 อัน)

| Component | หมายเหตุ |
|---|---|
| `Collider` + `DragableObject` | เหมือนขวด |
| `SpriteRenderer` | sprite ของชิ้นผลไม้นั้น |
| `FruitPieceInstance` | ไม่ต้องตั้งอะไร — `Initialize` ถูกเรียกตอน spawn จาก `FruitTraySlot` |

> **ชิ้นนี้จะไม่แสดงผล (renderer ปิดอยู่) จนกว่าจะเริ่มลากจริงๆ** — `FruitPieceInstance` ปิด
> `Renderer` ทุกตัวใน children ตอน `Awake()` แล้วเปิดกลับตอนเริ่มลาก (ยัง**มี** collider ทำงานอยู่
> เสมอ ลากได้ตั้งแต่แรก แค่มองไม่เห็นจนกว่าจะขยับ) — เพราะงั้น **`FruitTraySlot` ต้องมี sprite/art
> ของตัวเองแสดงว่า "มีผลไม้อยู่ในถาด"** ไม่งั้นถาดจะดูว่างเปล่า

---

## 3. วางในซีน `New Drag Drop System`

### 3.1 ชั้นวางแก้ว

1. สร้าง GameObject ว่างต่อหนึ่งตำแหน่งบนชั้นวาง → เพิ่ม component **`GlassShelfSlot`**
   → ผูกช่อง **Option** เป็น `SO_GlassOption` ที่ต้องการ
2. ทำซ้ำหนึ่งตำแหน่งต่อหนึ่งแบบแก้วที่เปิดให้เลือก (ดู §1.2)

### 3.2 โซนวางแก้วบนโต๊ะ

1. หา/สร้างพื้นผิวโต๊ะที่มี `Collider` อยู่แล้ว (หรือใช้ 1 ใน 3 กล่อง `SurfacePlacementZone` ที่มีอยู่ในซีน)
2. เพิ่มคอมโพเนนต์ **`GlassPlacementZone`** เข้าไปแทน/เพิ่มจาก `SurfacePlacementZone` เดิม (มันสืบทอดมาจาก `SurfacePlacementZone` อยู่แล้ว)
3. ตั้ง layer ของ collider นี้ให้ตรงกับ `N_InputManager._zoneLayer` (เหมือนโซนอื่นในซีน)
4. **นี่คือ zone เดียวกันที่ต้องผูกเข้าทั้ง `CocktailFlowBridge._glassZone` และ `GarnishFlowBridge._glassZone`** (ดู §4)

### 3.3 ถาดผลไม้ (6 จุด)

ต่อผลไม้แต่ละชนิด: สร้าง GameObject ที่ตำแหน่งถาด (**ใส่ sprite ของตัวเองด้วย** เพราะชิ้นที่ spawn
ออกมาจะมองไม่เห็นจนกว่าจะลาก — ดู §2.2) → เพิ่ม **`Collider`** + **`FruitTraySlot`** (ตอนนี้
`[RequireComponent(typeof(DragableObject))]` แล้ว — เพิ่ม `FruitTraySlot` ปุ๊บ Unity จะเพิ่ม
`DragableObject` ให้เองอัตโนมัติ) → ผูก **Fruit Type** (`Mixer` enum ค่าที่ตรงกัน) และ
**Piece Prefab** (จาก §2.2)

> พิจารณาว่าจะ**เอา `IngredientButtonUI`/`DragableObject` เดิมของ 6 ขวดนี้ออกจากชั้นวางขวด**
> เพราะตอนนี้กลายเป็นถาดแทนขวดแล้ว ไม่ใช่ขวดอีกต่อไป

**ทำไมตัว Tray เองก็เป็น `DragableObject`:** เพื่อให้ย้ายทั้งถาดได้ตอนจัดร้าน (Prepare) เหมือนของ
วางบาร์อื่นๆ — ชิ้นผลไม้ที่ spawn ออกมาตอนนี้เป็น **child ของ `FruitTraySlot`** แล้ว (ผูกไว้ใน
`Instantiate(..., transform)`) จะตามตัวถาดไปด้วยเวลาลากย้ายที่

**ผูกเปิด/ปิดการลากทั้งถาดตามช่วง (ใช้ Inspector binding แบบเดียวกับที่มีอยู่แล้ว ไม่ต้องเขียนโค้ดใหม่):**
ในซีนนี้ `[GameLoop] > GameFlow Hooks` ช่อง **Level 3 · `_addIngredient`** ผูก `OnEnter` →
`IngredientButtonGroup.Enable()` และ `OnExit` → `.Disable()` บน `_ingredients` อยู่แล้ว (คู่กับที่
`CocktailFlowBridge` ทำเรื่องเดียวกันซ้ำในโค้ด — ไม่ชนกัน) ให้ทำแบบเดียวกันกับถาดผลไม้ (และจะทำกับ
ขวดที่แปลงเป็น drag ได้ก็ได้เหมือนกัน) โดยผูกที่ **`_prepareBar`** (Level 1) แทน:
- **`_prepareBar.OnEnter`** → ลาก `FruitTraySlot`'s `DragableObject` ของแต่ละถาดเข้าไปในช่อง
  target → เลือก property **Interactable** (bool) → ตั้งเป็น **true**
- **`_prepareBar.OnExit`** → target เดียวกัน → **Interactable** → **false**

ผลคือ: ลากย้ายทั้งถาดได้เฉพาะตอน Prepare เท่านั้น พอเข้า Open Bar (ทุก step รวม PrepareDrinks)
ตัวถาดจะลากไม่ได้อีก — ส่วนการดึงชิ้นผลไม้ออกมาใส่แก้วชง (`FruitPieceInstance`) เป็นคนละ
`DragableObject` คนละ Interactable กัน ไม่ถูกล็อกไปด้วย ยังทำงานปกติทุก phase ที่ Track B เปิดอยู่

> ⚠️ **จุดที่ต้องระวังตอนเทส:** collider ของ `FruitTraySlot` (ทั้งถาด) กับ collider ของ
> `FruitPieceInstance` (child) อยู่ตำแหน่งใกล้/ทับกัน — ราคาศจะชนอันไหนก่อนขึ้นกับระยะ/ขนาด
> collider จริงในซีน ถ้าเทสแล้วพบว่าตอน PrepareDrinks ลากชิ้นผลไม้ไม่ออก (เหมือนกำลังลากทั้งถาด
> แทน) ให้ปรับขนาด/ตำแหน่ง collider ของ `FruitTraySlot` ให้เล็กกว่า/ไม่ทับกับจุดที่ชิ้นผลไม้อยู่พอดี

### 3.4 ภาชนะชง — ไม่ต้องเพิ่ม zone อะไรแล้ว

**เปลี่ยนจากแผนเดิม:** ตอนแรกออกแบบให้ภาชนะชงมี `IngredientDropTarget` (zone) แต่ระบบ
`PlacementZoneBase` จะ re-clamp ตำแหน่งของที่ลากอยู่ทุกเฟรมเมื่อ raycast ไปโดนโซน ทำให้ขวด
"บัคขึ้นๆ ลงๆ" ตอนลากเข้าใกล้ภาชนะชง — เปลี่ยนมาใช้การ raycast ตรงๆ ผ่าน
`N_InputManager.GetObjectMouseHover()` แทน (เหมือนกับที่ระบบใช้หาว่าเมาส์กำลังจะหยิบอะไรอยู่แล้ว)
ไม่ผ่านระบบ placement/clamp เลย **ไม่ต้องสร้าง/เพิ่มคอมโพเนนต์อะไรบนภาชนะชงสำหรับ Track B**
(สิ่งเดียวที่ต้องมีคือ `Collider` ของภาชนะชงต้องอยู่บน layer เดียวกับ `N_InputManager._draggableLayer`
— ซึ่งควรอยู่แล้ว เพราะภาชนะชงเป็น `DragableObject` ของมันเองอยู่แล้ว)

> **แก้บั๊ก "สั่นเหมือนวางไม่ได้":** ตอน hover ทับภาชนะชง ของที่ลากอยู่จะถูกสแนปไปที่
> `_hoverOffset` — ถ้าไม่กันไว้ collider ของตัวมันเองจะไปทับ ray เดียวกับที่ใช้เช็ค hover ทำให้ผลลัพธ์
> สลับ hover/ไม่ hover ทุกเฟรม (เห็นเป็นอาการสั่น) `BottleIngredientSource`/`FruitPieceInstance`
> แก้แล้วโดยสลับ `gameObject.layer` ของตัวเองไปเป็น **`Ignore Raycast`** ชั่วคราวระหว่างลาก แล้ว
> คืนค่าเดิมตอนปล่อย — **อย่าตั้งใจใช้ layer `Ignore Raycast` กับ gameplay logic อื่นในซีนนี้** เพราะ
> ขวด/ผลไม้จะไปอยู่ชั่วคราวตรงนั้นระหว่างลากทุกครั้ง

### 3.5 แปลงขวดเดิม (11 ขวดที่เหลือ) ให้ลากเทได้

ขวดที่ **ยังเป็นขวด** ตาม design: `Vodka`, `Gin`, `Whiskey`, `Rum`, `Tequila`, `Triplesec`,
`DryVermouth`, `SweetVermouth`, `Campari`, `Soda`, `Syrup`

ต่อขวดแต่ละอัน (มี `DragableObject` + `IngredientButtonUI` อยู่แล้ว): เพิ่มคอมโพเนนต์
**`BottleIngredientSource`** เข้าไป → ปรับช่อง **Hover Offset** (ค่าเริ่มต้น `(0, 0.3, -0.2)` เป็น
local space เทียบกับ transform ของภาชนะชง) ให้ขวดไปสแนปอยู่ "หน้าแก้วชง" ในตำแหน่งที่ดูดี —
ปรับได้อิสระต่อขวด ไม่ต้องเหมือนกันทุกอัน

### 3.6 `PourSource` บนภาชนะชง

เลือก GameObject `CocktailShaker` เดิม (ตัวเดียวกับ §3.4) → เพิ่มคอมโพเนนต์ **`PourSource`**
(ไม่ต้องตั้งค่าอะไร — เป็น marker เฉยๆ ใช้ตอนภาชนะชง *ถูกลาก* ไปเทลงแก้วใน Garnish
— คนละเรื่องกับ §3.4/§3.5 ที่ภาชนะชง *เป็นเป้าหมาย* ให้ขวด/ผลไม้ลากเข้ามาใส่ใน Add Ingredient)

---

## 4. ผูก Inspector

### 4.1 `CocktailFlowBridge` (ของเดิมบน `[GameLoop]`)

ช่องใหม่ที่เพิ่มเข้ามา:

| ช่อง | ผูกกับ |
|---|---|
| **Glass Zone** | `GlassPlacementZone` จาก §3.2 |

**ผลที่ได้:** ทุกครั้งที่เข้า/กลับเข้า `PrepareDrinks` (รวม backtrack จาก Garnish) แก้วที่วางไว้จะถูก
ทำลายทิ้ง — ลูกค้าคนถัดไปต้องลากแก้วใหม่เสมอ

### 4.2 `GarnishFlowBridge` (component ใหม่ — เพิ่มบน `[GameLoop]`)

| ช่อง | ผูกกับ |
|---|---|
| Game Loop | `GameLoopFSM` บน object เดียวกัน |
| Commands | `GameFlowCommands` บน object เดียวกัน |
| Shaker Contents | `ShakerContents` (ตัวเดียวกับที่ `CocktailFlowBridge` ใช้) |
| Shaker Dragable | `DragableObject` บน `CocktailShaker` (ตัวเดียวกับที่เพิ่ง Add `PourSource` ใน §3.5) |
| Glass Zone | `GlassPlacementZone` เดียวกับ §3.2/§4.1 |

**ผลที่ได้:** ตอนเข้า Garnish State ภาชนะชงจะถูกปลดล็อกให้ลากได้ (ปกติจะถูกล็อกมาจาก
`MinigameFlowBridge` ตอนจบ minigame) — ลากไปทับแก้วที่วางไว้ = เท สีน้ำ/sprite จะย้ายไปแสดงที่แก้ว
และภาชนะชงจะเด้งกลับตำแหน่งเดิมเอง

---

## 5. ปุ่ม UI ที่ต้องผูกใหม่

### 5.1 ปุ่ม "ใส่วัตถุดิบเสร็จแล้ว" (จบ step 2.1 → เข้ามินิเกม)

ยังไม่มีปุ่มไหนในซีนเรียก `GameFlowCommands.IngredientAdded()` เลย — ต้องสร้าง UI Button ใหม่
(หรือ reuse ปุ่มที่ตั้งใจไว้) แล้วผูก **OnClick → `GameFlowCommands.IngredientAdded()`**
(ไม่ต้องมี argument — เว้น `minigameType` ว่างไว้ให้ `ShakerContents.RequiredMinigame` เดาจาก
`PreparationMethod` แทน)

ผู้เล่นกดปุ่มนี้ได้ทุกเมื่อ **ไม่บังคับให้ครบ 10 ส่วนก่อน** (ตามที่ยืนยันไว้)

### 5.2 ปุ่ม "ตกแต่งเสร็จแล้ว" (จบ Garnish → เข้า Serve)

**ห้ามผูกปุ่มนี้เข้ากับ `GameFlowCommands.GarnishDone()` ตรงๆ** — ให้ผูกเข้ากับ
**`GarnishFlowBridge.TryFinishGarnish()`** แทน (component เดียวกับ §4.2) เพราะเมธอดนี้เช็คก่อนว่า
เทแล้วหรือยัง ถ้ายังไม่เท จะ log warning เฉยๆ ไม่ปล่อยให้ปิดงานลูกค้าทั้งที่ยังไม่ได้เท

---

## 6. ทดสอบทีละก้อนตามลำดับ (อย่าผูกทุกอย่างพร้อมกันแล้วเทสทีเดียว)

1. **§1 art + §2 prefab** อย่างน้อย 1 แบบแก้ว + 1 ชนิดผลไม้ ก่อน
2. **วางแค่ชั้นวางแก้ว + โซนวางแก้ว** (§3.1–3.2) ยังไม่ต้องผูก bridge — ลองลากจากชั้นวางไปวางบนโต๊ะ
   ด้วยมือ เช็คว่า: ลากไปวาง → ชั้นวาง spawn ตัวใหม่แทนที่ → ลากแก้วอีกใบ (คนละแบบ) มาวางทับ →
   **ใบเดิมถูกทำลายทิ้งอัตโนมัติ ใบใหม่เข้าแทนที่** (ไม่ใช่เด้งกลับ — ลากมาใบใหม่ = สลับ) → โซนมีแก้ว
   แค่ 1 ใบเสมอ
3. **ถาดผลไม้ + ขวดที่แปลงแล้ว** (§3.3, §3.5) ทีละ 1-2 ชิ้นก่อน เช็คว่า: คลิกยังทำงานเหมือนเดิม,
   ลากขวด/ผลไม้เข้าใกล้ภาชนะชง → **สแนปไปหน้าแก้วชงนิ่งๆ ไม่กระตุก** (ถ้ายังกระตุกอยู่ แปลว่ายังมี
   `SurfacePlacementZone`/zone อื่นทับ collider ของภาชนะชงอยู่ ให้ตรวจดู), ปล่อยตอน hover = เทเข้า
   (ขวดคืนที่เดิมเสมอ, ผลไม้หายเสมอ), ลากผลไม้ไปปล่อยที่อื่นไม่ผ่านภาชนะชง = หายเฉยๆ ไม่เข้า,
   เพดาน 10 หน่วยยังกันอยู่
4. **ผูกปุ่ม §5.1** เช็คว่า 2.1 → 2.2 (มินิเกม) ทำงาน
5. **ผูก bridge เต็ม §4 + `PourSource` §3.6 + ปุ่ม §5.2** เทสวงจรเต็ม: สั่งเครื่องดื่ม → ใส่วัตถุดิบ
   (ผสมคลิก/ลากขวด/ลากผลไม้) → กดจบ → มินิเกม → เข้า Garnish → วางแก้ว (ถ้ายังไม่วางตอน 2.1) →
   ลากภาชนะชงไปเท → กด "ตกแต่งเสร็จแล้ว" → Serve ให้คะแนนถูกต้อง
6. รัน `mcp__unityMCP__refresh_unity` + `read_console` ทุกครั้งหลังเพิ่ม component ใหม่ เพื่อจับ
   Missing Reference ก่อนเทสจริง
