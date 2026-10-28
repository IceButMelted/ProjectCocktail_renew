# `order_by_type`

**ประเภท:** Command (instance) · **กลุ่ม:** ออเดอร์และงานชงเครื่องดื่ม (`CocktailSystemManager`)

## วิธีเขียน
```
<<order_by_type "[CocktailSystem]" HighAlcohol>>
```

## Argument
target: ชื่อ GameObject ของ `CocktailSystemManager` ในฉากหลักคือ `[CocktailSystem]` · `type`: `HighAlcohol` / `LowAlcohol` / `NoneAlcohol`

## ทำอะไร
ลูกค้าสั่งแค่ประเภท (mode 5) อะไรก็ได้ที่ประเภทตรงถือว่าถูก

## ใช้ได้เมื่อ
ก่อน `flow_prepare_drinks`

## หมายเหตุ
เป็น instance command จึง**ต้อง**ใส่ชื่อ GameObject ให้ถูก (ใน prefab ชื่อ `SystemGame` แต่ในฉากถูกเปลี่ยนเป็น `[CocktailSystem]`) · type ผิดจะ warning · หลังเรียก `order_name()` เป็นค่าว่าง เพราะไม่ได้ระบุ drink

## โค้ด
`Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnOrders.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Orders

[← รายการทั้งหมด](README.md)
