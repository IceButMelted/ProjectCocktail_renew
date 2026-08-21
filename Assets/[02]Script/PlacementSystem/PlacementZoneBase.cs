using System;
using UnityEngine;

/// <summary>
/// Base class for every placement zone. A zone answers one question:
/// "given a raw raycast hit and the world-space bounds of the item being
/// dragged, where should that item's pivot actually end up?"
///
/// Attach a concrete subclass (VolumePlacementZone / SurfacePlacementZone)
/// to any GameObject whose collider sits on the input manager's zone layer.
/// N_InputManager finds this component via the raycast hit and delegates
/// all placement math to it — it does not know or care which zone type it hit.
/// </summary>
public abstract class PlacementZoneBase : MonoBehaviour
{
    protected enum PositioningMode { Free, SnapToCenter }

    [Header("Placement Zone")]
    [SerializeField] private bool _restrictByTag = false;
    [SerializeField] private string _requiredTag = "Placeable";
    [Header("Placement Mode")]
    [SerializeField] protected PositioningMode _positioningMode = PositioningMode.Free;
    /// <summary>Override for extra rules (item type, capacity, etc). Base check is an optional tag filter.</summary>
    public virtual bool CanPlace(GameObject item)
    {
        if (!_restrictByTag) return true;
        return item.CompareTag(_requiredTag);
    }

    /// <summary>Fired by N_PlacementSystem once an item actually lands in this zone (not on every drag frame).</summary>
    public event Action<GameObject> Placed;

    public void NotifyPlaced(GameObject item) => Placed?.Invoke(item);

    /// <summary>
    /// Returns the clamped world-space position for the item's pivot.
    /// itemBounds is the dragged item's own world-space renderer/collider bounds,
    /// used to keep it from clipping out of the zone.
    /// </summary>
    public abstract Vector3 GetClampedPosition(RaycastHit hit, Bounds itemBounds);

    /// <summary>
    /// Extra offset added on top of GetClampedPosition() before the item is moved.
    /// Surface zones need this: GetClampedPosition returns a point ON the surface,
    /// so the pivot must be lifted up by bottomOffset to sit above it correctly.
    /// Volume zones should return Vector3.zero — their clamp math already targets
    /// the pivot directly (it centers the item's own extents inside the bounds),
    /// so adding another offset on top would push the item out of the volume.
    /// </summary>
    public virtual Vector3 GetPivotOffset(float bottomOffset) => Vector3.up * bottomOffset;
}