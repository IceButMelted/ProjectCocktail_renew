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
    public static N_PlacementSystem _placementSystem;

    [Header("Debug")]
    public bool DebugDraw = false;

    /// <summary>True while ANY DragableObject is being dragged.
    /// Used by other interactables to suppress hover effects.</summary>
    public static bool IsAnyDragging { get; private set; }

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
    private void OnEnable()
    {
        if (_placementSystem == null)
            _placementSystem = FindFirstObjectByType<N_PlacementSystem>();
    }

    private void Awake()
    {
        PastLocation = transform.position;
        _button = GetComponent<Button>();
        _collider = GetComponent<Collider>();

        if (_collider == null)
            Debug.LogWarning($"[DragableObject] No Collider found on '{name}'.");
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
            if (OnThresholdCrossed(eventData)) return; // handed off to a different object — don't also drag this one

            _dragStarted = true;
            IsAnyDragging = true;
            _placementSystem.StartDrag(this);
        }
    }

    /// <summary>
    /// Override to redirect this gesture onto a different DragableObject entirely instead of
    /// dragging this one (e.g. DragableFruitTraySlot handing off to a spawned FruitPieceInstance).
    /// Return true to mean "handled — don't drag me." Default behaviour is unchanged for every
    /// object that doesn't override this.
    /// </summary>
    protected virtual bool OnThresholdCrossed(PointerEventData eventData) => false;

    /// <summary>
    /// Called by whoever decided to hand a drag gesture off to this object instead of the one
    /// that actually received OnPointerDown/OnDrag — takes over exactly where a normal drag
    /// would have started.
    /// </summary>
    public void BeginRedirectedDrag(PointerEventData eventData)
    {
        if (!Interactable || _dragStarted) return;

        _pointerDownScreenPos = eventData.position;
        _dragStarted = true;
        IsAnyDragging = true;
        _placementSystem.StartDrag(this);
    }

    /// <summary>
    /// Called by whoever redirected a drag onto this object (see <see cref="BeginRedirectedDrag"/>)
    /// once the pointer is released, since this object never receives its own OnPointerUp for a
    /// gesture that started on a different GameObject.
    /// </summary>
    public void FinishRedirectedDrag()
    {
        if (!_dragStarted) return;

        _placementSystem.ReleaseObject();
        BeingDrags = false;
        IsAnyDragging = false;
        _dragStarted = false;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!Interactable) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (_dragStarted)
        {
            _placementSystem.ReleaseObject();
            BeingDrags = false;
            IsAnyDragging = false;
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
        IsAnyDragging = false;
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