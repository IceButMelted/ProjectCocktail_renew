using UnityEngine;

public class N_PlacementSystem : MonoBehaviour
{
    [SerializeField] private GameObject _mouseIndicator;
    [SerializeField] private N_InputManager _inputManager;
    [SerializeField] private float _floatingDistance = 3f;

    private DragableObject _selectedDragable;
    private GameObject _selectedObject;
    private Collider _selectedCollider;

    private PlacementZoneBase _currentZone;
    private Vector3 _lastValidPoint;

    private float _bottomOffset;
    private bool _isFloating;

    private void Update()
    {
        _mouseIndicator.transform.position = _inputManager.GetSelectedMapPosition();

        if (_selectedObject != null)
            UpdateDragPosition();
    }

    /// <summary>Called by DragableObject when drag threshold is crossed.</summary>
    public void StartDrag(DragableObject dragable)
    {
        if (dragable == null) return;
        _selectedDragable = dragable;
        _selectedObject = dragable.gameObject;
        _selectedCollider = _selectedObject.GetComponentInChildren<Collider>();
        _bottomOffset = GetBottomOffset(_selectedObject);
        _selectedDragable.BeingDrags = true;
    }

    /// <summary>Called by DragableObject on mouse-up after dragging.</summary>
    public void ReleaseObject()
    {
        if (_selectedObject != null && _selectedDragable != null)
        {
            if (_selectedDragable.CanPlaced && !_isFloating &&
                _currentZone != null && _currentZone.CanPlace(_selectedObject))
            {
                _selectedDragable.PastLocation = _lastValidPoint;
                _selectedObject.transform.position = _lastValidPoint;
            }
            else
            {
                _selectedObject.transform.position = _selectedDragable.PastLocation;
            }

            _selectedDragable.BeingDrags = false;
            _selectedDragable.CanPlaced = true;
        }
        ResetSelection();
    }

    private void UpdateDragPosition()
    {
        if (_inputManager.TryGetPlacementPoint(_selectedCollider.bounds, out Vector3 placementPoint, out PlacementZoneBase zone))
        {
            _currentZone = zone;
            _lastValidPoint = placementPoint + zone.GetPivotOffset(_bottomOffset);
            _selectedObject.transform.position = _lastValidPoint;
            _isFloating = false;
        }
        else
        {
            _currentZone = null;
            _selectedObject.transform.position = _inputManager.GetFloatingPosition(_floatingDistance);
            _isFloating = true;
        }
    }

    private float GetBottomOffset(GameObject obj)
    {
        Renderer r = obj.GetComponentInChildren<Renderer>();
        if (r == null) return 0f;
        return obj.transform.position.y - r.bounds.min.y;
    }

    private void ResetSelection()
    {
        _selectedObject = null;
        _selectedDragable = null;
        _selectedCollider = null;
        _currentZone = null;
        _isFloating = false;
    }
}