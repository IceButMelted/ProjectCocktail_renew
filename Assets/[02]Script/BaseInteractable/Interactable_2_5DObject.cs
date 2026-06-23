using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// EventSystem-driven 3D interactable that behaves like a UI Button.
///
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(SpriteRenderer))]
public class Interactable_2_5DObject : PointerInteractableBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Sprites")]
    [SerializeField] protected Sprite S_Default;
    [SerializeField] protected Sprite S_Hover;
    [SerializeField] protected Sprite S_Clicked;

    [Tooltip("Sprite shown when Interactable is false. Falls back to S_Default + disabled colour.")]
    [SerializeField] private Sprite _disabledSprite;

    [Tooltip("Colour tint when Interactable is false (mirrors Unity Button behaviour).")]
    [SerializeField] private Color _disabledColour = new Color(1f, 1f, 1f, 0.5f);

    [Header("Drag Detection")]
    [Tooltip("Pixels the pointer must travel before the gesture is classified as a drag.")]
    [SerializeField] private float _dragThreshold = 10f;

    [Space]
    public UnityEvent OnClicked;

    // ── Private State ─────────────────────────────────────────────────────────

    private SpriteRenderer _spriteRenderer;
    private DragableObject _dragableObject;        // optional sibling
    private bool _isHovering;
    private bool _isDragging;
    private Vector2 _pointerDownScreenPos;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _dragableObject = GetComponent<DragableObject>();
        _spriteRenderer.sprite = S_Default;
        ApplyInteractableVisual();
    }

    // ── PointerInteractableBase Override ─────────────────────────────────────

    protected override void OnInteractableChanged(bool interactable) =>
        ApplyInteractableVisual();

    // ── Pointer Handlers ──────────────────────────────────────────────────────

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (_isDragging || !Interactable) return;
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
    /// Override to add extra guard conditions (cooldowns, resource checks, etc.).
    /// Base always returns <see cref="PointerInteractableBase.Interactable"/>.
    /// </summary>
    protected virtual bool CanClick() => Interactable;

    /// <summary>
    /// Override to run custom logic after a confirmed click.
    /// <see cref="OnClicked"/> has already fired when this is called.
    /// </summary>
    protected virtual void OnClick() { }

    // ── Public Helpers ────────────────────────────────────────────────────────

    /// <summary>Swap all three sprites and refresh the visual state.</summary>
    public void SetBTNSprite(Sprite defaultSprite, Sprite hoverSprite, Sprite clickedSprite)
    {
        S_Default = defaultSprite;
        S_Hover = hoverSprite;
        S_Clicked = clickedSprite;
        ApplyInteractableVisual();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Applies sprite and colour tint based on current Interactable state,
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