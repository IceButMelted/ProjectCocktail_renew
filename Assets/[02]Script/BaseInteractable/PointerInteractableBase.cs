using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shared base for all pointer-driven 3-D interactables.
///
/// Owns:
///   • Interactable property (inspector toggle + public get/set)
///   • All IPointer interface stubs — override whichever you need
///   • OnInteractableChanged() hook for subclass visual / audio reactions
///
/// Subclasses: Interactable3DObject, ScaleOnHover, UIPointerSound
/// </summary>
public abstract class PointerInteractableBase : MonoBehaviour,
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
    /// Mirrors Unity's Button.interactable: togglable in the Inspector
    /// or at runtime. Calls <see cref="OnInteractableChanged"/> on every change.
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
    public virtual void OnPointerExit (PointerEventData eventData) { }
    public virtual void OnPointerDown (PointerEventData eventData) { }
    public virtual void OnPointerUp   (PointerEventData eventData) { }
    public virtual void OnDrag        (PointerEventData eventData) { }
}
