# `SetEmotion`

**ประเภท:** Command · **กลุ่ม:** ลูกค้า (`TalkToCustomerManager`)

## วิธีเขียน
```
<<SetEmotion NPC_Name.Isla "happy">>
```

## Argument
`npc`: `NPC_Name.X` · `emotion`: ชื่ออารมณ์ เช่น `happy`, `neutral`, `upset`

## ทำอะไร
เปลี่ยนอารมณ์/สีหน้าของ NPC

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
ชื่อ NPC ผิดจะ warning

## โค้ด
`Assets/[02]Script/Customer/TalkToCustomerManager.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_NPC

[← รายการทั้งหมด](README.md)
