# `flow_remake_drink`

**ประเภท:** Command · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
<<flow_remake_drink>>
```

## Argument
ไม่มี

## ทำอะไร
ทิ้งเครื่องดื่มแล้วชงใหม่ กลับไป 2.1 AddIngredient ไม่ให้คะแนน

## ใช้ได้เมื่อ
เฉพาะ Garnish หรือ Serve ที่อื่นจะ warning

## หมายเหตุ
ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_FlowSteps

[← รายการทั้งหมด](README.md)
