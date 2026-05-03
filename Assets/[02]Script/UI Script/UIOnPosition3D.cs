using UnityEngine;

/// <summary>
/// Attach to a UI RectTransform (Screen Space Overlay or Camera canvas).
/// Positions the UI element over a 3D world-space reference point with an adjustable pixel offset.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIOnPosition3D : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The 3D transform to follow. Assign at runtime if spawned dynamically.")]
    [SerializeField] private Transform _target;

    [Header("Offset")]
    [Tooltip("Pixel offset applied after the world-to-screen projection.")]
    [SerializeField] private Vector2 _screenOffset = Vector2.zero;

    [Header("Settings")]
    [Tooltip("Hide this UI element when the target is behind the camera.")]
    [SerializeField] private bool _hideWhenBehindCamera = true;

    // ── Public API ────────────────────────────────────────
    public Transform Target { get => _target; set => _target = value; }
    public Vector2 ScreenOffset { get => _screenOffset; set => _screenOffset = value; }

    // ── Protected (available to subclasses) ───────────────
    protected RectTransform RectTr;
    protected Canvas UICanvas;
    protected Camera Cam;

    // Cached to avoid repeated GetComponent + renderMode checks every frame
    private RectTransform _canvasRectTr;
    private Camera _overlayCamera; // null = Overlay, worldCamera = Camera mode

    // ── Unity ─────────────────────────────────────────────
    protected virtual void Awake()
    {
        RectTr = GetComponent<RectTransform>();
        UICanvas = GetComponentInParent<Canvas>();
        Cam = Camera.main;

        if (UICanvas != null)
        {
            _canvasRectTr = UICanvas.GetComponent<RectTransform>();
            _overlayCamera = UICanvas.renderMode == RenderMode.ScreenSpaceOverlay
                           ? null
                           : UICanvas.worldCamera;
        }
    }

    protected virtual void LateUpdate()
    {
        if (_target == null || UICanvas == null) return;

        Vector3 screenPoint = Cam.WorldToScreenPoint(_target.position);

        if (_hideWhenBehindCamera)
        {
            bool isBehind = screenPoint.z < 0f;
            gameObject.SetActive(!isBehind);
            if (isBehind) return;
        }

        RectTr.anchoredPosition = ScreenToCanvasPosition(screenPoint) + _screenOffset;
    }

    // ── Helpers ───────────────────────────────────────────

    /// <summary>Converts a screen-space point to canvas local anchored position.</summary>
    protected Vector2 ScreenToCanvasPosition(Vector3 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRectTr, screenPoint, _overlayCamera, out Vector2 local);
        return local;
    }

    /// <summary>
    /// Returns true if the given screen point + UI size stays within screen bounds.
    /// </summary>
    protected bool IsOnScreen(Vector2 screenPoint, Vector2 halfSize)
    {
        return screenPoint.x >= halfSize.x &&
               screenPoint.x <= Screen.width - halfSize.x &&
               screenPoint.y >= halfSize.y &&
               screenPoint.y <= Screen.height - halfSize.y;
    }
}