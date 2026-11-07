# G4-I0 — ใบตอบผลทดสอบจอย

วิธีทดสอบอยู่ที่ `Docs/rewrite/Spike_G4-I0_Howto.md` เขียนคำตอบใต้ `**ตอบ:**` แล้วบอกผมว่า "ตอบแล้ว"
ผ่าน = `[x]`, ไม่ผ่าน = เขียนอาการ, ทดสอบไม่ได้ = เขียนว่าทำไม

## 0. อุปกรณ์ที่ใช้

- จอยที่ 1 (ชื่อ/รุ่น): 
- ต่อด้วย (สาย / บลูทูธ / ตัวรับ): 
- จอยที่ 2 (ถ้ามี): 
- ระบบ: Linux / Windows (Game view ขนาด): 

## 1. ผลต่อข้อ (จอยที่ 1)

| # | ทดสอบ | ผ่าน | อาการ / ค่าที่เห็น |
|---|---|---|---|
| 1 | stick ซ้ายขยับเคอร์เซอร์ลื่น | [x] | |
| 2 | ปล่อย stick แล้วเคอร์เซอร์นิ่ง (ไม่ลอย) | [x] | ค่า `LS raw` ตอนปล่อย: · `Max raw LS while cursor still`: |
| 3 | กดปุ่ม A/X บน Button A/B/C ตัวเลขเพิ่มทีละ 1 | [x] | |
| 4 | คลิกกล่อง 3D ผ่านภาพ RenderTexture ได้ | [x] | |
| 5 | ตอน Pause (`timeScale: 0`) เคอร์เซอร์ขยับ + กดปุ่ม + คลิกกล่องได้ | [ ] | |
| 6 | ขยับเมาส์จริง → เคอร์เซอร์เหลืองหาย ลูกศรระบบกลับมา | [x] | |
| 7 | แตะจอยอีกครั้ง → เคอร์เซอร์เหลืองกลับ ลูกศรระบบหาย | [x] | |
| 8 | stick ขวาเลื่อนรายการ `Row 01..20` | [x] แต่เลื่อนเร็วมาก | |
| 9 | `Last device` ขึ้นชื่อถูกต้อง | [x] | ที่ขึ้น: game pad (เมื่อใช้ PS)|

**ถ้าเคอร์เซอร์ลอย:** ต้องเลื่อน `Cursor dead zone` ถึงเท่าไรจึงนิ่ง: 
**ความเร็วเคอร์เซอร์ที่ชอบ (`Cursor speed`):** 

## 2. จอยที่ 2 (ถ้ามี)
ผลต่างจากจอยที่ 1 ตรงไหน (เช่น `Last device` ขึ้น Other / ปุ่ม A-X ไม่ตรง): 

## 3. HUD ที่ copy มา
กด **C** หรือ Select แล้ววางตรงนี้ (จอยละ 1 ครั้ง):

```
G4-I0 HUD  (Unity 6000.3.19f1, Input System 1.19.0)
Last device: Other
  PS4 Controller (layout Gamepad, name Gamepad)
Hardware cursor visible: False   Software cursor: True
Mouse.current: Mouse   ignored VirtualMouse events: 0
Virtual cursor: (529, 642)   screen 1920x1080
Pad: PS4 Controller
LS raw (+0.004, -0.004) |0.006|  -> cursor (+0.00, +0.00)
RS raw (-0.027, +0.004) |0.028|  -> (+0.00, +0.00)
Dead zone: 0.15 on cursor (after control default 0.125)
Max raw LS while cursor still (drift swallowed): 0.240
Buttons pressed: -
timeScale: 1
Right stick: RS HOLD released after 265 ms -> back to Forward
Clicks: A 6  B 8  C 6

Checklist (automatic):
[PASS] Pad detected (last device switched to pad)
[PASS] Cursor moves by left stick
[ -- ] Cursor moves while timeScale = 0
[PASS] UI button clicked by virtual cursor
[ -- ] UI button clicked by virtual cursor while timeScale = 0
[PASS] RT cube clicked by virtual cursor (through RawImage)
[PASS] List scrolled by right stick
[PASS] Device switched back to mouse after pad

Log:
 111.68s Button B clicked by VirtualMouse at timeScale 1
 111.84s Button B clicked by VirtualMouse at timeScale 1
 112.01s Button B clicked by VirtualMouse at timeScale 1
 112.35s Button A clicked by VirtualMouse at timeScale 1
 112.53s Button A clicked by VirtualMouse at timeScale 1
 112.69s Button A clicked by VirtualMouse at timeScale 1
 112.88s Button B clicked by VirtualMouse at timeScale 1
 114.07s Button C clicked by VirtualMouse at timeScale 1
 114.41s HUD copied to clipboard + Console
 117.06s HUD copied to clipboard + Console
 117.89s HUD copied to clipboard + Console
 118.09s HUD copied to clipboard + Console

```

## 4. คำถามที่ผมต้องการให้ตัดสิน

**Q1. stick ขวา = เปลี่ยนมุมกล้อง (ใช้ตัดสิน D8)**
ลองทั้ง 2 แบบแล้ว ถนัดแบบไหน
- [ ] ก. **แตะ = ไปมุมกล้องถัดไป** (ค่าเริ่มต้น) · [ ] ข. **ค้าง = แอบมอง** ปล่อยกลับ · [x] ค. ทั้งสองแบบ (แตะเปลี่ยนมุม ค้างแอบมอง) · [ ] ง. ไม่ชอบทั้งคู่ เสนอ: 

ตัวแยกแตะ/ค้างทำงานถูกไหม (แตะสั้นขึ้น TAP, ค้างนานขึ้น HOLD): 

**Q2. เคอร์เซอร์เสมือนพอใช้ไหม**
- [x] ใช้ได้ ทำต่อตามแผน (virtual cursor ทุกหน้าจอ)
- [ ] ใช้ได้แต่ไม่ถนัด · เหตุผล: 
- [ ] ไม่ไหว ต้องออกแบบเป็นแบบเลือกปุ่ม (focus navigation) เป็นหลักแทน · เหตุผล: 

**Q3. เกมนี้ส่วนไหนที่อยากให้ใช้ focus navigation (D-pad เลื่อนปุ่ม) แทนเคอร์เซอร์**
(เช่น เมนู, Pause, เลือกคำตอบในบท, ปุ่ม Hub) เลือกได้หลายข้อ:
- [x] เมนูหลัก · [x] Pause/Settings · [x] ตัวเลือกในบทสนทนา · [x] ปุ่ม Hub · [ ] มินิเกม · [x] Garnish · [x] เลือกวัตถุดิบ · [ ] ไม่ต้อง ใช้เคอร์เซอร์ทั้งหมด
 
 แต่ทั้งหมดนั้นยังรองรับการใช้จอยในการ back และอื่นๆ

**Q4. ปุ่มที่ไม่ถนัด / อยากเปลี่ยน (ตอบ B2 ใหม่ได้)**
ค่าเริ่มต้น: Advance = South · Back = East · Pause = Start · AutoPlay = Y · Skip = LB · Log = RB · มินิเกมเขย่า = South หรือ RT กดรัว

| ฟังก์ชัน | ปุ่มที่อยากได้ (ถ้าต่างจากค่าเริ่มต้น) |
|---|---|
| Advance | สี่เหลี่ยม + L1 |
| Back | วงกลม|
| Pause | option|
| AutoPlay | สีเหลี่ยม|
| Skip | สีเหลี่ยม + R1|
| Log | สามเหลี่ยม|
| มินิเกมเขย่า (กดรัว หรือ analog) | กดรัวๆ และ เขย่าจอยหากทำได้ mark ว่าเป็น ffeature ที่ไม่าน่าจะมีแต่ใส่มาให้ตลก |

**Q5. ที่อื่นที่ผมควรรู้ (ลักษณะ latency, ปุ่มบนจอยที่เสีย, ปัญหาเฉพาะ Linux ฯลฯ)**


## 5. สิ่งที่ผมจะทำเมื่อได้คำตอบ
- ผ่านทั้งหมด → ปลด "provisional" ของ I1–I3, I8, `DialogueAdvanceInput` และตาราง pointer mode ใน `Contracts_Final.md` · ลบ `Assets/Bar410/Spikes/G4I0/` · ปิด R1
- ไม่ผ่านบางข้อ → ผมแก้สเปกส่วนนั้น (ใช้ Opus) ก่อนปิด R1 ไม่ข้ามไป R2
- ผมจะไม่ลบฉาก spike จนกว่าคุณบอก
