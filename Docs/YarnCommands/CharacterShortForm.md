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

## คำสั่งของแต่ละตัวละคร (2026-10-29)

ทั้ง 8 ตัวใช้โหมดภาพเก่า (`UseLegacyVisual`) ชื่อ/เลขสีหน้ามาจาก `SO_Customer` ของแต่ละตัว (`Assets/[02]Script/NPC/CustomerData/`) ถ้าแก้ใน SO ให้แก้ตารางนี้ด้วย หรือดูช่อง Example Commands ในแท็บ Visual ของ SO

| ตัวละคร | สีหน้า (เลข = ชื่อ) | Look |
|---|---|---|
| `Cole` | 0 `Neutral`, 1 `Happy`, 2 `Sad` | F เท่านั้น (L/R ยังไม่มีรูป: ใช้รูปสีหน้า + warning) |
| `Owen` | 0 `Neutral`, 1 `Happy`, 2 `Sad`, 3 `Happy1` | F / L / R |
| `Walter` | 0 `Neutral`, 1 `Happy`, 2 `Sad`, 3 `KneeUp` | F / L / R |
| `Faye` | 0 `Neutral`, 1 `Happy`, 2 `Sad` | F เท่านั้น (L/R ยังไม่มีรูป: ใช้รูปสีหน้า + warning) |
| `Isla` | 0 `Neutral`, 1 `Happy`, 2 `Sad`, 3 `Sad2`, 4 `KneeUp` | F / L / R |
| `Claire` | 0 `Neutral`, 1 `Happy`, 2 `Amaze`, 3 `Rage` | F / L / R |
| `Eliana` | 0 `Neutral`, 1 `Happy`, 2 `Disappointed` | F / L / R |
| `Arthur` | 0 `Neutral`, 1 `NeutralLeft`, 2 `Mockery`, 3 `HandIn`, 4 `Angry`, 5 `Rage` | F / L / R |

```
<<Cole Enter 0>>  <<Cole Neutral>>  <<Cole Happy>>  <<Cole Sad>>  <<Cole Look L>>  <<Cole Look F>>  <<Cole Look R>>  <<Cole Exit>>
<<Owen Enter 0>>  <<Owen Neutral>>  <<Owen Happy>>  <<Owen Sad>>  <<Owen Happy1>>  <<Owen Look L>>  <<Owen Look F>>  <<Owen Look R>>  <<Owen Exit>>
<<Walter Enter 0>>  <<Walter Neutral>>  <<Walter Happy>>  <<Walter Sad>>  <<Walter KneeUp>>  <<Walter Look L>>  <<Walter Look F>>  <<Walter Look R>>  <<Walter Exit>>
<<Faye Enter 0>>  <<Faye Neutral>>  <<Faye Happy>>  <<Faye Sad>>  <<Faye Look L>>  <<Faye Look F>>  <<Faye Look R>>  <<Faye Exit>>
<<Isla Enter 0>>  <<Isla Neutral>>  <<Isla Happy>>  <<Isla Sad>>  <<Isla Sad2>>  <<Isla KneeUp>>  <<Isla Look L>>  <<Isla Look F>>  <<Isla Look R>>  <<Isla Exit>>
<<Claire Enter 0>>  <<Claire Neutral>>  <<Claire Happy>>  <<Claire Amaze>>  <<Claire Rage>>  <<Claire Look L>>  <<Claire Look F>>  <<Claire Look R>>  <<Claire Exit>>
<<Eliana Enter 0>>  <<Eliana Neutral>>  <<Eliana Happy>>  <<Eliana Disappointed>>  <<Eliana Look L>>  <<Eliana Look F>>  <<Eliana Look R>>  <<Eliana Exit>>
<<Arthur Enter 0>>  <<Arthur Neutral>>  <<Arthur NeutralLeft>>  <<Arthur Mockery>>  <<Arthur HandIn>>  <<Arthur Angry>>  <<Arthur Rage>>  <<Arthur Look L>>  <<Arthur Look F>>  <<Arthur Look R>>  <<Arthur Exit>>
```

## หมายเหตุ
- เพิ่มตัวละครใหม่ต้องเพิ่ม enum `CharacterName` (ต่อท้าย) **และ** 1 บรรทัด `[YarnCommand("ชื่อ")]` ท้ายไฟล์โค้ด ไม่งั้นแบบสั้นของคนนั้นใช้ไม่ได้ (แบบยาวยังใช้ได้)
- ชื่อสีหน้าที่ห้ามใช้: `Look`, `Enter`, `Exit` (ชนกับ verb)
- verb ใหม่ในอนาคต (เช่น `<<Isla Motion Nod>>`) เพิ่มได้โดยไม่กระทบคำสั่งเดิม

## โค้ด
`Assets/[02]Script/Customer/TalkToCustomerManager.Characters.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_NPC

[← รายการทั้งหมด](README.md)
