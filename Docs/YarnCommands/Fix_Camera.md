# `Fix_Camera`

**ประเภท:** Command · **กลุ่ม:** กล้อง (`CameraManager`)

## วิธีเขียน
```
<<Fix_Camera CamController true>>
```

## Argument
คำแรกไม่ถูกใช้ · `true` / `false`

## ทำอะไร
ล็อคมุมมองกล้อง (true) หรือให้หันตามเมาส์ (false)

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
ADR 0002: กล้อง fix มุมเดียว ปกติใช้ true · FSM ตั้ง true เองตอน Talking/BeforeOpen

## โค้ด
`Assets/[02]Script/CameraControl/CameraManager.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Camera

[← รายการทั้งหมด](README.md)
