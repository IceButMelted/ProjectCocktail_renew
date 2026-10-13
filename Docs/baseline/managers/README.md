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
| `lock_fingerprint.snippet.cs` / `lock_fingerprint.txt` | สถานะ lock ของ **19 object** (ขวด 17, shaker, ปุ่ม book) หลังแต่ละ writer ทำงาน 9 จุด และจำนวนขวดที่ถือ `ShakerContents` ตั้งแต่เริ่ม (17 จาก 17) เคยเป็น 21 object จนกว่า 4b-fix ลบ `IngredientButtonUI` ซากออกจากปุ่ม Shake/Stir | วางเป็น body ของ `execute_code` ตอน Play mode (Play ใหม่) → diff กับไฟล์ใน git |
| `wiring_dump.snippet.cs` / `wiring_dump.after_stage4b.txt` | นับ scene ref ทั้งต้นไม้ (ใน child เดียวกัน/ข้าม child/UnityEvent/ออกนอก prefab) `wiring_dump.txt` คือ Stage 0, `…after_stage4b.txt` คือหลัง 4b | รันใน Edit mode ตั้ง `tag` แล้ว diff ตัวเลขหัวไฟล์ |
| `f6_giveup.snippet.cs` / `f6_giveup.txt` / `f6_giveup.after_stage5.txt` | Stage 5 (**obsolete**: `BG_BTN` ถูกลบแล้ว snippet รันไม่ได้อีก เก็บเป็นหลักฐาน): กด BG_BTN/BTN_Reset (give-up) และ BTN_Next (cancel-direct) ใน 2.2 `f6_giveup.txt` = ก่อนแก้ (ค้างที่ `Minigame` + `Illegal transition Minigame -> Minigame`), `.after_stage5.txt` = หลังแก้ | — |
| `back_button.snippet.cs` / `back_button.txt` | Stage 5: ปุ่ม `BTN - Cancel Minigame` ด้วย pointer click จริง → `BackRequested` → bridge → `AnotherIngredient()` → 2.1 แล้วเข้า 2.2 ซ้ำได้ (drink ถูกเก็บ, shaker ปลด lock) | Play ใหม่ → รัน snippet → รอ `DONE` |
| `garnish_ui.snippet.cs` / `garnish_ui.txt` | Stage 6: ทุกอย่างที่ Garnish ทำแต่ `flow_trace` ไม่เห็น ด้วย pointer click จริง: tab ×3, เลือก/สลับ glass, ice add/remove (icon, flag, ภาพบน glass), slot/item/rim, finish ก่อน pour (ถูกปฏิเสธ), pour (lock grid/ปุ่ม), remove ice และสลับ glass หลัง pour (ถูกปฏิเสธ), Reset, default ตอนเข้าใหม่, ปุ่ม Serve/Remake และตำแหน่งปุ่ม Serve หา object ด้วย**ชื่อ** จึงใช้ได้ตอนย้าย panel ข้าม canvas | Play ใหม่ → รัน snippet → รอ `DONE` → `diff` กับ `garnish_ui.txt` (ต้องเหมือนทุกไบต์) |
| `resolve_priority.snippet.cs` / `resolve_priority.txt` | Stage 5: ลำดับเลือก minigame ตอนไม่ได้เลือก (default) / method ของ drink / เลือกเอง ซึ่ง trace เดิมไม่ครอบคลุม (ทุก trace เรียก `SelectShaking` ก่อน) | เหมือนข้างบน |
| `method_buttons_click.snippet.cs` / `method_buttons_click.txt` | กด Shake/Stir ด้วย **pointer click จริงผ่าน EventSystem** ตอน drink เต็ม `flow_trace` เรียก `onClick.Invoke()` ซึ่งไม่ดู `interactable` จึงมองไม่เห็นปุ่มที่ถูก lock (เคยพลาด regression ของ 4b) | เหมือนข้างบน |
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
| 4d | `f5ca4c9` | trace เหมือนไฟล์ใน git ทุกไบต์ (รวม camera `PrepareCam`, lock, method panel ตอนเต็ม และตอน Clear), Yarn contract exit 0, console errors 0 |
| 4c-VisualizeCocktail | `91ef84e` | ลบ screen-space panel; trace เหมือนเดิมทุกบรรทัด ยกเว้นฟิลด์ `visScreen=` ที่ถูกถอดออกจาก snippet และจากไฟล์ baseline (ตัว object ไม่มีแล้ว) |
| 4a | `c4865fe` | lock fingerprint เหมือนก่อนแก้ทั้ง 9 จุด (21 object), ขวดที่ถือ `ShakerContents` ตั้งแต่เริ่ม 3 → 19 จาก 19, trace เหมือนไฟล์ใน git ทุกไบต์, Yarn contract exit 0, console errors 0 |
| 4b | `3fe82b2` | trace เหมือนไฟล์ใน git ทุกไบต์ (snippet หา panel ด้วย type แล้ว), ตำแหน่งปุ่มใน Canvas_Cocktail กับ Canvas เหมือนกัน (1009.34, 430.47) หลัง camera blend จบ, ปุ่ม Reset ล้าง shaker และปิด panel, ปุ่ม Stir ไป Minigame ด้วย Stiring, ภาพต่างจากก่อนแก้ไม่เกิน 0.24% ของ pixel (ขยับตามเฟรมของ animation), Yarn contract exit 0, console errors 0 |
| 4b-fix | commit เดียวกัน (Stage 5) | แก้ regression ของ 4b: ย้าย Method panel เข้าใต้ `[CocktailSystem]` ทำให้ `IngredientButtonGroup` (เก็บสมาชิกตามตำแหน่ง) เก็บ `BTN_Shaking`/`BTN_Mixing` ไปด้วย แล้ว lock ตอน drink เต็ม (ผู้เล่นกด Shake/Stir ไม่ได้) พบเพราะ lock fingerprint ต่างจากไฟล์ใน git 5 จุด (ไฟล์นั้นสร้างตอน 4a ไม่ได้รันซ้ำหลัง 4b; ทดสอบด้วย `git stash` แล้วว่าไม่ได้มาจาก Stage 5) แก้ที่ต้นเหตุ (วิธี B): ลบ `IngredientButtonUI` ซาก (`action=None`, ไม่มี call, ไม่มี raycast) ออกจากสองปุ่มใน scene + prefab ไม่มี special-case ในโค้ด ยืนยันด้วย `method_buttons_click` (ก่อน: `interactable=False` คลิกแล้ว step ไม่ขยับ; หลัง: `True` เข้า `Minigame` ทั้ง 2 ปุ่ม) lock fingerprint: 19 object ที่เหลือเหมือน baseline ทุกจุดทุกค่า ไฟล์ regenerate แล้ว |
| 5 | commit เดียวกัน (Stage 5) | trace เหมือนไฟล์ใน git ทุกไบต์ (รันหลังแก้ทั้ง 5, หลัง 4b-fix, หลังลบ `BG_BTN` และหลังวิธี B), Yarn contract exit 0, console errors 0, `resolve_priority` ครบ 4 กรณี (รันก่อนลบ `BG_BTN`), F6 แก้แล้ว (ดู `f6_giveup*.txt`) และต่อมา user ให้ลบ `BG_BTN` ทั้งกลุ่ม (ปุ่ม give-up/cancel-direct/add-ice ที่ปิดไว้และไม่มีอะไรเปิด) เหลือทางออกจาก minigame คือปุ่ม back 2 ตัว ตรวจด้วย `back_button` ref ข้าม child 65 → **61** (root → `[MiniGameSystem]` 5 → 1), scene ref 215 → 211, comps 916 → 885, UnityEvent 19 เท่าเดิม |
| 6a | (Stage 6 commit แรก) | สร้าง `[GarnishSystem]/Canvas_Garnish` ย้าย `Panel - Granish UI`, `Panel - Serve` และ `Garnish_Plate` (พร้อม `GlassPlacementZone`, `GranishCam`, `Ref - Panel Serve`) เข้าไป ไม่เปลี่ยนโค้ด: `garnish_ui` เหมือนก่อนย้ายทุกไบต์ (รวมตำแหน่งปุ่ม Serve 772.6,567.0 / 772.6,468.4), flow trace เหมือน baseline ทุกไบต์, Yarn contract exit 0, console errors 0, ภาพ garnish step เหมือนเดิม (ตา) `flow_trace`/`serve_commit` หา Serve/Garnish panel ด้วยชื่อแทน path |
| 6b | (Stage 6 commit ที่สอง) | `GarnishManager` (ใหม่, บน `[GarnishSystem]`) ถือ glass/ice/ตกแต่ง/pour + ปุ่มและ tab ของ canvas ตัวเอง ส่ง event `PourRequested`/`IceToggled`/`FinishRequested`/`ResetRequested`/`PourStarted`/`Opened`; `GarnishFlowBridge` 330 → ~130 บรรทัด slot 20 → 7 (`_gameLoop`, `_commands`, `_garnish`, `_cocktail`, `_shakerContents`, `_ingredients`, `_cameraSwitcher`); 3 grid หา manager ด้วย `GetComponentInParent` และ glass grid ฟัง `Opened`/`PourStarted` แทนที่ bridge ถือ grid (ตัดวงกลม) ผล: `garnish_ui` เหมือนก่อนแก้ทุกบรรทัด (ต่างแค่ prefix ของ warning `[GarnishFlowBridge]` → `[GarnishManager]`), flow trace เหมือน baseline ทุกไบต์, `serve_commit` เหมือนทุกไบต์, Yarn contract exit 0, console errors 0 snippet ทั้งสอง (flow_trace, serve_commit) ใช้ API ของ `GarnishManager` แล้ว (`Pour(drink)`, `CanFinish`, `TryFinish()`) |

## เมื่อโครงสร้างใน scene เปลี่ยนจากตอน audit (2026-10-05)

- `SystemGame` → `[CocktailSystem]`, `MiniGameSystem` → `[MiniGameSystem]`, `PlacmentSystem` → `[PlacmentSystem]`
- มี `[CocktailSystem]/Canvas_Cocktail` แล้ว ตอนนี้มีแค่ `Panel - OrderDirnks` (post-it) ที่เหลืออยู่ใต้ `Canvas` เดิม
- `RecipeBook` และ `Architexturebook` ไม่อยู่แล้ว (Interactable_3DObject เหลือ 1, `DragableObject` 12, `ScaleOnHover` 17)
- `GranishCam` อยู่ใต้ `[CocktailSystem]/Garnish_Plate`
