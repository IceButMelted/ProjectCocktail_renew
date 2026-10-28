# `order_satisfaction`

**ประเภท:** Function → string · **กลุ่ม:** ออเดอร์และงานชงเครื่องดื่ม (`CocktailSystemManager`)

## วิธีเขียน
```
{order_satisfaction()}
```

## Argument
ไม่มี

## ทำอะไร
ผลคะแนน: `None` / `Fail` / `Acceptable` / `Perfect`

## ใช้ได้เมื่อ
หลังเสิร์ฟ (หลัง `wait_for_task`)

## หมายเหตุ
ค่าเดียวกันอยู่ในตัวแปร `$satisfaction`

## โค้ด
`Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnOrders.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_RealOrder

[← รายการทั้งหมด](README.md)
