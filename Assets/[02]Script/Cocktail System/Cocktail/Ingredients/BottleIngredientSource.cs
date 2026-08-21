// ============================================================
//  BottleIngredientSource.cs — Drag the whole bottle onto the mixing
//  vessel to pour. Detection is a raw raycast hover check
//  (IngredientHoverDetector), not a placement zone — while hovering
//  the shaker the bottle snaps to a small offset in front of it
//  (tune _hoverOffset freely), and on release it always snaps back to
//  its own spot regardless of outcome; it is a reusable source, never
//  consumed.
//
//  Sits alongside the bottle's existing DragableObject + IngredientButtonUI
//  (whose Invoke() does the actual pour and fires OnPoured/OnRejected).
// ============================================================

using UnityEngine;

[RequireComponent(typeof(DragableObject))]
[RequireComponent(typeof(IngredientButtonUI))]
public class BottleIngredientSource : MonoBehaviour
{
    [Tooltip("Local-space offset from the shaker's transform to snap to while hovering it during a drag.")]
    [SerializeField] private Vector3 _hoverOffset = new Vector3(0f, 0.3f, -0.2f);

    private DragableObject _dragable;
    private IngredientButtonUI _button;
    private Vector3 _homePosition;
    private bool _wasDragging;
    private bool _isHoveringShaker;

    private void Awake()
    {
        _dragable = GetComponent<DragableObject>();
        _button = GetComponent<IngredientButtonUI>();
        _homePosition = transform.position;
    }

    /// <summary>
    /// Runs after N_PlacementSystem's own Update() has already moved this object for the
    /// frame (floating with the cursor, since the shaker is no longer a placement zone), so
    /// the hover snap below always wins instead of fighting it.
    /// </summary>
    private void LateUpdate()
    {
        bool isDragging = _dragable.IsDragging;

        if (isDragging) UpdateHover();

        if (_wasDragging && !isDragging) OnDragEnded();

        _wasDragging = isDragging;
    }

    private void UpdateHover()
    {
        var shaker = IngredientHoverDetector.ResolveHoveredShaker();
        _isHoveringShaker = shaker != null;

        if (_isHoveringShaker)
            transform.position = shaker.transform.TransformPoint(_hoverOffset);
    }

    private void OnDragEnded()
    {
        // Always home, regardless of what N_PlacementSystem's own release logic decided —
        // a bottle is a source, it never actually lives anywhere but its own spot.
        transform.position = _homePosition;
        _dragable.PastLocation = _homePosition;

        if (_isHoveringShaker) _button.Invoke();

        _isHoveringShaker = false;
    }
}
