using System.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;

public class N_InputManager : MonoBehaviour
{
    [SerializeField]
    private Camera sceneCamera;
    [SerializeField]
    private float range = 100;

    private Vector3 lastPosition;
    private GameObject DraggedObject;
    private bool IsDrag;

    public Camera Camera => sceneCamera;

    [SerializeField]
    private LayerMask placementLayerMark;
    private Vector3 placementPosition;
    [SerializeField]
    private LayerMask boxplacementLayerMark;

    [SerializeField] 
    private LayerMask draggableLayer;


    public Vector3 GetSelectedMapPosition()
    {
        if (Mouse.current == null)
            return lastPosition;

        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = sceneCamera.nearClipPlane;
        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out RaycastHit boxHit, range, boxplacementLayerMark))
        {
            Renderer r = boxHit.collider.GetComponentInChildren<Renderer>();

            lastPosition = CalculateFixXorZ(boxHit, r);

            return lastPosition;
        }
        else if (Physics.Raycast(ray, out hit, range, placementLayerMark)) { 
            lastPosition = hit.point;           
        }
        
        return lastPosition;
    }

    public GameObject GetObjectSelected() {
        if (Mouse.current == null)
            return null;

        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = sceneCamera.nearClipPlane;
        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, range, draggableLayer))
        { 
            return hit.collider.gameObject;
        }
        else
            return null;
    }

    public Vector3 GetFloatingPosition(float distance)
    {
        if (Mouse.current == null)
            return Vector3.zero;

        Ray ray = sceneCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        return ray.origin + ray.direction * distance;
    }

    public bool TryGetPlacementPoint(out Vector3 point)
    {
        point = Vector3.zero;

        if (Mouse.current == null)
            return false;

        Ray ray = sceneCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit boxHit, range, boxplacementLayerMark))
        {
            Renderer r = boxHit.collider.GetComponentInChildren<Renderer>();
            if (r == null) return false;
            point = CalculateFixXorZ(boxHit, r);
            return true;
        }

        if (Physics.Raycast(ray, out RaycastHit hit, range, placementLayerMark))
        {
            point = hit.point;
            return true;
        }

        return false;
    }

    private Vector3 CalculateFixXorZ(RaycastHit boxHit ,Renderer r) {

        Bounds b = r.bounds;

        float BottomY = b.min.y;
        Vector3 center = b.center;

        bool fixZ = b.size.x > b.size.z;

        return fixZ
            ? new Vector3(boxHit.point.x, BottomY, center.z)
            : new Vector3(center.x, BottomY, boxHit.point.z);
    }
}
