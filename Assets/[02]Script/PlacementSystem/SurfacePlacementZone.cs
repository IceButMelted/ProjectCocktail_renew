using UnityEngine;

/// <summary>
/// Zone type B — "place on a surface".
/// The item rests on top of the surface and, by default, is free to
/// translate along the surface's local X/Z plane, clamped to the surface's
/// own extents. Height is locked to the surface's top so the item never
/// sinks in or floats.
///
/// Enable _snapToCenter to ignore where on the surface you drop the item
/// and always snap it to the exact center point of the surface instead.
///
/// Setup:
///   - Attach directly to the surface GameObject (table top, box lid, etc.)
///     — reuse its existing collider, no separate trigger volume needed.
///   - Put that collider on the layer assigned to N_InputManager._zoneLayer.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SurfacePlacementZone : PlacementZoneBase
{
    [Tooltip("If true, exposes the surface's up vector for callers that want to align item rotation to it.")]
    [SerializeField] private bool _alignToNormal = false;


    private Collider _surfaceCollider;

    private void Awake() => _surfaceCollider = GetComponent<Collider>();

    public override Vector3 GetClampedPosition(RaycastHit hit, Bounds itemBounds)
    {
        Bounds surfaceBounds = _surfaceCollider.bounds;
        Vector3 halfSurface = Abs(transform.InverseTransformVector(surfaceBounds.extents));

        if (_positioningMode == PositioningMode.SnapToCenter)
        {
            Vector3 centerLocal = transform.InverseTransformPoint(surfaceBounds.center);
            return transform.TransformPoint(new Vector3(centerLocal.x, halfSurface.y, centerLocal.z));
        }

        Vector3 local = transform.InverseTransformPoint(hit.point);
        Vector3 halfItem = Abs(transform.InverseTransformVector(itemBounds.extents));

        float x = Mathf.Clamp(local.x, -halfSurface.x + halfItem.x, halfSurface.x - halfItem.x);
        float z = Mathf.Clamp(local.z, -halfSurface.z + halfItem.z, halfSurface.z - halfItem.z);

        // Lock to the surface's own top in local space (equivalent to the old
        // "bottomY = bounds.min.y" snap, but expressed relative to this transform).
        float y = halfSurface.y;

        return transform.TransformPoint(new Vector3(x, y, z));
    }

    /// <summary>Surface's up vector, useful if you want to rotate the placed item to match a tilted surface.</summary>
    public Vector3 GetSurfaceNormal() => _alignToNormal ? transform.up : Vector3.up;

    private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    private void OnDrawGizmosSelected()
    {
        if (_surfaceCollider == null) _surfaceCollider = GetComponent<Collider>();
        if (_surfaceCollider == null) return;
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.35f);
        Gizmos.DrawWireCube(_surfaceCollider.bounds.center, _surfaceCollider.bounds.size);
    }
}