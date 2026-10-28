# `flow_prepare_drinks`

**ประเภท:** Command · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
<<flow_prepare_drinks>>
```

## Argument
ไม่มี

## ทำอะไร
ลูกค้าสั่งแล้ว Step 1 Talking → 2 Prepare Drinks (เริ่มที่ 2.1 AddIngredient) และหยุด Skip ถ้ากำลัง Skip อยู่

## ใช้ได้เมื่อ
เฉพาะ Talking

## หมายเหตุ
วางออเดอร์ก่อนเรียก (เช่น `Order_Cocktail_ByName_OutName`) · ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_FlowSteps, TEST_RealOrder

[← รายการทั้งหมด](README.md)
