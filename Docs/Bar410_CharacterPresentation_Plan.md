# Bar410 — Character Presentation Plan (Expression / Look / Enter / Exit)

สถานะ: **ล็อคการตัดสินใจแล้ว (2026-10-29) รอเจ้าของสั่ง "เริ่มลงมือได้"** จากนั้นให้ **Sonnet 5.5** ทำตามลำดับงานด้านล่างทีละ stage

## เป้าหมาย
- Demo 5 วัน มีลูกค้า 8 คน: Cole, Owen, Isla, Walter, Claire, Eliana, Faye, Arthur (Sprite มาทีหลัง)
- เปลี่ยนสีหน้าได้ และหัน ซ้าย / ตรง / ขวา โดยซ้าย/ขวาอิงจอผู้เล่น
- เข้า/ออกร้านแบบ fade silhouette แทนการเลื่อนตาม waypoint
- ข้อมูลภาพอยู่ใน **`SO_Customer` ตัวเดิม** (ตัวละครละ 1 asset รวมข้อมูลเครื่องดื่มกับภาพไว้ที่เดียว) โดย Expression กับ Look เป็นสองแกนแยกกัน และแก้ใน Inspector ได้ง่ายด้วย Odin
- ระบบใหม่มาจาก `CharacterExpression` (ฉาก `New Expression`, PR #21) และแทน `NPC_Base`
- เปิดช่องให้ Demo v2 มีอนิเมชันตัวละคร

## การตัดสินใจ (ล็อคแล้ว)
| # | เรื่อง | ตัดสินใจ |
|---|---|---|
| D1 | Freya | เปลี่ยนชื่อเป็น **Faye** ใช้ค่า enum เดิม (4) asset และ save เดิมจึงไม่เพี้ยน |
| D2 | ชื่อ enum | เปลี่ยน `NPC_Name` เป็น **`CharacterName`** ทั้ง C# และ yarn (`<<enum CharacterName>>`) |
| D3 | ตัวละครใหม่ | **เพิ่มต่อท้าย** ตามลำดับ Claire = 6, Eliana = 7, Arthur = 8 ห้ามเรียงใหม่ |
| D4 | รูปแบบคำสั่ง | รองรับ **ทั้งแบบยาวและแบบสั้น** (ตารางด้านล่าง) สีหน้าใส่ได้ทั้ง **ชื่อและตัวเลข** |
| D5 | Art: ตัว | ตัว (body) วาดแยก 3 รูป: ตรง / ซ้าย / ขวา |
| D6 | Art: หน้า | บางสีหน้าวาดแยกซ้าย/ขวา บางสีหน้าใช้รูปเดียวแล้วพลิก (กติกาในหัวข้อ Data) |
| D7 | ออกจากร้าน | fade ย้อนกลับ (สี → ดำ → หาย) |
| D8 | จุดยืน | ตัวละครหนึ่งตัวอยู่ได้ทีละจุด ถ้าสั่งเข้าจุดใหม่ขณะยืนจุดอื่นอยู่ จะออกก่อนแล้วค่อยเข้าจุดใหม่ |
| D9 | การรอ | Enter / Exit / ย้ายจุด **บล็อก Yarn** จน fade จบ บรรทัดถัดไปจึงเริ่ม |
| D10 | เวลา fade | ค่าเดียวทั้งเกม |
| D11 | หันตามกล้อง | ไม่ต้อง ซ้าย/ขวาคือซ้าย/ขวาของจอ ใช้ sprite ตรงๆ |
| D12 | ที่เก็บข้อมูลภาพ | รวมเข้า **`SO_Customer`** ไม่สร้าง SO ใหม่ และใช้ Odin ทำ editor |
| D13 | เจ้าของโค้ด | ระบบตัวละครเป็น **ส่วนหนึ่งของ `TalkToCustomerManager`** ไม่มี manager ใหม่ (ดูหัวข้อ "ไฟล์") |

## ไฟล์ (D13)
ทำ `TalkToCustomerManager` เป็น `partial class` แบบเดียวกับ `CocktailSystemManager.YarnTask.cs` / `.YarnOrders.cs`
| ไฟล์ | ใหม่/แก้ | มีอะไร |
|---|---|---|
| `Assets/[02]Script/Customer/TalkToCustomerManager.cs` | แก้ | เพิ่มคำว่า `partial` และตารางค้นหาเปลี่ยนจาก `NPC_Base` เป็น `CharacterView` ส่วนอื่นเหมือนเดิม (story, ตัวแปร Yarn, ความชอบ, save) |
| `Assets/[02]Script/Customer/TalkToCustomerManager.Characters.cs` | ใหม่ | `_spots`, เวลา fade (ค่าเดียวทั้งเกม), `Enter` / `Exit` / ย้ายจุด และคำสั่ง Yarn ทั้งแบบยาว 4 ตัวและแบบสั้น 8 ตัว แทน `SetEmotion` / `SetLookDirection` / `MoveIn` / `MoveOut` |
| `Assets/[02]Script/Customer/CharacterView.cs` | ใหม่ | component บนตัวละครแต่ละตัว: `Apply`, `FadeIn`, `FadeOut`, ปุ่ม Preview ไม่มีคำสั่ง Yarn และไม่รู้จัก flow ถูกสั่งจาก manager อย่างเดียว |
| `Assets/[02]Script/Cocktail System/Session/SO_Customer.cs` | แก้ | แท็บ Visual + Odin |
| `Assets/[02]Script/NPC/NPC_Base.cs`, `Assets/[02]Script/CharacterExpression.cs` | ลบ | ถูกแทนด้วย `CharacterView` |

ไม่มี Component ใหม่บน `[GameLoop]` ระบบตัวละครอยู่บน `TalkToCustomerManager` ตัวเดิม (คอมเมนต์ในโค้ดบอกว่าอยู่บน `NPCs_And_Set` ให้ยืนยันในฉากตอน stage 4) และ `FlowWiring` ไม่ต้องแก้

## คำสั่ง Yarn
แบบยาวและแบบสั้นในแต่ละแถวเรียกโค้ดชุดเดียวกัน
| ทำอะไร | แบบยาว | แบบสั้น | รอ |
|---|---|---|---|
| สีหน้า (ชื่อ) | `<<CharacterExpression CharacterName.Isla Sad>>` | `<<Isla Sad>>` | ไม่รอ |
| สีหน้า (เลข) | `<<CharacterExpression CharacterName.Isla 1>>` | `<<Isla 1>>` | ไม่รอ |
| หัน | `<<CharacterLook CharacterName.Isla L>>` (L / F / R) | `<<Isla Look L>>` | ไม่รอ |
| เข้าร้าน / ย้ายจุด | `<<CharacterEnter CharacterName.Isla 2>>` | `<<Isla Enter 2>>` | รอจน fade จบ |
| ออกจากร้าน | `<<CharacterExit CharacterName.Isla>>` | `<<Isla Exit>>` | รอจน fade จบ |

### กติกาของคำสั่ง
- **ชื่อตัวละคร:** รับ `CharacterName.Isla` หรือ `Isla` (`Find()` ตัดคำนำหน้าทิ้งอยู่แล้ว)
- **ไม่ใส่เลขจุด:** `Enter` ใช้จุด 0
- **สีหน้าเลข:** คือลำดับในลิสต์ของ SO และ 0 = สีหน้าเริ่มต้น (Neutral)
- **สะกดผิด:** ชื่อหรือเลขสีหน้าที่ไม่มี ขึ้น warning และคงภาพเดิม
- **ชื่อสีหน้าที่ห้ามใช้:** `Look`, `Enter`, `Exit` เพราะชนกับคำสั่งย่อยของแบบสั้น
- **สั่งระหว่างที่ยังไม่อยู่ในร้าน:** `Expression` / `Look` จำค่าไว้ ตอน `Enter` จะแสดงหน้าและท่านั้นเลย
- **สั่งซ้ำ:**
  - `Enter` จุดเดิมขณะยืนอยู่แล้ว ไม่ทำอะไร
  - `Exit` ตอนไม่อยู่ในร้าน ไม่ทำอะไร
- **Skip หรือ load (silent replay):** fade จบทันที เหมือน `wait_scene`

### วิธีทำแบบสั้น (B2)
- ใช้ static `[YarnCommand("Isla")]` ตัวละครละ 1 บรรทัด ทั้งหมด 8 บรรทัด
  - signature: `IEnumerator Isla(string verb, string arg = null)`
  - Yarn Spinner 3.2.5 รองรับ parameter optional และรอ `IEnumerator` ได้ (`Runtime/Commands/Actions.cs` บรรทัด 267–285)
- ได้ autocomplete และไม่มีเส้นเตือนใน editor
- ราคา: เพิ่มตัวละครใหม่ต้องเพิ่ม 1 บรรทัดในโค้ดด้วย นอกจาก enum
- ไม่เลือกการลงทะเบียนตอน runtime (B1) เพราะ editor ของ Yarn มองไม่เห็นคำสั่ง จะขึ้นเส้นเตือนทุกบรรทัด

## Data: เพิ่มเข้า `SO_Customer` ตัวเดิม (D12)
`SO_Customer` (`Assets/[02]Script/Cocktail System/Session/SO_Customer.cs`) มี `Id`, `CustomerName`, `NameColor`, `PreferredDrinkTypes` อยู่แล้ว
- asset ที่มีอยู่: `Isla`, `Owen`, `Walter` และ `Demo_CustomerRoster` ใน `Assets/[02]Script/NPC/CustomerData/`
- ใช้งานอยู่ที่ `TalkToCustomerManager` (ความชอบเครื่องดื่ม) และ `EventLogPresenter` (สีชื่อ)
- เพิ่มฟิลด์ภาพต่อท้าย ของเดิมไม่แตะ:

```
SO_Customer
  [Tab "Info"]    CharacterName Id, CustomerName, NameColor          (เดิม)
  [Tab "Drinks"]  List<TypeOfCocktail> PreferredDrinkTypes           (เดิม)
  [Tab "Visual"]  (ใหม่)
    LookSprites Forward / Left / Right     // 3 ช่องตายตัว ไม่ใช่ list
      Sprite  Body
      Vector2 FaceOffset                   // ตำแหน่งหน้าผูกกับท่า ไม่ใช่กับสีหน้า
    List<ExpressionRow> Expressions        // index 0 = ค่าเริ่มต้น (Neutral)
      string Id                            // Neutral, Happy, Sad ...
      Sprite Forward
      Sprite Left   (เว้นได้)
      Sprite Right  (เว้นได้)
```
- `ExpressionRow` / `LookSprites` เป็น `[Serializable] struct` ในไฟล์เดียวกับ `SO_Customer`
- ไม่เพิ่ม SO ใหม่และไม่แตะ `SO_CustomerRoster` / `ICustomerPreferences`

### Odin editor ใน `SO_Customer`
ใช้ attribute ของ Odin อย่างเดียว ไม่เขียน custom editor (โปรเจกต์ใช้ Odin อยู่แล้วใน `SO_DaySchedule` และ `HubManager`)
| ส่วน | Attribute | ได้อะไร |
|---|---|---|
| แบ่งหมวด | `[TabGroup("Info")]`, `[TabGroup("Drinks")]`, `[TabGroup("Visual")]` | writer กับ artist เปิดแท็บของตัวเอง |
| 3 ท่า | `[HorizontalGroup("Looks")]` + `[PreviewField(80)]` ที่ `Body` | เห็นรูปตัว ตรง / ซ้าย / ขวา เรียงกัน |
| สีหน้า | `[TableList(ShowIndexLabels = true)]` + `[PreviewField(50)]` ทั้ง 3 ช่อง | ตารางแถวละสีหน้า **เลข index ที่โชว์คือเลขที่ใช้ใน Yarn** (`<<Isla 1>>`) |
| ช่องที่ต้องมี | `[Required]` ที่ `Body` และ `ExpressionRow.Forward` | ขึ้นกรอบแดงถ้าลืมใส่ |
| ชื่อห้ามใช้ / ซ้ำ | `[ValidateInput]` ที่ `ExpressionRow.Id` | เตือนเมื่อชื่อว่าง ซ้ำ หรือเป็น `Look` / `Enter` / `Exit` |
| หน้าข้างที่จะพลิก | `[InfoBox]` แบบมีเงื่อนไขบนแถว | บอกว่า "Left ใช้ Right พลิก" ให้ artist รู้ว่าจะได้รูปไหน |
| ตัวอย่างคำสั่ง | `[ShowInInspector, ReadOnly]` property ที่ต่อชื่อ เช่น `<<Isla Happy>>` / `<<Isla 1>>` | writer คัดลอกคำสั่งได้จาก Inspector |

**ปรับ `faceOffset` ในฉาก:** ทำที่ `CharacterView` ไม่ใช่ที่ SO
- `CharacterView` มีช่อง `[ValueDropdown]` เลือกสีหน้า (ดึงจาก `SO_Customer.Expressions`) และช่อง enum เลือกท่า
- ปุ่ม `[Button("Preview")]` แสดงผลในฉากตอน edit mode ได้ แบบเดียวกับปุ่ม Preview ของ `HubManager`
- ใช้ Undo ใน editor ได้ (อยู่ใต้ `#if UNITY_EDITOR` และไม่รันตอนเล่น)

### กติกาหน้าตอนหันข้าง (D6)
- หันซ้าย: ใช้ `left` → ถ้าไม่มี ใช้ `right` พลิก (flipX) → ถ้าไม่มีอีก ใช้ `forward` และขึ้น warning
- หันขวา: ใช้ `right` → ถ้าไม่มี ใช้ `left` พลิก → ถ้าไม่มีอีก ใช้ `forward` และขึ้น warning
- ตัว (body) ไม่พลิก เพราะมี 3 รูปครบ (D5)

## View: `CharacterView` (MonoBehaviour ใหม่ แทน `NPC_Base` + `CharacterExpression`)
- ถือ Body / Face SpriteRenderer และ **`SO_Customer`**
  - ชื่อตัวละครมาจาก `SO_Customer.Id` ไม่มีช่องชื่อซ้ำบน View
  - `TalkToCustomerManager.Awake` สร้างตารางค้นหาจาก View ทุกตัวใต้ตัวเอง เหมือนที่ทำกับ `NPC_Base` ตอนนี้
- state:
  - `ExpressionIndex`
  - `Look` (enum ใหม่ `Facing { Forward, Left, Right }`)
  - `IsPresent`
  - `Spot`
- **`Apply()` คือที่เดียวที่เปลี่ยน sprite, flip และ faceOffset** (ดูหัวข้อ v2)
- Fade ใช้ DOTween tint สี (`SpriteRenderer.color`) ของ renderer **ทุกตัวใต้ตัวละคร** พร้อมกัน (เผื่อ rig v2) เวลา T ค่าเดียวทั้งเกม (D10):
  - **`FadeIn()`:** ดำ alpha 0→1 ในครึ่งแรก แล้วดำ→ขาวในครึ่งหลัง
  - **`FadeOut()`:** ย้อนกลับ ขาว→ดำในครึ่งแรก แล้วดำ alpha 1→0 ในครึ่งหลัง
- ไม่ใช้ `UnityEditor.Undo` ตอนเล่น ใช้ได้แค่ในปุ่ม Preview ตอน edit mode (ของเดิมใน `CharacterExpression` ทิ้งไป)

### จุดยืนและ Enter / Exit (D8, D9) อยู่ที่ `TalkToCustomerManager.Characters.cs`
- `[SerializeField] Transform[] _spots`: จุดยืนของร้าน ใช้ร่วมกันทุกตัวละคร
- **`Enter(name, spot)`** เป็น coroutine ที่ Yarn รอ:
  - ยังไม่อยู่ในร้าน: วางตัวที่จุดนั้นแล้ว `FadeIn`
  - อยู่จุดเดิมแล้ว: ไม่ทำอะไร
  - อยู่จุดอื่น: `FadeOut` → ย้ายไปจุดใหม่ → `FadeIn`
- **`Exit(name)`:** `FadeOut` แล้ว `IsPresent = false`
- ถ้าตัวละครอื่นยืนจุดนั้นอยู่แล้ว ขึ้น warning แต่ยังทำต่อ
  - ponytail: ไม่บังคับให้อีกตัวหลบ เพิ่มเมื่อบทต้องใช้จริง

## Save
- ปรับ `NPCData` ให้เก็บค่าที่ระบบใหม่ใช้: `isPresent`, `spot`, `expressionIndex`, `look`
- เลิกเก็บ position / rotation / waypoint
- `Restore` วางตัวละครทันที ไม่เล่น fade
- Save/Load ทั้งระบบยังพักอยู่ (ดู CLAUDE.md) รอบนี้แค่ให้ข้อมูลตัวละครครบ

## ช่องสำหรับอนิเมชัน (Demo v2: 2D bone + motion) — แค่ทำเครื่องหมายไว้ ไม่ทำใน Demo นี้
v2 คือการใส่ bone 2D ให้ตัวละคร แล้วทำ motion (เช่น หายใจ ขยับไหล่ พยักหน้า) ด้วย Unity 2D Animation
- package ติดตั้งไว้แล้ว: `com.unity.2d.animation` 13.0.5 และ `com.unity.2d.psdimporter` 12.0.2
- v2 น่าจะทำแบบนี้:
  - art ส่งเป็น `.psb` แยก layer (ตัว / หน้า / แขน ...)
  - rig ใน Skinning Editor แล้วได้ prefab ที่มี `SpriteSkin` และ bone
  - motion อยู่ใน `Animator` บน root ของตัวละคร

### สิ่งที่ Demo นี้ทำเผื่อไว้ (ไม่เพิ่มโค้ด แค่วิธีเขียน)
- **`CharacterView.Apply()` คือจุดเดียวที่เปลี่ยนภาพ:**
  - v2 เปลี่ยนข้างในจากสลับ sprite เป็น `SpriteResolver.SetCategoryAndLabel("Face", id)` (Sprite Library ของ 2D Animation ใช้สลับสีหน้าบน rig)
  - Yarn, Save และ SO ฝั่ง writer ไม่ต้องแก้
- **fade tint `SpriteRenderer` ทุกตัวใต้ตัวละคร (`GetComponentsInChildren`):**
  - Demo นี้มีแค่ Body กับ Face
  - rig ใน v2 มีหลายชิ้น fade ก็ยังใช้ได้โดยไม่ต้องแก้
  - โค้ดยาวเท่ากับการอ้าง 2 renderer ตรงๆ
- **Look แต่ละท่าแยกเป็น child object ได้:**
  - rig ด้านตรงกับด้านข้างมักมีโครง bone ไม่เหมือนกัน จึงสลับ sprite บน rig เดียวไม่ได้
  - v2 จะเปลี่ยน `Apply()` ให้เปิด/ปิด child ของท่านั้นแทนการสลับ body sprite
  - Demo นี้ยังสลับ sprite ตามเดิม
- **motion ไม่ชนกับ fade:**
  - fade แก้แค่ `SpriteRenderer.color`
  - ถ้าใน v2 Animator มี key สี จะทับกัน
  - ตอนทำ v2 ห้าม key สีใน clip หรือย้าย fade ไปใช้ `SortingGroup` / material แทน
- **`FadeIn()` / `FadeOut()` เป็น coroutine ที่ Yarn รออยู่แล้ว:** v2 เพิ่ม motion ตอนเข้า/ออกได้โดยบทยังรอเหมือนเดิม
- **SO:** `ExpressionRow` / `LookSprites` ใน `SO_Customer` คือที่เพิ่ม label ของ Sprite Library หรือชื่อ state ของ Animator ในอนาคต
- **verb แบบสั้น** ขยายได้ เช่น `<<Isla Motion Nod>>` โดยไม่กระทบคำสั่งเดิม

### เครื่องหมายในโค้ด
- ใส่ comment `// v2 (2D bone rig): ...` บอกว่าจะเปลี่ยนอะไร ที่ `Apply()`, `FadeIn()`, `FadeOut()` และที่ฟิลด์ `Body` ของ `LookSprites`
- ไม่สร้าง interface, Animator หรือ field ว่างไว้ก่อน

## แปลงบทเก่า
คำสั่งเก่า 231 ที่ใน 5 ไฟล์ แปลงด้วยสคริปต์เป็นแบบสั้น:
| เดิม | ใหม่ |
|---|---|
| `<<SetEmotion NPC_Name.Isla "happy">>` | `<<Isla Happy>>` |
| `<<SetEmotion NPC_Name.Isla "neutral">>` | `<<Isla Neutral>>` |
| `<<SetEmotion NPC_Name.Isla "upset">>` | `<<Isla Sad>>` (ชื่อสีหน้าใน SO ต้องมี `Sad`) |
| `<<SetLookDirection NPC_Name.Isla LookDirection.Left>>` | `<<Isla Look L>>` (Forward → F, Right → R) |
| `<<MoveIn NPC_Name.Isla 2>>` | `<<Isla Enter n>>` (n = จุดยืนใหม่ ดู stage 4) |
| `<<MoveOut NPC_Name.Isla>>` / `NPC_Name.isla` | `<<Isla Exit>>` |
| `NPC_Name.X` ที่อื่น (เช่น `Order_Cocktail_OutName(NPC_Name.Isla)`) | `CharacterName.X` |

ไฟล์อื่นที่ต้องแก้ในขั้นนี้:
- yarn: ลบ `<<enum LookDirection>>` และเปลี่ยน `<<enum NPC_Name>>` เป็น `<<enum CharacterName>>` ที่มี 9 case เรียงตรงกับ C#
- `Assets/[06]Dialogue/YarnScript/Day1_Demo.yarn` (นอก `Demo/`) ไม่อยู่ใน Yarn Project **ลบทิ้ง** แทนการแปลง

## ลำดับงาน — compile-check ทุก stage (`read_console` ต้องไม่มี error)
1. **Enum:**
   - เปลี่ยน `NPC_Name` เป็น `CharacterName` ทุกไฟล์ (~15 ไฟล์ C#)
   - เปลี่ยน Freya เป็น Faye
   - เพิ่ม Claire, Eliana, Arthur ต่อท้าย
   - เปลี่ยน `<<enum>>` ใน yarn ให้ตรงกัน
2. **Data + View:**
   - เพิ่มแท็บ Visual และ attribute Odin ใน `SO_Customer` (ตารางในหัวข้อ Data)
   - สร้าง `CharacterView` (`Apply`, `FadeIn`, `FadeOut`, ปุ่ม Preview)
   - ใส่รูป `PH_*` ใน asset `Isla`, `Owen`, `Walter` ที่มีอยู่
   - สร้าง asset ใหม่ 5 ตัว (`Cole`, `Faye`, `Claire`, `Eliana`, `Arthur`) และเพิ่มเข้า `Demo_CustomerRoster`
   - ลบ `CharacterExpression.cs`
   - ตรวจว่า Event Log ยังได้สีชื่อ และการสั่งเครื่องดื่มตามความชอบยังทำงาน
3. **คำสั่ง:**
   - ทำ `TalkToCustomerManager` เป็น `partial` แล้วสร้าง `TalkToCustomerManager.Characters.cs` ใส่คำสั่งแบบยาว 4 ตัวและแบบสั้น 8 ตัว
   - เพิ่ม `_spots` และ `Enter` / `Exit`
   - ทำกติกา skip / silent replay
   - ลบ `SetEmotion`, `SetLookDirection`, `MoveIn`, `MoveOut`
4. **ฉาก `New Drag Drop System`:**
   - ใส่ `CharacterView` ให้ตัวละครใต้ `NPCs_And_Set` แทน `NPC_Base` และเพิ่มตัวละครที่ยังไม่มีให้ครบ 8
   - สร้าง `_spots` จากตำแหน่ง waypoint บาร์เดิม (Walter 1, Isla 2, Owen 3) แล้วกำหนด n ในตารางแปลงบท
   - ลบ `NPC_Base.cs`
5. **แปลงบท:**
   - แปลง yarn 231 ที่ด้วยสคริปต์
   - ลบ `Day1_Demo.yarn` ตัวนอก `Demo/`
   - อัปเดตเอกสาร: `Docs/YarnCommands/` (ลบ 4 ไฟล์เก่า, เพิ่มไฟล์คำสั่งใหม่, อัปเดต README), `TEST_AllCommands.yarn` และ CLAUDE.md (ตาราง Component)
6. **Save:** ปรับ `NPCData`, `Snapshot` และ `Restore`
7. **ทดสอบใน Play:**
   - `TEST_AllCommands`: ทุกคำสั่งทั้งแบบยาวและแบบสั้น
   - ย้ายจุด
   - Skip ระหว่าง fade
   - เล่นวันที่ 1

แต่ละ stage เป็น commit แยกเมื่อสั่ง

## ยังไม่ครอบคลุม / ความเสี่ยง
- **Sprite จริงยังไม่มี:** ใช้ placeholder ขนาดและ `faceOffset` ต้องปรับใหม่เมื่อ art มา
- **ค่าใน Inspector อาจหาย:** ฟิลด์ที่ serialize เป็น `NPC_Name` เก็บเป็นตัวเลข การเปลี่ยนชื่อ type จึงไม่ทำให้ค่าหาย แต่ต้องตรวจใน Inspector ว่า `SO_Customer.Id` และ `CharacterData` ยังชี้ถูกตัว
- **ซ้าย/ขวาในบทเก่าอาจกลับด้าน:** `LookDirection.Left` ในบทเก่าหมายถึงซ้ายของจอหรือของตัวละคร ต้องดูด้วยตาตอน stage 7

## Implementation log (2026-10-29)
- Stage 1 Enum: `NPC_Name` -> `CharacterName` (C# + yarn `<<enum>>`), Freya -> Faye, Claire 6 / Eliana 7 / Arthur 8. Console 0 errors.
- Stage 2 Data + View: `SO_Customer` Odin tabs (Info / Drinks / Visual), `CharacterView`, assets Cole/Faye/Claire/Eliana/Arthur created and all 8 added to `Demo_CustomerRoster`; Isla/Owen/Walter filled with `PH_*` sprites. Forward body = `PH_BodyLeft` (the New Expression scene's Neutral body; there is no front placeholder). `CharacterExpression.cs` deleted. Console 0 errors.
- Stage 3 Commands: `TalkToCustomerManager` is partial; `.Characters.cs` has `_spots`, `_fadeSeconds` (0.8), Enter/Exit, long form x4, short form x8, skip/silent = instant fade. Old four commands removed. Save (stage 6) was done in the same compile: `NPCData` = isPresent / spot / expressionIndex / look, `Snapshot` / `Restore`; `E_Cocktail.Direction` removed (unused). Console 0 errors.
- Stage 4 Scene: `New Drag Drop System` > `NPCs_And_Set` > `NPC_*` x8 each with `CharacterView`, children `Body` / `Face` (root scale 1, Body 0.18, Face 0.07 like New Expression); `_spots` = `WayPoint (0..3)` so spot n = old waypoint n (Walter 1, Isla 2, Owen 3). `NPC_Base.cs` deleted. Leftover missing-script components on 3 `TMP_NPC/NPC_Owen (1)` objects removed.
- Stage 5 Yarn: scripted migration (77 SetEmotion, 57 SetLookDirection, 10 MoveIn, 10 MoveOut, LookDirection enum removed); grep shows 0 old names in .yarn/.cs. `YarnScript/Day1_Demo.yarn` (outside Demo/) deleted. Docs/YarnCommands, TEST_AllCommands (node TEST_NPC rewritten) and CLAUDE.md updated. Gotcha: `<<...>>` inside a dialogue line is a Yarn compile error; Yarn import errors do not show as C# errors, check the console after reimporting.
- Stage 7 Play test (via code and a real Yarn run of `TEST_NPC`): expression by name/int, look L/F/R, flip fallback (Happy+Left = Right face flipped), Forward fallback warning (Sad+Left), unknown expression warning, Enter blocks ~0.83s, same-spot Enter no-op, move = out -> move -> in (~1.6s), Exit ~0.8s, Exit when absent no-op, silent replay finishes in 1-2 frames, Snapshot/Restore round trip instant. Not checked by eye: fade look, art alignment, flipped-side direction in the old scripts, Day 1 played through, real skip button.
