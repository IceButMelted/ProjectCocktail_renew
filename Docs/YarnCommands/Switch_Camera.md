# `Switch_Camera`

**ประเภท:** Command · **กลุ่ม:** กล้อง (`CameraManager`)

## วิธีเขียน
```
<<Switch_Camera CamController CenterCam>>
```

## Argument
คำแรกไม่ถูกใช้ · `id`: id กล้องใน `CameraManager` เช่น `MainCam`, `CenterCam`, `NearLeftCam`, `NearRightCam`, `FarRightCam`, `SummaryCam`

## ทำอะไร
เปลี่ยนกล้องแบบ blend

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
id ที่ไม่มีจะขึ้น error ใน Console

## โค้ด
`Assets/[02]Script/CameraControl/CameraManager.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Camera

[← รายการทั้งหมด](README.md)
