using UnityEngine;

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
