# Yarn Commands

คำสั่ง (`<<...>>`) และฟังก์ชัน (`{...()}`) ของ Yarn ทั้งหมดในโปรเจค หนึ่งไฟล์ต่อหนึ่งคำสั่ง

**ตัวอย่างการเขียนวัน + Play test ครบรอบ:** `Assets/[06]Dialogue/YarnScript/Demo/PLAYTEST_Demo.yarn` เกมสั้น 3 วัน วันละ 3 คน (วัน 1 Isla / Cole / Faye, วัน 2 Walter / Owen / Claire, วัน 3 Eliana / Arthur / Isla) ใช้ทุกคำสั่งในหน้านี้ ยกเว้น `save_chapter_node` รวมถึงปลดล็อควัตถุดิบ (Rum จากเนื้อเรื่องวัน 2, popup วัน 3) หัวไฟล์บอกว่าแต่ละวันทดสอบอะไร และอธิบายโครงของหนึ่งวัน ใช้เป็นแม่แบบตอนเขียนวันใหม่ วิธีรัน: Day Schedule ที่ `[GameLoop]` › `FlowWiring` ต้องมีแถววัน 1-3 ที่ Start Node เป็น `PT_D1_Start` / `PT_D2_Start` / `PT_D3_Start` (ทั้ง `DaySchedule_Demo` และ `DaySchedule_PlayTest` ตั้งไว้แล้ว 2026-10-29) แล้ว Play ตอนชงเครื่องดื่มมีตัวเลือก "(Test) Let Yarn walk the steps" ให้ Yarn เดินทุก step แทนการชงเอง (ได้ Fail)

**เดโมจริง 3 วัน:** `Assets/[06]Dialogue/YarnScript/Demo/DEMO_3Days.yarn` (เดิมคือ `Day1_Demo.yarn` + วัน 2-3 จากบทของทีม) ใช้โครงเดียวกับ playtest Start Node `D1_00_Start` / `D2_00_Start` / `D3_00_Start` อยู่ใน `DaySchedule_Demo.asset` (ตารางที่ `FlowWiring` ใช้ตอนนี้) ถ้าจะรัน playtest ให้เปลี่ยน Day Schedule ของ `FlowWiring` เป็น `DaySchedule_PlayTest`

**อัดวิดีโอ:** `Assets/[06]Dialogue/YarnScript/Demo/VDO_Rec.yarn` เล่นเองทั้งหมด ตัวละคร 7 ตัวเข้าที่จุดเดียวกับเดโม 3 วัน กล้องโฟกัส แล้วโชว์ Neutral → สั่ง + จำลองชง → Happy → Sad จังหวะละประมาณ 5 วิ วิธีรัน: ที่ `[GameLoop]` › `FlowWiring` เปลี่ยน Day Schedule เป็น `DaySchedule_VDO` แล้ว Play และเปิด AutoPlay

**ทดสอบทีละคำสั่ง:** `Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn`
เปิด `Assets/[02]Script/Day Schedule/DaySchedule_Demo.asset` แล้วเปลี่ยน Start Node ของแถว Day 1 เป็น `TEST_Start` แล้ว Play (Start Node ของ `DialogueRunner` ไม่ถูกใช้แล้วตั้งแต่มีตารางวัน) เลือกหัวข้อจากเมนู Hub ทุกบรรทัดขึ้นต้นด้วย `(TEST)` และบอกค่าที่ควรเห็น เปลี่ยนกลับเป็น `D1_NL_00_Start` เมื่อเสร็จ

**คำศัพท์:**
- *instance* = ต้องใส่ชื่อ GameObject เป็นคำแรกให้ถูก และ GameObject ต้อง active ถ้าหาไม่เจอ Yarn จะขึ้น error และ**บทพูดค้าง** (ทดสอบแล้ว)
- *รอ* = บทพูดหยุดจนคำสั่งทำเสร็จ
- คำสั่งที่บอกว่า "คำแรกไม่ถูกใช้" เป็น static command ใส่ชื่ออะไรก็ได้ (ของเดิมใส่ `SystemGame` / `CamController`)

## Game flow (`GameFlowCommands`)

| ชื่อ | ประเภท | วิธีเขียน |
|---|---|---|
| [`flow_open_bar`](flow_open_bar.md) | Command | `<<flow_open_bar>>` |
| [`flow_check_events`](flow_check_events.md) | Command (รอ) | `<<flow_check_events>>` |
| [`flow_prepare_drinks`](flow_prepare_drinks.md) | Command | `<<flow_prepare_drinks>>` |
| [`flow_close_bar`](flow_close_bar.md) | Command | `<<flow_close_bar>>` |
| [`flow_next_day`](flow_next_day.md) | Command | `<<flow_next_day>>` |
| [`flow_garnish_done`](flow_garnish_done.md) | Command | `<<flow_garnish_done>>` |
| [`flow_serve_done`](flow_serve_done.md) | Command | `<<flow_serve_done>>` |
| [`flow_remake_drink`](flow_remake_drink.md) | Command | `<<flow_remake_drink>>` |
| [`flow_ingredient_added`](flow_ingredient_added.md) | Command | `<<flow_ingredient_added>>` |
| [`flow_minigame`](flow_minigame.md) | Command | `<<flow_minigame Shaking>>` |
| [`flow_another_ingredient`](flow_another_ingredient.md) | Command | `<<flow_another_ingredient>>` |
| [`flow_drink_complete`](flow_drink_complete.md) | Command | `<<flow_drink_complete>>` |
| [`flow_phase`](flow_phase.md) | Function → string | `{flow_phase()}` |
| [`flow_step`](flow_step.md) | Function → string | `{flow_step()}` |
| [`flow_minigame_type`](flow_minigame_type.md) | Function → string | `{flow_minigame_type()}` |
| [`flow_minigame_result`](flow_minigame_result.md) | Function → string | `{flow_minigame_result()}` |

## ออเดอร์และงานชงเครื่องดื่ม (`CocktailSystemManager`)

| ชื่อ | ประเภท | วิธีเขียน |
|---|---|---|
| [`Order_Cocktail_ByName_OutName`](Order_Cocktail_ByName_OutName.md) | Function → string (วางออเดอร์) | `{Order_Cocktail_ByName_OutName("Gin Fizz")}` |
| [`Order_Cocktail_ByName_OutDescription`](Order_Cocktail_ByName_OutDescription.md) | Function → string (วางออเดอร์) | `{Order_Cocktail_ByName_OutDescription("Gin Fizz")}` |
| [`Order_Cocktail_OutName`](Order_Cocktail_OutName.md) | Function → string (วางออเดอร์) | `{Order_Cocktail_OutName(5)}` |
| [`Order_Cocktail_OutDescription`](Order_Cocktail_OutDescription.md) | Function → string (วางออเดอร์) | `{Order_Cocktail_OutDescription(5)}` |
| [`order_by_type`](order_by_type.md) | Command (instance) | `<<order_by_type "[CocktailSystem]" HighAlcohol>>` |
| [`order_customer`](order_customer.md) | Command (instance) | `<<order_customer "[CocktailSystem]" Isla>>` |
| [`order_name`](order_name.md) | Function → string | `{order_name()}` |
| [`order_flavor`](order_flavor.md) | Function → string | `{order_flavor()}` |
| [`order_type`](order_type.md) | Function → string | `{order_type()}` |
| [`order_satisfaction`](order_satisfaction.md) | Function → string | `{order_satisfaction()}` |
| [`wait_for_task`](wait_for_task.md) | Command (รอ) | `<<wait_for_task SystemGame>>` |
| [`Reset_Variable`](Reset_Variable.md) | Command | `<<Reset_Variable SystemGame>>` |
| [`Can_End_Shift`](Can_End_Shift.md) | Command | `<<Can_End_Shift SystemGame>>` |
| [`wait_scene`](wait_scene.md) | Command (รอ) | `<<wait_scene 1>>` |

## กล้อง (`CameraManager`)

| ชื่อ | ประเภท | วิธีเขียน |
|---|---|---|
| [`Switch_Camera`](Switch_Camera.md) | Command | `<<Switch_Camera CamController Char2Cam>>` |
| [`Cut_Camera`](Cut_Camera.md) | Command | `<<Cut_Camera CamController MainCam>>` |

## หลังบ้านทำให้เอง (ไม่ต้องเขียน)

กล้องเริ่มต้นและการล็อคบาร์เป็นงานของ `FlowWiring` ตาม Phase writer ใช้แค่ `Switch_Camera` / `Cut_Camera` กำกับภาพ

| ช่วง | หลังบ้านทำ |
|---|---|
| เริ่มวัน (BeforeOpen) | ตัดไป MainCam + fix กล้อง + ล็อคบาร์ |
| 1 Talking | fix กล้อง + ล็อควัตถุดิบ (ช็อตที่ writer ตั้งไว้ไม่ถูกแตะ) |
| 2.1 เลือกวัตถุดิบ | MainCam + หมุนกล้องด้วยเมาส์ได้ + ปลดล็อควัตถุดิบ (เฉพาะขณะ shaker ยังไม่เต็ม) |
| shaker เต็ม / มินิเกม 2.2 | PrepareCam + fix |
| 3 Garnish | GarnishCam + fix + ล็อควัตถุดิบ |
| เสิร์ฟเสร็จ | กลับ MainCam + ล็อควัตถุดิบ |

## ลูกค้า (`TalkToCustomerManager`)

| ชื่อ | ประเภท | วิธีเขียน |
|---|---|---|
| [`CharacterExpression`](CharacterExpression.md) | Command | `<<CharacterExpression CharacterName.Isla Sad>>` |
| [`CharacterLook`](CharacterLook.md) | Command | `<<CharacterLook CharacterName.Isla L>>` |
| [`CharacterEnter`](CharacterEnter.md) | Command (รอ) | `<<CharacterEnter CharacterName.Isla 2>>` |
| [`CharacterExit`](CharacterExit.md) | Command (รอ) | `<<CharacterExit CharacterName.Isla>>` |
| [`<<Isla ...>>` แบบสั้น](CharacterShortForm.md) | Command (`Enter` / `Exit` รอ) | `<<Isla Sad>>` `<<Isla 1>>` `<<Isla Look L>>` `<<Isla Enter 2>>` `<<Isla Exit>>` |

## วัตถุดิบ (`IngredientUnlocks`)

| ชื่อ | ประเภท | วิธีเขียน |
|---|---|---|
| [`unlock_ingredient`](unlock_ingredient.md) | Command (static) | `<<unlock_ingredient LemonJuice>>` |
| [`ingredient_unlocked`](ingredient_unlocked.md) | Function → bool | `ingredient_unlocked("LemonJuice")` |

## UI / transition / ระบบอื่น

| ชื่อ | ประเภท | วิธีเขียน |
|---|---|---|
| [`Panel_FadeIn`](Panel_FadeIn.md) | Command (instance, รอ) | `<<Panel_FadeIn Panel_Transition>>` |
| [`Panel_FadeOut`](Panel_FadeOut.md) | Command (instance, รอ) | `<<Panel_FadeOut Panel_Transition>>` |
| [`Fade_To_Summmary`](Fade_To_Summmary.md) | Command (instance, รอ) | `<<Fade_To_Summmary Panel_Transition>>` |
| [`DisplayThx`](DisplayThx.md) | Command | `<<DisplayThx>>` |
| [`eventlog_clear`](eventlog_clear.md) | Command | `<<eventlog_clear>>` |
| [`save_chapter_node`](save_chapter_node.md) | Command (instance) | `<<save_chapter_node "[SaveLoadSystem]">>` |

คำสั่งที่ถูก comment ไว้ใน `Assets/[06]Dialogue/[00]Testing Yarn And Learning/TaskManager.cs` เป็นของเก่า ไม่ได้ใช้แล้ว ไม่อยู่ในรายการนี้
## ผลทดสอบล่าสุด (2026-10-28, Play mode ผ่าน Unity MCP)
- ผ่าน: ทุก node ใน `TEST_AllCommands.yarn` ยกเว้นข้างล่าง ค่าที่ได้ตรงกับที่บรรทัด `(TEST)` บอกทุกบรรทัด และคำสั่งที่เรียกผิดที่ (`flow_open_bar` ซ้ำ, `flow_remake_drink` / `flow_check_events` จาก Talking) แค่ warning
- ยังไม่ได้ทดสอบ: `TEST_RealOrder` (`wait_for_task` ต้องชงเครื่องดื่มจริง) และ `save_chapter_node` (ใช้ในฉากหลักไม่ได้ ดูไฟล์ของคำสั่ง)
- ทดสอบโดยเดินบทและเลือกเมนูด้วยโค้ด ไม่ได้คลิกจริง จึงไม่ได้ดูภาพว่ากล้อง/NPC/transition ขยับถูกต้องหรือไม่ ตรวจแค่ว่าคำสั่งทำงานจบและบทเดินต่อ
