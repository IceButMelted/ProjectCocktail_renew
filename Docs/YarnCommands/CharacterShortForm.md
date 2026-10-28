# `<<Isla ...>>` (แบบสั้นของคำสั่งตัวละคร)

**ประเภท:** Command (เฉพาะ `Enter` / `Exit` รอ) · **กลุ่ม:** ลูกค้า (`TalkToCustomerManager`)

## วิธีเขียน
```
<<Isla Sad>>          สีหน้าตามชื่อ     = <<CharacterExpression CharacterName.Isla Sad>>
<<Isla 1>>            สีหน้าตามเลข      = <<CharacterExpression CharacterName.Isla 1>>
<<Isla Look L>>       หัน L / F / R     = <<CharacterLook CharacterName.Isla L>>
<<Isla Enter 2>>      เข้าร้านจุด 2     = <<CharacterEnter CharacterName.Isla 2>>
<<Isla Exit>>         ออกจากร้าน        = <<CharacterExit CharacterName.Isla>>
```

## Argument
ชื่อคำสั่งคือชื่อตัวละคร: `Cole`, `Owen`, `Walter`, `Faye`, `Isla`, `Claire`, `Eliana`, `Arthur` คำที่สอง (verb) คือ `Look` / `Enter` / `Exit` หรือชื่อ/เลขสีหน้า ส่วนคำที่สามคือ `L`/`F`/`R` หรือเลขจุดยืน

## ทำอะไร
เรียกโค้ดชุดเดียวกับแบบยาวทุกประการ (ดูไฟล์ของแต่ละคำสั่ง)

## หมายเหตุ
- เพิ่มตัวละครใหม่ต้องเพิ่ม enum `CharacterName` (ต่อท้าย) **และ** 1 บรรทัด `[YarnCommand("ชื่อ")]` ท้ายไฟล์โค้ด ไม่งั้นแบบสั้นของคนนั้นใช้ไม่ได้ (แบบยาวยังใช้ได้)
- ชื่อสีหน้าที่ห้ามใช้: `Look`, `Enter`, `Exit` (ชนกับ verb)
- verb ใหม่ในอนาคต (เช่น `<<Isla Motion Nod>>`) เพิ่มได้โดยไม่กระทบคำสั่งเดิม

## โค้ด
`Assets/[02]Script/Customer/TalkToCustomerManager.Characters.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_NPC

[← รายการทั้งหมด](README.md)
