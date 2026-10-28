# `save_chapter_node`

**ประเภท:** Command (instance) · **กลุ่ม:** UI / transition / ระบบอื่น

## วิธีเขียน
```
<<save_chapter_node "[SaveLoadSystem]">>
```

## Argument
target: `[SaveLoadSystem]`

## ทำอะไร
ตั้ง node ปัจจุบันเป็น chapter ของ save

## ใช้ได้เมื่อ
ได้ทุกเมื่อ

## หมายเหตุ
ระบบ Save/Load พักไว้ (CLAUDE.md) ตอน load ยังไม่มีใครอ่านค่านี้

**⚠ ใช้ในฉากหลักไม่ได้ตอนนี้ (ทดสอบ 2026-10-28):** `[SaveLoadSystem]` ถูกปิด (inactive) ใน `New Drag Drop System` Yarn หา target ไม่เจอ ขึ้น error "doesn't have the correct component" และ**บทพูดค้าง** ไม่ได้อยู่ใน `TEST_AllCommands.yarn`

## โค้ด
`Assets/[02]Script/SaveLoad/SaveLoadManager.cs`

## ทดสอบ
ไม่ได้ทดสอบ (ดูหมายเหตุ)

[← รายการทั้งหมด](README.md)
