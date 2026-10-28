# `Order_Cocktail_ByName_OutName`

**ประเภท:** Function → string (วางออเดอร์) · **กลุ่ม:** ออเดอร์และงานชงเครื่องดื่ม (`CocktailSystemManager`)

## วิธีเขียน
```
{Order_Cocktail_ByName_OutName("Gin Fizz")}
```

## Argument
`name`: ชื่อ cocktail ใน `SO_CocktailList` (ไม่สนตัวพิมพ์เล็กใหญ่)

## ทำอะไร
วางออเดอร์ cocktail ตามชื่อ (GDD §12 mode 1) แล้วคืนชื่อไว้ใส่ในบท

## ใช้ได้เมื่อ
ก่อน `flow_prepare_drinks`

## หมายเหตุ
เรียกฟังก์ชันนี้คือการวางออเดอร์ ไม่ใช่แค่อ่านชื่อ · ห่อด้วย `[order]...[/order]` ให้ Skip หยุดตรงนี้

## โค้ด
`Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnOrders.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Orders, TEST_RealOrder

[← รายการทั้งหมด](README.md)
