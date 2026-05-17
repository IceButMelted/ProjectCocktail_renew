using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// EventSystem-driven 3D interactable that behaves like a UI Button:
///   - Interactable toggle inherited from <see cref="PointerInteractableBase"/>
///   - Disabled colour tint when non-interactable
///   - Hover / click sprite swap
///   - Drag suppression (own threshold + optional DragableObject sibling)
///
/// Extend this class and override CanClick() / OnClick() for custom behaviour.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(SpriteRenderer))]
public class Interactable3DObject : PointerInteractableBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Sprites")]
    [SerializeField] protected Sprite S_Default;
    [SerializeField] protected Sprite S_Hover;
    [SerializeField] protected Sprite S_Clicked;
    [Tooltip("Sprite to show when Interactable is false. If null, falls back to S_Default with disabled colour.")]
    [SerializeField] private Sprite _disabledSprite;
    [Tooltip("Sprite colour applied when Interactable is false (matches Unity Button behaviour).")]
    [SerializeField] private Color _disabledColour = new Color(1f, 1f, 1f, 0.5f);

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
        _dragableObject = GetComponent<DragableObject>();
        _spriteRenderer.sprite = S_Default;
        ApplyInteractableVisual();
    }

    // ── PointerInteractableBase Override ─────────────────────────────────────

    /// <summary>Refresh sprite and tint whenever Interactable flips.</summary>
    protected override void OnInteractableChanged(bool interactable)
    {
        ApplyInteractableVisual();
    }

    // ── Pointer Handlers ──────────────────────────────────────────────────────

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (_isDragging) return;
        if (!Interactable) return;
        _isHovering = true;
        _spriteRenderer.sprite = S_Hover;
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        if (!Interactable) return;
        _spriteRenderer.sprite = S_Default;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!Interactable) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        _isDragging = false;
        _pointerDownScreenPos = eventData.position;
        _spriteRenderer.sprite = S_Clicked;
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (_isDragging) return;
        if (Vector2.Distance(eventData.position, _pointerDownScreenPos) >= _dragThreshold)
            _isDragging = true;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!Interactable) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        _spriteRenderer.sprite = _isHovering ? S_Hover : S_Default;

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
    /// Override to add extra guard conditions (e.g. cooldowns, ingredient count).
    /// Return false to block the click. Base always returns <see cref="PointerInteractableBase.Interactable"/>.
    /// </summary>
    protected virtual bool CanClick() => Interactable;

    /// <summary>
    /// Override to run custom logic after a confirmed click.
    /// OnClicked has already been invoked when this is called.
    /// </summary>
    protected virtual void OnClick() { }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Swap all three sprites and refresh the visual state.</summary>
    public void SetBTNSprite(Sprite defaultSprite, Sprite hoverSprite, Sprite clickedSprite)
    {
        S_Default = defaultSprite;
        S_Hover = hoverSprite;
        S_Clicked = clickedSprite;
        ApplyInteractableVisual();
    }

    /// <summary>
    /// Applies colour tint based on current Interactable state,
    /// mirroring how Unity's Button greys out when disabled.
    /// </summary>
    private void ApplyInteractableVisual()
    {
        if (_spriteRenderer == null) return;

        if (Interactable)
        {
            _spriteRenderer.sprite = S_Default;
            _spriteRenderer.color = Color.white;
        }
        else
        {
            _spriteRenderer.sprite = _disabledSprite != null ? _disabledSprite : S_Default;
            _spriteRenderer.color = _disabledColour;
            _isHovering = false;
        }
    }
}