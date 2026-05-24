// ============================================================
//  MinigameSystemManager.cs
//
//  SOLID — O (Open / Closed):
//    Adding a new minigame type requires ONLY:
//      1. Add a new BaseMiniGame component to this GameObject.
//      2. Set its GameType property.
//    This class never changes. No new fields, no new Start*()
//    methods, no new switch cases.
//    All games are discovered at runtime via GetComponents<BaseMiniGame>()
//    and stored in a Dictionary<Enum_MiniGameType, BaseMiniGame>.
//
//  SOLID — D (Dependency Inversion):
//    Implements IMinigameContext so BaseMiniGame can call
//    ResetCamera() and NotifyGameEnded() without knowing this
//    concrete type exists.
//    External subscribers (GameLoopManager) get IMinigame via
//    GetMinigame(type) — they never need a concrete cast.
//
//  SOLID — I (Interface Segregation):
//    IMinigameContext is implemented explicitly (explicit interface
//    members) so callers going through IMinigameContext only see
//    those two methods — not the full manager surface.
//
//  SOLID — S (Single Responsibility):
//    Orchestrates which game is active and routes the Update tick.
//    Camera reset and end notification are forwarded — not owned.
// ============================================================

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

    // ── IMinigameContext (explicit — keeps the manager surface clean) ──

    void IMinigameContext.ResetCamera()
        => _cocktailCamera?.ResetRotaionAndMovement();

    void IMinigameContext.NotifyGameEnded()
        => OnEndedGame?.Invoke();

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
        OnStartedMinigame?.Invoke();
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
        _activeMinigame?.SetState(MiniGameState.Standby);
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