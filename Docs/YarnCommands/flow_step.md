# `flow_step`

**ประเภท:** Function → string · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
{flow_step()}
```

## Argument
ไม่มี

## ทำอะไร
step ย่อย: ใน BeforeOpen คืน `Event` / `StoryBeforeOpen` ใน Open คืน `TalkingWithCustomer` / `AddIngredient` / `Minigame` / `Garnish` / `Serve` นอกนั้นคืนค่าว่าง

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › ทุก node

[← รายการทั้งหมด](README.md)
