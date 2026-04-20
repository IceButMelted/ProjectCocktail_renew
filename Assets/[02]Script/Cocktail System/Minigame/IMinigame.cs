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