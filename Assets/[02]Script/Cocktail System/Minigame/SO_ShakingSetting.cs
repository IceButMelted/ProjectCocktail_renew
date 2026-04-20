using UnityEngine;

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
