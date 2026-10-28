# `flow_next_day`

**ประเภท:** Command · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
<<flow_next_day>>
```

## Argument
ไม่มี

## ทำอะไร
เริ่มวันใหม่ Close → BeforeOpen (เข้า Event แล้ว Story)

## ใช้ได้เมื่อ
เฉพาะ Close

## หมายเหตุ
บวก `$day` 1 (ตอนออกจาก Close) · node ของวันใหม่ (จาก `SO_DaySchedule`) **เริ่มเมื่อบทที่กำลังเล่นอยู่จบ** จึงควรเป็นคำสั่งท้ายๆ ของวัน ตัวอย่างใน `PLAYTEST_Demo.yarn` › PT_D1_Close · ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_EndDay

[← รายการทั้งหมด](README.md)
