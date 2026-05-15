using UnityEngine;

public class N_InputManager : MonoBehaviour
{
    [SerializeField] private Camera    _sceneCamera;
    [SerializeField] private float     _range             = 100f;
    [SerializeField] private LayerMask _placementLayer;
    [SerializeField] private LayerMask _boxPlacementLayer;
    //[SerializeField] private LayerMask _draggableLayer;

    public Camera Camera => _sceneCamera;

    private Vector3 _lastPosition;

    public Vector3 GetSelectedMapPosition()
    {
        Ray ray = MouseRay();
        if (Physics.Raycast(ray, out RaycastHit boxHit, _range, _boxPlacementLayer))
        {
            Renderer r = boxHit.collider.GetComponentInChildren<Renderer>();
            _lastPosition = CalculateSnappedPosition(boxHit, r);
            return _lastPosition;
        }
        if (Physics.Raycast(ray, out RaycastHit hit, _range, _placementLayer))
            _lastPosition = hit.point;
        return _lastPosition;
    }

    //public DragableObject GetDragableObject()
    //{
    //    Ray ray  = MouseRay();
    //    int mask = ~_boxPlacementLayer;
    //    if (Physics.Raycast(ray, out RaycastHit hit, _range, mask))
    //        if (((1 << hit.collider.gameObject.layer) & _draggableLayer) != 0)
    //            return hit.collider.gameObject.GetComponent<DragableObject>();
    //    return null;
    //}

    //public GameObject GetObjectMouseHover()
    //{
    //    Ray ray = MouseRay();
    //    if (Physics.Raycast(ray, out RaycastHit hit, _range, _draggableLayer))
    //        return hit.collider.gameObject;
    //    return null;
    //}

    public Vector3 GetFloatingPosition(float distance)
    {
        Ray ray = MouseRay();
        return ray.origin + ray.direction * distance;
    }

    public bool TryGetPlacementPoint(out Vector3 point)
    {
        point = Vector3.zero;
        Ray ray = MouseRay();
        if (Physics.Raycast(ray, out RaycastHit boxHit, _range, _boxPlacementLayer))
        {
            Renderer r = boxHit.collider.GetComponentInChildren<Renderer>();
            if (r == null) return false;
            point = CalculateSnappedPosition(boxHit, r);
            return true;
        }
        if (Physics.Raycast(ray, out RaycastHit hit, _range, _placementLayer))
        {
            point = hit.point;
            return true;
        }
        return false;
    }

    private Ray MouseRay() => _sceneCamera.ScreenPointToRay(Input.mousePosition);

    private Vector3 CalculateSnappedPosition(RaycastHit hit, Renderer r)
    {
        if (r == null) return hit.point;
        Bounds  b       = r.bounds;
        float   bottomY = b.min.y;
        Vector3 center  = b.center;
        bool    fixZ    = b.size.x > b.size.z;
        return fixZ
            ? new Vector3(hit.point.x, bottomY, center.z)
            : new Vector3(center.x,    bottomY, hit.point.z);
    }
}
