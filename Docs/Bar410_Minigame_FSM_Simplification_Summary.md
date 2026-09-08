# Bar410 — Minigame FSM Simplification · สรุปสั้น

**Date:** 2026-08-20 · **Branch:** `GameLoop/main`
**Plan ต้นทาง:** [`Bar410_Minigame_FSM_Simplification_Plan.md`](Bar410_Minigame_FSM_Simplification_Plan.md)
**รายละเอียดเต็ม:** [`Bar410_Minigame_FSM_Simplification_Report.md`](Bar410_Minigame_FSM_Simplification_Report.md)

---

## ทำอะไรไป

1. **ยุบ state machine 3 ชั้นเหลือชั้นเดียว** — `MiniGameState` + `SlidePhase` (8 ค่า) + `bool IsRunning`
   → `MinigamePhase { Idle, Intro, Play, Outro }` เส้นตรง ไม่มีตาราง transition
2. **ย้าย Slide Panel จากโค้ดไปเป็น Animator** — ลบ `PanelSlider` + `Easing*` (~500 บรรทัด)
   แทนด้วย Animator Controller + clip ที่ designer แก้เองได้
3. **Hook ของ subclass ใช้ชื่อเดียวกับ HSM** — `OnEnter()` / `OnTick(dt)` / `OnExit()` / `UpdateUI()`
4. **เพิ่ม `Cancel()` + ช่องต่อกลับ HSM** — ยกเลิกมินิเกมกลางคันแบบมี outcome ชัดเจน
   และเตรียม seam ไว้ให้ `MinigameFlowBridge` มาต่อในรอบถัดไป (ดู §การยกเลิก + ช่องต่อ HSM)

## ผลลัพธ์

| | ก่อน | หลัง |
|---|---|---|
| `BaseMiniGame.cs` | 456 | **225** |
| `MixingMinigame.cs` | 413 | 364 |
| `ShakingMinigame.cs` | 203 | 154 |
| `MinigameSystemManager.cs` | 145 | 183 |
| `PanelSlider` + `Easing*` | ~500 | **0 (ลบ)** |
| ไฟล์ใหม่ `MinigamePanelAnimator.cs` | — | 134 |
| **รวม** | **~1,720** | **~1,060** |

`MinigameSystemManager` ยาวขึ้นเพราะรับ seam ของ HSM มาไว้ที่นี่ (event + property + คอมเมนต์อธิบาย)

## บั๊กที่หายไป

| # | อาการเดิม | สถานะ |
|---|---|---|
| B1 | เล่น Mixing รอบ 2 แล้ว panel เล่นท่า Closing แทน InitPanel | ✅ ทดสอบผ่าน |
| B2 | `OnGameEnd(true)` ยิงเร็วไป ~1 วิ (ยิงตอนเริ่ม slide-out) | ✅ ยิงตอน Outro จบ |
| B3 | `OnEndedGame` ยิง 2 ครั้งเมื่อกดปุ่มปิด | ✅ ยิงครั้งเดียว |
| B4 | ใน build จริง `SO_ShakingSetting` asset ถูก `Destroy()` ทิ้ง | ✅ `ResolveSetting<T>()` ทำลายเฉพาะ instance ที่สร้างเอง |
| B5 | `ResetGame()` ไม่เคยถูกเรียกผ่าน FSM ในรอบแรก | ✅ `OnEnter()` เรียกทุกครั้งที่เข้า Play |

## การยกเลิก + ช่องต่อ HSM

**ยกเลิกมินิเกม** — `Cancel()` เป็นตัวจริง ที่เหลือเป็น alias ชี้มาที่เดียวกัน

```
BaseMiniGame.Cancel()          ← canonical · outro ยังเล่นปกติ · รายงาน Cancelled
  ├─ ClosePanel()              alias (ปุ่ม UI)
  └─ EndGame()                 alias (IMinigame + hotkey R/B)

MinigameSystemManager.CancelMinigame()   ← ปุ่มในซีนชี้มาที่นี่
```

**ผลลัพธ์ของรอบ** — `enum MinigameOutcome { Completed, Cancelled }`
`Complete()` → `Completed` · `Cancel()` → `Cancelled` · อ่านย้อนหลังได้ที่ `IMinigame.LastOutcome`

**ช่องต่อกลับ HSM (ยังไม่มีใคร subscribe — เตรียมไว้เฉย ๆ)**

| ฝั่ง Minigame | ทิศ | ฝั่ง HSM (`Bar410.GameFlow.MinigameState`) |
|---|---|---|
| `MinigameSystemManager.StartMinigame(type)` | ← | `OnStartRequested(type)` (ยิงจาก `OnEnter`) |
| `MinigameSystemManager.CancelMinigame()` | ← | `OnStopRequested` (ยิงจาก `OnExit`) |
| `MinigameSystemManager.MinigameFinished(type, outcome)` | → | `ReportResult(outcome)` → `LastOutcome` |
| `ActiveType` / `ActivePhase` | → | ใช้เช็คสถานะ |

ตัวกลางคือ `MinigameFlowBridge` ตาม `Bar410_Minigame_Integration_Plan.md` §3.3 — **ยังไม่ได้เขียน**
ตอนนี้ทั้ง 2 ฝั่งมีปลายสายรออยู่แล้ว งานรอบหน้าคือเขียน bridge ต่อสายอย่างเดียว ไม่ต้องแก้ทั้งสองระบบ

> `MinigameState` ยังเป็น plain C# class ไม่รู้จัก `MinigameSystemManager` และไม่มี `UnityEngine`
> — ไม่ทำลาย layering ตาม `Bar410_StateMachine_Implementation.md`

## Asset ใหม่ — `Assets/[08]Animation/MinigamePanel/`

- `Panel - MinigameStage.controller` → บน `Canvas_MiniGame` (InitPanel / Art001 / Art002 / BG)
- `Panel - MinigamePanel.controller` → บน `Canvas_ShakingMinigame` และ `Canvas_MixingMinigame`
- clip อย่างละ 4 ตัว: `_Hidden` / `_Intro` / `_Shown` / `_Outro`
  Intro 4.8s · Outro 1.2s · `AnimationEvent` ปลาย clip → `OnIntroFinished()` / `OnOutroFinished()`

ต่อสายให้แล้วครบใน `GamePlayScene.unity`, `GamePlayScene 1.unity`, `New Drag Drop System.unity`
ปุ่ม UI ที่เคยเรียก `NextPhase()` / `ClosePanelReset()` เปลี่ยนเป็น `CancelMinigame()` แล้ว

## ทดสอบแล้วใน Play Mode

- Shaking: Intro → Play → กดปิด → Outro → Idle · `OnGameEnd(false)` + `OnEndedGame` ยิงอย่างละ **1 ครั้ง**
- Mixing ชนะ: `OnGameEnd(true)` ยิง **หลัง** Outro จบ
- Shaking → Mixing → Mixing ซ้ำ: เล่นท่าเข้าทุกรอบ, panel ของเกมที่ไม่ได้เล่นค้างที่ hidden
- ปิด fallback timeout เป็น 60s แล้วยังจบที่ ~4.8s → ยืนยัน `AnimationEvent` ทำงานจริง
- **Cancel:** `SEAM_FINISH Shaking outcome=Cancelled` / `SEAM_FINISH Stiring outcome=Completed` ถูกต้องทั้งคู่
  `LastOutcome` ตรงกับทางที่จบจริง · `CancelMinigame()` ตอน Idle = no-op ไม่ยิง event ซ้ำ
- **Cancel กลาง Intro:** start + cancel เฟรมเดียวกัน → Outro → Idle, panel กลับ hidden ครบทุกตัว
- ไม่มี compile error / warning ใหม่

## ทำต่างจากแผน 4 ข้อ

1. **2 controller ไม่ใช่ 1** — clip เดียวจะดัน panel ของทั้ง Shaking และ Mixing เข้าจอพร้อมกัน
2. **เพิ่ม pose clip ให้ `Hidden` / `Shown`** — state ที่ไม่มี motion ทำให้ Unity คืนค่า default
3. **`Complete()` ยิง `OnGameEnd` ตอน Outro จบ** ตาม checklist §5 (ไม่ใช่ตามโค้ดตัวอย่าง §3.1 ที่ยิงทันที)
4. **ไม่ต้องใช้ overshoot ตาม §4.1** — ค่าที่ bake อิงความสูง panel (±200 = ±height/2) ไม่ใช่ความสูงจอ

## ค้างอยู่ 1 เรื่อง

`Assets/[04]Prefab/GameSystemPrefab/SystemGame.prefab` มี `ShakingMinigame`/`MixingMinigame`
แต่ข้างในเป็น `MiniGameCanvas` (โครงเก่า) ไม่ใช่ `Canvas_MiniGame` และซีนไม่ได้ instance prefab นี้
→ **ยังไม่ได้ต่อสาย `_panels`** ถ้ายังใช้อยู่ต้องตัดสินใจว่าจะอัปเดตโครง prefab หรือลบทิ้ง
