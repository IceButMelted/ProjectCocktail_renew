# `ingredient_unlocked`

**ประเภท:** Function → bool · **กลุ่ม:** วัตถุดิบ (`IngredientUnlocks`)

## วิธีเขียน
```
<<if ingredient_unlocked("LemonJuice")>> ... <<endif>>
```

## Argument
| ชื่อ | ความหมาย |
|---|---|
| ingredient | ชื่อ enum ของวัตถุดิบ ดูรายชื่อที่ [`unlock_ingredient`](unlock_ingredient.md) (ไม่สนตัวพิมพ์เล็ก/ใหญ่) |

## ทำอะไร
`true` เมื่อวัตถุดิบนี้ปลดล็อคแล้ว (ตามวัน `$day >= Unlock Day` หรือจาก `unlock_ingredient`) วัตถุดิบที่ไม่มีแถวในตาราง = `true`

## ใช้ได้เมื่อ
ได้ทุกเมื่อ อ่านอย่างเดียว

## หมายเหตุ
ชื่อที่ไม่ใช่วัตถุดิบ (สะกดผิด) = `Debug.LogError` และคืน `false` ไม่มี `IngredientUnlocks` ในฉาก = LogError และคืน `false`

## โค้ด
`Assets/[02]Script/Ingredient Unlock/IngredientUnlocks.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Unlock

[← รายการทั้งหมด](README.md)
