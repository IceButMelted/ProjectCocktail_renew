# `flow_phase`

**ประเภท:** Function → string · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
{flow_phase()}
<<if flow_phase() == "Open">>
```

## Argument
ไม่มี

## ทำอะไร
Phase ของ Level 1: `BeforeOpen` / `Open` / `Close` ก่อนวันเริ่ม (ระหว่างรอ Panel_Transition) คืนค่าว่าง

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
ตอบค่าได้แม้ระหว่าง silent replay จึงใช้ใน `<<if>>` ได้

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › ทุก node

[← รายการทั้งหมด](README.md)
