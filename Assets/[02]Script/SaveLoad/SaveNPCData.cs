using System.Collections.Generic;
using static E_Cocktail;

public class SaveNPCData
{
    public Dictionary<CharacterName, NPCData> npcDataDict = new Dictionary<CharacterName, NPCData>();
}

/// <summary>What the save keeps of one character: in the shop or not, where, and how they look.</summary>
public class NPCData
{
    public bool isPresent;
    public int spot;
    public int expressionIndex;
    public Facing look;
}
