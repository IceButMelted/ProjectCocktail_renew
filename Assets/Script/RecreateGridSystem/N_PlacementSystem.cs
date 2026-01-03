using UnityEngine;
using UnityEngine.InputSystem;

public class N_PlacementSystem : MonoBehaviour
{
    [SerializeField]
    private GameObject mouseIndicator;
    [SerializeField]
    private N_InputManager inputManager;

    private float floatingDistance = 3f;

    private GameObject selectedObject;
    private float bottomOffset;

    private void Update()
    {
        Vector3 mousePos = inputManager.GetSelectedMapPosition();
        mouseIndicator.transform.position = mousePos;

        //on click
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            selectedObject = inputManager.GetObjectSelected();

            if (selectedObject != null)
            {
                bottomOffset = GetBottomOffset(selectedObject);
            }
        }
        if (selectedObject != null && Mouse.current.leftButton.isPressed)
        {

            if (inputManager.TryGetPlacementPoint(out Vector3 placementPoint))
            {
                // On placement layer
                selectedObject.transform.position = placementPoint + Vector3.up * bottomOffset;
            }
            else
            {
                // Not on placement layer  float in front of camera
                selectedObject.transform.position = inputManager.GetFloatingPosition(floatingDistance);
            }
        }
        //on release
        if (Mouse.current.leftButton.wasReleasedThisFrame && selectedObject != null)
        {
            selectedObject.transform.position =
                mousePos + Vector3.up * bottomOffset;

            selectedObject = null;
        }
    }

    private float GetBottomOffset(GameObject obj)
    {
        Renderer r = obj.GetComponentInChildren<Renderer>();
        return obj.transform.position.y - r.bounds.min.y;
    }

}
