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

## ใช้ได้เมื่อ
เฉพาะตอนอยู่ใน BeforeOpen › StoryBeforeOpen ที่อื่นจะ warning และไม่เปลี่ยน Phase

## หมายเหตุ
yarn ของทุกวันต้องเรียก ไม่งั้นค้างที่ BeforeOpen · ระหว่าง silent replay (หลัง load) คำสั่งนี้ไม่ทำอะไร

## โค้ด
`Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs`

## ทดสอบ
`Assets/[06]Dialogue/YarnScript/Demo/TEST_AllCommands.yarn` › TEST_OpenBar

[← รายการทั้งหมด](README.md)
