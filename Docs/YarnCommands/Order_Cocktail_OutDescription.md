# `Order_Cocktail_OutDescription`

**ประเภท:** Function → string (วางออเดอร์) · **กลุ่ม:** ออเดอร์และงานชงเครื่องดื่ม (`CocktailSystemManager`)

## วิธีเขียน
```
{Order_Cocktail_OutDescription(5)}
```

## Argument
`npc`: เหมือนข้างบน

## ทำอะไร
เหมือน `Order_Cocktail_OutName` แต่คืนคำอธิบาย (mode 4)

## ใช้ได้เมื่อ
ก่อน `flow_prepare_drinks`

## โค้ด
`Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnOrders.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Orders

[← รายการทั้งหมด](README.md)
