# Stage 0 baseline · Managers restructure

สร้างวันที่ 2026-10-13 บน branch `GameFlow/Managers` (แตกจาก `GameFlow/Main` commit `1962402`) เป็นจุดเทียบสำหรับทุก stage ใน `Docs/Bar410_Managers_Restructure_Plan.md`

## ไฟล์

| ไฟล์ | คืออะไร | ใช้ยังไง |
|---|---|---|
| `wiring_dump.txt` | scene ref ทุกตัวใต้ `[GameLoop]` (ref ข้าม child, UnityEvent, ref นอก prefab) | รัน dump ใหม่ด้วย `execute_code` แล้ว diff ดูตัวเลขหัวไฟล์ |
| `yarn_surface.txt` | `[YarnCommand]`/`[YarnFunction]` ที่ลงทะเบียนอยู่ 42 ชื่อ | `python check_yarn_contract.py --check` ต้อง exit 0 ทุก stage |
| `yarn_used.txt` | command ที่ `.yarn` เรียกจริง 16 ชื่อ | เช่นเดียวกัน |
| `check_yarn_contract.py` | เขียน baseline หรือเทียบกับ baseline | ไม่มี dependency นอก Python |
| `flow_trace.snippet.cs` | driver เล่นวนครบ 1 รอบผ่าน HSM และปุ่มจริง | วางเป็น body ของ `execute_code` ตอน Play mode (เริ่มจาก Play ใหม่ทุกครั้ง) |
| `flow_trace.txt` | ผลของ driver 15 step ทั้ง state ของ flow, order, shaker, glass, camera, lock, panel | diff กับไฟล์ที่ run ใหม่ |
| `placement_drag.snippet.cs` / `placement_drag.txt` | จำลองการลากขวด 2 ตัวผ่าน `DragableObject` (PointerEventData) และเรียก `IngredientHoverDetector` ซึ่ง trace หลักไม่ครอบคลุม | วางเป็น body ของ `execute_code` ตอน Play mode แล้ว diff `placement_drag.txt` |
| `serve_commit.snippet.cs` / `serve_commit.txt` | ออกจาก Serve 3 ทาง (`ServeDone()` ตรงๆ, remake, ปุ่ม Serve) แล้วบันทึกว่าเขียนอะไรลง Yarn (`$task_done`, `$satisfaction`) | วางเป็น body ของ `execute_code` ตอน Play mode ใหม่ แล้ว diff `serve_commit.txt` |
| `lock_fingerprint.snippet.cs` / `lock_fingerprint.txt` | สถานะ lock ของ 21 object (ขวดทั้งหมด, shaker, ปุ่ม book) หลังแต่ละ writer ทำงาน 9 จุด และจำนวนขวดที่รู้จัก `ShakerContents` แล้วตั้งแต่เริ่ม | วางเป็น body ของ `execute_code` ตอน Play mode ใหม่ แล้ว diff |
| `screens_baseline/*.jpg` | ภาพอ้างอิงขนาดครึ่ง 960x540 | เทียบด้วยตา คู่กับ `screens/` ที่ driver เขียน (full-size, gitignored) |

## วิธีเช็กหลังแต่ละ stage

1. compile ผ่าน และ console ไม่มี error ใหม่
2. `python Docs/baseline/managers/check_yarn_contract.py --check` ต้อง exit 0
3. เข้า Play mode ใหม่ แล้วรัน `flow_trace.snippet.cs` จนเห็น `DONE`
4. `diff` กับ `flow_trace.txt` ใน git ต้องว่าง ยกเว้นบรรทัดที่ stage นั้นตั้งใจเปลี่ยน (ระบุใน commit) ถ้า `target=` ต่างกันอย่างเดียว ให้ดูหมายเหตุ determinism ด้านล่าง
5. เทียบ `screens/` กับ `screens_baseline/` สำหรับ stage ที่ย้าย UI

## ตัวเลข baseline

| | ค่า |
|---|---|
| components / MonoBehaviour | 933 / 410 |
| scene ref (ไม่นับ UI built-in) | 246 = 170 ใน child เดียวกัน + **76 ข้าม child** |
| ข้าม child จาก script บน root | Canvas 33, `[CocktailSystem]` 16, `[MiniGameSystem]` 5, CamController 4 (รวม 58) |
| Inspector UnityEvent call | 28 |
| ref ออกนอก prefab | 1 (`DialogueRunner`) |
| Yarn: registered / used | 42 / 16 |
| flow trace | 15 step, console errors = 0, warnings = 1 |

## สิ่งที่ baseline พบ (ยังไม่ได้แก้ ใช้ประกอบ stage ถัดไป)

- **`flow_close_bar` และ `flow_next_day` ไม่มี `.yarn` ไฟล์ไหนเรียก** (`flow_prepare_drinks` ถูกเรียก 9 ครั้ง) การปิดวันเกิดจาก hotkey หรือโค้ดเท่านั้น ข้อสมมติที่ว่า "Level 1 Yarn คุมอยู่แล้วโดยพฤตินัย" จึงยังไม่จริง ตอนนี้ L1 ไม่มีใครขับในเดโม
- `Day1_Demo.yarn` มีซ้ำ 2 ที่ (`YarnScript/` และ `YarnScript/Demo/`)
- state ที่ค้างหลัง step (ไม่ใช่ regression ของเรา)
  - `canFinish=True` ยังค้างหลัง remake จนกว่าจะเข้า Garnish ใหม่ (S07, S10)
  - หลัง Serve กลับ Talking แล้ว camera ยังเป็น `GranishCam` ทั้งที่ `TalkingWithCustomerBridge` ไม่สลับ camera (ปล่อยให้ Yarn `Switch_Camera` ทำ) (S12 ถึง S14)
  - `shaker.parts=10` และ `scored=True` ค้างข้ามวัน (S14) ล้างตอนเข้า PrepareDrinks รอบหน้า
  - minigame ค้าง phase `Outro` หลัง `DrinkComplete` ผ่านเส้นทาง cancel จนถึง S13 (outro เล่นนาน)
- ทางชนะ minigame ผ่าน `Complete()` (reflection) auto-advance ไป Garnish ถูกต้อง (S04) ทางยกเลิกผ่าน `DrinkComplete()` ก็ถูกต้อง (S08)
- ปุ่ม book มี state สลับ False/True ตามแต่ละ step (ดูคอลัมน์ `bookBtn`)

## ช่องว่างของ baseline นี้

- ไม่ได้ขับผ่าน Yarn dialogue (driver หยุด chapter dialogue ที่เริ่มเอง) ทางนั้นใช้ `Bar410DialogueOrderTest` (`[Bar410DialogueOrderTest]` ในฉาก, ContextMenu "Run Order Test") แยกต่างหาก
- ไม่ครอบคลุม save → load / silent replay และ NPC `MoveIn`/`MoveOut`/`SetEmotion`
- lock probe ดูแค่ขวด `BaseSpirit - Gin` กับ shaker และปุ่ม book ไม่ใช่ทุกขวด
- ผล `flow_trace.txt` ใน git มี `target=Manhattan`

## หมายเหตุ determinism

order ถูกสุ่มด้วย `UnityEngine.Random` ตอนเข้า step 2 ใน call เดียวกับ `PrepareDrinks()` snippet จึงเรียก `InitState(410)` ทันทีก่อน call นั้น ตรวจแล้วที่ Stage 1 ว่า run 2 ครั้งติดกันให้ trace เหมือนกันทุกไบต์ (`target=Sex On The Beach`) ดังนั้น `flow_trace.txt` ใน git ควร diff แล้วว่างเมื่อไม่มีการเปลี่ยนพฤติกรรม

## ข้อควรระวังเมื่อ save scene

อย่า save `New Drag Drop System.unity` จาก editor แล้ว commit ตรงๆ ให้ดู `git diff` ก่อน Cinemachine Brain ทำงานใน edit mode และเขียน FOV, far clip, ตำแหน่ง/การหมุนของ camera ลงไฟล์ scene ตอน save (Stage 1 เจอ 3 hunk) ถ้าเจอให้ตัดเฉพาะ hunk ที่ตั้งใจเปลี่ยน (`git diff` → กรอง → `git apply`)

## Stage log

| Stage | commit | ผลเทียบ baseline |
|---|---|---|
| 0 | `0b6b3f7` | สร้าง baseline |
| 1 | `fe06935` | trace เหมือน baseline ทุกบรรทัด (ยกเว้น `target=` ที่ตอนนี้ถูกตรึงด้วย seed), Yarn contract exit 0, console errors 0 |
| 2 | ข้าม | `FlowWiring` ว่างเปล่าไม่มีประโยชน์ สร้างตอน Stage 4 พร้อม wiring จริง |
| 3 | `dbbef25` | trace เหมือนไฟล์ใน git ทุกไบต์ (รวม `target=`), drag simulation เหมือนก่อนแก้, Yarn contract exit 0, console errors 0 |
| 4f | `b794bdf` | trace เหมือนไฟล์ใน git ทุกไบต์; serve_commit: `ServeDone()` ตรงๆ เขียน `$task_done=True` แล้ว (ก่อนแก้เป็น False), remake ไม่เขียนอะไร, ปุ่ม Serve เหมือนเดิม |

## เมื่อโครงสร้างใน scene เปลี่ยนจากตอน audit (2026-10-05)

- `SystemGame` → `[CocktailSystem]`, `MiniGameSystem` → `[MiniGameSystem]`, `PlacmentSystem` → `[PlacmentSystem]`
- มี `[CocktailSystem]/Canvas_Cocktail` แล้ว ตอนนี้มีแค่ `Panel - OrderDirnks` (post-it) ที่เหลืออยู่ใต้ `Canvas` เดิม
- `RecipeBook` และ `Architexturebook` ไม่อยู่แล้ว (Interactable_3DObject เหลือ 1, `DragableObject` 12, `ScaleOnHover` 17)
- `GranishCam` อยู่ใต้ `[CocktailSystem]/Garnish_Plate`
| 4d | `f5ca4c9` | trace เหมือนไฟล์ใน git ทุกไบต์ (รวม camera `PrepareCam`, lock, method panel ตอนเต็ม และตอน Clear), Yarn contract exit 0, console errors 0 |
| 4c-VisualizeCocktail | `91ef84e` | ลบ screen-space panel; trace เหมือนเดิมทุกบรรทัด ยกเว้นฟิลด์ `visScreen=` ที่ถูกถอดออกจาก snippet และจากไฟล์ baseline (ตัว object ไม่มีแล้ว) |
| 4a | ดู `git log` | lock fingerprint เหมือนก่อนแก้ทั้ง 9 จุด (21 object), ขวดที่ถือ `ShakerContents` ตั้งแต่เริ่ม 3 → 19 จาก 19, trace เหมือนไฟล์ใน git ทุกไบต์, Yarn contract exit 0, console errors 0 |
