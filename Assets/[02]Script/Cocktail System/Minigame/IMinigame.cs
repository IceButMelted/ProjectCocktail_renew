using UnityEngine;

#region Base Minigame Setting
// ─────────────────────────────────────────────
//  Base ScriptableObject for all Minigames
// ─────────────────────────────────────────────

/// <summary>
/// Base configuration required by every Minigame.
/// Derive this class to add game-specific settings.
/// </summary>
[CreateAssetMenu(fileName = "NewMinigameSetting", menuName = "Bar410/Minigame/Base Setting")]
public class SO_MinigameSetting : ScriptableObject
{
    [Header("General")]
    [Tooltip("Difficulty multiplier (1 = normal, >1 = harder)")]
    public float DifficultyMultiplier = 1f;

    [Tooltip("Total duration of the minigame in seconds.")]
    public float Duration = 5f;
}
#endregion

#region Shaking Setting 
// ─────────────────────────────────────────────
//  Shaking Setting
// ─────────────────────────────────────────────

/// <summary>
/// GDD 3.2.1 — Spam click to keep the gauge inside the target zone for the full duration.
/// </summary>
[CreateAssetMenu(fileName = "ShakingSetting", menuName = "Bar410/Minigame/Shaking Setting")]
public class SO_ShakingSetting : SO_MinigameSetting
{
    [Header("Shaking Zone")]
    [Range(0f, .4f)] public float TargetZoneMinSize = 0.4f;
    [Range(0f, .95f)] public float TargetZoneMaxSize = 0.7f;
    [Tooltip("Initial minimum value for the target zone. Target zone must not go below this value.")]
    [Range(.5f, .95f)] public float InitTargetZone_MinValue = 0.7f; // must not go below this
    [Range(0f, 0.05f)] public float TargetZone_DecreasePerProgress = 0.01f; // zone shrinks as progress grows

    [Header("Progress Bar")]
    [Range(0f,1f)]public float ProgressBar_IncreaseRate = 0.2f; // per second, while inside zone
    public Color ProgressBar_StartColor = Color.green;
    public Color ProgressBar_EndColor = Color.red;

    [Tooltip("Gauge decay per second when the player is not clicking.")]
    public float GaugeDecayRate = 0.15f;

    [Tooltip("Gauge increase per click.")]
    public float GaugeIncreasePerClick = 0.08f;
}
#endregion

#region Mixing Setting
// ─────────────────────────────────────────────
//  Mixing Setting
// ─────────────────────────────────────────────

/// <summary>
/// GDD 3.2.2 — Click to stop the needle inside the target zone (Timing Bar).
/// </summary>
[CreateAssetMenu(fileName = "MixingSetting", menuName = "Bar410/Minigame/Mixing Setting")]
public class SO_MixingSetting : SO_MinigameSetting
{
    [Header("Timing Bar")]
    [Range(0f, 1f)] public float TargetZoneMin = 0.45f;
    [Range(0f, 1f)] public float TargetZoneMax = 0.55f;

    [Tooltip("Needle speed in normalized units (0–1) per second.")]
    public float NeedleSpeed = 0.6f;

    [Tooltip("Number of successful hits required to complete the minigame.")]
    public int RequiredHits = 3;
}
#endregion

#region Grind Setting
// ─────────────────────────────────────────────
//  Grind Setting
// ─────────────────────────────────────────────

/// <summary>
/// GDD 3.2.3 — Grind the mixer first, then optionally chain into Shaking or Mixing.
/// </summary>
[CreateAssetMenu(fileName = "GrindSetting", menuName = "Bar410/Minigame/Grind Setting")]
public class SO_GrindSetting : SO_MinigameSetting
{
    [Header("Grind")]
    [Tooltip("Number of clicks required to complete the grind.")]
    public int RequiredGrindClicks = 10;

    [Tooltip("After grinding, the player may chain into Shaking or Mixing.")]
    public bool CanChainToShaking = true;
    public bool CanChainToMixing = true;
}

#endregion

/// <summary>
/// Standard interface that every Minigame must implement.
/// Allows BeverageManager to interact with any minigame polymorphically.
/// </summary>
public enum MiniGameState
{
    Processing,
    Success,
    Standby
}

public interface IMinigame
{
    SO_MinigameSetting Setting { get; set; }
    bool IsRunning { get; }

    void StartGame();
    void EndGame();
    void ProcessedGame();
    string GetGameState();
}