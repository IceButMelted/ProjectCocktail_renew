# `CharacterLook`

**ประเภท:** Command · **กลุ่ม:** ลูกค้า (`TalkToCustomerManager`)

## วิธีเขียน
```
<<CharacterLook CharacterName.Isla L>>
```
แบบสั้น: `<<Isla Look L>>` (ดู [`CharacterShortForm`](CharacterShortForm.md))

## Argument
`character`: `CharacterName.X` หรือ `X` · `look`: `L` (ซ้าย) / `F` (ตรง) / `R` (ขวา) ซ้าย-ขวาคือซ้าย-ขวาของจอผู้เล่น (ใช้ `Left` / `Forward` / `Right` เต็มคำได้ด้วย)

## ทำอะไร
หันตัวละคร ไม่รอ เปลี่ยนทั้งรูปตัวและตำแหน่งหน้า

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
สั่งตอนที่ยังไม่อยู่ในร้านได้ ค่าจะถูกจำไว้ ค่าที่ไม่รู้จักขึ้น warning

## โค้ด
`Assets/[02]Script/Customer/TalkToCustomerManager.Characters.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_NPC

[← รายการทั้งหมด](README.md)
