# Bar410 — คำถาม O1 (Localization) และ O4 (scene แยก Minigame/Garnish)

เขียน 2026-11-07 ให้อ่านและตอบ ช่องตอบอยู่ท้ายแต่ละส่วน ยังไม่ได้ลงมือ ไม่ได้ติดตั้ง package และไม่ได้แก้โค้ด
ที่มา: `Bar410_Rewrite_Options.md` §9 (O1, O4), §11, §15

หมายเหตุ: ข้อความที่คุณพิมพ์ในคำขอท้ายประโยค ("cpddyo .shz,sojvp") อ่านไม่ออก (น่าจะพิมพ์ผิดแป้นภาษา) ผมตีความว่าต้องการดูตัวอย่างการใช้ **Yarn อย่างเดียว** เทียบกับ **Unity Localization + Yarn รวมกัน** ถ้าหมายถึงอย่างอื่น บอกได้

โค้ดทั้งหมดในเอกสารเป็น **ร่างเพื่ออธิบาย** ยังไม่ได้คอมไพล์ในโปรเจกต์ ส่วนที่อ้าง API ผมระบุว่าตรวจจากซอร์สแล้วหรือยัง

---

# ส่วน 1 — O1: ระบบ Localization

## 1.1 ข้อเท็จจริงที่ตรวจในโปรเจกต์

| เรื่อง | ที่พบ |
|---|---|
| Unity Localization package | **ยังไม่ติดตั้ง** (ไม่มีใน `manifest.json`) |
| Line provider ที่ใช้อยู่ | `BuiltinLocalisedLineProvider` ใน prefab `Text Animator Dialogue System.prefab` |
| Yarn project หลัก | `Bar_410Demo.yarnproject`: `"localisation": {}` (ยังไม่มีภาษาเพิ่ม), `baseLanguage: "en"` |
| Line ID | `DEMO_3Days.yarn` มี `#line:` 229 จุด (ยังไม่ได้นับจำนวนบรรทัดบทพูดทั้งหมด จึงบอกไม่ได้ว่าครบหรือไม่) |
| Provider ทั้งสองแบบมี API เดียวกัน | `LocaleCode` ตั้งได้, `GetLocalizedLineAsync(Yarn.Line, ct)` รับ `Yarn.Line` เดิมแล้วคืนข้อความภาษาปัจจุบัน (ตรวจจากซอร์สของ Yarn 3.2.5) |
| `UnityLocalisedLineProvider` | ทำงานเมื่อมี define `USE_UNITY_LOCALIZATION`; ใช้ `LocalizedStringTable` + `LocalizedAssetTable`; `LocaleCode` แมปกับ `LocalizationSettings.SelectedLocale` |
| ทั้งสอง provider | รองรับ markup `[select] [plural] [ordinal]`, shadow line, และ asset ต่อ line ID (เช่น รูป/เสียง) |

ข้อสรุปจากข้อเท็จจริง: **เปลี่ยน provider ภายหลังไม่ต้องแก้ `.yarn`** (ใช้ line ID ชุดเดิม) และโค้ดฝั่งเกมที่เรียก `GetLocalizedLineAsync` ไม่ต้องเปลี่ยน ความต่างอยู่ที่ "ข้อความอื่นนอก Yarn" และ "การจัดการ asset/ฟอนต์/เครื่องมือ"

## 1.2 ทางเลือก 3 แบบ

- **แบบ 1 Yarn อย่างเดียว:** บทพูดใช้ตารางของ Yarn (CSV ต่อภาษา) ส่วนข้อความอื่นใช้ตาราง CSV ที่เราเขียนเอง
- **แบบ 2 Unity Localization + Yarn รวมกัน:** ทุกอย่างอยู่ในระบบ String/Asset Table ของ Unity รวมบทพูดผ่าน `UnityLocalisedLineProvider`
- **แบบ 3 ผสม:** บทพูดใช้ Yarn ล้วน ส่วนข้อความ/รูป/ฟอนต์อื่นใช้ Unity Localization (มีสองระบบ ต้องมีชั้นกลางสลับสองระบบพร้อมกัน)

## 1.3 ตัวอย่างการใช้งานเทียบกัน

### (ก) ตั้งค่าภาษา

**แบบ 1 — Yarn อย่างเดียว**
```jsonc
// Bar_410Demo.yarnproject  (ชื่อ key ตรวจในหน้า Inspector ของ Yarn project ก่อนใช้จริง)
"baseLanguage": "en",
"localisation": {
  "th": { "strings": "Localisation/th.csv", "assets": "Localisation/th_assets" }
}
```
บทพูดอยู่ใน `Localisation/th.csv` (คอลัมน์ id, text, ...) ที่ Yarn สร้าง/อัปเดตให้จาก `.yarn`

**แบบ 2 — Unity Localization + Yarn**
1. เพิ่ม `com.unity.localization` ใน Package Manager (ระบบจะตั้ง `USE_UNITY_LOCALIZATION` ให้)
2. `Window > Asset Management > Localization Tables` สร้าง Locale: `en`, `th`
3. สร้าง String Table Collection: `Dialogue` (บทพูด), `UI`, `Data` (ชื่อ/คำอธิบายสูตร, วัตถุดิบ, chapter, event) และ Asset Table Collection: `Sprites`, `Fonts`
4. ที่ prefab `Text Animator Dialogue System` เปลี่ยน line provider เป็น `UnityLocalisedLineProvider` แล้วผูก `Dialogue` ใน Inspector
5. Yarn ที่ติดตั้ง Unity Localization ไว้ มีโค้ดฝั่ง Editor ที่เติมตาราง `Dialogue` จาก `.yarn` ให้ (พบการอ้างถึงใน `YarnProjectImporter.cs` ยังไม่ได้ลองใช้ขั้นตอนนี้)

### (ข) สลับภาษาตอนเล่น

**แบบ 1**
```csharp
// ร่าง: LanguageService เป็นผู้ถือ locale ปัจจุบัน
public static class Loc
{
    public static string Code { get; private set; } = "en";
    public static event System.Action Changed;
    static Dictionary<string,string> _table;               // key -> text (โหลดจาก CSV ของเรา)

    public static string Get(string key) =>
        _table.TryGetValue(key, out var t) ? t : FallbackEn(key);   // ไม่มี -> EN + เตือน

    public static void Set(string code, DialogueRunner runner)
    {
        Code = code;
        _table = LoadCsv(code);                              // เราเขียนเอง
        runner.LineProvider.LocaleCode = code;               // Yarn (ตรวจจากซอร์สแล้ว)
        Changed?.Invoke();                                   // UI + ฟอนต์ + บรรทัดที่แสดงอยู่
    }
}
```

**แบบ 2**
```csharp
// สลับภาษาเป็นบรรทัดเดียว Yarn provider ตามอัตโนมัติ (LocaleCode แมปกับ SelectedLocale)
LocalizationSettings.SelectedLocale =
    LocalizationSettings.AvailableLocales.GetLocale("th");

// ฟังเมื่อภาษาเปลี่ยน เพื่อพิมพ์บรรทัดที่แสดงอยู่ใหม่ (ตัดสินใจแล้ว)
LocalizationSettings.SelectedLocaleChanged += _ => dialogueDirector.RetypeCurrentLine();
```
`RetypeCurrentLine()` เรียก `GetLocalizedLineAsync` ด้วย `Yarn.Line` ที่เก็บไว้ (เหมือนกันทั้งสองแบบ)

### (ค) ข้อความ UI

**แบบ 1** — component ของเรา
```csharp
public class LocalizedText : MonoBehaviour
{
    [SerializeField] string key;
    TMP_Text _t;
    void OnEnable() { _t ??= GetComponent<TMP_Text>(); Refresh(); Loc.Changed += Refresh; }
    void OnDisable() => Loc.Changed -= Refresh;
    void Refresh() { _t.text = Loc.Get(key); _t.font = FontFor(Loc.Code); }
}
```

**แบบ 2** — component สำเร็จรูป `Localize String Event` บน TMP (เลือก table + entry ใน Inspector) ไม่ต้องเขียนโค้ด; ฟอนต์สลับด้วย Asset Table หรือ Localize Event ของ font
```csharp
// ข้อความจากโค้ด (เช่น ชื่อเครื่องดื่ม)
[SerializeField] LocalizedString displayName;   // ใน SO แทน string ธรรมดา
label.text = displayName.GetLocalizedString();
displayName.StringChanged += s => label.text = s;   // อัปเดตเองเมื่อสลับภาษา
```

### (ง) ข้อมูลใน ScriptableObject (ชื่อ/คำอธิบายสูตร 36 อัน, วัตถุดิบ, chapter)

**แบบ 1** — SO เก็บ `string key`; `OrderDescriptionProvider` เรียก `Loc.Get(key)` และต้องฟัง `Loc.Changed` เองทุกที่
**แบบ 2** — SO เก็บ `LocalizedString` (เลือก entry ใน Inspector ได้เลย) ฟัง `StringChanged` ได้ในตัว

### (จ) รูปที่มีตัวอักษร (C4: มี)

**แบบ 1** — ต้องทำเอง: ตาราง locale → sprite + component สลับ
**แบบ 2** — Asset Table (`Sprites`) + `Localize Sprite Event` บน `Image` ไม่ต้องเขียนโค้ด

### (ฉ) ฟอนต์ไทย

**แบบ 1** — `SO_LocaleFonts` ที่เราเขียน (locale → TMP Font Asset) ให้ `LocalizedText` สลับ
**แบบ 2** — Asset Table (`Fonts`) หรือ Localize Event ของ font ผูกตาม Locale (ความเสี่ยงเรื่องตัดคำ/Text Animator ของภาษาไทยเท่ากันทั้งสองแบบ ดู Options §11.5)

### (ช) ส่งงานแปล (Outsource รับเป็น Spreadsheet)

**แบบ 1** — Yarn export CSV → เราเขียน script แปลง CSV ↔ Spreadsheet และ validator เอง; ตารางข้อความอื่น (UI/ข้อมูล) ต้องออกแบบรูปแบบ CSV ของเราเอง
**แบบ 2** — ตาราง `Dialogue/UI/Data` ทั้งหมดอยู่ในโครงเดียวกัน; package มีส่วนขยาย CSV / XLIFF และ Google Sheets (ซิงก์ตรงกับ Spreadsheet ได้ ต้องตั้งค่าบัญชี Google และสิทธิ์ ยังไม่ได้ตรวจกับโปรเจกต์นี้) ลดงาน tool ของเรา แต่ต้องยืนยันว่าการแมป Yarn line ID ↔ คอลัมน์ทำงานตามที่ต้องการ

## 1.4 เปรียบเทียบ

| หัวข้อ | 1. Yarn อย่างเดียว | 2. Unity Localization + Yarn | 3. ผสม |
|---|---|---|---|
| Dependency เพิ่ม | ไม่มี | เพิ่ม package (และ Addressables ที่มันใช้) | เพิ่ม package |
| โค้ดที่เราต้องเขียน | `Loc`, `LocalizedText`, ตารางฟอนต์/รูป, tool CSV, validator | น้อย (ใช้ component สำเร็จรูป) เหลือ glue กับ Yarn + validator | มากสุด (สองระบบ + ชั้นกลาง) |
| สลับภาษา live | เขียนเอง | มีให้ (`SelectedLocaleChanged`, `StringChanged`) | ต้องซิงก์สองระบบ |
| รูป/ฟอนต์ต่อภาษา | เขียนเอง | Asset Table | Asset Table |
| ทดสอบข้อความยาว (ไทย) | ทำเอง | มี Pseudo-Locale ในตัว (ดูข้อความขยายตัว) | มี |
| Spreadsheet/ผู้แปล | ต้องทำ tool เอง | มีส่วนขยาย CSV/XLIFF/Google Sheets | ครึ่งหนึ่ง |
| ความเสี่ยง | โค้ดเองเยอะ บั๊กเอง | โหลดแบบ async (ต้อง preload ตอนเริ่มเกม), build ใหญ่ขึ้นเล็กน้อย, ต้อง spike ร่วมกับ Text Animator | ซับซ้อนที่สุด ไม่แนะนำ |
| ย้ายภายหลัง | ย้ายไปแบบ 2 ได้ (ID เดิม) แต่ต้องเขียนข้อมูลใหม่ | ย้ายกลับยาก | — |
| ตรงกับเป้าหมาย "เพิ่ม feature ง่าย" | พอใช้ | ดีกว่า (เพิ่มภาษา/รูป/ฟอนต์ที่ 3 ได้ในตาราง) | แย่กว่า |

## 1.5 คำแนะนำของผม

**แบบ 2** เหตุผล: (1) ต้องมีรูปและฟอนต์ต่อภาษา กับสลับ live ซึ่งแบบ 1 ต้องสร้างเองทั้งหมด (2) ภาษาต่อไปเพิ่มด้วยการเพิ่ม Locale (3) เปลี่ยน provider ไม่กระทบ `.yarn` จึงเริ่มได้ด้วยการ spike ที่ย้อนกลับได้ แบบ 3 ผมไม่แนะนำ เพราะได้ข้อเสียของทั้งสองแบบ
ข้อควรระวัง: ความเสี่ยงหลักคือการโหลด async และ Addressables ซึ่งผมยังไม่ได้ทดสอบกับ pipeline ของโปรเจกต์ และกับ Text Animator

## 1.6 spike ที่เสนอใน R1 (ตัดสินด้วยผลทดลอง)
กิ่งแยกใน `RefactorCode` ติดตั้ง package แล้วทดสอบ:
1. บทพูด 10–20 บรรทัดจาก `DEMO_3Days.yarn` ผ่าน `UnityLocalisedLineProvider` EN/TH
2. สลับภาษาแล้วพิมพ์บรรทัดที่แสดงอยู่ใหม่ ด้วย Text Animator (tag `[wave]` ครบไหม)
3. ภาษาไทยในกล่องข้อความจริง (ตัดคำ, effect ต่อตัวอักษร)
4. SO หนึ่งตัวด้วย `LocalizedString` + รูป 1 ใบด้วย Asset Table
5. เวลาเริ่มเกมที่เพิ่มขึ้นและขนาด build ที่เปลี่ยน
**เกณฑ์ผ่าน:** 1–4 ใช้งานได้โดยไม่ต้องแก้ซอร์ส package และเวลาเริ่มเกมที่เพิ่มขึ้นยอมรับได้ (คุณกำหนดค่าในคำถาม)
**ถ้าไม่ผ่าน:** ถอยไปแบบ 1 ได้ เพราะ line ID ไม่เปลี่ยน

## 1.7 คำถามให้ตอบ (O1)

### Q1.1 ทางเลือกที่ต้องการ
- [ ] แบบ 2 ลองด้วย spike ก่อน แล้วค่อยตัดสินสุดท้าย (แนะนำ)
- [/] แบบ 2 ตัดสินเลย ไม่ต้อง spike
- [ ] แบบ 1
- [ ] แบบ 3

**คำตอบ:**

### Q1.2 ผู้แปลจะส่งงานคืนเป็นรูปแบบไหนจริงๆ
(Google Sheets ที่แชร์ตรง / ไฟล์ Excel / CSV) และคุณยอมให้ซิงก์กับ Google Sheets ตรงได้ไหม หรือต้องส่งไฟล์เท่านั้น

**คำตอบ:** ต้องการไฟล์เท่านั้น

### Q1.3 เวลาโหลดเกมที่เพิ่มขึ้นจาก Localization ยอมได้ไม่เกินกี่วินาที (ใช้เป็นเกณฑ์ผ่านของ spike)

**คำตอบ:** ต่ำกว่า 30 วิ หรือ น้อยกว่า 10 วิ จะดีมาก

### Q1.4 บรรทัดที่ยังไม่มี `#line:` ใน `.yarn`
ให้ผมเสนอ tool เติม line tag ให้ทุกไฟล์ก่อนเริ่ม spike ไหม (ผลคือ `.yarn` เปลี่ยนทุกไฟล์ จะ commit เป็นก้อนแยกให้)

**คำตอบ:** เสนอ Tool มา

### Q1.5 ความเห็นอื่น

**คำตอบ:**

---

# ส่วน 2 — O4: Minigame / Garnish เป็น scene แยก (additive) หรือไม่

## 2.1 สถานะ
ตอบไว้ใน `Options §15` แล้วว่าทำได้ และยังไม่ตัดสิน เสนอให้ทำ `WorldView` (กล้อง + RT + RawImage + click relay + เปิด/ปิด รวมเป็นคอมโพเนนต์เดียว) ก่อนใน R3 แล้ววัดผลว่าจำเป็นต้องแยก scene ไหม ตรงนี้คือคำถามที่ผมต้องการข้อมูลจากคุณเพื่อทำให้การวัดมีความหมาย เพราะผมไม่รู้บางเรื่องที่คุณรู้จากการเล่น/แก้ฉากจริง

## 2.2 ตัวอย่างหน้าตาถ้าเป็น additive (ร่าง ไม่ได้คอมไพล์)

```csharp
// โหลดตอนเริ่ม BeforeOpen แล้วค้างไว้ (ไม่ active) หรือโหลดตอนเข้า Station
var p = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D); // ฟิสิกส์แยก
var op = SceneManager.LoadSceneAsync("Minigame_Shaking", p);
await op;                                         // Awaitable (ตามที่เลือก)

// ฉากเสริมไม่มีอ้างอิงตรงถึงฉากหลัก ลงทะเบียนเองตอนโหลด
public class MinigameSceneEntry : MonoBehaviour
{
    [SerializeField] BaseMiniGame game;
    void Awake() => GameRoot.Instance.Register<IMinigameHost>(new MinigameHost(game));
    void OnDestroy() => GameRoot.Instance.Unregister<IMinigameHost>();
}

// Station เรียกผ่าน interface ไม่รู้ว่าอยู่คนละฉาก
var host = root.Get<IMinigameHost>();
var result = await host.RunAsync(order, ct);
```
ข้อสังเกต: ฉากเสริมต้องไม่มี EventSystem / AudioListener / Cinemachine Brain เพิ่ม และแสง ambient มาจากฉาก **active** ไม่ใช่ฉากเสริม (ต้องออกแบบ ไม่ใช่แค่ย้าย)

## 2.3 ข้อดีข้อเสีย (สรุปจาก Options §15 พร้อมน้ำหนักที่ปรับ)

| | น้ำหนักสำหรับโปรเจกต์นี้ |
|---|---|
| **ดี:** raycast ไม่ชนวัตถุในบาร์ (ฟิสิกส์แยก) | สำคัญถ้า Minigame/Garnish เคยชนวัตถุอื่น (ถามข้อ Q2.2) |
| **ดี:** ไม่ต้องใช้เลเยอร์/Rendering Layer Mask กันแสงรั่ว | สำคัญถ้ามีแสงรั่วจริง (ถามข้อ Q2.3) |
| **ดี:** ทดสอบฉากเดี่ยวได้ | ช่วยคนเดียวที่ต้องเพิ่ม/แก้บ่อย |
| **ดี:** ไฟล์ scene หลักเล็กลง | merge/diff ง่ายขึ้น |
| **เสีย:** อ้างอิงข้ามฉากไม่ได้ ต้องมีการลงทะเบียน | เพิ่มโค้ด glue ต่อ 1 ระบบ |
| **เสีย:** EventSystem/Listener/Brain ซ้ำพังเงียบๆ | ฉากหลักมี EventSystem 2 ตัวเป็นบั๊กอยู่แล้ว |
| **เสีย:** แสงและ lightmap เป็นรายฉาก | ผล bake อยู่ในฉากหลัก ต้องตรวจแสงของวัตถุที่ย้าย |
| **เสีย:** เวลาโหลด / ความจำ ถ้าค้างไว้ | ต้องวัด |
| **เสีย:** save/replay ต้องไม่โหลดฉากเหล่านี้ | ต้องออกแบบกับ R5 |
| **เสีย:** ไม่ลดโค้ด RawImage ซ้ำ | แก้ด้วย `WorldView` ไม่ใช่ scene |

## 2.4 แผนวัดผลใน R3 (เกณฑ์เป็นตัวเลข)

ทำ `WorldView` ก่อน แล้วถือว่า "ไม่ต้องแยก scene" ถ้าผ่านทุกข้อต่อไปนี้ ถ้าไม่ผ่านข้อใดให้ลอง additive **เฉพาะ Minigame** ก่อน:
1. ยิง raycast ใน Garnish แล้วไม่ชนวัตถุอื่นที่ไม่ใช่ของ Garnish (ทดสอบโดยวางวัตถุทดสอบหลังแก้ว)
2. แสงของวัตถุใน Garnish/Minigame ไม่ได้รับ/ไม่ส่งผลต่อวัตถุในบาร์ (เทียบภาพก่อน/หลัง)
3. คลิกผ่าน RT ด้วยเมาส์จริงและด้วย virtual cursor ของจอยใช้ได้
4. เปิด/ปิด `WorldView` ไม่ทำให้เฟรมตกเกินค่าที่คุณกำหนด (Q2.4)
5. เพิ่มมินิเกมตัวใหม่ได้ภายในงาน "ครึ่งวัน" โดยไม่ต้องแก้ฉากหลักมากกว่า 1 จุด (ตรวจจริงตอนทำ)

## 2.5 คำถามให้ตอบ (O4)

### Q2.1 คุณอยากได้อะไรจากการแยก scene เป็นหลัก
- [ ] แยกฟิสิกส์/raycast
- [ ] แก้แสงรั่ว
- [/] ให้ไฟล์ scene หลักเล็กลง / แก้งานง่ายขึ้น
- [ ] เปิดทดสอบมินิเกมเดี่ยวๆ
- [ ] ยังไม่แน่ใจ ตัดสินจากผลวัด

**คำตอบ:** ลดความซับซ้อนลงและแก้ง่ายโดยไม่กระทบกับ ตัว Scenes หลังให้ Scene หลักฟัง API ที่ส่งมาบอกว่าเสร็จแล้ว และรับข้อมูลบ้างส่วนกลับเข้า Scene หลัก เช่น Minigame ส่งแค่ว่าชงเสร็จแล้วหรือว่ายกเลิกก็พอ Garnish ส่งตัวการตกแต่งและการเสริฟกลับมาก็พอ เพราะทั้ง 2 ตัวนี้ส่วนใหญ่แล้วเป็น Ui ทำให้ผมมองกว่าถ้าเป็น Additive สามารถบอกผมได้หากผมเข้าใจผิด

### Q2.2 ตอนนี้ raycast ของ Garnish/Minigame เคยชนวัตถุอื่นผิดตัว หรือต้องพึ่งเลเยอร์เพื่อกันไหม

**คำตอบ:** เคยชนและเคยบังเพราะไม่ได้เอา Block raycast ออก

### Q2.3 เคยเห็นแสงจากวัตถุในมินิเกม/Garnish รั่วไปโดนของในบาร์ (หรือกลับกัน) ไหม ตอนนี้แก้ด้วยอะไร (เช่น Rendering Layer Mask 3 ของ `IngredientShadowLight`)

**คำตอบ:** เคย

### Q2.4 เกณฑ์เฟรมเรต/สะดุดที่ยอมรับได้ตอนเข้า Garnish/Minigame (เช่น ห้ามตกต่ำกว่า 60fps / ยอมสะดุดไม่เกินกี่ ms)

**คำตอบ:** ห้ามตกตำกว่า 60 fps ยอมสะดุดได้ไม่เกิน 300 ms 

### Q2.5 แก้วที่ Garnish ใช้ตกแต่ง เป็น object เดียวกับแก้วที่ชงในบาร์ หรือคนละชุด
(ถ้าเป็นชุดเดียวกัน Garnish แยก scene ยากกว่า)

**คำตอบ:** เป็นคนละชุดแต่ส่งกลับมาให้ตัวบาร์หลังจากเสร็จ

### Q2.6 วัตถุที่จะย้ายใช้ lightmap/light probe ของฉากหลัก (URP) อยู่ไหม (วัตถุในมินิเกมพึ่ง baked lightmap ของฉากหลักหรือเป็น realtime ล้วน)

**คำตอบ:** ใช่ Light Map จากของเดิม ให้ตัวของ Raw Image ที่ทั้ง Minigame และ Garnish ใช้มี Source ของ Rew image มาจากฉากหลัก แต่ว่าแยกตัว Ui ออกมาเป็นคนละ Scene 

### Q2.7 ถ้าผลวัดบอกว่าไม่ต้องแยก scene คุณพอใจกับ `WorldView` อย่างเดียวไหม

**คำตอบ:** ต้องดูอีกครั้งก่อน

### Q2.8 ความเห็นอื่น

**คำตอบ:**

---

# ส่วน 3 — สรุปสำหรับผม

หลังตอบ ผมจะอัปเดต `Bar410_Rewrite_Options.md` §9 (ปิด O1, O4 หรือกำหนดเกณฑ์ spike ให้ชัด), §11.2, §15 และ §16 ให้ตรงกัน
