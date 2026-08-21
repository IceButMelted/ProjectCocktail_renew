// ============================================================
//  FruitPieceInstance.cs — One piece dragged off a fruit tray.
//
//  Same raycast-hover detection as BottleIngredientSource (see
//  IngredientHoverDetector) — snaps to a small offset in front of the
//  shaker while hovering it. Unlike a bottle, a fruit piece never
//  snaps back: it is destroyed the instant the drag ends regardless
//  of outcome (delivered — landed on the shaker and had room — or
//  lost — dropped anywhere else, or the shaker had no room), and its
//  tray slot spawns a fresh replacement either way.
// ============================================================

using UnityEngine;
using UnityEngine.Events;
using static E_Cocktail;

[RequireComponent(typeof(DragableObject))]
public class FruitPieceInstance : MonoBehaviour
{
    [Tooltip("Local-space offset from the shaker's transform to snap to while hovering it during a drag.")]
    [SerializeField] private Vector3 _hoverOffset = new Vector3(0f, 0.3f, -0.2f);

    [Tooltip("Fired when this piece landed on the shaker and was added.")]
    public UnityEvent OnDelivered = new UnityEvent();

    [Tooltip("Fired when this piece was dropped anywhere other than the shaker, or the shaker had no room.")]
    public UnityEvent OnLost = new UnityEvent();

    private Mixer _fruitType;
    private FruitTraySlot _origin;

    private DragableObject _dragable;
    private bool _wasDragging;
    private ShakerContents _hoveredShaker;

    /// <summary>Called once, right after Instantiate, by the tray slot that spawned this.</summary>
    public void Initialize(Mixer fruitType, FruitTraySlot origin)
    {
        _fruitType = fruitType;
        _origin = origin;
    }

    private void Awake() => _dragable = GetComponent<DragableObject>();

    private void LateUpdate()
    {
        bool isDragging = _dragable.IsDragging;

        if (isDragging) UpdateHover();

        if (_wasDragging && !isDragging) Consume();

        _wasDragging = isDragging;
    }

    private void UpdateHover()
    {
        _hoveredShaker = IngredientHoverDetector.ResolveHoveredShaker();

        if (_hoveredShaker != null)
            transform.position = _hoveredShaker.transform.TransformPoint(_hoverOffset);
    }

    private void Consume()
    {
        bool delivered = false;

        if (_hoveredShaker != null)
        {
            int before = _hoveredShaker.TotalParts;
            _hoveredShaker.TryToAddMixer(_fruitType, 1);
            delivered = _hoveredShaker.TotalParts != before;
        }

        if (delivered) OnDelivered?.Invoke();
        else OnLost?.Invoke();

        if (_origin != null) _origin.SpawnReplacement();
        Destroy(gameObject);
    }
}
