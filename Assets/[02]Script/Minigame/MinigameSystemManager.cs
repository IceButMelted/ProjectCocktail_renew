using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static E_Cocktail;

public class MinigameSystemManager : MonoBehaviour, IMinigameContext
{
    // ── Inspector ──────────────────────────────────────────

    [Header("Dependencies")]
    [SerializeField] private CameraController _cocktailCamera;

    [Header("Events")]
    public UnityEvent OnStartedMinigame;
    public UnityEvent OnEndedGame;

    // ── Game-flow seam (HSM) ───────────────────────────────
    // Reserved for Bar410.GameFlow. Nothing subscribes yet — MinigameFlowBridge
    // (see Bar410_Minigame_Integration_Plan.md §3.3) is the intended and only listener.
    //
    //   MinigameState.OnStartRequested(type) ──> StartMinigame(type)
    //   MinigameFinished(type, outcome)      ──> MinigameState.ReportResult(outcome)
    //   MinigameState.OnStopRequested        ──> CancelMinigame()
    //
    // Kept as a plain C# event, not a UnityEvent: the flow layer subscribes in code
    // and must not be re-pointable from the Inspector.

    /// <summary>
    /// Raised once per run, when the outro has finished.
    /// Carries the type of the game that actually ended — not necessarily <see cref="ActiveType"/>,
    /// since the manager may already have switched away.
    /// </summary>
    public event Action<Enum_MiniGameType, MinigameOutcome> MinigameFinished;

    /// <summary>Raised when a run begins, before the intro plays.</summary>
    public event Action<Enum_MiniGameType> MinigameStarted;

    /// <summary>Which game the manager is currently driving. <c>None</c> if there is none.</summary>
    public Enum_MiniGameType ActiveType
        => _activeMinigame != null ? _activeMinigame.GameType : Enum_MiniGameType.None;

    /// <summary>The active game's phase — <c>Idle</c> when nothing is on screen.</summary>
    public MinigamePhase ActivePhase
        => _activeMinigame != null ? _activeMinigame.Phase : MinigamePhase.Idle;

    // ── Private State ──────────────────────────────────────

    /// <summary>
    /// Registry built automatically from every BaseMiniGame component on this GameObject.
    /// Adding a new minigame = add the component. Nothing else.
    /// </summary>
    private readonly Dictionary<Enum_MiniGameType, BaseMiniGame> _registry
        = new Dictionary<Enum_MiniGameType, BaseMiniGame>();

    private BaseMiniGame _activeMinigame;

    // ── Unity Lifecycle ────────────────────────────────────

    private void Awake()
    {
        // OCP: discover all minigames without any hardcoded references.
        foreach (var game in GetComponents<BaseMiniGame>())
        {
            game.Initialize(this); // DIP: pass IMinigameContext, not 'this' as concrete
            _registry[game.GameType] = game;
            Debug.Log($"[MinigameSystemManager] Registered: {game.GameType} → {game.GetType().Name}");
        }

        // Default active game (first found, or Shaking if present)
        if (_registry.TryGetValue(Enum_MiniGameType.Shaking, out var shaking))
            _activeMinigame = shaking;
        else if (_registry.Count > 0)
            _activeMinigame = GetComponents<BaseMiniGame>()[0];
    }

    private void Update()
    {
#if UNITY_EDITOR
        HandleEditorHotkeys();
#endif
        if (_activeMinigame == null)
        {
            Debug.LogWarning("[MinigameSystemManager] No active minigame.");
            return;
        }
        _activeMinigame.ProcessedGame();
    }

    void IMinigameContext.ResetCamera()
        => _cocktailCamera?.ResetRotaionAndMovement();

    /// <summary>
    /// The single exit for the end-of-game event. BaseMiniGame calls this once,
    /// when its outro finishes — nothing else raises OnEndedGame or MinigameFinished.
    /// </summary>
    void IMinigameContext.NotifyGameEnded(Enum_MiniGameType type, MinigameOutcome outcome)
    {
        Debug.Log($"[MinigameSystemManager] {type} finished: {outcome}");
        MinigameFinished?.Invoke(type, outcome);   // flow layer (typed)
        OnEndedGame?.Invoke();                     // scene listeners (designer-facing)
    }

    /// <summary>
    /// Cancels the active minigame — UI close button, or the flow layer backing out of step 2.2.
    /// The outro still plays; <see cref="MinigameFinished"/> then reports
    /// <see cref="MinigameOutcome.Cancelled"/> exactly once.
    /// </summary>
    public void CancelMinigame()
    {
        if (_activeMinigame == null)
        {
            Debug.LogWarning("[MinigameSystemManager] No active minigame to cancel.");
            return;
        }
        _activeMinigame.Cancel();
    }


    // ── Public API ─────────────────────────────────────────

    /// <summary>
    /// Starts the minigame registered under <paramref name="type"/>.
    /// OCP: this single method handles all current and future game types.
    /// </summary>
    public void StartMinigame(Enum_MiniGameType type)
    {
        if (!_registry.TryGetValue(type, out var game))
        {
            Debug.LogWarning($"[MinigameSystemManager] No game registered for type '{type}'.");
            return;
        }

        SwitchTo(game);
        game.StartGame();
        MinigameStarted?.Invoke(type);   // flow layer (typed)
        OnStartedMinigame?.Invoke();     // scene listeners (designer-facing)
    }

    /// <summary>
    /// Returns the IMinigame registered under <paramref name="type"/>.
    /// Callers (e.g. GameLoopManager) subscribe to OnGameEnd
    /// through the interface — no concrete cast needed.
    /// </summary>
    public IMinigame GetMinigame(Enum_MiniGameType type)
    {
        _registry.TryGetValue(type, out var game);
        return game;
    }

    // ── Backwards-compatible convenience methods ───────────
    // These exist so existing Yarn commands / UI buttons keep working.
    // They are thin wrappers — no logic duplication.

    public void StartShakingMinigame() => StartMinigame(Enum_MiniGameType.Shaking);
    public void StartMixingMinigame() => StartMinigame(Enum_MiniGameType.Stiring);

    // Kept for GameLoopManager — returns typed ref through interface.
    public IMinigame GetShakingMinigame() => GetMinigame(Enum_MiniGameType.Shaking);
    public IMinigame GetMixingMinigame() => GetMinigame(Enum_MiniGameType.Stiring);

    // ── Private ────────────────────────────────────────────

    private void SwitchTo(BaseMiniGame next)
    {
        if (next == null || ReferenceEquals(next, _activeMinigame)) return;

        // Never leave a half-played game ticking in the background.
        if (_activeMinigame != null && _activeMinigame.Phase != MinigamePhase.Idle)
            _activeMinigame.Cancel();

        _activeMinigame = next;
    }

#if UNITY_EDITOR
    private void HandleEditorHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTo(_registry.GetValueOrDefault(Enum_MiniGameType.Shaking));
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTo(_registry.GetValueOrDefault(Enum_MiniGameType.Stiring));
        else if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.B)) _activeMinigame?.EndGame();
        else if (Input.GetKeyDown(KeyCode.V)) _activeMinigame?.StartGame();
    }
#endif
}