# `wait_scene`

**ประเภท:** Command (รอ) · **กลุ่ม:** ออเดอร์และงานชงเครื่องดื่ม (`CocktailSystemManager`)

## วิธีเขียน
```
<<wait_scene 1>>
```

## Argument
`seconds`: ตัวเลข

## ทำอะไร
รอเป็นวินาที ใช้แทน `<<wait>>` ของ Yarn

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
ไม่รอระหว่าง Skip และ silent replay

## โค้ด
`Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnTask.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Misc

[← รายการทั้งหมด](README.md)
