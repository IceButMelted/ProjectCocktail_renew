# `flow_drink_complete`

**ประเภท:** Command · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
<<flow_drink_complete>>
```

## Argument
ไม่มี

## ทำอะไร
เครื่องดื่มเสร็จ ออกจาก Prepare Drinks → 3 Garnish

## ใช้ได้เมื่อ
เฉพาะ Minigame

## หมายเหตุ
ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_FlowSteps

[← รายการทั้งหมด](README.md)
