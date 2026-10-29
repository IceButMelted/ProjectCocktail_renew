# Bar410 — Ingredient Unlock Plan

สถานะ: **ลงมือแล้ว Stage 1-8 (2026-10-29)** ดู "ผลการทำ" ท้ายไฟล์ · เขียน 2026-10-28 · แก้ครั้งที่ 3: 2026-10-29 (คำตอบ A-D)
ADR ที่เกี่ยว: [0003](adr/0003-ingredient-unlock.md) (ทับ ADR 0001 เรื่อง "วัตถุดิบมีครบทุกวัน")

## ทิศทางที่เจ้าของเลือกแล้ว
| เรื่อง | คำตอบ |
|---|---|
| เลขวันมาจากไหน | ตัวแปร Yarn `$day` (flow บวกให้เองตอน Close → BeforeOpen ไม่ต้อง `<<set $day>>` ในบท) |
| สถานะปลดล็อคเก็บที่ไหน | รายการใน C# แยกต่างหาก + save เอง |
| วัตถุดิบที่ล็อคบนบาร์ | ซ่อนปุ่ม |
| หน้าสูตรในหนังสือ | รอตัวอย่างจากเจ้าของ รอบนี้ทำแค่ API |
| ออเดอร์สุ่ม (2026-10-29) | **(ก) กรองให้สุ่มเฉพาะสูตรที่ทำได้** |
| วัตถุดิบเริ่มต้น (2026-10-29) | ตามตารางของเจ้าของ (ด้านล่าง) |
| แจ้งผู้เล่น (2026-10-29) | **มี popup และเสียง** |
| ล็อคคืน (2026-10-29) | **ไม่มี** ปลดล็อคแล้วถาวร |
| popup ของการปลดล็อคกลางบท (A) | **รอไปขึ้นตอน Event ของวันถัดไป** |
| หน้าตา popup (B) | **popup เดียวต่อวัน** หัวข้อ `Unlock New Ingredient` เนื้อหาคือชื่อวัตถุดิบ (ไม่มีรูป) |
| popup วัน 1 (C) | **ไม่มี** |
| เสียง (D) | ยังไม่มีไฟล์ เว้นช่อง Sound ID ว่างไว้ |

## ตารางวัน (จากเจ้าของ 2026-10-29)
| Unlock Day | วัตถุดิบ (ชื่อ enum) | สูตรที่ทำได้รวม |
|---|---|---|
| 1 | `Vodka`, `Gin`, `Triplesec`, `Soda`, `CranberryJuice`, `LemonJuice`, `GrapefruitJuice`, `Syrup` | 8 |
| 3 | `Whiskey`, `DryVermouth`, `PepperMint` | 11 |
| 5 | `Rum`, `SweetVermouth`, `LimeJuice`, `OrangeJuice` | 19 |
| 7 | `Tequila`, `Campari` | 26 (ครบ) |

ตรวจกับข้อมูลสูตรจริง 26 สูตรแล้ว (2026-10-29): รายชื่อสูตรที่ทำได้ของทุกวัน **ตรงกับตารางของเจ้าของทุกตัว**
- Day 1: John Collins, White Lady, Gin Fizz, Sea Breeze, Cosmopolitan, Greyhound, Cranberry Fizz, Grapefruit Spritz
- Day 3: + Martini, Old Fashioned, Mint Julep
- Day 5: + Screwdriver, Sex On The Beach, Daiquiri, Mojito, Mai Tai, Manhattan, Orange Mint Soda, Nojito
- Day 7: + Negroni, Jungle Bird, Boulevardier, Margarita, Paloma, Rosita, Siesta

หมายเหตุ: Demo มี 5 วัน ดังนั้น Tequila / Campari (วัน 7) จะไม่ปลดล็อคใน Demo เว้นแต่เนื้อเรื่องสั่ง `<<unlock_ingredient>>`

## หลักการ
- **Single source of truth = `IngredientUnlocks`** (component ใหม่ ตัวเดียวในฉาก) ทุกคน (บาร์, หนังสือ, ออเดอร์, Yarn) ถามที่นี่ที่เดียว
- "ปลดล็อคแล้ว" **คำนวณ** ไม่ได้เก็บ:
  `IsUnlocked(x) = (row.Day > 0 && $day >= row.Day) || storyUnlocked.Contains(x)`
  - ปลดล็อคตามวัน → ไม่ต้อง save เพราะ `$day` save ไปกับ Yarn อยู่แล้ว
  - ปลดล็อคตามเนื้อเรื่อง → `HashSet<string>` ใน C# ที่ต้อง save
- **Key ของวัตถุดิบ = ชื่อ enum** (`Gin`, `LemonJuice`, …) ชื่อทั้ง 17 ตัวใน `BaseSpirit` / `Liqueur` / `Mixer` ไม่ซ้ำกัน จึงใช้เป็นชื่อใน Yarn ได้ตรงๆ
- ตารางใส่ครบ 17 แถวตามตารางวัน วัตถุดิบที่**ไม่มีแถว** = ปลดล็อคตั้งแต่ต้น (กันของใหม่ในอนาคตหายจากบาร์)

## ข้อมูล (Inspector)
`SO_IngredientUnlockTable` (ScriptableObject, ตาราง Odin) แถวละหนึ่งวัตถุดิบ:

| คอลัมน์ | ความหมาย |
|---|---|
| Ingredient | dropdown รวม 17 ชื่อ (เก็บเป็น string ชื่อ enum) |
| Unlock Day | `0` = ปลดล็อคด้วย Yarn เท่านั้น, `N` = ปลดล็อคเองเมื่อ `$day >= N` |
| Display Name | ชื่อที่ขึ้นใน popup เช่น `Lemon Juice` (ว่าง = ใช้ชื่อ enum) ย้ายไป String Table ตอนทำ Localization |

ทั้งสองทางใช้ร่วมกันได้: ตั้ง Day 5 แต่เนื้อเรื่องสั่ง `<<unlock_ingredient X>>` ก่อนวัน 5 ก็ได้

## Yarn
```
<<unlock_ingredient LemonJuice>>
<<if ingredient_unlocked("LemonJuice")>> ... <<endif>>
```
- `unlock_ingredient` เรียกซ้ำได้ไม่มีผล (silent replay ตอน load จะรันซ้ำ)
- ชื่อผิด → `Debug.LogError` ไม่เงียบ

## ออเดอร์ (คำตอบ ก)
- `OrderService.PickForCustomer` (สุ่มตามรสนิยมลูกค้า) และ `order_by_type` (`GetRandom(type)`) **กรองเหลือเฉพาะสูตรที่ `IsRecipeComplete`** ก่อนสุ่ม
- ถ้ากรองแล้วไม่เหลือเลย (เช่น ลูกค้าชอบแต่ประเภทที่ยังล็อค) → สุ่มจากสูตรที่ทำได้ทั้งหมดแทน + warning (ไม่สั่งของที่ทำไม่ได้)
- ออเดอร์ที่ writer สั่งชื่อตรงๆ (`Order_Cocktail_ByName_*`) **ไม่กรอง** (writer ตั้งใจ) แต่ขึ้น `Debug.LogWarning` ถ้าสูตรนั้นยังทำไม่ได้
- `OrderService` ไม่รู้จัก `IngredientUnlocks` โดยตรง: รับ `Func<S_Drink, bool> canMake` ตอนสร้าง (`CocktailSystemManager` ส่งให้) manager ยังไม่รู้จักกันเอง

## แจ้งผู้เล่น (คำตอบ: popup + เสียง)
- ใช้ `EventPopupQueue` ที่มีอยู่ (BeforeOpen › Event) ไม่สร้าง UI ใหม่
- `IngredientUnlocks` เก็บ `HashSet<string> notified` (แจ้งแล้ว) ตอนเข้า Event ของวัน `FlowWiring` ถาม `IngredientUnlocks.TakeNewUnlocks()` (ปลดล็อคแล้วแต่ยังไม่แจ้ง ทั้งตามวันและจากเนื้อเรื่อง แล้วจดว่าแจ้งแล้ว) ถ้ามี → `Enqueue` **ครั้งเดียว**: หัวข้อ `Unlock New Ingredient` เนื้อหาเป็น Display Name ทีละบรรทัด ไม่มีรูป แล้วค่อย `ShowAll()`
- `<<unlock_ingredient>>` กลางวัน: ปลดล็อคทันที (ปุ่มขึ้นบนบาร์ตอนเข้า AddIngredient ครั้งถัดไป) แต่ popup รอไปรวมกับ Event ของวันถัดไป
- `notified` ต้อง save ไม่งั้น load แล้ว popup ขึ้นซ้ำ
- เกมใหม่: วัตถุดิบวัน 1 ถือว่า "แจ้งแล้ว" ตั้งแต่ต้น ไม่มี popup วัน 1
- **เสียง:** เพิ่มช่อง Sound ID (UiSFX ของ `SoundManager`) ใน `EventPopupQueue` เล่นตอน popup แต่ละอันขึ้น ใช้ได้กับ popup ทุกชนิด ไม่ผูกกับระบบปลดล็อค ตอนนี้ยังไม่มีไฟล์เสียง ช่องว่าง = เงียบ

## หนังสือ (รอบนี้แค่ API)
- `IsUnlocked(string ingredient)`
- `IsRecipeComplete(S_Drink)` = วัตถุดิบทุกตัวใน Alcohol/Liqueur/Mixer list ปลดล็อคแล้ว
- กติกาที่เจ้าของกำหนด:
  - สูตรยังไม่ครบ → วัตถุดิบที่ปลดล็อคขึ้นชื่อ ที่ล็อคเป็น `???` **ไม่แสดงปริมาณเลย**
  - สูตรครบ → ขึ้นชื่อ + ปริมาณทุกตัว
- UI หนังสือรอตัวอย่างจากเจ้าของ (`BookUI_V2` ตอนนี้เป็นหน้าเขียนตายตัว)

## Stage (compile-check ทุก stage)
1. **ข้อมูล + ตัวกลาง:** `SO_IngredientUnlockTable` + asset ตามตารางวัน, `IngredientUnlocks` (scene-scoped, มี `Current`), `IsUnlocked`, `IsRecipeComplete`, อ่าน `$day` ผ่าน `YarnVariableSync`
2. **Yarn:** `<<unlock_ingredient>>`, function `ingredient_unlocked` + เอกสาร `Docs/YarnCommands/` + node ทดสอบใน `TEST_AllCommands.yarn`
3. **บาร์:** ตอนเข้า AddIngredient `FlowWiring` สั่ง refresh → `IngredientButtonGroup` ซ่อนเฉพาะปุ่ม AddAlcohol/AddLiqueur/AddMixer ที่ล็อค (ปุ่ม Ice / Method / Reset ไม่แตะ)
4. **ออเดอร์:** กรอง random ตามหัวข้อ "ออเดอร์"
5. **Popup + เสียง:** `TakeNewUnlocks` → `EventPopupQueue` ใน Event, Sound ID ใน `EventPopupQueue`
6. **Save/Load:** เพิ่ม `UnlockData` (story unlocks + notified) ใน `SaveData`, ตอน load **แทนที่** set (ไม่ merge) หมายเหตุ: Save/Load ของฉากหลักยังพังอยู่ (ดู CLAUDE.md) stage นี้แค่เขียน/อ่านข้อมูลให้ถูก ทดสอบจริงได้เมื่อ refactor save แล้ว
7. **Validate:** เพิ่มใน `Bar410 > Validate Cocktail Data`: ชื่อในตารางไม่ซ้ำ/มีจริง, รายงานจำนวนสูตรที่ทำได้ต่อวัน
8. **ทดสอบ Play:** วัน 1 (8 สูตร, ปุ่มซ่อน), ข้ามไปวัน 3 (popup 3 อัน + เสียง, ออเดอร์สุ่มไม่หลุดสูตรที่ทำไม่ได้), `<<unlock_ingredient>>` กลางบท

## คำถามที่ยังเปิด
ไม่มี (ตอบครบ 2026-10-29)

## ผลการทำ (2026-10-29)
ทุก stage compile ผ่าน (`read_console` ไม่มี error) ยังไม่ commit

| Stage | ผล |
|---|---|
| 1 ข้อมูล + ตัวกลาง | `SO_IngredientUnlockTable` (Odin table, dropdown 17 ชื่อ) + asset `Assets/[02]Script/Ingredient Unlock/IngredientUnlockTable.asset` (17 แถวตามตารางวัน, Display Name อังกฤษ) · `IngredientUnlocks` (scene-scoped, `Current`, `IsUnlocked`, `IsRecipeComplete`, story set, notified set) วางที่ `New Drag Drop System` › `[GameLoop]` › `[IngredientUnlocks]` อ่าน `$day` ผ่าน `YarnVariableSync` จาก `DialogueRunner` ที่ผูกไว้ |
| 2 Yarn | `<<unlock_ingredient X>>` (ซ้ำได้, ไม่สนตัวพิมพ์, ชื่อผิด = LogError) และ `ingredient_unlocked("X")` (ชื่อผิด = LogError + false) เอกสาร `Docs/YarnCommands/unlock_ingredient.md` / `ingredient_unlocked.md` + README + node `TEST_Unlock` ใน `TEST_AllCommands.yarn` (ลิงก์จาก `TEST_Hub`) |
| 3 บาร์ | `IngredientButtonUI.IngredientKey`, `IngredientButtonGroup.Refresh(isUnlocked)` ซ่อน/แสดงเฉพาะปุ่ม AddAlcohol/AddLiqueur/AddMixer ด้วย `SetActive` · `FlowWiring.OnAddIngredientEntered` เรียก Refresh ก่อน `SetInteractable` **หมายเหตุ:** ในฉากตอนนี้ปุ่มที่ล็อควัน 1 ถูกปิด (inactive) ไว้ก่อนแล้ว (เจ้าของทำเอง) Refresh จะเปิดให้เองเมื่อปลดล็อค |
| 4 ออเดอร์ | `OrderService` รับ `Func<S_Drink,bool> canMake` (ไม่รู้จัก `IngredientUnlocks`) ใหม่: `PickRandom()` / `PickRandom(type)` · `PickForCustomer`, `CocktailSystemManager.RandomCocktail()` / `RandomCocktail(type)` (ใช้กับ `RandomCocktailIfNoOrder`) กรองสูตรที่ทำได้ ถ้าไม่เหลือ → สุ่มจากที่ทำได้ทั้งหมด + warning · `OrderByName` ไม่กรอง แค่ warning · `FlowWiring.Awake` ส่ง `_unlocks.IsRecipeComplete` ให้ `CocktailSystemManager.UseRecipeFilter` (manager ไม่รู้จักกัน เหมือน `UsePreferences`) · `order_by_type` ไม่มีการสุ่มสูตร (ไม่มี Target) จึงไม่กรอง ใส่ warning ถ้าไม่มีสูตรประเภทนั้นที่ทำได้ |
| 5 Popup + เสียง | `FlowWiring.OnEventEntered` เรียก `TakeNewUnlocks()` ถ้ามี → `Enqueue("Unlock New Ingredient", ชื่อทีละบรรทัด)` ครั้งเดียว ก่อน `ShowAll()` · เกมใหม่: วัตถุดิบ Unlock Day = 1 ถือว่าแจ้งแล้วใน `Awake` · `EventPopupQueue` มีช่อง `Show Sound Id` (UiSFX) เว้นว่าง = เงียบ |
| 6 Save | `SaveUnlockData` (`StoryUnlocked`, `Notified`) ใน `SaveData.UnlockData` · `SaveToFile` เก็บ `IngredientUnlocks.Snapshot()` · `OnSceneLoaded` เรียก `Restore` (แทนที่ ไม่ merge) save เก่าที่ไม่มี `UnlockData` จะได้ set ว่าง → popup วัน 1 ขึ้นหนึ่งครั้งหลัง load |
| 7 Validate | `Bar410 > Validate Cocktail Data` เช็คชื่อในตารางไม่ซ้ำ/มีจริง และ log จำนวนสูตรที่ทำได้ต่อวัน: `day 1 = 8; day 3 = 11; day 5 = 19; day 7 = 26` (ตรงตาราง) ผ่าน |
| 8 Play test | ดูด้านล่าง |

### Play test (ผ่าน Unity MCP, ขับด้วยโค้ด)
- วัน 1: สูตรที่ทำได้ 8/26, เข้า AddIngredient (ผ่าน `GameFlowCommands.OpenBar` / `PrepareDrinks`) ปุ่มวัตถุดิบขึ้น 8/17 ตรงตารางวัน 1, ไม่มี popup
- สุ่ม 4,900 ครั้ง (`PickForCustomer` ทุก `CharacterName`, `PickRandom`, `PickRandom(type)`, `RandomCocktail`) ไม่หลุดสูตรที่ทำไม่ได้เลย (ได้ 8 ชื่อตรงรายชื่อวัน 1)
- `<<unlock_ingredient>>` กลางวัน: Rum ปลดล็อคทันที (ซ้ำ/ตัวพิมพ์เล็กได้), ปุ่ม Rum ขึ้นหลัง `Refresh`, ชื่อผิดขึ้น LogError
- ตั้ง `$day`=2 แล้ว `CloseBar` → `NextDay` ($day=3): Event ขึ้น popup เดียว "Unlock New Ingredient" เนื้อหา `Whiskey / Dry Vermouth / Peppermint` (Rum แจ้งไปแล้วจึงไม่ซ้ำ), ปิด popup แล้วเข้า Story, `TakeNewUnlocks` ซ้ำได้ 0, สูตรที่ทำได้ 11
- save/restore: Snapshot → JSON → Restore แทนที่ set จริง (Tequila ที่ปลดล็อคทีหลังหายไป)
- เสียง: ใส่ Sound ID ปลอมใน `EventPopupQueue` แล้ว popup ขึ้น → `SoundManager` ถูกเรียกด้วย id นั้น (ขึ้น warning ไม่พบ id) ช่องว่าง = เงียบ
- Yarn `TEST_Unlock` รันจริงใน dialogue: ค่าทุกบรรทัดตรงที่คาด, LogError 2 ครั้งสำหรับชื่อผิด, บทเดินต่อ

### ยังไม่ได้ทดสอบ
- คลิกปุ่ม/ดูภาพจริง (popup, ปุ่มวัตถุดิบที่ขึ้นหลังปลดล็อค), controller
- Save/Load ลงไฟล์จริงและ load กลับในเกม (ฉากหลักยังพังอยู่ ดู CLAUDE.md) ทดสอบแค่ Snapshot/Restore ด้วยโค้ด
- Event ที่เข้าซ้ำกลางบทด้วย `<<flow_check_events>>` จะแจ้งการปลดล็อคที่ค้างอยู่ด้วย (ตอนเข้า Event ทุกครั้ง ไม่ใช่เฉพาะต้นวัน)
- วัน 5 / วัน 7 ในเกมจริง (ทดสอบวัน 1 กับ 3 ส่วนวัน 5/7 ตรวจด้วย Validate)
- หน้าสูตรในหนังสือ (รอบนี้ทำแค่ API ตามแผน)

### Log 2026-10-29: key ของตารางเป็น enum (IngredientId)
- เหตุผล: แถวเก็บชื่อเป็นข้อความ ถ้าเปลี่ยนชื่อ enum แถวจะไม่ตรงอะไรเลย วัตถุดิบนั้นกลายเป็น "ไม่มีแถว" = ปลดล็อควันแรกเงียบๆ เจ้าของเลือกใช้ตัวเลือก enum จริง (ตารางยังเป็น ScriptableObject)
- `IngredientId` (หมวด + BaseSpirit/Liqueur/Mixer) แทน `string ingredient` ใน `Row` ส่วน runtime ยังใช้ `id.Key` (string) เหมือนเดิม
- ย้ายข้อมูล 17 แถวด้วยโค้ด ตรวจแล้ว key/วัน/ชื่อตรงเดิมทุกแถว, ลบช่องเก่าออกแล้ว
- เพิ่มปุ่ม **Add Missing Ingredients**, `ValidateInput` (ว่าง/ซ้ำ), Validate: error แถวว่าง/ซ้ำ + warning วัตถุดิบที่ไม่มีแถว
- ทดสอบ: ตารางสำเนา ลบ Gin → Validate เตือน 1 → ปุ่มเติม Gin กลับ (วัน 1, ชื่อว่าง) ลบสำเนาแล้ว; Validate จริง day 1 = 8, 3 = 11, 5 = 19, 7 = 26 ไม่มี error/warning; Play: IsUnlocked(Gin)=true, Whiskey=false, ขวด 8/17 (เมื่อไม่มี story unlock), `$day`=3 ได้ Whiskey / Dry Vermouth / Peppermint
- ยังไม่ได้ทดสอบ: หน้าตา Odin ของ `IngredientId` ในตารางจริง (ดูด้วยตา), กดปุ่มใน Inspector จริง (เรียกผ่าน reflection)
