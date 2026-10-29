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
ให้ตัวละครออกจากร้านด้วย fade ย้อนกลับ (สีจริง → ดำทึบ → หาย) พร้อมขยับขึ้นลงเหมือนเดิน (`Walk Bob`) **บทรอจน fade จบ**

**เสียงออกร้าน (2026-10-30):** หลัง fade ออกจบ `Exit Sound Delay` วินาที (ค่าเริ่มต้น 0.6) จะเล่น `Exit Sound Id` บทไม่รอเสียงนี้ ตั้งที่ `NPCs_And_Set` › `TalkToCustomerManager`

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
ถ้าไม่อยู่ในร้านไม่ทำอะไร ระหว่าง Skip หรือ silent replay fade จบทันที

## โค้ด
`Assets/[02]Script/Customer/TalkToCustomerManager.Characters.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_NPC

[← รายการทั้งหมด](README.md)
