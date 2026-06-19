using UnityEngine;
using static E_Cocktail;
using System.Collections.Generic;

public class SaveNPCData 
{
   public Dictionary<NPC_Name, NPCData> npcDataDict = new Dictionary<NPC_Name, NPCData>();
}

public class NPCData
{
    public Vector3 position;
    public Quaternion rotation;
    public int CurrentWayPointIndex;
    public Direction CurrentLookDirection;
}
