using UnityEngine;
using UnityEngine.EventSystems;

public class UIPointerSound : PointerInteractableBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Pointer Sound IDs")]
    [SerializeField] private string _onEnterID;
    [SerializeField] private string _onExitID;
    [SerializeField] private string _onDownID;
    [SerializeField] private string _onUpID;
    [SerializeField] private string _onDragID;
    [SerializeField] private string _onEndDragID;

    [Header("Drag Throttle")]
    [SerializeField, Min(0f)] private float _dragInterval = 0.08f;

    // ── Private State ─────────────────────────────────────────────────────────

    // Per-event play-gates (suppressed while dragging)
    private bool _canPlayEnter = true;
    private bool _canPlayExit = true;
    private bool _canPlayUp = true;

    private DragableObject _dragableObject;
    private float _lastDragTime = -1f;
    private bool _wasDragging;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _dragableObject = GetComponent<DragableObject>();
    }

    private void LateUpdate()
    {
        if (!Interactable) return;
        if (_dragableObject == null) return;

        bool isDragging = _dragableObject.IsDragging;

        // Drag just started — suppress hover/up sounds
        if (isDragging && !_wasDragging)
        {
            _canPlayEnter = false;
            _canPlayExit = false;
            _canPlayUp = false;
        }

        // Throttled drag sound
        if (isDragging &&
            !string.IsNullOrEmpty(_onDragID) &&
            Time.unscaledTime - _lastDragTime >= _dragInterval)
        {
            _lastDragTime = Time.unscaledTime;
            TryPlay(_onDragID);
        }

        // Drag just ended — restore hover/up sounds and fire end-drag sound
        if (!isDragging && _wasDragging)
        {
            _lastDragTime = -1f;
            _canPlayEnter = true;
            _canPlayExit = true;
            _canPlayUp = true;
            TryPlay(_onEndDragID);
        }

        _wasDragging = isDragging;
    }

    // ── PointerInteractableBase Override ─────────────────────────────────────

    /// <summary>Sync play-gates when Interactable is toggled externally.</summary>
    protected override void OnInteractableChanged(bool interactable)
    {
        _canPlayEnter = interactable;
        _canPlayExit = interactable;
        _canPlayUp = interactable;
    }

    // ── Pointer Overrides ─────────────────────────────────────────────────────

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!_canPlayEnter) return;
        TryPlay(_onEnterID);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!_canPlayExit) return;
        TryPlay(_onExitID);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        TryPlay(_onDownID);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!_canPlayUp) return;
        TryPlay(_onUpID);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void TryPlay(string id)
    {
        if (!string.IsNullOrEmpty(id))
            ManagerSound.PlayEffect(id);
    }
}