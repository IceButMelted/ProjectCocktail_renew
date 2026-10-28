# `flow_close_bar`

**ประเภท:** Command · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
<<flow_close_bar>>
```

## Argument
ไม่มี

## ทำอะไร
จบการขายของวัน Talking → ออกจาก Open → Close

## ใช้ได้เมื่อ
เฉพาะ Talking

## หมายเหตุ
Close ยังไม่มี UI · ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_EndDay

[← รายการทั้งหมด](README.md)
