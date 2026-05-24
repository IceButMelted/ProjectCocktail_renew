using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shared base for all pointer-driven 3-D interactables.
///
/// SOLID notes:
///   S — owns only the Interactable toggle and pointer-event contract.
///   O — subclasses extend via virtual methods; no modification needed here.
///   L — any subclass can replace this without breaking callers.
///   I — IPointerInteractable is the thin interface outsiders depend on.
///   D — InteractableController depends on IPointerInteractable, not this concrete type.
///
/// Subclasses: Interactable3DObject, DragableObject, UIPointerSound
/// </summary>
public interface IPointerInteractable
{
    bool Interactable { get; set; }

    // Hover
    void OnPointerEnter(PointerEventData eventData);
    void OnPointerExit(PointerEventData eventData);

    // Press
    void OnPointerDown(PointerEventData eventData);
    void OnPointerUp(PointerEventData eventData);

    // Drag
    void OnDrag(PointerEventData eventData);
}

public abstract class PointerInteractableBase : MonoBehaviour,
    IPointerInteractable,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    // ── Shared Inspector Field ────────────────────────────────────────────────

    [Header("Interaction")]
    [SerializeField] private bool _interactable = true;

    // ── Public Property ───────────────────────────────────────────────────────

    /// <summary>
    /// Whether this object responds to pointer events.
    /// Togglable in the Inspector or at runtime.
    /// Calls <see cref="OnInteractableChanged"/> on every change.
    /// </summary>
    public bool Interactable
    {
        get => _interactable;
        set
        {
            if (_interactable == value) return;
            _interactable = value;
            OnInteractableChanged(_interactable);
        }
    }

    // ── Virtual Lifecycle Hooks ───────────────────────────────────────────────

    /// <summary>
    /// Called whenever <see cref="Interactable"/> flips.
    /// Override to update visuals, audio flags, or other state.
    /// </summary>
    protected virtual void OnInteractableChanged(bool interactable) { }

    // ── IPointer Stubs (all virtual — override only what you need) ────────────

    public virtual void OnPointerEnter(PointerEventData eventData) { }
    public virtual void OnPointerExit(PointerEventData eventData) { }
    public virtual void OnPointerDown(PointerEventData eventData) { }
    public virtual void OnPointerUp(PointerEventData eventData) { }
    public virtual void OnDrag(PointerEventData eventData) { }
}