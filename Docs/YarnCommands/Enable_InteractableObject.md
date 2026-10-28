# `Enable_InteractableObject`

**ประเภท:** Command · **กลุ่ม:** ออเดอร์และงานชงเครื่องดื่ม (`CocktailSystemManager`)

## วิธีเขียน
```
<<Enable_InteractableObject SystemGame false>>
<<Enable_InteractableObject true>>
```

## Argument
`true` / `false` (คำแรกเป็นชื่อ object ที่ไม่ถูกใช้ ไม่ใส่ก็ได้)

## ทำอะไร
เปิด/ปิดปุ่มวัตถุดิบและของที่ผูกกับ `IngredientButtonGroup`

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
FSM ก็ล็อค/ปลดเองตาม step ค่าที่ตั้งด้วยคำสั่งนี้อาจถูกทับตอนเปลี่ยน step · ค่าที่ไม่ใช่ true/false จะ warning · ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnTask.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Misc

[← รายการทั้งหมด](README.md)
