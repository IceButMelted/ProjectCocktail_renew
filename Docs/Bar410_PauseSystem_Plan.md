# [Pause] ระบบ Pause + หน้า UI แบบ Additive Scene (Settings / Save / Load) — แผน

Date: 2026-10-06 · Status: ความเสี่ยงที่เปิดอยู่จัดการแล้ว (ดู §2.3, §5, §9) แก้โค้ดเดิม 3 ไฟล์ ระบบ pause ยังไม่เริ่มเขียน · Branch: `GameFlow/Main`

เป้าหมาย: เปิดหน้า Pause / Settings / Save / Load ระหว่างเล่นได้ โดย Yarn Spinner (บทสนทนา, typewriter, option timeout) และ Text Animator หยุดนิ่งแล้วเดินต่อได้ถูกต้อง หน้า UI แต่ละหน้าเป็น **Additive Scene** แยกกัน เพื่อให้หลายคนทำ UI พร้อมกันโดยไม่ชนกัน ขอบเขตตอนนี้คือ **UI + ระบบ pause** ส่วนเนื้อหาในหน้า (ปุ่ม, slot) ทำเป็น mock ก่อนแล้วค่อยต่อกับ `SoundManager` / `SaveLoadManager`

ข้อมูลในเอกสารมาจากการอ่านโค้ดเท่านั้น ข้อที่ยังไม่ได้ยืนยันใน Unity ถูกทำเครื่องหมาย **(ต้องเช็ก)** ไว้

---

## 1. Decision Log (ล็อกแล้ว)

| # | การตัดสินใจ |
|---|---|
| P1 | ใช้ `Time.timeScale = 0` เป็นกลไก pause ไม่ pause `DialogueRunner` หรือ Text Animator ตรง ๆ |
| P2 | หน้า UI เป็น **Additive Scene** คนละไฟล์ต่อหน้า เหตุผล: `SoundManager` และระบบ SaveLoad เป็น Singleton แบบ `DontDestroyOnLoad` อยู่แล้ว UI scene จึงเข้าถึงผ่าน `Instance` ได้โดยไม่ต้องอ้าง Inspector ข้าม scene |
| P3 | **Preload แล้วซ่อน** — โหลด UI scene ไว้ตอนเริ่ม scene แล้วปิด root object เปิด/ปิดหน้าด้วยการ SetActive ไม่โหลด/unload ตอนใช้งาน |
| P4 | Save เปิดได้ทุกช่วงของ HSM แต่ถ้า Save ระหว่าง Minigame / ทำเครื่องดื่ม / Garnish จะบันทึกเฉพาะ state ของ **Conversation** (ย้อนไปที่ checkpoint ก่อน task) |
| P5 | เสียงตอน pause: **Music และ Ambient ลดเสียง**, **SFX หยุด** (UI SFX ยังเล่นเพื่อให้ปุ่มในเมนูมีเสียง) |
| P6 | `PauseManager` เป็น Singleton แบบ **scene-scoped** (ไม่ใส่ `DontDestroyOnLoad`) เพื่อไม่ให้ `timeScale = 0` ค้างข้าม scene |
| P7 | **Voice** ถูก pause ด้วยตอน pause (`Pause()` / `UnPause()` เหมือน SFX) เล่นต่อจากเดิมตอน resume |
| P8 | หน้า **Settings** ยกเลิก duck ของ Music/Ambient ขณะเปิดอยู่ เพื่อให้ผู้เล่นได้ยินเสียงจริงตอนเลื่อน slider ส่วน SFX/Voice ยังหยุดอยู่ |
| P9 | `PlaytimeTracker` **นับเวลาที่ pause** ตามเดิม ไม่แก้โค้ด |
| P10 | **Lead** สร้าง UI scene เปล่าทั้งหมดและ register Build Settings ครั้งเดียวก่อนเริ่มงาน ทีมไม่แตะ Build Settings |
| P11 *(default, ยังไม่ได้ยืนยัน)* | `Esc` ใน MainMenu ที่ไม่มีหน้าเปิดอยู่: ไม่ทำอะไร |
| P12 *(default, ยังไม่ได้ยืนยัน)* | Preload: MainMenu โหลด `Settings` อย่างเดียว, Gameplay โหลด `PauseMenu`, `Settings`, `SaveLoad` |

---

## 2. สิ่งที่ตรวจเจอในโค้ด

### 2.1 หยุดเองเมื่อ `timeScale = 0`

- Yarn: `YarnTask.Delay` (ตัว Awaitable ใช้ `Awaitable.WaitForSecondsAsync` ซึ่งเป็น scaled time), คำสั่ง `<<wait>>` (`WaitForSeconds`), `LetterTypewriter` (`Time.timeAsDouble`), `Effects.FadeAlphaAsync` (`Time.deltaTime`), auto-advance ของ presenter
- `GameLoopFSM.Tick(Time.deltaTime)`, minigame (`BaseMiniGame`, `MixingMinigame`), `CameraController`
- `TimeoutBar` และ timeout ของ option (`OptionPresenterType2`, `OptionPresenterCocktailProject` ใช้ `Time.deltaTime`) — timeout หยุดตาม pause ซึ่งเป็นพฤติกรรมที่ต้องการ
- Text Animator **effects**: `TextAnimatorComponentBase.timeScale` ใช้ `Settings.timeScale` ตรวจค่าจริงแล้ว: enum `TimeScale` คือ `Scaled = 0`, `Unscaled = 1` และ `timeScale: 0` (Scaled) ในทั้ง `Shared Animator Settings.asset`, `Text Animator Dialogue System.prefab` (2 จุด) และ scene `YarnSaveLoad` จึงหยุดตาม `timeScale = 0` ✔
- Text Animator **typewriter** (พิมพ์ทีละตัว): ตัวนับเวลาอยู่ใน `Febucci.TextAnimatorCore.dll` อ่านจาก source ไม่ได้ ยังไม่ยืนยัน **(ต้องเช็กตอน playtest, §9)**

### 2.2 ไม่หยุดเอง ต้องกันเอง

- `CustomLineAdvancer.Update()` poll ปุ่ม Space (KeyCodes mode) ผู้เล่นกดตอน pause แล้วบทสนทนาจะเลื่อน
- Click ของ UI และ 3D object ไปทาง EventSystem ซึ่งไม่สน `timeScale` ต้องมี backdrop เต็มจอที่ `raycastTarget = true` ในทุกหน้า
- `HoverTooltip` สร้าง canvas `DontDestroyOnLoad` ที่ `sortingOrder = 9999` จะทับหน้า pause ถ้าตั้งต่ำกว่า
- Hotkey debug (`GameFlowDebugHotkeys`, `MinigameSystemManager`) ยิงตอน pause ได้ แต่เป็น editor-only ยอมรับได้

### 2.3 กับดักที่ต้องจัดการ (สถานะ ณ 2026-10-06)

- ✅ **`SaveLoadManager.OnSceneLoaded`** กรองแค่ `scene.name == "LoadingScene"` การโหลด UI scene แบบ additive จึงทำให้ `InitializeReferences()` ถูกเรียกซ้ำ → `onNodeStart` / `onNodeComplete` / `onDialogueComplete` ถูก `AddListener` ซ้ำ → `_detourDepth` นับซ้ำ → การแยก `<<jump>>` กับ `<<detour>>` เพี้ยน **แก้แล้ว:** เพิ่ม `if (mode == LoadSceneMode.Additive) return;` ที่บรรทัดแรก
- ✅ **`LoadingManager`** ใช้ `Time.deltaTime` และ `WaitForSeconds(0.3f)` ถ้า `timeScale` ยังเป็น 0 ตอนเข้า `LoadingScene` จะค้างตลอด **แก้แล้ว:** เปลี่ยนเป็น `Time.unscaledDeltaTime` และ `WaitForSecondsRealtime(0.3f)` หน้า loading จึงไม่ขึ้นกับ `timeScale` เลย ไม่ต้องพึ่งลำดับ `OnDestroy` ของ `PauseManager` (ยังคง reset ใน `OnDestroy` ไว้เป็นชั้นที่สอง)
- ✅ **`SoundManager` ไม่มี getter** **แก้แล้ว:** เพิ่ม `GetMasterVolume` ... `GetVoiceVolume` (7 ตัว) อ่านจาก PlayerPrefs ค่าเดียวกับที่ setter เขียน และจะไม่ถูกกระทบโดย duck เพราะ duck ไม่เขียน PlayerPrefs field `m_Slider*` ใช้ข้าม scene ไม่ได้ ให้เว้นว่าง
- ⚠️ **`SoundManager` duplicate**: ถ้ามี `SoundManager` อยู่ใน scene ที่สอง ตัว duplicate จะสั่ง `Instance.PlayBGM(m_BGMStart)` เพลงจะเริ่มใหม่ ยังเป็นกฎของทีม (§6 ข้อ 2) ไม่มีโค้ดกัน
- `PlaytimeTracker` ใช้ `Time.realtimeSinceStartup` เวลาที่ pause จึงถูกนับเข้าเวลาเล่น ตัดสินแล้วว่าให้นับตามเดิม (P9) ไม่ต้องแก้

---

## 3. โครงสร้างระบบ

### 3.1 `PauseManager` (Singleton, scene-scoped)

อยู่ใน scene ที่ต้องการหน้า pause (Gameplay และ MainMenu) pattern เดียวกับ `SoundManager` คือ duplicate ทำลายตัวเอง แต่ไม่ `DontDestroyOnLoad`

```csharp
public static PauseManager Instance { get; private set; }
public static bool IsPaused { get; }          // มีหน้าเปิดอยู่อย่างน้อย 1 หน้า
public static void Open(string scene);        // เปิดหน้า (ซ้อนบน stack)
public static void Close();                   // ปิดหน้าบนสุด
public static void CloseAll();                // ปุ่ม Resume
```

พฤติกรรม:

- **Preload ใน `Start`**: โหลด UI scene ตามรายชื่อใน Inspector ด้วย `LoadSceneAsync(name, Additive)` เมื่อโหลดเสร็จให้ `SetActive(false)` ทุก root object ของ scene นั้นทันที
- **`Open(scene)`**: เปิด root object, ตั้ง `canvas.sortingOrder = 10000 + ความลึกของ stack` (สูงกว่า tooltip ที่ 9999 และไม่ต้องให้แต่ละคนเดาเลข), push เข้า stack
- **`Close()`**: ปิด root object ของหน้าบนสุด, pop
- **ชั้นแรกที่เปิด** (stack จาก 0 เป็น 1): ถ้า `pauseTime` เปิดอยู่ให้ตั้ง `Time.timeScale = 0` **ชั้นสุดท้ายที่ปิด** (stack เหลือ 0): `timeScale = 1` การนับตาม stack ทำให้ Settings เปิดซ้อนบน Pause แล้วปิด Settings เกมยังหยุดอยู่
- **เสียง**: หลังทุกครั้งที่ `Open` / `Close` (เมื่อ `pauseTime` เปิดอยู่) เรียก `SoundManager.Instance?.SetPaused(IsPaused, duckMusic)` โดย `duckMusic` เป็น `false` เมื่อหน้าบนสุดอยู่ในรายชื่อ `duckExemptPages` (Inspector, ค่าเริ่มต้น `Settings`) ตาม P8 เพื่อไม่ให้หน้า Settings ต้องรู้เรื่องเสียงของ pause เอง
- **`pauseTime` (Inspector)**: ใน MainMenu ปิดไว้ (เปิด Settings โดยไม่หยุดเวลา/เสียง) ใน Gameplay เปิดไว้
- **`Esc`** (อ่านผ่าน `Keyboard.current.escapeKey.wasPressedThisFrame` ใน `Update` ซึ่งยังทำงานตอน `timeScale = 0`): ถ้า stack ไม่ว่างให้ `Close()` ถ้าว่างและอยู่ใน Gameplay (`pauseTime` เปิด) ให้ `Open("PauseMenu")` ถ้าว่างและอยู่ใน MainMenu ไม่ทำอะไร (P11)
- **`_busy` flag** กันการกดซ้ำระหว่างที่ preload ยังไม่เสร็จ
- **`OnDestroy`**: `Time.timeScale = 1` และ `SetPaused(false)`

### 3.2 Component สำหรับดีไซเนอร์ (ไม่ต้องเขียนโค้ด)

ปุ่มใน UI scene อ้าง `PauseManager` ผ่าน Inspector ไม่ได้เพราะอยู่คนละ scene จึงมี component เล็ก 2 ตัวไว้แปะบนปุ่ม:

- `OpenPageButton` — ฟิลด์ `string sceneName` เรียก `PauseManager.Open(sceneName)` ตอนกด
- `ClosePageButton` — เรียก `PauseManager.Close()` (ปุ่ม Back) ส่วนปุ่ม Resume ใช้ `CloseAll()` ผ่านฟิลด์ `bool closeAll`

### 3.3 กฎของหน้า UI ที่ถูก preload

เพราะหน้าถูกโหลดแล้วซ่อนทันที `Awake` / `OnEnable` ทำงานตอน preload และ `Start` จะเลื่อนไปทำงานตอนเปิดครั้งแรก จึงต้อง:

- refresh ข้อมูลของหน้า (ค่า slider, รายการ slot ของ Save/Load) ใน **`OnEnable`** ไม่ใช่ `Awake` / `Start`
- ไม่พึ่ง `Start` ในการเตรียมค่าเริ่มต้นที่หน้าต้องใช้ก่อนเปิด

---

## 4. เสียงตอน Pause (P5)

เพิ่มใน `SoundManager`:

- `SetPaused(bool paused, bool duckMusic = true)`
  - **Music และ Ambient**: เมื่อ `paused && duckMusic` ตั้ง mixer param เป็น `ToDecibel(vol * duckFactor)` ด้วยฟิลด์ `[SerializeField] float m_PauseDuck = 0.3f` ไม่เขียนลง PlayerPrefs (ค่าที่ผู้เล่นตั้งไว้ไม่เสีย) และ lerp ด้วย unscaled time (ใน `SoundManager` มี fade แบบ `Time.unscaledDeltaTime` อยู่แล้ว) เมื่อ `duckMusic == false` (หน้า Settings เปิดอยู่, P8) ให้กลับเป็น volume ที่ผู้เล่นตั้งไว้ เพื่อให้ได้ยินเสียงจริงตอนเลื่อน slider และ `ApplyVolume` ต้องคำนึงถึงสถานะ duck ปัจจุบัน เพื่อไม่ให้ค่าที่เพิ่งเลื่อนถูกคูณซ้ำหรือถูกทับตอนปิดหน้า
  - **SFX**: เรียก `Pause()` / `UnPause()` กับ `m_SfxPool` ทุกตัวและ source ใน `m_LoopingSfx` เพื่อให้กลับมาเล่นต่อจากเดิมตอน resume (หยุดอยู่ตลอดที่ `paused` แม้หน้า Settings เปิด)
  - **UI SFX**: ไม่แตะ เพื่อให้ปุ่มในหน้า pause มีเสียง
  - **Voice**: `Pause()` / `UnPause()` `m_VoiceSrc` เหมือน SFX (P7)
- Getter ของ volume ทั้ง 7 ตัว ✅ เพิ่มแล้ว (§2.3) หน้า Settings ใช้ตั้งค่าเริ่มต้นของ slider ใน `OnEnable`

---

## 5. Save / Load (P4)

**ข้อเท็จจริงจากโค้ดและ Yarn script (ตรวจแล้ว):**

- ใน `Day1_Demo.yarn` (ทั้ง 3 ไฟล์ รวม 9 จุดที่มี `<<wait_for_task>>`) แต่ละ task มีรูปแบบเดียวกัน: บรรทัดสั่ง ("Coming right up") → `<<flow_prepare_drinks>>` → `<<Fix_Camera>>` → `<<wait_for_task>>` ไม่มีบรรทัดบทสนทนาคั่นระหว่างทาง ดังนั้นระหว่าง task จะไม่มีบรรทัดใหม่แสดง และ `SceneLoaderBridge.CurrentLineId` คือบรรทัดสุดท้ายก่อน task เสมอ
- **ไม่มี tag `#save_checkpoint` ใน Yarn script จริงเลย** (มีเฉพาะ `WaitLoop.yarn` ที่เป็นสคริปต์ทดสอบ) เดิมโค้ดจึงใช้ `CheckpointLineId` ที่เป็น null หรือค่าเก่าค้างจาก node อื่น ตอน Save ระหว่าง task ทำให้ Load แล้วไม่ replay หรือ replay ไปหาบรรทัดที่ไม่มีใน node นี้ (ค้างใน silent replay)
- `IsWaitingForTask` เป็น `true` ตั้งแต่เข้า `<<wait_for_task>>` จนกว่า `Order.IsScored` ครอบคลุม Minigame, ทำเครื่องดื่ม, Garnish และ Serve ช่องว่างระหว่าง `flow_prepare_drinks` กับ `wait_for_task` มีแค่ command 0–2 ตัว (`Switch_Camera`, `Fix_Camera`) ตรวจครบทั้ง 9 จุด และถึงจะ Save ตกในช่องว่างนั้น `CurrentLineId` ก็ให้ตำแหน่งเดียวกับ checkpoint อยู่แล้ว ดังนั้น P4 **ไม่ต้องแก้ HSM** และไม่ต้องล็อกปุ่ม Save ตาม state

เมื่อ Load ระบบ replay แบบ silent ไปถึงบรรทัดนั้น (`IsSilentReplay` + `TargetLineId`) แล้วผู้เล่นทำเครื่องดื่มใหม่ ความคืบหน้าของแก้วที่ทำค้างไว้จะหายโดยออกแบบ

**จัดการแล้ว (`SaveLoadManager.cs`):**

1. ✅ **Fallback ของ checkpoint:** `LastLineId` ใช้ `CheckpointLineId` เฉพาะเมื่อ `IsWaitingForTask` **และ** ค่าไม่ว่าง ไม่เช่นนั้นใช้ `CurrentLineId` ซึ่งตามข้อเท็จจริงข้างบนคือบรรทัดก่อน task อยู่แล้ว ทำให้ Yarn script ที่ไม่มี tag ทำงานถูกต้อง และ tag `#save_checkpoint` ยังใช้เลือกย้อนไกลกว่านั้นได้
2. ✅ **Reset `CheckpointLineId` ทุกครั้งที่เริ่ม root node ใหม่** (`OnNodeStart` สาขา `<<jump>>` / เริ่มใหม่) เพราะ Load จะ replay เริ่มจาก `ChapterName` = root node ปัจจุบัน checkpoint จาก root ก่อนหน้าจึงไม่มีทางเจอ และ `IsSilentReplay` จะค้างตลอด (เกมดูเหมือนค้าง)
3. ✅ **ห้าม Save ระหว่าง silent replay:** `SaveToFile` คืนทันทีพร้อม `LogWarning` เมื่อ `IsSilentReplay == true` (ค่า `CurrentLineId` ตอนนั้นไม่ใช่ตำแหน่งจริง) การ Save ตอนยังไม่มีบรรทัดใดแสดงยังทำได้ (Load แล้วเริ่มจากต้น node)

**ยังต้องทำตอนทำ UI:**

4. **ปิดปุ่ม Save** ในหน้า Save เมื่อ `SceneLoaderBridge.IsSilentReplay` เป็น true (guard ใน `SaveToFile` เป็นชั้นสุดท้ายเท่านั้น)
5. **บอกผู้เล่น**: ข้อความในหน้า Save ควรระบุว่าถ้า Save ระหว่างทำเครื่องดื่ม จะย้อนกลับไปก่อนเริ่มทำ
6. `LoadFromFile` เรียก `FindAnyObjectByType<SceneLoader>()?.ReloadCurrentScene()` ต้องมี `SceneLoader` อยู่ใน Gameplay scene (การโหลดแบบ single ทำลาย UI scene ที่เป็น additive ทั้งหมดและ `PauseManager` เองด้วย)
7. `CaptureAndSave` ใช้ `WaitForEndOfFrame` ซึ่งทำงานได้ปกติตอน `timeScale = 0`

---

## 6. กติกาสำหรับทีม (ทำ UI หลายคน)

1. **หนึ่งหน้า = หนึ่ง scene = หนึ่งเจ้าของ** เก็บใน `[05]Scenes/UI/` เช่น `PauseMenu`, `Settings`, `SaveLoad`
2. สร้าง scene จาก **Empty template** ใน scene มีแค่ Canvas (Screen Space Overlay) **ห้ามมี** Camera, Light, Volume, EventSystem, AudioListener และห้ามวาง `SoundManager` / `SaveLoadManager`
3. ทุกหน้ามี backdrop เต็มจอที่ `raycastTarget = true`
4. ห้ามตั้ง `sortingOrder` เอง `PauseManager` ตั้งให้ตอนเปิด
5. ทุกอย่างในหน้า pause ต้องใช้ **unscaled time**: Animator ตั้ง Update Mode เป็น Unscaled Time, coroutine ใช้ `WaitForSecondsRealtime`, tween ใช้ unscaled
6. หน้า UI ไม่อ้าง gameplay object ข้าม scene (Unity ก็อ้างไม่ได้) คุยผ่าน `SoundManager.Instance`, `SaveLoadManager.Instance`, `PauseManager` หรือ event
7. refresh ข้อมูลใน `OnEnable` ไม่ใช่ `Awake` / `Start` (§3.3)
8. **Singleton ทุกตัวที่ subscribe `SceneManager.sceneLoaded` ต้องเช็ก `mode`** และข้าม `LoadSceneMode.Additive` (รวมถึง Singleton SaveLoad ตัวใหม่ที่จะทำในอนาคต)
9. **Build Settings** (P10): additive ต้อง register ทุก scene และ `EditorBuildSettings.asset` เป็นจุด conflict ของทีม **Lead** สร้าง scene เปล่าทั้งหมดแล้ว register ครั้งเดียวก่อนเริ่ม ทีมแก้เฉพาะเนื้อหาใน scene ห้ามเพิ่ม/ลบ scene ใน Build Settings เอง ถ้าต้องการหน้าใหม่ให้ขอ Lead
10. ชื่อ scene ที่ใช้ใน Inspector (`OpenPageButton`) ต้องตรงกับชื่อไฟล์เป๊ะ — เปลี่ยนชื่อ scene ต้องแจ้งทีม
11. Scene ใหม่ที่เปิดเดี่ยว ๆ เพื่อทดสอบไม่มี EventSystem ให้ใช้ helper ที่ทำงานเฉพาะ editor เติมให้ (และไม่ถูก build)

---

## 7. ไฟล์ที่เปลี่ยน

| ไฟล์ | การเปลี่ยนแปลง |
|---|---|
| `SaveLoad/SaveLoadManager.cs` ✅ | guard `LoadSceneMode.Additive` ใน `OnSceneLoaded`; reset `CheckpointLineId` ตอนเริ่ม root node; guard `IsSilentReplay` และ fallback checkpoint ใน `SaveToFile` |
| `SceneLoader/LoadingManager.cs` ✅ | `Time.unscaledDeltaTime`, `WaitForSecondsRealtime` |
| `Sound/SoundManager.cs` | ✅ getter ของ volume 7 ตัว; ยังต้องทำ `SetPaused`, `m_PauseDuck` |
| `[06]Dialogue/.../CustomLineAdvancer.cs` | `Update()` return ถ้า `PauseManager.IsPaused` |
| **ใหม่** `PauseManager.cs`, `OpenPageButton.cs`, `ClosePageButton.cs` | ตาม §3 |
| **ใหม่** scene `PauseMenu`, `Settings`, `SaveLoad` (+ Build Settings) | เนื้อหา mock |

ไม่เปลี่ยน: `SceneLoader`, `DialogueRunner`, Text Animator, HSM

---

## 8. ลำดับงาน

ทุกขั้น compile-check ผ่าน Unity MCP (`refresh_unity` + `read_console`) และ commit แยก

1. ✅ **แก้ของเดิม (เสร็จ 2026-10-06, compile ผ่าน ไม่มี error/warning ใหม่)**: `SaveLoadManager`, `LoadingManager`, getter ใน `SoundManager` ยังไม่ได้ commit
2. **ระบบ pause**: `PauseManager`, `OpenPageButton`, `ClosePageButton`, gate ใน `CustomLineAdvancer`
3. **เสียง**: `SoundManager.SetPaused` (duck Music/Ambient, pause SFX)
4. **Scene เปล่า** + register Build Settings (Lead)
5. **UI mock**: PauseMenu (Resume, Settings, Save, Load, Main Menu, Quit), Settings (slider เสียง 7 ตัว), SaveLoad (6 slot ตาม `GetAllSlotsMeta()`)
6. **ต่อของจริง**: Settings → `SoundManager`, Save/Load → `SaveLoadManager`, ปิดปุ่ม Save ตอน `IsSilentReplay` (ข้อ 4 §5)

### Checklist ทดสอบ

- Pause กลางบรรทัดที่ typewriter กำลังพิมพ์: ตัวอักษรและ effect หยุด, Space ไม่เลื่อนบรรทัด, resume แล้วพิมพ์ต่อ
- Pause ระหว่าง option ที่มี timeout: bar หยุด, resume แล้วเดินต่อ
- Pause ใน minigame, ตอนเลือก Garnish, ตอน Serve
- เสียง: Music/Ambient เบาลง, SFX (รวม loop) และ Voice หยุดแล้วเล่นต่อจากเดิม, ปุ่มในเมนูยังมีเสียง
- เปิด Settings จาก Pause: Music/Ambient กลับเป็นเสียงปกติระหว่างเลื่อน slider (SFX/Voice ยังหยุด) ปิด Settings แล้ว duck กลับมา ค่า volume ที่ตั้งไว้ถูกบันทึกถูกต้อง (ไม่ถูกคูณ duck ซ้ำ)
- เปิด Settings ซ้อนบน Pause แล้ว `Esc` ปิดทีละชั้น เกมยังหยุดจนปิดชั้นสุดท้าย
- เปิดรัว ๆ ระหว่าง preload ยังไม่เสร็จ
- Hover tooltip ไม่ทับหน้า pause
- Save ระหว่างทำเครื่องดื่ม แล้ว Load: กลับไปที่ checkpoint, ไม่มี `IsSilentReplay` ค้าง
- Load จากหน้า pause: `LoadingScene` ไม่ค้าง, scene ใหม่ `timeScale = 1`
- กลับ MainMenu จาก pause แล้ว `timeScale = 1`
- เปิด Settings จาก MainMenu: เวลาและเสียงไม่ถูก pause
- Save แล้วเช็กว่า `SaveLoadManager` ไม่ถูก init ซ้ำตอน preload (listener Yarn ไม่ซ้ำ, jump/detour ถูกต้อง)

---

## 9. คำถามที่เปิดอยู่ — ตอบแล้ว (2026-10-06)

| คำถาม | คำตอบ | อ้างอิง |
|---|---|---|
| Voice ตอน pause | Pause แล้วเล่นต่อ | P7, §4 |
| Settings กับ duck | ยกเลิก duck ของ Music/Ambient ตอน Settings เปิด | P8, §3.1, §4 |
| `PlaytimeTracker` นับเวลา pause | นับตามเดิม ไม่แก้โค้ด | P9, §2.3 |
| เจ้าของ Build Settings | Lead สร้าง scene เปล่าและ register ครั้งเดียว | P10, §6 ข้อ 9 |
| `Esc` ใน MainMenu ที่ไม่มีหน้าเปิด | ไม่ทำอะไร *(default)* | P11, §3.1 |
| รายชื่อ scene ที่ preload | MainMenu: `Settings` / Gameplay: `PauseMenu`, `Settings`, `SaveLoad` *(default)* | P12 |

สองข้อสุดท้ายยังไม่ได้ยืนยันกับเจ้าของโปรเจกต์ ใช้ค่าเริ่มต้นไปก่อน แก้ได้โดยไม่กระทบโครงสร้าง (เป็นค่าใน Inspector ของ `PauseManager`)

### ความเสี่ยงที่เคยค้าง — สถานะ (2026-10-06)

| ความเสี่ยง | สถานะ |
|---|---|
| Time Scale ของ Text Animator effects | ✅ ยืนยันแล้ว: Scaled ทุกจุด (§2.1) |
| Load จากหน้า pause แล้ว `LoadingScene` ค้าง | ✅ แก้ที่ต้นเหตุ: `LoadingManager` ใช้ unscaled time (§2.3) |
| `SaveLoadManager` ซ้ำตอนโหลด additive | ✅ แก้แล้ว: guard `LoadSceneMode.Additive` (§2.3) |
| `SoundManager` ไม่มี getter | ✅ เพิ่มแล้ว (§2.3) |
| Save ระหว่าง task: checkpoint เป็น null/ค้างจาก node อื่น, ค้างใน silent replay | ✅ แก้แล้ว: fallback + reset + guard (§5) |
| State ใน HSM ที่นอก `<<wait_for_task>>` แต่ Save ได้ | ✅ ตรวจแล้ว: ไม่มีช่องว่างที่ทำให้ผิด (§5) |
| **Typewriter ของ Text Animator นับเวลาแบบ scaled หรือไม่** | ⏳ ยืนยันไม่ได้จาก source (อยู่ใน DLL) ต้องทดสอบจริงเป็นข้อแรกของ checklist ตอนมี `PauseManager` แล้ว ถ้าพบว่าไม่หยุด ต้องหาวิธีให้ typewriter ใช้ scaled time (หรือ gate การพิมพ์เองผ่าน `IsPaused`) |
| `SoundManager.ApplyVolume` ร่วมกับสถานะ duck | ⏳ ตรวจตอนเขียน `SetPaused` (ขั้นที่ 3): getter อ่านจาก PlayerPrefs และ duck ไม่เขียน PlayerPrefs จึงไม่กระทบค่าที่ผู้เล่นตั้ง |

## 10. Non-goals

- Pause `DialogueRunner` หรือ Text Animator โดยตรง (ใช้ `timeScale`)
- เก็บ state ของ minigame / เครื่องดื่มลง save (P4: save เฉพาะ Conversation)
- เนื้อหาจริงของ Settings นอกจากเสียง
- แก้ HSM, `StateMachine`, ตาราง transition
- ย้าย/รื้อ `SaveLoadManager` (การทำ Singleton SaveLoad ตัวใหม่ในอนาคตแยกเป็นงานต่างหาก ใช้กฎข้อ 8 §6)
