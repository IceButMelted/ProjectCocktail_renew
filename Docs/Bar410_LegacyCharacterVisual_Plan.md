# Bar410 — Legacy Character Visual Plan (ภาพแผ่นเดียว)

สถานะ: **แผนล็อคแล้ว (2026-10-29) ยังไม่มีโค้ด** รอเจ้าของสั่ง "เริ่มลงมือได้" แล้วให้ Sonnet 5.5 ทำ
ต่อยอดจาก `Docs/Bar410_CharacterPresentation_Plan.md` (ระบบใหม่ commit `6c8508e`)

## ปัญหา
- ระบบใหม่ (`CharacterView`) แยก ตัว (Body) กับ หน้า (Face) และแยกแกน Expression กับ Look
- art ที่ได้มาจริงยังเป็น **แผ่นเดียว** แบบ `NPC_Base` เดิม:
  - แต่ละสีหน้าคือรูปทั้งตัว หันหน้าตรง
  - หันซ้าย/ขวาคือรูปทั้งตัวอีกแผ่น หน้าติดมากับรูป จึงเปลี่ยนสีหน้าไม่ได้
- ตอนนี้จึงใช้ระบบใหม่กับ art จริงไม่ได้

## ทางแก้
เพิ่ม **โหมดภาพเก่า** ใน `SO_Customer` เปิด/ปิดด้วย tick ต่อตัวละคร
- ตัวที่ art ยังเป็นแผ่นเดียวให้ tick ไว้
- ตัวที่ได้ art แยกชิ้นแล้วให้เอา tick ออก
- คำสั่ง Yarn, fade, Enter/Exit, จุดยืน และ save **เหมือนเดิมทุกอย่าง** writer ไม่ต้องรู้ว่าตัวไหนใช้โหมดไหน

## การตัดสินใจ
| # | เรื่อง | ตัดสินใจ |
|---|---|---|
| L1 | ที่เก็บ | อยู่ใน `SO_Customer` แท็บ Visual เหมือนเดิม ไม่สร้าง SO ใหม่ |
| L2 | เปิด/ปิด | tick `UseLegacyVisual` ต่อตัวละคร |
| L3 | กติกาเมื่อสั่งสลับ | **คำสั่งล่าสุดชนะ** เหมือน `NPC_Base` เดิม: สั่งสีหน้า → กลับมาหันตรงและแสดงสีหน้านั้น / สั่ง Look L หรือ R → แสดงรูปด้านข้าง (หน้าติดมากับรูป) / สั่ง Look F → แสดงสีหน้าล่าสุด |
| L4 | เลขสีหน้า | index ในลิสต์สีหน้าของโหมดเก่า (0 = Neutral) เหมือนระบบใหม่ |
| L5 | การลบ | ทุกจุดของโหมดเก่าติดป้าย `LEGACY VISUAL` ให้ grep เจอและลบได้ครั้งเดียวเมื่อ art แยกชิ้นมาครบ |

## Data: เพิ่มใน `SO_Customer` แท็บ Visual
```
[Tab "Visual"]
  bool UseLegacyVisual                          // tick: ใช้ภาพแผ่นเดียว
  ── แสดงเมื่อ tick (ShowIf) ──
  List<LegacyExpression> LegacyExpressions      // index 0 = Neutral
    string Id                                   // Neutral, Happy, Sad ...
    Sprite Sprite                               // รูปทั้งตัว หันตรง
  Sprite LegacyLookLeft                         // รูปทั้งตัว หันซ้าย (หน้าติดมา)
  Sprite LegacyLookRight                        // รูปทั้งตัว หันขวา
  ── แสดงเมื่อไม่ tick (HideIf) ──
  LookSprites ..., Expressions ...              // ของระบบใหม่ เหมือนเดิม
```
- ข้อมูลของระบบใหม่ไม่ถูกลบตอน tick แค่ซ่อนไว้ สลับกลับได้โดยไม่เสียค่า
- `IndexOf(id)` และจำนวนสีหน้าต้องอ่านจากลิสต์ของโหมดที่ใช้อยู่ ให้ `CharacterView`, Odin dropdown และ validation ใช้ตัวเดียวกัน

### Odin
| ส่วน | Attribute |
|---|---|
| tick | `[ToggleLeft]` พร้อม `[InfoBox]` อธิบายสั้นๆ ว่าโหมดนี้คืออะไร |
| ลิสต์สีหน้าเก่า | `[ShowIf(nameof(UseLegacyVisual))]`, `[TableList(ShowIndexLabels = true)]`, `[PreviewField(60)]` |
| รูปหันซ้าย/ขวา | `[ShowIf]` + `[HorizontalGroup]` + `[PreviewField(80)]` |
| ช่องที่ต้องมี | `[Required]` ที่ `Sprite` ของสีหน้า ส่วนรูปหันข้างเว้นได้ (ดูกติกาด้านล่าง) |
| ชื่อห้ามใช้ / ซ้ำ | `[ValidateInput]` ตัวเดียวกับของระบบใหม่ (`Look` / `Enter` / `Exit`, ว่าง, ซ้ำ) |
| ของระบบใหม่ | `[HideIf(nameof(UseLegacyVisual))]` |
| ตัวอย่างคำสั่ง | ช่องเดิม (`<<Isla Happy>>` / `<<Isla 1>>`) อ่านจากลิสต์ของโหมดที่ใช้อยู่ |

## View: `CharacterView`
`Apply()` ยังเป็นที่เดียวที่เปลี่ยนภาพ แยกเป็น 2 ทาง:
- **ไม่ tick:** เหมือนเดิม (Body + Face + flip + faceOffset)
- **tick:**
  - `_face` ไม่แสดง (`sprite = null`)
  - `_body.flipX = false`
  - Look F → `_body.sprite = LegacyExpressions[ExpressionIndex].Sprite`
  - Look L/R → `_body.sprite = LegacyLookLeft / LegacyLookRight`
  - ถ้ารูปด้านข้างว่าง ใช้รูปสีหน้าปัจจุบันแทน และขึ้น warning
- **กติกาคำสั่งล่าสุดชนะ (L3):** ใน `SetExpression` ถ้าโหมดเก่าให้ตั้ง `Look = Forward` ก่อน `Apply()`
  - ระบบใหม่ไม่เปลี่ยน (สีหน้ากับท่าเป็นอิสระกัน)
- ปุ่ม Preview / dropdown ใช้ลิสต์ของโหมดที่ใช้อยู่
- fade tint ทุก SpriteRenderer อยู่แล้ว ไม่ต้องแก้

## Save
- ไม่ต้องเปลี่ยน `NPCData` (expressionIndex + look ใช้ได้ทั้ง 2 โหมด)
- `Restore` ต้องเรียก **`SetExpression` ก่อน `SetLook`** ตามลำดับที่โค้ดตอนนี้ทำอยู่แล้ว ไม่งั้นโหมดเก่าจะดึงท่ากลับเป็นตรง ใส่ comment กันคนสลับลำดับ

## ข้อมูลเริ่มต้น
- รูปแผ่นเดียวของ Isla, Owen, Walter ยังอยู่ใน prefab เก่า `Assets/[04]Prefab/Character/NPC_*.prefab` (ฟิลด์ `_neutralSprite`, `_happySprite`, `_upsetSprite`, `_lookLeftSprite`, `_lookRightSprite`)
- ให้ดึงจาก prefab พวกนี้มาใส่ asset 3 ตัว (upset → `Sad`) แล้ว tick `UseLegacyVisual`
- จากนั้นลบ prefab 3 ตัว (อ้างสคริปต์ `NPC_Base` ที่ถูกลบไปแล้ว) ต้องอ่าน GUID ของ sprite จาก YAML ก่อนลบ
- อีก 5 ตัวยังไม่มี art ปล่อยไม่ tick ใช้ placeholder ของระบบใหม่ไปก่อน
- **scale ของ Body:** รูปแผ่นเดียวเดิมใช้ scale 0.18 บน `NPC_Image` เดิม (ตอนนี้คือ `Body`) ซึ่งเท่ากับที่ใช้อยู่ ต้องดูด้วยตาว่าขนาดถูก

## ป้ายสำหรับลบทีหลัง (L5)
- ทุกโค้ดของโหมดเก่าอยู่ใต้ comment เดียวกัน: `// LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).`
- `grep -rn "LEGACY VISUAL" Assets` ต้องเจอทุกจุด
- **เช็คลิสต์ตอนลบ (เมื่อ art แยกชิ้นมาครบทุกตัว):**
  1. ทุก `SO_Customer` ไม่ได้ tick แล้ว
  2. ลบทุกบรรทัดที่ grep เจอ (ฟิลด์ใน `SO_Customer`, ทางแยกใน `CharacterView.Apply` / `SetExpression` / `IndexOf`)
  3. compile + `TEST_AllCommands` ใน Play
  4. ลบแถวนี้ใน CLAUDE.md หัวข้อ "สิ่งที่ต้องแก้"

## ลำดับงาน (compile-check ทุก stage)
1. `SO_Customer`: ฟิลด์ใหม่ + Odin + ให้ `IndexOf` / จำนวนสีหน้า / `GetFace` เลือกตามโหมด
2. `CharacterView`: ทางแยกใน `Apply`, กติกา L3 ใน `SetExpression`, Preview + comment ลำดับใน `Restore`
3. Data: ดึงรูปจาก prefab เก่าเข้า Isla / Owen / Walter, tick, ลบ prefab 3 ตัว
4. ทดสอบใน Play: `TEST_NPC` และวันที่ 1 กับตัวละครที่ tick (สีหน้า, หันซ้าย/ขวา, หันกลับตรงแล้วสีหน้าล่าสุดกลับมา, สั่งสีหน้าขณะหันข้างแล้วกลับมาตรง, fade, Snapshot/Restore)
5. อัปเดตเอกสาร: `Docs/YarnCommands/CharacterLook.md` + `CharacterExpression.md` (กติกาโหมดเก่า), CLAUDE.md (ตาราง Component + สิ่งที่ต้องแก้), แผนนี้ (log)

## ยังไม่ครอบคลุม
- ตัวละครที่มีทั้งรูปแผ่นเดียวบางสีหน้าและแยกชิ้นบางสีหน้าในตัวเดียวกัน: ไม่รองรับ เลือกได้โหมดเดียวต่อตัว


## Implementation log (2026-10-29)
ทำตามแผนครบ 5 stage ยังไม่ commit
- **Stage 1** `SO_Customer.cs`: เพิ่ม `UseLegacyVisual`, `LegacyExpressions` (`LegacyExpression` {Id, Sprite}), `LegacyLookLeft/Right` พร้อม Odin ตามตาราง (ของระบบใหม่ `HideIf`) เพิ่ม `ExpressionCount` / `ExpressionId(i)` ให้ `IndexOf`, ตัวอย่างคำสั่ง, validation (`ValidateIds` ตัวเดียวกันทั้ง 2 ลิสต์) อ่านจากลิสต์ของโหมดที่ใช้อยู่ และ `GetLegacySprite(expression, look)` (รูปข้างว่าง = รูปสีหน้า + warning)
- **Stage 2** `CharacterView.cs`: `Apply()` มีทางแยกโหมดเก่า (face = null, `flipX` false, body = รูปตามสีหน้า/ท่า) `SetExpression` ตั้ง `Look = Forward` ก่อน `Apply()` ในโหมดเก่า dropdown / bounds ใช้ `ExpressionCount` `TalkToCustomerManager.Characters.cs` `Restore` มี comment ว่าต้อง `SetExpression` ก่อน `SetLook`
- **Stage 3** ข้อมูล: ดึง GUID จาก YAML ของ prefab `NPC_Isla/Owen/Walter` ใส่ `Isla.asset` / `Owen.asset` / `Walter.asset` (Neutral, Happy, Sad = upset, LookLeft, LookRight) และ tick `UseLegacyVisual` อีก 5 ตัวไม่ tick **ไม่ได้ลบ prefab 3 ตัว:** `New Drag Drop System.unity` (`NPCs_And_Set` › `NPC_Isla/Owen/Walter`) ยังเป็น prefab instance ของมัน (ถือ `CharacterView` เป็น override) และ `New Cocktail System.unity` / `New Expression.unity` ก็อ้างถึง การลบจะทำให้ instance พังและฉากหลักเสีย ต้องให้เจ้าของตัดสินใจ (แก้ฉาก unpack/แทน prefab ก่อน)
- **Stage 4** Play test (ฉาก `New Drag Drop System`, สั่งผ่านโค้ด Isla ที่ tick + Cole ที่ไม่ tick): สีหน้าชื่อ/เลข, Look L/R ได้รูปข้างและ face ว่าง, Look F กลับรูปสีหน้าล่าสุด, สั่งสีหน้าตอนหันข้างแล้วกลับมาตรง, รูปข้างว่างขึ้น warning และใช้รูปสีหน้า, สีหน้าไม่มีขึ้น warning, `ShowInstant` / `HideInstant` / `Enter` (fade), Snapshot/Restore (Happy + Right กลับมาถูกลำดับ), Cole ไม่ tick ทำงานเหมือนเดิม scale Body = 0.18 ตามที่แผนระบุ **ยังไม่ได้ดูด้วยตา** (ตัวละครอยู่นอกจอที่มุมกล้องตอนทดสอบ) และยังไม่ได้รัน `TEST_NPC` ด้วย Yarn จริง
- **Stage 5** เอกสาร: `CharacterLook.md`, `CharacterExpression.md`, `CLAUDE.md`, ไฟล์นี้

## Revision (2026-10-29): ช่อง Look F แยก + คำสั่งล่าสุดชนะ

เจ้าของยืนยันว่ารูป Look กับรูปสีหน้าเป็นคนละชุด (F / L / R เป็นรูปทั้งตัว 3 ช่อง ส่วนสีหน้าแต่ละอันเป็นรูปทั้งตัวแผ่นเดียว) และ L3 คงไว้ ข้อที่เปลี่ยนจากแผนเดิม:
- **เพิ่ม `LegacyLookForward`:** Look F แสดงรูปนี้ (เดิม Look F แสดงสีหน้าล่าสุด)
- **`CharacterView.LegacyShowsLook`:** จำว่าคำสั่งล่าสุดคือ Look (แสดงรูป Look) หรือสีหน้า (แสดงรูปสีหน้า ตั้ง Look = Forward)
- **Fallback `GetLegacyLook`:** ข้างที่ว่างใช้อีกข้างพลิก (`_body.flipX`) ไม่มีเลยใช้รูปสีหน้าปัจจุบัน + warning
- **Save:** ไม่ได้เก็บว่าอันไหนมาหลัง `Restore` ตั้งสีหน้าก่อน แล้วสั่ง Look เฉพาะถ้าหันข้าง ข้อจำกัด: save ตอน Look F ที่สั่งหลังสีหน้า load แล้วได้รูปสีหน้าแทน
- **Data:** Isla / Owen / Walter ตั้ง Look F = รูป Neutral เดิม (`SPR_*_Default`) เปลี่ยนได้ใน Inspector
- ทดสอบด้วยโค้ดใน edit mode (Isla): Happy → Look L → Sad → Look F → Look R → สีหน้า 1 ได้รูปถูกทุกขั้น, fallback พลิก, ไม่มี Look F ใช้รูปสีหน้า ยังไม่ได้ดูภาพจริง
