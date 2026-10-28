# `MoveIn`

**ประเภท:** Command (รอ) · **กลุ่ม:** ลูกค้า (`TalkToCustomerManager`)

## วิธีเขียน
```
<<MoveIn NPC_Name.Isla 2>>
```

## Argument
`npc`: `NPC_Name.X` · `waypoint`: ลำดับ waypoint (ตัวเลข)

## ทำอะไร
ให้ NPC เดินไป waypoint **บทรอจนเดินถึง**

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
ระหว่าง silent replay ไม่เดิน

## โค้ด
`Assets/[02]Script/Customer/TalkToCustomerManager.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_NPC

[← รายการทั้งหมด](README.md)
