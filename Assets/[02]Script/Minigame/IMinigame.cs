using System;
using static E_Cocktail;

// ── Phase ──────────────────────────────────────────────────

/// <summary>
/// The only state machine a minigame has.
/// Runs as a straight line: Idle → Intro → Play → Outro → Idle.
///
/// Intro/Outro are owned by the Animator (see MinigamePanelAnimator);
/// Play is the only phase that ticks gameplay or accepts input.
/// </summary>
public enum MinigamePhase
{
    /// <summary>Nothing on screen. Panels sit at their hidden Animator pose.</summary>
    Idle,

    /// <summary>Intro clip playing. No input, no gameplay tick.</summary>
    Intro,

    /// <summary>Active gameplay. Input is polled and OnTick(dt) runs every frame.</summary>
    Play,

    /// <summary>Outro clip playing. When it finishes the game reports its result and returns to Idle.</summary>
    Outro
}

// ── Outcome ────────────────────────────────────────────────

/// <summary>
/// Why a run ended. Reported once, when the outro finishes.
///
/// Deliberately not a bool: the flow layer needs to tell "player finished the drink"
/// apart from "player backed out", because those lead to different HSM transitions.
/// </summary>
public enum MinigameOutcome
{
    /// <summary>Win condition met — the game ran to its natural end.</summary>
    Completed,

    /// <summary>Aborted before the win condition: close button, flow backtrack, or a game switch.</summary>
    Cancelled
}

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
/// Abstracts the two things BaseMiniGame needs from the outside world.
/// MinigameSystemManager implements this; nothing else needs to know.
///
/// This is also the seam the game-flow layer plugs into: a minigame never learns
/// what <c>Bar410.GameFlow.PrepSub</c> is, and the HSM never learns what
/// <see cref="MinigamePhase"/> is. Everything crosses here.
/// </summary>
public interface IMinigameContext
{
    /// <summary>Called at game start — e.g. snaps the camera back.</summary>
    void ResetCamera();

    /// <summary>
    /// Called exactly once per run, when the outro finishes.
    /// Carries its own <paramref name="type"/> so a game that ended after the manager
    /// already switched away still reports itself correctly.
    /// </summary>
    void NotifyGameEnded(E_Cocktail.Enum_MiniGameType type, MinigameOutcome outcome);
}
