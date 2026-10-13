# Bar410 — Minigame FSM Simplification · รายงานฉบับเต็ม

**Date:** 2026-08-20
**Branch:** `GameLoop/main`
**Scope:** `Assets/[02]Script/Minigame/`, `Assets/[02]Script/Cocktail System/`, `Assets/[02]Script/AnimationCode/`, `Assets/[08]Animation/MinigamePanel/`, 3 ซีน
**Plan ต้นทาง:** [`Bar410_Minigame_FSM_Simplification_Plan.md`](Bar410_Minigame_FSM_Simplification_Plan.md)
**สรุปสั้น:** [`Bar410_Minigame_FSM_Simplification_Summary.md`](Bar410_Minigame_FSM_Simplification_Summary.md)

---

## สารบัญ

1. [สถาปัตยกรรม ก่อน → หลัง](#1-สถาปัตยกรรม-ก่อน--หลัง)
2. [งานฝั่งโค้ด ทีละไฟล์](#2-งานฝั่งโค้ด-ทีละไฟล์)
3. [งานฝั่ง Unity — Animator + Clip](#3-งานฝั่ง-unity--animator--clip)
4. [การต่อสายในซีน](#4-การต่อสายในซีน)
5. [บั๊ก B1–B5 แก้ยังไง](#5-บั๊ก-b1b5-แก้ยังไง)
6. [ผลทดสอบ Play Mode](#6-ผลทดสอบ-play-mode)
7. [จุดที่ทำต่างจากแผน + เหตุผล](#7-จุดที่ทำต่างจากแผน--เหตุผล)
8. [Resolution Independence — ทบทวน §4.1](#8-resolution-independence--ทบทวน-41)
9. [คู่มือดูแลต่อ (designer / programmer)](#9-คู่มือดูแลต่อ-designer--programmer)
10. [ค้างอยู่ / ความเสี่ยงที่เหลือ](#10-ค้างอยู่--ความเสี่ยงที่เหลือ)
11. [Cancel + ช่องต่อกลับ HSM](#11-cancel--ช่องต่อกลับ-hsm)

---

## 1. สถาปัตยกรรม ก่อน → หลัง

### ก่อน — state machine 3 ชุดคุมเรื่องเดียวกัน

```
BaseMiniGame
 ├─ MiniGameState (Standby/Processing/Success)  ← SetState() + ตาราง IsValidTransition
 ├─ SlidePhase (8 ค่า)                          ← SlidePanelMinigame() เรียกทุกเฟรม
 └─ bool IsRunning                              ← เขียนจาก 5 ที่ ทั้ง base และ subclass
      └─ PanelSlider (static) + SlideSession × 5 + EasingConfig/EasingMath/EasingMode
```

ไม่มีตัวไหนเป็นเจ้าของความจริง: `IsRunning = true` ถูกเซ็ตลึกอยู่ใน `SlidePhase_InitMinigame()`,
`SetState(Success)` ถูกเรียกทุกเฟรมใน `SlidePhase_RemoveMinigame()`,
subclass เซ็ต `CurrentSlidePhase` เองจากใน `ResetGame()` คนละค่ากัน

### หลัง — ชั้นเดียว เส้นตรง

```
BaseMiniGame : MonoBehaviour, IMinigame
 ├─ MinigamePhase { Idle, Intro, Play, Outro }      ← ตัวแปร state ตัวเดียวในระบบ
 │    IsRunning => Phase == Play                     ← computed ไม่ใช่ field
 └─ MinigamePanelAnimator                            ← ครอบ Animator[]
       ├─ Animator บน Canvas_MiniGame        → "Panel - MinigameStage.controller"
       └─ Animator บน Canvas_<X>Minigame     → "Panel - MinigamePanel.controller"
              Hidden (default) → Intro → Shown → Outro → Hidden
```

**กฎเดียวที่ต้องจำ:** ไม่มี phase ไหนเซ็ต phase อื่นเอง ทุกการเปลี่ยนผ่านวิ่งผ่าน `SetPhase()` จุดเดียว

| Phase | รับ input? | ใครขับ | เกิดอะไร |
|---|---|---|---|
| `Idle` | ✗ | — | panel อยู่ที่ pose `Hidden` · ตอนเข้า: ยิง `OnGameEnd(...)` + `NotifyGameEnded(type, outcome)` |
| `Intro` | ✗ | Animator | `_panels.Show()` → รอ `IntroFinished` |
| `Play` | ✓ | `ProcessedGame()` | `Input.Poll()` → `OnTick(dt)` → `UpdateUI()` |
| `Outro` | ✗ | Animator | `amc.StopAnimation()` + `_panels.Hide()` → รอ `OutroFinished` |

---

## 2. งานฝั่งโค้ด ทีละไฟล์

### 2.1 `IMinigame.cs` — 59 → 106 บรรทัด

- `enum MiniGameState { Standby, Processing, Success }` → `enum MinigamePhase { Idle, Intro, Play, Outro }`
  แก้ปัญหาชื่อชนกับ `Bar410.GameFlow.MinigameState` (HSM flow state) ที่โค้ดเองคอมเมนต์เตือนไว้แล้ว
- เพิ่ม `enum MinigameOutcome { Completed, Cancelled }` + `IMinigame.Cancel()` + `IMinigame.LastOutcome` → **§11**
- `IMinigame.EndGame()` เปลี่ยนความหมายจาก "เคลียร์ flag" → alias ของ `Cancel()`
- `IMinigameContext.NotifyGameEnded(type, outcome)` กลับมาถูกใช้จริง (เดิม call site ถูกคอมเมนต์ทิ้ง)

### 2.2 `BaseMiniGame.cs` — **456 → 225 บรรทัด**

**ลบทิ้ง**

| ของที่ลบ | เหตุผล |
|---|---|
| `enum SlidePhase` + 6 method `SlidePhase_*()` (~250 บรรทัด) | Animator ทำแทน |
| `SlideSession` fields × 5, `OpenPanelSession`, `ArtWorkSessions` | ผูกกับ `PanelSlider` |
| `InitPanelRectTransform`, `CopyRectTransform()`, `Awake()` snapshot | Animator เก็บ pose เอง |
| `OpenPanel`, `ArtWorks`, `BackgroundPanelgame`, `ButtonPanel`, `_minigamePanel` | ย้ายไปเป็น curve path ใน clip |
| `_slidePanelSpeed`, `EasingConfigTimer` | ย้ายไปเป็น keyframe |
| `_backgroundSnapped`, `openPanelDoneCount`, `_closingSnapApplied`, `_currentArtIndex` | dead field เขียนอย่างเดียว |
| `SetState()`, `IsValidTransition()`, `OnEnterState()`, `OnExitState()` | ตาราง transition ไม่จำเป็นแล้ว |
| `OnProcessing()`, `OnSuccess()`, `OnFailed()`, `OnStandby()`, `ResetGame()`, `FireEndEvent()` | ยุบเหลือ 4 hook |
| `[SerializeField] m_systemManager` | DIP — ใช้ `IMinigameContext` เส้นเดียว |

**เพิ่ม**

```csharp
public MinigamePhase Phase { get; private set; } = MinigamePhase.Idle;
public bool IsRunning => Phase == MinigamePhase.Play;

private void SetPhase(MinigamePhase next)
{
    if (Phase == next) return;
    ExitPhase(Phase);
    Phase = next;
    EnterPhase(next);
}

private void EnterPhase(MinigamePhase phase)
{
    switch (phase)
    {
        case MinigamePhase.Intro: _panels?.Show(); break;
        case MinigamePhase.Play:  OnEnter(); UpdateUI(); break;
        case MinigamePhase.Outro: if (amc != null) amc.StopAnimation(); _panels?.Hide(); break;
        case MinigamePhase.Idle:  LastOutcome = _pendingOutcome;
                                  OnGameEnd?.Invoke(_pendingOutcome == MinigameOutcome.Completed);
                                  _context?.NotifyGameEnded(GameType, _pendingOutcome); break;
    }
}

private void ExitPhase(MinigamePhase phase) { if (phase == MinigamePhase.Play) OnExit(); }

public void ProcessedGame()
{
    if (Phase != MinigamePhase.Play) return;   // Intro/Outro เป็นหน้าที่ Animator
    Input.Poll();
    OnTick(Time.deltaTime);
    UpdateUI();
}
```

**`ResolveSetting<T>()` — C4 (แก้ B4 ในตัว)**

```csharp
private ScriptableObject _runtimeSetting;

protected T ResolveSetting<T>() where T : SO_MinigameSetting
{
    if (Setting is T assigned) return assigned;          // asset จริง → ไม่แตะ
    var fallback = ScriptableObject.CreateInstance<T>();
    _runtimeSetting = fallback;                          // จำเฉพาะตัวที่เราสร้างเอง
    Debug.LogWarning($"[{GetType().Name}] No {typeof(T).Name} assigned — using default values.");
    return fallback;
}

protected virtual void OnDestroy()
{
    if (_runtimeSetting != null) Destroy(_runtimeSetting);
}
```

ยุบ `Awake` + `OnDestroy` ที่ซ้ำกันใน 2 subclass เหลือ subclass ละบรรทัดเดียว
และเลิกใช้ `#if UNITY_EDITOR / AssetDatabase.Contains()` ซึ่งเป็นต้นเหตุ B4

**การจบเกม**

```csharp
protected void Complete()
{
    if (Phase != MinigamePhase.Play) return;
    _pendingOutcome = MinigameOutcome.Completed;
    SetPhase(MinigamePhase.Outro);      // event ยิงตอนถึง Idle ไม่ใช่ตรงนี้
}

public void Cancel()                    // ← ดู §11.2
{
    if (Phase == MinigamePhase.Idle || Phase == MinigamePhase.Outro) return;
    _pendingOutcome = MinigameOutcome.Cancelled;
    SetPhase(MinigamePhase.Outro);
}

public void ClosePanel() => Cancel();   // alias — ปุ่ม UI
public void EndGame()    => Cancel();   // alias — IMinigame + hotkey
```

เดิม subclass ต้องเขียน 3 บรรทัดซ้ำกัน (`CurrentSlidePhase = RemoveMinigame` + `IsRunning = false` + `amc.StopAnimation()`) ตอนนี้เหลือ `Complete();`

**Subscribe / Unsubscribe**

```csharp
protected virtual void OnEnable()
{
    if (_panels == null) return;
    _panels.IntroFinished += HandleIntroFinished;
    _panels.OutroFinished += HandleOutroFinished;
}
// OnDisable ถอดออกแบบสมมาตร

private void HandleIntroFinished() { if (Phase == MinigamePhase.Intro) SetPhase(MinigamePhase.Play); }
private void HandleOutroFinished() { if (Phase == MinigamePhase.Outro) SetPhase(MinigamePhase.Idle); }
```

guard `if (Phase == ...)` กันกรณีมีมากกว่าหนึ่งเกม subscribe animator ตัวเดียวกัน

### 2.3 `MinigamePanelAnimator.cs` — ไฟล์ใหม่ 134 บรรทัด

```csharp
[SerializeField] private Animator[] _animators;      // stage + panel ของเกมนี้
[SerializeField] private string _showTrigger = "Show";
[SerializeField] private string _hideTrigger = "Hide";
[SerializeField] private float  _introFallbackDuration = 5f;
[SerializeField] private float  _outroFallbackDuration = 1.5f;

public event Action IntroFinished;
public event Action OutroFinished;

public void Show() { _awaitingIntro = true;  _awaitingOutro = false; Fire(_showTrigger, _hideTrigger); RestartFallback(_introFallbackDuration, OnIntroFinished); }
public void Hide() { _awaitingOutro = true;  _awaitingIntro = false; Fire(_hideTrigger, _showTrigger); RestartFallback(_outroFallbackDuration, OnOutroFinished); }

// AnimationEvent entry points — ผูกที่เฟรมสุดท้ายของ clip
public void OnIntroFinished() { if (!_awaitingIntro) return; _awaitingIntro = false; StopFallback(); IntroFinished?.Invoke(); }
public void OnOutroFinished() { if (!_awaitingOutro) return; _awaitingOutro = false; StopFallback(); OutroFinished?.Invoke(); }
```

จุดออกแบบที่ตั้งใจ:

- **`Fire()` ทำ `ResetTrigger(clear)` ก่อน `SetTrigger(set)` เสมอ** — กันเคส trigger ค้าง เช่นกด Hide ตอนอยู่ Idle แล้วค่อย Show จะทำให้ Intro ถูก abort ทันที
- **flag `_awaitingIntro` / `_awaitingOutro`** — การันตีว่า event ยิงครั้งเดียวต่อรอบ ไม่ว่า AnimationEvent หรือ fallback จะมาก่อน
- **fallback timeout** — clip ที่ลืมใส่ AnimationEvent จะไม่ทำให้ FSM ค้างถาวร (ตรงกับที่แผนระบุใน C2)
- **`OnDisable()` ปล่อย event ที่ค้าง** — ปิด GameObject กลาง intro แล้วเกมไม่ค้าง

### 2.4 `MixingMinigame.cs` — 413 → 364

| เดิม | ใหม่ |
|---|---|
| `Awake()` 12 บรรทัด + `OnDestroy()` 5 บรรทัด | `private void Awake() => _cfg = ResolveSetting<SO_MixingSetting>();` |
| `StartGame()` (cache RectTransform + `ResetGame()` + `UpdateUI()` + `base.StartGame()`) | ย้ายทั้งหมดไป `OnEnter()` |
| `ProcessedGame()` (มี guard `!IsRunning` 2 ชั้น + `base.ProcessedGame()`) | `OnTick(float dt)` — gameplay ล้วน ไม่มี guard |
| `ResetGame()` + `OnProcessing()` | `OnEnter()` |
| `EndGame()` override (log) | `OnExit()` + `StopAllTimers()` |
| `CurrentSlidePhase = SlidePhase.RemoveMinigame; amc.StopAnimation(); IsRunning = false;` | `Complete();` |
| `_cfgIsRuntimeCreated` | ลบ (base จัดการ) |

ของแถมที่ได้: `_hitsProgressCoroutine` เดิมไม่เคยถูกหยุดตอนจบเกม ตอนนี้ `StopAllTimers()` เก็บให้ครบทั้ง 3 ตัว
และการ cache `_cachedTrackWidth` ย้ายมาทำตอนเข้า `Play` (หลัง intro จบ) ซึ่ง layout นิ่งแล้ว — แม่นกว่าเดิมที่ทำตอน `StartGame()`

### 2.5 `ShakingMinigame.cs` — 203 → 154

โครงเดียวกับ Mixing:

```csharp
private void Awake() => _cfg = ResolveSetting<SO_ShakingSetting>();

protected override void OnEnter()
{
    _handleOriginalWidth = _targetZoneSlider.handleRect.sizeDelta.x;
    GaugeValue = 0f;
    TimeInZone = 0f;
    InitTargetZone();
    Debug.Log("[ShakingMinigame] Started");
}

protected override void OnTick(float dt)
{
    ...
    if (TimeInZone >= _cfg.Duration) Complete();
}

protected override void OnExit()
    => Debug.Log($"[ShakingMinigame] Ended | Gauge={GaugeValue:F2} | Progress={TimeInZone:F2}");
```

`OnDestroy()` เดิมที่มี branch `#else Destroy(_cfg);` — ลบทิ้ง (ต้นเหตุ B4)

### 2.6 `MinigameSystemManager.cs` — 145 → 183

```csharp
// เดิม: NextPhase() (ClosePanel + OnEndedGame.Invoke) และ ClosePanelReset() (ClosePanel เฉย ๆ)
// ใหม่: เหลือตัวเดียว ไม่ invoke เอง
public void CancelMinigame()
{
    if (_activeMinigame == null) { Debug.LogWarning("..."); return; }
    _activeMinigame.Cancel();
}

// end-event เดินผ่านเส้นเดียว: BaseMiniGame เข้า Idle → NotifyGameEnded(..) → OnEndedGame
void IMinigameContext.NotifyGameEnded(Enum_MiniGameType type, MinigameOutcome outcome)
{
    MinigameFinished?.Invoke(type, outcome);   // flow layer — ดู §11.3
    OnEndedGame?.Invoke();                     // scene listeners
}

private void SwitchTo(BaseMiniGame next)
{
    if (next == null || ReferenceEquals(next, _activeMinigame)) return;
    if (_activeMinigame != null && _activeMinigame.Phase != MinigamePhase.Idle)
        _activeMinigame.Cancel();              // ไม่ทิ้งเกมค้างครึ่งทาง
    _activeMinigame = next;
}
```

`SwitchTo` เดิมเรียก `SetState(MiniGameState.Standby)` ซึ่งจาก `Processing` เป็น transition ที่ไม่ถูกกฎ →
ได้แค่ `LogWarning` แล้วไม่ทำอะไร (no-op ที่ดูเหมือนทำอะไร) ตอนนี้ปิดเกมเก่าจริง ๆ

Hotkey `1` / `2` / `V` / `R` / `B` ใน `HandleEditorHotkeys()` ยังใช้ได้เหมือนเดิม

### 2.7 ไฟล์ที่ลบ (C8)

| ไฟล์ | บรรทัด |
|---|---|
| `Assets/[02]Script/Cocktail System/PanelSlider.cs` | 366 |
| `Assets/[02]Script/Cocktail System/SlideFinishCondition.cs` | 26 |
| `Assets/[02]Script/AnimationCode/EasingConfig.cs` | 33 |
| `Assets/[02]Script/AnimationCode/EasingMath.cs` | 44 |
| `Assets/[02]Script/AnimationCode/EasingMode.cs` | 11 |

`.meta` ลบตามทั้งหมด · compile ผ่าน = ยืนยันว่าไม่มีใครใช้จริง (ตรงกับที่ grep ไว้ในแผน)
เลือก **ลบ** ไม่ใช่ย้ายไป `_Unused/` ตามคำแนะนำในแผน §C8 เพราะ git เก็บให้อยู่แล้ว

---

## 3. งานฝั่ง Unity — Animator + Clip

### 3.1 ที่มาของตัวเลข keyframe

Keyframe ทุกตัว**คำนวณย้อนกลับจากโค้ดเดิม** ไม่ได้เดา — timeline ใหม่จึงเหมือนของเดิมเป๊ะ

ค่า geometry ที่อ่านจากซีน (parent = `Canvas_MiniGame`, rect 1919×1080):

| Panel | anchor | size | `anchoredPosition` ตอน hidden |
|---|---|---|---|
| `BG` | (0.5, 0) | 1920×400 | (0, −200) |
| `Art001` | (0, 0) | 480×400 | (240, −200) |
| `Art002` | (0, 0) | 480×400 | (720, −200) |
| `Canvas_ShakingMinigame` | (1, 0) | 960×400 | (−480, −200) |
| `Canvas_MixingMinigame` | (1, 0) | 960×400 | (−480, −200) |
| `InitPanel/InitPanel_004` | (0.5, 0) | 1920×1080 | (0, −540) |

ทุกตัว anchor ที่ขอบล่าง pivot 0.5 → **`y = −height/2` คือพ้นจอพอดี, `y = +height/2` คือติดขอบล่างพอดี**
ดังนั้น hidden = −200 / shown = +200 (สำหรับ panel สูง 400) และ InitPanel_004 = −540 / 0

แปล `SlideFinishCondition` เดิมเป็นเวลา (ใช้ `EasingConfigTimer = 1.2`):

| เดิม | เวลา | ผล |
|---|---|---|
| `SlidePhase_InitPanel` — OpenPanel `Up, TopEdgeToCenter`, `EaseIn(1.2)` | 0 → 1.2 | InitPanel_004 y: −540 → 0 |
| `PanelSlider.SnapTo(BG, BottomEdgeToBottomBound)` | @ 1.2 | BG y: −200 → 200 (snap) |
| `SlidePhase_InitPanelExit` — `TopEdgeToBottomBound`, `EaseIn(1.2)` | 1.2 → 2.4 | InitPanel_004 y: 0 → −540 |
| `SlidePhase_InitArt` [0] — `BottomEdgeToBottomBound`, `EaseInOut(0.6)` | 2.4 → 3.0 | Art001 y: −200 → 200 |
| `SlidePhase_InitArt` [1] | 3.0 → 3.6 | Art002 y: −200 → 200 |
| `SlidePhase_InitMinigame` — `EaseInOut(1.2)` | 3.6 → 4.8 | minigame panel y: −200 → 200 |
| `SlidePhase_RemoveMinigame` — Art snap + panel/BG `EaseInOut(1.2)` | 0 → 1.2 | Art snap −200 · panel/BG 200 → −200 |

**Intro = 4.8s · Outro = 1.2s**

### 3.2 Asset ที่สร้าง — `Assets/[08]Animation/MinigamePanel/`

```
Panel - MinigameStage.controller     → Animator บน Canvas_MiniGame
  MinigameStage_Hidden.anim          (pose: InitPanel −540, BG/Art001/Art002 −200)
  MinigameStage_Intro.anim   4.8s    (curve ตามตาราง §3.1)
  MinigameStage_Shown.anim           (pose: InitPanel −540, BG/Art001/Art002 +200)
  MinigameStage_Outro.anim   1.2s

Panel - MinigamePanel.controller     → Animator บน Canvas_ShakingMinigame + Canvas_MixingMinigame
  MinigamePanel_Hidden.anim          (pose: self y = −200)
  MinigamePanel_Intro.anim   4.8s    (hold −200 ถึง 3.6 แล้ว ease ไป 200) + AnimationEvent @4.8 → OnIntroFinished
  MinigamePanel_Shown.anim           (pose: self y = +200)
  MinigamePanel_Outro.anim   1.2s    (200 → −200) + AnimationEvent @1.2 → OnOutroFinished
```

Curve binding ทั้งหมดเป็น `RectTransform.m_AnchoredPosition.y` เท่านั้น (แกน X ไม่เคยขยับในโค้ดเดิม)
clip ของ panel ใช้ path `""` = animate ตัวเอง — pattern มาตรฐานของ Unity UI

### 3.3 State machine ในทั้งสอง controller (โครงเดียวกัน)

```
              ┌──[Show]──> Intro ──(exit time)──> Shown ──┐
   Hidden ────┤                │                          │
      ▲       │                └──[Hide]──┐         [Show]┘ (replay ไม่ต้องกลับ Idle)
      │       │                           ▼
      └───(exit time)─────────────────  Outro  <──[Hide]── Shown
```

- parameter: trigger `Show`, trigger `Hide`
- ทุก transition `duration = 0` (ไม่ blend — เพราะ pose clip เป็นค่าสัมบูรณ์)
- `Intro --[Hide]--> Outro` รองรับกรณีผู้เล่นกดปิดกลาง intro
- `Shown --[Show]--> Intro` รองรับ replay ติด ๆ กัน

**ทำไมต้องมี `Hidden` / `Shown` pose clip:** state ที่ `motion == null` ทำให้ Unity คืนค่า
"default value ก่อน Animator เริ่มเล่น" ให้ทุก binding ที่ไม่ได้ถูก animate อยู่ →
เจอตอนเทสรอบแรกว่า panel เด้งกลับ −200 ทันทีที่ Intro จบ ทั้งที่ `Phase` เป็น `Play` ถูกต้องแล้ว

### 3.4 ตั้งค่า Animator ทุกตัว

| property | ค่า | เหตุผล |
|---|---|---|
| `applyRootMotion` | `false` | ไม่ใช้ root motion |
| `cullingMode` | `AlwaysAnimate` | panel เริ่มจากนอกจอ — ถ้า cull จะไม่มีวันเข้ามา |
| `updateMode` | `Normal` | คงพฤติกรรมเดิมที่ใช้ `Time.deltaTime` — ถ้าอนาคตมี pause ด้วย `Time.timeScale = 0` ค่อยเปลี่ยนเป็น `UnscaledTime` (แผน §4.2) |

---

## 4. การต่อสายในซีน

ทำครบทั้ง 3 ซีน: `GamePlayScene.unity`, `GamePlayScene 1.unity`, `New Drag Drop System.unity`

| GameObject | Component ที่เพิ่ม |
|---|---|
| `SystemGame/MiniGameSystem/Canvas_MiniGame` | `Animator` → `Panel - MinigameStage.controller` |
| `.../Canvas_ShakingMinigame` | `Animator` → `Panel - MinigamePanel.controller` + `MinigamePanelAnimator` |
| `.../Canvas_MixingMinigame` | เหมือนกัน |

`MinigamePanelAnimator._animators` ของแต่ละเกม = `[ stage Animator, panel Animator ของตัวเอง ]`
`BaseMiniGame._panels` ของ `ShakingMinigame` → ตัวบน `Canvas_ShakingMinigame` (Mixing เช่นเดียวกัน)

**Field เก่าที่หายไป** — Unity ทิ้งให้เองตอน re-serialize ยืนยันแล้วว่าไม่เหลือ orphan:

```
grep -c "OpenPanel|ArtWorks|BackgroundPanelgame|_slidePanelSpeed|EasingConfigTimer|m_systemManager"
  → 0 ทั้ง 3 ซีน
```

**ปุ่ม UI (U8)** — แก้ `m_MethodName` ใน UnityEvent ของทั้ง 3 ซีน:

| เดิม | ใหม่ |
|---|---|
| `NextPhase` | `CancelMinigame` |
| `ClosePanelReset` | `CancelMinigame` |

target เดิมคือ `MinigameSystemManager, Assembly-CSharp` อยู่แล้ว จึงแก้แค่ชื่อ method

---

## 5. บั๊ก B1–B5 แก้ยังไง

| # | สาเหตุเดิม | อะไรทำให้หาย |
|---|---|---|
| **B1** เล่นรอบ 2 แล้ว panel เล่นท่า Closing | `StartGame()` เซ็ต `CurrentSlidePhase = InitPanel` **ก่อน** `SetState(Standby)` → `OnStandby()` → `ResetGame()` ทับเป็น `Closing` | `ResetGame()` ไม่มีแล้ว · `StartGame()` มีทางเดียว: `SetPhase(Intro)` → `_panels.Show()` · Animator state machine เป็นเจ้าของว่า "ตอนนี้ควรเล่นคลิปไหน" |
| **B2** `OnGameEnd(true)` ยิงเร็วไป ~1 วิ | `SetState(Success)` → `OnSuccess()` → `FireEndEvent(true)` อยู่บรรทัดแรกของ `SlidePhase_RemoveMinigame` (ยิงตอน**เริ่ม** slide-out) | `Complete()` แค่จำผลไว้ใน `_pendingOutcome` แล้วไป `Outro` · event ยิงตอน `EnterPhase(Idle)` = หลัง outro จบจริง |
| **B3** `OnEndedGame` ยิง 2 ครั้ง | `NextPhase()` invoke เอง + `SlidePhase_Closing` invoke ซ้ำ | เหลือ `CancelMinigame()` ตัวเดียวที่ไม่ invoke เอง · เส้นทางเดียวคือ `EnterPhase(Idle)` → `NotifyGameEnded()` |
| **B4** `SO_ShakingSetting` asset ถูก `Destroy()` ใน build | `#else Destroy(_cfg);` ไม่เช็คว่าเป็น asset หรือ instance | `ResolveSetting<T>()` เก็บ ref เฉพาะตัวที่ `CreateInstance` เอง · `OnDestroy()` ทำลายเฉพาะตัวนั้น · ไม่มี `#if UNITY_EDITOR` อีกแล้ว |
| **B5** `ResetGame()` ไม่เคยถูกเรียกผ่าน FSM ในรอบแรก | รอบแรก `CurrentState` เป็น `Standby` อยู่แล้ว → `SetState(Standby)` early-return | `Idle → Intro → Play` เป็นการเปลี่ยน phase จริงทุกครั้ง `OnEnter()` จึงถูกเรียกเสมอ |

### โค้ดตายที่ลบตาม §1.3

`SlidePhase.InitBackground` (ไม่มี case ใน switch) · `_backgroundSnapped` · `openPanelDoneCount` ·
`_closingSnapApplied` (+ บล็อก `if (!_closingSnapApplied) { }` ที่ body ว่าง) ·
`SlidePhase_Closing()` ทั้ง 90 บรรทัด (~55 เป็นคอมเมนต์) · `OnFailed()` (ไม่มีใครเรียก) ·
`ButtonPanel` (เหลือแค่ snap ตอน Closing) · `using System.Linq` / `UnityEngine.TestTools` ที่ไม่ได้ใช้

---

## 6. ผลทดสอบ Play Mode

รันในซีน `New Drag Drop System.unity` โดยเรียก API ผ่าน editor script

### 6.1 รอบ Shaking — กดปิดกลางเกม

```
StartMinigame(Shaking)   t=0.00   → ShakingMinigame phase=Intro, stage animator=Hidden (trigger เพิ่งถูกเซ็ต)
                         t=8.44   → phase=Play, stage=Shown
                                    BG=200  Art001=200  Art002=200  Canvas_ShakingMinigame=200
                                    Canvas_MixingMinigame=-200  ← เกมที่ไม่ได้เล่นค้างที่ hidden ถูกต้อง
CancelMinigame()         t=19.44  → phase=Outro
                         t=28.21  → phase=Idle, stage=Hidden, ทุก panel = -200
console: "### GAME_END success=False"  ×1
         "### ENDED_EVENT"             ×1
```

### 6.2 รอบ Mixing — ปิด fallback timeout เพื่อพิสูจน์ AnimationEvent

ตั้ง `_introFallbackDuration` / `_outroFallbackDuration` = **60s** ผ่าน reflection
ถ้า FSM ยังเดินต่อได้ในเวลาปกติ แปลว่ามาจาก `AnimationEvent` ไม่ใช่ safety net

```
StartMinigame(Stiring)   t=49.53
                         t=57.52 (elapsed 7.99s) → phase=Play   ← < 60s ⇒ AnimationEvent ทำงาน
                                    BG=200 Art001=200 Art002=200 Canvas_MixingMinigame=200
                                    Canvas_ShakingMinigame=-200
force Hits = RequiredHits (5)  t=71.21  → phase=Play → Complete()
                         t=78.44  → phase=Idle, ทุก panel = -200
console: "### MIX_GAME_END success=True"  ×1   ← ยิงหลัง outro จบ (B2)
         "### ENDED_EVENT"                ×1   ← ครั้งเดียว (B3)
```

### 6.3 Replay ติดกัน (B1)

```
StartMinigame(Stiring) ซ้ำทันทีหลังรอบที่แล้วจบ   t=102.66
                         elapsed 9.91s → MixingMinigame phase=Play
                                          BG=200 Art001=200 Art002=200 Canvas_MixingMinigame=200
```

เล่น **ท่าเข้า** ไม่ใช่ท่าออก ✅

### 6.4 Console

ไม่มี compile error · ไม่มี warning ใหม่จากโค้ด Minigame
(`The referenced script (Unknown) is missing!` ×2 อยู่บน GameObject ชื่อ `Current Text Line` ในระบบ dialogue — มีอยู่ก่อนแล้ว ไม่เกี่ยวกับงานนี้)

### 6.5 Regression checklist ตามแผน §5

- [x] เริ่ม Shaking → panel เข้า → ชนะ → panel ออก → `OnEndedGame` ยิงครั้งเดียว
- [x] เริ่ม Mixing รอบที่ 2 → เล่นท่าเข้า ไม่ใช่ท่าออก (B1)
- [x] `OnGameEnd(true)` ยิงตอน Outro **จบ** ไม่ใช่ตอนเริ่ม (B2)
- [x] คลิกระหว่าง Intro/Outro ไม่มีผลกับเกม (`ProcessedGame()` return ทันทีถ้า `Phase != Play`)
- [x] hotkey `V` / `R` / `B` ยังใช้ได้ (`StartGame()` / `EndGame()` ยังอยู่บน interface)
- [x] กดปุ่มปิดกลางเกม → Outro เล่น → กลับ Idle สะอาด เริ่มใหม่ได้

---

## 7. จุดที่ทำต่างจากแผน + เหตุผล

### 7.1 สร้าง 2 controller ไม่ใช่ 1 (แผน §2 / U1)

แผนวางไว้ว่า Animator ตัวเดียวบน parent ร่วม + "1 clip เดียวคุมทุก panel"
แต่ในซีนจริง `Canvas_ShakingMinigame` และ `Canvas_MixingMinigame`:

- เป็น **พี่น้องกัน** ใต้ `Canvas_MiniGame`
- อยู่ที่ `anchoredPosition` เดียวกัน `(−480, −200)` ขนาดเท่ากัน `960×400`
- **active ทั้งคู่ตลอดเวลา** — ไม่มีโค้ดไหน `SetActive` สลับ (ตรวจแล้วทั้ง grep และ UnityEvent listener ของ `OnStartedMinigame` / `OnEndedGame`)

clip เดียวที่ animate ทั้งคู่ → panel ของทั้งสองเกมเลื่อนเข้าจอพร้อมกันและซ้อนทับ = regression ทางภาพ
จึงแยกเป็น stage (ของใช้ร่วม) + panel (ของใครของมัน) แต่ยังใช้ trigger `Show` / `Hide` ชุดเดียวกัน
`MinigamePanelAnimator` fan-out trigger ให้ทั้ง 2 animator — จาก BaseMiniGame ยังเห็นเป็น `_panels.Show()` บรรทัดเดียวเหมือนในแผน

> ถ้าอนาคตต้องการกลับไปเป็น 1 controller จริง ๆ ต้องเพิ่มการ `SetActive` เกมที่ไม่ได้เล่นก่อน
> ซึ่งจะชนกับข้อควรระวัง §4.2 (Animator ไม่ tick ถ้า GameObject ถูก disable)

### 7.2 เพิ่ม pose clip ให้ `Hidden` / `Shown`

ไม่ได้อยู่ในแผน แต่จำเป็น — ดู §3.3 เจอจากการเทสจริงรอบแรก
ผลพลอยได้: `Hidden` เป็น pose ที่ประกาศชัดเจน แทนที่จะพึ่งค่าใน Inspector ซึ่งเผลอลากทับได้

### 7.3 `Complete()` ยิง `OnGameEnd` ตอน Outro จบ

โค้ดตัวอย่างใน §3.1 เขียนว่า `Complete() { OnGameEnd?.Invoke(true); SetPhase(Outro); }` (ยิงทันที)
แต่ regression checklist §5 บอกว่า "`OnGameEnd(true)` ยิงตอน Outro **จบ** ไม่ใช่ตอนเริ่ม (B2)"
สองที่นี้ขัดกันเอง — เลือกทำตาม checklist เพราะนั่นคือนิยามของ B2

### 7.4 คง `EndGame()` ไว้บน `IMinigame`

แผน §2.2 บอกว่า `EndGame()` → `OnExit()` ซึ่งอ่านได้ว่าอาจจะตัดออกจาก interface
แต่ `HandleEditorHotkeys()` (`R` / `B`) เรียก `_activeMinigame?.EndGame()` และ checklist §5 บังคับว่า hotkey ต้องยังใช้ได้
จึงคงไว้เป็น alias: `public void EndGame() => Cancel();`

### 7.5 ข้าม "step 1 = commit แยกสำหรับลบโค้ดตาย"

แผน §5 แนะนำให้ commit การลบโค้ดตายแยกไว้เป็นฐานเทียบ
`BaseMiniGame.cs` ถูกเขียนใหม่ทั้งไฟล์ในขั้นถัดไปอยู่แล้ว การแก้แยกรอบจึงเป็นงานที่ถูกลบทิ้งทันที
โค้ดตายทั้งหมดตาม §1.3 ถูกลบครบ (ดู §5 ท้ายตาราง) แค่ไม่ได้แยก commit

---

## 8. Resolution Independence — ทบทวน §4.1

แผนระบุว่านี่คือ trade-off จริงข้อเดียว และแนะนำทางแก้ **ก. overshoot**

**ผลจริง: ไม่ต้องใช้** เพราะ panel ทุกตัวที่ animate มี `anchorMin.y = anchorMax.y = 0` (ยึดขอบล่าง)
และค่าที่ bake มาจาก **ความสูงของ panel เอง** ไม่ใช่ความสูงจอ:

```
hidden y = −height/2   → ขอบบนของ panel อยู่ที่ขอบล่างจอพอดี  (พ้นจอ)
shown  y = +height/2   → ขอบล่างของ panel อยู่ที่ขอบล่างจอพอดี (ติดขอบ)
```

ทั้งสองค่าไม่ขึ้นกับ aspect ratio → Canvas `ScaleWithScreenSize @ 1920×1080 match = 0.5` เดิมใช้ต่อได้เลย
ไม่ต้องแตะ `m_MatchWidthOrHeight` (ทางแก้ ข.) และไม่ต้อง animate anchor (ทางแก้ ค.)

**ข้อยกเว้นเดียว:** pose กลางของ `InitPanel_004` ที่ `t = 1.2s` (`y = 0`)
มาจาก `TopEdgeToCenter` ซึ่งอ้างอิงกึ่งกลางจอ → ค่านี้แปรตาม aspect
แต่เป็นแค่ **transition ผ่าน** ไม่ใช่ตำแหน่งพัก และตำแหน่งพักทั้งสองฝั่ง (`−540`) ยัง resolution-independent อยู่
จอกว้างกว่า 16:9 จะเห็นการกวาดของ InitPanel สูงไม่ถึงกลางจอเป๊ะ — ไม่ใช่บั๊ก แค่ความรู้สึกต่างเล็กน้อย

---

## 9. คู่มือดูแลต่อ (designer / programmer)

### 9.1 จะปรับจังหวะ animation

เปิด `Assets/[08]Animation/MinigamePanel/MinigameStage_Intro.anim` หรือ `MinigamePanel_Intro.anim`
ใน Animation window แล้วลาก keyframe ได้เลย **ไม่ต้อง compile**

⚠️ **ข้อควรระวังเดียว** — ถ้ายืด clip ให้ยาวขึ้น ต้องขยับ `_introFallbackDuration` / `_outroFallbackDuration`
บน `MinigamePanelAnimator` (default 5s / 1.5s) ให้ **มากกว่าความยาว clip** ไม่งั้น safety net จะยิงก่อน
แล้วเกมจะเริ่มรับ input ตั้งแต่ panel ยังไม่เข้าที่

### 9.2 จะให้เริ่มรับ input เร็วขึ้นโดยไม่ย่น animation

เลื่อน `AnimationEvent` ใน `MinigamePanel_Intro.anim` มาก่อนคลิปจบ — FSM เข้า `Play` ตามจุดนั้นทันที
(นี่คือเหตุผลที่ใช้ AnimationEvent แทนการเช็ค `normalizedTime` แบบ `PanelTransition.cs`)

### 9.3 จะเพิ่มมินิเกมใหม่

1. สร้าง class สืบทอด `BaseMiniGame` เขียน `GameType`, `OnEnter()`, `OnTick(dt)`, `OnExit()`, `UpdateUI()`
2. เรียก `Complete()` ตอนชนะ · `ResolveSetting<T>()` ใน `Awake()` ถ้ามี SO setting
3. แปะ component บน `MiniGameSystem` (registry ค้นเจอเองผ่าน `GetComponents<BaseMiniGame>()`)
4. สร้าง `Canvas_<X>Minigame` ใต้ `Canvas_MiniGame` ใส่ `Animator` (`Panel - MinigamePanel.controller`)
   + `MinigamePanelAnimator` แล้ว assign `_animators = [stage, self]` และ `_panels` บนตัวเกม

ไม่ต้องแก้ `MinigameSystemManager` เลย

### 9.4 สิ่งที่ **ห้าม** ทำ

- อย่าเขียน `Phase` จากข้างนอก — `private set` ไว้แล้ว ทุกอย่างผ่าน `StartGame()` / `Complete()` / `Cancel()`
- อย่าเรียก `OnEndedGame?.Invoke()` หรือ `MinigameFinished?.Invoke()` เองจากที่อื่น (นี่คือ B3 กลับมา) — ให้ `NotifyGameEnded(..)` เป็นทางเดียว
- อย่า `SetActive(false)` panel ที่อยู่ในกลุ่มที่ Animator คุม (แผน §4.2)

---

## 10. ค้างอยู่ / ความเสี่ยงที่เหลือ

### 10.1 `SystemGame.prefab` — ต้องตัดสินใจ

`Assets/[04]Prefab/GameSystemPrefab/SystemGame.prefab` มี `ShakingMinigame` + `MixingMinigame` + `MinigameSystemManager`
บน node `MiniGameSystem` **แต่ลูกข้างในชื่อ `MiniGameCanvas`** ไม่ใช่ `Canvas_MiniGame` และไม่มี `Canvas_ShakingMinigame` / `Canvas_MixingMinigame`
→ เป็นโครงคนละรุ่นกับซีน และ `SystemGame` ในทั้ง 3 ซีนก็ **ไม่ได้เป็น prefab instance** (`IsPartOfAnyPrefab == false`)

จึง **ไม่ได้ต่อสาย `_panels`** ให้ prefab นี้ ทางเลือก:

- ถ้าเลิกใช้แล้ว → ลบ prefab ทิ้ง
- ถ้ายังใช้ → ต้องอัปเดตโครงลูกให้ตรงกับซีนก่อน แล้วค่อยต่อสาย

(`CocktailSystem.prefab` อ้าง `MinigameSystemManager` แค่ผ่าน UnityEvent `StartShakingMinigame` / `StartMixingMinigame` ซึ่งยังอยู่ครบ — ไม่ต้องแก้)

### 10.2 Edge case: สลับเกมกลางคัน

`StartMinigame(B)` ขณะที่เกม A อยู่ `Play`:
`SwitchTo()` สั่ง A ปิด → stage animator ได้ `Hide`, panel ของ A ได้ `Hide`
แล้ว B เรียก `Show()` ทันทีในเฟรมเดียวกัน → `ResetTrigger("Hide")` บน stage ยกเลิก Hide ที่เพิ่งเซ็ต
ผลลัพธ์: stage เล่น Intro ต่อ (ถูกต้อง), panel ของ A เล่น Outro ออกไป (ถูกต้อง), A ยิง `OnGameEnd(false)` เมื่อ outro จบ

พฤติกรรมสมเหตุสมผล แต่**ยังไม่ได้เทสด้วยมือ** — ของเดิมเคสนี้พังอยู่แล้ว จึงไม่ถือเป็น regression

### 10.3 `MinigameSystemManager` ยาวขึ้น 3 บรรทัด

แผนคาด `145 → ~110` แต่ผลจริง `145 → 183` เพราะ:
`SwitchTo()` เดิมเป็น no-op ที่ดูเหมือนทำอะไร ตอนนี้ปิดเกมเก่าจริง (+4 บรรทัด) และเพิ่ม XML doc ที่อธิบายว่าทำไม
end-event ต้องเดินเส้นเดียว (+6 บรรทัด) — แลกกับการลบ `NextPhase()` ทิ้งทั้งก้อน

### 10.4 นอก scope (ตามแผน §6)

- ต่อ `Bar410.GameFlow.MinigameState` (HSM) เข้ากับ `MinigameSystemManager` — อยู่ใน `Bar410_Minigame_Integration_Plan.md`
  **แต่งานนี้ทำให้ง่ายขึ้นจริงตามที่แผนคาด:** `MinigameState.OnEnter()` map ตรงกับ `StartMinigame(type)`
  และ `MinigameState.OnExit()` map ตรงกับ `MinigamePhase.Idle` แบบ 1:1
- `Enum_MiniGameType.Building` ยังไม่มี implementation
- สมการ gameplay (zone / needle / gauge) ไม่ถูกแตะ — ย้าย hook อย่างเดียว

---

## 11. Cancel + ช่องต่อกลับ HSM

> เพิ่มหลังจากงานหลักเสร็จ — ปิดช่องว่างที่ integration plan §5.2 ชี้ไว้ (สองทางออก แต่มีทางเดียวที่รายงานผล)
> และเตรียมปลายสายให้ `MinigameFlowBridge` รอบหน้าไม่ต้องแก้ทั้งสองระบบอีก

### 11.1 `MinigameOutcome` — ทำไมไม่ใช้ bool

```csharp
public enum MinigameOutcome
{
    Completed,   // เล่นจบตามเงื่อนไขชนะ
    Cancelled    // ถูกยกเลิกกลางคัน: ปุ่มปิด / flow backtrack / สลับเกม
}
```

ฝั่ง flow ต้องแยก "ผู้เล่นทำเครื่องดื่มเสร็จ" ออกจาก "ผู้เล่นถอยออก" เพราะสอง case นี้พาไปคนละ transition
ใน HSM — `bool success` บอกไม่ได้ว่า `false` แปลว่าแพ้หรือยกเลิก
(ตรงกับ open question §6.2 ของ integration plan ที่ถามว่า "แพ้แล้วจะ retry หรือไปต่อ")

`IMinigame.OnGameEnd(bool)` **ยังคงอยู่เหมือนเดิม** ไม่ทำลาย contract เดิม —
ตอนนี้เป็น `outcome == Completed` และใครอยากรู้ละเอียดค่อยอ่าน `LastOutcome`

### 11.2 `Cancel()` — หนึ่ง implementation สามชื่อ

```
BaseMiniGame.Cancel()          ← ตัวจริงตัวเดียว
  ├─ ClosePanel()  => Cancel()   alias — ปุ่ม UI ที่คุ้นชื่อนี้
  └─ EndGame()     => Cancel()   alias — IMinigame + editor hotkey R/B
```

```csharp
public void Cancel()
{
    if (Phase == MinigamePhase.Idle || Phase == MinigamePhase.Outro) return;
    _pendingOutcome = MinigameOutcome.Cancelled;
    SetPhase(MinigamePhase.Outro);
}
```

พฤติกรรมที่ตั้งใจ:

| เรียกตอน | ผล |
|---|---|
| `Idle` | no-op — ไม่ยิง event ซ้ำ (ทดสอบแล้ว §6.6) |
| `Intro` | abort กลาง intro → Animator ใช้ transition `Intro --[Hide]--> Outro` |
| `Play` | `OnExit()` ทำงาน → Outro |
| `Outro` | no-op — กันกดปุ่มรัว |

**สำคัญ:** cancel ≠ ซ่อน panel ทันที — outro ยังเล่นเต็ม แล้วค่อยรายงานผลตอนถึง `Idle`
เส้นทางออกจึงมีเส้นเดียวเหมือนเดิม (`EnterPhase(Idle)`) ไม่ว่าจะชนะหรือยกเลิก

`_pendingOutcome` ถูกรีเซ็ตเป็น `Cancelled` ทุกครั้งที่ `StartGame()` —
**อะไรก็ตามที่จบก่อน `Complete()` นับเป็นยกเลิกโดยปริยาย** ไม่มีทางที่จะรายงาน `Completed` โดยไม่ได้ชนะจริง

### 11.3 ช่องต่อฝั่ง Minigame — `MinigameSystemManager`

```csharp
// ── Game-flow seam (HSM) ───────────────────────────────
public event Action<Enum_MiniGameType>                    MinigameStarted;
public event Action<Enum_MiniGameType, MinigameOutcome>   MinigameFinished;

public Enum_MiniGameType ActiveType  => _activeMinigame?.GameType  ?? Enum_MiniGameType.None;
public MinigamePhase     ActivePhase => _activeMinigame?.Phase     ?? MinigamePhase.Idle;

public void CancelMinigame() { ... _activeMinigame.Cancel(); }
```

เป็น **plain C# event ไม่ใช่ UnityEvent** โดยตั้งใจ — flow layer subscribe ในโค้ด
และต้องไม่ถูกลาก re-point ใน Inspector ได้ ส่วน `OnStartedMinigame` / `OnEndedGame` (UnityEvent)
ยังอยู่ครบสำหรับ designer ยิงพร้อมกันทั้งคู่จากจุดเดียว

**`IMinigameContext` เปลี่ยน signature**

```csharp
// เดิม
void NotifyGameEnded();
// ใหม่
void NotifyGameEnded(E_Cocktail.Enum_MiniGameType type, MinigameOutcome outcome);
```

ทำไมต้องส่ง `type` มาด้วยแทนที่จะให้ manager อ่าน `ActiveType` เอง:
ถ้า `SwitchTo()` ยกเลิกเกมเก่าแล้วสลับไปเกมใหม่ทันที เกมเก่าจะจบ outro **หลัง** `_activeMinigame`
เปลี่ยนไปแล้ว → `ActiveType` จะเป็นเกมใหม่ และรายงานผิดตัว การให้เกมบอกชื่อตัวเองปิดช่องนี้ถาวร

### 11.4 ช่องต่อฝั่ง HSM — `Bar410.GameFlow.MinigameState`

เติมตาม integration plan §3.2 (work item 1) แต่ใช้ `MinigameOutcome` แทน `bool`:

```csharp
public Enum_MiniGameType PendingType  { get; private set; } = Enum_MiniGameType.None;
public MinigameOutcome?  LastOutcome  { get; private set; }   // null = ยังไม่เคยเล่น

public event Action<Enum_MiniGameType> OnStartRequested;      // ยิงจาก OnEnter
public event Action                    OnStopRequested;       // ยิงจาก OnExit

public void SelectType(Enum_MiniGameType type) => PendingType = type;
public void ReportResult(MinigameOutcome outcome) => LastOutcome = outcome;

protected override void OnEnter()
{
    LastOutcome = null;
    OnStartRequested?.Invoke(PendingType);
}

protected override void OnExit()
{
    OnStopRequested?.Invoke();
    // TODO: fold LastOutcome into DrinkOrderContext (HSM doc §8 Q4)
}
```

ข้อจำกัดที่รักษาไว้ทั้งหมด:

- **ยังเป็น plain C# class** ไม่ใช่ MonoBehaviour
- **ไม่ reference `MinigameSystemManager`** และไม่ `using UnityEngine`
- ทำได้แค่ **ถือข้อมูล + ยิง event** — ไม่ตัดสินใจ transition แทนใคร
- `ReportResult()` เขียนค่าเฉย ๆ ไม่สั่ง transition — การตัดสินใจว่า `Cancelled` แล้วจะ retry
  หรือไปต่อ ยังเป็น open question §6.2 ที่ยังไม่ได้ตอบ **จึงจงใจไม่ implement**

เนื่องจากยังไม่มีใคร subscribe `OnStartRequested` / `OnStopRequested`
`OnEnter` / `OnExit` จึงยังเป็น no-op เหมือนเดิมเป๊ะ — **flow ปัจจุบันไม่เปลี่ยนพฤติกรรมเลย**

### 11.5 แผนที่การต่อสายรอบหน้า

```
        Bar410.GameFlow                MinigameFlowBridge              Minigame layer
        (plain C#)                     (MonoBehaviour, ยังไม่มี)        (MonoBehaviour)
   ┌──────────────────────┐          ┌────────────────────┐      ┌──────────────────────────┐
   │ MinigameState        │          │                    │      │ MinigameSystemManager    │
   │                      │          │                    │      │                          │
   │ OnStartRequested(t) ─┼─────────>│  Resolve(t)        │─────>│ StartMinigame(type)      │
   │ OnStopRequested ─────┼─────────>│                    │─────>│ CancelMinigame()         │
   │                      │          │                    │      │                          │
   │ ReportResult(o) <────┼──────────┤                    │<─────┼─ MinigameFinished(t, o)  │
   │ SelectType(t) <──────┼── GameFlowCommands            │      │  ActiveType / ActivePhase │
   └──────────────────────┘          └────────────────────┘      └──────────────────────────┘
```

งานที่เหลือทั้งหมดอยู่ในกล่องกลาง — **ปลายสายสองข้างพร้อมแล้ว**
`MinigameFlowBridge.cs` จะเป็นไฟล์ใหม่ไฟล์เดียว ไม่ต้องแก้ทั้ง `MinigameState` และ `MinigameSystemManager` อีก
(เทียบกับ integration plan work items: ข้อ 1 และ 4 ทำเสร็จแล้ว เหลือ 2, 3, 5, 6, 7)

### 11.6 ผลทดสอบเพิ่มเติม

subscribe `MinigameStarted` / `MinigameFinished` แบบเดียวกับที่ bridge จะทำ:

```
StartMinigame(Shaking) → CancelMinigame() ตอน Play
   ### SEAM_START  Shaking
   ### SEAM_FINISH Shaking outcome=Cancelled      ← ยิงหลัง outro จบ
   ### UNITYEVENT_ENDED                            ← 1 ครั้ง
   ShakingMinigame.LastOutcome = Cancelled

StartMinigame(Stiring) → บังคับชนะ
   ### SEAM_START  Stiring
   ### SEAM_FINISH Stiring outcome=Completed
   ### UNITYEVENT_ENDED                            ← 1 ครั้ง
   MixingMinigame.LastOutcome = Completed

CancelMinigame() ตอน Idle
   → phase ยัง Idle, ไม่มี SEAM_FINISH / UNITYEVENT_ENDED เพิ่ม   ✅ no-op จริง

StartMinigame(Shaking) + CancelMinigame() ในเฟรมเดียวกัน (ยกเลิกกลาง Intro)
   → phase=Outro แล้วกลับ Idle
   → BG/Art001/Art002/Canvas_ShakingMinigame = -200, InitPanel_004 = -540   ✅ กลับ hidden ครบ
```

ไม่มี error ใน console · ปุ่มในซีนทั้ง 3 ชี้ไปที่ `CancelMinigame` แล้ว

---

## ภาคผนวก — สรุปไฟล์ที่เปลี่ยน

```
 M Assets/[02]Script/Minigame/BaseMiniGame.cs                      456 → 225 บรรทัด
 M Assets/[02]Script/Minigame/IMinigame.cs                          59 → 106
 M Assets/[02]Script/Minigame/MinigameSystemManager.cs             145 → 183
 M Assets/[02]Script/Minigame/Minigame/MixingMinigame.cs           413 → 364
 M Assets/[02]Script/Minigame/Minigame/ShakingMinigame.cs          203 → 154
 A Assets/[02]Script/Minigame/MinigamePanelAnimator.cs                   134

 M Assets/[02]Script/Hierarchical State Machine/
     Level 3 - Prepare Drinks/MinigameState.cs                      40 →  83   (§11.4 ช่องต่อ HSM)

 D Assets/[02]Script/Cocktail System/PanelSlider.cs                366 -
 D Assets/[02]Script/Cocktail System/SlideFinishCondition.cs        26 -
 D Assets/[02]Script/AnimationCode/EasingConfig.cs                  33 -
 D Assets/[02]Script/AnimationCode/EasingMath.cs                    44 -
 D Assets/[02]Script/AnimationCode/EasingMode.cs                    11 -

 A Assets/[08]Animation/MinigamePanel/Panel - MinigameStage.controller
 A Assets/[08]Animation/MinigamePanel/Panel - MinigamePanel.controller
 A Assets/[08]Animation/MinigamePanel/MinigameStage_{Hidden,Intro,Shown,Outro}.anim
 A Assets/[08]Animation/MinigamePanel/MinigamePanel_{Hidden,Intro,Shown,Outro}.anim

 M Assets/[05]Scenes/MainScene/GamePlayScene.unity                 169 +++---
 M Assets/[05]Scenes/MainScene/GamePlayScene 1.unity                180 +++---
 M Assets/[05]Scenes/Deverlopment/New Drag Drop System.unity        148 ++---

 รวมโค้ด: 577 insertions(+), 1177 deletions(-)
```
