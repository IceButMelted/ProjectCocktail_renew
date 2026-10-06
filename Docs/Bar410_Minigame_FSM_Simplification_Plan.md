# Bar410 — Minigame FSM Simplification & Animator-Driven Panels

**Date:** 2026-08-20
**Branch:** `GameLoop/main`
**Scope:** `Assets/[02]Script/Minigame/` (+ `Cocktail System/PanelSlider.cs`, `AnimationCode/Easing*`)

**เป้าหมาย 2 ข้อ**

1. ยุบ FSM ที่ซ้อนกันอยู่ 3 ชั้นใน `BaseMiniGame` ให้เหลือ **ตัวเดียว** ที่มี hook ชื่อเดียวกับ HSM ที่มีอยู่แล้ว — `OnEnter()` / `OnTick(dt)` / `OnExit()`
2. ย้ายการ Slide Panel จาก **โค้ด** (`PanelSlider` + `EasingConfig`) ไปเป็น **Animator Controller** ที่ designer แก้เองได้

Companion กับ `Bar410_Minigame_Integration_Plan.md` — เอกสารนั้นคุยเรื่อง *ใคร* สั่งเริ่มมินิเกม
เอกสารนี้คุยเรื่อง *ข้างในมินิเกมทำงานยังไง*

---

## 1. ปัญหาที่เจอตอนนี้

### 1.1 มี state machine ซ้อนกัน 3 ชุดใน `BaseMiniGame.cs` (456 บรรทัด)

| ชุด | ที่อยู่ | ขับเคลื่อนโดย |
|---|---|---|
| `MiniGameState` (Standby / Processing / Success) | `IMinigame.cs:6` | `SetState()` + ตาราง `IsValidTransition` |
| `SlidePhase` (8 ค่า) | `BaseMiniGame.cs:42` | `SlidePanelMinigame()` เรียกทุกเฟรมจาก `ProcessedGame()` |
| `bool IsRunning` | `BaseMiniGame.cs:25` | เขียนจาก 5 ที่ ทั้ง base และ subclass |

ทั้ง 3 ชุดควบคุมเรื่องเดียวกัน (“ตอนนี้รับ input ได้ยัง / ตอนนี้อยู่ช่วงไหน”) แต่ไม่มีตัวไหนเป็นเจ้าของความจริง:

- `IsRunning = true` ถูกเซ็ตลึกอยู่ใน `SlidePhase_InitMinigame()` ([BaseMiniGame.cs:275](Assets/[02]Script/Minigame/BaseMiniGame.cs:275))
- `SetState(Success)` ถูกเรียก **ทุกเฟรม** ใน `SlidePhase_RemoveMinigame()` ([:281](Assets/[02]Script/Minigame/BaseMiniGame.cs:281)) — รอดมาได้เพราะ `SetState` มี early-return ตอน state เท่าเดิม
- subclass สั่ง slide phase จากใน `ResetGame()` ([MixingMinigame.cs:223](Assets/[02]Script/Minigame/Minigame/MixingMinigame.cs:223), [ShakingMinigame.cs:151](Assets/[02]Script/Minigame/Minigame/ShakingMinigame.cs:151)) — คนละค่ากันทั้งที่เป็น base เดียวกัน

### 1.2 บั๊กจริงที่เกิดจากความซ้อนนี้

| # | อาการ | ที่มา |
|---|---|---|
| B1 | **เล่นรอบที่ 2 ของ Mixing แล้ว panel เล่นท่า Closing แทน InitPanel** | `StartGame()` เซ็ต `CurrentSlidePhase = InitPanel` **ก่อน** `SetState(Standby)` → `OnStandby()` → `ResetGame()` ทับเป็น `Closing` ([BaseMiniGame.cs:410-411](Assets/[02]Script/Minigame/BaseMiniGame.cs:410)) |
| B2 | `OnGameEnd(true)` ยิงเร็วไป ~1 วิ (ยิงตอนเริ่ม slide-out ไม่ใช่ตอนจบ) | `SetState(Success)` → `OnSuccess()` → `FireEndEvent(true)` อยู่บรรทัดแรกของ `SlidePhase_RemoveMinigame` |
| B3 | `OnEndedGame` ยิง **2 ครั้ง** เมื่อกดปุ่มปิด | `NextPhase()` invoke เอง ([MinigameSystemManager.cs:74](Assets/[02]Script/Minigame/MinigameSystemManager.cs:74)) แล้ว `SlidePhase_Closing` invoke ซ้ำ ([BaseMiniGame.cs:401](Assets/[02]Script/Minigame/BaseMiniGame.cs:401)) |
| B4 | **ใน build จริง `SO_ShakingSetting` asset ถูก `Destroy()` ทิ้ง** | `ShakingMinigame.OnDestroy()` branch `#else` ไม่ได้เช็คว่าเป็น asset หรือ runtime instance ([ShakingMinigame.cs:56-61](Assets/[02]Script/Minigame/Minigame/ShakingMinigame.cs:56)) |
| B5 | `ResetGame()` ไม่เคยถูกเรียกผ่าน FSM ในรอบแรก | รอบแรก `CurrentState` เป็น `Standby` อยู่แล้ว → `SetState(Standby)` early-return → ต้องพึ่ง subclass เรียก `ResetGame()` เองซ้ำใน `StartGame()` |

### 1.3 โค้ดตาย / โค้ดคอมเมนต์ทิ้ง

- `SlidePhase.InitBackground` — ประกาศไว้ ไม่มี case ใน switch
- `_backgroundSnapped`, `openPanelDoneCount`, `_closingSnapApplied` — เขียนอย่างเดียว ไม่มีใครอ่านผล
- `SlidePhase_Closing()` — 90 บรรทัด โดย ~55 บรรทัดเป็นคอมเมนต์ และมี `if (!_closingSnapApplied) { }` ที่ body ว่างเปล่า
- `OnFailed()` — ไม่มีใครเรียก
- `IMinigameContext.NotifyGameEnded()` — implement ไว้ใน manager แต่ call site ถูกคอมเมนต์ทิ้ง ([BaseMiniGame.cs:429](Assets/[02]Script/Minigame/BaseMiniGame.cs:429)) แล้วใช้ `[SerializeField] m_systemManager` ยิงตรงแทน → **DIP ที่ตั้งใจไว้ตั้งแต่แรกถูกทิ้ง** และมี 2 เส้นทางไปหา event เดียวกัน
- `ButtonPanel` — เหลือแค่ snap ตอน Closing, การใช้งานจริงถูกคอมเมนต์หมด

### 1.4 การ Slide ทำด้วยโค้ดล้วน

`PanelSlider.cs` (366 บรรทัด) + `SlideFinishCondition.cs` (26) + `EasingConfig/EasingMath/EasingMode`
รวม ~500 บรรทัด **มี `BaseMiniGame` ใช้อยู่ที่เดียวในทั้งโปรเจกต์** (ยืนยันด้วย grep)

ผลที่ตามมา:
- ปรับจังหวะ/ความรู้สึกของ animation ต้องแก้โค้ด compile ใหม่ทุกรอบ designer แก้เองไม่ได้
- ลำดับ 5 phase (`InitPanel → InitPanelExit → InitArt → InitMinigame`) จริง ๆ แล้วคือ **timeline เดียว** ที่ถูกกางออกเป็น state machine เพราะไม่มีเครื่องมือ timeline
- `SlideFinishCondition` 15 ค่า เป็นการเขียน keyframe ด้วยภาษาเรขาคณิต

**ที่สำคัญ: โปรเจกต์นี้มี pattern Animator สำหรับ panel อยู่แล้ว**
`Assets/[08]Animation/OrderDirnks/Panel - OrderDirnks.controller` (params `IsVisible`, `IsOutScreen` + clip `Open_/Close_/Out_OrderDrinks`) และ `PanelTransition.cs` ที่รอ clip จบด้วย `normalizedTime`
→ แผนนี้คือการเอามินิเกมมาใช้ pattern เดียวกับที่ทีมใช้อยู่แล้ว ไม่ใช่การประดิษฐ์ของใหม่

### 1.5 ชื่อชนกัน

`MiniGameState` (gameplay enum) vs `Bar410.GameFlow.MinigameState` (HSM flow state) — โค้ดเองก็คอมเมนต์เตือนไว้แล้ว

---

## 2. เป้าหมายปลายทาง

```
BaseMiniGame : MonoBehaviour, IMinigame
 ├─ MinigamePhase   (enum เดียว: Idle → Intro → Play → Outro → Idle)
 └─ MinigamePanelAnimator  (component ใหม่ ~60 บรรทัด — ครอบ Animator)
       └─ Animator + "Panel - Minigame.controller"
            ├─ Hidden (default)
            ├─ Intro   ── AnimationEvent ปลาย clip → IntroFinished
            ├─ Shown
            └─ Outro   ── AnimationEvent ปลาย clip → OutroFinished
```

### 2.1 FSM ใหม่ — 4 state, เส้นตรง, ไม่ต้องมีตาราง transition

| State | รับ input? | เกิดอะไร |
|---|---|---|
| `Idle` | ✗ | ทุก panel อยู่นอกจอ (Animator state `Hidden`) |
| `Intro` | ✗ | Animator เล่น clip เข้า → รอ `IntroFinished` |
| `Play` | ✓ | `OnTick(dt)` เดินเกม, `Input.Poll()` |
| `Outro` | ✗ | Animator เล่น clip ออก → รอ `OutroFinished` → แจ้ง manager → `Idle` |

`IsRunning` กลายเป็น computed property: `=> Phase == MinigamePhase.Play` — ตัดตัวแปรที่เขียนจาก 5 ที่ทิ้ง

### 2.2 Hook ที่ subclass override — ชื่อตรงกับ `Bar410.GameFlow.StateBase`

| Hook เดิม (7 ตัว) | Hook ใหม่ (4 ตัว) |
|---|---|
| `StartGame()` + `ResetGame()` + `OnProcessing()` | `OnEnter()` — reset ค่า, สุ่ม zone, sync UI |
| `ProcessedGame()` | `OnTick(float dt)` — gameplay ล้วน ไม่มี guard, ไม่มี slide |
| `EndGame()` | `OnExit()` — หยุด coroutine, `amc.StopAnimation()` |
| `UpdateUI()` | `UpdateUI()` (คงเดิม) |
| `OnSuccess()` / `OnFailed()` / `OnStandby()` | ตัดทิ้ง — subclass เรียก `Complete()` แทน |

การจบเกมเปลี่ยนจาก “เซ็ต `CurrentSlidePhase = RemoveMinigame` + `IsRunning = false` + `amc.StopAnimation()` 3 บรรทัดที่ซ้ำกันใน 2 ไฟล์” เหลือ `Complete();` บรรทัดเดียวใน base

---

## 3. งานฝั่งโค้ด

| # | ไฟล์ | งาน | Δ บรรทัด |
|---|---|---|---|
| C1 | `IMinigame.cs` | `MiniGameState` → `MinigamePhase { Idle, Intro, Play, Outro }`, เลิกชนกับ HSM | ~เท่าเดิม |
| C2 | **ใหม่** `Minigame/MinigamePanelAnimator.cs` | ครอบ `Animator`: `Show()` / `Hide()` + `event Action IntroFinished, OutroFinished` (เรียกจาก AnimationEvent) + fallback timeout กันคลิปไม่มี event | +60 |
| C3 | `BaseMiniGame.cs` | ลบ `SlidePhase` ทั้งชุด, `SlideSession` fields, `InitPanelRectTransform`/`CopyRectTransform`, list `OpenPanel`/`ArtWorks`/`BackgroundPanelgame`/`ButtonPanel`, `_slidePanelSpeed`, `EasingConfigTimer` → เหลือ FSM 4 state + hook | **456 → ~130** |
| C4 | `BaseMiniGame.cs` | เพิ่ม `protected T ResolveSetting<T>()` generic (ยุบ `Awake` + `OnDestroy` ที่ซ้ำกันใน 2 subclass และแก้ B4 ในตัว) | +12 |
| C5 | `MixingMinigame.cs` | ย้ายไป `OnEnter/OnTick/OnExit`, ลบ `CurrentSlidePhase = …`, ลบ `Awake/OnDestroy` (ใช้ C4) | 413 → ~330 |
| C6 | `ShakingMinigame.cs` | เหมือน C5 | 203 → ~165 |
| C7 | `MinigameSystemManager.cs` | ยุบ `NextPhase()` + `ClosePanelReset()` เหลือตัวเดียว (แก้ B3), ให้ end-event เดินผ่าน `IMinigameContext.NotifyGameEnded()` เส้นเดียว, ลบ `[SerializeField] m_systemManager` ออกจาก `BaseMiniGame` | 145 → ~110 |
| C8 | `Cocktail System/PanelSlider.cs`, `SlideFinishCondition.cs`, `AnimationCode/EasingConfig.cs`, `EasingMath.cs`, `EasingMode.cs` | **ไม่มีใครใช้แล้ว** — ลบ | −500 |

รวม **~1,350 → ~700 บรรทัด**

> **หมายเหตุ C8:** ถ้าอยากเก็บ `PanelSlider` ไว้เผื่อระบบอื่นในอนาคต ให้ย้ายไป `Assets/[02]Script/_Unused/` แทนการลบ จะได้ไม่หลอกคนอ่านว่ายัง active อยู่ — แต่คำแนะนำคือลบ เพราะ git เก็บให้อยู่แล้ว

### 3.1 หน้าตา `BaseMiniGame` หลังแก้ (โครงคร่าว ๆ)

```csharp
public abstract class BaseMiniGame : MonoBehaviour, IMinigame
{
    [SerializeField] private MinigamePanelAnimator _panels;
    [SerializeField] protected AnimationMinigameController amc;
    [field: SerializeField] public SO_MinigameSetting Setting { get; set; }

    public MinigamePhase Phase { get; private set; } = MinigamePhase.Idle;
    public bool IsRunning => Phase == MinigamePhase.Play;
    public event Action<bool> OnGameEnd;
    public abstract Enum_MiniGameType GameType { get; }

    // ── Transition เส้นเดียว ────────────────────────────
    private void SetPhase(MinigamePhase next)
    {
        if (Phase == next) return;
        ExitPhase(Phase);
        Phase = next;
        EnterPhase(next);
    }

    private void EnterPhase(MinigamePhase p)
    {
        switch (p)
        {
            case MinigamePhase.Intro: _panels.Show(); break;        // ← Animator ทำงานแทนโค้ด
            case MinigamePhase.Play:  OnEnter(); UpdateUI(); break;
            case MinigamePhase.Outro: amc.StopAnimation(); _panels.Hide(); break;
            case MinigamePhase.Idle:  _context.NotifyGameEnded(); break;
        }
    }

    private void ExitPhase(MinigamePhase p)
    {
        if (p == MinigamePhase.Play) OnExit();
    }

    // ── Driver เดียว ────────────────────────────────────
    public void ProcessedGame()
    {
        if (Phase != MinigamePhase.Play) return;   // Intro/Outro เป็นหน้าที่ Animator
        Input.Poll();
        OnTick(Time.deltaTime);
        UpdateUI();
    }

    public void StartGame() { _context?.ResetCamera(); SetPhase(MinigamePhase.Intro); }
    protected void Complete() { OnGameEnd?.Invoke(true); SetPhase(MinigamePhase.Outro); }
    public void ClosePanel() { if (Phase != MinigamePhase.Idle) SetPhase(MinigamePhase.Outro); }

    // ── Hook (subclass override) ────────────────────────
    protected virtual void OnEnter() { }
    protected virtual void OnTick(float dt) { }
    protected virtual void OnExit() { }
    public virtual void UpdateUI() { }
}
```

`_panels.IntroFinished += () => SetPhase(Play)` และ `_panels.OutroFinished += () => SetPhase(Idle)` subscribe ใน `OnEnable` / unsubscribe ใน `OnDisable`

**ผลลัพธ์:** ลูป `Idle → Intro → Play → Outro → Idle` อ่านจบภายในหน้าจอเดียว ไม่มี phase ไหนแอบเซ็ต phase อื่น

---

## 4. งานฝั่ง Unity Editor

| # | งาน | หมายเหตุ |
|---|---|---|
| U1 | สร้าง `Assets/[08]Animation/MinigamePanel/Panel - Minigame.controller` | copy โครงจาก `Panel - OrderDirnks.controller`: state `Hidden`(default) / `Intro` / `Shown` / `Outro`, trigger `Show` + `Hide` |
| U2 | author `Minigame_Intro.anim` | **1 clip เดียวคุมทุก panel** — เดิม 5 phase ในโค้ดคือ OpenPanel เข้ากลาง → OpenPanel ออกบน → ArtWorks เข้า → MinigamePanel เข้า ตอนนี้เป็นแค่ keyframe ต่างเวลากันบน timeline เดียว |
| U3 | author `Minigame_Outro.anim` | MinigamePanel + Background ออกล่าง, ArtWorks ตามลงไป |
| U4 | ใส่ `AnimationEvent` เฟรมสุดท้ายของแต่ละ clip → `OnIntroFinished()` / `OnOutroFinished()` | ให้ designer เลื่อน event มาก่อนคลิปจบได้ ถ้าอยากให้เริ่มรับ input เร็วขึ้น |
| U5 | แปะ `Animator` + `MinigamePanelAnimator` บน root ของกลุ่ม panel มินิเกม | ต้องเป็น **parent ร่วม** ของ OpenPanel / ArtWorks / Background / MinigamePanel เพราะ Animator เล่นตาม path ของลูก |
| U6 | assign ช่อง `_panels` ใน Inspector ทั้ง `ShakingMinigame` และ `MixingMinigame` | |
| U7 | เคลียร์ field เก่าที่หายไปใน `GamePlayScene.unity`, `GamePlayScene 1.unity`, `CocktailSystem.prefab`, `SystemGame.prefab` | Unity จะทิ้ง reference กำพร้าไว้ ควรเปิดทีละอันตรวจ |
| U8 | rewire ปุ่ม UI ที่เคยเรียก `NextPhase()` → method เดียวที่เหลือ | ดู C7 |

### 4.1 ⚠ ข้อควรระวังหลัก — resolution independence

นี่คือ **trade-off จริงข้อเดียว** ของแผนนี้ ต้องรู้ก่อนลงมือ

`PanelSlider` คำนวณตำแหน่งเป้าหมายจาก `parentRT.rect` **ตอน runtime** → ถูกต้องทุกความละเอียดอัตโนมัติ
ส่วน Animator เก็บ `anchoredPosition` เป็น **ค่าคงที่ที่ bake ไว้ตอน author**

Canvas ในซีนตั้ง `ScaleWithScreenSize` @ 1920×1080 **match = 0.5** → ขนาด canvas เชิงตรรกะยังแปรตาม aspect ratio อยู่ ดังนั้นค่า off-screen ที่ bake ที่ 16:9 อาจ **ไม่พ้นจอ** บนจอ 21:9

เลือกทางแก้ 1 ใน 3 (แนะนำ **ก**):

- **ก. overshoot** — bake ตำแหน่งนอกจอให้เกินไปสัก 1.3–1.5 เท่าของขนาด panel ง่ายสุด ไม่ต้องแตะ setting อื่น กินแค่เวลา animation นิดเดียว
- **ข. ตั้ง `m_MatchWidthOrHeight = 1`** (ยึดความสูง) แล้วค่า Y ทั้งหมดจะคงที่ทุก aspect — แต่กระทบ UI อื่นทั้งซีน
- **ค. animate `anchorMin`/`anchorMax`** แทน `anchoredPosition` — normalize 100% แต่ author ยากและ Animator record ตัวนี้ไม่ค่อยสะดวก

### 4.2 ข้อควรระวังอื่น

- Animator **ไม่ tick ถ้า GameObject ถูก disable** — ถ้าโค้ดเดิมที่ไหน `SetActive(false)` panel ตอน Idle ต้องเปลี่ยนไปใช้ `CanvasGroup.alpha` หรือปล่อยให้ Animator ดันออกนอกจอแทน
- `Animator.updateMode` ต้องไม่ใช่ `AnimatePhysics`; ถ้ามีจุดที่ pause เกมด้วย `Time.timeScale = 0` ให้ตั้งเป็น `UnscaledTime`
- `amc.StopAllCoroutines()` ใน `EndGame()`/`FireEndEvent()` ปัจจุบัน **ยื่นมือไปหยุด coroutine ของ component อื่น** ([BaseMiniGame.cs:428,442](Assets/[02]Script/Minigame/BaseMiniGame.cs:428)) — เปลี่ยนเป็น `amc.StopAnimation()` ซึ่งเป็น public API ที่ตั้งใจไว้อยู่แล้ว

---

## 5. ลำดับการทำ

แต่ละ step compile ผ่านและเล่นได้ ไม่ต้อง big-bang

| Step | ทำอะไร | ตรวจว่า |
|---|---|---|
| **1** | ลบโค้ดตายตาม §1.3 ล้วน ๆ (`InitBackground`, `_backgroundSnapped`, `openPanelDoneCount`, `_closingSnapApplied`, บล็อกคอมเมนต์ใน `SlidePhase_Closing`, `OnFailed`) | พฤติกรรมเหมือนเดิมเป๊ะ — commit แยกไว้เป็นฐานเทียบ |
| **2** | แก้ B4 (`ShakingMinigame.OnDestroy`) + B3 (`NextPhase` ยิงซ้ำ) | ยังไม่แตะ FSM |
| **3** | U1–U5: ทำ Animator controller + clip ให้จบก่อน ยังไม่แตะโค้ด FSM | กด `Show`/`Hide` trigger มือใน Animator window แล้ว panel วิ่งถูก |
| **4** | C2: เพิ่ม `MinigamePanelAnimator` | log จาก `IntroFinished`/`OutroFinished` ขึ้นตรงจังหวะ |
| **5** | C1 + C3 + C4: เขียน `BaseMiniGame` ใหม่ทั้งไฟล์ | compile error จะชี้จุดที่ subclass ต้องแก้ให้เอง |
| **6** | C5 + C6: ย้าย 2 มินิเกมไป `OnEnter/OnTick/OnExit` | B1, B2, B5 หายไปเองเพราะเส้นทางเดียวแล้ว |
| **7** | C7 + U6–U8: manager + scene rewire | เล่น Shaking → Mixing → Shaking ซ้ำ 3 รอบ ต้องไม่มีอาการ B1 |
| **8** | C8: ลบ `PanelSlider` + `Easing*` | compile ผ่าน = ยืนยันว่าไม่มีใครใช้จริง |

**Regression checklist ที่ต้องผ่านทุก step ตั้งแต่ 5 เป็นต้นไป**

- [ ] เริ่ม Shaking → panel เข้า → ชนะ → panel ออก → `OnEndedGame` ยิง **ครั้งเดียว**
- [ ] เริ่ม Mixing รอบที่ 2 → เล่นท่าเข้า ไม่ใช่ท่าออก (B1)
- [ ] `OnGameEnd(true)` ยิงตอน Outro **จบ** ไม่ใช่ตอนเริ่ม (B2)
- [ ] คลิกระหว่าง Intro/Outro ไม่มีผลกับเกม
- [ ] hotkey `V` / `R` / `B` ใน `HandleEditorHotkeys` ยังใช้ได้
- [ ] กดปุ่มปิดกลางเกม → Outro เล่น → กลับ Idle สะอาด เริ่มใหม่ได้

---

## 6. สิ่งที่ **ไม่** อยู่ใน scope นี้

- การต่อ `Bar410.GameFlow.MinigameState` (HSM) เข้ากับ `MinigameSystemManager` — อยู่ใน `Bar410_Minigame_Integration_Plan.md` แล้ว **แต่แผนนี้ทำให้มันง่ายขึ้น**: หลังแก้เสร็จ `MinigameState.OnEnter()` จะ map ตรงกับ `StartMinigame(type)` และ `OnExit()` map ตรงกับ `MinigamePhase.Idle` แบบ 1:1
- `Enum_MiniGameType.Building` ที่ยังไม่มี implementation
- ตัว gameplay เอง (สูตรคำนวณ zone / needle / gauge) — ย้าย hook อย่างเดียว ไม่แตะสมการ
