using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class BTN_2_5D : MonoBehaviour
{
    private Material m_Material;
    private MeshRenderer meshRenderer;
    private Camera mainCam;

    [Header("Textures")]
    [SerializeField] private Texture2D T_Default;
    [SerializeField] private Texture2D T_Hover;
    [SerializeField] private Texture2D T_Clicked;

    

    [Header("Input Actions")]
    [SerializeField] private InputActionReference pointAction;
    [SerializeField] private InputActionReference clickAction;

    [Header("ByPass Layer")]
    [SerializeField] private LayerMask boxplacementLayerMark;


    public UnityEvent OnClicked;

    private bool isHovering;

    // Shader property IDs
    private static readonly int EmissionStrengthID = Shader.PropertyToID("_EmssionStrength");
    private static readonly int CurrentTextureID = Shader.PropertyToID("_CurrentTexture");

    private void Awake()
    {
        pointAction = FindFirstObjectByType<InputSystemManager>().PointActionRef;
        clickAction = FindFirstObjectByType<InputSystemManager>().TapActionRef;

        meshRenderer = GetComponent<MeshRenderer>();
        m_Material = meshRenderer.material;
        mainCam = Camera.main;

        SetMaterial(0f, T_Default);
    }

    private void OnEnable()
    {
        pointAction.action.Enable();
        clickAction.action.Enable();

        clickAction.action.performed += OnClick;
    }

    private void OnDisable()
    {
        clickAction.action.performed -= OnClick;

        pointAction.action.Disable();
        clickAction.action.Disable();
    }

    private void Start()
    {
        clickAction.action.started += Context =>
        {
            Debug.Log($"{name} clicked (New Input System - started)");
        };

        clickAction.action.performed += Context =>
        {
            Debug.Log($"{name} clicked (New Input System - performed)");
        };

        clickAction.action.canceled += Context => {
            Debug.Log($"{name} clicked (New Input System - cancled)");
        };
    }

    private void Update()
    {
        CheckHover();
    }

    private void CheckHover()
    {
        Vector2 mousePos = pointAction.action.ReadValue<Vector2>();
        Ray ray = mainCam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~boxplacementLayerMark))
        {
            if (hit.collider.gameObject == gameObject)
            {
                if (!isHovering)
                {
                    isHovering = true;
                    SetMaterial(0.25f, T_Hover);
                    Debug.Log($"{name} - Hover Enter");
                }
                return;
            }
        }

        if (isHovering)
        {
            isHovering = false;
            SetMaterial(0f, T_Default);
            Debug.Log($"{name} - Hover Exit");
        }
    }



    private void OnClick(InputAction.CallbackContext context)
    {
        if (!isHovering) return;

        Debug.Log($"{name} clicked (New Input System)");
        OnClicked?.Invoke();

        SetMaterial(0.125f, T_Clicked);
        Invoke(nameof(ResetVisual), 0.1f);
    }

    private void ResetVisual()
    {
        SetMaterial(isHovering ? 0.25f : 0f, isHovering ? T_Hover : T_Default);
    }

    private void SetMaterial(float emission, Texture2D tex)
    {
        m_Material.SetFloat(EmissionStrengthID, emission);
        m_Material.SetTexture(CurrentTextureID, tex);
    }

    private void OnDestroy()
    {
        if (m_Material != null)
            Destroy(m_Material);
    }
}
