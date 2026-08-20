using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemManager : MonoBehaviour
{
    [SerializeField]
    public InputActionReference holdActionRef;
    [SerializeField]
    public InputActionReference TapActionRef;
    [SerializeField]
    public InputActionReference PointActionRef;
    [SerializeField]
    private InputAction input;

    private void OnEnable()
    {
        holdActionRef.action.Enable();
        TapActionRef.action.Enable();
    }

    private void OnDisable()
    {
        holdActionRef.action.Disable();
        TapActionRef.action.Disable();
    }

    private void Start()
    {

    }
}
