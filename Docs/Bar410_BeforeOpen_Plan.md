# Bar410 — BeforeOpen Level 2 Plan

สถานะ: **ร่าง รอเจ้าของอนุมัติ** (ยังไม่มีโค้ด) · 2026-10-28
เกี่ยวกับ: [Bar410_IngredientUnlock_Plan.md](Bar410_IngredientUnlock_Plan.md) (popup ใน Event มาจากระบบนี้)

## ที่ตกลงแล้ว
| เรื่อง | คำตอบ |
|---|---|
| Level 2 ใต้ BeforeOpen | `Event` → `StoryBeforeOpen` |
| Event | เช็ค event ประจำวัน (ตอนนี้มีแค่ปลดล็อควัตถุดิบ) และ**มี popup ให้กดปิด** |
| Story | บทสั้นก่อนเปิดร้าน ไป Event ได้ (แล้วกลับมาเล่นต่อ) หรือออกไป Open |
| `$day` | ตัวแปร Yarn, **C# บวก 1 ตอน Close → BeforeOpen** |
| node ของวัน + ชื่อ Chapter | ตารางเดียวใน Inspector: วัน / node / ชื่อ Chapter |
| UI บอก Chapter/วัน | ขึ้นตอนเข้า Open |
| Save/Load | **พักไว้** ดู CLAUDE.md "สิ่งที่ต้องแก้" แผนนี้ไม่แตะ |

## โครง
```
BeforeOpen (L1)                        เข้า: SetInitial(Event)
 ├─ Event          เข้า: ขอคิว popup ใหม่ → คิวว่างไป Story ทันที / ปิดอันสุดท้าย → Story
 └─ StoryBeforeOpen
                   เข้าครั้งแรกของวัน: StartDialogue(node ของ $day)
                   เข้าจาก Event รอบหลัง: ไม่เริ่มบทใหม่ บทเดินต่อเอง
                   <<flow_check_events>> → Event (คำสั่งรอจน popup ปิดหมด)
                   ปุ่ม / <<flow_open_bar>> → ออกไป Open
Close → BeforeOpen : FlowWiring บวก $day ผ่าน YarnVariableSync
Open.Entered       : แสดง UI Chapter/วัน
```
state ยังเป็น C# ล้วนตามกติกาเดิม งานกับ Unity (popup, StartDialogue, UI) อยู่ที่ `FlowWiring`

## ข้อมูล (Inspector)
`SO_DaySchedule` ตาราง Odin แถวละหนึ่งวัน:

| Day | Start Node | Chapter Name |
|---|---|---|
| 1 | `D1_NL_00_Start` | (ชื่อ Chapter) |

- ไม่มีแถวของ `$day` → log error และไม่เริ่มบท (ไม่เดา node)
- Validate: วันไม่ซ้ำ, node มีอยู่จริงใน Yarn project

## Popup ของ Event
- Event ไม่รู้ว่า popup มีอะไร มันแค่ถามว่ามีของใหม่ไหม แล้วรอจนกว่าจะปิด
- ระหว่างรอระบบปลดล็อค (แผนปลดล็อค Stage 1–3) คิวจะว่างตลอด Event จึงผ่านไปทันที
- popup เป็น System UI ของตัวเอง ไม่อยู่ในตาราง `HubManager`

## Stage (compile-check ทุก stage)
1. **FSM:** sub-machine ใน `BeforeOpenPhase` (`Event`, `StoryBeforeOpen`) + event ออก; `GameFlowCommands.OpenBar` ใช้ได้เฉพาะตอนอยู่ใน Story; `<<flow_check_events>>`; `flow_step` ตอบ sub-state ของ BeforeOpen
2. **ตารางวัน + เริ่มบท:** `SO_DaySchedule`; ปิด auto start ของ `DialogueRunner` ในฉากหลัก; Story เข้าครั้งแรก → `StartDialogue`; `<<declare $day = 1>>`
3. **Close → BeforeOpen:** บวก `$day`; ทดสอบผ่าน `GameFlowDebugHotkeys` (Close ยังไม่มีทางเข้า)
4. **UI Chapter/วัน:** ตอน Open.Entered แสดงชื่อจากตาราง (fade เข้า-ออก)
5. **Popup:** UI popup + คิว; Event รอปิด; ต่อกับระบบปลดล็อคเมื่อแผนปลดล็อคถึง Stage 3
6. **ทดสอบ Play:** วัน 1 ครบรอบ, `<<flow_check_events>>` กลางบท, ข้ามวันด้วย hotkey

## คำถามที่ยังเปิด
1. UI Chapter/วัน: หน้าตาจะใช้ของที่มีอยู่แล้วไหม หรือผมทำแบบง่ายๆ (ข้อความกลางจอ fade) ไว้ก่อน
2. ชื่อ Chapter ต้องรองรับหลายภาษาไหม (หน้า Setting มีแท็บ Language)
