// ============================================================
//  BaseMiniGame.cs — Abstract base for all minigames.
//
//  SOLID — S (Single Responsibility):
//    BaseMiniGame now owns exactly: state machine + event wiring.
//    Panel sliding   → delegated to PanelSlider (plain C# class).
//    Input polling   → delegated to IInputProvider.
//    Camera / system → delegated to IMinigameContext.
//
//  SOLID — D (Dependency Inversion):
//    Initialize() accepts IMinigameContext — an abstraction.
//    BaseMiniGame never references CameraController,
//    CocktailSystemManager, or MinigameSystemManager directly.
//    IInputProvider defaults to UnityInputProvider; swap freely.
//
//  SOLID — O (Open / Closed):
//    New minigames extend this class and override the hooks
//    (OnProcessing, OnSuccess, OnStandby, ProcessedGame).
//    The state machine and event wiring never change.
//
//  SOLID — L (Liskov Substitution):
//    Every subclass is a valid IMinigame. The IsRunning guard
//    lives HERE so subclasses don't each need to repeat it —
//    they cannot accidentally break the contract.
// ============================================================

using System;
using UnityEngine;
using static E_Cocktail;

public abstract class BaseMiniGame : MonoBehaviour, IMinigame
{
    // ── Inspector ──────────────────────────────────────────

    /// <summary>
    /// Assign the matching SO_*Setting asset in the Inspector.
    /// Kept on the base class (not on IMinigame) — it is an
    /// implementation detail, not part of the public contract.
    /// </summary>
    [field: SerializeField]
    public SO_MinigameSetting Setting { get; set; }

    [SerializeField] protected RectTransform _minigamePanel;

    [Header("Slide Settings")]
    [SerializeField] private float _slidePanelSpeed = 800f;

    // ── IMinigame — Public State ───────────────────────────

    public bool IsRunning { get; protected set; }

    /// <inheritdoc/>
    public event Action<bool> OnGameEnd;

    // ── State Machine ──────────────────────────────────────

    public MiniGameState CurrentState { get; protected set; } = MiniGameState.Standby;

    // ── Abstract ───────────────────────────────────────────

    /// <summary>
    /// Identifies this game in the registry.
    /// Drives OCP in MinigameSystemManager — no hardcoded fields.
    /// </summary>
    public abstract Enum_MiniGameType GameType { get; }

    // ── Private Dependencies (injected, not hardcoded) ─────

    private IMinigameContext _context;
    protected IInputProvider Input { get; private set; }

    // ── Extracted: panel + input ───────────────────────────

    /// <summary>Slide helper — constructed after _minigamePanel is set.</summary>
    protected PanelSlider PanelSlider { get; private set; }

    // ── Unity Lifecycle ────────────────────────────────────

    protected virtual void Awake()
    {
        PanelSlider = new PanelSlider(_minigamePanel, _slidePanelSpeed);
    }

    // ── Initialization ─────────────────────────────────────

    /// <summary>
    /// Call from MinigameSystemManager.Awake() before any game starts.
    /// <paramref name="inputProvider"/> defaults to UnityInputProvider when null.
    /// </summary>
    public void Initialize(IMinigameContext context, IInputProvider inputProvider = null)
    {
        _context = context;
        Input = inputProvider ?? new UnityInputProvider();
        IsRunning = false;
    }

    // ── State Machine ──────────────────────────────────────

    public void SetState(MiniGameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;

        switch (newState)
        {
            case MiniGameState.Processing: OnProcessing(); break;
            case MiniGameState.Success: OnSuccess(); break;
            case MiniGameState.Standby: OnStandby(); break;
        }
    }

    // ── Protected Hooks (override in subclasses) ───────────

    protected virtual void OnProcessing() { }
    protected virtual void OnSuccess() => FireEndEvent(true);
    protected virtual void OnStandby() => ResetGame();

    // ── IMinigame ──────────────────────────────────────────

    public virtual void StartGame()
    {
        IsRunning = true;
        _context?.ResetCamera();
        SetState(MiniGameState.Standby);
        SetState(MiniGameState.Processing);
    }

    /// <summary>
    /// Called every frame by MinigameSystemManager.
    /// Polls input and guards against running = false.
    /// Subclasses call base.ProcessedGame() to get Input refreshed,
    /// then read Input.IsClickedThisFrame.
    /// The IsRunning guard lives here — subclasses do not repeat it.
    /// </summary>
    public virtual void ProcessedGame()
    {
        if (!IsRunning) return;
        Input.Poll();
    }

    public virtual void UpdateUI() { }

    public virtual void EndGame()
    {
        IsRunning = false;
        _context?.NotifyGameEnded();
    }

    public virtual string GetGameState()
        => $"{GetType().Name} | State: {CurrentState} | Running: {IsRunning}";

    // ── Helpers ────────────────────────────────────────────

    /// <summary>Ends the game and fires OnGameEnd.</summary>
    protected void FireEndEvent(bool success)
    {
        EndGame();
        OnGameEnd?.Invoke(success);
    }

    protected virtual void ResetGame() { }
}