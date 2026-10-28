# Bar410 — Button Feedback (Hover / Hold / Release / Click) Plan

ผู้ทำ: **Sonnet** · ผู้อนุมัติ: เจ้าของ · วันที่: 2026-10-28

## เป้าหมาย
ปุ่มใน `Canvas_HUD` มี animation scale ตอน Hover, Hold, Release และ Click ปรับค่าได้ภายหลังจากที่เดียว
และใช้ `Button.interactable` เป็นตัวเปิด/ปิด feedback

## ข้อตัดสินใจที่เจ้าของตอบแล้ว
1. ค่าที่ Tune เก็บใน **ScriptableObject preset ตัวเดียวที่ใช้ร่วมกัน** (ปุ่มไหนต่างก็สร้าง preset ใหม่)
2. Scale **ตัวปุ่มเอง** (`transform` ของ GameObject ที่มี Button) และมีช่อง `Target` ไว้ชี้ไปที่ child ได้ถ้าต้องการ
3. รอบนี้ทำ **Prefab + ติด component ให้ปุ่มทุกตัวใต้ `Canvas_HUD`**

## ไฟล์ที่จะสร้าง
| ไฟล์ | ทำอะไร |
|---|---|
| `Assets/[02]Script/UI Script/ButtonFeedback/SO_ButtonFeedback.cs` | ScriptableObject เก็บค่า 4 ขั้น (Hover / Hold / Release / Click) และ `useUnscaledTime` |
| `Assets/[02]Script/UI Script/ButtonFeedback/ButtonScaleFeedback.cs` | Component (MonoBehaviour) ติดบนปุ่ม รับ pointer/controller event แล้วเล่น DOTween |
| `Assets/[02]Script/UI Script/ButtonFeedback/ButtonFeedback_Default.asset` | preset ตั้งต้น |
| `Assets/[04]Prefab/UI/PF_Button_Hub.prefab` | ปุ่มพร้อมใช้: `Image` + `Button` (Sprite Swap) + `ButtonScaleFeedback` (ชี้ preset Default) ไม่มี text เพราะปุ่ม Hub เป็นไอคอน |

ไม่ใช้ `PointerInteractableBase` เพราะ base นั้นมี `_interactable` ของตัวเอง แต่งานนี้ต้องอ่านจาก `Button`

## ค่าใน `SO_ButtonFeedback`
ทุกขั้นใช้ struct เดียวกัน (serializable, โชว์ใน Inspector ได้ ใช้ Odin `[InlineProperty]`/`[BoxGroup]` ได้ตามสะดวก):

```csharp
[Serializable] public struct ScaleStep
{
    public float scale;      // multiplier of the button's base scale
    public float duration;   // seconds
    public Ease ease;
    public float overshoot;  // used only by Back eases
}
```

| ขั้น | เกิดตอน | ค่าเริ่มต้น (แก้ได้) |
|---|---|---|
| `hover` | ชี้เมาส์เข้า / controller เลือก (Select) | scale 1.08, 0.18s, `OutBack`, overshoot 1.2 |
| `hold` | กดค้าง (PointerDown) | scale 0.94, 0.08s, `OutQuad` |
| `release` | ปล่อย (PointerUp) → กลับไปขนาดพัก* | 0.15s, `OutBack`, overshoot 1.5 (`scale` ไม่ใช้) |
| `click` | `Button.onClick` / controller Submit | scale 1.12, 0.08s, `OutQuad` แล้วต่อด้วย `release` กลับขนาดพัก |
| `exit` | เมาส์ออก / Deselect | ใช้ `release` กลับไป base (ไม่มีช่องแยก) |

\* **ขนาดพัก** = `hover.scale` ถ้าเมาส์ยังอยู่บนปุ่ม/ยังถูกเลือก ไม่งั้น = 1 (base)

`useUnscaledTime` (ค่าเริ่ม `true`): `PauseManager` ตั้ง `timeScale = 0` ปุ่มใน Pause จะได้ยังขยับ

## พฤติกรรมของ `ButtonScaleFeedback`
- `[RequireComponent(typeof(Button))]` ช่อง: `preset` (SO), `target` (Transform, ว่าง = ตัวเอง)
- implement `IPointerEnterHandler`, `IPointerExitHandler`, `IPointerDownHandler`, `IPointerUpHandler`, `ISelectHandler`, `IDeselectHandler` และฟัง `button.onClick` (ครอบคลุมทั้งคลิกเมาส์และ Submit ของ controller)
- เก็บ `baseScale = target.localScale` ใน `Awake` (ไม่ assume ว่าเป็น 1)
- ทุก event: ถ้า `!button.IsInteractable()` ไม่เล่น (`IsInteractable()` รวม CanvasGroup ของ `HubManager` แล้ว)
- เล่นใหม่ทุกครั้ง `target.DOKill()` ก่อน แล้ว `DOScale(baseScale * step.scale, step.duration).SetEase(step.ease, step.overshoot).SetUpdate(useUnscaledTime)` ไม่สะสม error เพราะคำนวณจาก `baseScale` เสมอ
- Click = `DOTween.Sequence()` : ไป `click.scale` → `release` กลับขนาดพัก
- ลำดับ event ของ Unity: PointerUp มาก่อน onClick ในเฟรมเดียวกัน → Release เริ่มแล้วถูก Click แทนทันที **Release จะเห็นเฉพาะตอนกดค้างแล้วลากออกไปปล่อยนอกปุ่ม** (ยกเลิก) ส่วนคลิกจริงเห็นเป็น Click
- **ปุ่มถูกปิดระหว่าง Hover** (เช่น `HubManager` fade ออก หรือโค้ดตั้ง `interactable = false`): ใน `Update` เช็ค `IsInteractable()` เทียบค่าเฟรมก่อน ถ้าเปลี่ยนเป็น false → `DOKill` + กลับ `baseScale` แบบ tween `release` และล้างสถานะ hover/hold
- `OnDisable`: `DOKill` + ตั้ง `target.localScale = baseScale` ทันที + ล้างสถานะ (ปุ่มที่ปิดตอนขยายอยู่จะไม่ค้างขยาย)
- `preset == null` → `Debug.LogWarning` ครั้งเดียวใน `Awake` แล้วไม่ทำอะไร

## Stages (compile-check ทุก stage: `read_console` ต้องไม่มี error)

### Stage 1 — Script + Preset
1. สร้าง `SO_ButtonFeedback.cs` (`[CreateAssetMenu(menuName = "Bar410/UI/Button Feedback")]`) และ `ButtonScaleFeedback.cs` ตามด้านบน
2. compile-check
3. สร้าง asset `ButtonFeedback_Default.asset` ด้วยค่าเริ่มต้นในตาราง

### Stage 2 — Prefab
1. สร้าง `PF_Button_Hub.prefab` ใน `Assets/[04]Prefab/UI/`: RectTransform 100×100, `Image` (raycast on), `Button` (Transition = Sprite Swap ช่อง sprite ว่างไว้), `ButtonScaleFeedback` → preset Default
2. ทดสอบในฉากทดสอบหรือ Play mode: hover / hold / ลากออกแล้วปล่อย / click / ปิด interactable ระหว่าง hover

### Stage 3 — ติดให้ปุ่มใน `Canvas_HUD`
1. ใน `New Drag Drop System` หา GameObject ที่มี `Button` ทุกตัวใต้ `[GameLoop]` › `Canvas_HUD` แล้ว **รายงานรายชื่อให้เจ้าของดูก่อนติด** (คาดว่ามี BTN_Skip, BTN_Toggle_AutoPlay, BTN_OpenEventLog, BTN_Setting, BTN_Clock, BTN_Open_CloseBook, Button_ConfirmSkip/CancelSkip ฯลฯ)
2. ก่อนติด เช็คว่าปุ่มไหนมีสิ่งที่แตะ `localScale` อยู่แล้ว (Animator, `Button` transition = Animation, DOTween ในโค้ดเช่น `DialogueControls`, `PanelTransition`) ถ้ามีให้ข้ามและแจ้ง
3. ติด `ButtonScaleFeedback` + preset Default ให้ปุ่มที่ผ่าน แล้ว save scene
4. Play mode ผ่าน Unity MCP: เปิดวัน, hover/click ปุ่ม Hub, เปลี่ยน Phase ให้ `HubManager` fade ปุ่มออกระหว่าง hover แล้วเช็คว่า scale กลับ 1, เปิด Pause แล้วเช็คว่าปุ่มไม่ค้าง

### Stage 4 — เอกสาร
- เพิ่ม `ButtonScaleFeedback` ลงตาราง "Component ที่เพิ่มเข้ามา" ใน `CLAUDE.md` (อยู่ที่ prefab `PF_Button_Hub` และปุ่มใต้ `Canvas_HUD` ตามรายชื่อ Stage 3)
- รายงานไฟล์ที่สร้าง/แก้ + GameObject ที่ติด component

## นอกขอบเขต (ไม่ทำรอบนี้)
- เสียง hover/click (มี `UIPointerSound` อยู่แล้ว ติดเพิ่มทีหลังได้)
- ปุ่มใน `PauseScene` / `MainMenu_V2` (ใช้ prefab/component เดียวกันได้ภายหลัง)
- ทดสอบด้วย controller จริง (Select/Submit มีในโค้ด แต่ทดสอบได้แค่ผ่าน EventSystem ใน MCP)

## สิ่งที่ Sonnet ต้องหยุดถามเจ้าของ
- ปุ่มใน Canvas_HUD ที่มีระบบ scale อยู่แล้ว (Stage 3 ข้อ 2)
- ถ้าลำดับ Release/Click ข้างบนไม่ตรงที่เจ้าของต้องการตอนทดสอบ

## ส่วนเพิ่ม: Event + เสียง (2026-10-28 ทำแล้ว)
เจ้าของเลือก: C# event + component แยก และเก็บ Sound ID ใน SO preset
- `ButtonScaleFeedback.Fired` (`Action<ButtonFeedbackStep>`: Hover / Hold / Release / Click) ยิงหลังเล่น animation และเฉพาะตอนปุ่ม interactable เท่านั้น ถ้า controller Select ปุ่ม จะยิงเป็น Hover
- `SO_ButtonFeedback` มีช่อง `hoverSound` / `holdSound` / `releaseSound` / `clickSound` (dropdown UiSFX) ถ้าปล่อยว่างจะไม่มีเสียง
- `ButtonFeedbackSound` ฟัง `Fired` แล้วเรียก `SoundManager.Instance.PlayUiSFX(id)` ติดไว้ที่ prefab และ 13 ปุ่มแล้ว
- คลิกปกติจะยิง Release แล้วตามด้วย Click ในเฟรมเดียวกัน ถ้าใส่ทั้ง `releaseSound` และ `clickSound` จะได้ยินทั้งสองเสียง
- ถ้าจะเพิ่มระบบอื่นในอนาคต (VFX, haptic) ให้ทำ component ใหม่ที่ฟัง `Fired` แบบเดียวกัน ไม่ต้องแก้ `ButtonScaleFeedback`

## ส่วนเพิ่ม: ปุ่มใน Canvas_HUD เป็น instance ของ `PF_Button_Hub` (2026-10-28 ทำแล้ว)
- แปลงปุ่มเดิมทั้ง 13 ตัวด้วย `PrefabUtility.ConvertToPrefabInstance` (match แบบ ByHierarchy, เก็บค่าที่ต่างจาก prefab เป็น override, ชื่อปุ่มคงเดิม)
- ปุ่มเดิมไม่ได้ถูกลบหรือสร้างใหม่: instance ID เดิม และ reference จากระบบอื่นยังครบ 15 จุด (`HubManager.rows`, `DialogueControls`, `FlowWiring._btnBook`, `BookUI_V2`, `EventLogPresenter._btnOpen`, `IngredientButtonGroup`, `CocktailSystemManager._endShiftBTN`)
- ค่าของแต่ละปุ่มที่เป็น override: RectTransform, sprite/สีของ Image, transition/SpriteState/colors ของ Button, onClick และ component ที่เพิ่มเอง (`CanvasGroup`, `LoadSceneButton`, `SceneLoader`) รวมถึง child (text)
- **แก้ที่ prefab แล้วเปลี่ยนตามทุกปุ่ม:** preset ของ `ButtonScaleFeedback`, `ButtonFeedbackSound`, component ใหม่ที่เพิ่มบน prefab และค่าใดๆ ที่ปุ่มยังไม่ได้ override
- ถ้าอยากให้ค่าที่ override อยู่ (เช่น transition) กลับมาเป็นของ prefab ให้คลิกขวาที่ช่องนั้นใน Inspector แล้วเลือก Revert
- backup ของฉากก่อนแปลงอยู่ใน scratchpad ของ session นี้เท่านั้น ถ้าต้องย้อนกลับให้ใช้ git

## ส่วนเพิ่ม: ปุ่มใน Manager อื่น (2026-10-28 ทำแล้ว)
- แปลงเป็น instance ของ `PF_Button_Hub` วิธีเดียวกับ Canvas_HUD ทั้งหมด 33 ปุ่ม
  - `Canvas_Cocktail/Panel - Method`: `BTN_Mixing`, `BTN_Shaking`, `BTN_Reset`
  - `Canvas_Garnish`: ทุกปุ่ม 28 ตัว (รวมปุ่มลูกศร `Button (1)/(2)` ที่ไม่มีชื่อ)
  - `BTN - Cancel Minigame` ของ Mixing และ Shaking: Animator ของ canvas ไม่ได้แตะ scale ของปุ่ม
- เช็คแล้วว่าค่าเดิมทุกปุ่มตรงกับก่อนแปลง ต่างกันแค่มี component feedback/sound เพิ่มเข้ามา และ reference จากระบบอื่นยังครบ 31 จุด
- `BTN_CloseEventLog` อยู่ใน prefab `Assets/[06]Dialogue/Text Animator Dialogue System.prefab` จึงใส่ `ButtonScaleFeedback` + `ButtonFeedbackSound` ใน prefab นั้นตรงๆ (ไม่ได้เป็น instance ของ `PF_Button_Hub` แต่ใช้ preset Default ตัวเดียวกัน)
- ข้าม:
  - `BTN - OpenOrder` และ `Panel - Note`: `Post_It_Order` scale สองตัวนี้ตอนเปิด/ปิดอยู่แล้ว
  - `Continue Button`: ปุ่มเลื่อนบทพูด
  - ปุ่ม Save/Load และ `Panel_EventLog/Blocker`

## ส่วนเพิ่ม: ปุ่มที่กดไม่ได้จางลง (2026-10-28 ทำแล้ว)
- `SO_ButtonFeedback.disabledAlpha` (ค่าเริ่ม 2/3) และ `disabledFadeDuration` (0.15s)
- ทุกครั้งที่ `Button.IsInteractable()` เปลี่ยน (รวม CanvasGroup ของ `HubManager`) `ButtonScaleFeedback` จะ `DOFade` ทุก `Graphic` ใต้ปุ่ม (Image, text) ไปที่ alpha เดิม × `disabledAlpha` หรือกลับเป็น alpha เดิม ตอน `OnEnable` ตั้งค่าทันทีไม่ fade
- tween แค่ alpha สีที่สคริปต์อื่นตั้งไว้จึงยังอยู่ (เช่นสี on/off ของ AutoPlay)
- คูณกับ alpha ของ CanvasGroup: ปุ่ม Hub ที่ `HubManager` ซ่อนอยู่ยังมองไม่เห็นเหมือนเดิม
- `disabledColor` ของ Button ใน `PF_Button_Hub` และ `BTN_CloseEventLog` เปลี่ยนเป็นสีขาว alpha 1 ปุ่ม ColorTint จึงไม่จางซ้อนสองชั้น
- `Canvas_ShakingMinigame` › `BTN - Cancel Minigame` เคย override `disabledColor` เป็นสีแดง ตอนนี้ revert กลับไปใช้สีขาวของ prefab แล้ว
- ข้อจำกัด: ถ้า `DialogueControls` ตั้งสีปุ่ม AutoPlay ใหม่ระหว่างที่ปุ่มกดไม่ได้ alpha ของปุ่มจะกลับเป็นตามสีที่ตั้งจนกว่าสถานะ interactable จะเปลี่ยนอีกครั้ง
