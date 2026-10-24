# Cocktail Crafter V2

ระบบน้ำในแก้วค็อกเทลแบบ 2D: ผิวน้ำกระเพื่อมและโยกตอนเท, สายน้ำโค้งจากปากขวด, และน้ำแข็งที่ตกลงมาทีละก้อนแล้วชนกันในแก้ว

ทุกชิ้นเป็น world object (`SpriteRenderer`, `LineRenderer`, `SpriteMask`, Physics2D) ไม่ได้อยู่ใน Canvas และไม่ใช้ Render Texture

Design spec: `NewCocktail — Design Spec v2.md` (ดูหัวข้อ [ต่างจาก spec](#ต่างจาก-spec-v2) ด้วย)

---

## สารบัญ

1. [เริ่มต้นใช้งาน](#เริ่มต้นใช้งาน)
2. [ไฟล์ในโฟลเดอร์](#ไฟล์ในโฟลเดอร์)
3. [ระบบทำงานยังไง](#ระบบทำงานยังไง)
4. [โครงสร้าง Prefab และลำดับการวาด](#โครงสร้าง-prefab-และลำดับการวาด)
5. [ค่าที่ตั้งได้](#ค่าที่ตั้งได้)
6. [Shader](#shader)
7. [วิธีทำงานที่ใช้บ่อย](#วิธีทำงานที่ใช้บ่อย)
8. [เรียกใช้จากโค้ด](#เรียกใช้จากโค้ด)
9. [แก้ปัญหา](#แก้ปัญหา)
10. [ข้อจำกัดและ Performance](#ข้อจำกัดและ-performance)
11. [ทำเป็น Package](#ทำเป็น-package)
12. [ต่างจาก spec v2](#ต่างจาก-spec-v2)

---

## เริ่มต้นใช้งาน

1. เปิดฉาก `Demo/CocktailDemo.unity` (ตั้งค่าไว้ครบแล้ว: แก้ว, ขวด, กล้อง, ไฟ, Volume)
2. กด Play
3. ใช้ปุ่มมุมซ้ายบน (มาจาก `CocktailDemo`)
   - **Next Menu**: สลับเมนู
   - **Add Ice**: ปล่อยน้ำแข็ง (กดได้เฉพาะตอนแก้วยังว่าง เพราะน้ำแข็งต้องใส่ก่อนน้ำเสมอ)
   - **Pour**: เท
   - **Reset**: เทน้ำทิ้งและเอาน้ำแข็งออก

ทุกอย่างที่ระบบใช้อยู่ในโฟลเดอร์นี้ ไม่มี reference ไปที่อื่นนอกจาก package ของ Unity (URP, Shader Graph)

**ใส่ในฉากของเกม:** ลาก `Cocktail_V2.prefab` กับ `PourController.prefab` เข้าฉาก แล้วเรียก API ตาม [เรียกใช้จากโค้ด](#เรียกใช้จากโค้ด) ไม่ต้องใช้ `CocktailDemo`

---

## ไฟล์ในโฟลเดอร์

| ไฟล์ | หน้าที่ |
| --- | --- |
| `Scripts/CocktailGlass.cs` | ตัวแก้ว: จำลองผิวน้ำ, ระดับน้ำ, ปล่อยน้ำแข็ง, ส่งค่าเข้า shader, วาด gizmo |
| `Scripts/PourController.cs` | การเท: เล่น clip ขวด, สายน้ำ, จังหวะน้ำขึ้น, ยิงคลื่น, fade ขวด |
| `Scripts/BottleEvents.cs` | รับ Animation Event จาก clip แล้วส่งต่อให้ `PourController` |
| `Scripts/CocktailMenu.cs` | ScriptableObject ของเมนู (สี Top/Bottom) |
| `Scripts/LiquidSettings.cs` | ScriptableObject ค่ารวมทั้งเกม (`Liquid Opacity`) |
| `Scripts/CocktailDemo.cs` | ปุ่มทดสอบ (`OnGUI`) ใช้ในฉาก lab เท่านั้น ไม่ใช่ส่วนของระบบ |
| `Scripts/Editor/CocktailGlassEditor.cs` | ปุ่ม **Generate Inner Collider From Mask** และ warning ถ้าระยะเผื่อคลื่นแคบเกิน |
| `Cocktail_V2.prefab` | Prefab แก้ว |
| `PourController.prefab` | Prefab ขวด + สายน้ำ + Animator (ใช้ร่วมทุกแก้ว) |
| `Demo/CocktailDemo.unity` | ฉากตัวอย่างที่ตั้งค่าครบ |
| `Demo/CocktailDemoProfile.asset` | Volume profile ของฉากตัวอย่าง (Bloom, Tonemapping, Vignette) |
| `PH_Glass.png`, `PH_LiquidMasking.png`, `PH_IceCube.png` | Sprite placeholder ของแก้ว, ทรงด้านใน, น้ำแข็ง และขวด |
| `Ice.prefab` | Prefab น้ำแข็ง 1 ก้อน |
| `WaterShader_V2.shadergraph` + `Mat_Water_V2.mat` | Shader และ material ของน้ำ |
| `Animation/BottlePour.anim` | ท่าขวดตอนเท + event `StartStream` / `StopStream` |
| `Animation/BottleAnimator.controller` | State: `Idle` (default) → `BottlePour` → `Idle` |
| `Menus/*.asset` | เมนูตัวอย่าง |
| `LiquidSettings.asset` | ค่ารวมทั้งเกม |
| `StreamPixel.png` | Sprite สี่เหลี่ยมขาว (pivot ขอบบน สูง 1 unit) ไว้ใช้เป็น placeholder |

---

## ระบบทำงานยังไง

### หลักการ

**C# คำนวณว่าผิวน้ำสูงเท่าไร, shader แค่วาดตาม**

```
CocktailGlass (C#)                                  Shader
──────────────────                                  ──────
ผิวน้ำ 64 จุด = Spring + Slosh
        │
        ▼
Soft scale (ไม่ให้เกินขอบแก้ว)
        │
        ▼
เขียนลง texture 64×1 ─────── _SurfaceTex ─────▶ pixel อ่านความสูงตรงตำแหน่ง x ของตัวเอง
ระดับน้ำ ────────────────── _LevelY ─────────▶ อยู่ต่ำกว่าผิวน้ำ = แสดง, สูงกว่า = โปร่งใส
ขอบแก้ว ─────────────────── _InnerBottomY/TopY ▶ Gradient สี Bottom → Top
```

### ผิวน้ำ: 2 ส่วนบวกกัน

**Spring (แอ่งกลางแก้ว)**
- ผิวน้ำเป็นจุด 64 จุดเรียงจากขอบซ้ายถึงขอบขวาของแก้ว
- แต่ละจุดเป็นสปริงที่ดึงกลับเข้าระดับน้ำ (`Stiffness`) และโดนจุดข้าง ๆ ดึงด้วย (`Spread`) พอจุดกลางยุบ ระลอกจะวิ่งออกไปหาขอบ
- สายน้ำที่ตกกลางแก้วจะกดจุดกลางลงทุก `Impulse Interval` วินาที

**Slosh (โยกซ้าย-ขวา)**
- ใช้ค่าเดียว คือความสูงที่ขอบขวา ขอบซ้ายเป็นค่าตรงข้าม จุดที่อยู่ระหว่างกลางไล่เป็นเส้นตรง
- ทำงานเหมือนลูกตุ้ม: มีแรงดึงกลับ (`Slosh Stiffness`) และแรงหน่วง (`Slosh Damping`)
- ทุกครั้งที่เติมแรง จะดันไปทางเดียวกับที่น้ำกำลังโยกอยู่ (เหมือนผลักชิงช้า) น้ำเลยโยกต่อเนื่องตลอดการเท ตอนเริ่ม ฝั่งที่สายน้ำไหลไปหาจะยกขึ้นก่อน

**Soft scale**
- ถ้ายอดคลื่นจะเกิน `Headroom Usage` ของพื้นที่ว่าง (ทั้งด้านบนถึง `InnerTop` และด้านล่างถึง `InnerBottom`) จะย่อผิวน้ำทั้งเส้นตามสัดส่วน ทรงคลื่นเหมือนเดิม แค่เตี้ยลง
- มี hard clamp ต่อท้ายเผื่อพลาด น้ำจึงไม่ทะลุ mask
- น้ำยิ่งน้อย ยิ่งโยกได้น้อย เพราะพื้นที่ด้านล่างแคบ

**ประหยัด:** เมื่อทุกจุดนิ่งแล้ว ระบบจะหยุดคำนวณและไม่ upload texture

### ลำดับการเท

```
Pour() ─▶ ย้าย PourController ไปที่ InnerTop + Glass Offset
       ─▶ เล่น clip BottlePour ──┬─ ขวด fade in (Fade In Duration แรกของ clip)
                                 │
                       event StartStream
                                 │
                                 ├─ สายน้ำยืดตามเส้นโค้งจนถึงแก้ว (ความยาวเส้น ÷ Fall Speed)
                                 ├─ น้ำขึ้นจนถึง FillLine + ยิงคลื่นทุก Impulse Interval
                                 │     เวลาน้ำขึ้น = (StopStream − StartStream) − เวลาตก
                                 │
                       event StopStream
                                 │
                                 ├─ หางสายน้ำหลุดจากปากขวดแล้วตกลงแก้ว
                                 ├─ ขวด fade out (Fade Out Duration สุดท้ายของ clip)
                                 ▼
                          clip จบ → ซ่อนขวด
```

- **ตำแหน่งขวดผูกกับแก้ว:** key ของ `Bottle` ใน clip เป็นตำแหน่งเทียบกับ `PourController` และโค้ดย้าย `PourController` ไปวางเทียบกับ `InnerTop` ของแก้วก่อนเทเสมอ clip เดียวเลยใช้ได้กับแก้วทุกใบ ไม่ว่าจะสูงแค่ไหนหรือวางตรงไหน
- **เวลาเททั้งหมดอยู่ใน clip.** อยากเทนานขึ้น ให้ลาก event `StopStream` ใน Animation window น้ำจะขึ้นช้าลงตามเอง
- **สายน้ำเป็นเส้นโค้ง Bezier:** เริ่มที่ `Mouth` พุ่งออกตามทิศที่ปากขวดชี้ แล้วโค้งลงกลางแก้วที่ความสูงผิวน้ำ ปากขวดเลยไม่ต้องอยู่ตรงเหนือแก้วเป๊ะ
- **ตอนหยุดเท:** เส้นโค้งล็อกไว้ที่จังหวะหยุด หางเลยไม่สะบัดตามขวดตอนตั้งกลับ

### น้ำแข็ง

1. ปล่อยทีละก้อน ห่างกัน `Ice Drop Interval` ที่ตำแหน่งสุ่มระหว่าง `IceSpawnLeft` กับ `IceSpawnRight` และสุ่มมุม 0–360°
2. ขนาดสุ่มเป็นตัวคูณจาก `Ice Scale Range` บน scale ของ prefab
3. โผล่ที่ขนาด 0 แล้วขยายจนเต็ม (เด้งเกินนิดหน่อย) ใน `Ice Grow Duration` ระหว่างนี้ปิด physics ไว้ ก้อนเลยลอยนิ่ง
4. ขยายเต็มแล้วเปิด physics ก้อนตกลง ชนกันเอง และค้างอยู่ใน `InnerCollider`

**Ice Proxy:** Physics2D ทำงานบนระนาบ XY ของ world เท่านั้น ถ้าคำนวณน้ำแข็งในแก้วจริงตรง ๆ แก้วที่หันไปทางอื่นจะพัง ระบบเลยแยกเป็น 2 ชุด

- ตอนเริ่ม แต่ละแก้วสร้าง object `<ชื่อแก้ว> (Ice Proxy)` ไว้ที่ (2000, 2000) ใน world ข้างในมี `InnerCollider` ที่ก๊อปมาแบบไม่หมุน
- น้ำแข็งที่มี physics อยู่ใน proxy (ซ่อน renderer ไว้)
- น้ำแข็งที่เห็นอยู่ใต้ `IceRoot` ของแก้ว เป็นสำเนาที่ถอด Rigidbody2D / Collider2D ออก ทุก frame จะก๊อปตำแหน่ง มุม และขนาดจาก proxy มาวางในพื้นที่ของแก้ว
- ผลคือแก้วหันไปทางไหน หรือเลื่อนไปไหนก็ได้ น้ำแข็งตามไปด้วย และ `InnerCollider` ของแก้วจริงถูกปิดตอน Play
5. ตกนิ่งแล้ว Rigidbody2D จะ sleep เอง ไม่เปลี่ยนเป็น Kinematic (เผื่อทำน้ำแข็งลอยทีหลัง)

น้ำวาดทับหน้าน้ำแข็งแบบโปร่งแสง น้ำแข็งเลยถูกย้อมเป็นสีน้ำเอง น้ำแข็งไม่มีผลกับผิวน้ำ

---

## โครงสร้าง Prefab และลำดับการวาด

### `Cocktail_V2.prefab`

```
Cocktail_V2                 CocktailGlass
├─ GlassBack                SpriteRenderer      order 0
├─ IceRoot                  (น้ำแข็งถูก spawn เป็นลูกของตัวนี้)
├─ Liquid                   SpriteRenderer      order 3   Mat_Water_V2, Visible Inside Mask
├─ LiquidMask               SpriteMask          (ทรงด้านในแก้ว)
├─ GlassFront               SpriteRenderer      order 4
├─ InnerCollider            EdgeCollider2D      (ขอบด้านใน เปิดด้านบน)
├─ InnerBottom              ┐
├─ InnerTop                 │  Marker: ลากวางใน Scene view
├─ FillLine                 │
├─ IceSpawnLeft             │
└─ IceSpawnRight            ┘
```

### `PourController.prefab`

```
PourController              PourController
├─ Bottle                   Animator (BottleAnimator) + BottleEvents
│  └─ BottlePivot
│     ├─ Body               SpriteRenderer      order 5
│     ├─ Neck               SpriteRenderer      order 5
│     └─ Mouth              (จุดเริ่มสายน้ำ แกน Y ชี้ออกจากปากขวด)
└─ Stream                   LineRenderer        order 2
```

### ลำดับการวาด (หลัง → หน้า)

| Order | ชิ้น |
| --- | --- |
| 0 | GlassBack |
| 1 | Ice |
| 2 | Stream |
| 3 | Liquid |
| 4 | GlassFront |
| 5 | Bottle |

ลำดับนี้ใช้ Sorting Order ของ `SpriteRenderer` / `LineRenderer` ถ้าแก้ order ต้องรักษาลำดับนี้ไว้ ไม่งั้นน้ำแข็งจะลอยหน้าน้ำ หรือสายน้ำจะทับผิวน้ำ

---

## ค่าที่ตั้งได้

### `CocktailGlass` (บนแก้ว)

**Assigns**: ต่อไว้ใน prefab แล้ว ปกติไม่ต้องแตะ

| ค่า | ความหมาย |
| --- | --- |
| `Liquid Renderer` | SpriteRenderer ของน้ำ |
| `Liquid Mask` | SpriteMask ทรงด้านในแก้ว ใช้ตอนกดปุ่ม **Generate Inner Collider From Mask** |
| `Inner Collider` | EdgeCollider2D ด้านในแก้ว ใช้ทั้งกันน้ำแข็งและกำหนดความกว้างผิวน้ำ |
| `Ice Root` | ตัวแม่ของน้ำแข็งที่ spawn |
| `Ice Prefab` | Prefab น้ำแข็ง (ต้องมี Rigidbody2D) |
| `Settings` | `LiquidSettings.asset` |

**Markers**: ลากวางใน Scene view ไม่ต้องพิมพ์ตัวเลข

| Marker | ความหมาย |
| --- | --- |
| `InnerBottom` | ก้นด้านในแก้ว = จุด 0 ของ gradient และระดับน้ำตอนว่าง ใช้แกน X เป็นกลางแก้วด้วย |
| `InnerTop` | ขอบบนด้านในแก้ว = จุด 1 ของ gradient และเพดานของคลื่น |
| `FillLine` | ระดับที่เทถึง ทุกเมนูเทถึงระดับเดียวกัน |
| `IceSpawnLeft` / `IceSpawnRight` | ช่วงที่น้ำแข็งโผล่ ควรอยู่เหนือปากแก้วและแคบกว่าปากแก้ว |

**Ice**

| ค่า | ค่าเริ่ม | ความหมาย |
| --- | --- | --- |
| `Ice Count` | 4 | จำนวนก้อนที่ปล่อย |
| `Ice Drop Interval` | 0.25 | เวลาระหว่างก้อน (วินาที) |
| `Ice Scale Range` | 0.8–1.2 | ตัวคูณขนาดแบบสุ่ม คูณกับ scale ของ prefab |
| `Ice Grow Duration` | 0.15 | เวลาขยายจาก 0 จนเต็ม (วินาที) |

**Surface Spring** (แอ่งกลางแก้ว)

| ค่า | ค่าเริ่ม | ความหมาย |
| --- | --- | --- |
| `Surface Points` | 64 | จำนวนจุดผิวน้ำ มากขึ้น = ระลอกละเอียดขึ้น |
| `Stiffness` | 15 | แรงดึงกลับ สูง = เด้งไวและสั้น |
| `Damping` | 1.5 | แรงหน่วง สูง = นิ่งไว |
| `Spread` | 1500 | ความเร็วที่ระลอกวิ่งไปจุดข้าง ๆ |
| `Impulse Width` | 2 | ความกว้างของแอ่งตอนโดนสายน้ำ (นับเป็นจำนวนจุด) |

**Surface Slosh** (โยกซ้าย-ขวา)

| ค่า | ค่าเริ่ม | ความหมาย |
| --- | --- | --- |
| `Slosh Stiffness` | 25 | สูง = โยกเร็ว |
| `Slosh Damping` | 1.2 | สูง = หยุดโยกไว |

**Wave Limits**

| ค่า | ค่าเริ่ม | ความหมาย |
| --- | --- | --- |
| `Headroom Usage` | 0.9 | สัดส่วนของพื้นที่ว่างที่คลื่นใช้ได้ก่อนโดนย่อ |
| `Min Headroom` | 0.15 | Inspector เตือนถ้า `InnerTop − FillLine` แคบกว่านี้ |

### `PourController` (ในฉาก)

**Assigns**

| ค่า | ความหมาย |
| --- | --- |
| `Bottle` | ตัวที่มี Animator ระบบจะซ่อนหรือ fade ทุก SpriteRenderer ที่อยู่ใต้ตัวนี้ |
| `Bottle Animator` | Animator ของขวด |
| `Bottle Mouth` | จุดเริ่มสายน้ำ แกน Y (สีเขียวใน Scene view) ต้องชี้ออกจากปากขวด |
| `Stream` | LineRenderer ของสายน้ำ |

**Animation**

| ค่า | ค่าเริ่ม | ความหมาย |
| --- | --- | --- |
| `Pour State` | `BottlePour` | ชื่อ state ใน Animator **ต้องตรงกับชื่อ clip** เพราะระบบอ่าน event จาก clip ชื่อนี้ |

**Placement**

| ค่า | ค่าเริ่ม | ความหมาย |
| --- | --- | --- |
| `Glass Offset` | (5, 0, 0) | ก่อนเท `PourController` จะถูกย้ายไปที่ `InnerTop` ของแก้ว + ค่านี้ **ตามแกนของแก้ว** และหมุนตามแก้ว แก้วหันไปทางไหน ขวดก็ไปเทฝั่งเดิมของแก้วเสมอ เปลี่ยนค่านี้ = ขยับท่าเททั้ง clip ไปพร้อมกัน ตั้งค่าจากฉากได้ด้วยเมนู **Capture Glass Offset From Nearest Glass** (ดู [แก้ท่าเท](#แก้ท่าเท--ความยาวการเท)) |

**Stream**

| ค่า | ค่าเริ่ม | ความหมาย |
| --- | --- | --- |
| `Fall Speed` | 8 | ความเร็วสายน้ำตามเส้นโค้ง (unit/วินาที) ใช้ทั้งตอนเริ่มเทและตอนหางตก |
| `Stream Reach` | 0.35 | สายน้ำพุ่งออกจากปากขวดไกลแค่ไหนก่อนโค้งลง (สัดส่วนของความยาว) |
| `Stream Width` | 0.08 | ความกว้างที่ปากขวด ปลายจะเรียวเหลือ 60% |
| `Stream Points` | 16 | จำนวนจุดของเส้นโค้ง |

**Bottle Fade**

| ค่า | ค่าเริ่ม | ความหมาย |
| --- | --- | --- |
| `Fade In Duration` | 0.3 | ช่วงต้น clip ที่ขวดค่อย ๆ ชัดขึ้น |
| `Fade Out Duration` | 0.3 | ช่วงท้าย clip ที่ขวดค่อย ๆ จางหาย |

**Ripples**

| ค่า | ค่าเริ่ม | ความหมาย |
| --- | --- | --- |
| `Impulse Interval` | 0.12 | ยิงคลื่นถี่แค่ไหนระหว่างเท |
| `Impulse Strength` | 1.5 | แรงแอ่งกลางแก้ว (0 = ปิด) |
| `Slosh Strength` | 0.15 | แรงโยกซ้าย-ขวา (0 = ปิด) |

### `CocktailMenu` (asset ต่อเมนู)

| ค่า | ความหมาย |
| --- | --- |
| `Top` | สีบนของ gradient (ใช้แค่ RGB) |
| `Bottom` | สีล่างของ gradient และสีสายน้ำ (ใช้แค่ RGB) |

- สีกลางเป็นการ Lerp RGB ตรง ๆ คู่สีตรงข้ามจะออกเทาตรงกลาง
- อยากได้สีเดียวล้วน ให้ตั้ง Top = Bottom
- ช่อง A ของสีไม่มีผล ความโปร่งมาจาก `Liquid Opacity`

### `LiquidSettings` (asset เดียวทั้งเกม)

| ค่า | ค่าเริ่ม | ความหมาย |
| --- | --- | --- |
| `Liquid Opacity` | 0.85 | ความทึบของน้ำ ต่ำ = เห็นน้ำแข็งชัดขึ้น |

---

## Shader

`WaterShader_V2.shadergraph`: URP Lit, Transparent, Alpha blend, Render Face Both

### Property ที่ C# ส่งเข้ามา

ชื่อ Reference ต้องตรงตัวอักษร ถ้าเปลี่ยนชื่อใน shader ต้องแก้ใน `CocktailGlass.cs` ด้วย

| Reference | ชนิด | มาจาก |
| --- | --- | --- |
| `_ColorTop` / `_ColorBottom` | Color | `CocktailMenu` |
| `_Opacity` | Float | `LiquidSettings.liquidOpacity` |
| `_InnerBottomY` / `_InnerTopY` | Float | ตำแหน่ง Y (world) ของ marker |
| `_LevelY` | Float | ระดับน้ำปัจจุบัน (world Y) |
| `_SurfaceAxis` | Vector3 | แกนขวาของแก้ว (`transform.right`) |
| `_SurfaceMinX` / `_SurfaceMaxX` | Float | ขอบซ้าย/ขวาของ `InnerCollider` วัดตาม `_SurfaceAxis` |
| `_SurfaceTex` | Texture2D | ความสูงผิวน้ำ 64×1 (RFloat, Bilinear, Clamp) |

### ภาพรวมของ graph

```
Position (World) ─┬─ Split ─ G = worldY
                  └─ Dot Product(Position, _SurfaceAxis) = across   (ตำแหน่งตามความกว้างแก้ว ใช้ได้ทุกมุมหมุน)

Gradient:   InverseLerp(_InnerBottomY, _InnerTopY, worldY) → Saturate → Lerp(_ColorBottom, _ColorTop) = col
Brightness: factor = Min(1.5, Reciprocal(Max(Max(Max(col.R, col.G), col.B), 0.0001)))
            Base Color = col × factor        (สว่างขึ้นสูงสุด 1.5 เท่า แต่ไม่เกิน 1 จึงไม่ bloom และ hue ไม่เพี้ยน)

Surface:    u = Saturate(InverseLerp(_SurfaceMinX, _SurfaceMaxX, across))
            surfaceY = _LevelY + SampleTexture2D(_SurfaceTex, (u, 0.5)).R
Alpha:      Step(Edge = worldY, In = surfaceY) × _Opacity
```

ห้ามต่ออะไรเข้า Emission หรือ Vertex Position

---

## วิธีทำงานที่ใช้บ่อย

### เพิ่มเมนูใหม่

1. คลิกขวาใน Project → **Create → Cocktail → Menu**
2. ตั้งสี `Top` / `Bottom`
3. ส่ง asset นี้เข้า `PourController.Pour(glass, menu)` หรือใส่ใน `CocktailDemo.Menus` เพื่อทดสอบ

### เพิ่มแก้วทรงใหม่

1. **Duplicate** `Cocktail_V2.prefab` แล้วเปลี่ยนชื่อ
2. **ใส่ sprite**
   - `GlassBack` / `GlassFront`: sprite แก้วด้านหลัง/ด้านหน้า
   - `Liquid` และ `LiquidMask`: sprite ทรงด้านในแก้ว (สีขาวทึบ ใช้ตัวเดียวกันทั้งคู่ และ transform ต้องตรงกัน)
3. **ลาก marker** ใน Scene view โดยดู gizmo ประกอบ
   - เส้นเหลือง = `InnerBottom` / `InnerTop`
   - เส้นฟ้า = `FillLine`
   - แถบเขียว = ระยะเผื่อคลื่น ถ้ากลายเป็นแดงแปลว่าแคบเกิน ให้ลด `FillLine`
   - เส้นขาว = ช่วงปล่อยน้ำแข็ง
   - `InnerBottom` ต้องอยู่**กลางแก้ว**ในแนวนอน เพราะสายน้ำเล็งไปที่แกน X ของตัวนี้
4. **สร้าง `InnerCollider`**: เลือกตัวแก้ว (root) → กดปุ่ม **Generate Inner Collider From Mask** บนสุดของ Inspector
   - ระบบอ่านเส้นขอบ physics shape ของ sprite ใน `LiquidMask` แล้วตัดขอบปากแก้ว (จุดที่อยู่ในช่วง 5% บนสุดของความสูง) ออกให้น้ำแข็งตกเข้าได้
   - sprite ต้องเปิด **Generate Physics Shape** ใน import settings (ปกติเปิดอยู่แล้ว)
   - ความละเอียดของเส้นขึ้นกับ physics shape ของ sprite ถ้าอยากได้ละเอียดขึ้น ให้ปรับ Outline Tolerance ใน Sprite Editor → Custom Physics Shape แล้วกดปุ่มใหม่
   - กด Undo ได้ ถ้าอยากแต่งต่อ ให้เลือก `InnerCollider` แล้วกด **Edit Collider**
   - ความกว้างของ collider คือความกว้างของผิวน้ำด้วย
5. ตั้ง `Ice Count` ให้เหมาะกับขนาดแก้ว แล้วกด Play เทดูว่าน้ำแข็งไม่กองล้นปากแก้ว

### เปลี่ยนขวด

1. ใส่ sprite ใหม่ให้ `Body` / `Neck` หรือเพิ่ม SpriteRenderer อื่นใต้ `BottlePivot` ได้เลย (ทุก SpriteRenderer ใต้ `Bottle` จะ fade ไปพร้อมกัน)
2. ตั้ง Sorting Order ของขวดให้มากกว่า 4 (อยู่หน้าแก้ว)
3. ย้าย **`Mouth`** ไปไว้ที่ปลายปากขวด และให้แกน Y ชี้ออกจากปาก
4. เล่นใน Animation window เพื่อเช็กว่าท่าเทยังเข้ากับขวดใหม่ ถ้าขนาดเปลี่ยนมาก ให้แก้ key ตำแหน่ง หรือขยับทั้งท่าด้วย `Glass Offset`

### แก้ท่าเท / ความยาวการเท

1. **จัดฉากให้ตรงกับตอนเล่นจริงก่อน:** วาง `PourController` ไว้ที่ `InnerTop` ของแก้วในฉาก + `Glass Offset` และหมุนให้เท่ากับแก้ว ถ้าจะเปลี่ยนระยะห่าง ให้ย้าย `PourController` ไปตำแหน่งใหม่ แล้วคลิกขวาที่ component `PourController` → **Capture Glass Offset From Nearest Glass**
2. เลือก `Bottle` ในฉาก → เปิด **Window → Animation → Animation**
3. แก้ key ได้อิสระ ทั้งตำแหน่ง หมุน และ scale ของ `Bottle` และ `BottlePivot` ตำแหน่งที่ key เป็นตำแหน่งเทียบกับ `PourController` **ห้าม key ตัว `PourController` เอง** เพราะโค้ดเป็นคนย้ายตัวนี้
4. **Event 2 ตัวต้องมีเสมอ** และ `StartStream` ต้องมาก่อน `StopStream`
   - `StartStream`: ควรวางตอนขวดเอียงถึงท่าเทแล้ว
   - `StopStream`: ระยะห่างจาก `StartStream` คือเวลาเททั้งหมด ต้องนานกว่าเวลาที่สายน้ำตกถึงแก้ว ไม่งั้นจะขึ้น warning
5. ไม่ต้อง key alpha เพราะโค้ด fade ให้แล้ว
6. ถ้าสร้าง clip ใหม่ ให้ตั้งชื่อ state ใน Animator ให้ตรงกับชื่อ clip แล้วใส่ชื่อนั้นใน `Pour State`

### ปรับความรู้สึกของน้ำ

| อยากได้ | ปรับ |
| --- | --- |
| โยกแรงขึ้น | `PourController.Slosh Strength` ↑ |
| โยกเร็วขึ้น / ช้าลง | `CocktailGlass.Slosh Stiffness` ↑ / ↓ |
| โยกนานขึ้นหลังเทเสร็จ | `CocktailGlass.Slosh Damping` ↓ |
| แอ่งกลางลึกขึ้น | `PourController.Impulse Strength` ↑ |
| ระลอกวิ่งไวขึ้น | `CocktailGlass.Spread` ↑ |
| ผิวน้ำนิ่งเร็วขึ้น | `CocktailGlass.Damping` ↑ |
| เห็นน้ำแข็งชัดขึ้น | `LiquidSettings.Liquid Opacity` ↓ |

ปรับระหว่าง Play ได้ทันที แต่ค่าจะหายเมื่อหยุด Play ถ้าได้ค่าที่ชอบ ให้คลิกขวาที่ component → **Copy Component** แล้วออกจาก Play ไปทำ **Paste Component Values**

---

## เรียกใช้จากโค้ด

```csharp
public CocktailGlass glass;
public PourController pour;
public CocktailMenu menu;

// ใส่น้ำแข็ง (ต้องทำก่อนเทเสมอ)
if (glass.Level01 <= 0f && !glass.IsDroppingIce) glass.DropIce();

// เท
if (!pour.IsPouring && !glass.IsDroppingIce) pour.Pour(glass, menu);

// ล้างแก้ว
glass.ResetGlass();
```

| API | ใช้ทำอะไร |
| --- | --- |
| `CocktailGlass.DropIce()` | ปล่อยน้ำแข็งตาม `Ice Count` |
| `CocktailGlass.ResetGlass()` | เอาน้ำแข็งออก ระดับน้ำ = 0 ผิวน้ำนิ่ง |
| `CocktailGlass.Level01` | ระดับน้ำ 0 = ว่าง, 1 = ถึง `FillLine` |
| `CocktailGlass.IsDroppingIce` | ยังปล่อยน้ำแข็งอยู่ไหม |
| `CocktailGlass.SetMenu(menu)` | เปลี่ยนสีน้ำทันทีโดยไม่เท |
| `PourController.Pour(glass, menu)` | เทเมนูนี้ลงแก้วนี้ ถ้ากำลังเทอยู่จะไม่ทำอะไร |
| `PourController.IsPouring` | ยังเทอยู่ไหม (รวมช่วงหางสายน้ำตก) |

`PourController` ตัวเดียวใช้เทได้ทุกแก้ว แต่เทได้ทีละแก้ว

### สิ่งที่โค้ดเกมต้องกันเอง

ระบบไม่ได้ตรวจลำดับการเรียกให้ ฝั่งเกมต้องเช็กก่อนเรียก:

| ห้าม | เพราะ | เช็กด้วย |
| --- | --- | --- |
| เรียก `ResetGlass()` ระหว่างเท | น้ำจะขึ้นต่อหลังรีเซ็ต เพราะ `PourController` ยังเทอยู่ | `!pour.IsPouring` |
| ทำลาย (Destroy) แก้วระหว่างเท | `PourController` ยังอ้างถึงแก้ว จะเกิด error | `!pour.IsPouring` |
| เทระหว่างน้ำแข็งยังตก | น้ำแข็งตกลงไปในน้ำที่กำลังขึ้น ผิด spec ที่ให้ใส่น้ำแข็งก่อนเสมอ | `!glass.IsDroppingIce` |
| ใส่น้ำแข็งหลังเทแล้ว | ผิด spec และน้ำแข็งจะตกลงไปในน้ำ | `glass.Level01 <= 0f` |

**ค่า Physics2D ของทั้งโปรเจกต์มีผลกับน้ำแข็ง:** Ice Proxy ใช้ Physics2D ปกติของเกม ถ้าเกมเปลี่ยนค่าเหล่านี้ น้ำแข็งจะได้รับผลด้วย

- `Physics2D.gravity`: น้ำแข็งตกเร็วหรือช้าตาม
- **Simulation Mode = Script**: น้ำแข็งจะไม่ขยับ จนกว่าเกมจะเรียก `Physics2D.Simulate` เอง
- **Layer Collision Matrix**: น้ำแข็งกับ collider ใน proxy อยู่ layer ของ `Ice.prefab` (Default) ถ้าปิดการชนของ layer นี้ น้ำแข็งจะทะลุแก้ว

---

## แก้ปัญหา

| อาการ | สาเหตุ / วิธีแก้ |
| --- | --- |
| ผิวน้ำเรียบ ไม่มีคลื่น หรือขึ้นลงทั้งเส้น | ใน shader `InverseLerp` ของ `_SurfaceMinX/MaxX` ต้องรับ T จาก `Dot Product(Position World, _SurfaceAxis)` ถ้าใช้ `Split.R` จะใช้ได้แค่ตอนแก้วไม่หมุน |
| น้ำเต็มแก้วตลอด ไม่สนระดับ | Alpha ไม่ได้ต่อจาก `Step × _Opacity` หรือ Reference ของ property สะกดไม่ตรง |
| น้ำเรืองแสง | มีอะไรต่อเข้า Emission อยู่ |
| ขวดไม่ขยับ / ไม่เท | `Pour State` ไม่ตรงกับชื่อ state และชื่อ clip |
| Console: `needs a StartStream event followed by a StopStream event` | clip ไม่มี event หรือชื่อ function สะกดผิด หรือ `StopStream` มาก่อน `StartStream` |
| Console: `StopStream is too close to StartStream` | ลาก `StopStream` ให้ห่างขึ้น หรือเพิ่ม `Fall Speed` |
| ขวดลอยห่างหรือชนแก้ว | ปรับ `Glass Offset` ใน `PourController` |
| แก้ `Glass Offset` แล้วขวดไม่ขยับ | clip ไป key ตำแหน่งของ `PourController` เอง ให้ย้าย key ไปไว้ที่ `Bottle` |
| ปุ่ม Generate บอกว่าไม่มี physics shape | เปิด **Generate Physics Shape** ใน import settings ของ sprite mask |
| สายน้ำโผล่กลางตัวขวด | `Mouth` ไม่ได้อยู่ที่ปากขวด |
| สายน้ำพุ่งผิดทาง | แกน Y ของ `Mouth` ไม่ได้ชี้ออกจากปากขวด |
| สายน้ำไม่ลงกลางแก้ว | `InnerBottom` ไม่ได้อยู่กลางแก้วในแนวนอน |
| น้ำแข็งตกทะลุแก้ว | `InnerCollider` ไม่ตรงขอบ หรือ `IceSpawnLeft/Right` กว้างกว่าปากแก้ว |
| น้ำแข็งลอยนอกแก้ว | กองสูงเกินปากแก้ว ให้ลด `Ice Count` หรือ `Ice Scale Range` |
| คลื่นโดนตัดเป็นเส้นตรงชนขอบ | ระยะเผื่อแคบ ให้ลด `FillLine` (Inspector จะเตือนอยู่แล้ว) |
| น้ำแข็งลอยหน้าน้ำ / สายน้ำทับผิวน้ำ | Sorting Order ไม่ตรงกับตาราง [ลำดับการวาด](#ลำดับการวาด-หลัง--หน้า) |
| Play แล้วเกมค้างตอนสลับไปหน้าต่างอื่น | Unity หยุดเดินเมื่อไม่ได้ focus เปิด **Project Settings → Player → Resolution and Presentation → Run In Background** หรือกลับมาที่หน้าต่าง Unity |

---

## ข้อจำกัดและ Performance

### ข้อจำกัด

- **น้ำแข็งชนได้แค่กันเองกับผนังแก้ว** เพราะจำลองอยู่ใน proxy ไม่ชนกับ object อื่นในฉาก
- **อย่าวาง Physics2D อื่นไว้แถว (2000, 2000)** เพราะเป็นที่อยู่ของ Ice Proxy (แต่ละแก้วห่างกัน 20 unit ตามแกน X)
- **แก้วหมุนได้ทุกทิศ** แต่ระดับน้ำและ gradient ใช้แกน Y ของ world ผิวน้ำเลยขนานพื้นเสมอ ถ้าเอียงแก้วมาก ๆ น้ำอาจล้นขอบ mask ด้านที่ต่ำลง ส่วนน้ำแข็งคำนวณในพื้นที่ของแก้ว ถ้าแก้วเอียง (หมุนรอบแกน Z) น้ำแข็งจะตกไปทางก้นแก้ว ไม่ใช่ทางพื้น การหมุนรอบแกน Y ไม่มีปัญหานี้
- **Ice prefab** ต้องมี Rigidbody2D อยู่ที่ root ส่วนที่เป็นภาพ (SpriteRenderer ฯลฯ) อยู่ที่ไหนก็ได้ ระบบจะสร้างสำเนาภาพที่ถอด physics ออกเอง
- **ระยะขวดเทียบกับแก้วเท่ากันทุกใบ:** ขวดวางเทียบกับ `InnerTop` จึงตามความสูงของแก้วได้ แต่ถ้าปากแก้วกว้างหรือแคบต่างกันมาก ขวดอาจดูห่างหรือชิดเกิน (สายน้ำยังลงกลางแก้วถูก)
- **เทได้ทีละแก้วต่อ `PourController` 1 ตัว**
- **น้ำแข็งไม่ลอย และไม่ดันระดับน้ำ**
- **ใช้ใน Canvas (UI) ไม่ได้** เพราะ `SpriteRenderer`, `SpriteMask` และ Physics2D ไม่ทำงานใน Canvas
- **`CocktailDemo` เป็นของทดสอบ** ใช้ `OnGUI` ไม่ควรใช้ในเกมจริง

### Performance

- ตอนน้ำขยับ แต่ละแก้วคำนวณราวพันการคำนวณ float ต่อ frame (ระดับไมโครวินาที) และ upload texture 256 bytes
- ตอนน้ำนิ่ง ไม่คำนวณและไม่ upload
- แต่ละแก้วสร้าง material instance ของตัวเอง น้ำ 1 แก้ว = 1 draw call ไม่มีปัญหาถ้ามีไม่กี่ใบ ถ้าวันหนึ่งมีหลายสิบใบ ควรเปลี่ยนเป็น `MaterialPropertyBlock`

---

## ทำเป็น Package

ทุกอย่างอยู่ในโฟลเดอร์นี้ ไม่มี reference ออกไปข้างนอก (ยกเว้น URP และ Shader Graph) มี 2 แบบ

**แบบที่ 1: ก๊อปโฟลเดอร์ไปไว้ใน `Assets/` ของโปรเจกต์อื่น**

ไม่ต้องตั้งค่าเพิ่ม โฟลเดอร์ `Editor` จะถูกแยกออกตอน build เอง

**แบบที่ 2: UPM package (`Packages/` หรือ git URL)**

1. เพิ่ม `package.json` ที่ root ของโฟลเดอร์ และใส่ dependency `com.unity.render-pipelines.universal` กับ `com.unity.shadergraph`
2. เพิ่ม asmdef 2 ตัว
   - `Scripts/` → Runtime asmdef
   - `Scripts/Editor/` → Editor asmdef: อ้างถึง Runtime asmdef และตั้ง **Include Platforms = Editor** อย่างเดียว ไม่งั้นจะ build ไม่ผ่าน
3. ไฟล์ใน UPM package แก้ไม่ได้ (read-only) ให้ย้าย `Demo/` และ `Scripts/CocktailDemo.cs` ไปไว้ใน `Samples~/Demo/` แล้วประกาศใน `package.json` ช่อง `samples` คนใช้กด **Import** จาก Package Manager เพื่อก๊อปฉาก demo ออกมาแก้ได้
4. `CocktailDemo.cs` ใช้กับฉาก demo เท่านั้น ถ้าอยู่ใน Samples~ ต้องมี asmdef ของตัวเองที่อ้างถึง Runtime asmdef

---

## ต่างจาก spec v2

ระหว่างทำ มีบางเรื่องเปลี่ยนไปจาก spec:

| เรื่อง | Spec v2 | ที่ทำจริง |
| --- | --- | --- |
| เวลาเท | ตัวเลขบน `PourController` | อยู่ใน clip (event `StartStream` / `StopStream`) |
| ตำแหน่งขวด | โค้ดเลื่อนขวดไปเหนือแก้ว | ท่าเทอยู่ใน clip แล้วโค้ดวาง `PourController` ตาม `InnerTop` + `Glass Offset` |
| ขวดตอนไม่ได้เท | ไม่ได้ระบุ | ซ่อนไว้ และ fade in/out ตามต้น/ท้าย clip |
| สายน้ำ | ตกตรงกลางแก้ว | เส้นโค้งจากปากขวดลงกลางแก้ว และหางค่อย ๆ ตกลงตอนหยุด |
| ช่อง `tilt` | สำรองไว้ให้การเลื่อนแก้ว (พักไว้) | ใช้เป็นการโยกซ้าย-ขวาตอนเทแล้ว ถ้าวันหนึ่งทำเลื่อนแก้ว ให้เติมแรงเข้าตัวโยกเดียวกันนี้ |
| น้ำแข็ง | สุ่มตำแหน่งและมุม | เพิ่มสุ่มขนาด และขยายจาก 0 ตอนโผล่ |
