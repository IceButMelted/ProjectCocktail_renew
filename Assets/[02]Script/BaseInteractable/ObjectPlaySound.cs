using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Plays sound effects in response to pointer events on the same GameObject.
///
/// SOLID notes:
///   S — plays sounds only; drag-state awareness is read from the sibling
///       DragableObject rather than re-implementing it here.
///   O — add new sound IDs in the inspector; no code changes needed.
///   L — substitutable for any PointerInteractableBase.
///   I — TryPlay is a private helper; external code never needs to call it.
///   D — depends on DragableObject via interface property (IsDragging /
///       BeingDrags). Could be further abstracted behind IDraggable.
/// </summary>
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
        if (!Interactable || _dragableObject == null) return;

        bool isDragging = _dragableObject.IsDragging;

        if (isDragging && !_wasDragging)
            SuppressHoverSounds();

        if (isDragging)
            TickDragSound();

        if (!isDragging && _wasDragging)
            RestoreHoverSounds();

        _wasDragging = isDragging;
    }

    // ── PointerInteractableBase Override ─────────────────────────────────────

    protected override void OnInteractableChanged(bool interactable)
    {
        _canPlayEnter = interactable;
        _canPlayExit = interactable;
        _canPlayUp = interactable;
    }

    // ── Pointer Overrides ─────────────────────────────────────────────────────

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (_canPlayEnter) TryPlay(_onEnterID);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (_canPlayExit) TryPlay(_onExitID);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        TryPlay(_onDownID);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (_canPlayUp) TryPlay(_onUpID);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private void SuppressHoverSounds()
    {
        _canPlayEnter = false;
        _canPlayExit = false;
        _canPlayUp = false;
    }

    private void RestoreHoverSounds()
    {
        _lastDragTime = -1f;
        _canPlayEnter = true;
        _canPlayExit = true;
        _canPlayUp = true;
        TryPlay(_onEndDragID);
    }

    private void TickDragSound()
    {
        if (!string.IsNullOrEmpty(_onDragID) &&
            Time.unscaledTime - _lastDragTime >= _dragInterval)
        {
            _lastDragTime = Time.unscaledTime;
            TryPlay(_onDragID);
        }
    }

    private static void TryPlay(string id)
    {
        if (!string.IsNullOrEmpty(id))
            ManagerSound.PlayEffect(id);
    }
}