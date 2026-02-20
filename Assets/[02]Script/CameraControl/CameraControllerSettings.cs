using UnityEngine;

/// <summary>
/// ScriptableObject asset that holds all configurable settings for CameraController.
/// Create via: Assets > Create > Camera > Camera Controller Settings
/// </summary>
[CreateAssetMenu(fileName = "CameraControllerSettings", menuName = "Camera/Camera Controller Settings", order = 0)]
public class CameraControllerSettings : ScriptableObject
{
    [Header("Feature Toggles")]
    [Tooltip("Enable left/right camera rotation when hovering at screen edges")]
    public bool canRotateSideways = false;

    [Tooltip("Enable camera position movement (vertical) when looking down")]
    public bool canMoveCamera = false;

    [Header("Edge Detection Thresholds (%)")]
    [Tooltip("Distance from screen edge (%) to trigger side view")]
    [Range(5, 70)] public float sideViewTriggerThreshold = 30f;

    [Tooltip("Distance from screen edge (%) to return from side view")]
    [Range(5, 70)] public float sideViewReturnThreshold = 40f;

    [Tooltip("Distance from bottom (%) to trigger down view")]
    [Range(5, 50)] public float downViewTriggerThreshold = 20f;

    [Tooltip("Distance from bottom (%) to return from down view")]
    [Range(5, 80)] public float downViewReturnThreshold = 30f;

    [Header("Camera Rotation Angles")]
    public Vector3 forwardAngle = new Vector3(0, 0, 0);
    public Vector3 leftSideAngle = new Vector3(0, -90, 0);
    public Vector3 rightSideAngle = new Vector3(0, 90, 0);
    public Vector3 downAngle = new Vector3(45, 0, 0);

    [Header("Transition Settings")]
    [Tooltip("Time to complete rotation transition")]
    [Range(0.1f, 2f)] public float rotationDuration = 0.5f;

    [Tooltip("Time mouse must hover before triggering transition")]
    [Range(0.1f, 2f)] public float hoverDelayDuration = 0.6f;

    [Tooltip("Time to complete camera position movement")]
    [Range(0.1f, 2f)] public float movementDuration = 0.6f;

    [Header("Camera Translation")]
    [Tooltip("Distance to move camera down when looking down")]
    public float moveDownDistance = 1f;

    private void OnValidate()
    {
        if (sideViewReturnThreshold <= sideViewTriggerThreshold)
            Debug.LogWarning($"[{name}] Side return threshold should be greater than trigger threshold to prevent flickering!");

        if (downViewReturnThreshold <= downViewTriggerThreshold)
            Debug.LogWarning($"[{name}] Down return threshold should be greater than trigger threshold to prevent flickering!");
    }
}