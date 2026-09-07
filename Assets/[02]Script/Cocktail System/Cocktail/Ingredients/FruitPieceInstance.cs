// ============================================================
//  FruitPieceInstance.cs — One piece dragged off a fruit tray.
//
//  Exists (collider active) from the moment its tray slot spawns it, but
//  stays invisible until an actual drag begins — the tray's own art
//  shows "fruit is here"; this becomes visible only once pulled out.
//  Same raycast-hover detection as BottleIngredientSource (see
//  IngredientHoverDetector) — snaps to a small offset in front of the
//  shaker while hovering it. Unlike a bottle, never snaps back: destroyed
//  the instant the drag ends regardless of outcome (delivered — landed
//  on shaker with room — or lost — dropped elsewhere, or shaker full),
//  and its tray slot spawns a fresh (also invisible-until-dragged)
//  replacement either way.
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
    private Renderer[] _renderers;
    private int _homeLayer;
    private int _ignoreRaycastLayer;
    private bool _wasDragging;
    private ShakerContents _hoveredShaker;

    /// <summary>Called once, right after Instantiate, by the tray slot that spawned this.</summary>
    public void Initialize(Mixer fruitType, FruitTraySlot origin)
    {
        _fruitType = fruitType;
        _origin = origin;
    }

    private void Awake()
    {
        _dragable = GetComponent<DragableObject>();
        _homeLayer = gameObject.layer;
        _ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        // Invisible until pulled from the tray — tray's own art reads as "fruit is here";
        // collider stays active so a drag can still start on it.
        _renderers = GetComponentsInChildren<Renderer>(true);
        SetVisible(false);
    }

    /// <summary>See BottleIngredientSource.OnEnable/OnDisable — same reasoning, kept in sync
    /// with this component's active lifetime rather than set once in Awake.</summary>
    private void OnEnable()
    {
        if (_dragable != null) _dragable.IgnorePlacementZones = true;
    }

    private void OnDisable()
    {
        if (_dragable != null) _dragable.IgnorePlacementZones = false;
    }

    private void LateUpdate()
    {
        bool isDragging = _dragable.IsDragging;

        if (!_wasDragging && isDragging) OnDragStarted();

        if (isDragging) UpdateHover();

        if (_wasDragging && !isDragging) Consume();

        _wasDragging = isDragging;
    }

    private void OnDragStarted()
    {
        SetVisible(true);

        // Same self-occlusion fix as BottleIngredientSource: excluded from its own hover
        // raycast during the drag, otherwise snapping in front of the shaker puts this
        // piece's collider on the same ray IngredientHoverDetector casts, causing flicker.
        gameObject.layer = _ignoreRaycastLayer;
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

    private void SetVisible(bool visible)
    {
        for (int i = 0; i < _renderers.Length; i++) _renderers[i].enabled = visible;
    }
}
