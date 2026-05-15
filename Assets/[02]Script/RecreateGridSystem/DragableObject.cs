using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to any 3D object that should be draggable on the placement grid.
/// Requires a PhysicsRaycaster on the camera and an EventSystem in the scene
/// (same setup ScaleOnHover uses).
///
/// CLICK  — PointerDown + PointerUp without crossing _dragThreshold pixels
///          => fires the sibling Button.onClick (if one exists).
/// DRAG   — pointer moves past _dragThreshold while held
///          => calls N_PlacementSystem.StartDrag, moves object until pointer-up.
/// </summary>
//[RequireComponent(typeof(Collider))]
public class DragableObject : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [Header("Drag Settings")]
    [Tooltip("Pixels pointer must move while held before drag begins.")]
    [SerializeField] private float _dragThreshold = 10f;

    [Tooltip("Layers to check for overlapping objects.")]
    public LayerMask detectLayerMask;

    [Header("References")]
    [SerializeField] private N_PlacementSystem _placementSystem;

    [Header("Debug")]
    public bool DebugDraw = false;

    // State readable by N_PlacementSystem
    public bool    CanPlaced               { get; set; } = false;
    public bool    BeingDrags              { get; set; } = false;
    public Vector3 PastLocation            { get; set; }
    public int     NumbersObjectOverlaying { get; private set; }

    public bool IsDragging => BeingDrags && _dragStarted;

    private Button   _button;
    private Collider  _collider;
    private Vector2 _pointerDownScreenPos;
    private bool    _dragStarted;

private void Awake()
    {
        PastLocation = transform.position;
        _button      = GetComponent<Button>();
        _collider    = GetComponent<Collider>();

        if (_collider == null)
            Debug.LogWarning($"[DragableObject] No Collider found on '{name}' or its children.");

        if (_placementSystem == null)
            _placementSystem = FindFirstObjectByType<N_PlacementSystem>();
    }

    private void Update()
    {
        if (BeingDrags)
            CheckForCollisions();
    }

    // ── EventSystem Pointer Handlers ──────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        _dragStarted          = false;
        _pointerDownScreenPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragStarted) return;

        float moved = Vector2.Distance(eventData.position, _pointerDownScreenPos);
        if (moved >= _dragThreshold)
        {
            _dragStarted = true;
            _placementSystem.StartDrag(this);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (_dragStarted)
        {
            _placementSystem.ReleaseObject();
            BeingDrags   = false;
        }
        else
        {
            // Clean click — fire Button if present
            _button?.onClick.Invoke();
        }

        _dragStarted = false;
    }

    // ── Collision Check ───────────────────────────────────

private void CheckForCollisions()
    {
        if (_collider == null) return;

        Bounds  b    = _collider.bounds;
        Collider[] hits = Physics.OverlapBox(b.center, b.extents, transform.rotation, detectLayerMask);
        NumbersObjectOverlaying = hits.Length - 1;
        CanPlaced = NumbersObjectOverlaying <= 0;
    }

    // ── Gizmos ────────────────────────────────────────────

private void OnDrawGizmos()
    {
        if (!DebugDraw) return;
        Collider col = _collider != null ? _collider : GetComponent<Collider>();
        if (col == null) return;
        Bounds b = col.bounds;
        Gizmos.color = CanPlaced ? Color.green : Color.red;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
