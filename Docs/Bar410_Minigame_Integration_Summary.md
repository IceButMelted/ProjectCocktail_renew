# Bar410 — Minigame ↔ Game Loop Integration · สรุปสั้น

**Date:** 2026-08-21 · **Branch:** `GameLoop/main`
**แผนต้นทาง:** [`Bar410_Minigame_Integration_Plan.md`](Bar410_Minigame_Integration_Plan.md)
**รายละเอียดเต็ม:** [`Bar410_Minigame_Integration_Report.md`](Bar410_Minigame_Integration_Report.md)

---

## ทำอะไรไป

1. **เขียน `MinigameFlowBridge`** — HSM สั่งเปิด/ปิดมินิเกม และรับผลกลับ เป็นที่เดียวที่สองระบบเจอกัน
2. **`GameFlowCommands` เลือกมินิเกมได้** — `SelectShaking` / `SelectStiring` / `<<flow_minigame>>`
   + `flow_minigame_type()` / `flow_minigame_result()`
3. **ปุ่มวิธีชงเปลี่ยนเจ้าของ** — จากเรียก `StartShakingMinigame` ตรง ๆ เป็นบอก FSM แล้ว FSM สั่งเริ่ม
4. **โยก Cocktail System ที่ค้าง** — `VisualizeCocktail` และ `IngredientButtonUI` อ่าน/เขียนผ่าน
   `ShakerContents` แทนของเก่า · 5 binding เลิกปิด component ด้วย `enabled` เปลี่ยนไปใช้
   `Interactable` ของ BaseInteractable
5. **เปิดทางให้ designer ผูกเอง** — `ShakerContents.Changed` / `Cleared` / `IdentityResolved`
   เปลี่ยนจาก C# event เป็น UnityEvent · `IngredientButtonUI` เพิ่ม `OnPoured` / `OnRejected`

## บั๊กที่แก้ไปด้วย

| # | อาการ |
|---|---|
| V1 | `VisualizeCocktail` หา `CocktailShaker` ไม่เจอในซีนที่ย้ายเสร็จแล้ว → NullReferenceException ทันทีที่กด `BTN_Reset` |
| V2 | ปิดการลากด้วย `DragableObject.enabled = false` ทำให้การลากที่ค้างอยู่ไม่ถูกยกเลิก · `Interactable` เรียก `CancelDrag()` ให้ |
| V3 | ยกเลิกมินิเกมแล้ว shaker ถูกล็อกค้าง ไม่มีใครปลด — ตอนนี้ bridge ปลดให้เมื่อผลเป็น `Cancelled` |
| V4 | `IngredientButtonUI` หา `CocktailShakerData` ไม่เจอในซีนที่ย้ายเสร็จแล้ว → NullReferenceException ทุกครั้งที่คลิกวัตถุดิบ (ทั้ง 20 ปุ่ม) |

## แผนเก่าล้าสมัย

`Integration_Plan.md` เขียนก่อน FSM Simplification · item 1, 8, 9 และปัญหา §5.1–5.3 ถูกปิดไปแล้วในรอบนั้น
`SlidePhase` / `MiniGameState` ที่แผนอ้างถึงไม่มีอยู่จริงแล้ว — เขียนหมายเหตุเตือนไว้หัว §6 ของแผนแล้ว

## กติกาที่ design ยืนยัน (เข้า GDD §10.1 แล้ว)

- **มินิเกมแพ้ไม่ได้** จบได้สองทาง: เล่นสำเร็จ (`Completed`) หรือกดยกเลิก (`Cancelled`)
- **ยกเลิกได้ทุกเมื่อ** เครื่องดื่มไม่เสียหาย กดวิธีชงใหม่เริ่มได้อีก
- **เล่นสำเร็จ = เครื่องดื่มเสร็จ** ไปขั้นตกแต่งทันที (`_autoAdvanceOnWin = true`)
  ลูป 2.1 ↔ 2.2 จึงไม่ถูกใช้ — วิธีชงเป็นท่าปิดท้าย ไม่ใช่ทำต่อวัตถุดิบหนึ่งชนิด

## ตรวจแล้ว

คอมไพล์ผ่านไม่มี error ใหม่ · Yarn ลงทะเบียน `flow_minigame` / `flow_minigame_type` / `flow_minigame_result` ครบ ·
ทดสอบชั้น flow ผ่าน `execute_code` ผ่านทุกเคส · ในซีนไม่เหลือ `CocktailShaker`, `CocktailShakerData`
หรือ `DragableObject.set_enabled` แล้ว

## ค้างอยู่ 1 เรื่อง — ยังเล่นทดสอบไม่ได้

**ไม่มีใครเรียก `flow_open_bar` / `flow_prepare_drinks`** ทั้งในซีนและใน `.yarn`
flow เลยหยุดที่ Level 1 · ปุ่มวิธีชงกดแล้วได้แค่ warning

ต้องตัดสินใจว่าจะให้บทสนทนาเป็นคนขับ (ตรงเจตนา GDD §12) หรือผูกปุ่มไว้ทดสอบก่อน
