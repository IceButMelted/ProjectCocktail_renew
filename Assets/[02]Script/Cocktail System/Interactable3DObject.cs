using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// EventSystem-driven 2D-sprite-in-3D-world interactable: hover and click
/// sprite swapping (like a UI Button) plus drag suppression.
///
/// Setup requirements (same as DragableObject):
///   • PhysicsRaycaster on the camera
///   • EventSystem in the scene
///
/// Drag suppression:
///   • Built-in: tracks pointer movement against <see cref="_dragThreshold"/> pixels.
///   • Optional: if a <see cref="DragableObject"/> sibling exists and reports
///     <see cref="DragableObject.BeingDrags"/> == true on pointer-up, the click
///     is also suppressed — the two components never interfere.
///
/// Extend this class for specialised interactables (e.g. CocktailShaker).
/// Override <see cref="CanClick"/> to add guards; override <see cref="OnClick"/>
/// to add custom click behaviour.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(SpriteRenderer))]
public class Interactable3DObject : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Sprites")]
    [SerializeField] protected Sprite S_Default;
    [SerializeField] protected Sprite S_Hover;
    [SerializeField] protected Sprite S_Clicked;

    [Header("Drag Detection")]
    [Tooltip("Pixels the pointer must travel while held before the gesture is " +
             "classified as a drag (suppresses the click).")]
    [SerializeField] private float _dragThreshold = 10f;

    [Space]
    public UnityEvent OnClicked;

    // ── Private State ─────────────────────────────────────────────────────────
    private SpriteRenderer _spriteRenderer;
    private bool _isHovering;
    private bool _isDragging;
    private Vector2 _pointerDownScreenPos;
    private DragableObject _dragableObject; // optional sibling — may be null

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _dragableObject = GetComponent<DragableObject>(); // null-safe — optional component

        _spriteRenderer.sprite = S_Default;
    }

    // ── IPointerEnterHandler / IPointerExitHandler ────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        _spriteRenderer.sprite = S_Hover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        _spriteRenderer.sprite = S_Default;
    }

    // ── IPointerDownHandler / IPointerUpHandler / IDragHandler ───────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        _isDragging = false;
        _pointerDownScreenPos = eventData.position;
        _spriteRenderer.sprite = S_Clicked;
    }

    /// <summary>
    /// Tracks pointer movement. Once the threshold is crossed the gesture is
    /// marked as a drag so pointer-up will not fire a click.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging) return;

        float moved = Vector2.Distance(eventData.position, _pointerDownScreenPos);
        if (moved >= _dragThreshold)
            _isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // Restore hover/default sprite regardless of whether the click fires
        _spriteRenderer.sprite = _isHovering ? S_Hover : S_Default;

        // Suppress the click if:
        //   (a) the pointer crossed _dragThreshold on this object, OR
        //   (b) a sibling DragableObject says it is (or just finished) being dragged.
        bool gestureIsDrag = _isDragging
                             || (_dragableObject != null && _dragableObject.BeingDrags);

        if (!gestureIsDrag)
            TryClick();

        _isDragging = false;
    }

    // ── Click Pipeline ────────────────────────────────────────────────────────

    private void TryClick()
    {
        if (!CanClick()) return;

        OnClicked?.Invoke();
        OnClick();
    }

    /// <summary>
    /// Override to add guard conditions (e.g. ingredient count, cooldowns).
    /// Return <c>false</c> to block both <see cref="OnClicked"/> and
    /// <see cref="OnClick"/>.  Base implementation always returns <c>true</c>.
    /// </summary>
    protected virtual bool CanClick() => true;

    /// <summary>
    /// Override to add custom behaviour that runs after a confirmed click.
    /// <see cref="OnClicked"/> has already been invoked when this is called.
    /// </summary>
    protected virtual void OnClick() { }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Swap all three sprites and immediately apply the default state.</summary>
    public void SetBTNSprite(Sprite defaultSprite, Sprite hoverSprite, Sprite clickedSprite)
    {
        S_Default = defaultSprite;
        S_Hover = hoverSprite;
        S_Clicked = clickedSprite;
        _spriteRenderer.sprite = S_Default;
    }
}