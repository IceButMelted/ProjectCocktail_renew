using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class GridData
{
    Dictionary<Vector3Int, PlacementData> placedObjects = new();

    public void AddPlacedObject(Vector3Int gridPosition,
                                Vector2Int objectSize,
                                int ID,
                                int placementObjectIndex)
    {
        List<Vector3Int> positionToOccupy = CalculatePosition(gridPosition,objectSize); 
        PlacementData data = new PlacementData(positionToOccupy, ID, placementObjectIndex);
        foreach (var pos in positionToOccupy) {
            if (placedObjects.ContainsKey(pos)) { 
                throw new Exception("Cell already occupied at position: " + pos);
            }
            placedObjects[pos] = data;
        }
    }

    private List<Vector3Int> CalculatePosition(Vector3Int gridPosition, Vector2Int objectSize)
    {
        List<Vector3Int> returnVal = new();
        for (int x = 0; x < objectSize.x; x++) {
            for (int y = 0; y < objectSize.y; y++) {
                returnVal.Add(gridPosition + new Vector3Int(x,0,y));
            }
        }
        return returnVal;
    }

    public bool CanPlaceObjectAt(Vector3Int gridPosition, Vector2Int objectSize) { 
        List<Vector3Int> positionToOccupy = CalculatePosition(gridPosition, objectSize);
        foreach (var pos in positionToOccupy) {
            if (placedObjects.ContainsKey(pos)) {
                return false;
            }
        }
        return true;
    }
}

public class  PlacementData
{
    public List<Vector3Int> occupiedCellsPositions;
    public int ID { get; private set; }
    public int PlacedObjectIndex { get; private set; }

    public PlacementData(List<Vector3Int> occupiedCellsPositions, int iD, int placedObjectIndex)
    {
        this.occupiedCellsPositions = occupiedCellsPositions;
        ID = iD;
        PlacedObjectIndex = placedObjectIndex;
    }
}