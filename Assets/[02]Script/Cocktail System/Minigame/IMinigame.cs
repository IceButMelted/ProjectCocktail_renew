// ============================================================
//  IMinigame.cs — Core minigame abstractions.
//
//  SOLID — I (Interface Segregation):
//    IMinigame defines ONLY what a game loop driver needs.
//    SO_MinigameSetting is removed from the interface — it was
//    an implementation detail, not a contract. Consumers that
//    only call StartGame/EndGame/ProcessedGame never needed it.
//
//    IMinigameContext is a separate, focused interface for the
//    two things BaseMiniGame needs from the outside world:
//    camera reset and end-game notification.
//
//  SOLID — D (Dependency Inversion):
//    BaseMiniGame depends on IMinigameContext (abstraction),
//    not on CameraController or MinigameSystemManager (concrete).
//    Swap implementations — or mock in tests — without touching
//    any minigame class.
//
//  SOLID — O (Open / Closed):
//    OnGameEnd is on the interface so new subscribers (e.g.
//    GameLoopManager) can hook any IMinigame without casting to
//    a concrete type.
// ============================================================

using System;
using static E_Cocktail;

// ── State ──────────────────────────────────────────────────

public enum MiniGameState
{
    Standby,
    Processing,
    Success
}

// ── Core Minigame Contract ─────────────────────────────────

/// <summary>
/// Contract every minigame must fulfill.
/// MinigameSystemManager drives any IMinigame polymorphically —
/// no knowledge of concrete types required.
/// </summary>
public interface IMinigame
{
    /// <summary>True while the game loop is active.</summary>
    bool IsRunning { get; }

    /// <summary>
    /// Fired when the game ends.
    /// True = success, false = failure.
    /// Exposed on the interface so subscribers never need to cast.
    /// </summary>
    event Action<bool> OnGameEnd;

    void StartGame();
    void EndGame();

    /// <summary>Called every frame by MinigameSystemManager.Update().</summary>
    void ProcessedGame();

    string GetGameState();
}

// ── Context Abstraction (DIP) ──────────────────────────────

/// <summary>
/// Abstracts the two things BaseMiniGame needs from the outside world.
/// MinigameSystemManager implements this; nothing else needs to know.
/// </summary>
public interface IMinigameContext
{
    /// <summary>Called at game start — e.g. snaps the camera back.</summary>
    void ResetCamera();

    /// <summary>Called at game end — e.g. fires the OnEndedGame UnityEvent.</summary>
    void NotifyGameEnded();
}

// ── Grind Setting (kept here as it extends the minigame hierarchy) ──

/// <summary>
/// Click repeatedly to grind, then optionally chain into Shaking or Mixing.
/// </summary>
[UnityEngine.CreateAssetMenu(fileName = "GrindSetting", menuName = "Bar410/Minigame/Grind Setting")]
public class SO_GrindSetting : SO_MinigameSetting
{
    [UnityEngine.Header("Grind")]
    [UnityEngine.Tooltip("Number of clicks required to finish grinding.")]
    public int RequiredGrindClicks = 10;

    [UnityEngine.Tooltip("Which minigames may follow after grinding.")]
    public bool CanChainToShaking = true;
    public bool CanChainToMixing = true;
}