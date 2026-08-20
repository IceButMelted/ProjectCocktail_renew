using UnityEngine;

public class N_InputManager : MonoBehaviour
{
    [SerializeField] private Camera _sceneCamera;
    [SerializeField] private float _range = 100f;

    [Header("Placement")]
    [Tooltip("Layer used by BOTH VolumePlacementZone and SurfacePlacementZone colliders. " +
             "Replaces the old separate _placementLayer / _boxPlacementLayer split — " +
             "the zone type is now read off the hit collider's component instead of the layer.")]
    [SerializeField] private LayerMask _zoneLayer;

    [Header("Dragging")]
    [SerializeField] private LayerMask _draggableLayer;

    public Camera Camera => _sceneCamera;

    private Vector3 _lastPosition;

    /// <summary>Drives a free-floating mouse indicator. Not clamped to any item size —
    /// use TryGetPlacementPoint for the actual dragged-object position.</summary>
    public Vector3 GetSelectedMapPosition()
    {
        Ray ray = MouseRay();
        if (Physics.Raycast(ray, out RaycastHit hit, _range, _zoneLayer))
            _lastPosition = hit.point;
        return _lastPosition;
    }

    public DragableObject GetDragableObject()
    {
        Ray ray = MouseRay();
        if (Physics.Raycast(ray, out RaycastHit hit, _range, _draggableLayer))
            return hit.collider.GetComponentInParent<DragableObject>();
        return null;
    }

    public GameObject GetObjectMouseHover()
    {
        Ray ray = MouseRay();
        if (Physics.Raycast(ray, out RaycastHit hit, _range, _draggableLayer))
            return hit.collider.gameObject;
        return null;
    }

    public Vector3 GetFloatingPosition(float distance)
    {
        Ray ray = MouseRay();
        return ray.origin + ray.direction * distance;
    }

    /// <summary>
    /// Raycasts against the zone layer and, if it hits a PlacementZoneBase,
    /// asks that zone to resolve a clamped position for an item of the given
    /// world-space bounds. All zone-specific math lives on the zone itself.
    /// </summary>
    public bool TryGetPlacementPoint(Bounds itemBounds, out Vector3 point, out PlacementZoneBase zone)
    {
        point = Vector3.zero;
        zone = null;

        Ray ray = MouseRay();
        if (!Physics.Raycast(ray, out RaycastHit hit, _range, _zoneLayer))
            return false;

        zone = hit.collider.GetComponentInParent<PlacementZoneBase>();
        if (zone == null)
            return false;

        point = zone.GetClampedPosition(hit, itemBounds);
        return true;
    }

    private Ray MouseRay() => _sceneCamera.ScreenPointToRay(Input.mousePosition);
}