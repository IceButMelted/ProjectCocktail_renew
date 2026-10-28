# `flow_ingredient_added`

**ประเภท:** Command · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
<<flow_ingredient_added>>
<<flow_ingredient_added Shaking>>
```

## Argument
`type` (ไม่ใส่ก็ได้): `Shaking` / `Stiring` (`Stirring`, `Mixing` ก็ได้) / `Building`

## ทำอะไร
เลือกวัตถุดิบแล้ว 2.1 → 2.2 Minigame ถ้าใส่ type จะเลือกมินิเกมก่อนแล้วค่อยไป ถ้าไม่ใส่ใช้ตัวที่เลือกไว้หรือตาม method ของ shaker

## ใช้ได้เมื่อ
เฉพาะ AddIngredient

## หมายเหตุ
ชื่อ type ผิดจะ warning และไม่เปลี่ยนตัวที่เลือก · ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_FlowSteps

[← รายการทั้งหมด](README.md)
