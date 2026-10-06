using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// A 2.5D mesh button that responds to hover and click via raycasting.
/// Extend this class for specialised buttons (e.g. CocktailShaker).
/// </summary>
public class BTN_2_5D : MonoBehaviour
{
    // ── Shader IDs ────────────────────────────────────────
    private static readonly int EmissionID = Shader.PropertyToID("_EmssionStrength");
    private static readonly int TextureID  = Shader.PropertyToID("_CurrentTexture");

    // ── Inspector ─────────────────────────────────────────
    [Header("Textures")]
    [SerializeField] protected Texture2D T_Default;
    [SerializeField] protected Texture2D T_Hover;
    [SerializeField] protected Texture2D T_Clicked;

    [Header("Input")]
    [SerializeField] private InputActionReference _pointAction;
    [SerializeField] private InputActionReference _clickAction;

    [Header("Layer Mask")]
    [Tooltip("Layers this button is NOT blocked by.")]
    [SerializeField] private LayerMask _bypassLayerMask;

    [Space]
    public UnityEvent OnClicked;

    // ── Private State ─────────────────────────────────────
    private Material    _material;
    private MeshRenderer _meshRenderer;
    private Camera      _mainCam;
    private bool        _isHovering;

    // ── Unity ─────────────────────────────────────────────
    protected virtual void Awake()
    {
        var inputManager = FindFirstObjectByType<InputSystemManager>();
        _pointAction = inputManager.PointActionRef;
        _clickAction = inputManager.TapActionRef;

        _meshRenderer = GetComponent<MeshRenderer>();
        _material     = _meshRenderer.material;
        _mainCam      = Camera.main;

        ApplyMaterial(0f, T_Default);
    }

    private void OnEnable()
    {
        _pointAction.action.Enable();
        _clickAction.action.Enable();
        _clickAction.action.performed += OnClick;
    }

    private void OnDisable()
    {
        _clickAction.action.performed -= OnClick;
        _pointAction.action.Disable();
        _clickAction.action.Disable();
    }

    protected virtual void Update() => CheckHover();

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }

    // ── Hover ─────────────────────────────────────────────
    private void CheckHover()
    {
        Vector2 screenPos = _pointAction.action.ReadValue<Vector2>();
        Ray ray = _mainCam.ScreenPointToRay(screenPos);

        bool hittingThis = Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~_bypassLayerMask)
                           && hit.collider.gameObject == gameObject;

        if (hittingThis && !_isHovering)
        {
            _isHovering = true;
            ApplyMaterial(0.25f, T_Hover);
        }
        else if (!hittingThis && _isHovering)
        {
            _isHovering = false;
            ApplyMaterial(0f, T_Default);
        }
    }

    // ── Click ─────────────────────────────────────────────
    protected virtual void OnClick(InputAction.CallbackContext context)
    {
        if (!_isHovering) return;

        OnClicked?.Invoke();
        ApplyMaterial(0.125f, T_Clicked);
        Invoke(nameof(ResetVisual), 0.1f);
    }

    private void ResetVisual()
        => ApplyMaterial(_isHovering ? 0.25f : 0f, _isHovering ? T_Hover : T_Default);

    // ── Helpers ───────────────────────────────────────────
    private void ApplyMaterial(float emission, Texture2D tex)
    {
        _material.SetFloat(EmissionID, emission);
        _material.SetTexture(TextureID, tex);
    }

    /// <summary>Swap all three textures and reset to default state.</summary>
    public void SetBTNSprite(Texture2D defaultTex, Texture2D hoverTex, Texture2D clickedTex)
    {
        T_Default = defaultTex;
        T_Hover   = hoverTex;
        T_Clicked = clickedTex;
        ApplyMaterial(0f, T_Default);
    }
}
