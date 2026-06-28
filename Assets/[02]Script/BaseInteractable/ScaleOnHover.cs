using UnityEngine;
using UnityEngine.EventSystems;

public class ScaleOnHover : PointerInteractableBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [SerializeField] private float newScale = 1.1f;
    [SerializeField] private float scaleDuration = 0.2f;

    // ── Private State ─────────────────────────────────────────────────────────

    private Vector3 _initScale;
    private Vector3 _targetScale;
    private bool _isScaling;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _initScale = transform.localScale;
        _targetScale = _initScale;

        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"ScaleOnHover on '{gameObject.name}' requires a Collider. Adding BoxCollider...");
            gameObject.AddComponent<BoxCollider>();
        }
    }

    private void Update()
    {
        if (!_isScaling) return;

        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime / scaleDuration);

        if (Vector3.Distance(transform.localScale, _targetScale) < 0.001f)
        {
            transform.localScale = _targetScale;
            _isScaling = false;
        }
    }

    // ── PointerInteractableBase Overrides ─────────────────────────────────────

    /// <summary>Snap back to normal scale when disabled mid-hover.</summary>
    protected override void OnInteractableChanged(bool interactable)
    {
        if (!interactable)
            ChangeScale(false);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!Interactable) return;
        if (DragableObject.IsAnyDragging) return;
        ChangeScale(true);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (DragableObject.IsAnyDragging) return;
        ChangeScale(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ChangeScale(bool enlarge)
    {
        _targetScale = enlarge ? _initScale * newScale : _initScale;
        _isScaling = true;
    }
}