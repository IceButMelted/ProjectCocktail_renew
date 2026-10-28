# `CharacterExit`

**ประเภท:** Command (รอ) · **กลุ่ม:** ลูกค้า (`TalkToCustomerManager`)

## วิธีเขียน
```
<<CharacterExit CharacterName.Isla>>
```
แบบสั้น: `<<Isla Exit>>` (ดู [`CharacterShortForm`](CharacterShortForm.md))

## Argument
`character`: `CharacterName.X` หรือ `X`

## ทำอะไร
ให้ตัวละครออกจากร้านด้วย fade ย้อนกลับ (สีจริง → ดำทึบ → หาย) **บทรอจน fade จบ**

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
ถ้าไม่อยู่ในร้านไม่ทำอะไร ระหว่าง Skip หรือ silent replay fade จบทันที

## โค้ด
`Assets/[02]Script/Customer/TalkToCustomerManager.Characters.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_NPC

[← รายการทั้งหมด](README.md)
