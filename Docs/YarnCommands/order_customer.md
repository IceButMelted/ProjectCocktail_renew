# `order_customer`

**ประเภท:** Command (instance) · **กลุ่ม:** ออเดอร์และงานชงเครื่องดื่ม (`CocktailSystemManager`)

## วิธีเขียน
```
<<order_customer "[CocktailSystem]" Isla>>
```

## Argument
target: `[CocktailSystem]` · `npc`: ชื่อ `CharacterName` (Cole / Owen / Walter / Faye / Isla)

## ทำอะไร
บอกว่าออเดอร์ถัดไปเป็นของใคร ใช้ให้คะแนนรสนิยม และใช้เลือกว่าจะแสดงลูกค้าคนไหนในภาพข้างจอตอนมินิเกม (`CharacterCameraView`)

## ใช้ได้เมื่อ
ก่อนมินิเกม จะอยู่ก่อนหรือหลังวางออเดอร์ก็ได้ (คำสั่งนี้ไม่ล้างเมนูที่สั่งไว้)

## หมายเหตุ
**ต้องใส่ทุกออเดอร์ที่ใช้ `Order_Cocktail_ByName_*` หรือ `order_by_type`** เพราะสองอย่างนี้ไม่ตั้งลูกค้าให้ ถ้าไม่ใส่ ภาพลูกค้าข้างจอตอนมินิเกมจะถูกซ่อนและมี warning `[FlowWiring] The order has no customer` ส่วน `Order_Cocktail_OutName(n)` ตั้งลูกค้าเองจึงไม่ต้องใส่
ชื่อผิดจะ warning

## โค้ด
`Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnOrders.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Orders

[← รายการทั้งหมด](README.md)
