using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.InputSystem;

public class N_PlacementSystem : MonoBehaviour
{
    private InputSystemManager inputSystemManager;

    [SerializeField]
    private GameObject mouseIndicator;
    [SerializeField]
    private N_InputManager inputManager;

    [SerializeField]
    private float floatingDistance = 3f;

    private GameObject selectedObject;
    private DragableObject selectedDragsObject;
    private bool selectedObjectIsFloating = false;

    private bool IsHolding;

    private float bottomOffset;

    private void Awake()
    {
        if (inputSystemManager = FindFirstObjectByType<InputSystemManager>())
        {
            Debug.Log("Found InputSystemManager");
        }
        else { 
            Debug.LogError("Cannot find InputSystemManager");
        }
    }

    private void Start()
    {

        inputSystemManager.holdActionRef.action.started += context =>
        {
        };

        inputSystemManager.holdActionRef.action.performed += context => 
        {
            DragObject();
        };

        inputSystemManager.holdActionRef.action.canceled += context =>
        {
            ReleaseObject();

        };
    }

    private void Update()
    {
        Vector3 mousePos = inputManager.GetSelectedMapPosition();
        mouseIndicator.transform.position = mousePos;

        if (selectedObject != null && IsHolding)
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

    }

    private float GetBottomOffset(GameObject obj)
    {
        Renderer r = obj.GetComponentInChildren<Renderer>();
        return obj.transform.position.y - r.bounds.min.y;
    }

    private void ReleaseObject() {
        Vector3 mousePos = inputManager.GetSelectedMapPosition();
        mouseIndicator.transform.position = mousePos;

        if (selectedObject != null && selectedDragsObject != null)
        {
            //if can placed that place
            if (selectedDragsObject.CanPlaced && !selectedObjectIsFloating)
            {
                selectedDragsObject.PastLocation = transform.position = mousePos + Vector3.up * bottomOffset;
                //to where mouse are
                selectedObject.transform.position = mousePos + Vector3.up * bottomOffset;
            }
            else
            {
                selectedObject.transform.position = selectedDragsObject.PastLocation;
            }

            selectedDragsObject.BeingDrags = false;
            selectedDragsObject.CanPlaced = true;
        }


        selectedObject = null;
        selectedDragsObject = null;

        IsHolding = false;
    }

    private void DragObject() {
        selectedObject = inputManager.GetObjectSelected();
        selectedDragsObject = inputManager.GetDragableObject();

        if (selectedObject != null && selectedDragsObject != null)
        {
            bottomOffset = GetBottomOffset(selectedObject);
            selectedDragsObject.BeingDrags = true;
        }
        IsHolding = true;
    }

}
