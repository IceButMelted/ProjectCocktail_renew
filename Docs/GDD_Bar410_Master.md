# GDD : Bar410 — Master Design Document
# เอกสารออกแบบเกมกลาง (สำหรับ Designer และ Programmer)

> รวมเนื้อหาจาก `GDD _ Bar410.md` (ต้นฉบับ) + ผลสรุปจากเซสชัน grill ออกแบบระบบ (19-20 ส.ค. 2026)
> Combines the original `GDD _ Bar410.md` with the finalized mechanics decisions from the design-grill session.
>
> **Engine / เครื่องมือ:** Unity 6 · Yarn Spinner Plus (dialogue) · Text Animator by Febucci (text FX) · Cinemachine (camera)
> **Code principle / หลักการเขียนโค้ด:** SOLID

---

## 1. จุดประสงค์ของโปรเจกต์ / Project Purpose

โปรเจกต์นี้ทำขึ้นเพื่อพัฒนาเกมแนว **Visual Novel** โดยมีจุดเด่นคือการชงเครื่องดื่มให้กับลูกค้าที่มาที่ร้าน โดยมีตัวดำเนินเรื่องคือผู้เล่นและระดับความพึงพอใจของลูกค้าที่ได้รับเครื่องดื่ม

This project is a **Visual Novel** built around bartending — mixing drinks for customers. The narrative driver is the player and the satisfaction level of the customer receiving each drink.

## 2. เรื่องย่อของเกม / Story Summary

เกม Bar410 เป็นเรื่องราวของ NPC ต่างๆ ในเมืองแถบชนบทของยุโรป ผู้เล่นรับบทเป็นบาร์เทนเดอร์หน้าใหม่ในเมือง เปิดร้านรับฟังเรื่องราวของ NPC ทั้งตัวหลักและตัวประกอบ เพื่อเข้าใจเรื่องราวของเมือง ปัญหาของแต่ละคน และช่วยแก้ปัญหาโดยมีตัวกลางเป็นเครื่องดื่มที่ผู้เล่นชงออกมา

Bar410 follows the NPCs of a small rural European town. The player is a newcomer bartender who opens a bar, listens to main and side characters, and helps resolve their problems — using the drinks they mix as the medium of connection.

## 3. Art Style

**2.5D**: ตัวละคร (Character), เครื่องดื่ม (Drinks), และวัตถุดิบ เป็นภาพ 2D วาดมือ ส่วน Environment เป็น 3D
Characters, drinks, and ingredients are hand-drawn 2D; the environment is 3D.

## 4. Tools

- **Unity Engine 6** — เกมหลักและระบบต่างๆ / core engine & systems
- **Yarnspinner Plus for Unity 6** — ระบบบทสนทนา (dialogue system) ที่ผู้เล่นโต้ตอบกับ NPC
- **Text Animator for Unity (Febucci)** — UI Toolkit + TextMesh Pro, ทำ Text Animation คู่กับ Yarnspinner
- **Unity Cinemachine** — มุมกล้องต่างๆ / camera control

## 5. Game Loop

```
ขึ้นวันใหม่ >> สนทนากับ NPC ที่เข้ามา >> ทำเครื่องดื่มตามที่ NPC สั่ง >> ดู Reaction >> สนทนากับ NPC >> จบวัน

New Day >> Talk with incoming NPC >> Mix the drink they ordered >> Watch Reaction >> Talk with NPC >> End Day
```

## 6. Phase Structure / โครงสร้าง Phase

- **Phase 1 — ช่วงก่อนเปิดร้าน (Pre-open Prep):** ผู้เล่นจัดวางร้าน (วัตถุดิบ, ของตกแต่ง ฯลฯ) ด้วยระบบ Drag-Drop
  Player arranges the shop (ingredients, décor, etc.) using the drag-drop placement system.
- **Phase 2 — ช่วงเปิดร้าน (Open Hours):** วนลูปสนทนา → สั่งเครื่องดื่ม → ชง → เสิร์ฟ → ดู reaction ตาม Game Loop ด้านบน
  Runs the dialogue → order → mix → serve → reaction loop described in §5.

---

# ส่วนที่ 1: กลไกเกม (สำหรับ Designer เป็นหลัก)
# Part 1: Game Mechanics (Designer-facing overview)

## 7. Customer Data / ข้อมูลลูกค้า

แต่ละลูกค้ามีข้อมูลดังนี้ / Each customer has:

| Field | คำอธิบาย (TH) | Description (EN) |
|---|---|---|
| `customerName` | ชื่อลูกค้า | Customer's name |
| `preferredDrinkTypes` | List ของประเภทเครื่องดื่มที่ชอบ (`DrinkType`) — ไม่ใช่รายชื่อเมนู | List of preferred `DrinkType` values — not specific recipe names |
| `relationshipValue` | ค่าความสัมพันธ์กับผู้เล่น ช่วง 0–10 | Relationship value with the player, range 0–10 |

## 8. ระบบ Drag-Drop Placement / Drag-Drop Placement System

ใช้สำหรับการเคลื่อนย้ายวัตถุดิบและของต่างๆ ในฉาก ทั้งการหยิบจับ การเสิร์ฟ และการชงเครื่องดื่ม
Used to move ingredients and objects in the scene — picking up, serving, and mixing all use this system.

### 8.1 Juicy Feedback Components / คุณสมบัติเสริมความรู้สึก

Component แบบ reusable แนบกับ object ที่ลากได้ทุกตัว (composition over inheritance ตาม SOLID/SRP):
A shared, reusable component attached to every draggable object (composition over inheritance, SRP):

- เล่นเสียงเมื่อ Hover / Play sound on hover
- เปลี่ยนขนาดเมื่อ Hover / Scale change on hover
- เล่นเสียงเมื่อปล่อยลง (Drop) / Play sound on drop
- เล่นเสียงเมื่อคลิก / Play sound on click
- เล่นเสียงระหว่างลาก / Play sound while dragging

> **Programmer note:** แนะนำ interface `IHoverFeedback`, `IDragFeedback`, `IDropFeedback` หรือ component เดียว `DragDropFeedbackComponent` (configurable AudioClip/animation curve) แนบคู่กับ `IDraggable` หลัก — แยก feedback ออกจาก drag logic ตาม SRP

## 9. สูตรเครื่องดื่ม / Drink Recipes (Concept)

สูตรเครื่องดื่มแต่ละสูตรประกอบด้วย:
Every recipe consists of:

- **วัตถุดิบ (Ingredients)** — แบ่งเป็น 2 กลุ่ม: **Alcohol** (Base Spirit, Liqueur) และ **Mixer**
  Split into two groups: **Alcohol** (Base Spirit, Liqueur) and **Mixer**
- **วิธีการชง (Mix Method)** — เป็น Minigame (ดู §10)
  A minigame (see §10)
- **น้ำแข็ง** — ใส่ / ไม่ใส่
  Ice — yes/no
- **คำอธิบายรสชาติ** และ **ชื่อเครื่องดื่ม**
  Flavor description and drink name
- **ประเภทเครื่องดื่ม (DrinkType):** `Non_Alcohol`, `Low_Alcohol`, `High_Alcohol` (เกณฑ์แม่นยำ ดู §14.2)
- **สีเครื่องดื่มเมื่อเสร็จ:** `Top Color`, `Bottom Color`
- สูตรทำผ่าน **Unity ScriptableObject** — ตั้งราคา, ประเภท, ส่วนผสมได้ทั้งหมด (โครงสร้างเต็ม ดู §14)
  Authored as a Unity ScriptableObject — price, type, and ingredients all configurable (full schema in §14).

## 10. ระบบการชงเครื่องดื่ม / Mixing System

- วัตถุดิบรวมทั้งหมด **10 ส่วน** ต่อเครื่องดื่ม 1 แก้ว (นิยามแม่นยำ ดู §13)
  Total of **10 units** of ingredients per glass (precise definition in §13)
- ลากวัตถุดิบลงแก้ว หรือกดที่วัตถุดิบให้เกิด Animation เทลงแก้ว
  Drag ingredients into the glass, or click to trigger a pour animation
- ใส่น้ำแข็งด้วยการลากน้ำแข็งจากจุดหนึ่งไปยังแก้ว
  Ice is added by dragging it from its source to the glass
- ชงด้วย Minigame ตามวิธีการชงที่กำหนด (Shake / Stir / Build)
  Mixed via a minigame matching the recipe's method (Shake / Stir / Build)
- เลือกแก้วที่จะใส่เครื่องดื่ม (Cosmetic เท่านั้น — ดู §17)
  Select a glass to pour into (cosmetic only — see §17)
- ตกแต่งแก้วก่อนเสิร์ฟ (ดู §16)
  Decorate the glass before serving (see §16)

## 11. การเสิร์ฟเครื่องดื่ม / Serving

ลากแก้วที่ชงเสร็จแล้วไปที่ลูกค้าเพื่อเสิร์ฟ
Drag the finished glass to the customer to serve it.

## 12. ระบบสั่งเครื่องดื่มที่ผู้เล่นต้องทำ / Order Generation (Concept)

ลูกค้าจะสั่งเครื่องดื่มหลังบทสนทนาจบ โดยนักเขียนเลือกรูปแบบการสั่งเองใน Yarn ผ่าน YarnCommand เฉพาะของแต่ละแบบ (ไม่มีการสุ่มเลือก "แบบการสั่ง" อัตโนมัติ) มี 5 แบบ — รายละเอียด/ signature ของแต่ละ Command ดู §19:
Customers order after dialogue ends. Writers pick the order mode explicitly per Yarn node via a dedicated YarnCommand (no automatic mode randomization). Five modes — full command signatures in §19:

1. สั่งแบบตายตัว ระบุชื่อเมนู / Fixed order by drink name
2. สั่งแบบตายตัว ระบุคำอธิบายรสชาติ / Fixed order by flavor description
3. สุ่มผ่านความชอบประเภทเครื่องดื่มของตัวละคร ออกมาเป็นชื่อเมนู / Random via preferred type, shown as a name
4. สุ่มผ่านความชอบของตัวละคร ออกมาเป็นคำอธิบายรสชาติ / Random via preferred type, shown as a flavor description
5. สั่งแบบตายตัวแต่ระบุแค่ "ประเภทเครื่องดื่ม" — เสิร์ฟอะไรก็ได้ในประเภทนั้นภายใต้เงื่อนไข §15
   Fixed order by DrinkType only — any recipe of that type satisfies §15's conditions

## 13. ระบบมุมกล้อง / Camera System (Concept)

แบ่งเป็น 2 Phase / Split into two phases:

- **Phase สนทนา (Dialogue):** กล้อง Fixed องศา/ตำแหน่ง เปลี่ยนได้ 6 ตำแหน่ง — Zoom เข้าลูกค้าคนที่ 1/2/3, Zoom ระหว่างคนที่ 1-2, Zoom ระหว่างคนที่ 2-3, Zoom ออกเห็นทั้ง 3 คน
  Fixed angle/position, 6 preset shots: zoom on customer 1/2/3, zoom between 1-2, zoom between 2-3, wide shot of all three.
- **Phase ชงเครื่องดื่ม (Mixing):** กล้อง Fixed ตำแหน่งแต่หันซ้าย-ขวาได้ในองศาจำกัด เลื่อนลง/ก้มมองเล็กน้อยได้ อิงตำแหน่งเมาส์ผ่าน FSM (State ละเอียด ดู §20)
  Fixed position but can pan within a limited angle and tilt down slightly; mouse-position-driven FSM (full states in §20).
- สลับกล้องด้วย **Cinemachine**; เปลี่ยน Phase ควบคุมด้วย **YarnCommand**
  Camera switching via Cinemachine; phase transitions controlled by YarnCommand.

## 14. Dialogue System (Concept)

- Yarn Spinner เขียนและแสดงบทสนทนา; Text Animator (Febucci) ทำ Text Animation
  Yarn Spinner authors/displays dialogue; Text Animator (Febucci) drives text animation.
- InlineEvent ใน Yarn หรือ event ของ Text Animator ใช้เปลี่ยนท่าทาง/สีหน้าตัวละครระหว่างบทสนทนา
  Inline Yarn events or Text Animator events change character pose/expression mid-dialogue.
- มีระบบเปิด/ปิด AutoPlay และ Instant Dialogue
  Toggleable AutoPlay and Instant Dialogue.
- Branch บทสนทนาแตกผ่านค่าความสัมพันธ์ (`$rel_<id>`) — รายละเอียด ดู §18
  Dialogue branches via the relationship variable (`$rel_<id>`) — see §18.
- เมื่อ Pause เกม บทสนทนาจะไม่ Run ต่อ
  Dialogue does not advance while the game is paused.
- **Dialogue Box:** เป็นแบบ Bubble ลอยในฉาก ปลายกล่องชี้ไปทาง customer ที่พูด ปรับความกว้าง/สูงตามข้อความ พร้อม Animation ก่อนข้อความขึ้น
  Floating speech-bubble style, tail points at the speaking customer, auto-resizes to text with a size-change animation before text appears.

---

# ส่วนที่ 2: สเปกทางเทคนิค (สำหรับ Programmer เป็นหลัก)
# Part 2: Technical Spec (Programmer-facing detail)

> ทุกกฎ/ตัวเลขในส่วนนี้ผ่านการยืนยันกับทีมออกแบบแล้วในเซสชัน grill — ใช้เป็น source of truth เดียวสำหรับ implement
> Every rule/number below was confirmed with design in the grill session — treat this as the single source of truth for implementation.

## 15. โมเดลหน่วยวัตถุดิบและการชง / Ingredient & Mixing Unit Model

**กฎ / Rule:** ทุกสูตรมีวัตถุดิบรวมกัน **= 10 หน่วย (int) เสมอ** — Every recipe's ingredients sum to exactly **10 integer units**.

```
ตัวอย่าง / Example: Gin 5, Vodka 4, Syrup 1 → รวม/total = 10
```

UI การชงอาจมีวัตถุดิบให้เลือกมากกว่า 10 ชนิด แต่ปริมาณที่เทลงแก้วจริงต้องรวมกันเท่ากับ 10 หน่วยเสมอ
The mixing UI may offer more than 10 ingredient choices, but the poured total per glass must always equal 10.

### 15.1 ชนิดวัตถุดิบ / Ingredient Type

แบ่งเป็น **enum แยกต่อหมวด** ไม่ใช่ enum แบนตัวเดียว — เพื่อให้ปุ่ม Mixer ถูกตั้งเป็นเหล้าไม่ได้
และ dropdown ใน Inspector ยังสั้นอยู่เมื่อวัตถุดิบเพิ่มขึ้น
Split into one enum per category rather than a single flat enum, so a Mixer slot cannot be
assigned a spirit and each Inspector dropdown stays short as the roster grows.

```csharp
public enum BaseSpirit { None = 0, Vodka, Gin, Whiskey, Rum, Tequila }
public enum Liqueur    { None = 0, Triplesec, DryVermouth, SweetVermouth, Campari }
public enum Mixer      { None = 0, Soda, CranberryJuice, LimeJuice, LemonJuice,
                         GrapefruitJuice, Syrup, PepperMint, OrangeJuice }
```

ไม่ต้องมีตาราง `IngredientType → IngredientCategory` เพราะหมวดคือชนิดของ enum อยู่แล้ว
No category lookup table is needed — the enum type *is* the category.

> ⚠️ **ต้นทุนของทางเลือกนี้:** การเพิ่ม *ชนิด* ในหมวดเดิมถูกมาก (เพิ่มค่า enum ค่าเดียว) แต่การเพิ่ม
> **หมวดใหม่** (เช่น Bitters, Garnish) ต้องเพิ่ม enum + struct + ลิสต์ + แก้จุดรวมผล 4 จุดในโค้ด
> ซึ่งมีคอมเมนต์ `เพิ่มหมวดใหม่: แก้ที่นี่` กำกับไว้ให้แล้ว — ปรึกษาโปรแกรมเมอร์ก่อนเพิ่มหมวด
> Adding a *type* to an existing category is trivial; adding a whole new **category** touches
> four aggregation points in code, all marked with that comment. Check with a programmer first.

### 15.2 เกณฑ์แอลกอฮอล์ → ประเภทเครื่องดื่ม / Alcohol → DrinkType Threshold

```
alcoholUnits = Σ quantity ที่ category == BaseSpirit หรือ Liqueur (ไม่นับ Mixer)
             = Σ quantity where category == BaseSpirit OR Liqueur (Mixer excluded)

alcoholUnits == 0        → Non_Alcohol
1 <= alcoholUnits <= 5    → Low_Alcohol   (5 หน่วยพอดี = Low / 5 is inclusive)
alcoholUnits >= 6         → High_Alcohol
```

ใช้ทั้งจัดประเภทสูตรที่รู้จัก และคำนวณ DrinkType ของเครื่องดื่มกรณี Fail(b) (ดู §17.3)
Used both to classify known recipes and to compute DrinkType for a Fail(b) drink (§17.3).

## 16. โมเดลข้อมูลสูตรเครื่องดื่ม / Drink Recipe Data Model (ScriptableObject)

```csharp
public enum DrinkType { Non_Alcohol, Low_Alcohol, High_Alcohol }
public enum MixMethod { Shake, Stir, Build }

[CreateAssetMenu(menuName = "Bar410/Recipe")]
public class DrinkRecipeSO : ScriptableObject
{
    public string   drinkName;
    public DrinkType drinkType;                 // คำนวณตอน edit-time จากวัตถุดิบ / derived at edit-time

    // สามลิสต์แยกตามหมวด (§15.1) — รวมทั้งสามลิสต์ต้องได้ 10
    // Three lists, one per category (§15.1) — the three together must sum to 10
    public List<AlcoholIngredient> alcoholList;
    public List<LiqueurIngredient> liqueurList;
    public List<MixerIngredient>   mixerList;
    public MixMethod mixMethod;
    public bool useIce;
    public Color topColor;
    public Color bottomColor;
    public string flavorDescription;
    public float price;
    public GlassType glassType;                  // cosmetic เท่านั้น / cosmetic only, see §21
    public bool unlockedByDefault;
}

// struct หนึ่งตัวต่อหนึ่งหมวด รูปร่างเหมือนกันหมด
// One struct per category, all the same shape
[System.Serializable] public struct AlcoholIngredient { public BaseSpirit Type; public int Amount; }
[System.Serializable] public struct LiqueurIngredient { public Liqueur    Type; public int Amount; }
[System.Serializable] public struct MixerIngredient   { public Mixer      Type; public int Amount; }
```

**ตรวจสอบตอน edit-time / Editor validation:**
- `Σ (alcohol + liqueur + mixer).Amount == 10` (block save/build ถ้าไม่ตรง)
  ตรวจได้จากเมนู `Bar410 > Validate Cocktail Data` / check with that menu item
- ห้ามมี `DrinkRecipeSO` สองสูตรที่วัตถุดิบชุดเดียวกันเป๊ะ (ตรวจตอน edit-time — ดู §17.2)
  No two `DrinkRecipeSO` assets may share an identical ingredient multiset (design-time check — §17.2)

## 17. อัลกอริทึมการตรวจสอบเครื่องดื่ม / Recipe Matching Algorithm

### 17.1 สูตรคำนวณความผิดเพี้ยน / Deviation Formula

```
deviation(poured, recipe) = Σ |recipe[i] - poured[i]|
```
รวมทุกชนิดวัตถุดิบที่ปรากฏในฝั่งใดฝั่งหนึ่ง (ไม่มี = 0) — **ไม่หาร 2**
Over the union of ingredient types present on either side (missing = 0) — **no division by 2**.

```
ตัวอย่าง / Example: สูตร/recipe Gin7 Vodka3 | ใส่จริง/poured Gin7 Vodka2 Syrup1
deviation = |7-7| + |3-2| + |0-1| = 0 + 1 + 1 = 2
```

คำนวณกับทุกสูตรใน database แล้วเลือกสูตร deviation ต่ำสุดเป็น **best-match**
Compute against every recipe in the database; lowest deviation = **best-match**.

### 17.2 Tie-break (deviation เท่ากันหลายสูตร)

- Exact match (`deviation == 0`) ซ้ำกัน: **ห้ามเกิดขึ้น** (ตรวจตอน edit-time, §16)
- Tie ที่ deviation 1–3 ตรงกับหลายสูตร: ใช้ **ลำดับ index ใน List/array** (ตัวแรกชนะ) สำหรับ v1
  🔖 **Future (marked, not built):** ให้ผู้เล่นเลือกเองผ่าน UI แทน auto-resolve

### 17.3 การกำหนด Flag / Recipe Determination Flags

```
methodMatch = (poured.mixMethod == recipe.mixMethod)   // boolean เป๊ะ ไม่มี partial credit
iceMatch    = (poured.useIce == recipe.useIce)          // boolean เป๊ะ ไม่มี partial credit

if deviation == 0 and methodMatch and iceMatch:
    Flag = "Perfect"      → ชื่อ/ประเภท จากสูตรที่ match
elif deviation == 0:                                     # method หรือ ice ไม่ตรง
    Flag = "Seem_Like"    → ชื่อ/ประเภท จากสูตรที่ match
elif 0 < deviation <= 3 and มี bestMatch:
    Flag = "Seem_Like"    → ชื่อ/ประเภท จาก bestMatch
elif deviation > 3 หรือ ไม่มีสูตรใดที่ deviation<=3:
    Flag = "Fail"
    drinkName = สุ่ม() / Random()
    drinkType = คำนวณจาก AlcoholUnits(poured)   # ดู §15.2
```

## 18. ระบบเช็คความพึงพอใจ / Satisfaction & Reaction System

ลำดับความสำคัญ — เช็คบนลงล่าง เจอก่อนใช้ก่อน / Priority order, top to bottom, first match wins:

```
1. deviation == 0  AND methodMatch AND iceMatch
      → Perfect

2. deviation == 0  AND (NOT methodMatch OR NOT iceMatch)
      → Acceptable

3. 0 < deviation <= 3  AND servedDrinkType == orderedDrinkType
      → Acceptable

4. 0 < deviation <= 3  AND servedDrinkType != orderedDrinkType
      → Fail   (Fail "a" — ยังจับคู่สูตรใกล้เคียงได้ / still matched a nearby recipe)

5. deviation > 3
      → Fail   (Fail "b" — ไม่ตรงสูตรใดเลย สุ่มชื่อ / fully unmatched, random name)
```

หมายเหตุ: ผลนี้คือ **ความพึงพอใจ** ต่อยอดจาก Flag ใน §17.3 — เครื่องดื่มที่ Flag เป็น `Seem_Like` ได้ผลลัพธ์เป็นได้ทั้ง `Acceptable` (เคส 3) หรือ `Fail` (เคส 4) ขึ้นกับประเภทตรงกับที่สั่งหรือไม่
Note: this is the **satisfaction** outcome, built on top of the §17.3 Flag — a `Seem_Like` drink can resolve to `Acceptable` (case 3) or `Fail` (case 4) depending on type match.

### 18.1 ระบบราคา / Pricing

```
Perfect              → ราคา/price = recipe.price * 1.5
Acceptable           → ราคา/price = recipe.price * 1.0
Fail (a) — จับคู่สูตรใกล้เคียงได้ / matched a nearby recipe
                     → ราคา/price = recipe.price * 0.5
Fail (b) — ไม่ตรงสูตรใดเลย / fully unmatched
                     → ราคา/price = 50  (คงที่ / fixed, ไม่ขึ้นกับ drinkType)
```

### 18.2 ผลต่อความสัมพันธ์ / Relationship Impact

ปรับค่าอัตโนมัติหลังเสิร์ฟ ผ่าน YarnCommand ที่เรียกทันทีหลัง reaction แสดงผล:
Auto-adjusted after serving, triggered via YarnCommand right after the reaction plays:

```
Perfect     → $rel_<id> += 0.5
Acceptable  → $rel_<id> += 0.25
Fail        → $rel_<id> += 0        (ไม่เปลี่ยน ทั้งสองแบบของ Fail / no change, either Fail type)
```

## 19. ระบบสั่งเครื่องดื่ม / Order Generation (Technical)

```csharp
// 5 YarnCommands แยกกันตามแบบการสั่ง / one dedicated command per mode:
OrderFixedByName(string recipeName);
OrderFixedByFlavorDescription(string recipeName);       // แสดงเป็นคำอธิบายรสชาติ ไม่ใช่ชื่อ
OrderRandomByPreferenceType(string characterID);         // -> ออกมาเป็นชื่อเครื่องดื่ม
OrderRandomByPreferenceFlavor(string characterID);       // -> ออกมาเป็นคำอธิบายรสชาติ
OrderFixedByType(DrinkType type);                        // สูตรใดก็ได้ในประเภทนี้ ผ่านเงื่อนไข §18
```

### 19.1 ข้อมูลลูกค้า / Customer Data (schema)

```csharp
public class CustomerSO : ScriptableObject
{
    public NPC_Name id;                           // ต้องตรงกับที่ dialogue ใช้ / must match dialogue
    public string customerName;
    public List<DrinkType> preferredDrinkTypes;   // List<DrinkType> ไม่ใช่ List<Recipe>
}
```

> ⚠️ **ไม่มี `relationshipValue` ที่นี่โดยเจตนา** — §22 กำหนดให้ Yarn `$rel_<id>` เป็นแหล่งความจริงเดียว
> การ mirror ค่าลง ScriptableObject ทำให้ค่าที่เขียนตอน runtime ติดค้างข้ามรอบเล่นใน Editor
> และต้องมีโค้ด sync ที่พังเงียบได้ ให้อ่านผ่าน `DialogueRunner.VariableStorage` เสมอ
> **No `relationshipValue` here, on purpose** — §22 makes the Yarn variable the single source of
> truth. A ScriptableObject written at runtime keeps its value between Play sessions in the Editor.
> Always read it from `DialogueRunner.VariableStorage`.

### 19.2 อัลกอริทึมสุ่มเลือกสูตร / Random Selection Algorithm (โหมด 3, 4)

```
candidates = สูตรทั้งหมด.Where(r => customer.preferredDrinkTypes.Contains(r.drinkType))
chosenRecipe = สุ่มแบบ Uniform(candidates)   // ไม่มี weight พิเศษใน v1 / no weighting in v1
```

## 20. Camera FSM (Phase ชงเครื่องดื่ม) — Technical

ขับเคลื่อนด้วยตำแหน่งเมาส์ 4 states มี hysteresis กันการสั่นที่ขอบ zone พิกัดหน้าจอ normalize 0-100%
Mouse-position-driven, 4 states, with hysteresis to prevent flicker at zone boundaries. Screen coords normalized 0–100%.

```
States: LookForward, LookLeft, LookRight, LookDown

จาก/From LookForward:
    mouseX < 20%  → LookLeft
    mouseX > 80%  → LookRight
    mouseY > 70%  → LookDown     (วัด Y จากด้านบน / Y measured from top — confirm sign with camera/art lead)

จาก/From LookLeft:
    mouseX > 75%  → LookForward   (ต้องลากกลับผ่าน 75% ไม่ใช่แค่ 20% / must cross back past 75%, not just 20%)

จาก/From LookRight:
    mouseX < 25%  → LookForward   (ต้องลากกลับผ่าน 25% ไม่ใช่แค่ 80% / must cross back past 25%, not just 80%)

จาก/From LookDown:
    mouseY < 50%  → LookForward   (ควรเช็ค axis sign กับทีม camera/art / clarify axis sign with camera/art lead)
```

ทำเป็นการสลับ Cinemachine virtual camera ตาม state (state → vcam ที่ active) โดยให้ Cinemachine จัดการ blend/damping เอง ไม่ทำ interpolation เอง
Implemented as Cinemachine virtual camera swaps driven by this FSM (state → active vcam); Cinemachine's built-in blend/damping handles the transition, not manual interpolation.

## 21. ระบบเลือกแก้วและตกแต่ง / Glass Selection & Decoration

- **เลือกแก้ว = Cosmetic เท่านั้น** ไม่กระทบ Perfect/Acceptable/Fail ใน §17-18 `glassType` เก็บในสูตร (§16) แต่อัลกอริทึมไม่อ่านค่านี้
  **Glass selection is cosmetic only** — no effect on §17-18 scoring. `glassType` is stored on the recipe (§16) but is never read by the matching algorithm.

### 21.0 `glassType = NotFix` — สูตรที่ให้ผู้เล่นเลือกแก้วเอง

ค่าพิเศษหนึ่งค่าใน `GlassType` เปลี่ยนความหมายของช่องนี้จาก "แก้วของสูตร" เป็น "ผู้เล่นเลือกเอง":
One special value flips this field from "the recipe's glass" to "the player's choice":

| สูตรตั้ง `glassType` เป็น | ผลตอนชง / Behaviour |
|---|---|
| แก้วเจาะจง เช่น `Martini` | สูตรกำหนด — เขียนทับสิ่งที่ผู้เล่นเลือก / recipe wins, overwrites the player's pick |
| **`NotFix`** | **สิ่งที่ผู้เล่นเลือกคงอยู่ ไม่ถูกเขียนทับ** / the player's pick stands |
| `NotFix` แต่ผู้เล่นยังไม่เลือก | ใช้ค่าเริ่มต้น (`Hi_ball`) / falls back to the default glass |
| ไม่ตรงสูตรใดเลย (Fail b, §17.3) | ใช้แก้ว "ไม่รู้จัก" (`Rocks`) ให้ดูออกว่าไม่ใช่เครื่องดื่มจริง / a distinct glass marks an unmatched drink |

ทั้งสี่กรณีเป็น cosmetic ล้วน ไม่มีผลต่อคะแนนตาม §17-18
All four cases are purely cosmetic and do not affect §17-18 scoring.

> **Programmer note:** กติกานี้อยู่ที่ `DrinkBuilder.ApplyGlass` จุดเดียว · UI ที่ให้ผู้เล่นกดเลือก
> ยังไม่ได้ทำ เมื่อทำแล้วให้เรียก `ShakerContents.SetGlass(glass)` ค่าจะไม่ถูกเขียนทับเอง
- การตกแต่งไม่ส่งผลต่อรสชาติ/ส่วนผสม/คะแนน (ตาม GDD)
  Decoration does not affect taste/ingredients/scoring.
- **ขอบเขต v1: Slot-based asset swap เท่านั้น** — แต่ละจุดตกแต่งเป็น slot ตายตัว คลิกแล้วสลับ asset ที่กำหนดไว้ล่วงหน้า
  **v1 scope: slot-based asset swap only** — each decoration point is a fixed slot; clicking cycles a predefined asset.
  🔖 **เฟสต่อไป / Future phase:** ระบบตกแต่งแบบ free-placement (สไตล์ Puni the Florist ลาก-วางอิสระ) — ยังไม่ทำใน v1

### 21.1 การกำหนดสี / Color Resolution

```
ถ้า Flag เป็น Perfect หรือ Seem_Like:
    topColor, bottomColor = ค่าคงที่จาก recipe SO ที่ match
มิฉะนั้น (Flag == Fail แบบไม่ตรงสูตรใดเลย):
    topColor, bottomColor = BlendIngredientColors(poured)   // คำนวณจากวัตถุดิบจริง
```

## 22. ระบบความสัมพันธ์ / Relationship System (Technical)

- เก็บต่อตัวละครเป็นตัวแปร Yarn: `$rel_<characterID>` ช่วง 0–10 ถูกบันทึกลง save data
  Stored per-character as Yarn variable `$rel_<characterID>`, range 0–10, persisted in save data.
- **ไม่มีเกณฑ์กลาง** — แต่ละ Node เขียนเงื่อนไข threshold เอง (`<<if $rel_bob >= 6>>`)
  **No global banding** — each node authors its own threshold checks.
- ค่าปรับหลังเสิร์ฟ: ดู §18.2
  Post-serve adjustment: see §18.2.

## 23. ระบบ Save / Load — Technical

### 23.1 จังหวะการ Save / Timing

- ปุ่ม/UI Save **เปิดใช้งานตลอดเวลา** รวมถึงระหว่าง Phase ชง
  Save button/UI **remains enabled at all times**, including during the mixing phase.
- ระบบเขียน **auto-save checkpoint** แบบเงียบๆ ทันทีที่เข้าสู่ Phase ชง โดยเก็บ Node + Line ID ของบทสนทนา ณ จุดก่อนเข้า Phase ชง
  An **auto-save checkpoint** is silently written the moment mixing phase begins, capturing dialogue Node + Line ID *just before entering mixing*.
- หากผู้เล่นกด Save ระหว่าง/หลัง Phase ชง ระบบจะ **resolve ไปที่ checkpoint นั้นแทน** — ผู้เล่นรู้สึกเหมือน Save จริง แต่จริงๆ Save ไปก่อนหน้าแล้ว
  Pressing Save during/after mixing **resolves to that checkpoint** — gives the feeling of a manual save without persisting live mixing-phase state.

### 23.2 ข้อมูลที่บันทึก / Save Data Contents

```csharp
public class SaveData
{
    public Dictionary<string, float> relationshipByCharacterId;  // mirror ของ $rel_<id>
    public string currentYarnNode;
    public string currentLineId;
    public List<YarnChoiceLogEntry> choiceLogForCurrentNode;      // ดู 23.3
    public float currentMoney;
    public List<ShopLayoutEntry> phase1Layout;                    // ตำแหน่ง NPC/วัตถุดิบ/แก้ว — เฉพาะ Phase 1 เท่านั้น
}
```

`phase1Layout` ครอบคลุมเฉพาะ **Phase 1 (ก่อนเปิดร้าน)** เท่านั้น — ตำแหน่งระหว่าง Phase 2 (ชง) ไม่ถูกบันทึก เพราะ save ระหว่างชงจะ resolve ไปที่ checkpoint ก่อนเข้า Phase เสมอ (§23.1)
`phase1Layout` covers **Phase 1 (pre-open)** only — Phase 2 (mixing) positions are never persisted, since mid-mixing saves always resolve to the pre-mixing checkpoint (§23.1).

### 23.3 กลไกการกลับเข้าสู่บทสนทนา / Yarn Resume Mechanism

Yarn Spinner ไม่รองรับการกระโดดเข้ากลาง Node ได้ตรงๆ ตอน Load ต้อง:
Yarn Spinner cannot jump directly into the middle of a Node. On load:

```
1. เข้า currentYarnNode ใหม่ตั้งแต่ต้น / Re-enter currentYarnNode from the start.
2. เล่นซ้ำแบบเงียบๆ (ไม่แสดง UI) ทุก choice ใน choiceLogForCurrentNode โดย auto-select ตัวเลือกเดิมตามลำดับ
   Silently replay every logged choice, auto-selecting the same option at each <<choice>>, in order.
3. เมื่อถึง currentLineId แล้ว กลับมาแสดงผลปกติต่อจากตรงนั้น
   Once execution reaches currentLineId, resume normal visible playback.
```

```csharp
public struct YarnChoiceLogEntry
{
    public string atLineId;
    public int chosenOptionIndex;
}
```

---

## 24. รายการที่ยังเปิดไว้ / Open Items — Marked for Future

| รายการ / Item | สถานะ / Status |
|---|---|
| UI ให้ผู้เล่นเลือกเองเมื่อมีหลายสูตร match เท่ากัน (§17.2) / Player-driven tie-break UI | เลื่อนออกไป — v1 ใช้ index-order fallback / Deferred — v1 uses index-order fallback |
| ระบบตกแต่งแบบ free-placement สไตล์ Puni the Florist (§21) / Free-placement decoration | เลื่อนออกไป — v1 ใช้ slot-based / Deferred — v1 ships slot-based |
| การสุ่มสูตรแบบมี weight สำหรับโหมดสั่ง 3/4 (§19.2) / Weighted random order selection | เลื่อนออกไป — v1 ใช้ uniform random / Deferred — v1 uses uniform random |
