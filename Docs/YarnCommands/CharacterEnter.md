# `CharacterEnter`

**ประเภท:** Command (รอ) · **กลุ่ม:** ลูกค้า (`TalkToCustomerManager`)

## วิธีเขียน
```
<<CharacterEnter CharacterName.Isla 2>>
```
แบบสั้น: `<<Isla Enter 2>>` (ดู [`CharacterShortForm`](CharacterShortForm.md))

## Argument
`character`: `CharacterName.X` หรือ `X` · `spot`: เลขจุดยืน ลำดับใน `_spots` ของ `TalkToCustomerManager` (ไม่ใส่ = 0)

## ทำอะไร
ให้ตัวละครเข้าร้านที่จุดนั้นด้วย fade (ดำโปร่งใส → ดำทึบ → สีจริง) พร้อมขยับขึ้นลงเล็กน้อยเหมือนเดิน (`Walk Bob`) **บทรอจน fade จบ** ถ้ายืนอยู่จุดอื่น จะ fade ออก ย้าย แล้ว fade เข้า ถ้ายืนจุดนั้นอยู่แล้วไม่ทำอะไร

**เสียงเข้าร้าน (2026-10-30):** ถ้าตัวละครมาจากนอกร้าน จะเล่น `Enter Sound Id` ก่อน แล้วรอ `Enter Sound Lead` วินาที (ค่าเริ่มต้น 0.6) ค่อยเริ่ม fade บทรอทั้งหมด ตั้งที่ `NPCs_And_Set` › `TalkToCustomerManager` ใช้กับทุกตัวละคร การย้ายจุดในร้านไม่มีเสียง

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
จุดที่ตัวละครอื่นยืนอยู่แล้วขึ้น warning แต่ยังทำต่อ ระหว่าง Skip หรือ silent replay fade จบทันที

## โค้ด
`Assets/[02]Script/Customer/TalkToCustomerManager.Characters.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_NPC

[← รายการทั้งหมด](README.md)
