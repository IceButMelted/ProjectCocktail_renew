using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// EventSystem-driven 3D interactable that behaves like a UI Button:
///   - Interactable toggle (inspector tick + public get/set)
///   - Disabled colour tint when non-interactable
///   - Hover / click sprite swap
///   - Drag suppression (own threshold + optional DragableObject sibling)
///
/// Extend this class and override CanClick() / OnClick() for custom behaviour.
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

    [Header("Interaction")]
    [SerializeField] private bool  _interactable  = true;
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

    // ── Interactable property (public get + set) ──────────────────────────────

    /// <summary>
    /// Whether this object responds to pointer events — identical to UI Button.interactable.
    /// Tick in the Inspector or set at runtime. Automatically updates the visual tint.
    /// </summary>
    public bool Interactable
    {
        get => _interactable;
        set
        {
            if (_interactable == value) return;
            _interactable = value;
            ApplyInteractableVisual();
        }
    }

    // ── Private State ─────────────────────────────────────────────────────────

    private SpriteRenderer _spriteRenderer;
    private bool           _isHovering;
    private bool           _isDragging;
    private Vector2        _pointerDownScreenPos;
    private DragableObject _dragableObject; // optional sibling — may be null

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        _spriteRenderer        = GetComponent<SpriteRenderer>();
        _dragableObject        = GetComponent<DragableObject>();
        _spriteRenderer.sprite = S_Default;
        ApplyInteractableVisual();
    }

    // ── Pointer Handlers ──────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_interactable) return;
        _isHovering            = true;
        _spriteRenderer.sprite = S_Hover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering            = false;
        if (!_interactable) return;
        _spriteRenderer.sprite = S_Default;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_interactable) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        _isDragging           = false;
        _pointerDownScreenPos = eventData.position;
        _spriteRenderer.sprite = S_Clicked;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging) return;
        if (Vector2.Distance(eventData.position, _pointerDownScreenPos) >= _dragThreshold)
            _isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_interactable) return;
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
    /// Return false to block the click. Base always returns <see cref="Interactable"/>.
    /// </summary>
    protected virtual bool CanClick() => _interactable;

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
        S_Hover   = hoverSprite;
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

        if (_interactable)
        {
            _spriteRenderer.sprite = S_Default;
            _spriteRenderer.color  = Color.white;
        }
        else
        {
            // Disabled sprite takes priority; colour-tinted S_Default is the fallback
            _spriteRenderer.sprite = _disabledSprite != null ? _disabledSprite : S_Default;
            _spriteRenderer.color  = _disabledColour;
            _isHovering            = false;
        }
    }
}
