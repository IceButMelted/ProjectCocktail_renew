using UnityEngine;

/// <summary>
/// Attach this to any GameObject that holds a CinemachineVirtualCamera (or FreeLook, etc.).
/// It is plain data: CameraManager collects every CameraEntry in the scene when it loads, so cameras
/// can be switched by ID.
///
/// Setup:
///   1. Add this component to your virtual camera GameObject.
///   2. Fill in a unique cameraId string (e.g. "PlayerCam", "CutsceneCam").
///   3. Set blendTime to override the global blend for THIS camera (0 = use global default).
/// </summary>
public class CameraEntry : MonoBehaviour
{
    #region Serialized Fields

    [Tooltip("Unique name used to reference this camera. Must be unique across all registered cameras.")]
    [SerializeField] private string cameraId = "MyCameraId";

    [Tooltip("Blend duration (seconds) when transitioning TO this camera. " +
             "Set to 0 to use the CameraManager's global blend time.")]
    [SerializeField] private float blendTime = 0f;

    #endregion

    #region Properties

    /// <summary>Unique identifier for this camera.</summary>
    public string CameraId => cameraId;

    /// <summary>Blend time when transitioning TO this camera. 0 = use global default.</summary>
    public float BlendTime => blendTime;

    #endregion

    #region Editor Helpers

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(cameraId))
            Debug.LogWarning($"CameraEntry on '{gameObject.name}' has an empty cameraId.");

        if (blendTime < 0f)
        {
            blendTime = 0f;
            Debug.LogWarning($"CameraEntry '{cameraId}': blendTime cannot be negative. Reset to 0.");
        }
    }
#endif

    #endregion

}
