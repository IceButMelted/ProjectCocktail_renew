using UnityEngine;
using UnityEngine.InputSystem;

public class N_PlacementSystem : MonoBehaviour
{
    [SerializeField]
    private GameObject mouseIndicator;
    [SerializeField]
    private N_InputManager inputManager;

    [SerializeField]
    private float floatingDistance = 3f;

    private GameObject selectedObject;
    private DragableObject selectedDragsObject;
    private bool selectedObjectIsFloating = false;

    private float bottomOffset;

    private void Update()
    {
        Vector3 mousePos = inputManager.GetSelectedMapPosition();
        mouseIndicator.transform.position = mousePos;

        //on click
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            selectedObject = inputManager.GetObjectSelected();
            selectedDragsObject = inputManager.GetDragableObject();

            if (selectedObject != null && selectedDragsObject != null)
            {
                bottomOffset = GetBottomOffset(selectedObject);
                selectedDragsObject.BeingDrags = true;
            }
        }
        if (selectedObject != null && Mouse.current.leftButton.isPressed)
        {

            if (inputManager.TryGetPlacementPoint(out Vector3 placementPoint))
            {
                // On placement layer
                selectedObject.transform.position = placementPoint + Vector3.up * bottomOffset;
                selectedObjectIsFloating = false;
            }
            else
            {
                // Not on placement layer  float in front of camera
                selectedObject.transform.position = inputManager.GetFloatingPosition(floatingDistance);
                selectedObjectIsFloating = true;
            }
        }
        //on release
        if (Mouse.current.leftButton.wasReleasedThisFrame && selectedObject != null && selectedDragsObject != null)
        {
            //if can placed that place
            if (selectedDragsObject.CanPlaced && !selectedObjectIsFloating)
            {
                selectedDragsObject.PastLocation = transform.position = mousePos + Vector3.up * bottomOffset;
                //to where mouse are
                selectedObject.transform.position = mousePos + Vector3.up * bottomOffset;
            }
            else {
                selectedObject.transform.position = selectedDragsObject.PastLocation;
            }

            //free gameobject
            selectedObject = null;
            selectedDragsObject.BeingDrags = false;
            selectedDragsObject.CanPlaced = true;
            selectedDragsObject = null;
        }
    }

    private float GetBottomOffset(GameObject obj)
    {
        Renderer r = obj.GetComponentInChildren<Renderer>();
        return obj.transform.position.y - r.bounds.min.y;
    }

}
