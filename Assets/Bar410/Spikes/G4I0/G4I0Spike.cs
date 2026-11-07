// ============================================================
//  G4I0Spike.cs — THROWAWAY spike for task G4-I0
//  (Docs/rewrite/API_G4_SaveInput.md §2, Docs/rewrite/Spike_G4-I0_Howto.md).
//  Builds the whole test bench in Awake: EventSystem + InputSystemUIInputModule,
//  a VirtualMouseInput software cursor driven by the left stick, 3 uGUI buttons,
//  a pause toggle (timeScale 0/1), a RenderTexture view of 3 cubes clicked through
//  the shipped RenderTextureClickRelay, a 20-row ScrollRect and a HUD with an
//  automatic checklist. Delete the whole Spikes/G4I0 folder after the decision.
// ============================================================

using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Bar410.Spikes
{
    public sealed class G4I0Spike : MonoBehaviour
    {
        [Tooltip("Extra stick dead zone on the cursor action (stacks on the control's own 0.125 default).")]
        [SerializeField, Range(0f, 0.6f)] float deadZone = 0.15f;
        [Tooltip("Cursor speed in pixels per second at 1080p (scaled by screen height).")]
        [SerializeField] float cursorSpeed = 1200f;

        const float TapTime = 0.25f, TapDeflection = 0.7f, ReleaseDeflection = 0.3f;
        const float PadActivityThreshold = 0.5f; // a drifting stick under this never steals "last used" from the mouse

        enum Kind { None, Mouse, Keyboard, Xbox, PlayStation, Other }
        static bool IsPad(Kind k) => k == Kind.Xbox || k == Kind.PlayStation || k == Kind.Other;

        static readonly string[] CheckNames =
        {
            "Pad detected (last device switched to pad)",
            "Cursor moves by left stick",
            "Cursor moves while timeScale = 0",
            "UI button clicked by virtual cursor",
            "UI button clicked by virtual cursor while timeScale = 0",
            "RT cube clicked by virtual cursor (through RawImage)",
            "List scrolled by right stick",
            "Device switched back to mouse after pad",
        };
        readonly bool[] _checks = new bool[CheckNames.Length];

        Font _font;
        InputActionMap _map;
        InputAction _stick, _press, _scroll, _look, _pause, _copy;
        VirtualMouseInput _vmi;
        RectTransform _cursor;
        Image _cursorImage;
        Text _hud, _pauseLabel, _deadZoneLabel, _speedLabel;
        ScrollRect _list;
        RenderTexture _rt;
        readonly Text[] _buttonLabels = new Text[3];
        readonly int[] _buttonCounts = new int[3];
        readonly Renderer[] _cubes = new Renderer[3];
        static readonly Color[] CubeColors = { new(0.9f, 0.2f, 0.2f), new(0.2f, 0.8f, 0.3f), new(0.25f, 0.4f, 0.95f) };
        static readonly string[] CubeNames = { "Red", "Green", "Blue" };
        readonly List<string> _log = new();

        Kind _device;
        string _deviceName = "-";
        InputDevice _pendingDevice;
        Kind _pendingKind;
        Mouse _realMouse;
        int _ignoredVirtualEvents;

        Vector2 _lastCursor;
        float _lastScroll = 1f;
        float _maxSwallowedDrift;
        float _rsStart = -1f;
        bool _rsHold;
        string _rsDir = "", _rsText = "-";

        // ------------------------------------------------------------ build

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildActions();
            BuildEventSystem();
            BuildWorld();
            BuildUi();
            BuildCursor();
            _map.Enable();
            SetDeadZone(deadZone);
            InputSystem.onEvent += OnInputEvent;
            Log("Spike ready. Click the Game view, then touch the pad.");
        }

        void OnDestroy()
        {
            InputSystem.onEvent -= OnInputEvent;
            _map?.Dispose();
            if (_rt != null) _rt.Release();
            Cursor.visible = true;
            Time.timeScale = 1f;
        }

        void BuildActions()
        {
            _map = new InputActionMap("G4I0");
            _stick = _map.AddAction("CursorMove", InputActionType.Value, expectedControlLayout: "Vector2");
            _stick.AddBinding("<Gamepad>/leftStick").WithProcessor($"stickDeadzone(min={deadZone},max=0.925)");
            _press = _map.AddAction("CursorPress", InputActionType.Button, "<Gamepad>/buttonSouth");
            _scroll = _map.AddAction("ScrollView", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");
            _look = _map.AddAction("CameraLook", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");
            _pause = _map.AddAction("Pause", InputActionType.Button, "<Gamepad>/start");
            _pause.AddBinding("<Keyboard>/p");
            _pause.performed += _ => TogglePause();
            _copy = _map.AddAction("CopyHud", InputActionType.Button, "<Keyboard>/c");
            _copy.AddBinding("<Gamepad>/select");
            _copy.performed += _ => CopyHud();
        }

        void BuildEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var es = go.GetComponent<EventSystem>();
            es.sendNavigationEvents = false; // Cursor mode: South must not also Submit the selected button
            var module = go.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null || module.point == null) module.AssignDefaultActions();
            module.deselectOnBackgroundClick = false;
        }

        void BuildWorld()
        {
            var main = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            main.tag = "MainCamera";
            main.clearFlags = CameraClearFlags.SolidColor;
            main.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            main.cullingMask = 0; // the cubes are only for the RT camera

            var light = new GameObject("Directional Light", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            _rt = new RenderTexture(640, 360, 24) { name = "RT_G4I0" };
            var rtCam = new GameObject("RT Camera", typeof(Camera)).GetComponent<Camera>();
            rtCam.transform.SetPositionAndRotation(new Vector3(0f, 100f, -6f), Quaternion.identity);
            rtCam.clearFlags = CameraClearFlags.SolidColor;
            rtCam.backgroundColor = new Color(0.25f, 0.22f, 0.2f);
            rtCam.targetTexture = _rt;
            _rtCamera = rtCam;

            for (int i = 0; i < 3; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Cube " + CubeNames[i];
                cube.transform.position = new Vector3((i - 1) * 2.2f, 100f, 0f);
                cube.transform.rotation = Quaternion.Euler(20f, 35f, 0f);
                _cubes[i] = cube.GetComponent<Renderer>();
                _cubes[i].material.color = CubeColors[i];
                cube.AddComponent<SpikeClickTarget>().Clicked = OnCubeClicked;
            }
        }
        Camera _rtCamera;

        void BuildUi()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;
            var res = new DefaultControls.Resources();

            Label(root, "G4-I0 spike (throwaway)", 40, 20, 380, 40, 26);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var b = MakeButton(root, res, "", 40, 70 + i * 85, 380, 70, () => { _buttonCounts[index]++; RefreshButtons(); });
                b.gameObject.AddComponent<SpikeClickTarget>().Clicked = (_, dev) => OnUiClicked(index, dev);
                _buttonLabels[i] = b.GetComponentInChildren<Text>();
            }
            RefreshButtons();
            _pauseLabel = MakeButton(root, res, "", 40, 330, 380, 70, TogglePause).GetComponentInChildren<Text>();
            MakeButton(root, res, "Copy HUD (C / Select)", 40, 415, 380, 60, CopyHud);

            _deadZoneLabel = Label(root, "", 40, 495, 380, 30, 20);
            MakeSlider(root, res, 40, 530, 380, 0f, 0.6f, deadZone, SetDeadZone);
            _speedLabel = Label(root, "", 40, 575, 380, 30, 20);
            MakeSlider(root, res, 40, 610, 380, 300f, 3000f, cursorSpeed, v => cursorSpeed = v);
            Label(root,
                "Left stick = cursor\nA / Cross = click\nRight stick over the list = scroll\n" +
                "Right stick flick / hold = view test\nStart (or P) = pause\nSelect (or C) = copy HUD\nMove the real mouse = back to mouse",
                40, 660, 380, 260, 20);

            Label(root, "RenderTexture: click a cube through the picture", 460, 20, 640, 30, 20);
            var raw = DefaultControls.CreateRawImage(res);
            Place(raw, root, 460, 55, 640, 360);
            raw.GetComponent<RawImage>().texture = _rt;
            var relay = raw.AddComponent<RenderTextureClickRelay>(); // the shipped relay, not a copy
            typeof(RenderTextureClickRelay).GetField("_camera", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(relay, _rtCamera);

            Label(root, "Scroll list: put the cursor over it, push the right stick", 460, 430, 640, 30, 20);
            var scrollGo = DefaultControls.CreateScrollView(res);
            Place(scrollGo, root, 460, 465, 640, 580);
            _list = scrollGo.GetComponent<ScrollRect>();
            _list.horizontal = false;
            _list.scrollSensitivity = 30f;
            var content = _list.content;
            content.gameObject.AddComponent<VerticalLayoutGroup>().childControlHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int i = 1; i <= 20; i++)
            {
                var row = Label(content, $"Row {i:00}", 0, 0, 600, 48, 24);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            }

            var panel = DefaultControls.CreatePanel(res);
            Place(panel, root, 1140, 20, 760, 1040);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            panel.GetComponent<Image>().raycastTarget = false;
            _hud = Label(panel.transform, "", 12, 10, 736, 1020, 19);
        }

        void BuildCursor()
        {
            var canvasGo = new GameObject("CursorCanvas", typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // on top of everything; no CanvasScaler so anchoredPosition == screen pixels

            var go = new GameObject("VirtualCursor", typeof(RectTransform));
            go.SetActive(false); // VirtualMouseInput finds its canvas in OnEnable, so configure first
            go.transform.SetParent(canvasGo.transform, false);
            _cursor = (RectTransform)go.transform;
            _cursor.anchorMin = _cursor.anchorMax = Vector2.zero;
            _cursor.pivot = new Vector2(0.5f, 0.5f);
            _cursor.sizeDelta = new Vector2(28, 28);
            _cursor.anchoredPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            _cursorImage = go.AddComponent<Image>();
            _cursorImage.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            _cursorImage.raycastTarget = false;
            _cursorImage.enabled = false;
            var dot = new GameObject("HotSpot", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(go.transform, false);
            ((RectTransform)dot.transform).sizeDelta = new Vector2(6, 6);
            var dotImage = dot.GetComponent<Image>();
            dotImage.color = Color.black;
            dotImage.raycastTarget = false;

            _vmi = go.AddComponent<VirtualMouseInput>();
            _vmi.cursorMode = VirtualMouseInput.CursorMode.SoftwareCursor;
            _vmi.cursorGraphic = _cursorImage;
            _vmi.cursorTransform = _cursor;
            _vmi.stickAction = new InputActionProperty(_stick);
            _vmi.leftButtonAction = new InputActionProperty(_press);
            _vmi.scrollWheelAction = new InputActionProperty(_scroll);
            go.SetActive(true);
        }

        // ------------------------------------------------------------ input

        // Runs inside the input update, so it only records; Update() reacts.
        void OnInputEvent(InputEventPtr ptr, InputDevice device)
        {
            if (!ptr.IsA<StateEvent>() && !ptr.IsA<DeltaStateEvent>()) return;
            if (device.name.StartsWith("VirtualMouse")) { _ignoredVirtualEvents++; return; }

            Kind kind = Kind.None;
            switch (device)
            {
                case Mouse m:
                    _realMouse = m;
                    bool moved = m.delta.ReadValueFromEvent(ptr, out Vector2 d) && d.sqrMagnitude > 4f;
                    bool pressed = (m.leftButton.ReadValueFromEvent(ptr, out float l) && l > 0.5f)
                                   || (m.rightButton.ReadValueFromEvent(ptr, out float r) && r > 0.5f)
                                   || (m.scroll.ReadValueFromEvent(ptr, out Vector2 s) && s.sqrMagnitude > 0f);
                    if (moved || pressed) kind = Kind.Mouse;
                    break;
                case Keyboard:
                    if (AnyChanged(ptr, device)) kind = Kind.Keyboard;
                    break;
                case Gamepad:
                    if (AnyChanged(ptr, device)) kind = Classify(device);
                    break;
            }
            if (kind == Kind.None) return;
            _pendingKind = kind;
            _pendingDevice = device;
        }

        static bool AnyChanged(InputEventPtr ptr, InputDevice device)
        {
            foreach (var _ in ptr.EnumerateChangedControls(device, PadActivityThreshold)) return true;
            return false;
        }

        static Kind Classify(InputDevice d)
        {
            if (d is DualShockGamepad) return Kind.PlayStation;
            string s = (d.layout + " " + d.description.product + " " + d.description.interfaceName).ToLowerInvariant();
            if (s.Contains("xinput") || s.Contains("xbox")) return Kind.Xbox;
            if (s.Contains("dualsense") || s.Contains("dualshock") || s.Contains("wireless controller")) return Kind.PlayStation;
            return Kind.Other;
        }

        void ApplyPendingDevice()
        {
            if (_pendingDevice == null) return;
            var device = _pendingDevice; var kind = _pendingKind;
            _pendingDevice = null;
            _deviceName = $"{device.displayName} (layout {device.layout}, name {device.name})";
            if (kind == _device) return;

            bool wasPad = IsPad(_device), pad = IsPad(kind);
            _device = kind;
            Log($"Device -> {kind}: {device.displayName}");
            if (pad)
            {
                Pass(0);
                if (!wasPad) WarpCursorToRealMouse();
                Cursor.visible = false;
                _cursorImage.enabled = true;
            }
            else if (kind == Kind.Mouse)
            {
                if (wasPad) Pass(7);
                Cursor.visible = true;
                _cursorImage.enabled = false;
            }
        }

        void WarpCursorToRealMouse()
        {
            if (_realMouse == null || _vmi.virtualMouse == null) return;
            Vector2 p = _realMouse.position.ReadValue();
            p.x = Mathf.Clamp(p.x, 0, Screen.width);
            p.y = Mathf.Clamp(p.y, 0, Screen.height);
            InputState.Change(_vmi.virtualMouse.position, p);
            _cursor.anchoredPosition = p;
            _lastCursor = p;
        }

        // ------------------------------------------------------------ frame

        void Update()
        {
            ApplyPendingDevice();
            _vmi.cursorSpeed = cursorSpeed * Screen.height / 1080f;

            Vector2 cursorPos = _vmi.virtualMouse != null ? _vmi.virtualMouse.position.ReadValue() : Vector2.zero;
            if ((cursorPos - _lastCursor).sqrMagnitude > 0.01f && _stick.ReadValue<Vector2>() != Vector2.zero)
            {
                Pass(1);
                if (Time.timeScale == 0f) Pass(2);
            }
            _lastCursor = cursorPos;

            var pad = Gamepad.current;
            if (pad != null && _stick.ReadValue<Vector2>() == Vector2.zero)
                _maxSwallowedDrift = Mathf.Max(_maxSwallowedDrift, pad.leftStick.ReadUnprocessedValue().magnitude);

            if (Mathf.Abs(_list.verticalNormalizedPosition - _lastScroll) > 0.001f && _scroll.ReadValue<Vector2>() != Vector2.zero)
                Pass(6);
            _lastScroll = _list.verticalNormalizedPosition;

            UpdateRightStick();
            _hud.text = BuildHud(cursorPos, pad);
        }

        void UpdateRightStick()
        {
            Vector2 rs = _look.ReadValue<Vector2>();
            float now = Time.unscaledTime;
            if (rs.magnitude > TapDeflection)
            {
                _rsDir = Mathf.Abs(rs.x) > Mathf.Abs(rs.y) ? (rs.x > 0 ? "Right" : "Left") : (rs.y > 0 ? "Up" : "Down");
                if (_rsStart < 0f) _rsStart = now;
                else if (!_rsHold && now - _rsStart > TapTime)
                {
                    _rsHold = true;
                    _rsText = $"RS HOLD -> peek {_rsDir}";
                    Log(_rsText);
                }
            }
            else if (rs.magnitude < ReleaseDeflection && _rsStart >= 0f)
            {
                float ms = (now - _rsStart) * 1000f;
                if (_rsHold || ms > TapTime * 1000f) { _rsText = $"RS HOLD released after {ms:0} ms -> back to Forward"; Log(_rsText); }
                else { _rsText = $"RS TAP -> next view ({_rsDir}, {(now - _rsStart) * 1000f:0} ms)"; Log(_rsText); }
                _rsStart = -1f;
                _rsHold = false;
            }
        }

        string BuildHud(Vector2 cursorPos, Gamepad pad)
        {
            var sb = new StringBuilder(2048);
            sb.AppendLine("G4-I0 HUD  (Unity " + Application.unityVersion + ", Input System " + InputSystem.version + ")");
            sb.AppendLine($"Last device: {_device}");
            sb.AppendLine($"  {_deviceName}");
            sb.AppendLine($"Hardware cursor visible: {Cursor.visible}   Software cursor: {_cursorImage.enabled}");
            sb.AppendLine($"Mouse.current: {Mouse.current?.name ?? "-"}   ignored VirtualMouse events: {_ignoredVirtualEvents}");
            sb.AppendLine($"Virtual cursor: ({cursorPos.x:0}, {cursorPos.y:0})   screen {Screen.width}x{Screen.height}");
            if (pad != null)
            {
                Vector2 lsRaw = pad.leftStick.ReadUnprocessedValue(), rsRaw = pad.rightStick.ReadUnprocessedValue();
                Vector2 ls = _stick.ReadValue<Vector2>(), rs = _look.ReadValue<Vector2>();
                sb.AppendLine($"Pad: {pad.displayName}");
                sb.AppendLine($"LS raw ({lsRaw.x:+0.000;-0.000}, {lsRaw.y:+0.000;-0.000}) |{lsRaw.magnitude:0.000}|  -> cursor ({ls.x:+0.00;-0.00}, {ls.y:+0.00;-0.00})");
                sb.AppendLine($"RS raw ({rsRaw.x:+0.000;-0.000}, {rsRaw.y:+0.000;-0.000}) |{rsRaw.magnitude:0.000}|  -> ({rs.x:+0.00;-0.00}, {rs.y:+0.00;-0.00})");
                sb.AppendLine($"Dead zone: {deadZone:0.00} on cursor (after control default 0.125)");
                sb.AppendLine($"Max raw LS while cursor still (drift swallowed): {_maxSwallowedDrift:0.000}");
                sb.Append("Buttons pressed: ");
                bool any = false;
                foreach (var c in pad.allControls)
                    if (c is ButtonControl b && !c.synthetic && b.isPressed) { sb.Append(c.name).Append(' '); any = true; }
                sb.AppendLine(any ? "" : "-");
            }
            else sb.AppendLine("Pad: none connected");
            sb.AppendLine($"timeScale: {Time.timeScale}");
            sb.AppendLine($"Right stick: {_rsText}");
            sb.AppendLine($"Clicks: A {_buttonCounts[0]}  B {_buttonCounts[1]}  C {_buttonCounts[2]}");
            sb.AppendLine();
            sb.AppendLine("Checklist (automatic):");
            for (int i = 0; i < CheckNames.Length; i++)
                sb.AppendLine($"[{(_checks[i] ? "PASS" : " -- ")}] {CheckNames[i]}");
            sb.AppendLine();
            sb.AppendLine("Log:");
            foreach (var line in _log) sb.AppendLine(line);
            return sb.ToString();
        }

        // ------------------------------------------------------------ events

        void OnUiClicked(int index, string device)
        {
            bool virtualCursor = device.StartsWith("VirtualMouse");
            Log($"Button {(char)('A' + index)} clicked by {device} at timeScale {Time.timeScale}");
            if (!virtualCursor) return;
            Pass(3);
            if (Time.timeScale == 0f) Pass(4);
        }

        void OnCubeClicked(SpikeClickTarget target, string device)
        {
            for (int i = 0; i < 3; i++)
            {
                bool hit = _cubes[i].gameObject == target.gameObject;
                _cubes[i].material.color = hit ? Color.white : CubeColors[i];
                if (hit) Log($"RT cube {CubeNames[i]} clicked by {device}");
            }
            if (device.StartsWith("VirtualMouse")) Pass(5);
        }

        void TogglePause()
        {
            Time.timeScale = Time.timeScale == 0f ? 1f : 0f;
            RefreshPauseLabel();
            Log($"timeScale -> {Time.timeScale}");
        }

        void SetDeadZone(float v)
        {
            deadZone = v;
            _stick.ApplyBindingOverride(0, new InputBinding { overrideProcessors = $"stickDeadzone(min={v},max=0.925)" });
            if (_deadZoneLabel != null) _deadZoneLabel.text = $"Cursor dead zone: {v:0.00}";
        }

        void CopyHud()
        {
            GUIUtility.systemCopyBuffer = _hud.text;
            Debug.Log("[G4I0] HUD\n" + _hud.text);
            Log("HUD copied to clipboard + Console");
        }

        void Pass(int i)
        {
            if (_checks[i]) return;
            _checks[i] = true;
            Log("PASS: " + CheckNames[i]);
        }

        void Log(string line)
        {
            _log.Add($"{Time.unscaledTime,7:0.00}s {line}");
            if (_log.Count > 12) _log.RemoveAt(0);
        }

        void RefreshButtons()
        {
            for (int i = 0; i < 3; i++)
                if (_buttonLabels[i] != null) _buttonLabels[i].text = $"Button {(char)('A' + i)}  ({_buttonCounts[i]})";
        }

        void RefreshPauseLabel() => _pauseLabel.text = Time.timeScale == 0f ? "PAUSED (timeScale 0) - Resume" : "Pause (timeScale 1)";

        void LateUpdate()
        {
            if (_speedLabel != null) _speedLabel.text = $"Cursor speed: {cursorSpeed:0} px/s @1080p";
            if (_pauseLabel != null) RefreshPauseLabel();
        }

        // ------------------------------------------------------------ ui helpers

        static void Place(GameObject go, Transform parent, float x, float y, float w, float h)
        {
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        Text Label(Transform parent, string text, float x, float y, float w, float h, int size)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            Place(go, parent, x, y, w, h);
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.color = Color.white;
            t.text = text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        Button MakeButton(Transform parent, DefaultControls.Resources res, string text, float x, float y, float w, float h, UnityEngine.Events.UnityAction onClick)
        {
            var go = DefaultControls.CreateButton(res);
            Place(go, parent, x, y, w, h);
            var label = go.GetComponentInChildren<Text>();
            label.font = _font;
            label.fontSize = 24;
            label.color = Color.black;
            label.text = text;
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            return button;
        }

        static void MakeSlider(Transform parent, DefaultControls.Resources res, float x, float y, float w, float min, float max, float value, UnityEngine.Events.UnityAction<float> onChange)
        {
            var go = DefaultControls.CreateSlider(res);
            Place(go, parent, x, y, w, 30);
            var slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(onChange);
        }
    }
}
