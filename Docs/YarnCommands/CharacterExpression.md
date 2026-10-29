# `CharacterExpression`

**ประเภท:** Command · **กลุ่ม:** ลูกค้า (`TalkToCustomerManager`)

## วิธีเขียน
```
<<CharacterExpression CharacterName.Isla Sad>>
<<CharacterExpression CharacterName.Isla 1>>
```
แบบสั้น: `<<Isla Sad>>   <<Isla 1>>` (ดู [`CharacterShortForm`](CharacterShortForm.md))

## Argument
`character`: `CharacterName.X` หรือ `X` · `expression`: ชื่อสีหน้า (ไม่สนตัวพิมพ์) หรือเลขลำดับในลิสต์ `Expressions` ของ `SO_Customer` (0 = ค่าเริ่มต้น Neutral)

## ทำอะไร
เปลี่ยนสีหน้าของตัวละคร ไม่รอ

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
ชื่อหรือเลขที่ไม่มีจะขึ้น warning และคงภาพเดิม ชื่อห้ามใช้เป็นสีหน้า: `Look`, `Enter`, `Exit` สั่งตอนที่ยังไม่อยู่ในร้านได้ ค่าจะถูกจำไว้แล้วแสดงตอน `Enter` ถ้าสีหน้านั้นไม่มีภาพซ้าย/ขวา ระบบใช้อีกข้างพลิก (ถ้าไม่มีทั้งสองข้างใช้ภาพตรงและ warning)

**ตัวละครโหมดภาพเก่า** (`UseLegacyVisual` ใน `SO_Customer` ดู [แผน](../Bar410_LegacyCharacterVisual_Plan.md)): เลขลำดับ/ชื่ออ่านจากลิสต์ `LegacyExpressions` (รูปทั้งตัวหันตรง) และ **สั่งสีหน้าแล้วรูปสีหน้าทับ Look** (กลับมาหันตรง) ถ้าอยากให้หันข้างต่อ ให้สั่ง `Look` หลังสีหน้า

## โค้ด
`Assets/[02]Script/Customer/TalkToCustomerManager.Characters.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_NPC

[← รายการทั้งหมด](README.md)
