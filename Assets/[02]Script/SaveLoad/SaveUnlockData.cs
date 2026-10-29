using System.Collections.Generic;

/// <summary>
/// What the save keeps of the ingredient unlocks. Day unlocks are derived from $day (saved with
/// Yarn); only story unlocks and the "already announced" list need storing.
/// </summary>
public class SaveUnlockData
{
    public List<string> StoryUnlocked = new List<string>();
    public List<string> Notified = new List<string>();
}
