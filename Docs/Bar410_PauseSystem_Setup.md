# Bar410 — Pause System: ขั้นตอน Setup ใน Unity

**Date:** 2026-10-06 · **Branch:** `GameFlow/Main` · **Unity:** 6000.3.19f1
**คู่กับ:** `Bar410_PauseSystem_Plan.md` (แผนและ Decision Log P1–P13)
**สถานะ:** Part A และ B ทำครบแล้ว (2026-10-06): โค้ดเสร็จ, `PauseScene` ต่อ component แล้วและ register ใน Build Settings แล้ว, prefab `[PauseManager]` วางใน scene เกมกับ MainMenu แล้ว ทดสอบใน Play mode จาก scene เกมแล้ว (ดู B5) เหลือ Part C (เนื้อหา layer) ชื่อ field ในเอกสารตรงกับโค้ดจริง

รูปแบบที่ใช้ (P2): **additive scene เดียวชื่อ `PauseScene`** สำหรับฝั่งเกม มีหลาย layer (PauseMenu, Settings, SaveLoad) ส่วน **MainMenu** วาง layer Settings/SaveLoad ใน scene ตรง ๆ

| ส่วน | ใครทำ | สถานะ |
|---|---|---|
| **A** `PauseScene` (โครง, component, register Build Settings) | Lead | ✅ เสร็จ |
| **B** วาง `PauseManager` ใน scene เกมและ MainMenu | Lead | ✅ เสร็จ |
| **C** สร้างเนื้อหาในแต่ละ layer | เจ้าของ layer | ทำต่อได้เลย |
| **D** ทดสอบ | ทุกคน | |

---

## ข้อมูลของโปรเจกต์ที่ต้องรู้ก่อน (ตรวจจากไฟล์จริง 2026-10-06)

- **Scene ที่อยู่ใน Build Settings:** `MainMenu`, `LoadingScene`, `New Drag Drop System` (scene เกม), `PauseScene` ทุก scene ใช้ `InputSystemUIInputModule`
- **`SoundManager` มีทั้งใน `MainMenu` (`SoundSystem.prefab`) และใน scene เกม `New Drag Drop System`** ตัวแรกที่เกิดจะอยู่ถาวร (`DontDestroyOnLoad`) ตัวที่สองทำลายตัวเอง ดังนั้นกด Play จาก scene เกมตรง ๆ ก็มี `SoundManager.Instance` (ตรวจแล้วใน Play mode) ส่วน `PauseScene` เปิดเดี่ยว ๆ จะไม่มี `SoundManager`
- **`[SaveLoadSystem].prefab` (`SaveLoadManager` + `SceneLoader`) วางอยู่ใน `New Drag Drop System` เท่านั้น** หน้า Save/Load จึงใช้ได้เฉพาะใน scene เกม
- **`[GameLoop]` canvas ใช้ Scale With Screen Size, Reference Resolution 1920×1080, Match 0.5** Canvas ของ UI ใหม่ควรใช้ค่าเดียวกัน (Canvas ใน `MainMenu` เป็น Constant Pixel Size 800×600 อย่าลอก)
- `HoverTooltip` มี canvas `DontDestroyOnLoad` ที่ `sortingOrder = 9999` `PauseManager` ตั้ง `sortingOrder` ของ Canvas ใน `PauseScene` เป็น 10000 ให้เอง

---

## Part A — `PauseScene` (Lead)

### A1. โครงที่มีอยู่ใน `Assets/[05]Scenes/MainScene/PauseScene.unity`

```
Directional Light            (ของทดสอบเดี่ยว ๆ — PauseManager ปิดถาวรตอนโหลดแบบ additive)
[PauseSystem]                Canvas (Overlay) + CanvasScaler + GraphicRaycaster   ← ชื่อ root ของ Canvas ใน scene นี้
└─ BG                        Backdrop เต็มจอ (Raycast Target เปิด กันคลิกทะลุ)
   ├─ Panel - Pause          layer ฐาน (เมนูหลัก): Resume / Settings / SaveLoad / Back to Main Menu
   ├─ Panel - Setting        layer ย่อย → Panel - Sound Setting (slider Master, Music, Ambient, Master SFX)
   └─ Panel - SaveLoad       layer ย่อย
EventSystem                  (ของทดสอบเดี่ยว ๆ — PauseManager ปิดถาวรตอนโหลดแบบ additive)
```

> `[PauseSystem]` ใน scene นี้คือ **root ของ Canvas** อย่าสับสนกับ prefab ที่ถือ `PauseManager` ใน Part B ซึ่งตั้งชื่อ `[PauseManager]`

### A2. Component ที่ต่อแล้ว

| GameObject | Component | ตั้งค่า |
|---|---|---|
| `Panel - Pause` | `PausePanel` | **Is Base** ✔ |
| `Panel - Setting` | `PausePanel` | **Release Music Duck** ✔ |
| `Panel - SaveLoad` | `PausePanel` | — |
| `BTN - Resume` | `ClosePageButton` | — |
| `BTN - Settings` | `Button.onClick` | → `Panel - Setting` → `PausePanel.Show()` |
| `BTN - SaveLoad` | `Button.onClick` | → `Panel - SaveLoad` → `PausePanel.Show()` |
| slider ทั้ง 4 ตัวใน `Panel - Sound Setting` | `VolumeSlider` | Master, Music, Ambient, Master SFX |
| `BTN - Back to Main Menu` | — | mock ยังไม่ต่อ |

`Panel - Setting` และ `Panel - SaveLoad` ยังไม่มีปุ่ม Back กด `Esc` เพื่อย้อนกลับได้ ถ้าต้องการปุ่ม ให้เพิ่มแล้วผูก `Button.onClick` → layer ของตัวเอง → `PausePanel.Hide()`

### A3. สิ่งที่ `PauseManager` ทำกับ scene นี้ตอนโหลดแบบ additive (ไม่ต้องตั้งค่าเอง)

- root ที่ **ไม่มี `PausePanel`** (`Directional Light`, `EventSystem`) ถูกปิดถาวร จึงใช้ EventSystem ของ scene เกมและไม่มีไฟซ้อน ปล่อยทิ้งไว้ใน scene ได้เพื่อทดสอบ `PauseScene` เดี่ยว ๆ
- Canvas ของ root ที่มี `PausePanel` ถูกตั้ง `sortingOrder = 10000` (เหนือ tooltip) layer ซ้อนกันตามลำดับ sibling ใน Hierarchy (ตัวล่างอยู่บน)
- layer ย่อย (ไม่ติ๊ก Is Base) ถูกปิดทุกครั้งตอน preload และตอนปิด Pause ส่วน layer ฐานถูกเปิดทุกครั้งที่ Open ดังนั้น **ในไฟล์ scene จะบันทึกเป็น active หรือ inactive ก็ได้** แก้งานใน editor ได้โดยไม่ต้องกลัวสถานะค้าง
- ระหว่าง preload `PausePanel` ที่ถูก enable โดยการโหลดจะไม่ทำให้เกมหยุดชั่วขณะ

### A4. Register ใน Build Settings (Lead — ทำครั้งเดียว) ✅ เสร็จแล้ว

1. `File > Build Profiles` (Unity 6) เปิด **Scene List**
2. เปิด `PauseScene` แล้วกด **Add Open Scenes** (หรือลากไฟล์ `Assets/[05]Scenes/MainScene/PauseScene.unity` ลงรายการ) ตรวจว่าติ๊กเปิดใช้งาน
3. commit `ProjectSettings/EditorBuildSettings.asset` พร้อมไฟล์ scene ในครั้งเดียว ทีมไม่แตะ Build Settings เอง (P10)

### A5. ตรวจผล

- ✅ Scene List มี 4 scene (`MainMenu`, `LoadingScene`, `New Drag Drop System`, `PauseScene`) และ `PauseScene` เปิดใช้งาน (ตรวจแล้ว)

### A6. ถ้าหลายคนแก้ layer พร้อมกันแล้วชนกันที่ไฟล์ scene

ตอนนี้ทุก layer อยู่ใน scene เดียว ถ้าเริ่มชนกัน ให้แยก layer เป็น prefab ได้ (`Panel - Setting` → `Panel_Settings.prefab` ฯลฯ) `PausePanel` ทำงานเหมือนเดิม และการผูกปุ่มระหว่าง layer (`Show()`) ยังทำใน scene `PauseScene` เพราะ prefab เก็บ reference ไป instance อื่นใน scene ไม่ได้

---

## Part B — วาง `[PauseManager]` (Lead) ✅ เสร็จแล้ว

### B1. สร้าง prefab

1. สร้าง GameObject ว่างชื่อ `[PauseManager]` เพิ่ม component `PauseManager`
2. ลากเป็น prefab ไปที่ `Assets/[04]Prefab/GameSystemPrefab/[PauseManager].prefab` (โฟลเดอร์เดียวกับ `SoundSystem`, `[SaveLoadSystem]`)

ฟิลด์ใน Inspector:

| ฟิลด์ | ความหมาย | ค่าใน scene เกม | ค่าใน MainMenu |
|---|---|---|---|
| **Pause Time** | หยุดเวลาและ pause SFX/Voice ตอนมี layer เปิด | ✔ เปิด | ✘ ปิด |
| **Pause Scene** | ชื่อ additive scene ที่ preload แล้วซ่อน | `PauseScene` | **ว่าง** (ลบข้อความออก) |

### B2. วางใน scene เกม (`New Drag Drop System`)

ลาก `[PauseManager]` เข้า scene ที่ root (ไม่ใช่ข้างใน `[GameLoop]`) ใช้ค่าเริ่มต้น (Pause Time ✔, Pause Scene `PauseScene`)

### B3. วางใน `MainMenu`

ลาก `[PauseManager]` เข้า scene ตั้ง **Pause Time ✘** และ **ลบชื่อใน Pause Scene ให้ว่าง** (MainMenu ไม่มี additive scene จึงไม่มีการเปิด Pause ด้วย `Esc` เหลือแค่ปิด layer ที่เปิดอยู่)

### B4. ห้ามวางใน `LoadingScene`

### B5. ตรวจผล

**ผลที่ตรวจแล้ว (Play จาก scene เกม `New Drag Drop System` ใน editor, จำลองปุ่ม `Esc` ผ่าน Input System):** `PauseManager` โหลด `PauseScene` จาก Build Settings เอง, root ทั้ง 3 ถูกซ่อน, `Esc` เปิด Pause (เกมหยุด), กดปุ่ม Settings เปิด layer, `Esc` ถอยกลับไป Pause, `Esc` อีกครั้งปิดและเกมเดินต่อ Console ไม่มี error ส่วนเสียงทดสอบแยกแล้วใน Plan §4 รายการที่ต้องตรวจเองเมื่อเล่นจริง (Play จาก `MainMenu`):

1. เข้าเกม scene `PauseScene` ต้องโผล่เป็น scene ซ้อนใน Hierarchy (root ถูกซ่อนอยู่ ไม่เห็นบนหน้าจอ)
2. กด `Esc`: layer PauseMenu เปิด เกมหยุด เสียง Music/Ambient เบาลง SFX หยุด
3. กด `Esc` อีกครั้ง: ปิด เกมเดินต่อ เสียงกลับมา
4. Console ไม่มี error `Scene 'PauseScene' can't be loaded — add it to Build Settings` (ถ้ามี กลับไป A4)

---

## Part C — สร้างเนื้อหาในแต่ละ layer (เจ้าของ layer)

### C0. กติกาสำหรับทุก layer (อ่านก่อนเริ่ม)

1. **แก้เฉพาะ layer ของตัวเอง** (GameObject ใต้ `BG` ใน `PauseScene`) ไม่แก้ layer ของคนอื่น และไม่เพิ่ม/ลบ scene ใน Build Settings นัดกันก่อนเมื่อมีคนแก้ scene `PauseScene` พร้อมกัน (หรือแยก layer เป็น prefab ตาม A6)
2. ห้ามตั้ง `sortingOrder` ของ Canvas เอง `PauseManager` ตั้งให้ ห้ามลบ `PausePanel` ออกจาก root ของ layer layer ใหม่ต้องมี `PausePanel` เสมอ (ไม่งั้น Esc และการหยุดเกมไม่รู้จัก layer นั้น)
3. ห้ามเพิ่ม Camera, AudioListener หรือ `SoundManager`/`SaveLoadManager` ลงใน `PauseScene` (EventSystem และไฟที่ใช้ทดสอบเดี่ยว ๆ ปล่อยไว้ได้ ถูกปิดถาวรตอนโหลดแบบ additive ดู A3)
4. ทุกอย่างใน layer ต้องใช้ **unscaled time** เพราะเกมหยุดอยู่ตอน layer เปิด:
   - Animator: ตั้ง **Update Mode = Unscaled Time**
   - Coroutine: ใช้ `WaitForSecondsRealtime` ไม่ใช่ `WaitForSeconds`
   - Tween/Timeline: ใช้โหมด unscaled
5. **refresh ข้อมูลใน `OnEnable`** ไม่ใช่ `Awake`/`Start` ระบบเปิด/ปิด layer ซ้ำ ๆ และ `Start` จะไม่ทำงานจนกว่าจะเปิดครั้งแรก
6. ไม่อ้าง object นอก `PauseScene` ใน Inspector (อ้างข้าม scene ไม่ได้) คุยกับระบบอื่นผ่าน `SoundManager.Instance`, `SaveLoadManager.Instance` หรือ event และต้องทนค่า null ได้
7. ใช้ TextMeshPro และสไตล์ (ฟอนต์, สีปุ่ม) ชุดเดียวกับ UI หลักของเกม
8. ห้ามใส่ `PauseManager` ลงใน `PauseScene` (อยู่ใน scene เกมและ MainMenu ตาม Part B)

### C1. การเปิด/ปิด layer (ไม่ต้องเขียนโค้ด)

| ต้องการ | วิธี |
|---|---|
| เปิด layer อื่นซ้อนทับ (เช่น ปุ่ม Settings) | `Button.onClick` → ลาก layer นั้น (ใน scene `PauseScene`) → `PausePanel.Show()` |
| ย้อนกลับ (ปุ่ม Back ของ layer ย่อย) | `Button.onClick` → ลาก layer ของตัวเอง → `PausePanel.Hide()` |
| Resume (ปิดทั้ง Pause กลับเข้าเกม) | แปะ **`ClosePageButton`** บนปุ่ม (ไม่ต้องตั้งค่า) |
| `Esc` | ไม่ต้องทำอะไร ปิด layer ย่อยบนสุดก่อน แล้วค่อยปิดทั้ง Pause |

ถ้าคนละคนทำคนละ layer ให้คุยกันว่าปุ่มไหนเรียก `Show()` ของ layer ไหน

### C2. layer `Panel_PauseMenu`

| ปุ่ม | การเชื่อม |
|---|---|
| Resume | `ClosePageButton` |
| Settings | `PausePanel.Show()` ของ `Panel_Settings` |
| Save | `PausePanel.Show()` ของ `Panel_SaveLoad` (+ เลือกแท็บ Save เมื่อ panel มี method ให้) |
| Load | `PausePanel.Show()` ของ `Panel_SaveLoad` (+ เลือกแท็บ Load) |
| Main Menu | mock ก่อน (ยังไม่ต่อ ดูหมายเหตุ) |
| Quit | mock ก่อน (ยังไม่ต่อ ดูหมายเหตุ) |

หมายเหตุ: **Main Menu** และ **Quit** ต้องมี component โหลด scene/ออกเกมเพิ่ม ซึ่งยังไม่อยู่ในขอบเขต ตอนนี้วางปุ่มไว้เฉย ๆ

### C3. layer `Panel_Settings`

- Slider 7 ตัว ชื่อตามช่องเสียงใน `SoundManager`: Master, Music, Ambient, Master SFX, SFX, UI, Voice
  - Min **0**, Max **1**, ปิด **Whole Numbers**
- ปุ่ม Back (`Button.onClick` → `PausePanel.Hide()` ของตัวเอง)

**ต่อกับ `SoundManager`:** แปะ component **`VolumeSlider`** (`Assets/[02]Script/Sound/VolumeSlider.cs`) บน GameObject เดียวกับ `Slider` แล้วเลือก **Channel** จาก dropdown (Master, Music, Ambient, Master SFX, SFX, UI, Voice) ไม่ต้องลาก reference ใด ๆ ใช้ได้ทั้งใน scene `PauseScene` และ MainMenu:

- `OnEnable`: อ่านค่าจาก `SoundManager.Instance` มาตั้ง slider (ไม่ trigger การบันทึก) และ subscribe การเลื่อน → `SoundManager.SetVolume(channel, value)` ซึ่งบันทึกลง PlayerPrefs เอง
- slider ต้อง Min 0 / Max 1 / Whole Numbers ปิด
- ไม่มี `SoundManager.Instance` (เปิด Play จาก scene `PauseScene` เดี่ยว ๆ): slider จะถูกปิด (interactable ✘) ไม่ error ทดสอบเสียงให้เล่นจาก scene เกมหรือ `MainMenu`
- Settings ปล่อย duck อัตโนมัติ (Release Music Duck ✔) จึงได้ยินเสียง Music/Ambient จริงตอนเลื่อน slider ส่วน SFX/Voice ยังหยุดอยู่ตอน pause (เรื่อง preview เสียงดู Plan §9)

### C4. layer `Panel_SaveLoad`

- แท็บ Save / Load (สลับ panel ภายใน layer เดียวกัน)
- รายการ slot 6 ช่อง (ตาม `SaveLoadManager.GetAllSlotsMeta()` คืน 6 slot) แต่ละช่องแสดง: ชื่อ, เวลาบันทึก, เวลาเล่น, ชื่อ chapter, สถานะว่าง
- ปุ่ม Back (`PausePanel.Hide()` ของตัวเอง)
- ข้อความเตือนในแท็บ Save: "ถ้า Save ระหว่างทำเครื่องดื่ม จะย้อนกลับไปก่อนเริ่มทำเครื่องดื่ม"

ตอนนี้ทำเป็น mock การต่อกับ `SaveLoadManager` ทำทีหลัง โดยมีข้อควรรู้:

- ปิดปุ่ม Save ขณะ `SceneLoaderBridge.IsSilentReplay` เป็น true
- refresh รายการ slot ใน `OnEnable`
- `SaveLoadManager` มีอยู่เฉพาะใน scene เกม

### C5. ใช้ layer เดียวกันใน MainMenu (ทำเมื่อพร้อม ยังไม่ได้ทำ)

layer ใน `PauseScene` เป็นลูกของ Canvas ใน scene นั้น การเอาไปใช้ใน MainMenu ต้องแยกเป็น prefab ก่อน:

1. แยก `Panel - Setting` (และ `Panel - SaveLoad` ถ้าต้องการ) เป็น prefab ไว้ที่ `Assets/[04]Prefab/UI/Pause/` (ลากจาก Hierarchy ลงโฟลเดอร์) แล้วใช้ prefab นั้นใน `PauseScene` ด้วย เพื่อให้ทั้งสองที่แก้ที่เดียว
2. ใน `MainMenu` สร้าง **Canvas ใหม่สำหรับ layer นี้** (Overlay, Canvas Scaler Scale With Screen Size 1920×1080 Match 0.5, `sortingOrder` สูงกว่า Canvas เดิมของ MainMenu เช่น 10000) ห้ามวางใต้ Canvas เดิมของ MainMenu เพราะเป็น Constant Pixel Size 800×600 layout จะเพี้ยน แล้ววาง prefab เป็นลูก **ปิด active** ไว้ก่อน
3. ปุ่ม Settings ของ MainMenu: `Button.onClick` → instance ของ prefab ใน scene → `PausePanel.Show()` ปุ่ม Back (ถ้ามี) ผูก `PausePanel.Hide()` และ `Esc` ปิด layer บนสุดให้
4. ใน MainMenu `PauseManager` ปิด Pause Time และเว้น Pause Scene ว่างไว้ เปิด Settings จึงไม่หยุดเวลาและเสียง
5. **ปุ่ม Load ที่ใช้งานได้จริงใน MainMenu ยังทำไม่ได้** ต้องแก้โค้ดเพิ่ม: `SaveLoadManager` ไม่มีใน MainMenu และ `LoadFromFile` เรียก `ReloadCurrentScene()` (จะโหลด MainMenu ซ้ำ) ส่วน `SaveMetaData` ไม่เก็บชื่อ scene เกม ตอนนี้ใช้เป็น UI mock หรือซ่อนปุ่ม Load ไว้

### C6. เช็กลิสต์ก่อนส่งงาน (ต่อ layer)

- [ ] layer ใหม่มี `PausePanel` บน root ของ layer (Is Base เฉพาะเมนูหลัก, Release Music Duck เฉพาะ Settings)
- [ ] ไม่ได้ตั้ง `sortingOrder` ของ Canvas เอง
- [ ] ไม่มี Camera, AudioListener, `SoundManager`, `SaveLoadManager`, `PauseManager` ใน `PauseScene`
- [ ] Animator ทั้งหมดเป็น Unscaled Time, coroutine ใช้ `WaitForSecondsRealtime`
- [ ] ข้อมูลที่ต้อง refresh อยู่ใน `OnEnable`
- [ ] ไม่ได้แตะ `EditorBuildSettings.asset`
- [ ] ถ้า commit scene `PauseScene` ให้ pull ก่อนและไม่ทับงานของคนอื่น

---

## Part D — ทดสอบ

### D1. ทดสอบในการเล่นจริง (แนะนำ)

เปิด Play จาก `MainMenu` เสมอ (เพื่อให้มี `SoundManager`) แล้วเข้าเกมกด `Esc` ไล่เปิด layer ตามลำดับ `Esc` ปิดทีละชั้น

### D2. เปิด scene `PauseScene` เดี่ยว ๆ

มี EventSystem ของ scene เอง ปุ่มกดได้ แต่ไม่มี `PauseManager` (layer เปิดแล้วไม่หยุดเกม และปุ่ม Resume ไม่ทำอะไร) และไม่มี `SoundManager` (slider เสียงเป็นสีเทา) ใช้ดูหน้าตาและทดสอบการนำทางระหว่าง layer เท่านั้น การหยุดเกมและเสียงทดสอบตาม D1

### D3. สิ่งที่ยังไม่เคยทดสอบด้วยของจริง

- ปุ่ม `Esc` กดด้วยมือจริงบนแป้นพิมพ์ (ทดสอบด้วย key event จำลองใน Input System แล้ว)
- Typewriter ของ Text Animator หยุดตอน pause (ดู Plan §9)
- กด `Esc` ระหว่างลากของ (ดู Plan §8 เช็กลิสต์)

---

## แก้ปัญหาที่พบบ่อย

| อาการ | สาเหตุและวิธีแก้ |
|---|---|
| `[PauseManager] Scene 'PauseScene' can't be loaded — add it to Build Settings` | scene `PauseScene` ยังไม่ได้ register หรือชื่อไฟล์ไม่ตรงกับฟิลด์ Pause Scene กลับไป A4 |
| กด `Esc` แล้วไม่มีอะไรเกิดขึ้นในเกม | ไม่มี `[PauseManager]` ใน scene เกม, Pause Scene ว่าง หรือ scene `PauseScene` ยังโหลดไม่เสร็จ/หา root ไม่เจอ (ดู Console) |
| กด `Esc` แล้วเมนูเปิดแต่เกมไม่หยุด | **Pause Time** ปิดอยู่ใน `PauseManager` ของ scene นั้น หรือ layer ไม่มี `PausePanel` บน root |
| ปุ่มในหน้ากดไม่ได้เลย | ในเกมปกติ: Canvas ต้องมี Graphic Raycaster เมื่อเปิดเดี่ยว ๆ: ไม่มี EventSystem (D2) |
| UI ตอบสนองแปลก ๆ / กดซ้ำสองครั้ง / warning ว่ามี EventSystem มากกว่า 1 | ลืมลบ EventSystem ที่ Unity สร้างให้พร้อม Canvas (A2 ข้อ 2) |
| Warning `There are 2 audio listeners` | มี Camera หรือ AudioListener เหลืออยู่ใน `PauseScene` |
| คลิกทะลุหน้าไปโดนของในเกม | `Backdrop` หายไปหรือปิด Raycast Target |
| layer อยู่ใต้ tooltip หรือซ้อนกันผิดลำดับ | มีคนตั้ง `sortingOrder` เอง ลบ/ปล่อยให้ `PausePanel` จัดการ และตรวจว่า `PausePanel` อยู่บน GameObject เดียวกับ Canvas |
| เปิด Esc แล้วเห็น Settings ค้างอยู่ตั้งแต่เปิดครั้งแรก | instance `Panel_Settings` ใน scene `PauseScene` ยังเปิด active อยู่ (A3 ข้อ 3) |
| Animation/Tween ใน layer ไม่เล่นตอน pause | ใช้ scaled time ตั้ง Animator เป็น Unscaled Time และใช้ `WaitForSecondsRealtime` |
| ค่าใน slider ไม่ตรงกับเสียงจริงตอนเปิด layer | ตั้งค่าเริ่มต้นใน `Start`/`Awake` แทน `OnEnable` |
| Music เริ่มเล่นใหม่ทุกครั้งที่เปิด layer | มี `SoundManager` อยู่ใน scene `PauseScene` หรือ prefab ลบทิ้ง |
| เปิด Settings จาก MainMenu แล้ว Music เบาลง/เวลาหยุด | `PauseManager` ใน `MainMenu` ยังเปิด **Pause Time** (B3) |
| ไม่มีเสียงเบาลงตอน pause | ไม่มี `SoundManager.Instance` (เปิด Play จาก `PauseScene` เดี่ยว ๆ) หรือ `PauseManager` ของ scene นั้นปิด **Pause Time** |
| `timeScale` ค้างเป็น 0 หลังกลับ `MainMenu` | แจ้งผู้เขียน `PauseManager` (ควรถูก reset ใน `OnDestroy` โดยอัตโนมัติ) |
