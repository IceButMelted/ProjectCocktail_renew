using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class Interactable_3DObject : PointerInteractableBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Drag Detection")]
    [Tooltip("Pixels the pointer must travel before the gesture is classified as a drag.")]
    [SerializeField] private float _dragThreshold = 10f;

    [Space]
    public UnityEvent OnClicked;

    // ── Private State ─────────────────────────────────────────────────────────

    private DragableObject _dragableObject;        // optional sibling
    private bool _isDragging;
    private Vector2 _pointerDownScreenPos;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        _dragableObject = GetComponent<DragableObject>();
    }

    // ── PointerInteractableBase Override ─────────────────────────────────────

    protected override void OnInteractableChanged(bool interactable) { }

    // ── Pointer Handlers ──────────────────────────────────────────────────────

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!Interactable) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        _isDragging = false;
        _pointerDownScreenPos = eventData.position;
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (_isDragging) return;
        if (Vector2.Distance(eventData.position, _pointerDownScreenPos) >= _dragThreshold)
            _isDragging = true;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!Interactable) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        bool gestureIsDrag = _isDragging
                             || (_dragableObject != null && _dragableObject.BeingDrags);
        if (!gestureIsDrag)
            TryClick();

        _isDragging = false;
    }

    // ── Click Pipeline ────────────────────────────────────────────────────────

    private void TryClick()
    {
        if (!CanClick()) return;
        OnClicked?.Invoke();
        OnClick();
    }

    /// <summary>
    /// Override to add extra guard conditions (cooldowns, resource checks, etc.).
    /// Base always returns <see cref="PointerInteractableBase.Interactable"/>.
    /// </summary>
    protected virtual bool CanClick() => Interactable;

    /// <summary>
    /// Override to run custom logic after a confirmed click.
    /// <see cref="OnClicked"/> has already fired when this is called.
    /// </summary>
    protected virtual void OnClick() { }
}