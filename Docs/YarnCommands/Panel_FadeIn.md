# `Panel_FadeIn`

**ประเภท:** Command (instance, รอ) · **กลุ่ม:** UI / transition / ระบบอื่น

## วิธีเขียน
```
<<Panel_FadeIn Panel_Transition>>
```

## Argument
target: `Panel_Transition`

## ทำอะไร
เล่น transition FadeIn (เปิดฉาก) บทรอจน animation `Transition_FadeIn` เล่นจบแล้วค่อยไปบรรทัดถัดไป

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
`Panel_Transition` เปลี่ยนรูปแบบได้แต่ต้องรักษา behavior (ดู CLAUDE.md)

## โค้ด
`Assets/[02]Script/UI Script/PanelTransition.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_BeforeOpen, TEST_Misc

[← รายการทั้งหมด](README.md)
