using System;
using static E_Cocktail;

// ── Core Minigame Contract ─────────────────────────────────

/// <summary>
/// Contract every minigame must fulfill.
/// MinigameSystemManager drives any IMinigame polymorphically —
/// no knowledge of concrete types required.
/// </summary>
public interface IMinigame
{
    /// <summary>True only while the game is in <see cref="MinigamePhase.Play"/>.</summary>
    bool IsRunning { get; }

    /// <summary>How the most recent run ended. Meaningless until <see cref="OnGameEnd"/> has fired once.</summary>
    MinigameOutcome LastOutcome { get; }

    /// <summary>
    /// Fired once the outro has finished, never before.
    /// True = <see cref="MinigameOutcome.Completed"/>, false = <see cref="MinigameOutcome.Cancelled"/>.
    /// Read <see cref="LastOutcome"/> inside the handler when the distinction matters.
    /// </summary>
    event Action<bool> OnGameEnd;

    /// <summary>Idle → Intro. Ignored if the game is already running.</summary>
    void StartGame();

    /// <summary>
    /// Aborts the run from any phase: plays the outro, then reports
    /// <see cref="MinigameOutcome.Cancelled"/>. No-op when already Idle or in the outro.
    /// </summary>
    void Cancel();

    /// <summary>Alias for <see cref="Cancel"/>, kept for existing callers and editor hotkeys.</summary>
    void EndGame();

    /// <summary>Called every frame by MinigameSystemManager.Update().</summary>
    void ProcessedGame();

    string GetGameState();
}

// ── Context Abstraction (DIP) ──────────────────────────────

/// <summary>
/// Abstracts what BaseMiniGame needs from the outside world: to say that a run is over.
/// MinigameSystemManager implements this; nothing else needs to know.
///
/// This is also the seam the game-flow layer plugs into: a minigame never learns
/// what <c>Bar410.GameFlow.PrepSub</c> is, and the HSM never learns what
/// <see cref="MinigamePhase"/> is. Everything crosses here.
/// </summary>
public interface IMinigameContext
{
    /// <summary>
    /// Called exactly once per run, when the outro finishes.
    /// Carries its own <paramref name="type"/> so a game that ended after the manager
    /// already switched away still reports itself correctly.
    /// </summary>
    void NotifyGameEnded(E_Cocktail.Enum_MiniGameType type, MinigameOutcome outcome);
}
