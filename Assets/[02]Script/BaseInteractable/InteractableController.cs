using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Central controller for every <see cref="IPointerInteractable"/> component
/// that lives on the same GameObject (or optionally its children).
///
/// SOLID notes:
///   S — single job: orchestrate interactable state and forward pointer events.
///   O — new interactable types plug in automatically via the interface; no
///       changes needed here.
///   L — depends on IPointerInteractable, not any concrete subclass.
///   I — exposes only the operations callers actually need.
///   D — all dependencies injected through the interface; no hard coupling to
///       Interactable_2_5DObject / DragableObject / UIPointerSound.
///
/// Usage
/// ─────
/// 1. Add this component to the root of an interactable GameObject.
/// 2. All sibling PointerInteractableBase scripts are auto-discovered on Awake.
/// 3. Toggle <see cref="Interactable"/> here to enable/disable every sibling.
/// 4. Call <see cref="SimulatePointerEnter"/> / <see cref="SimulatePointerExit"/>
///    etc. to fire events programmatically on every registered component.
/// 5. Subscribe to <see cref="OnAnyHoverEnter"/> / <see cref="OnAnyHoverExit"/>
///    / <see cref="OnAnyPointerDown"/> / <see cref="OnAnyPointerUp"/> from code.
/// </summary>
[DisallowMultipleComponent]
public class InteractableController : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Interaction")]
    [SerializeField] private bool _interactable = true;

    [Tooltip("When true, components on child GameObjects are also discovered.")]
    [SerializeField] private bool _includeChildren = false;

    [Header("Debug")]
    [SerializeField] private bool _debugLog = false;

    // ── Events (subscribe from code or wire up in Inspector via wrapper) ───────

    public System.Action<PointerEventData> OnAnyHoverEnter;
    public System.Action<PointerEventData> OnAnyHoverExit;
    public System.Action<PointerEventData> OnAnyPointerDown;
    public System.Action<PointerEventData> OnAnyPointerUp;
    public System.Action<PointerEventData> OnAnyDrag;

    // ── Private ───────────────────────────────────────────────────────────────

    private readonly List<IPointerInteractable> _interactables = new();

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake() => DiscoverInteractables();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Master interactable toggle — propagated to every discovered component.
    /// </summary>
    public bool Interactable
    {
        get => _interactable;
        set
        {
            if (_interactable == value) return;
            _interactable = value;
            ApplyInteractableToAll(_interactable);
            Log($"Interactable set to {_interactable}");
        }
    }

    /// <summary>Returns how many components are currently controlled.</summary>
    public int ControlledCount => _interactables.Count;

    /// <summary>
    /// Re-scans the GameObject (and optionally children) for
    /// IPointerInteractable components and syncs their state.
    /// Call this if you add components at runtime.
    /// </summary>
    public void DiscoverInteractables()
    {
        _interactables.Clear();

        IPointerInteractable[] found = _includeChildren
            ? GetComponentsInChildren<IPointerInteractable>()
            : GetComponents<IPointerInteractable>();

        foreach (IPointerInteractable item in found)
        {
            // Don't add ourselves if this MonoBehaviour also implements the interface
            if (item is InteractableController) continue;
            _interactables.Add(item);
        }

        ApplyInteractableToAll(_interactable);
        Log($"Discovered {_interactables.Count} interactable(s).");
    }

    // ── Programmatic Event Simulation ────────────────────────────────────────

    /// <summary>Fires OnPointerEnter on every registered component.</summary>
    public void SimulatePointerEnter(PointerEventData eventData = null)
    {
        eventData ??= new PointerEventData(EventSystem.current);
        ForEach(i => i.OnPointerEnter(eventData));
        OnAnyHoverEnter?.Invoke(eventData);
    }

    /// <summary>Fires OnPointerExit on every registered component.</summary>
    public void SimulatePointerExit(PointerEventData eventData = null)
    {
        eventData ??= new PointerEventData(EventSystem.current);
        ForEach(i => i.OnPointerExit(eventData));
        OnAnyHoverExit?.Invoke(eventData);
    }

    /// <summary>Fires OnPointerDown on every registered component.</summary>
    public void SimulatePointerDown(PointerEventData eventData = null)
    {
        eventData ??= new PointerEventData(EventSystem.current);
        ForEach(i => i.OnPointerDown(eventData));
        OnAnyPointerDown?.Invoke(eventData);
    }

    /// <summary>Fires OnPointerUp on every registered component.</summary>
    public void SimulatePointerUp(PointerEventData eventData = null)
    {
        eventData ??= new PointerEventData(EventSystem.current);
        ForEach(i => i.OnPointerUp(eventData));
        OnAnyPointerUp?.Invoke(eventData);
    }

    /// <summary>Fires OnDrag on every registered component.</summary>
    public void SimulateDrag(PointerEventData eventData = null)
    {
        eventData ??= new PointerEventData(EventSystem.current);
        ForEach(i => i.OnDrag(eventData));
        OnAnyDrag?.Invoke(eventData);
    }

    // ── IPointer Handlers (forwarded from EventSystem) ────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        Log("OnPointerEnter");
        OnAnyHoverEnter?.Invoke(eventData);
        // Components handle their own pointer events via their own handlers;
        // this controller's handlers exist purely for the aggregate C# events.
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Log("OnPointerExit");
        OnAnyHoverExit?.Invoke(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Log("OnPointerDown");
        OnAnyPointerDown?.Invoke(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Log("OnPointerUp");
        OnAnyPointerUp?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnAnyDrag?.Invoke(eventData);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ApplyInteractableToAll(bool state)
    {
        foreach (IPointerInteractable item in _interactables)
            item.Interactable = state;
    }

    private void ForEach(System.Action<IPointerInteractable> action)
    {
        foreach (IPointerInteractable item in _interactables)
            action(item);
    }

    private void Log(string message)
    {
        if (_debugLog)
            Debug.Log($"[InteractableController] {name} — {message}");
    }
}
