// ============================================================
//  BottleIngredientSource.cs — Drag the whole bottle onto the mixing
//  vessel to pour. Detection is a raw raycast hover check
//  (IngredientHoverDetector), not a placement zone — while hovering the
//  shaker the bottle snaps to a small offset in front of it (tune
//  _hoverOffset freely) and always snaps back home on release regardless
//  of outcome; it's a reusable source, never consumed.
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
    private int _homeLayer;
    private int _ignoreRaycastLayer;
    private bool _wasDragging;
    private bool _isHoveringShaker;

    private void Awake()
    {
        _dragable = GetComponent<DragableObject>();
        _button = GetComponent<IngredientButtonUI>();
        _homePosition = transform.position;
        _homeLayer = gameObject.layer;
        _ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
    }

    /// <summary>
    /// Only while active (AddIngredient phase — see
    /// InteractableToggle.ApplyOnlyBottleIngredientSource) should the shared DragableObject
    /// skip placement zones and float. During PrepareBar this component is disabled so the
    /// bottle reverts to a plain bar-layout DragableObject; setting the flag in Awake instead
    /// would leave it floating there too, never finding a valid zone, snapping back to
    /// PastLocation every release.
    /// </summary>
    private void OnEnable()
    {
        if (_dragable != null) _dragable.IgnorePlacementZones = true;
    }

    private void OnDisable()
    {
        if (_dragable != null) _dragable.IgnorePlacementZones = false;
    }

    /// <summary>
    /// Runs after N_PlacementSystem's Update() already moved this object for the frame
    /// (floating with the cursor, since the shaker isn't a placement zone), so the hover
    /// snap below always wins instead of fighting it.
    /// </summary>
    private void LateUpdate()
    {
        bool isDragging = _dragable.IsDragging;

        if (!_wasDragging && isDragging) OnDragStarted();

        if (isDragging) UpdateHover();

        if (_wasDragging && !isDragging) OnDragEnded();

        _wasDragging = isDragging;
    }

    private void OnDragStarted()
    {
        // Excluded from its own hover raycast for the whole drag. Otherwise, once it snaps
        // in front of the shaker its own collider sits on the same camera-to-mouse ray
        // IngredientHoverDetector casts, intercepting it — hover flips off next frame, snap
        // undoes, ray reaches shaker again, hover flips back on: a 2-frame oscillation that
        // reads as the object shaking in place.
        gameObject.layer = _ignoreRaycastLayer;
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
        // Always home, regardless of N_PlacementSystem's own release logic — a bottle is
        // a source, it never lives anywhere but its own spot.
        transform.position = _homePosition;
        _dragable.PastLocation = _homePosition;
        gameObject.layer = _homeLayer;

        if (_isHoveringShaker) _button.Invoke();

        _isHoveringShaker = false;
    }
}
