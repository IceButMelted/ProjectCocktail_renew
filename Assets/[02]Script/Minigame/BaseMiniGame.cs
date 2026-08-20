using System;
using UnityEngine;
using static E_Cocktail;

/// <summary>
/// Everything a minigame shares: one phase machine, one driver, four hooks.
///
/// The loop is a straight line — Idle → Intro → Play → Outro → Idle.
/// Intro and Outro are the Animator's job (see <see cref="MinigamePanelAnimator"/>);
/// this class only reacts to the finish events it raises. No phase ever sets another
/// phase behind the FSM's back.
///
/// Subclasses override <see cref="OnEnter"/> / <see cref="OnTick"/> / <see cref="OnExit"/> /
/// <see cref="UpdateUI"/> — same hook names as Bar410.GameFlow.StateBase — and call
/// <see cref="Complete"/> when the win condition is met.
/// </summary>
public abstract class BaseMiniGame : MonoBehaviour, IMinigame
{
    // ── Inspector ──────────────────────────────────────────

    [Tooltip("Drives the intro/outro panel animation and reports when each clip is done.")]
    [SerializeField] private MinigamePanelAnimator _panels;

    [SerializeField] protected AnimationMinigameController amc; //<<Anim Minigame Controller

    [field: SerializeField]
    public SO_MinigameSetting Setting { get; set; }

    // ── IMinigame — Public State ───────────────────────────

    /// <summary>The one and only state variable. Everything else is derived from it.</summary>
    public MinigamePhase Phase { get; private set; } = MinigamePhase.Idle;

    public bool IsRunning => Phase == MinigamePhase.Play;

    /// <summary>How the most recent run ended. Written the moment the outro finishes.</summary>
    public MinigameOutcome LastOutcome { get; private set; } = MinigameOutcome.Cancelled;

    public event Action<bool> OnGameEnd;

    public abstract Enum_MiniGameType GameType { get; }

    // ── Private Dependencies ───────────────────────────────

    private IMinigameContext _context;
    protected IInputProvider Input { get; private set; }

    /// <summary>Only set when <see cref="ResolveSetting{T}"/> had to fabricate a default.</summary>
    private ScriptableObject _runtimeSetting;

    /// <summary>Outcome carried from Complete()/Cancel() until the outro finishes.</summary>
    private MinigameOutcome _pendingOutcome = MinigameOutcome.Cancelled;

    // ── Unity Lifecycle ────────────────────────────────────

    protected virtual void OnEnable()
    {
        if (_panels == null) return;
        _panels.IntroFinished += HandleIntroFinished;
        _panels.OutroFinished += HandleOutroFinished;
    }

    protected virtual void OnDisable()
    {
        if (_panels == null) return;
        _panels.IntroFinished -= HandleIntroFinished;
        _panels.OutroFinished -= HandleOutroFinished;
    }

    protected virtual void OnDestroy()
    {
        // Only ever destroys the instance we created ourselves — never a project asset.
        if (_runtimeSetting != null) Destroy(_runtimeSetting);
    }

    // ── Initialization ─────────────────────────────────────

    public void Initialize(IMinigameContext context, IInputProvider inputProvider = null)
    {
        _context = context;
        Input = inputProvider ?? new UnityInputProvider();
    }

    /// <summary>
    /// Returns <see cref="Setting"/> typed as <typeparamref name="T"/>, or a runtime default
    /// if the Inspector slot is empty or holds the wrong asset type.
    /// A fabricated default is destroyed in <see cref="OnDestroy"/>; an assigned asset never is.
    /// </summary>
    protected T ResolveSetting<T>() where T : SO_MinigameSetting
    {
        if (Setting is T assigned) return assigned;

        var fallback = ScriptableObject.CreateInstance<T>();
        _runtimeSetting = fallback;
        Debug.LogWarning($"[{GetType().Name}] No {typeof(T).Name} assigned — using default values.");
        return fallback;
    }

    // ── Phase Machine ──────────────────────────────────────

    private void SetPhase(MinigamePhase next)
    {
        if (Phase == next) return;

        ExitPhase(Phase);
        Phase = next;
        EnterPhase(next);
    }

    private void EnterPhase(MinigamePhase phase)
    {
        switch (phase)
        {
            case MinigamePhase.Intro:
                _panels?.Show();
                break;

            case MinigamePhase.Play:
                OnEnter();
                UpdateUI();
                break;

            case MinigamePhase.Outro:
                if (amc != null) amc.StopAnimation();
                _panels?.Hide();
                break;

            case MinigamePhase.Idle:
                // The single exit. Both the win path and the cancel path land here,
                // and nothing reports a result anywhere else.
                LastOutcome = _pendingOutcome;
                OnGameEnd?.Invoke(_pendingOutcome == MinigameOutcome.Completed);
                _context?.NotifyGameEnded(GameType, _pendingOutcome);
                break;
        }
    }

    private void ExitPhase(MinigamePhase phase)
    {
        if (phase == MinigamePhase.Play) OnExit();
    }

    private void HandleIntroFinished()
    {
        if (Phase == MinigamePhase.Intro) SetPhase(MinigamePhase.Play);
    }

    private void HandleOutroFinished()
    {
        if (Phase == MinigamePhase.Outro) SetPhase(MinigamePhase.Idle);
    }

    // ── IMinigame ──────────────────────────────────────────

    /// <summary>Idle → Intro. Ignored while a run is already in flight.</summary>
    public void StartGame()
    {
        if (Phase != MinigamePhase.Idle)
        {
            Debug.LogWarning($"[{GetType().Name}] StartGame ignored — already in {Phase}.");
            return;
        }

        // Anything that ends the run before Complete() counts as cancelled.
        _pendingOutcome = MinigameOutcome.Cancelled;
        _context?.ResetCamera();
        SetPhase(MinigamePhase.Intro);
    }

    /// <summary>The single driver. Intro and Outro belong to the Animator, so they tick nothing.</summary>
    public void ProcessedGame()
    {
        if (Phase != MinigamePhase.Play) return;

        Input.Poll();
        OnTick(Time.deltaTime);
        UpdateUI();
    }

    /// <summary>Win condition met. Plays the outro; the result is reported when it finishes.</summary>
    protected void Complete()
    {
        if (Phase != MinigamePhase.Play) return;

        _pendingOutcome = MinigameOutcome.Completed;
        SetPhase(MinigamePhase.Outro);
    }

    /// <summary>
    /// Aborts the run: close button, flow backtrack, or the manager switching games.
    /// The outro still plays — cancelling is not the same as hiding the panel instantly —
    /// and <see cref="MinigameOutcome.Cancelled"/> is reported once it finishes.
    /// No-op when already Idle, and harmless mid-Intro.
    /// </summary>
    public void Cancel()
    {
        if (Phase == MinigamePhase.Idle || Phase == MinigamePhase.Outro) return;

        _pendingOutcome = MinigameOutcome.Cancelled;
        SetPhase(MinigamePhase.Outro);
    }

    /// <summary>Alias for <see cref="Cancel"/> — kept for UI buttons wired to "close panel".</summary>
    public void ClosePanel() => Cancel();

    /// <summary>IMinigame alias for <see cref="Cancel"/> — kept for the editor hotkeys.</summary>
    public void EndGame() => Cancel();

    public virtual string GetGameState()
        => $"{GetType().Name} | Phase: {Phase} | Running: {IsRunning} | Last: {LastOutcome}";

    // ── Hooks (override in subclasses) ─────────────────────

    /// <summary>Entering Play: reset counters, randomise zones, sync UI.</summary>
    protected virtual void OnEnter() { }

    /// <summary>One frame of gameplay. No guards needed — only called during Play.</summary>
    protected virtual void OnTick(float dt) { }

    /// <summary>Leaving Play: stop coroutines, release anything OnEnter started.</summary>
    protected virtual void OnExit() { }

    /// <summary>Pushes runtime state onto the UI. Called after OnEnter and every OnTick.</summary>
    public virtual void UpdateUI() { }
}
