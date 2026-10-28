# `wait_for_task`

**ประเภท:** Command (รอ) · **กลุ่ม:** ออเดอร์และงานชงเครื่องดื่ม (`CocktailSystemManager`)

## วิธีเขียน
```
<<wait_for_task SystemGame>>
```

## Argument
คำแรกไม่ถูกใช้ (ใส่หรือไม่ใส่ก็ได้)

## ทำอะไร
เปิดให้ใช้วัตถุดิบ แล้ว**บทหยุดรอ**จนเครื่องดื่มถูกเสิร์ฟและให้คะแนน

## ใช้ได้เมื่อ
หลัง `flow_prepare_drinks`

## หมายเหตุ
เกมยังเล่นได้ปกติระหว่างรอ · ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Cocktail System/Yarn/CocktailSystemManager.YarnTask.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_RealOrder

[← รายการทั้งหมด](README.md)
