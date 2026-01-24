using UnityEngine;
using System.Collections;   
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;


public class PlacementSystem : MonoBehaviour
{
    [SerializeField]
    [Header("Idecator Object")]
    private GameObject mouseIndicator, cellIndicator;

    [SerializeField]
    [Header("Input Manager")]
    [Tooltip("Drag Drop Iunpur Manager to This")]
    private InputManager inputManager;

    [SerializeField]
    private Grid grid;

    [SerializeField]
    private ObjectDataBaseSO dataBase;
    private int seletedObjectIndex = -1;

    [SerializeField]
    private GameObject gridVisualization;

    private GridData FloorData, furnitureData;

    private Renderer previewRenderer;

    private List<GameObject> placedObjects = new();

    private void Start()
    {
        StopPlacement();
        FloorData = new GridData();
        furnitureData = new();
        previewRenderer = cellIndicator.GetComponentInChildren<Renderer>();
    }

    public void StartPlacement(int ID) {
        StopPlacement();
        seletedObjectIndex = dataBase.objectDate.FindIndex(x => x.ID == ID);
        if (seletedObjectIndex < 0) { 
            Debug.LogError("Object with ID " + ID + " not found in database.");
            return;
        }

        gridVisualization.SetActive(true);
        cellIndicator.SetActive(true);
        inputManager.OnClicked += PlaceObject;
        inputManager.OnExit += StopPlacement;
    }

    private void PlaceObject()
    {
        if (inputManager.IsPointerOverUI()) {
            return;
        }
        Vector3 mousePosition = inputManager.GetSelectMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        bool placementValidity = CheckPlacementValidity(gridPosition,seletedObjectIndex);
        if (!placementValidity) return;

        GameObject newObject = Instantiate(dataBase.objectDate[seletedObjectIndex].Prefab);
        newObject.transform.position = grid.CellToWorld(gridPosition);
        placedObjects.Add(newObject);

        GridData selectedData = dataBase.objectDate[seletedObjectIndex].ID == 0 ? FloorData : furnitureData;

        selectedData.AddPlacedObject(gridPosition,
                                    dataBase.objectDate[seletedObjectIndex].Size,
                                    dataBase.objectDate[seletedObjectIndex].ID,
                                    placedObjects.Count - 1);    
    }

    private bool CheckPlacementValidity(Vector3Int gridPosition, int seletedObjectIndex)
    {
        GridData selectedData = dataBase.objectDate[seletedObjectIndex].ID == 0 ? FloorData : furnitureData;

        return selectedData.CanPlaceObjectAt(gridPosition,
                                                dataBase.objectDate[seletedObjectIndex].Size);
    }

    private void StopPlacement()
    {
        seletedObjectIndex = -1;
        gridVisualization.SetActive(false);
        cellIndicator.SetActive(false);
        inputManager.OnClicked -= PlaceObject;
        inputManager.OnExit -= StopPlacement;
    }

    private void Update()
    {
#if UNITY_EDITOR

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            StartPlacement(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            StartPlacement(1);
        else if (Keyboard.current.digit3Key.IsPressed())
            StopPlacement();
#endif

            if (seletedObjectIndex < 0)
        {
            return;
        }

        Vector3 mousePosition = inputManager.GetSelectMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        bool placementValidity = CheckPlacementValidity(gridPosition, seletedObjectIndex);
        previewRenderer.material.color = placementValidity ? Color.green : Color.red;

        mouseIndicator.transform.position = mousePosition;  
        cellIndicator.transform.position = grid.CellToWorld(gridPosition);   
    }



}
