using UnityEngine;

public static class PlaytimeTracker
{
    private static float _sessionStart = -1f;
    private static float _accumulated = 0f;

    public static void StartSession()
    {
        _sessionStart = Time.realtimeSinceStartup;
    }

    // Call this after loading a save, so the clock resumes from saved time
    public static void SetAccumulated(float seconds)
    {
        _accumulated = seconds;
        _sessionStart = Time.realtimeSinceStartup;
    }

    public static float TotalSeconds =>
        _accumulated + (_sessionStart >= 0 ? Time.realtimeSinceStartup - _sessionStart : 0f);

    public static string Formatted()
    {
        int t = Mathf.FloorToInt(TotalSeconds);
        return $"{t / 3600:D2}:{(t % 3600) / 60:D2}:{t % 60:D2}";
    }
}