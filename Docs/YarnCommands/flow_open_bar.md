# `flow_open_bar`

**ประเภท:** Command · **กลุ่ม:** Game flow (`GameFlowCommands`)

## วิธีเขียน
```
<<flow_open_bar>>
```

## Argument
ไม่มี

## ทำอะไร
ออกจาก BeforeOpen ไป Open (เริ่มที่ TalkingWithCustomer) เหมือนกดปุ่มที่ผูก `GameFlowCommands.OpenBar`

**บทรอ (2026-10-29):** ชื่อ Chapter (`ChapterTitleUI`) ขึ้นตอนเข้า Open บทหยุดรอจนชื่อ fade เข้า-ค้าง-ออกครบ (ค่าเริ่มต้น 0.5 + 1.5 + 0.5 = 2.5 วินาที ตั้งที่ `Panel_ChapterTitle`) แล้วค่อยเล่นบรรทัดถัดไป ตอน Skip ไม่รอ วันไหนไม่มีแถวใน `SO_DaySchedule` ไม่มีชื่อขึ้น = ไม่รอ

## ใช้ได้เมื่อ
เฉพาะตอนอยู่ใน BeforeOpen › StoryBeforeOpen ที่อื่นจะ warning และไม่เปลี่ยน Phase

## หมายเหตุ
yarn ของทุกวันต้องเรียก ไม่งั้นค้างที่ BeforeOpen · ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_OpenBar

[← รายการทั้งหมด](README.md)
