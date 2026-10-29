# Bar410: ระบบปลดล็อควัตถุดิบ ทำงานอย่างไร

อัปเดต 2026-10-29 · แผนและการตัดสินใจ: [Bar410_IngredientUnlock_Plan.md](Bar410_IngredientUnlock_Plan.md), [adr/0003](adr/0003-ingredient-unlock.md)

ไฟล์นี้อธิบายว่าระบบทำงานยังไง ข้อมูลไหลผ่านอะไรบ้าง และถ้าจะเปลี่ยนพฤติกรรมต้องไปแก้ที่ไหน

---

## 1. ภาพรวมแบบสั้น

- วัตถุดิบแต่ละตัวมี **วันปลดล็อค** อยู่ในตาราง (`IngredientUnlockTable.asset`)
- ถ้า `$day` (ตัวแปร Yarn) ถึงวันนั้นแล้ว ถือว่าปลดล็อค
- บท Yarn สั่ง `<<unlock_ingredient X>>` เพื่อปลดล็อคก่อนถึงวันได้ และปลดล็อคแล้วถาวร
- ระบบอื่น**ไม่เก็บสถานะเอง** ทุกระบบมาถาม `IngredientUnlocks` ที่เดียว:
  - **บาร์:** ขวดที่ยังล็อคถูกซ่อน
  - **ออเดอร์สุ่ม:** สุ่มเฉพาะสูตรที่วัตถุดิบปลดล็อคครบ
  - **Event ตอนเช้า:** popup "Unlock New Ingredient" แจ้งของใหม่ วันละอันเดียว
  - **Yarn:** เช็คด้วย `ingredient_unlocked("X")`
  - **Save:** เก็บแค่ที่บทปลดล็อค และรายการที่แจ้งผู้เล่นแล้ว

```
                      ┌──────────────────────────┐
  IngredientUnlockTable│  SO (ตารางวัน)            │
  .asset ─────────────►│                          │
  $day (Yarn) ────────►│   IngredientUnlocks      │◄── <<unlock_ingredient X>> (Yarn)
  save (UnlockData) ──►│   [GameLoop] ›           │
                      │   [IngredientUnlocks]    │──► ingredient_unlocked("X") (Yarn)
                      └────────────┬─────────────┘
                                   │ FlowWiring ส่งคำตอบต่อ (manager ไม่รู้จักกัน)
          ┌────────────────────────┼─────────────────────────┐
          ▼                        ▼                         ▼
  IngredientButtonGroup     CocktailSystemManager      EventPopupQueue
  (ซ่อน/โชว์ขวด)             → OrderService             (popup + เสียง)
                            (กรองออเดอร์สุ่ม)
```

---

## 2. กติกาหลัก: "ปลดล็อคแล้ว" คำนวณใหม่ทุกครั้ง ไม่ได้เก็บ

```
IsUnlocked(x) =  ไม่มีแถวของ x ในตาราง                → true   (ของใหม่ในอนาคตไม่หายจากบาร์)
             หรือ x อยู่ในรายการที่บทปลดล็อค (story)     → true
             หรือ (Unlock Day > 0 และ $day >= Unlock Day) → true
             นอกนั้น                                   → false
```

- **ปลดล็อคตามวันไม่ต้อง save** เพราะ `$day` ถูก save ไปกับตัวแปร Yarn อยู่แล้ว
- **Unlock Day = 0** แปลว่าไม่ปลดล็อคเองตามวัน ต้องรอบทสั่งเท่านั้น
- **ชื่อวัตถุดิบ = ชื่อ enum** (`Gin`, `LemonJuice`, `PepperMint`, …) ใช้ทั้งในตาราง ใน Yarn และในโค้ด ชื่อจาก 3 enum (`BaseSpirit` / `Liqueur` / `Mixer`) ไม่ซ้ำกัน
- **สูตรทำได้ (`IsRecipeComplete`)** = วัตถุดิบทุกตัวใน Alcohol / Liqueur / Mixer ของสูตรปลดล็อคแล้ว (ไม่นับ None)

---

## 3. ชิ้นส่วนทั้งหมด (ไฟล์ › Component › GameObject)

| ชิ้น | ไฟล์ | อยู่ที่ (ฉาก `New Drag Drop System`) | หน้าที่ |
|---|---|---|---|
| ตารางวัน (data) | `Assets/[02]Script/Ingredient Unlock/SO_IngredientUnlockTable.cs` | asset `Assets/[02]Script/Ingredient Unlock/IngredientUnlockTable.asset` | แถวละวัตถุดิบ: Id (เลือกหมวด + ค่าจาก enum จริง) / Unlock Day / Display Name / **Icon** มีปุ่ม **Add Missing Ingredients** และเตือนเมื่อแถวซ้ำ/ว่าง |
| **ตัวกลาง** | `Assets/[02]Script/Ingredient Unlock/IngredientUnlocks.cs` | `[GameLoop]` › `[IngredientUnlocks]` | ตอบ `IsUnlocked` / `IsRecipeComplete`, จำที่บทปลดล็อค + ที่แจ้งแล้ว, คำสั่ง/ฟังก์ชัน Yarn, `Snapshot`/`Restore` ให้ save |
| ตัวต่อสาย | `Assets/[02]Script/Hierarchical State Machine/FlowWiring.cs` | `[GameLoop]` (ช่อง `_unlocks`) | ส่งคำตอบของ `IngredientUnlocks` ไปให้บาร์ ออเดอร์ และ popup ตาม Phase |
| ขวดบนบาร์ | `Assets/[02]Script/Cocktail System/Cocktail/Shaker/IngredientButtonGroup.cs` | `[GameLoop]` › `[CocktailSystem]` | `Refresh(isUnlocked)` เปิด/ปิดขวด |
| ขวดแต่ละขวด | `Assets/[02]Script/Cocktail System/IngredientButtonUI.cs` | `[CocktailSystem]` › `Base Spirits` / `Liqueur` / `Mixer` › ขวด 17 ขวด | `IngredientKey` บอกว่าขวดนี้คือวัตถุดิบอะไร (ปุ่ม Ice / Method / Reset ได้ null จึงไม่ถูกซ่อน) |
| ออเดอร์ | `Assets/[02]Script/Cocktail System/Cocktail/CocktailSystemManager.cs` + `Assets/[02]Script/Cocktail System/Session/OrderService.cs` | `[GameLoop]` › `[CocktailSystem]` | `UseRecipeFilter` รับตัวกรอง แล้ว `OrderService` ใช้กรองการสุ่ม |
| popup | `Assets/[02]Script/UI Script/EventPopupQueue.cs` | `[GameLoop]` › `Canvas_HUD` › `Panel_EventPopup` | แสดง popup ทีละอัน + เสียง (`_showSoundId`) |
| save | `Assets/[02]Script/SaveLoad/SaveUnlockData.cs` + `SaveLoadManager.cs` | `[SaveLoadSystem]` | เก็บ / คืน `UnlockData` |
| ชนิดของ key | `Assets/[02]Script/Ingredient Unlock/IngredientId.cs` | ใช้เป็นช่อง `id` ของแถว | หมวด (Spirit / Liqueur / Mixer) + ค่า enum ของหมวดนั้น `Key` = ชื่อ enum (`"LemonJuice"`) ที่ Yarn, save, `IngredientKey` ใช้ต่อ enum เก็บเป็นตัวเลข จึง **เพิ่มต่อท้ายเท่านั้น ห้ามสลับลำดับ/แทรก** (กติกาเดียวกับสูตร) เปลี่ยนชื่อ enum ได้ ตารางตามให้เอง |
| ตรวจข้อมูล | `Assets/Editor/CocktailDataValidator.cs` | เมนู `Bar410 > Validate Cocktail Data` | แถวว่าง/ซ้ำ = error, วัตถุดิบ enum ที่ไม่มีแถว = warning, + จำนวนสูตรที่ทำได้ต่อวัน |
| ทดสอบ | `Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` | node `TEST_UnlockPopup`, `TEST_Unlock*` | test ใน Play |

`IngredientUnlocks` เป็น singleton แบบ scene-scoped (`IngredientUnlocks.Current`) ถ้ามีตัวซ้ำ ตัวหลังทำลายตัวเอง ไม่ใช้ DontDestroyOnLoad

---

## 4. Flow ตามลำดับเวลา

### 4.1 เปิดฉาก (`Awake`)
1. `IngredientUnlocks.Awake` ตั้ง `Current` และใส่วัตถุดิบที่ Unlock Day = 1 ลงรายการ "แจ้งแล้ว" (วัน 1 จึงไม่มี popup)
2. `FlowWiring.Awake` เรียก `_cocktail.UseRecipeFilter(_unlocks.IsRecipeComplete)`
   → `CocktailSystemManager` สร้าง `OrderService` ใหม่พร้อมตัวกรองนี้
   → `CocktailSystemManager` ได้แค่ฟังก์ชัน ไม่รู้จัก `IngredientUnlocks`

### 4.2 เริ่มวัน → BeforeOpen › Event
```
FlowWiring.OnEventEntered
  ├─ _unlocks.TakeNewUnlocks()        // ของที่ปลดล็อคแล้วแต่ยังไม่แจ้ง (ไล่ตามลำดับแถวในตาราง)
  │     และจดว่า "แจ้งแล้ว" ทันที (เรียกซ้ำได้ลิสต์ว่าง)
  ├─ ถ้ามี → _eventPopups.Enqueue("Unlock New Ingredient", Display Name ทีละบรรทัด)   // อันเดียว ไม่มีรูป
  └─ _eventPopups.ShowAll()           // คิวว่าง = Event จบทันที
        └─ popup ขึ้น → เล่นเสียง _showSoundId (ถ้าใส่) → ผู้เล่นกดปิด → AllClosed → ไป StoryBeforeOpen
```
ตัวอย่าง: `$day` เปลี่ยนจาก 2 เป็น 3 → popup มี Whiskey / Dry Vermouth / Peppermint

### 4.3 ขวดบนบาร์ (แก้ 2026-10-29: ขวดขึ้นทันที)
บาร์อยู่ในจอทุก Phase จึงต้องอัปเดตขวดทันทีที่สถานะเปลี่ยน ไม่ใช่รอเข้าบาร์
```
FlowWiring.RefreshBottles()  →  _ingredients.Refresh(_unlocks.IsUnlocked)
                                  └─ ทุกขวด: SetActive(IsUnlocked(IngredientKey))   // ล็อค = ซ่อน, ปลดล็อคแล้ว = โชว์
ถูกเรียกเมื่อ:
  ├─ IngredientUnlocks.Changed   // <<unlock_ingredient>> ปลดล็อคของใหม่ หรือ load save → ขวดขึ้นทันที ทุก Phase
  ├─ OnEventEntered              // เริ่มวันใหม่ ($day เปลี่ยน) → ขวดของวันนี้ขึ้นก่อนเปิดร้าน
  └─ OnAddIngredientEntered      // กันพลาด เช่น $day ถูก <<set>> ในบทระหว่างวัน
```
`Changed` ไม่ยิงตอน `$day` เปลี่ยน (ตัวแปร Yarn ไม่มี event) จึงต้องมีจุด Event และ AddIngredient ด้วย

### 4.4 ออเดอร์
| ทางสั่ง | กรองไหม | ถ้าไม่มีสูตรเหลือ |
|---|---|---|
| สุ่มตามรสนิยมลูกค้า (`Order_Cocktail_OutName(n)` → `OrderService.PickForCustomer`) | กรอง | สุ่มจากทุกสูตรที่ทำได้ + warning |
| สุ่มทั่วไป / ตามประเภท (`RandomCocktail()`, `RandomCocktail(type)` → `PickRandom`) | กรอง | สุ่มจากทุกสูตรที่ทำได้ + warning (ไม่มีเลย = null + error) |
| ระบุชื่อ (`Order_Cocktail_ByName_*`) | **ไม่กรอง** (writer ตั้งใจ) | warning ถ้ายังทำไม่ได้ |
| ตามประเภทแบบไม่มีสูตรเป้าหมาย (`order_by_type`) | ไม่มีอะไรให้กรอง | warning ถ้าประเภทนั้นยังไม่มีสูตรที่ทำได้ |

### 4.5 Yarn
```
<<unlock_ingredient Rum>>          // ปลดล็อคทันที ถาวร เรียกซ้ำไม่มีผล ตัวพิมพ์เล็ก/ใหญ่ได้ ชื่อผิด = LogError แล้วบทเล่นต่อ
<<if ingredient_unlocked("Rum")>>  // true/false ชื่อผิด = LogError + false
```
- popup ของสิ่งที่บทปลดล็อคจะขึ้นตอน Event **ครั้งถัดไป**
- **ข้อจำกัดตอนนี้:** ถ้าบทเรียก `<<flow_check_events>>` ภายหลังในวันเดียวกัน popup จะขึ้นวันนั้นเลย ไม่ได้รอวันถัดไปตามคำตอบ A ยังไม่ได้แก้ (ดูหัวข้อ 7)
- เอกสาร: `Docs/YarnCommands/unlock_ingredient.md`, `Docs/YarnCommands/ingredient_unlocked.md`

### 4.6 Save / Load
- **Save:** `SaveLoadManager.SaveToFile` → `IngredientUnlocks.Current.Snapshot()` → `SaveData.UnlockData { StoryUnlocked, Notified }`
- **Load:** `SaveLoadManager.OnSceneLoaded` → `Restore(UnlockData)` **แทนที่**ข้อมูลเดิม (ไม่รวม)
- Save/Load ของฉากหลักยังพังอยู่ (ดู CLAUDE.md) ส่วนนี้ทดสอบแค่ Snapshot → JSON → Restore ไม่ได้ลองโหลดเกมจริง

---

## 5. สิ่งที่แก้ได้เองโดยไม่ต้องแตะโค้ด

| อยากทำ | แก้ที่ |
|---|---|
| เพิ่มวัตถุดิบใหม่ลงตาราง | กดปุ่ม **Add Missing Ingredients** บน `IngredientUnlockTable.asset` (Undo ได้ ขึ้น log ว่าเติมอะไร) |
| เปลี่ยนวันที่ปลดล็อค | `IngredientUnlockTable.asset` › คอลัมน์ **Unlock Day** |
| ให้ปลดล็อคด้วยเนื้อเรื่องเท่านั้น | ตั้ง **Unlock Day = 0** แล้วใส่ `<<unlock_ingredient X>>` ในบท |
| ปลดล็อคก่อนวันในตาราง | ใส่ `<<unlock_ingredient X>>` ในบท (ใช้ร่วมกับ Unlock Day ได้) |
| เปลี่ยนชื่อที่ขึ้นใน popup | `IngredientUnlockTable.asset` › **Display Name** (ว่าง = ชื่อ enum) |
| รูปใน popup | `IngredientUnlockTable.asset` › **Icon** ของแต่ละแถว popup ของวันนั้นเรียงไอคอนของทุกตัวที่ปลดล็อคใหม่ในแถวเดียว (แถวที่ไม่ใส่ Icon = ไม่มีไอคอนของตัวนั้น ไม่มีเลย = ไม่มีรูป) |
| ใส่เสียงตอน popup ขึ้น | `Canvas_HUD` › `Panel_EventPopup` › `EventPopupQueue` › **Show Sound Id** (ID ของ UiSFX ใน `SoundManager`) ใช้กับ popup ทุกชนิด |
| หน้าตา popup | GameObject ใต้ `Panel_EventPopup` › `Window` (หัวข้อ / ข้อความ / `Panel_Icons` / ปุ่มปิด) ไอคอนคือ `Panel_Icons` › `IMG_Popup` (Horizontal Layout Group จัดเรียง ตัวที่ 2 ขึ้นไปเป็นสำเนาของ `IMG_Popup` แต่งขนาด/สีที่ `IMG_Popup` ตัวเดียว ระยะห่างที่ `Panel_Icons`) |
| แยกบทตามของที่มี | `<<if ingredient_unlocked("Campari")>> ... <<endif>>` |

หลังแก้ตารางให้รัน **`Bar410 > Validate Cocktail Data`** ทุกครั้ง ระบบจะบอกจำนวนสูตรที่ทำได้ต่อวัน ตอนนี้คือ day 1 = 8, day 3 = 11, day 5 = 19, day 7 = 26

---

## 6. ถ้าจะเปลี่ยนพฤติกรรม ต้องแก้โค้ดตรงไหน

| อยากเปลี่ยน | ไฟล์ › จุด |
|---|---|
| กติกาว่าอะไรนับว่าปลดล็อค | `IngredientUnlocks.cs` › `IsUnlocked` |
| กติกาว่าสูตรไหนทำได้ | `IngredientUnlocks.cs` › `IsRecipeComplete` |
| หัวข้อ/รูปแบบข้อความ popup | `FlowWiring.cs` › `OnEventEntered` (ข้อความ `"Unlock New Ingredient"` และ `string.Join("\n", …)`) |
| popup ขึ้นเมื่อไหร่ / ของไหนนับว่าใหม่ | `IngredientUnlocks.cs` › `TakeNewUnlocks` (และตอนเรียกใน `FlowWiring.OnEventEntered`) |
| วัน 1 ไม่มี popup | `IngredientUnlocks.cs` › `Awake` (ใส่ Unlock Day = 1 ลง notified) |
| ซ่อนขวดแบบอื่น (เช่น เทา แทนซ่อน) | `IngredientButtonGroup.cs` › `Refresh` |
| ขวดไหนนับเป็นวัตถุดิบ | `IngredientButtonUI.cs` › `IngredientKey` |
| ออเดอร์สุ่ม/fallback | `OrderService.cs` › `PickForCustomer`, `PickRandom`, `PickRandom(type)` |
| ออเดอร์ระบุชื่อแล้วจะบล็อกแทนเตือน | `OrderService.cs` › `OrderByName` |
| สิ่งที่ save | `SaveUnlockData.cs` + `IngredientUnlocks.Snapshot` / `Restore` |
| หน้าสูตรในหนังสือ (ยังไม่ทำ) | เรียก `IngredientUnlocks.Current.IsUnlocked` / `IsRecipeComplete` จาก UI หนังสือ กติกาที่ตกลงไว้: สูตรไม่ครบ = ชื่อที่ปลดล็อค + `???` ไม่มีปริมาณ, สูตรครบ = ชื่อ + ปริมาณทุกตัว |
| เพิ่มวัตถุดิบใหม่ | เพิ่มค่า **ต่อท้าย** enum ใน `E_Cocktail.Drink.cs` → เพิ่มขวดบนบาร์ → เปิด `IngredientUnlockTable.asset` กด **Add Missing Ingredients** (เติมแถว Unlock Day 1 ให้เอง) แล้วแก้วัน/ชื่อ → Validate (ไม่มีแถว = ปลดล็อคตั้งแต่ต้น และ Validate จะเตือน) |

กติกาสถาปัตยกรรมที่ต้องรักษา: **manager ไม่รู้จัก `IngredientUnlocks`** ให้ `FlowWiring` ส่งฟังก์ชันหรือคำตอบไปให้เท่านั้น

---

## 7. ข้อจำกัดที่รู้แล้ว
- **popup วันเดียวกัน:** `<<flow_check_events>>` กลางบทเรียก `TakeNewUnlocks` อีกรอบ สิ่งที่บทปลดล็อคก่อนหน้าในวันนั้นจะถูกแจ้งเลย (คำตอบ A บอกให้รอวันถัดไป) รอเจ้าของตัดสินว่าจะแก้ไหม
- **save เก่า:** save ที่ทำก่อนมีระบบนี้ไม่มี `UnlockData` โหลดแล้วอาจมี popup ของวัน 1 ขึ้นหนึ่งครั้ง
- **Demo มี 5 วัน:** Tequila / Campari (วัน 7) จะไม่ปลดล็อคใน Demo ถ้าบทไม่สั่ง

## 8. ยังไม่ได้ทดสอบ
- หน้าตาจริงของ popup/ขวด และการคลิกเมาส์/controller จริง (ทดสอบด้วยโค้ดเท่านั้น)
- save ลงไฟล์จริงแล้วโหลดกลับ
- วัน 5 และ 7 ตอนเล่นจริง
- หน้าสูตรในหนังสือ (มีแค่ API)
- node ใหม่ใน `TEST_AllCommands.yarn` (compile ผ่าน ยังไม่ได้รันใน Play)

## 9. วิธีทดสอบ
1. `DaySchedule_Demo.asset` ตั้ง Start Node ของวัน 1 เป็น `TEST_Start` แล้วกด Play (ตั้งกลับหลังทดสอบ)
2. ใน `TEST_Start` เลือก "Test the ingredient unlock popup" (ทดสอบ popup ในช่วง BeforeOpen)
3. ใน hub เลือก "Ingredient unlock" → Today / Story / Bar / Orders
