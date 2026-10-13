using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static E_Cocktail;

public class MinigameSystemManager : MonoBehaviour, IMinigameContext
{
    // ── Inspector ──────────────────────────────────────────

    [Header("Dependencies")]
    [SerializeField] private CameraController _cocktailCamera;

    [Header("Selection")]
    [Tooltip("Runs when nothing was selected and the drink has no preparation method yet.")]
    [SerializeField] private Enum_MiniGameType _defaultType = Enum_MiniGameType.Shaking;

    [Header("Buttons (this manager's own canvas)")]
    [Tooltip("Leave the minigame and keep the drink: the 'BTN - Cancel Minigame' button of each " +
             "minigame canvas.")]
    [SerializeField] private Button[] _btnBack;

    // ── Game-flow seam (HSM) ───────────────────────────────
    // MinigameFlowBridge (see Bar410_Minigame_Integration_Plan.md §3.3) is the only listener.
    //
    //   MinigameState.OnStartRequested(type) ──> StartMinigame(Resolve(type, ...))
    //   MinigameFinished(type, outcome)      ──> MinigameState.ReportResult(outcome)
    //   MinigameState.OnStopRequested        ──> CancelMinigame()
    //   BackRequested                        ──> GameFlowCommands.AnotherIngredient()
    //
    // Plain C# events, not UnityEvents: the flow layer subscribes in code and must not be
    // re-pointable from the Inspector.

    /// <summary>
    /// Raised once per run, when the outro has finished.
    /// Carries the type of the game that actually ended — not necessarily <see cref="ActiveType"/>,
    /// since the manager may already have switched away.
    /// </summary>
    public event Action<Enum_MiniGameType, MinigameOutcome> MinigameFinished;

    /// <summary>Raised when a run begins, before the intro plays.</summary>
    public event Action<Enum_MiniGameType> MinigameStarted;

    /// <summary>The player pressed a back button: leave the minigame, keep the drink.</summary>
    public event Action BackRequested;

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

    // The buttons are wired here, not in the flow layer: the manager owns its canvas and only
    // reports which one was pressed. What a press means is decided by whoever listens.
    private void OnEnable()
    {
        if (_btnBack == null) return;

        foreach (var btn in _btnBack)
            if (btn != null) btn.onClick.AddListener(OnBackClicked);
    }

    private void OnDisable()
    {
        if (_btnBack == null) return;

        foreach (var btn in _btnBack)
            if (btn != null) btn.onClick.RemoveListener(OnBackClicked);
    }

    private void OnBackClicked() => BackRequested?.Invoke();

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
        => _cocktailCamera?.ResetView();

    /// <summary>
    /// The single exit for the end-of-game event. BaseMiniGame calls this once,
    /// when its outro finishes — nothing else raises OnEndedGame or MinigameFinished.
    /// </summary>
    void IMinigameContext.NotifyGameEnded(Enum_MiniGameType type, MinigameOutcome outcome)
    {
        Debug.Log($"[MinigameSystemManager] {type} finished: {outcome}");
        MinigameFinished?.Invoke(type, outcome);
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
        MinigameStarted?.Invoke(type);
    }

    /// <summary>
    /// Which minigame to run: an explicit selection wins, then the one the drink's own
    /// preparation method calls for, then <see cref="_defaultType"/>. The caller supplies both
    /// inputs — the selection is flow state and the method belongs to the cocktail.
    /// </summary>
    public Enum_MiniGameType Resolve(Enum_MiniGameType selected, Enum_MiniGameType fromDrink)
    {
        if (selected != Enum_MiniGameType.None) return selected;
        return fromDrink != Enum_MiniGameType.None ? fromDrink : _defaultType;
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
    // Shift + key only. The bare number row belongs to GameFlowDebugHotkeys and R to
    // Demo_KeysShortCut, so a single press used to fire two unrelated tools.
    private void HandleEditorHotkeys()
    {
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTo(_registry.GetValueOrDefault(Enum_MiniGameType.Shaking));
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTo(_registry.GetValueOrDefault(Enum_MiniGameType.Stiring));
        else if (Input.GetKeyDown(KeyCode.B)) _activeMinigame?.EndGame();
        else if (Input.GetKeyDown(KeyCode.V)) _activeMinigame?.StartGame();
    }
#endif
}