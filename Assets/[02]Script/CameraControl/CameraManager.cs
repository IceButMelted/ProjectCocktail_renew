using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Yarn.Unity;
using Mono.Cecil.Cil;

/// <summary>
/// The one owner of the camera: which virtual camera is live, and whether the mouse may move the view.
///
/// Cameras are plain <see cref="CameraEntry"/> data (an id and a blend time) that this manager collects
/// when the scene loads. The rest of the game asks for a shot by what it is (<see cref="ShowBar"/>,
/// <see cref="ShowShaker"/>, <see cref="ShowGarnish"/>) and never spells a camera id, so the id table
/// lives here and nowhere else.
///
/// The .yarn files talk to the camera with <c>Switch_Camera</c> and <c>Cut_Camera</c> only, to direct a shot.
/// Whether the mouse may move the view is the game flow's call (FlowWiring), never Yarn's.
/// Those are static commands: an instance command is found by the NAME of the object
/// it sits on (<c>GameObject.Find</c>), which breaks the moment the object is renamed. The first argument
/// the scripts still write (<c>CamController</c>) is accepted and ignored.
/// </summary>
public class CameraManager : MonoBehaviour
{
    /// <summary>The manager of the loaded scene, or null. Only the Yarn commands below use it.</summary>
    public static CameraManager Current { get; private set; }

    [Header("Cinemachine")]
    [Tooltip("The CinemachineBrain attached to your Main Camera.")]
    [SerializeField] private CinemachineBrain brain;

    [Tooltip("Mouse-driven look around the live camera. Found on this object when left empty.")]
    [SerializeField] private CameraController _look;

    [Header("Blend Settings")]
    [Tooltip("Default blend duration (seconds) used when a camera's own blendTime is 0.")]
    [SerializeField] private float globalBlendTime = 1f;

    [Tooltip("Default blend style applied globally.")]
    [SerializeField] private CinemachineBlendDefinition.Styles globalBlendStyle =
        CinemachineBlendDefinition.Styles.EaseInOut;

    [Header("Shots the game flow asks for (CameraEntry ids)")]
    [SerializeField] private string _barShot = "MainCam";
    [SerializeField] private string _shakerShot = "PrepareCam";
    [SerializeField] private string _garnishShot = "GarnishCam";

    [Header("Customer inset (minigame picture)")]
    [Tooltip("Disabled camera that renders to the inset's RenderTexture. No Brain, no CameraEntry, no AudioListener.")]
    [SerializeField] private Camera _customerInsetCamera;
    [Tooltip("The panel (Panel_CharacterView) that holds the inset's RawImage. Hidden when there is no customer.")]
    [SerializeField] private GameObject _customerInsetPanel;
    [Tooltip("Where the inset camera sits relative to the customer. Tune by eye.")]
    [SerializeField] private Vector3 _customerInsetOffset = new Vector3(0f, 1.2f, 2f);

    [Header("Debug")]
    [SerializeField] private bool logTransitions = true;
    [SerializeField] private bool IsOnShakerView = false;

    private readonly Dictionary<string, CameraEntry> registry = new();
    private readonly Dictionary<string, CinemachineVirtualCameraBase> vcamRegistry = new();

    /// <summary>The ID of the currently active camera, or null if none.</summary>
    public string ActiveCameraId { get; private set; }

    // ── Unity ──────────────────────────────────────────────

    private void Awake()
    {
        Current = this;
        if (_look == null) _look = GetComponent<CameraController>();

        foreach (var entry in FindObjectsByType<CameraEntry>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Register(entry);
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private void Register(CameraEntry entry)
    {
        string id = entry.CameraId;

        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogError($"CameraManager: Cannot register camera on '{entry.gameObject.name}' — cameraId is empty.");
            return;
        }

        var vcam = entry.GetComponent<CinemachineVirtualCameraBase>();
        if (vcam == null)
        {
            Debug.LogError($"CameraManager: No CinemachineVirtualCameraBase found on '{entry.gameObject.name}'. " +
                           "Make sure the CameraEntry is on the same GameObject as your virtual camera.");
            return;
        }

        if (registry.ContainsKey(id))
            Debug.LogWarning($"CameraManager: A camera with id '{id}' is already registered. Overwriting.");

        registry[id] = entry;
        vcamRegistry[id] = vcam;

        // every camera starts disabled; the first switch enables one
        vcam.enabled = false;

        if (logTransitions)
            Debug.Log($"CameraManager: Registered camera '{id}' on GameObject '{entry.gameObject.name}'.");
    }

    // ── What the game flow asks for ────────────────────────

    /// <summary>The bar, over the shelves: the view while ingredients are picked.</summary>
    public void ShowBar() => SwitchCamera(_barShot);

    public void ToggleShakerView() {
        if (IsOnShakerView)
        {
            SwitchCamera(_barShot);
            IsOnShakerView = false;
        }
        else
        {
            SwitchCamera(_shakerShot);
            IsOnShakerView = true;
        }
    }

    /// <summary>The bar shot with no blend: the start of a day.</summary>
    public void CutToBar() => CutToCamera(_barShot);

    /// <summary>Close on the shaker: the view once the drink is full.</summary>
    public void ShowShaker() {
        SwitchCamera(_shakerShot);
        IsOnShakerView = true;
    }

    /// <summary>The garnish station.</summary>
    public void ShowGarnish() => SwitchCamera(_garnishShot);

    /// <summary>Points the inset camera at the customer. Null hides the inset picture.</summary>
    public void ShowCustomerInset(Transform customer)
    {
        if (_customerInsetPanel != null) _customerInsetPanel.SetActive(customer != null);
        if (_customerInsetCamera == null) return;

        _customerInsetCamera.enabled = customer != null;
        if (customer != null) _customerInsetCamera.transform.position = customer.position + _customerInsetOffset;
    }

    public void HideCustomerInset()
    {
        if (_customerInsetCamera != null) _customerInsetCamera.enabled = false;
    }

    /// <summary>
    /// While fixed, the mouse cannot move the view; fixing it also snaps the view back to forward.
    /// </summary>
    public void SetLookFixed(bool value)
    {
        if (_look != null) _look.IsFixedCamera = value;
    }

    /// <summary>Snaps the mouse-driven view back to forward without changing whether it is fixed.</summary>
    public void ResetLook()
    {
        if (_look != null) _look.ResetView();
    }

    // ── Switching ──────────────────────────────────────────

    /// <summary>
    /// Switches to the camera with the given ID, using that camera's own blendTime
    /// (or globalBlendTime if blendTime == 0).
    /// </summary>
    public void SwitchCamera(string id)
    {
        if (!TryGetRegistered(id, out var entry, out var vcam)) return;
        if (id == ActiveCameraId)
        {
            if (logTransitions)
                Debug.Log($"CameraManager: Camera '{id}' is already active.");
            return;
        }

        IsOnShakerView = false;

        float blend = entry.BlendTime > 0f ? entry.BlendTime : globalBlendTime;
        ApplyTransition(id, vcam, blend, globalBlendStyle);
    }

    /// <summary>Immediately cuts to the camera with the given ID (no blend).</summary>
    public void CutToCamera(string id)
    {
        if (!TryGetRegistered(id, out _, out var vcam)) return;

        ApplyTransition(id, vcam, 0f, CinemachineBlendDefinition.Styles.Cut);
    }

    private bool TryGetRegistered(string id,
                                   out CameraEntry entry,
                                   out CinemachineVirtualCameraBase vcam)
    {
        entry = null;
        vcam = null;

        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogError("CameraManager.SwitchCamera: id is null or empty.");
            return false;
        }

        if (!registry.TryGetValue(id, out entry) || !vcamRegistry.TryGetValue(id, out vcam))
        {
            Debug.LogError($"CameraManager.SwitchCamera: No camera registered with id '{id}'.");
            return false;
        }

        return true;
    }

    private void ApplyTransition(string id,
                                  CinemachineVirtualCameraBase targetVcam,
                                  float blendTime,
                                  CinemachineBlendDefinition.Styles style)
    {
        // The previous camera stays enabled (so the brain can blend out of it); it only loses priority.
        if (ActiveCameraId != null && vcamRegistry.TryGetValue(ActiveCameraId, out var previousVcam))
        {
            previousVcam.enabled = true;
            previousVcam.Priority = 10;
        }

        if (brain != null)
            brain.DefaultBlend = new CinemachineBlendDefinition(style, blendTime);
        else
            Debug.LogWarning("CameraManager: No CinemachineBrain assigned — blend settings cannot be applied.");

        targetVcam.enabled = true;
        targetVcam.Priority = 20;

        ActiveCameraId = id;

        if (logTransitions)
            Debug.Log($"CameraManager: Switched to '{id}' | blend: {blendTime}s | style: {style}.");
    }

    // ── Yarn ───────────────────────────────────────────────

    [YarnCommand("Switch_Camera")]
    public static void Yarn_SwitchCamera(string target, string id) => Resolve("Switch_Camera")?.SwitchCamera(id);

    [YarnCommand("Cut_Camera")]
    public static void Yarn_CutCamera(string target, string id) => Resolve("Cut_Camera")?.CutToCamera(id);

    private static CameraManager Resolve(string command)
    {
        if (Current == null)
            Debug.LogWarning($"[CameraManager] {command}: no CameraManager in the scene.");
        return Current;
    }

    // ── Editor ─────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (brain == null)
            Debug.LogWarning("CameraManager: No CinemachineBrain assigned.");

        if (globalBlendTime < 0f)
        {
            globalBlendTime = 0f;
            Debug.LogWarning("CameraManager: globalBlendTime cannot be negative. Reset to 0.");
        }
    }
#endif
}
