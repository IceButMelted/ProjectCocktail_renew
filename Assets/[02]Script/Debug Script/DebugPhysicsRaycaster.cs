using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drop this on any GameObject (e.g. the EventSystem or your camera rig)
/// to visualize what a specific PhysicsRaycaster is actually hitting.
///
/// Three independent debug modes, each toggleable:
///
///  1. RAW PHYSICS RAY — a plain Physics.Raycast from the given camera
///     through the mouse position. Mirrors N_InputManager.MouseRay(); good
///     for sanity-checking layer masks / range with zero EventSystem
///     involvement.
///
///  2. PHYSICSRASTER HOOK (the important one) — calls
///     <see cref="PhysicsRaycaster"/>.Raycast(...) DIRECTLY on the assigned
///     component, the same method the EventSystem calls internally, but in
///     isolation from every other raycaster in the scene. This tells you
///     exactly what THIS raycaster sees: its own eventMask, eventCamera,
///     maxRayIntersections, and every RaycastResult it produces, in the
///     order it produces them — with nothing from any GraphicRaycaster or
///     other PhysicsRaycaster mixed in.
///
///  3. FULL EVENTSYSTEM PIPELINE — EventSystem.current.RaycastAll, which
///     merges every registered raycaster (all PhysicsRaycasters +
///     GraphicRaycasters), sorted the way pointer events are actually
///     dispatched. Useful for comparing against mode 2 to see whether
///     something else (a UI panel, another PhysicsRaycaster) is stealing
///     the hit before your interactable ever sees it.
///
/// Draws colour-coded points at each hit (Scene view gizmos + Game view
/// Debug.DrawLine crosses) and an OnGUI list with distance / layer / module
/// / whether the hit GameObject implements IPointerInteractable.
///
/// Toggle at runtime with <see cref="_debugKey"/> (default F9).
/// </summary>

public class DebugPhysicsRaycaster : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private bool _enabled = true;
    [SerializeField] private KeyCode _debugKey = KeyCode.F9;

    [Header("Raw Physics Ray")]
    [SerializeField] private bool _useRawPhysicsRay = true;
    [Tooltip("Camera to cast from. Defaults to Camera.main if left empty.")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _range = 100f;
    [Tooltip("Leave as Everything to see all colliders, or match a specific mask you're debugging.")]
    [SerializeField] private LayerMask _physicsLayerMask = ~0;

    [Header("PhysicsRaycaster Hook")]
    [Tooltip("Hook directly into this PhysicsRaycaster's own Raycast() call. Leave empty to auto-find one on the assigned camera, then anywhere in the scene.")]
    [SerializeField] private PhysicsRaycaster _physicsRaycaster;
    [SerializeField] private bool _useRaycasterHook = true;

    [Header("Full EventSystem Pipeline")]
    [Tooltip("Also run EventSystem.current.RaycastAll to compare against every registered raycaster (Physics + Graphic).")]
    [SerializeField] private bool _useEventSystemRay = false;

    [Header("Display")]
    [SerializeField] private bool _drawInGameView = true;
    [SerializeField] private bool _showOnScreenList = true;
    [SerializeField] private int _fontSize = 14;
    [SerializeField] private Vector2 _screenOffset = new Vector2(12f, 12f);

    private readonly List<RaycastResult> _raycasterResults = new();
    private readonly List<RaycastResult> _eventSystemResults = new();
    private PointerEventData _pointerData;

    private RaycastHit _rawHit;
    private bool _rawHitSomething;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_physicsRaycaster == null && _camera != null)
            _physicsRaycaster = _camera.GetComponent<PhysicsRaycaster>();

        if (_physicsRaycaster == null)
            _physicsRaycaster = FindFirstObjectByType<PhysicsRaycaster>();

        if (_physicsRaycaster == null)
            Debug.LogWarning("[RaycastDebugger] No PhysicsRaycaster found on the camera or in the scene. " +
                              "Assign one in the Inspector to use the raycaster-hook mode.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(_debugKey))
            _enabled = !_enabled;

        if (!_enabled) return;

        EnsurePointerData();

        if (_useRawPhysicsRay)
            RunRawPhysicsRay();

        if (_useRaycasterHook && _physicsRaycaster != null)
            RunRaycasterHook();

        if (_useEventSystemRay)
            RunEventSystemRay();
    }

    private void EnsurePointerData()
    {
        if (EventSystem.current == null) return;
        _pointerData ??= new PointerEventData(EventSystem.current);
        _pointerData.position = Input.mousePosition;
    }

    // ── Mode 1: Raw Physics.Raycast ───────────────────────────────────────────

    private void RunRawPhysicsRay()
    {
        if (_camera == null)
        {
            Debug.LogWarning("[RaycastDebugger] No camera assigned and Camera.main is null.");
            return;
        }

        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        _rawHitSomething = Physics.Raycast(ray, out _rawHit, _range, _physicsLayerMask);

        Color lineColor = _rawHitSomething ? Color.green : Color.red;
        float lineLength = _rawHitSomething ? _rawHit.distance : _range;
        Debug.DrawRay(ray.origin, ray.direction * lineLength, lineColor);

        if (_rawHitSomething)
            DrawGizmoPoint(_rawHit.point, 0.05f, Color.green);
    }

    // ── Mode 2: Direct PhysicsRaycaster hook ──────────────────────────────────

    /// <summary>
    /// Calls PhysicsRaycaster.Raycast(...) directly on the assigned component —
    /// the exact method BaseRaycaster / EventSystem invokes internally — but
    /// in isolation, so results here belong ONLY to this raycaster. This is
    /// the mode to use when you want to know "what does THIS PhysicsRaycaster
    /// see", independent of any other raycaster registered in the scene.
    /// </summary>
    private void RunRaycasterHook()
    {
        _raycasterResults.Clear();

        if (_pointerData == null) return; // no EventSystem in scene

        // BaseRaycaster.Raycast(PointerEventData, List<RaycastResult>) —
        // PhysicsRaycaster's override does its own Physics.RaycastAll against
        // its eventCamera, eventMask and maxRayIntersections, then appends
        // RaycastResults to the list we pass in.
        _physicsRaycaster.Raycast(_pointerData, _raycasterResults);

        for (int i = 0; i < _raycasterResults.Count; i++)
        {
            Color c = i == 0 ? Color.cyan : new Color(1f, 0.6f, 0f); // orange for this mode
            DrawGizmoPoint(_raycasterResults[i].worldPosition, 0.045f, c);
        }
    }

    // ── Mode 3: Full EventSystem pipeline (all raycasters merged) ────────────

    private void RunEventSystemRay()
    {
        _eventSystemResults.Clear();

        if (EventSystem.current == null || _pointerData == null)
        {
            Debug.LogWarning("[RaycastDebugger] No EventSystem in scene — can't run full pipeline ray.");
            return;
        }

        EventSystem.current.RaycastAll(_pointerData, _eventSystemResults);

        for (int i = 0; i < _eventSystemResults.Count; i++)
        {
            Color c = i == 0 ? Color.cyan : Color.yellow;
            DrawGizmoPoint(_eventSystemResults[i].worldPosition, 0.035f, c);
        }
    }

    private void DrawGizmoPoint(Vector3 point, float radius, Color color)
    {
        if (!_drawInGameView) return;

        Vector3 up = point + Vector3.up * radius;
        Vector3 down = point - Vector3.up * radius;
        Vector3 right = point + Vector3.right * radius;
        Vector3 left = point - Vector3.right * radius;
        Vector3 fwd = point + Vector3.forward * radius;
        Vector3 back = point - Vector3.forward * radius;

        Debug.DrawLine(up, down, color);
        Debug.DrawLine(right, left, color);
        Debug.DrawLine(fwd, back, color);
    }

    // ── Scene View Gizmos ─────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!_enabled) return;

        if (_useRawPhysicsRay && _rawHitSomething && _camera != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_rawHit.point, 0.06f);
            Gizmos.DrawLine(_camera.transform.position, _rawHit.point);
        }

        for (int i = 0; i < _raycasterResults.Count; i++)
        {
            Gizmos.color = i == 0 ? Color.cyan : new Color(1f, 0.6f, 0f);
            Gizmos.DrawWireSphere(_raycasterResults[i].worldPosition, 0.05f);
        }

        for (int i = 0; i < _eventSystemResults.Count; i++)
        {
            Gizmos.color = i == 0 ? Color.cyan : Color.yellow;
            Gizmos.DrawWireSphere(_eventSystemResults[i].worldPosition, 0.035f);
        }
    }

    // ── On-Screen Debug List ──────────────────────────────────────────────────

    private void OnGUI()
    {
        if (!_enabled || !_showOnScreenList) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = _fontSize,
            normal = { textColor = Color.white }
        };

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[RaycastDebugger] ({_debugKey} to toggle)");

        if (_useRawPhysicsRay)
        {
            sb.AppendLine("── Raw Physics Ray ──");
            sb.AppendLine(_rawHitSomething
                ? $"HIT: {_rawHit.collider.name}  layer={LayerMask.LayerToName(_rawHit.collider.gameObject.layer)}  dist={_rawHit.distance:F2}"
                : "no hit");
        }

        if (_useRaycasterHook)
        {
            sb.AppendLine("── PhysicsRaycaster Hook ──");
            if (_physicsRaycaster == null)
            {
                sb.AppendLine("no PhysicsRaycaster assigned/found");
            }
            else
            {
                sb.AppendLine($"raycaster: {_physicsRaycaster.name}  " +
                               $"eventCamera={_physicsRaycaster.eventCamera?.name}  " +
                               $"eventMask={_physicsRaycaster.eventMask.value}  " +
                               $"maxRayIntersections={_physicsRaycaster.maxRayIntersections}");
                AppendResults(sb, _raycasterResults);
            }
        }

        if (_useEventSystemRay)
        {
            sb.AppendLine("── Full EventSystem Pipeline (all raycasters) ──");
            AppendResults(sb, _eventSystemResults);
        }

        int lineCount = 2 + (_useRawPhysicsRay ? 2 : 0)
                          + (_useRaycasterHook ? 2 + Mathf.Max(1, _raycasterResults.Count) : 0)
                          + (_useEventSystemRay ? 1 + Mathf.Max(1, _eventSystemResults.Count) : 0);

        GUI.Box(new Rect(_screenOffset.x, _screenOffset.y, 600, 24 + 18 * lineCount), GUIContent.none);
        GUI.Label(new Rect(_screenOffset.x + 8, _screenOffset.y + 4, 580, 700), sb.ToString(), style);
    }

    private static void AppendResults(StringBuilder sb, List<RaycastResult> results)
    {
        if (results.Count == 0)
        {
            sb.AppendLine("no hits");
            return;
        }

        for (int i = 0; i < results.Count; i++)
        {
            RaycastResult r = results[i];
            GameObject go = r.gameObject;
            bool hasInteractable = go != null && go.GetComponent<IPointerInteractable>() != null;

            sb.AppendLine($"{i}: {go?.name ?? "(null)"}  " +
                          $"layer={(go != null ? LayerMask.LayerToName(go.layer) : "-")}  " +
                          $"dist={r.distance:F2}  " +
                          $"module={r.module?.GetType().Name}  " +
                          $"{(hasInteractable ? "[IPointerInteractable]" : "")}");
        }
    }
}
