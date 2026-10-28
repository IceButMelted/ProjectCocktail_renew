# `flow_serve_done`

**ประเภท:** Command · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
<<flow_serve_done>>
```

## Argument
ไม่มี

## ทำอะไร
ส่งเครื่องดื่ม Step 4 → 1 Talking **ให้คะแนนตอนนี้** (ที่เดียวในเกม)

## ใช้ได้เมื่อ
เฉพาะ Serve

## หมายเหตุ
ปกติปุ่ม Serve เป็นคนเรียก · ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_FlowSteps

[← รายการทั้งหมด](README.md)
