# `Order_Cocktail_OutName`

**ประเภท:** Function → string (วางออเดอร์) · **กลุ่ม:** ออเดอร์และงานชงเครื่องดื่ม (`CocktailSystemManager`)

## วิธีเขียน
```
{Order_Cocktail_OutName(5)}
```

## Argument
`npc`: `CharacterName` เป็นตัวเลข (None 0, Cole 1, Owen 2, Walter 3, Faye 4, Isla 5)

## ทำอะไร
สุ่ม cocktail ตามความชอบของลูกค้า (mode 3) คืนชื่อ

## ใช้ได้เมื่อ
ก่อน `flow_prepare_drinks`

## หมายเหตุ
เรียกคู่กับ `Order_Cocktail_OutDescription` ได้ จะได้ drink ตัวเดียวกัน (ไม่สุ่มใหม่) · ถ้ามีออเดอร์ค้างอยู่ (เช่นเพิ่งเรียก `Order_Cocktail_ByName_*`) จะคืนออเดอร์นั้นแทนการสุ่ม เรียก `Reset_Variable` ก่อนถ้าต้องการสุ่มใหม่

## โค้ด
`Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnOrders.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Orders

[← รายการทั้งหมด](README.md)
