using System;
using static E_Cocktail;

// ── State ──────────────────────────────────────────────────

public enum MiniGameState
{
    /// <summary>Idle. The game is not running. Enters via reset or before first start.</summary>
    Standby,

    /// <summary>Active gameplay tick. Input is polled and game logic runs every frame.</summary>
    Processing,

    /// <summary>Win condition met. Panel slides out; OnGameEnd(true) fires.</summary>
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