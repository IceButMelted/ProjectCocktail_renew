using UnityEngine;
using UnityEngine.EventSystems;
public class UIPointerSound : MonoBehaviour,
    IPointerEnterHandler, 
    IPointerExitHandler,
    IPointerDownHandler, 
    IPointerUpHandler
{
    [Header("Pointer Sound IDs")]
    [SerializeField] private string _onEnterID;
    private bool _canPlayEnter = true;
    [SerializeField] private string _onExitID;
    private bool _canPlayExit = true;
    [SerializeField] private string _onDownID;
    [SerializeField] private string _onUpID;
    private bool _canPlayUp = true;
    [SerializeField] private string _onDragID;
    [SerializeField] private string _onEndDragID;
    [Header("Drag Throttle")]
    [SerializeField, Min(0f)] private float _dragInterval = 0.08f;


    private DragableObject _dragableObject;
    private float _lastDragTime = -1f;
    private bool _wasDragging = false;
    private bool _isHovering = false;

    private void Awake()
    {
        _dragableObject = GetComponent<DragableObject>();
    }
    private void LateUpdate()
    {
        if (_dragableObject == null) return;

        if (!_wasDragging)
        {
            _canPlayUp = true;
        }
        else
        { 
            _canPlayUp = false;
        }

            bool isDragging = _dragableObject.IsDragging;
        if (isDragging && !_wasDragging)
        {
            _canPlayEnter = false;
            _canPlayExit = false;
            _canPlayUp = false;
        }
        if (isDragging)
        {
            if (!string.IsNullOrEmpty(_onDragID) &&
                Time.unscaledTime - _lastDragTime >= _dragInterval)
            {
                _lastDragTime = Time.unscaledTime;
                TryPlay(_onDragID);
            }
        }
        if (!isDragging && _wasDragging)
        {
            _lastDragTime = -1f;
            _canPlayEnter = true;
            _canPlayExit = true;
            TryPlay(_onEndDragID);
        }
        _wasDragging = isDragging;
        
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        if (!_canPlayEnter) return;
        TryPlay(_onEnterID);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        if (!_canPlayExit) return;
        TryPlay(_onExitID);
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        TryPlay(_onDownID);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_canPlayUp) return;
        TryPlay(_onUpID);
    }

    private static void TryPlay(string id)
    {
        if (!string.IsNullOrEmpty(id))
            ManagerSound.PlayEffect(id);
    }
}
