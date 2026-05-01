using UnityEngine;

public class N_PlacementSystem : MonoBehaviour
{
    [SerializeField] private GameObject     _mouseIndicator;
    [SerializeField] private N_InputManager _inputManager;
    [SerializeField] private float          _floatingDistance = 3f;

    private DragableObject _selectedDragable;
    private GameObject     _selectedObject;
    private float          _bottomOffset;
    private bool           _isFloating;

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
        _selectedDragable            = dragable;
        _selectedObject              = dragable.gameObject;
        _bottomOffset                = GetBottomOffset(_selectedObject);
        _selectedDragable.BeingDrags = true;
    }

    /// <summary>Called by DragableObject on mouse-up after dragging.</summary>
    public void ReleaseObject()
    {
        if (_selectedObject != null && _selectedDragable != null)
        {
            if (_selectedDragable.CanPlaced && !_isFloating)
            {
                Vector3 snapPos = _inputManager.GetSelectedMapPosition() + Vector3.up * _bottomOffset;
                _selectedDragable.PastLocation     = snapPos;
                _selectedObject.transform.position = snapPos;
            }
            else
            {
                _selectedObject.transform.position = _selectedDragable.PastLocation;
            }
            _selectedDragable.BeingDrags = false;
            _selectedDragable.CanPlaced  = true;
        }
        ResetSelection();
    }

    private void UpdateDragPosition()
    {
        if (_inputManager.TryGetPlacementPoint(out Vector3 placementPoint))
        {
            _selectedObject.transform.position = placementPoint + Vector3.up * _bottomOffset;
            _isFloating = false;
        }
        else
        {
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
        _selectedObject   = null;
        _selectedDragable = null;
        _isFloating       = false;
    }
}
