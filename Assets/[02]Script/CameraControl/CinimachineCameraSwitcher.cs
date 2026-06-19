using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Yarn.Unity;

/// <summary>
/// Singleton controller for switching between registered Cinemachine virtual cameras by ID.
/// 
/// Setup:
///   1. Add this component to any persistent GameObject (e.g. "CameraManager").
///   2. Assign your CinemachineBrain (usually on the Main Camera) to the 'brain' field.
///   3. Set globalBlendTime as the fallback blend duration.
///   4. Add a CameraEntry component to each virtual camera and give it a unique cameraId.
///   5. Call CinimachineCameraSwitcher.Instance.SwitchCamera("YourCameraId") from anywhere.
/// </summary>
public class CinimachineCameraSwitcher : MonoBehaviour
{
    #region Singleton

    public static CinimachineCameraSwitcher Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("CinimachineCameraSwitcher: Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region Serialized Fields

    [Header("Cinemachine")]
    [Tooltip("The CinemachineBrain attached to your Main Camera.")]
    [SerializeField] private CinemachineBrain brain;

    [Header("Blend Settings")]
    [Tooltip("Default blend duration (seconds) used when a camera's own blendTime is 0.")]
    [SerializeField] private float globalBlendTime = 1f;

    [Tooltip("Default blend style applied globally.")]
    [SerializeField] private CinemachineBlendDefinition.Styles globalBlendStyle =
        CinemachineBlendDefinition.Styles.EaseInOut;

    [Header("Debug")]
    [SerializeField] private bool logTransitions = true;

    #endregion

    #region Private Fields

    // cameraId → CameraEntry
    private readonly Dictionary<string, CameraEntry> registry = new();

    // cameraId → virtual camera component
    private readonly Dictionary<string, CinemachineVirtualCameraBase> vcamRegistry = new();

    private string activeCameraId = null;

    #endregion

    #region Properties

    /// <summary>The ID of the currently active camera, or null if none.</summary>
    public string ActiveCameraId => activeCameraId;

    /// <summary>Read-only list of all registered camera IDs.</summary>
    public IReadOnlyCollection<string> RegisteredIds => registry.Keys;

    #endregion

    #region Registration

    /// <summary>
    /// Called automatically by CameraEntry.Awake(). You can also call this manually.
    /// </summary>
    public void RegisterCamera(CameraEntry entry)
    {
        if (entry == null)
        {
            Debug.LogError("CameraController2.RegisterCamera: entry is null.");
            return;
        }

        string id = entry.CameraId;

        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogError($"CinimachineCameraSwitcher: Cannot register camera on '{entry.gameObject.name}' — cameraId is empty.");
            return;
        }

        if (registry.ContainsKey(id))
        {
            Debug.LogWarning($"CinimachineCameraSwitcher: A camera with id '{id}' is already registered. Overwriting.");
        }

        // Resolve the virtual camera component on the same GameObject
        var vcam = entry.GetComponent<CinemachineVirtualCameraBase>();
        if (vcam == null)
        {
            Debug.LogError($"CinimachineCameraSwitcher: No CinemachineVirtualCameraBase found on '{entry.gameObject.name}'. " +
                           "Make sure the CameraEntry is on the same GameObject as your virtual camera.");
            return;
        }

        registry[id] = entry;
        vcamRegistry[id] = vcam;

        // All cameras start disabled except the active one
        vcam.enabled = (id == activeCameraId);

        Debug.Log($"CinimachineCameraSwitcher: Registered camera '{id}' on GameObject '{entry.gameObject.name}'.");

        if (logTransitions)
            Debug.Log($"CinimachineCameraSwitcher: Registered camera '{id}'.");
    }

    /// <summary>
    /// Removes a camera from the registry. Called automatically by CameraEntry.OnDestroy().
    /// </summary>
    public void UnregisterCamera(string id)
    {
        if (registry.Remove(id))
        {
            vcamRegistry.Remove(id);
            if (logTransitions)
                Debug.Log($"CinimachineCameraSwitcher: Unregistered camera '{id}'.");
        }
    }

    #endregion

    #region Switching

    /// <summary>
    /// Switches to the camera with the given ID, using that camera's own blendTime
    /// (or globalBlendTime if blendTime == 0).
    /// </summary>
    /// <param name="id">The cameraId set on the target CameraEntry.</param>
    [YarnCommand("Switch_Camera")]
    public void SwitchCamera(string id)
    {
        if (!TryGetRegistered(id, out var entry, out var vcam)) return;
        if (id == activeCameraId)
        {
            if (logTransitions)
                Debug.Log($"CinimachineCameraSwitcher: Camera '{id}' is already active.");
            return;
        }

        float blend = entry.BlendTime > 0f ? entry.BlendTime : globalBlendTime;
        ApplyTransition(id, vcam, blend, globalBlendStyle);
    }

    /// <summary>
    /// Switches to the camera with the given ID using a fully custom blend definition.
    /// </summary>
    public void SwitchCamera(string id, float customBlendTime, CinemachineBlendDefinition.Styles style)
    {
        if (!TryGetRegistered(id, out _, out var vcam)) return;

        ApplyTransition(id, vcam, customBlendTime, style);
    }

    /// <summary>
    /// Immediately cuts to the camera with the given ID (no blend).
    /// </summary>
    [YarnCommand("Cut_Camera")]
    public void CutToCamera(string id)
    {
        SwitchCamera(id, 0f, CinemachineBlendDefinition.Styles.Cut);
    }

    #endregion

    #region Internal

    private bool TryGetRegistered(string id,
                                   out CameraEntry entry,
                                   out CinemachineVirtualCameraBase vcam)
    {
        entry = null;
        vcam = null;

        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogError("CinimachineCameraSwitcher.SwitchCamera: id is null or empty.");
            return false;
        }

        if (!registry.TryGetValue(id, out entry) || !vcamRegistry.TryGetValue(id, out vcam))
        {
            Debug.LogError($"CinimachineCameraSwitcher.SwitchCamera: No camera registered with id '{id}'.");
            return false;
        }

        return true;
    }

    private void ApplyTransition(string id,
                                  CinemachineVirtualCameraBase targetVcam,
                                  float blendTime,
                                  CinemachineBlendDefinition.Styles style)
    {
        // Disable the previously active camera
        if (activeCameraId != null && vcamRegistry.TryGetValue(activeCameraId, out var previousVcam))
        {
            previousVcam.enabled = true;
        }

        // Apply the blend override on the brain
        if (brain != null)
        {
            brain.DefaultBlend = new CinemachineBlendDefinition(style, blendTime);
        }
        else
        {
            Debug.LogWarning("CinimachineCameraSwitcher: No CinemachineBrain assigned — blend settings cannot be applied.");
        }

        // Enable and prioritise the target camera
        targetVcam.enabled = true;
        targetVcam.Priority = 20;

        // Lower priority of the old camera so Cinemachine picks the new one cleanly
        if (activeCameraId != null && vcamRegistry.TryGetValue(activeCameraId, out var prev))
            prev.Priority = 10;

        activeCameraId = id;

        if (logTransitions)
            Debug.Log($"CinimachineCameraSwitcher: Switched to '{id}' | blend: {blendTime}s | style: {style}.");
    }

    #endregion

    #region Editor Helpers

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (brain == null)
            Debug.LogWarning("CameraController2: No CinemachineBrain assigned.");

        if (globalBlendTime < 0f)
        {
            globalBlendTime = 0f;
            Debug.LogWarning("CameraController2: globalBlendTime cannot be negative. Reset to 0.");
        }
    }
#endif

    #endregion
}
