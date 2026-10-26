using System.Collections.Generic;

[System.Serializable]
public class EventLogEntry
{
    public string Speaker;   // "" for a line with no character name
    public string Text;
    public bool IsChoice;    // true → the player's picked option (PlayerChoseOption row); old saves default to false
    public bool ShowName;    // true → With_Name row, false → WithOut_Name row
}

public class SaveEventLogData
{
    public List<EventLogEntry> Entries = new List<EventLogEntry>();
}
