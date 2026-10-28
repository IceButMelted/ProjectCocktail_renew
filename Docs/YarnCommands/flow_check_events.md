# `flow_check_events`

**ประเภท:** Command (รอ) · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
<<flow_check_events>>
```

## Argument
ไม่มี

## ทำอะไร
พากลับไป Event เพื่อแสดง popup ของ event ประจำวัน **บทพูดหยุดรอ** จนกว่า Event จะจบแล้วกลับมาที่ Story จากนั้นเล่นบรรทัดถัดไปต่อ ไม่เริ่มบทใหม่

## ใช้ได้เมื่อ
เฉพาะตอนอยู่ใน StoryBeforeOpen ที่อื่นจะ warning

## หมายเหตุ
popup มาจาก `EventPopupQueue` (บทรอจนผู้เล่นปิดครบ) คิวว่าง = ผ่านทันที ตอนนี้ยังไม่มีระบบไหนใส่คิว · ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Start

[← รายการทั้งหมด](README.md)
