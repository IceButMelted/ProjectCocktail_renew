# `flow_minigame`

**ประเภท:** Command · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
<<flow_minigame Shaking>>
```

## Argument
`type`: เหมือน `flow_ingredient_added`

## ทำอะไร
เลือกมินิเกมของการเข้า 2.2 ครั้งถัดไป **ไม่เปลี่ยน step**

## ใช้ได้เมื่อ
ได้ทุกเมื่อ (มีผลตอนเข้า 2.2)

## หมายเหตุ
อ่านค่ากลับด้วย `flow_minigame_type()` · ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_FlowSteps

[← รายการทั้งหมด](README.md)
