# `order_customer`

**ประเภท:** Command (instance) · **กลุ่ม:** ออเดอร์และงานชงเครื่องดื่ม (`CocktailSystemManager`)

## วิธีเขียน
```
<<order_customer "[CocktailSystem]" Isla>>
```

## Argument
target: `[CocktailSystem]` · `npc`: ชื่อ `CharacterName` (Cole / Owen / Walter / Faye / Isla)

## ทำอะไร
บอกว่าออเดอร์ถัดไปเป็นของใคร (ไม่ใส่ก็ได้)

## ใช้ได้เมื่อ
ก่อนวางออเดอร์

## หมายเหตุ
ชื่อผิดจะ warning

## โค้ด
`Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnOrders.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Orders

[← รายการทั้งหมด](README.md)
