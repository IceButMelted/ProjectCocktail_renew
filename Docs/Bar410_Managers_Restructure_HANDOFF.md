# Bar410 · Managers Restructure · HANDOFF

เขียนเมื่อ 2026-10-13 หลังจบ Stage 4b · branch `GameFlow/Managers` (แตกจาก `GameFlow/Main` ที่ `1962402`, **ยังไม่ push**)

> **อัปเดตหลังเขียน:** Stage 5 (Minigame, `e61fc99`) และ **Stage 6 (Garnish, 4 commit: `6621bd2`, `754daee`, `eb63d71`, `e5601e2`)** ทำเสร็จ ตรวจแล้ว และ commit แล้ว (ยังไม่ push) รายละเอียด Stage 6 ใน §2 §3 §7 และตัวตรวจใหม่ `garnish_ui` ใน §6.1

เอกสารนี้ให้คนที่รับงานต่อ (หรือ session ใหม่) ทำงานต่อได้โดยไม่ต้องอ่าน chat เดิม อ่านตามลำดับ §1 → §2 → §6 แล้วเริ่มที่ §7

เอกสารที่เกี่ยวข้อง
- `Docs/Bar410_Managers_Restructure_Plan.md` แผนเต็ม, สถาปัตยกรรมเป้าหมาย, single source of truth, **decision log (§8)**
- `Docs/Bar410_GameLoop_Decoupling_Plan.md` audit ต้นทาง (§1–§3 ยังใช้ได้, §5 ถูกแทนที่)
- `Docs/baseline/managers/README.md` วิธีเช็กหลังแต่ละ stage และ stage log

---

## 1. สถานะโดยสรุป

| | |
|---|---|
| เป้าหมาย | แยก `[GameLoop]` เป็น Manager อิสระต่อกัน (Cocktail, Minigame, Garnish, TalkToCustomer, Camera, Placement) แต่ละตัวมี Canvas ของตัวเอง ข้อมูลที่ใช้ร่วมกันมีเจ้าของเดียว FSM คุมแค่ Level 2–3 |
| ทำเสร็จ (commit แล้ว) | Stage 0, 1, 3, 4a, 4b, 4c (เฉพาะ VisualizeCocktail), 4d, 4f, **5** (Stage 5 + `4b-fix` + ลบ `BG_BTN` ใน commit เดียว), **6** (6a–6d) |
| ข้าม | Stage 2 (`FlowWiring` ว่างเปล่า), Stage 4c ส่วน book (เลื่อนไป Stage 8) |
| เหลือ | Stage 7 TalkToCustomer, 8 Camera (+ presenter, book/post-it, `ResetCamera`), 9 Shell + ลบ bridge, 10 (optional) |
| working tree | สะอาด ยกเว้น `ProjectSettings/Packages/dev.yarnspinner/Assembly-CSharp-generated.ysls.json` ซึ่ง Yarn สร้างเอง **ไม่ใช่งานของเรา ไม่ต้อง stage** |
| ตรวจล่าสุด | flow trace, `serve_commit`, lock fingerprint (19 object) เหมือน baseline ทุกไบต์; `garnish_ui` เหมือน baseline ทุกบรรทัด (ต่างแค่ชื่อ class ใน warning); Yarn contract exit 0; console error 0 (1 warning เดิม) |

ตัวเลขเทียบ (wiring dump, scene `New Drag Drop System`)

| | Stage 0 | หลัง 4b |
|---|---|---|
| components / MonoBehaviour | 933 / 410 | 916 / 402 |
| scene ref (ไม่นับ UI built-in) | 246 | 215 |
| ref ข้าม child | 76 | **65** |
| Inspector UnityEvent call | 28 | **19** |
| ref ออกนอก prefab | 1 (`DialogueRunner`) | 1 |
| script บน `[GameLoop]` root | 10 | 8 |
| root → Canvas / `[CocktailSystem]` / `[MiniGameSystem]` / CamController | 33 / 16 / 5 / 4 | 29 / 17 / 5 / 4 |

หลัง Stage 5 (`wiring_dump.after_stage5.txt`): ref ข้าม child **61** (root → `[MiniGameSystem]` 5 → 1 เพราะปุ่ม cancel ย้ายไปอยู่ใน manager เอง), ref ใน child เดียวกัน 150 เท่าเดิม, scene ref 215 → 211, comps 916 → 885 / MonoBehaviour 402 → 386 (ลบ `BG_BTN` 7 object และ `IngredientButtonUI` ซาก 2 ตัว), UnityEvent 19 เท่าเดิม (UnityEvent ที่ลบเป็นตัวว่าง)

หลัง Stage 6 (`wiring_dump.after_stage6.txt`): ref ข้าม child **40** (76 → 61 → 40: root → `Canvas` 29 → 12 เพราะ Garnish/Serve UI ย้ายออกจาก `Canvas`, root → `[GarnishSystem]` 2), ref ใน child เดียวกัน 150 → 167, scene ref 211 → 207, comps 885 → 891 / MonoBehaviour 386 → 389 (`GarnishManager` + CanvasScaler/GraphicRaycaster ของ `Canvas_Garnish`), UnityEvent 19 เท่าเดิม, script บน root 8 เท่าเดิม `GarnishFlowBridge` 20 slot → 7, `ServeFlowBridge` 7 slot → 5

ไฟล์เต็ม: `Docs/baseline/managers/wiring_dump.txt` (Stage 0) และ `wiring_dump.after_stage4b.txt`

---

## 2. Commit ที่ทำไปแล้ว (เรียงตามเวลา)

| commit | Stage | ทำอะไร | ทำไม |
|---|---|---|---|
| `0b6b3f7` | 0 | แผน + baseline (wiring dump, Yarn contract, flow trace, screenshot) | จุดเทียบของทุก stage |
| `fe06935` | 1 | `GameLoopFSM` build ใน `Awake` + `[DefaultExecutionOrder(-100)]` ลบ `EnsureBuilt()` 6 จุด; `flow_*` Yarn command ข้ามเมื่อ `SceneLoaderBridge.IsSilentReplay`; hotkey ไม่ชน (minigame ใช้ Shift+1/2/B/V); ย้าย `GameFlowDebugHotkeys` ไป `[Debug]`; ลบ `Demo_KeysShortCut` บน `[GameLoop]` (listener target null); แก้ comment เก่า | ลด hack, กัน `flow_prepare_drinks` ดัน FSM ตอน silent replay |
| `dbbef25` | 3 | `N_PlacementSystem.Current` (scene-scoped) + `.InputManager` แทน `public static DragableObject._placementSystem` และ Find ใน `IngredientHoverDetector` | ลบ global ที่ใครก็เขียนได้ |
| `b794bdf` | 4f | ให้คะแนนที่เดียว: `CocktailFlowBridge` ฟัง `OpenBar.Sub.OnTransition` (Serve → Talking) เรียก `ServeDrink()` ซึ่ง idempotent ด้วย `IsScored`; ปุ่ม Serve เรียกแค่ `ServeDone()`; ลบ fallback ใน `OnServeExited` | `ServeDone()` ตรงๆ (`<<flow_serve_done>>`, hotkey 7) ให้ `IsScored` แต่ไม่เขียน `$task_done`/`$satisfaction` ลง Yarn และ fallback เดิมทำงานตอน remake ด้วย |
| `f5ca4c9` | 4d | `ShakerContents` Inspector UnityEvent 9 call (Changed 2, Cleared 3, OnFulled 4) → listener ในโค้ดที่ `CocktailFlowBridge`; `IsCocktailFull` ย้ายเข้า `ShakerContents.RaiseChanged()` | logic ซ่อนใน Inspector, อยู่นอก HSM |
| `7bfdba4` | docs | บันทึกหลักฐานว่า screen-space `VisualizeCocktail` ไม่เคยถูกเปิด | |
| `91ef84e` | 4c-Vis | **ลบ** `Canvas/Panel - VisurlCocktail` (screen-space) + `_panelVisualCocktail` (decision D10) | ไม่มีโค้ด/event ใดเปิดมัน; ผู้เล่นเห็นแต่ตัว world-space |
| `c4865fe` | 4a | `IngredientButtonGroup` เก็บขวดจาก `IngredientButtonUI` ใต้ตัวเอง (list ใน scene 20 → 2: shaker, ปุ่ม book) + `BindShaker` จาก `CocktailSystemManager.Awake` | ขวดถือ `ShakerContents` ตั้งแต่เริ่ม 3 → 19 จาก 19 |
| `3fe82b2` | 4b | `MethodPanel` (component ใหม่) เป็นเจ้าของปุ่ม Shake/Stir/Reset ส่ง event; bridge เหลือ 1 slot (เดิม 4); ย้าย panel ไป `[CocktailSystem]/Canvas_Cocktail`; `Canvas_Cocktail` CanvasScaler match 0 → 0.5; prefab เก่าได้ component เดียวกัน | panel ดูแลตัวเอง |
| `e61fc99` | 4b-fix | ลบ `IngredientButtonUI` (`action=None`, ไม่มี call) ออกจาก `BTN_Shaking`/`BTN_Mixing` ใน scene + prefab; `IngredientButtonGroup.Members` กลับเป็น `GetComponentsInChildren<IngredientButtonUI>` ล้วน (ไม่มี special-case) | 4b ย้าย panel เข้าใต้ `[CocktailSystem]` สองปุ่มนี้เลยกลายเป็นสมาชิกของกลุ่มที่ `OnFulled` lock → ผู้เล่นกด Shake/Stir ตอน drink เต็มไม่ได้ trace มองไม่เห็นเพราะเรียก `onClick.Invoke()` |
| `e61fc99` | 5 | `MinigameSystemManager` ถือปุ่ม back ×2 + `Resolve()` + `_defaultType` (ส่ง `BackRequested`); bridge เหลือ slot `_gameLoop`/`_commands`/`_minigames`/`_shakerContents`; ลบ `OnStartedMinigame`/`OnEndedGame`; **ลบ `BG_BTN` ทั้งกลุ่ม** (user สั่ง) จึงไม่มี give-up/cancel-direct/F6 อีก | ปุ่ม cancel อยู่ใน bridge ที่ root; ปุ่ม dev ที่ปิดไว้และตายทั้งกลุ่ม |
| `6621bd2` | 6a | สร้าง `[GarnishSystem]/Canvas_Garnish` (scaler เท่า `Canvas_Cocktail`) ย้าย `Panel - Granish UI`, `Panel - Serve`, `Garnish_Plate` (พร้อม `GlassPlacementZone`, `GranishCam`, `Ref - Panel Serve`) เข้าไป ไม่เปลี่ยนโค้ด; ตัวตรวจใหม่ `garnish_ui`; `flow_trace`/`serve_commit` หา panel ด้วยชื่อ | D2/D9 — Garnish มี canvas และ root ของตัวเอง |
| `754daee` | 6b | `GarnishManager` (ใหม่) ถือ glass/ice/ตกแต่ง/pour + ปุ่ม tab ของ canvas ตัวเอง ส่ง event; `GarnishFlowBridge` 20 → 7 slot; grid ×3 หา manager ด้วย `GetComponentInParent`, glass grid ฟัง `Opened`/`PourStarted` (ตัดวงกลม bridge ↔ grid); `IceToggled` → bridge → `ShakerContents.SetIce` | bridge ถือ UI + logic ทั้งหมด, 20 slot, lambda ไม่ unsubscribe |
| `eb63d71` | 6c | Serve panel + 2 ปุ่มเป็นของ `GarnishManager` (`OpenServe`/`CloseServe`, `ServeRequested`/`RemakeRequested`); `ServeFlowBridge` เหลือฝั่ง flow; ลบ `CocktailFlowBridge._panelServe` และ `ShakerPanelController._serveUI` กับ flag serve | panel เดียวมี 3 writer |
| `e5601e2` | 6d | glass เป็นของ `GarnishManager` (`Glass`, `ClearGlass()`); ลบ `PlacedGlassInstance.Current`/`Spawn`/`DestroyCurrent` (static); การเคลียร์ glass ย้ายจาก `CocktailFlowBridge` ไป `GarnishFlowBridge` | static ที่ใครก็เข้าถึง glass ได้ |

---

## 3. สิ่งที่ตัดสินใจ**ต่างจากแผนเดิม** (และเหตุผล)

| แผนเดิม | ที่ทำจริง | เหตุผล |
|---|---|---|
| Stage 2 สร้าง `FlowWiring` ว่าง | ข้าม | class ว่างไม่มีอะไรให้เทียบ baseline; bridge เดิมทำหน้าที่ seam ได้ จะรวมเป็นไฟล์เดียวตอน Stage 9 |
| Stage 3 รวม `N_PlacementSystem` + `N_InputManager` | ไม่รวม ทำ `Current` แทน | อยู่ GameObject เดียวและ ref กันเองอยู่แล้ว การรวมแก้ข้อมูล serialized โดยไม่ลด wiring และ trace จับไม่ครบ ตัวเลข "ลบ Find ×14+1" ผิด (static ตัวเดียว, Find เกิดครั้งแรกครั้งเดียว) |
| 4f ให้คะแนนตอน `Serve.Exited` | ให้คะแนนตอน Serve → Talking | `Exited` ยิงตอน remake ด้วย จะเขียน relationship ผิด |
| 4a เปลี่ยนชื่อเป็น `IngredientShelf`, writer เดียว, รวม API | คงชื่อ `IngredientButtonGroup`; ไม่รวม `SetInteractable` กับ `EnableInteractablePrepareDrinksPhase` | rename ไม่ลด wiring; สองตัวต่างกันที่ `DragableFruitTraySlot` hijack (`Apply` ไม่เปิด, `ApplyPrepareDrinksPhase` เปิด) รวมแล้วพฤติกรรมเปลี่ยน — รอ presenter Stage 8 |
| 4c ตรวจ `VisualizeCocktail` แล้วคงไว้ทั้งสอง (D8) | ลบ screen-space (D10) | user ตัดสินหลังเห็นหลักฐาน (2 ภาพใน `screens_baseline/visualize_*.jpg`) |
| 4e ice API ใน Stage 4 | ย้ายไป Stage 6 | ผู้เรียกคือ Garnish |
| baseline trace มีฟิลด์ `visScreen=` | ถอดออกจาก snippet + ไฟล์ baseline | object ไม่มีแล้ว |
| ตั้ง `Canvas_Cocktail` CanvasScaler | match 0.5 (เดิม 0) | ให้เท่า `Canvas` เดิม ไม่งั้น Method panel/post-it สเกลต่างเมื่อจอไม่ใช่ 16:9 ถ้า user ตั้ง 0 ไว้จริงให้คืน |
| Stage 5 ย้าย `PendingType`/`LastOutcome` เข้า manager | ไม่ย้าย | เป็น state ระดับ flow ที่ Yarn อ่าน (`flow_minigame_type`/`flow_minigame_result`) และ `LastOutcome` ถูกล้างทุกครั้งที่เข้า 2.2 การย้ายต้องให้ `GameFlowCommands` ถือ ref ไป manager ซึ่งข้ามชั้น; ย้ายเฉพาะส่วนที่เป็นของ manager จริง (ปุ่ม, ลำดับเลือก, default) |
| Stage 5 ลบ `_cocktailCamera`/`ResetCamera()` | เลื่อนไป Stage 8 | null ทั้ง scene และ prefab (no-op) แต่เป็น seam ที่ออกแบบไว้ "snap กล้องตอนเริ่มเกม" เป็นเรื่องของ Camera manager ให้ตัดสินตอนนั้นว่าต่อหรือลบ |
| Stage 5 แก้ F6 ให้ Give-up/Cancel-direct ออกผ่าน HSM | ลบปุ่มทั้งคู่ (`BG_BTN`) | หลักฐานใน `f6_giveup.txt`: ค้างที่ 2.2 แล้ว `IngredientAdded()` ถูกปฏิเสธ ปุ่มอยู่ใน `BG_BTN` ที่ปิดไว้และไม่มีอะไรเปิด ผมแก้ให้ออกผ่าน HSM ก่อน (ผ่าน `f6_giveup.after_stage5.txt`) แล้ว user สั่งลบทั้งกลุ่ม จึงถอดโค้ด give-up ออกด้วย |
| 6 ice: Garnish ส่ง `IceToggled` → `Cocktail.SetIce` → `IceChanged` กลับ | `IceToggled` → bridge → `ShakerContents.SetIce` ไม่มี echo | writer ของ ice มีทางเดียว (Garnish) และ icon ของ Garnish เป็นสถานะของตัวเองที่รีเซ็ตตอน `Close()` echo จะไม่ได้ทำอะไร (YAGNI) flag ยังอยู่ที่ `CurrentCocktail.AddIce` (D5) |
| 6 `GarnishChoice` (plain C#: glass + item + rim) | ไม่ทำ | ไม่มี consumer: ไม่มีอะไรอ่านสามอย่างนี้รวมกัน (scoring ไม่ใช้) ทำตอนมีผู้ใช้จริง |
| 6 Serve ส่ง `ServeClicked` ให้ FlowWiring | `ServeRequested` ไป `ServeFlowBridge` | FlowWiring ยังไม่มี (Stage 2 ข้าม) bridge ทำหน้าที่เดียวกัน จะรวมตอน Stage 9 |
| 6 `Canvas_Garnish` ลำดับวาด | อยู่ก่อน `Canvas` (sortingOrder 0 เท่ากัน) เหมือน `Canvas_Cocktail` | Hub/BookUI/Transition ที่อยู่ใน `Canvas` จึงวาดทับ Garnish UI (ก่อนย้าย Garnish UI อยู่เหนือ Hub) ภาพ garnish step เทียบด้วยตาแล้วเหมือนเดิม ถ้า Hub ทับ panel ในอนาคตให้ปรับ sortingOrder |

---

## 4. Decision log (ย่อ — ฉบับเต็มใน plan §8)

| # | ตัดสินใจ | สถานะ |
|---|---|---|
| D1 | FSM = Level 2–3; Level 1 ให้ Yarn คุม | ยืนยัน ส่วน Level 1 **แทนที่ด้วย D11** |
| D2 | Serve panel อยู่ `Canvas_Garnish` | ยืนยัน |
| D3 | Camera เป็น Manager แยก (ไม่มี canvas) | ยืนยัน |
| D4 | Yarn variable writer + `DialogueRunner` ref ไป TalkToCustomer | ยืนยัน |
| D5 | **Ice อยู่ที่ Cocktail** (เป็นเงื่อนไขตอนเสิร์ฟ) ไม่ใช่ Garnish | ยืนยัน |
| D6 | `CharacterData` + roster อยู่ TalkToCustomer | ยืนยัน |
| D7 | `PlacementSystem` คงไว้เป็น Manager แยก | ยืนยัน (ทำแบบ `Current`) |
| D8 | `Canvas/Panel - VisurlCocktail` อยู่กับ Cocktail | **แทนที่ด้วย D10** |
| D9 | `GlassPlacementZone` อยู่กับ Garnish (คงไว้ ไม่ลบ) | ยืนยัน 2026-10-13 |
| D10 | ลบ screen-space `VisualizeCocktail` เหลือ world-space ตัวเดียว | ยืนยัน |
| D11 | Level 1 (Open ⇄ Close) ขับด้วยปุ่ม/hotkey ตามเดิม ไม่เพิ่ม `flow_close_bar`/`flow_next_day` ใน `.yarn` | ยืนยัน 2026-10-13 ไม่มีงานแก้ L1 |

ค่า default ที่ยังไม่ยืนยัน: Shell (Hub, EndShift, Thx, Transition) เป็น `Canvas_Shell` ไม่มี manager; Placement เข้าถึงผ่าน `Current`; `RecipeBook` หายไปจาก scene แล้ว (ไม่ต้องตัดสิน)

---

## 5. สถาปัตยกรรมเป้าหมาย (ย่อ)

```
[GameLoop]
 ├ Flow            GameLoopFSM (L2-3), GameFlowCommands, (FlowWiring ตอน Stage 9)
 ├ Cocktail        CocktailSystemManager + Canvas_Cocktail  (post-it, Method panel อยู่แล้ว; book รอ Stage 8)
 ├ Minigame        MinigameSystemManager + Canvas_MiniGame
 ├ Garnish         GarnishManager + Canvas_Garnish          (+ Serve panel)
 ├ Customer        TalkToCustomerManager (NPCs_And_Set + CharacterData + roster + YarnVariableSync)
 ├ Camera          CameraManager
 ├ Placement       N_PlacementSystem + N_InputManager (Current)
 └ Canvas_Shell    Hub, EndShift, Thx, Transition
```

กฎ: Manager ไม่รู้จัก FSM และไม่รู้จักกัน (verb เข้า, event ออก); ข้อมูลร่วมมีเจ้าของเดียว คนอื่นรับเป็น argument; ชื่อ `[YarnCommand]`/`[YarnFunction]` ห้ามเปลี่ยน; singleton เฉพาะ service ข้าม scene + static ที่ Yarn ต้องการ (+ `N_PlacementSystem.Current`)

---

## 6. วิธีทำงานต่อ (ต้องทำตามนี้ทุก stage)

**Principle:** 1 stage (หรือ 1 commit ย่อย) = 1 commit, ตรวจกับ baseline ก่อน commit, ไม่รวมหลายอย่างในคอมมิตเดียว

### 6.1 ตัวตรวจ (อยู่ใน `Docs/baseline/managers/`)

| ตัวตรวจ | ไฟล์ | ครอบคลุม | วิธีรัน |
|---|---|---|---|
| Yarn contract | `check_yarn_contract.py` | 42 ชื่อที่ลงทะเบียนต้องไม่หาย; command ที่ `.yarn` ใช้ต้องมี | `python Docs/baseline/managers/check_yarn_contract.py --check` ต้อง exit 0 |
| flow trace | `flow_trace.snippet.cs` → `flow_trace.txt` | 15 step ผ่าน HSM + ปุ่มจริง: ชนะ minigame, ยกเลิก, remake จาก Garnish/Serve, Serve, close bar, next day | Play ใหม่ → วาง snippet ใน `execute_code` → รอบรรทัด `DONE` → `git diff Docs/baseline/managers/flow_trace.txt` ต้องว่าง |
| serve commit | `serve_commit.snippet.cs` → `serve_commit.txt` | ออกจาก Serve 3 ทาง แล้วดู `$task_done`/`$satisfaction` | เหมือนข้างบน |
| placement drag | `placement_drag.snippet.cs` → `placement_drag.txt` | ลากขวด 2 ตัวผ่าน `PointerEventData` + `IngredientHoverDetector` | เหมือนข้างบน |
| lock fingerprint | `lock_fingerprint.snippet.cs` → `lock_fingerprint.txt` | สถานะ lock ของ 19 object (ขวด 17, shaker, ปุ่ม book) × 9 จุด (ครบทุก writer) | เหมือนข้างบน |
| wiring dump | `wiring_dump.snippet.cs` | จำนวน ref/UnityEvent/ข้าม child | รันใน Edit mode, ตั้ง `tag` ให้ชื่อไฟล์ |
| F6 give-up / cancel-direct | `f6_giveup.snippet.cs` → `f6_giveup.after_stage5.txt` (`f6_giveup.txt` = ก่อนแก้) | ปุ่ม `BG_BTN/BTN_Reset`/`BTN_Next` ใน 2.2 แล้วลอง `IngredientAdded()` ต่อ | เหมือนข้างบน |
| minigame type resolution | `resolve_priority.snippet.cs` → `resolve_priority.txt` | default / method ของ drink / เลือกเอง (trace อื่นทุกตัวเรียก `SelectShaking` ก่อนจึงไม่ครอบคลุม) | เหมือนข้างบน |
| กดปุ่ม Method จริง | `method_buttons_click.snippet.cs` → `method_buttons_click.txt` | Shake/Stir ด้วย pointer click ผ่าน EventSystem ตอน drink เต็ม (ดู `interactable`) | เหมือนข้างบน; ต้องรันเมื่อย้าย object ใต้ `[CocktailSystem]` |
| Garnish UI | `garnish_ui.snippet.cs` → `garnish_ui.txt` | tab, glass swap, ice round trip, slot/item/rim, finish/pour gating, remove ice/swap glass หลัง pour, Reset, default ตอนเข้าใหม่, ปุ่ม Serve และตำแหน่ง ด้วย pointer click จริง หา object ด้วยชื่อ | เหมือนข้างบน; `diff` กับ `garnish_ui.txt` ต้องเหมือนทุกไบต์ |

ทุก snippet รันเป็น body ของ `mcp__unityMCP__execute_code` ใน Play mode **เริ่มจาก Play session ใหม่** (stop → play) และทุกตัวหยุด chapter dialogue ของฉากก่อน

ถ้า stage แตะ**ส่วนที่ trace ไม่ครอบคลุม** ให้สร้างตัวตรวจใหม่ก่อนแก้ แล้วรันทั้งก่อน/หลัง (ทำแบบนี้มาแล้วใน 3, 4f, 4a, 4b) — ช่องว่างที่รู้ตอนนี้: Yarn dialogue จริง, save → load / silent replay, NPC `MoveIn/MoveOut/SetEmotion`, fruit tray drag, mouse จริง

### 6.2 ขั้นตอนต่อ stage
1. อ่านแผน stage นั้นใน plan §7 และ gap ใน §6.1
2. สร้าง/รันตัวตรวจ "ก่อน" ถ้าจุดนั้นยังไม่มีตัวตรวจ
3. แก้โค้ด → `refresh_unity` (ดู §6.3) → ตรวจ console error
4. แก้ scene/prefab ผ่าน `execute_code` (SerializedObject) แล้ว save
5. **กรอง scene diff** (§6.3 ข้อ 2)
6. Play: รัน flow trace + ตัวตรวจเฉพาะ stage + `check_yarn_contract.py --check`
7. อัปเดต README stage log + plan §7 → `git add` **ระบุ path** → commit (ข้อความอังกฤษ ลงท้าย `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`)

### 6.3 กับดัก (เจอมาแล้ว)

1. **`[GameLoop]` ใน `New Drag Drop System.unity` เป็นสำเนา unpack ไม่ผูกกับ prefab** `[04]Prefab/GameSystemPrefab/[GameLoop].prefab` เก่ากว่า scene และถูกใช้เฉพาะ dev scene `New Cocktail System`, `Garnish Mockup` (ไม่อยู่ใน build) ทุก stage แก้ที่ object ใน scene จริง แตะ prefab เฉพาะกรณีที่ถ้าไม่แตะ dev scene จะพัง (ทำแล้วใน 4b)
2. **save scene จาก editor สร้าง noise ของ Cinemachine** (FOV, far clip, ตำแหน่ง/การหมุน camera) เจอทุกครั้ง 3 hunk ขั้นตอน: `git diff -U3 -- <scene> > full.patch` → ใช้ python แยก hunk (`re.split(r"(?m)^(?=@@ )", text)`), ทิ้ง hunk ที่มี `m_LocalRotation`/`m_LocalPosition`/`value:` ของ camera → `git checkout HEAD -- <scene>` → `git apply --recount --whitespace=nowarn filtered.patch` → ใน Unity `EditorSceneManager.OpenScene(path)` ให้ memory ตรงกับไฟล์
3. **`refresh_unity` ที่ `wait_for_ready=true` timeout ได้** ใช้ `wait_for_ready=false` แล้วรอ ~30 วินาที ตรวจ `EditorApplication.isCompiling`/ชนิดที่เพิ่ม ก่อนใช้ type ใหม่ใน `execute_code`
4. **`execute_code`**: `Object` กำกวม ใช้ `UnityEngine.Object`; local iterator function (`System.Collections.IEnumerator Run() { yield ... }`) ใช้เป็น coroutine ผ่าน `cmds.StartCoroutine(Run())` ได้; ผลต้องเขียนไฟล์แล้วโพล `DONE`
5. **minigame ที่ถูก cancel เล่น outro นาน (>15 วินาที)** เริ่มใหม่ไม่ได้จนกว่า phase = `Idle` — รอ `Idle` ก่อน `IngredientAdded()` (ใช้ใน `serve_commit` และ `lock_fingerprint`)
6. **order สุ่ม**: `InitState(410)` ต้องเรียก**ทันทีก่อน** `cmds.PrepareDrinks()` (consumer อื่นของ `UnityEngine.Random` ทำให้ seed ต้นไม่พอ)
7. **`git add` ระบุ path เท่านั้น ห้าม `-A`** (มี `ysls.json` ที่ไม่ใช่ของเรา); ข้อความเตือน LF/CRLF ไม่เป็นไร
8. **hotkey ใน editor**: `2`–`0` = flow command (`GameFlowDebugHotkeys` บน `[Debug]`), Shift+`1`/`2`/`B`/`V` = minigame (`MinigameSystemManager`), `R` = reload scene (`Demo_KeysShortCut` บน `NPCs_And_Set`)
9. **ชื่อ child เปลี่ยนไปแล้ว**: `SystemGame` → `[CocktailSystem]`, `MiniGameSystem` → `[MiniGameSystem]`, `PlacmentSystem` → `[PlacmentSystem]` เอกสารเก่าบางที่ยังใช้ชื่อเดิม
10. Unity MCP อาจหลุด (`MCP server disconnected`) → ตรวจ editor state ก่อนแก้ต่อ
11. **หลัง `manage_editor play` ต้องรอให้พร้อมก่อนรัน snippet** ถ้ารันทันที snippet อาจติดอยู่ก่อน domain reload ของ Play mode: `GameFlowCommands.Instance` เป็น null ภายหลัง, `BaseMiniGame.Input` เป็น null (NRE ใน `ProcessedGame`), trace ค้างกลางทาง (เจอตอน Stage 5 ครั้งแรก) เช็กก่อนว่า `Instance != null` และ `GetComponent<ShakingMinigame>()` มี `Input` แล้ว (snippet ทุกตัวเริ่มต้นด้วยเช็กนี้ได้) และ **ห้ามแก้ .cs ระหว่างอยู่ใน Play**
12. **`Button.onClick.Invoke()` ไม่ดู `interactable`** trace/snippet ที่เรียกแบบนี้พิสูจน์ได้แค่ว่า handler ทำงาน ไม่ใช่ว่าผู้เล่นกดได้ สำหรับปุ่มที่ lock ได้ให้ใช้ `ExecuteEvents.Execute(go, PointerEventData, pointerClickHandler)` (ดู `method_buttons_click.snippet.cs`)
13. **ย้าย object เข้าใต้ `[CocktailSystem]` ต้องรัน lock fingerprint ซ้ำ** `IngredientButtonGroup` เก็บสมาชิกด้วย `GetComponentsInChildren<IngredientButtonUI>` ตามตำแหน่ง จึงเก็บของที่ย้ายเข้ามาเงียบๆ (4b พลาดตรงนี้: README ระบุว่า fingerprint "เหมือนก่อนแก้" แต่เป็นของ 4a) 4b-fix แก้ด้วยการลบ `IngredientButtonUI` ซากออกจากปุ่ม Shake/Stir ดังนั้นตอนนี้ **ทุก `IngredientButtonUI` ใต้ `[CocktailSystem]` คือสมาชิกที่ถูก lock** ถ้าจะเติม component นี้บนปุ่มที่ไม่ใช่ขวดต้องรู้ว่ามันจะถูก lock ตอน drink เต็ม

---

## 7. งานที่เหลือ (ละเอียดต่อ stage)

### Stage 4c · book (เลื่อนไป Stage 8)
ปุ่ม `Canvas/Panel - Hub/BTN - Open_CloseBook` ถูกเขียนจาก 4 จุด: `IngredientButtonGroup` (lock), `CocktailFlowBridge._btnBook` (เปิดกลับตอน `OnFulled`), `UIFlowBridge` (onClick → `BookUI_V2`), `TalkingWithCustomerBridge`/`CocktailFlowBridge` (ปิด book) กฎ "step ไหนกดได้" กระจายอยู่ จึงเลื่อนไปทำพร้อม presenter/`FlowStep` ใน Stage 8

### Stage 5 · Minigame Manager (**เสร็จ**)

ที่ทำจริง
- `MinigameSystemManager` ถือ `_btnBack[2]` (`BTN - Cancel Minigame` ของ Shaking/Mixing canvas) ผูก listener ใน `OnEnable`/`OnDisable` ส่ง event `BackRequested`; `Resolve(selected, fromDrink)` + `_defaultType` ย้ายมาจาก bridge (ค่า serialized 1 = Shaking เท่าเดิม); ลบ `OnStartedMinigame`/`OnEndedGame` (UnityEvent ว่าง ไม่มี listener ทั้ง scene, prefab, dev scene)
- `MinigameFlowBridge`: `OnBackRequested` → `AnotherIngredient()`; ลบ warning "no minigame to run" (ซ้ำกับ `StartMinigame` และ default คือ Shaking)
- F6 (`f6_giveup.txt` ก่อน → `.after_stage5.txt` หลัง): ก่อนแก้ Give-up/Cancel-direct ทิ้ง flow ค้างที่ 2.2; ผมแก้ให้ออกผ่าน HSM แล้ว user สั่งลบ `BG_BTN` ทั้งกลุ่ม (`BTN_Reset`, `BTN_Next`, `BTN_AddIce`: ปิดไว้ทั้ง scene + prefab, ไม่มีอะไรเปิด, 7 object) F6 จึงไม่มีให้เกิดอีกทาง UI เหลือแค่ Shift+B ใน editor ที่ตั้งใจให้ยกเลิกตรง ผู้เล่นรีเซ็ต drink ได้จากปุ่ม Reset ของ Method panel เหมือนเดิม
- ตัวตรวจทางออกที่เหลือ: `back_button` (กดปุ่ม back จริง → 2.1 → เข้า 2.2 ซ้ำได้, drink ถูกเก็บ, shaker ปลด lock)

ที่วางแผนไว้เดิม (เก็บไว้ดูว่าตัดสินอะไร):
- **ย้ายเข้า manager**: การเลือก type (`explicit > ShakerContents.RequiredMinigame > _defaultType`, อยู่ใน `MinigameFlowBridge.Resolve`), ปุ่ม cancel 4 ตัว (`_btnCancel[2]` = `BTN - Cancel Minigame` ใน Canvas_ShakingMinigame/Canvas_MixingMinigame, `_btnGiveUp` = `BG_BTN/BTN_Reset`, `_btnCancelDirect` = `BG_BTN/BTN_Next`), `LastOutcome`
- **single source of truth**: ตอนนี้ "minigame ไหน" อยู่ 3 ที่ (`MinigameState.PendingType`, `ShakerContents.RequiredMinigame`, `_defaultType`) และ `LastOutcome` อยู่ที่ `MinigameState` — Yarn `flow_minigame_type`/`flow_minigame_result`/`flow_minigame` อ่าน/เขียนตรงนี้ (ชื่อห้ามเปลี่ยน)
- **F6 (ยังไม่ได้ยืนยัน)**: Give-up / Cancel-direct เรียก `CancelMinigame()` โดยไม่ผ่าน HSM → `Cancelled` แค่บันทึก flow ค้างที่ 2.2 และ `ShakerContents.Clear()` ทำให้ `OnShakerCleared` เปิด ingredient ทั้งที่ยังอยู่ 2.2 **ต้องจำลองให้เห็นก่อนแก้** (Play → PrepareDrinks → pour → Shake → รอ `Play` → กด give-up → ดู `flow_step`)
- **เศษที่ต้องตรวจก่อนลบ**: `MinigameSystemManager._cocktailCamera` เป็น null (`ResetCamera()` เป็น no-op), `OnStartedMinigame`/`OnEndedGame` ไม่มี listener ใน `New Drag Drop System` แต่**ต้องเช็ก `New Cocktail System.unity` และ `Garnish Mockup.unity`** ก่อนลบ field; `Update()` เรียก `ProcessedGame()` ทุกเฟรมแม้ Idle และเตือน "No active minigame" ถ้าไม่มี
- ตัวตรวจ: flow trace (S03, S04, S08) + ตัวใหม่สำหรับ give-up/cancel-direct + Yarn contract

### Stage 6 · Garnish Manager (**เสร็จ**, 4 commit)

ที่ทำจริง
- **6a** `[GarnishSystem]/Canvas_Garnish` ใหม่ (อยู่ก่อน `Canvas` ใน `[GameLoop]`) รับ `Panel - Granish UI`, `Panel - Serve` และ `Garnish_Plate` (world position คงเดิม) ทั้ง scene และ prefab
- **6b** `GarnishManager` (`Assets/[02]Script/Garnish/`, บน `[GarnishSystem]`) ถือ glass/ice/ตกแต่ง/pour และปุ่ม tab/Pour/Finish/Reset/ice ของ canvas ตัวเอง; รับ drink เป็น argument (`Pour(drink)`); events `Opened`, `PourRequested`, `PourStarted`, `IceToggled`, `FinishRequested`, `ResetRequested`; `GarnishFlowBridge` เหลือ 7 slot: เข้า step → `UpdateCocktailInShaker`, กล้อง, ล็อก ingredient, `Open()`; event ของ manager → flow/cocktail
- **6c** Serve panel + ปุ่ม Serve/Remake เป็นของ `GarnishManager` (D2); `ServeFlowBridge` ถือฝั่ง flow (เปิด/ปิดตาม step, ซ่อน post-it, ส่ง `ServeDone()`/`RemakeDrink()`); ตัด writer อีก 2 ตัว (`CocktailFlowBridge._panelServe`, `ShakerPanelController._serveUI` + flag serve)
- **6d** `PlacedGlassInstance` ไม่มี static; `GarnishManager.Glass`/`ClearGlass()`; `GarnishFlowBridge` เคลียร์ glass ตอน `PrepareDrinks.Entered` และ `Serve.Exited` (เดิมอยู่ `CocktailFlowBridge`)

ค้าง/สังเกต
- `CanFinish` ยังเป็น True หลัง `ClearGlass()` จนกว่า `Open()` ถัดไป (พฤติกรรมเดิม baseline ล็อกไว้ ไม่มีผลเพราะ panel ปิดอยู่) ถ้าจะแก้ให้ reset ใน `ClearGlass()` แล้ว trace จะเปลี่ยน `canFinish=` ที่ S07/S10 ต้องบันทึกในคอมมิต
- `GlassPlacementZone` คงไว้ใต้ `[GarnishSystem]/Garnish_Plate` (D9) ไม่มีใครเรียก (`SetGlass`, static `_occupant`) ถ้าจะลบเป็นการตัดสินใจ design
- `Bar410DialogueOrderTest` ใช้ `GarnishManager` แล้ว (ต่อ `_garnish` ใน scene แล้ว) ยังไม่ได้รันจริง (ต้องมี dialogue)
- `IngredientButtonUI._action` ยังมีค่า `SetShaking`/`SetMixing`/`AddIce`/`ResetShaker` ที่ไม่มีใครใช้ (ปุ่ม method มี `MethodPanel` แล้ว) ถ้าจะตัดเป็นงานเล็กแยก
- ดูข้อ "ที่วางแผนไว้เดิม" ด้านล่างเฉพาะเมื่อต้องรู้ว่าตัดสินอะไรไปจากแผน (ข้อสรุปอยู่ใน §3)

ที่วางแผนไว้เดิม
- แยก `GarnishManager` + `Canvas_Garnish` ออกจาก `GarnishFlowBridge` (330 บรรทัด, 20 slots): tabs (3 ปุ่ม + 3 panel, ควร reset ที่ `OnEnable`), ice toggle (2 ปุ่ม, `OnEnable` → สถานะ "add"), pour/finish/reset, glass choice; `GarnishChoice` = glass + garnish item + rim (**ไม่รวม ice**)
- **Serve panel ย้ายเข้า `Canvas_Garnish`** (D2): Garnish ส่ง `ServeClicked`/`RemakeClicked`; การให้คะแนนอยู่ที่ Cocktail แล้ว (4f) ปุ่ม Serve แค่ `ServeDone()`; ตัด `ShakerPanelController._serveUI`
- **ice (D5, 4e)**: flag อยู่ที่ `CurrentCocktail.AddIce`; Garnish ส่ง `IceToggled(bool)` → `Cocktail.SetIce` → `IceChanged(bool)` กลับ ต้องไม่ยิง `Changed` (`ShakerContents.SetIce` ตั้งใจไม่ยิง กัน UI toggle เด้ง); ตรวจก่อนว่า recipe match ขึ้นกับ `AddIce` ไหม (`UpdateCocktailInShaker` รันตอนเข้า Garnish ก่อนเลือก ice)
- `PlacedGlassInstance.Current` static → instance ของ Garnish; `DestroyCurrent()` ถูกเรียกจาก `CocktailFlowBridge` 2 จุด (`OnPrepareDrinksEntered`, `OnServeExited`) ย้ายให้ Garnish เป็นเจ้าของ
- `GlassChoiceGrid`/`GarnishItemChoiceGrid`/`GarnishRimChoiceGrid` ถือ `_garnishBridge` (วงกลมกับ `_glassGrid`) → หา manager ด้วย `GetComponentInParent`, grid ฟัง `PourStarted`
- `GlassPlacementZone` (บน `[CocktailSystem]/Garnish_Plate`) คงไว้ใต้ Garnish (D9, ยังไม่ยืนยัน) ไม่มี script ใดอ้างถึง
- เก็บกวาดใน `GarnishFlowBridge`: lambda ปุ่ม ice/tab ไม่ถูก unsubscribe, `_btnAddIce`/`_btnRemoveIce` เรียก `GetComponent` ทุกครั้ง, `OnGarnishExited` ไม่มี null guard
- ตัวตรวจ: flow trace (S05–S12), lock fingerprint (L7), `serve_commit`, + ตัวใหม่สำหรับ ice round-trip/ปุ่ม tab

### Stage 7 · TalkToCustomer Manager
- `NPC_Base` ใช้ static `_registry` + Yarn command static (`SetEmotion`, `SetLookDirection`, `MoveIn`, `MoveOut`) → registry แบบ instance, **ชื่อ command เดิม**; ในฉาก NPC 3 ตัว (Walter, Owen, Isla) ใต้ `NPCs_And_Set` (อยู่นอก `[GameLoop]`)
- `SaveLoadManager.CollectNPCData/ApplyNPCData` ใช้ `FindObjectsByType<NPC_Base>` และแตะ `CurrentWaypointIndex`/`CurrentLookDirection` ตรงๆ → `Snapshot()`/`Restore()`
- รับ `YarnVariableSync` + `_dialogueRunner` + `_customerRoster` จาก `CocktailSystemManager` (ref เดียวที่ออกนอก prefab); `Cocktail.UsePreferences(ICustomerPreferences)` ผ่าน seam; `OrderScored` เป็น event แบบ **synchronous** (`wait_for_task` รอ `IsScored` — ห้าม defer ข้ามเฟรม)
- `CharacterData` (legacy) บน `NPCs_And_Set` + `Demo_KeysShortCut` ตัวที่สองอยู่ที่นั่น (live: R → `LoadScene`)
- roster ขาด Cole, Freya (design data อย่าเดา)
- ตัวตรวจ: **ต้องทดสอบ save → load จริง** (ยังไม่มีตัวตรวจ), `Bar410DialogueOrderTest` (`[Bar410DialogueOrderTest]`, ContextMenu "Run Order Test") ใช้เสริมได้

### Stage 8 · Camera Manager + presenter + book
- ตอนนี้: `CinimachineCameraSwitcher` (singleton + referenced + Yarn `Switch_Camera`/`Cut_Camera`) + `CameraController` (381 บรรทัด, Yarn `Fix_Camera`/`Reset_Camera`) + camera ID 3 ที่ (`MainCam` ใน `CocktailFlowBridge`, `GranishCam` ใน `GarnishFlowBridge`, `PrepareCam` ใน `CocktailFlowBridge._fullDrinkCameraId`) → ID table เดียว
- `TalkingWithCustomerBridge` ไม่สลับ camera (ปล่อยให้ Yarn ทำ) — หลังกลับ Talking camera ยังเป็น `GranishCam` ถ้าไม่มี Yarn (baseline S12–S14)
- `CinimachineCameraSwitcher.ApplyTransition`: comment บอก "disable previous camera" แต่โค้ด `previousVcam.enabled = true` — อย่าแก้โดยไม่ทดสอบ (เปลี่ยน blend)
- `DebugPhysicsRaycaster` อยู่บน Main Camera ใน nested prefab `CamController` (ข้ามไว้ตั้งแต่ Stage 1)
- `N_InputManager._sceneCamera` รับ `Camera` จาก Camera manager
- **`FlowStep` flat enum + presenter** (Camera, ingredient policy, Book, PostIt): ที่นี่คือที่กฎ "step → ปุ่ม book/post-it/ingredient" ควรอยู่ รวมถึงการรวม `SetInteractable` กับ `EnableInteractablePrepareDrinksPhase` (ข้อ 4a) และ 4c book; drink reset ต้องคงอยู่บน `PrepareDrinks.Entered` (flat step แยก "เข้าใหม่" กับ "กลับมาจาก cancel" ไม่ได้)

### Stage 9 · Shell + ลบ bridge
`LoadSceneButton` บน `EndShiftButton` และ `Panel_Thx` (มี Button + `SceneLoader` ใน GameObject เดียวกันแล้ว), `Canvas_Shell`, ลบ bridge ที่ว่างทีละตัว (`TalkingWithCustomerBridge`, `ServeFlowBridge`, `UIFlowBridge`, `CocktailFlowBridge`, `MinigameFlowBridge`, `GarnishFlowBridge`) รวมเป็น `FlowWiring` ไฟล์เดียวถ้ายังจำเป็น; ลบ slot `_gameLoop`/`_commands` ที่ซ้ำ (3 ตัวเป็น null อยู่แล้วและทุกอย่างยังทำงาน)

### Stage 10 (optional) · Level 1 เป็น enum
ลดโค้ดราว 130 บรรทัด มีค่าน้อย ไม่จำเป็นต่อเป้าหมาย: ไม่มี `.yarn` ไฟล์ไหนเรียก `flow_close_bar`/`flow_next_day` (ใช้แค่ `flow_prepare_drinks` 9 ครั้ง) และ user ตัดสินแล้ว (D11) ให้ L1 ขับด้วยปุ่ม/hotkey ตามเดิม ไม่ต้องให้ writer เพิ่ม command

---

## 8. คำถามที่ยังเปิด / ต้องการคำตอบจาก user

~~1. Level 1 ให้ใครขับ~~ **ตอบแล้ว (D11)**: ปุ่ม/hotkey ตามเดิม
~~2. D9 `GlassPlacementZone`~~ **ตอบแล้ว**: อยู่กับ Garnish

3. **`Canvas_Cocktail` match 0.5** ตั้งใจเดิมเป็น 0 หรือเปล่า (ผมเปลี่ยนใน 4b) — ยังไม่ได้คำตอบ
4. **Shell** เป็น `Canvas_Shell` ที่ไม่มี manager ใช่ไหม (default)
5. **F6** — ยังไม่เคยทดสอบด้วยมือ (Give-up ค้างใน 2.2?) ถ้า user รู้อยู่แล้วบอกได้ จะข้ามขั้นจำลอง
6. ทดสอบด้วยเมาส์จริงที่ MCP ทำไม่ได้: ลากขวดลง shaker, ลากผลไม้จาก tray, Shift+1/2 hotkey ขอให้ user ลองสั้นๆ หลัง Stage 5/6
7. ~~commit ได้หรือยัง~~ **ตอบแล้ว: commit เดียว** (Stage 5 + `4b-fix` + ลบ `BG_BTN`) ทำแล้ว
8. ~~`BG_BTN`~~ **ตอบแล้ว: ลบทิ้ง** (user 2026-10-13) ทั้งใน scene และ prefab
10. ~~วิธีแก้ regression 4b~~ **ตอบแล้ว: วิธี B** (user 2026-10-13) ลบ `IngredientButtonUI` ซากออกจากปุ่ม Shake/Stir ทำแล้วและตรวจแล้ว
9. warning เดิมที่ไม่ใช่ของเรา: `Can't call command <<Enable_InteractableObject SystemGame false>>, because SystemGame doesn't have the correct component` — `.yarn` ยังเรียกชื่อ `SystemGame` ซึ่งเปลี่ยนเป็น `[CocktailSystem]` แล้ว (ตรวจว่ามี object ชื่อนั้นเหลือไหม และ writer ควรเปลี่ยนเป็นชื่อใหม่ไหม) ชื่อ command ห้ามแตะ แต่ argument ใน `.yarn` เป็นของ writer

---

## 9. ไฟล์สำคัญ

| ไฟล์ | ใช้ทำอะไร |
|---|---|
| `Assets/[02]Script/Hierarchical State Machine/GameFlowCommands.cs` | Yarn/UI entry; `CommandTarget()` guard |
| `…/Level 3 - Prepare Drinks/CocktailFlowBridge.cs` | seam ที่โตที่สุด: shaker events, scoring trigger, Method panel handler, glass cleanup |
| `Assets/[02]Script/Garnish/GarnishManager.cs` | Garnish manager: glass, ice, ตกแต่ง, pour, Serve panel (Stage 6) |
| `…/Level 2 - Open Bar/GarnishFlowBridge.cs` | seam ของ step 3: เปิด/ปิด station, ส่ง drink, ice flag, finish/reset, เคลียร์ glass |
| `…/Level 3 - Prepare Drinks/MinigameFlowBridge.cs` | Stage 5 |
| `Assets/[02]Script/Cocktail System/Cocktail/CocktailSystemManager.cs` (+ `Yarn/*.cs` partial) | Cocktail manager; `ServeDrink()` idempotent; `BindShaker` |
| `Assets/[02]Script/Cocktail System/Cocktail/Shaker/IngredientButtonGroup.cs` | auto-collect ขวด |
| `Assets/[02]Script/Cocktail System/MethodPanel.cs` | panel ที่ดูแลปุ่มตัวเอง (pattern ที่จะใช้ต่อกับ Serve/Garnish panel) |
| `Assets/[02]Script/PlacementSystem/N_PlacementSystem.cs` | `Current`, `InputManager` |
| `Assets/[02]Script/NPC/NPC_Base.cs` | Stage 7 |
| `Assets/[02]Script/CameraControl/*` | Stage 8 |
| `Assets/[05]Scenes/Deverlopment/New Drag Drop System.unity` | scene จริง (ใน Build Settings) |

---

## 10. คำสั่งเริ่ม session ใหม่

> อ่าน `Docs/Bar410_Managers_Restructure_HANDOFF.md` แล้วทำต่อที่ Stage 7 (TalkToCustomer Manager) ตาม §6 และ §7 ก่อนแก้อะไร ให้ตรวจว่าอยู่ branch `GameFlow/Managers` และ working tree สะอาด (ยกเว้น `ysls.json`) จากนั้นรัน `check_yarn_contract.py --check`, flow trace, `garnish_ui` และ lock fingerprint เพื่อยืนยันว่า baseline ยังผ่าน (รอให้ Play พร้อมก่อนรัน snippet ดู §6.3 ข้อ 11) Stage 7 ต้องทดสอบ save → load จริง ซึ่งยังไม่มีตัวตรวจ

ผู้ใช้เป็นคนไทย ตอบไทยกระชับ ชื่อเทคนิคเป็นอังกฤษ; ตัดสินใจทุกอย่างผ่าน decision log; คุ้นกับ Unity MCP; ไม่ push/ไม่เปิด PR จนกว่าจะบอก
