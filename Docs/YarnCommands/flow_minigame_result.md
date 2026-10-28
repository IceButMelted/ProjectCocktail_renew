# `flow_minigame_result`

**ประเภท:** Function → string · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
{flow_minigame_result()}
```

## Argument
ไม่มี

## ทำอะไร
ผลของมินิเกมรอบล่าสุด: `Completed` / `Cancelled` หรือค่าว่างถ้ายังไม่เคยเล่น

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
มินิเกมแพ้ไม่ได้ จึงมีแค่สองค่านี้

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_FlowSteps

[← รายการทั้งหมด](README.md)
