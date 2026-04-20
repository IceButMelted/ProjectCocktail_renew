using UnityEngine;

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