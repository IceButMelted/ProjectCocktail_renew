# R3 Owner Playtest: ชงหนึ่งแก้วใน Main_V2 (ด้วยเมาส์)

เป้าหมาย: ลองเล่นฉากใหม่ `Assets/Bar410/Content/Scenes/Main_V2.unity` ชงหนึ่งแก้วครบวงจร แล้วตอบจุดตัดสิน R3 ท้ายไฟล์
ยังไม่มี Yarn ในฉากนี้ ใช้ panel ของ `StubStory` บนจอแทน (ไม่ต้องใช้จอย จอยเป็นเฟส R6)

## 0. เตรียม
- [x] เปิดฉาก `Main_V2` กด Play (ถ้ามี Pause ซ้อนให้โหลด `PauseScene` ตามปกติ)
- [x] หลังกด Play ช่วงแรกประมาณ 7 วินาที (ใน Editor) จะยังไม่มีอะไรเกิดขึ้นบนจอ: เกมกำลัง preload ฉาก UI (`Minigame_UI`, `Garnish_UI`) ยังไม่ใช่บั๊ก รอจน panel `StubStory` ขึ้น part = Open 
- [x] Console ไม่มี error สีแดง (warning ที่รู้อยู่แล้วดูหัวข้อ "ปัญหาที่รู้" ด้านล่าง)

## 1. วนหนึ่งแก้ว
- [x] Panel `StubStory`: กด "Customer arrives" ลูกค้าเข้าร้านและออเดอร์ขึ้น
- [x] Hub (ปุ่มบนจอ) โชว์เฉพาะที่ควรโชว์ใน Phase Talking
- [x] วันที่ 1 บนบาร์มีเฉพาะขวดที่ปลดล็อคแล้ว (ขวดที่ยังล็อคต้องไม่โผล่) และออเดอร์สุ่มเป็นสูตรที่ทำได้จากขวดที่มี
- [x] เข้า AddIngredient: เลือกวัตถุดิบด้วยเมาส์ได้ หมุนกล้องด้วยเมาส์ได้เฉพาะตอน shaker ยังไม่เต็ม
- [x] วัตถุดิบครบ 10 หน่วย เข้า Minigame (Shaking/Stirring) เล่นได้ด้วยเมาส์
- [x] ภาพลูกค้าข้างจอตอนมินิเกม (inset) เห็นถูกคน กล้องอยู่ด้านหน้าลูกค้า (offset (0, 0.6, -2.2) เท่าฉากเก่า) กรอบภาพโอเค ไม่เห็นเพื่อนข้างๆ เกินไป
- [x] จบมินิเกม เข้า Garnish: เห็นแก้วผ่าน RenderTexture วาง Garnish Item ได้ (ลง slot 0 เสมอ: `Granish_Topping_1/2` ปิดอยู่ใน glass prefab มาตั้งแต่ฉากเก่า จึงคลิกเลือก slot บนแก้วไม่ได้)
- [x] Serve: เสิร์ฟแล้วได้คะแนนครั้งเดียว
- [ยังไม่เห็น Reaction แต่เห็นจาก Log ของ Sub panel แล้วว่ารุ้สึกอย่างไร] Reaction ของลูกค้าขึ้นแล้วกลับไป Talking
- [x] Back: กด Back ในแต่ละ Phase ที่มีปุ่มแล้วย้อนถูกที่ ไม่ค้าง
- [x] Reset (ปุ่ม Reset ของ shaker) ใช้ได้
- [x] Remake / Cancel มินิเกม ไม่ให้คะแนน และไม่ค้าง
- [x] ปุ่ม Hub ในแต่ละ Phase แสดง/ซ่อนถูกต้อง ปุ่มที่ซ่อนกดไม่ได้
- [x] Pause ด้วยปุ่ม Setting บนจอ (UI) เกมหยุด กลับมาเล่นต่อได้ ไม่ค้าง
- [x] Pause ระหว่าง Minigame และระหว่าง Garnish: หน้า Pause อยู่บนสุด ไม่ถูก Canvas ของ Minigame/Garnish บัง
- [x] จบแก้วแล้วกด "Close bar" แล้ว "Next day" ใน panel `StubStory`: วันถัดไปเริ่ม (Event → Story → Open) ไม่ค้าง (หมายเหตุ: วันของ StubStory ไม่ใช่ `$day` ของ Yarn ขวดที่ปลดล็อคจึงยังเหมือนวันที่ 1)

## 2. ภาพและชั้น Canvas
- [x] ลำดับ Canvas ถูก: Hub / Minigame / Garnish / popup ไม่บังกันผิด
- [x] ภาพ RenderTexture ของ Minigame / Garnish ไม่เบลอ ไม่ยืด ไม่มีขอบดำผิดปกติ
- [x] ตอนสลับ Phase ไม่มีภาพกระพริบ / กล้องกระโดด
- [x] เทียบกับฉากเก่า `New Drag Drop System` แล้วหน้าตาใกล้เคียง (จดข้อต่างด้านล่าง)

บันทึกข้อสังเกต: ______________________________________________

## 3. ปัญหาที่รู้อยู่แล้ว (ไม่ต้องรายงานซ้ำ)
- Preload ฉาก UI ทำให้วันเริ่มช้ากว่าฉากเก่า (ราว 6 วินาทีใน Editor) ไม่ได้ซ่อนหลัง `Panel_Transition` (1.33 วินาที)
- warning missing script บน `NPC_Walter` / `NPC_Owen` / `NPC_Isla` (สืบทอดจาก prefab เก่า)
- warning Event System ซ้ำ 2 ตัวตอน `PauseScene` โหลด
- warning ของ `SoundManager` เรื่อง `Book_Close`
- ตัวเลข fps วัดใน Editor เท่านั้น: Main_V2 Talking 56-60, AddIngredient 53-58, Minigame 41-42, Garnish/Serve 32-33 (ฉากเก่าวัดแบบเดียวกัน: 33-38, 31-34, 30-31, 29-30) ยังไม่ได้วัดใน build จึงยังตัดสินเกณฑ์ 60 fps ไม่ได้ (`G3-T12_Measurements.md`)
- slot ตกแต่ง `Granish_Topping_1/2` ปิดอยู่ใน glass prefab จึงคลิก slot ไม่ได้ Garnish Item ลง slot 0 เสมอ (มีมาตั้งแต่ฉากเก่า)

## 4. จุดตัดสิน R3 (Execution Plan §6 R3) ตอบตรงนี้
1. ผ่านเกณฑ์ >= 60 fps / สะดุดตอนเข้า Phase ครั้งแรก <= 300 ms และคลิกผ่าน RenderTexture ได้ไหม
   (วัดแล้ว: hitch 50-66 ms ผ่าน, คลิกผ่าน RT ผ่าน, fps ต่ำกว่า 60 ใน Editor ยังไม่รู้ผลใน build)
   คำตอบ: ผมให้ผ่านเพราะที่ใช่อยู่ตอนนี้เป็น Spec Laptop ค่อนข้างเก่าคือ  i7600hq + GTX960m
2. เพิ่ม Station หรือ verb ใหม่ง่ายกว่าเดิมจริงไหม (ถ้าไม่ ย้อนไปแผนระดับ A)
   คำตอบ: จริง
3. จะคงฉาก UI แยก (`Minigame_UI`, `Garnish_UI`) หรือให้ UI อยู่ฉากหลัก (เรื่อง preload ช้าด้วย: เริ่มคู่กับ transition หรือโหลดใน `LoadingScene`)
   คำตอบ: ui แยกแบบนี้ดีแล้ว

## 5. คำถามเปิดของเจ้าของ
- **D6 ที่นั่งของแก้ว:** `GarnishUiHost` หา seat ได้แล้ว แต่ยังไม่แสดงอะไรที่ `GarnishGlassView1/2/3` ต้องตัดสิน: แก้วเดียวกันหรือสำเนา, ค้างนานแค่ไหน (`ClearGlass` ทำงานทันทีหลังให้คะแนน), Remake ทำอย่างไร
  คำตอบ: เป็นสำเนา ค้างนานจนกว่าตัวละครจะหายไปหรือ Writer เขียนให้หายไป
- **`Granish_Topping_1/2` ปิดอยู่ใน `BaseGlass.prefab`** (และ variant) อนุญาตให้เปิดไหม (แก้ glass prefab)
  คำตอบ: อนุญาติให้เปิด
- **OpenBook บนจอย:** ไม่มี binding (Triangle = OpenLog เหลือแค่ `<Gamepad>/select`) จะใช้ปุ่มไหน
  คำตอบ: ใช้ R2
