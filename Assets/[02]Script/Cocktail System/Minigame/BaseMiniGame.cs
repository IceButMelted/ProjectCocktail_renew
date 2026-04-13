// ============================================================
//  Bar410 — BaseMiniGame (FSM Core)
//
//  Update loop is intentionally NOT here.
//  MinigameSystemManager owns Update and calls ProcessedGame()
//  each frame, keeping tick control in one place.
// ============================================================

using System;
using UnityEngine;

public abstract class BaseMiniGame : MonoBehaviour, IMinigame
{
    // ── IMinigame ──────────────────────────────────────────
    [field: SerializeField]
    public SO_MinigameSetting Setting { get; set; }

    public bool IsRunning { get; protected set; }

    // ── FSM ────────────────────────────────────────────────
    public MiniGameState CurrentState { get; protected set; } = MiniGameState.Initialize;

    // ── Events ─────────────────────────────────────────────
    /// <summary>Fired when the game ends. Bool = success.</summary>
    public event Action<bool> OnGameEnd;


    // ── Input (read-only for subclasses) ───────────────────

    /// <summary>
    /// True on the exact frame the player pressed the left mouse button.
    /// Polled inside ProcessedGame() so subclasses never call Input directly.
    /// </summary>
    protected bool IsClickedThisFrame { get; private set; }

    // ── FSM Transitions ────────────────────────────────────
    /// <summary>Transition to a new state and invoke the matching handler.</summary>
    protected void SetState(MiniGameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;

        switch (newState)
        {
            case MiniGameState.Initialize: OnInitialize(); break;
            case MiniGameState.Processing: OnProcessing(); break;
            case MiniGameState.Success: OnSuccess(); break;
            case MiniGameState.Fail: OnFail(); break;
        }
    }

    // ── FSM Handlers (override in subclasses) ──────────────

    protected virtual void OnInitialize() { }
    protected virtual void OnProcessing() { }
    protected virtual void OnSuccess() => FireEndEvent(true);
    protected virtual void OnFail() => FireEndEvent(false);

    // ── IMinigame ──────────────────────────────────────────

    public virtual void StartGame()
    {
        IsRunning = true;
        SetState(MiniGameState.Processing);
    }

    public virtual void EndGame()
    {
        IsRunning = false;
    }

    /// <summary>
    /// Called every frame by MinigameSystemManager.Update().
    /// Refreshes IsClickedThisFrame then runs subclass logic.
    /// Base guard: exits early when not running.
    /// </summary>
    public virtual void ProcessedGame()
    {
        // Always poll input so subclasses can read IsClickedThisFrame
        IsClickedThisFrame = Input.GetMouseButtonDown(0);

        if (!IsRunning) return;
    }

    public virtual string GetGameState()
        => $"{GetType().Name} | State: {CurrentState} | Running: {IsRunning}";

    // ── Helper ─────────────────────────────────────────────

    protected void FireEndEvent(bool success)
    {
        EndGame();
        OnGameEnd?.Invoke(success);
    }
}