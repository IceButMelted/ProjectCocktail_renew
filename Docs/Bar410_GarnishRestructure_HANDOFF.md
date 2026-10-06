# Bar410 — Garnish Restructure: เอกสารส่งต่องาน / HANDOFF

**Date:** 2026-09-07 · **Branch:** `GameLoop/main`
**สถานะ:** โค้ดคอมไพล์ผ่าน ไม่มี error/warning ใหม่ · `Bar410 > Validate Cocktail Data` ผ่าน ·
ทดสอบ end-to-end จริงใน Play mode (ไม่ใช่แค่อ่านโค้ด) ทุกจุดสำคัญ

> อ่านคู่กับ [CONTEXT.md](../CONTEXT.md) (คำศัพท์/กติกาที่ resolve แล้ว) และ
> [docs/adr/0001](adr/0001-remove-prepare-phase-fixed-roster.md) /
> [docs/adr/0002](adr/0002-glass-pour-fixed-position-buttons.md) (การตัดสินใจเชิงสถาปัตยกรรม)
> งานนี้ **reverse** ฟีเจอร์ "Glass Freedom" บางส่วน (ดู `Bar410_CocktailSystem_HANDOFF.md` §1b) —
> ส่วนลาก-วางของแก้ว/การเทในเอกสารนั้นล้าสมัยแล้ว ส่วนการลากวัตถุดิบ (ขวด/ผลไม้) ยังใช้ได้เหมือนเดิม

---

## 0. สรุปสั้น — เปลี่ยนอะไรไปบ้าง

มาจากคำขอเปลี่ยนแผน gameplay: ตัดการจัดร้าน (bar-layout) ออก, ล็อคกล้อง/แก้ว/shaker ให้อยู่กับที่,
และย้ายการเลือกแก้ว+ใส่น้ำแข็ง+ตกแต่งมารวมกันไว้ใน Garnish step เดียวแบบกดปุ่มทั้งหมด (ไม่ลากแล้ว)

ระหว่างทำและทดสอบจริง เจอบั๊กที่มีอยู่เดิมหลายจุดที่ไม่เกี่ยวกับงานนี้โดยตรงแต่บล็อก flow ทั้งหมด —
แก้ไปด้วยเพราะไม่งั้นทดสอบงานหลักไม่ได้เลย (ดู §3)

---

## 1. งานหลัก — ตัด Bar Layout, ล็อคกล้อง/แก้ว/Shaker

**ตัดสินใจ (ดู ADR 0001, 0002 สำหรับเหตุผล):**

- `GamePhase` เหลือแค่ `{ Open, Close }` ไม่มี `Prepare` แล้ว — ลบ `PrepareBarPhase.cs`,
  `BarSetupBridge.cs` ทิ้ง loop กลายเป็น `Open → Close → Open` ตรงๆ วัตถุดิบมีครบทุกวัน ไม่มี roster
  รายวันอีกต่อไป (ยืนยันว่า `BarSetupBridge` ไม่เคยถูกต่อเข้า scene นี้จริง — ตอนแรกเช็คผิดว่าไม่มี
  เพราะ component ที่ script โดนลบไปกลายเป็น "missing script" ที่ hierarchy listing ไม่โชว์ ต้อง sweep
  ทั้ง scene แบบ recursive ถึงเจอ)
- `CameraController.isFixedCamera` default เป็น `true` ตั้งแต่เริ่มเกม — กล้องไม่หมุนตาม mouse-edge-hover
  อีกต่อไป (`CinimachineCameraSwitcher` ที่ใช้ตัดมุมกล้องช่วงคุยกับลูกค้าเป็นคนละระบบ ไม่กระทบ)
- `GlassPlacementZone` เลิกเป็น `SurfacePlacementZone` (ไม่มี drag แล้ว) เหลือ `SetGlass(SO_GlassOption)`
  ที่ spawn แก้วให้อยู่ตำแหน่งคงที่ทันที — `PlacedGlassInstance` ตัด `DragableObject` requirement ออก
  — ลบ `GlassShelfSlot.cs`, `PourSource.cs` (เลิกใช้แล้ว)
- `GarnishFlowBridge` เพิ่ม `ChooseGlass(SO_GlassOption)` / `Pour()` / `ToggleIce(bool)` แทนกลไกลาก
  เดิมทั้งหมด — เลือกแก้วจาก UI list, กด Pour แทนลาก shaker ไปวางบนแก้ว (shaker ล็อคอยู่แล้วตั้งแต่
  minigame จบ ไม่ต้องปลดล็อคอีก)
- `Pour()` เล่น fill animation จริง (น้ำในแก้วไต่ระดับ) แล้วค่อยปลดล็อค "Finish Garnish" — ไม่ใช่
  set ทันทีเหมือนตอนแรก (`WaterSlosh.IsFilling`/`_fillCoroutine` เคลียร์ตอนจบเองแล้ว — ของเดิมมีบั๊ก
  เล็กๆ ค้างเป็น true ตลอดไปด้วย แก้ไปพร้อมกัน)

**Scene wiring** (`New Cocktail System.unity`, ทำผ่าน Unity MCP live แล้ว sync กลับเข้า
`[GameLoop].prefab` ด้วย — ทั้งสอง scene ที่ใช้ prefab นี้ได้ของครบ):

- เพิ่มปุ่ม `BTN - ChooseHiBall` / `BTN - ChooseRock` / `BTN - Pour` ใน `Panel - Granish UI`
- `Panel - AddIce` ย้ายมาโชว์/ซ่อนพร้อม Garnish (`GameFlowHooks.Garnish.OnEnter/OnExit`) แทนที่จะ
  อยู่ตอนผสม — ปุ่ม `BTN_AddIce`/`BTN_RemoveIce` เพิ่มเรียก `GarnishFlowBridge.ToggleIce` (ของเดิม
  ที่ toggle ไอคอนน้ำแข็ง/สลับปุ่มยังอยู่ครบ ไม่ได้ทุบทิ้ง)

---

## 2. Mockup scene ใหม่ — `Garnish Mockup.unity`

Scene แยกต่างหาก มีแค่ `[GameLoop]` (prefab instance) + `CamController` (prefab instance) +
`EventSystem`/`Directional Light`/`Plane` (`GlassPlacementZone`) ขั้นต่ำที่สุด — ไม่มี NPC/waypoint/
บทสนทนา ใช้ `GameFlowDebugHotkeys` (ติดมากับ `[GameLoop]` อยู่แล้ว, ปุ่ม 2–9,0) ไล่ flow แทน

ผูก `GameFlowHooks.AddIngredient.OnEnter → CocktailSystemManager.RandomCocktailForDebug()`
(**เฉพาะ scene นี้**) — เข้า AddIngredient ทีไรได้ order สุ่มให้ทำ/เสิร์ฟทุกครั้ง ทดสอบ loop เต็มรูปแบบ
โดยไม่ต้องพึ่งบทสนทนาเลย

---

## 3. บั๊กเดิมที่เจอและแก้ระหว่างทาง (ไม่เกี่ยวกับงาน restructure โดยตรง แต่บล็อก flow ทั้งหมด)

ทุกข้อยืนยันด้วย Unity MCP live Play-mode test จริง ไม่ใช่แค่อ่านโค้ด:

1. **`PlacedGlassInstance._waterSlosh` เป็น `NULL`** ใน `BaseGlass.prefab`/`Hi_Ball_Glass.prefab`/
   `Rock_Glass.prefab` (`Margarita.prefab` ผูกถูกอยู่แล้ว) — `ApplyDrink`/`ApplyIce`/`StartFill`
   เงียบไม่ทำอะไรเลย ไม่มี error ให้เห็น แก้โดยผูกให้ชี้ไปที่ `WaterSlosh` component บน object
   เดียวกันเอง
2. **สีเครื่องดื่ม alpha=0 เสมอ** — `CocktailSystemManager.UpdateCocktailInShaker()` (ตัวเดียวที่
   จับคู่ส่วนผสมกับสูตรแล้วเซ็ตสี) ไม่เคยถูกเรียกที่ไหนเลยทั้ง scene/prefab แก้โดยเพิ่ม field
   `_cocktail` ใน `GarnishFlowBridge` เรียกตอนเข้า Garnish (จุด "ผสมเสร็จแล้ว" ตาม GDD §10.1)
3. **`Panel - Serve` ไม่เคยโชว์เลย** — `GameFlowHooks.Serve.OnEnter/OnExit` ไม่เคยถูกผูกใน scene นี้
   แก้โดยผูกให้เหมือน Garnish
4. **กด Serve จริงแล้ว flow ไม่ไปไหน + แก้วไม่หาย** — `BTN_Serving.onClick` ไม่เคยเรียก
   `GameFlowCommands.ServeDone()` เลย วิ่งผ่านแค่ path เก่า `CocktailSystemManager.ServeDrink()` →
   Yarn task เท่านั้น เพิ่ม `ServeDone()` เข้าไปในปุ่ม (อยู่ใน prefab เลยได้ทั้งสอง scene) ตอนนี้กด
   Serve แล้ว state machine กลับ `TalkingWithCustomer` และ `CocktailFlowBridge.OnServeExited`
   ทำลายแก้วที่เสิร์ฟไปถูกต้อง
5. **`New Cocktail System.unity` มี `GlassPlacementZone` ซ้อนกัน 2 อัน** (`Plane`/`Plane (1)`) —
   `GarnishFlowBridge`/`CocktailFlowBridge` ชี้กันคนละอัน (ไม่พังเพราะ occupant เป็น static
   แต่สับสน) รีไวร์ให้ชี้ตัวเดียวกันแล้ว (`Plane (1)`)

**เพิ่ม fallback สำหรับ `New Cocktail System.unity`:** `CocktailSystemManager.RandomCocktailIfNoOrder()`
— สุ่ม order ให้เฉพาะตอนที่ยังไม่มีออเดอร์จากบทสนทนาเท่านั้น (ไม่ทับของจริง) ผูกเข้า
`AddIngredient.OnEnter` ที่ scene นี้ — ทดสอบได้โดยไม่ต้องพึ่ง NPC แต่บทสนทนาจริงยังทำงานปกติ

---

## 4. เครื่องมือ debug ใหม่

`CocktailInspectorReadout.cs` (`Assets/[02]Script/Cocktail System/`) — component แสดงผลอย่างเดียว
บน Inspector (ไม่มี UI Canvas) โชว์: Target (ออเดอร์ลูกค้า), Shaker (ของในแก้วตอนนี้), Resolved
(ผลจับคู่สูตร) ใช้ `DrinkFormatter` ที่มีอยู่แล้ว ไม่เขียน format ใหม่ อัปเดตทุกเฟรมผ่าน `Update()`
ใส่ไว้บน `CocktailSystem` ใน `New Cocktail System.unity` เท่านั้น (ยังไม่ได้ push เข้า prefab กลาง —
บอกได้ถ้าอยากให้มีทุก scene) ต่างจาก `DebugCocktail.cs` เดิมที่เป็น UI Text ผูกกับ `CocktailShaker`/
`CocktailShakerData` รุ่นเก่าที่ scene ปัจจุบันไม่มีคอมโพเนนต์นี้แล้ว

---

## 5. ค้างไว้ ยังไม่แตะ (flag ไว้ ไม่ใช่บั๊กของงานนี้)

- `BTN_Serving`/`BTN_Reset (2)` มี persistent listener ที่ target เป็น `null` ค้างอยู่ (ของเก่าที่
  หายไปแล้ว) และยังอ้างถึงแก้วเก่าชื่อ `CocktailGrass` (มี `WaterSlosh` ของตัวเอง คนละตัวกับ
  `PlacedGlassInstance` ใหม่) — ปัจจุบัน inactive ไม่ชนกัน แต่เป็นของค้างจากก่อน Glass Freedom
- ระบบตกแต่งแก้ว Rim/Side/Topping (ออกแบบไว้ใน `CONTEXT.md` แล้วจากเซสชันก่อน) ยังไม่ implement —
  ยัง TODO ใน `GarnishFlowBridge` ตามเดิม ตั้งใจไม่ทำรอบนี้

---

## 6. ไฟล์ที่เกี่ยวข้อง

**ลบ:** `PrepareBarPhase.cs`, `BarSetupBridge.cs`, `GlassShelfSlot.cs`, `PourSource.cs`

**แก้ไข (โค้ดหลัก):** `GameLoopFSM.cs`, `ClosingBarPhase.cs`, `GameFlowHooks.cs`,
`GameFlowCommands.cs`, `GameFlowDebugHotkeys.cs`, `InteractableToggle.cs`, `IngredientButtonGroup.cs`,
`CameraController.cs`, `GlassPlacementZone.cs`, `PlacedGlassInstance.cs`, `GarnishFlowBridge.cs`,
`CocktailFlowBridge.cs`, `WaterSlosh.cs`, `CocktailSystemManager.cs`

**ใหม่:** `CocktailInspectorReadout.cs`, `CONTEXT.md` (เพิ่มเนื้อหา), `docs/adr/0001-*.md`,
`docs/adr/0002-*.md`, scene `Garnish Mockup.unity`

**Scene/Prefab (แก้ผ่าน Unity MCP):** `New Cocktail System.unity`, `Garnish Mockup.unity`,
`[GameLoop].prefab`, `Hi_Ball_Glass.prefab`, `Rock_Glass.prefab`, `BaseGlass.prefab`

**บันทึกละเอียดเพิ่มเติม (ลำดับเหตุการณ์/สาเหตุแต่ละบั๊กแบบเต็ม):** อยู่ใน session memory ของ
Claude (`project-bar410-garnish-restructure`) — ไฟล์นี้เป็นสรุปสำหรับคนอ่าน ไม่ใช่ log ทุกขั้นตอน
