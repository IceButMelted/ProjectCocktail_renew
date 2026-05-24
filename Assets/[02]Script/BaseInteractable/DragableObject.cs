using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to any 3D object that should be draggable on the placement grid.
/// Requires a PhysicsRaycaster on the camera and an EventSystem in the scene.
///
/// CLICK  — PointerDown + PointerUp without crossing _dragThreshold pixels
///          => fires the sibling Button.onClick (if one exists).
/// DRAG   — pointer moves past _dragThreshold while held
///          => calls N_PlacementSystem.StartDrag, moves object until pointer-up.
/// </summary>
public class DragableObject : PointerInteractableBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Drag Settings")]
    [Tooltip("Pixels pointer must move while held before drag begins.")]
    [SerializeField] private float _dragThreshold = 10f;

    [Tooltip("Layers to check for overlapping objects.")]
    public LayerMask detectLayerMask;

    [Header("References")]
    [SerializeField] private N_PlacementSystem _placementSystem;

    [Header("Debug")]
    public bool DebugDraw = false;

    // ── Public State ──────────────────────────────────────────────────────────

    public bool CanPlaced { get; set; } = false;
    public bool BeingDrags { get; set; } = false;
    public Vector3 PastLocation { get; set; }
    public int NumbersObjectOverlaying { get; private set; }

    /// <summary>True only while an active drag gesture is in progress.</summary>
    public bool IsDragging => BeingDrags && _dragStarted;

    // ── Private State ─────────────────────────────────────────────────────────

    private Button _button;
    private Collider _collider;
    private Vector2 _pointerDownScreenPos;
    private bool _dragStarted;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        PastLocation = transform.position;
        _button = GetComponent<Button>();
        _collider = GetComponent<Collider>();

        if (_collider == null)
            Debug.LogWarning($"[DragableObject] No Collider found on '{name}'.");

        if (_placementSystem == null)
            _placementSystem = FindFirstObjectByType<N_PlacementSystem>();
    }

    private void Update()
    {
        if (BeingDrags)
            CheckForCollisions();
    }

    // ── PointerInteractableBase Overrides ─────────────────────────────────────

    /// <summary>Cancel any active drag when disabled at runtime.</summary>
    protected override void OnInteractableChanged(bool interactable)
    {
        if (!interactable && _dragStarted)
            CancelDrag();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!Interactable) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        _dragStarted = false;
        _pointerDownScreenPos = eventData.position;
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (!Interactable || _dragStarted) return;

        if (Vector2.Distance(eventData.position, _pointerDownScreenPos) >= _dragThreshold)
        {
            _dragStarted = true;
            _placementSystem.StartDrag(this);
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!Interactable) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (_dragStarted)
        {
            _placementSystem.ReleaseObject();
            BeingDrags = false;
        }
        else
        {
            _button?.onClick.Invoke();
        }

        _dragStarted = false;
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private void CancelDrag()
    {
        _placementSystem?.ReleaseObject();
        BeingDrags = false;
        _dragStarted = false;
    }

    private void CheckForCollisions()
    {
        if (_collider == null) return;

        Bounds bounds = _collider.bounds;
        Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation, detectLayerMask);
        NumbersObjectOverlaying = hits.Length - 1;          // subtract self
        CanPlaced = NumbersObjectOverlaying <= 0;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

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