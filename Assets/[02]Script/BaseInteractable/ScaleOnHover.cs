using UnityEngine;
using UnityEngine.EventSystems;

public class ScaleOnHover : PointerInteractableBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [SerializeField] private float newScale = 1.1f;
    [SerializeField] private float scaleDuration = 0.2f;
    [SerializeField] private GameObject scaleTarget; //Optinal to prevent scaling collider

    // ── Private State ─────────────────────────────────────────────────────────

    private Vector3 _initScale;
    private Vector3 _targetScale;
    private bool _isScaling;

    private DragableObject _dragable;
    private bool _wasDragging;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _initScale = transform.localScale;
        _targetScale = _initScale;
        _dragable = GetComponent<DragableObject>();

        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"ScaleOnHover on '{gameObject.name}' requires a Collider. Adding BoxCollider...");
            gameObject.AddComponent<BoxCollider>();
        }

        // If no scale target is assigned, default to the GameObject this script is attached to.
        if (scaleTarget == null) { 
            scaleTarget = this.gameObject;
        }
    }

    private void Update()
    {
        // OnPointerExit ignores IsAnyDragging while a drag is in progress (see below), so if
        // this object's own drag causes it to lose hover mid-drag (e.g. BottleIngredientSource
        // switching it to the "Ignore Raycast" layer), that exit is silently swallowed and the
        // enlarged scale is never undone by a pointer event. Force it back down the moment this
        // object's own drag ends, regardless of hover state.
        if (_dragable != null)
        {
            bool isDragging = _dragable.IsDragging;
            if (_wasDragging && !isDragging) ChangeScale(false);
            _wasDragging = isDragging;
        }

        if (!_isScaling) return;

        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime / scaleDuration);

        if (Vector3.Distance(transform.localScale, _targetScale) < 0.001f)
        {
            transform.localScale = _targetScale;
            _isScaling = false;
        }
    }

    // ── PointerInteractableBase Overrides ─────────────────────────────────────

    /// <summary>Snap back to normal scale when disabled mid-hover.</summary>
    protected override void OnInteractableChanged(bool interactable)
    {
        if (!interactable)
            ChangeScale(false);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!Interactable) return;
        if (DragableObject.IsAnyDragging) return;
        ChangeScale(true);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (DragableObject.IsAnyDragging) return;
        ChangeScale(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ChangeScale(bool enlarge)
    {
        _targetScale = enlarge ? _initScale * newScale : _initScale;
        _isScaling = true;
    }

    /// <summary>
    /// Forces the enlarged-hover scale back down immediately, bypassing pointer-event/drag
    /// state entirely. For a DragableFruitTraySlot host: hijacking a drag onto a spawned
    /// FruitPieceInstance means the host's own DragableObject.IsDragging never turns true (see
    /// DragableFruitTraySlot.OnThresholdCrossed), so this component's own drag-end watch above
    /// never fires for it — call this directly from the hijack site instead.
    /// </summary>
    public void ForceReset() => ChangeScale(false);
}