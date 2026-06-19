using System.Collections.Generic;

public class SaveMetaData
{
    public int Slot;
    public string SaveName;
    public string Timestamp;           // "Jun 8, 2026  14:32"
    public string PlaytimeFormatted;   // "01:24:33"
    public float PlaytimeSeconds;      // raw value for accumulation on load
    public string LastLineId = string.Empty;                    // line:xxxx — silent replay target on load
    public List<int> ReplayOptionChoices = new List<int>();     // DialogueOptionIDs chosen in current node, in order
    public string ChapterName;         // "Ch.2 – Under the Stars"
    public bool IsEmpty = true;

}