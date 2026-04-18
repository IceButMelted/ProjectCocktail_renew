using Unity.Hierarchy;
using UnityEngine;

// ─────────────────────────────────────────────
//  Minigame State
// ─────────────────────────────────────────────

public enum MiniGameState
{
    Standby,
    Processing,
    Success
}

// ─────────────────────────────────────────────
//  Interface
// ─────────────────────────────────────────────

/// <summary>
/// Contract every minigame must fulfill.
/// Allows MinigameSystemManager to drive any minigame polymorphically.
/// </summary>
public interface IMinigame
{
    SO_MinigameSetting Setting { get; set; }
    bool IsRunning { get; }

    void StartGame();
    void EndGame();
    void ProcessedGame();
    string GetGameState();
}

// ─────────────────────────────────────────────
//  Base ScriptableObject setting
// ─────────────────────────────────────────────

/// <summary>
/// Shared configuration inherited by every minigame setting asset.
/// </summary>
[CreateAssetMenu(fileName = "NewMinigameSetting", menuName = "Bar410/Minigame/Base Setting")]
public class SO_MinigameSetting : ScriptableObject
{
    [Header("General")]
    [Tooltip("Difficulty multiplier applied on top of base values (1 = normal, >1 = harder).")]
    public float DifficultyMultiplier = 1f;

    [Tooltip("Total duration the player must survive / succeed within.")]
    public float Duration = 5f;
}

// ─────────────────────────────────────────────
//  Shaking setting  (GDD 3.2.1)
// ─────────────────────────────────────────────

/// <summary>
/// Spam-click to keep the gauge inside the target zone for the full duration.
/// </summary>
[CreateAssetMenu(fileName = "ShakingSetting", menuName = "Bar410/Minigame/Shaking Setting")]
public class SO_ShakingSetting : SO_MinigameSetting
{
    [Header("Target Zone")]
    [Range(0f, 0.4f)] public float TargetZoneMinSize = 0.4f;
    [Range(0f, 0.95f)] public float TargetZoneMaxSize = 0.7f;

    [Tooltip("Lowest normalized value the zone center may start at.")]
    [Range(0.5f, 0.95f)] public float InitTargetZoneMinValue = 0.7f;

    [Tooltip("How much the zone shrinks per unit of progress gained.")]
    [Range(0f, 0.05f)] public float TargetZoneShrinkPerProgress = 0.01f;

    [Header("Gauge")]
    [Tooltip("Gauge decay per second while the player is not clicking.")]
    public float GaugeDecayRate = 0.15f;

    [Tooltip("Gauge increase per click.")]
    public float GaugeIncreasePerClick = 0.08f;

    [Header("Progress Bar")]
    [Tooltip("Progress gained per second while gauge is inside the zone.")]
    [Range(0f, 1f)] public float ProgressIncreaseRate = 0.2f;

    public Color ProgressBarStartColor = Color.green;
    public Color ProgressBarEndColor = Color.red;
}

// ─────────────────────────────────────────────
//  Mixing setting  (GDD 3.2.2)
// ─────────────────────────────────────────────

/// <summary>
/// Click to stop the needle inside the target zone the required number of times.
/// </summary>
[CreateAssetMenu(fileName = "MixingSetting", menuName = "Bar410/Minigame/Mixing Setting")]
public class SO_MixingSetting : SO_MinigameSetting
{
    [Header("Target Zone")]
    [Range(0f, 1f)] public float TargetZoneMinSize = 0.45f;
    [Range(0f, 1f)] public float TargetZoneMaxSize = 0.55f;
    [Range(0.0f, 0.3f)] public float TargetZoneShrinkPerHit = 0.05f;
    [Range(0f, 1f)] public float TargetZoneExtendPerMiss = 0.1f;

    [Header("Needle")]
    [Tooltip("Needle speed in normalized units per second (base / reset value).")]
    public float NeedleInitSpeed = 0.6f;
    [Range(1f, 5f)] public float NeedleMaxSpeed;
    [Range(0f, 0.75f)] public float NeedleSpeedIncreasePerHit = 0.5f;
    [Range(0f, 1f)] public float NeedleSpeedDecreasePerMiss = 0.1f;
    //[field: SerializeField] public float NeedleAcceleration { get; private set; } = 0.66f;

    [Header("Win Condition")]
    [Tooltip("Number of successful hits required to complete the minigame.")]
    public int RequiredHits = 3;
}

// ─────────────────────────────────────────────
//  Grind setting  (GDD 3.2.3)
// ─────────────────────────────────────────────

/// <summary>
/// Click repeatedly to grind, then optionally chain into Shaking or Mixing.
/// </summary>
[CreateAssetMenu(fileName = "GrindSetting", menuName = "Bar410/Minigame/Grind Setting")]
public class SO_GrindSetting : SO_MinigameSetting
{
    [Header("Grind")]
    [Tooltip("Number of clicks required to finish grinding.")]
    public int RequiredGrindClicks = 10;

    [Tooltip("Which minigames may follow after grinding.")]
    public bool CanChainToShaking = true;
    public bool CanChainToMixing = true;
}