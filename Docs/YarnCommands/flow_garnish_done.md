# `flow_garnish_done`

**ประเภท:** Command · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
<<flow_garnish_done>>
```

## Argument
ไม่มี

## ทำอะไร
ยืนยัน Garnish Step 3 → 4 Serve

## ใช้ได้เมื่อ
เฉพาะ Garnish

## หมายเหตุ
ปกติปุ่มใน Garnish เป็นคนเรียก · ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_FlowSteps

[← รายการทั้งหมด](README.md)
