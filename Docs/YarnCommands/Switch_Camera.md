# `Switch_Camera`

**ประเภท:** Command · **กลุ่ม:** กล้อง (`CameraManager`)

## วิธีเขียน
```
<<Switch_Camera CamController Char2Cam>>
```

## Argument
คำแรกไม่ถูกใช้ · `id`: id กล้องใน `CameraManager` (ช่อง `cameraId` ของ `CameraEntry`, ตั้งชื่อ GameObject ให้ตรงกัน)

| id | ภาพ |
|---|---|
| `MainCam` | บาร์ (มุมเริ่มต้น) |
| `Char1Cam` / `Char2Cam` / `Char3Cam` | ลูกค้าคนที่ 1 / 2 / 3 |
| `Char12Cam` / `Char23Cam` | ลูกค้าคนที่ 1+2 / 2+3 |
| `WideCam` | ภาพกว้างทั้งร้าน |
| `FrontCam` / `BackCam` | มุมทำเครื่องดื่ม หน้า / หลัง |
| `PrepareCam` / `GarnishCam` | ใช้โดยหลังบ้าน (shaker เต็ม / Garnish) |
| `SummaryCam` | หน้าสรุปตอนจบ |

เปลี่ยนชื่อเมื่อ 2026-10-30: `FarLeftCam`→`Char1Cam`, `CenterCam`→`Char2Cam`, `FarRightCam`→`Char3Cam`, `NearLeftCam`→`Char12Cam`, `NearRightCam`→`Char23Cam`, `GranishCam`→`GarnishCam`

## ทำอะไร
เปลี่ยนกล้องแบบ blend

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
id ที่ไม่มีจะขึ้น error ใน Console
เริ่มวันและหลังเสิร์ฟกล้องกลับ MainCam เอง (หลังบ้านทำ) ไม่ต้องสั่ง

## โค้ด
`Assets/[02]Script/CameraControl/CameraManager.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_Camera

[← รายการทั้งหมด](README.md)
