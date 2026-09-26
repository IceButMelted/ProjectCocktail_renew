using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to the Idle-state virtual camera. Continuously tilts it toward the
/// mouse position for a parallax look. Independent of CameraController.cs —
/// that script drives discrete edge-hover states, this one is a continuous follow.
/// </summary>
public class MenuParallaxLook : MonoBehaviour
{
    [SerializeField] private float maxYaw = 8f;
    [SerializeField] private float maxPitch = 5f;
    [SerializeField] private float smoothSpeed = 5f;

    private Quaternion baseRotation;

    private void OnEnable()
    {
        baseRotation = transform.localRotation;
    }

    private void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        float nx = Mathf.Clamp(mousePos.x / Screen.width * 2f - 1f, -1f, 1f);
        float ny = Mathf.Clamp(mousePos.y / Screen.height * 2f - 1f, -1f, 1f);

        Quaternion target = baseRotation * Quaternion.Euler(-ny * maxPitch, nx * maxYaw, 0f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * smoothSpeed);
    }
}