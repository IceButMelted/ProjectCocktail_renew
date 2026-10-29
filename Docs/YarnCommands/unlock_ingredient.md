# `unlock_ingredient`

**ประเภท:** Command (static) · **กลุ่ม:** วัตถุดิบ (`IngredientUnlocks`)

## วิธีเขียน
```
<<unlock_ingredient LemonJuice>>
```

## Argument
| ชื่อ | ความหมาย |
|---|---|
| ingredient | ชื่อ enum ของวัตถุดิบ (ไม่สนตัวพิมพ์เล็ก/ใหญ่): `Vodka` `Gin` `Whiskey` `Rum` `Tequila` `Triplesec` `DryVermouth` `SweetVermouth` `Campari` `Soda` `CranberryJuice` `LimeJuice` `LemonJuice` `GrapefruitJuice` `Syrup` `PepperMint` `OrangeJuice` |

## ทำอะไร
ปลดล็อควัตถุดิบถาวร (ไม่มีล็อคคืน) ปุ่มขึ้นบนบาร์ตอนเข้า AddIngredient ครั้งถัดไป และสูตรที่ใช้วัตถุดิบนี้ครบแล้วจะถูกสุ่มเป็นออเดอร์ได้ popup "Unlock New Ingredient" รอไปขึ้นตอน Event ของวันถัดไป

## ใช้ได้เมื่อ
ได้ทุกเมื่อ เรียกซ้ำได้ไม่มีผล (silent replay ตอน load จึงรันซ้ำได้)

## หมายเหตุ
ชื่อผิด = `Debug.LogError` (ไม่เงียบ) บทพูดเดินต่อ · ตารางวัน (`IngredientUnlockTable.asset`) ใช้ร่วมกันได้: ตั้ง Day 5 แต่สั่งคำสั่งนี้ก่อนวัน 5 ก็ได้ · ถ้าไม่มี `IngredientUnlocks` ในฉาก = LogError

## โค้ด
`Assets/[02]Script/Ingredient Unlock/IngredientUnlocks.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Unlock

[← รายการทั้งหมด](README.md)
