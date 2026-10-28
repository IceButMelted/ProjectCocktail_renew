# `Reset_Variable`

**ประเภท:** Command · **กลุ่ม:** ออเดอร์และงานชงเครื่องดื่ม (`CocktailSystemManager`)

## วิธีเขียน
```
<<Reset_Variable SystemGame>>
```

## Argument
คำแรกไม่ถูกใช้

## ทำอะไร
ล้างออเดอร์ และรีเซ็ต `$satisfaction`, `$type_of_cocktail`, `$task_done` เตรียมลูกค้าคนถัดไป

## ใช้ได้เมื่อ
หลังจบลูกค้าแต่ละคน

## หมายเหตุ
ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnTask.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Orders

[← รายการทั้งหมด](README.md)
