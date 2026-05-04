using System;
using UnityEngine;
using UnityEngine.Events;
using static E_Cocktail;

/// <summary>
/// Abstract base for all minigames. Handles state, input polling, panel slide, and camera.
/// Derived classes override ProcessedGame() for their specific gameplay loop.
/// </summary>
public abstract class BaseMiniGame : MonoBehaviour, IMinigame
{
    // ── Inspector ─────────────────────────────────────────
    [field: SerializeField]
    public SO_MinigameSetting Setting { get; set; }

    [SerializeField] protected RectTransform _minigamePanel;

    [Header("Slide Settings")]
    [SerializeField] private float _slidePanelSpeed = 800f;

    // ── Public State ──────────────────────────────────────
    public bool IsRunning { get; protected set; }
    public MiniGameState CurrentState { get; protected set; } = MiniGameState.Standby;

    /// <summary>
    /// Fired when the minigame ends. True = success, false = failure.
    /// Use += / -= for subscription (safe across sessions).
    /// </summary>
    public event Action<bool> OnGameEnd;

    // ── Protected State ───────────────────────────────────
    /// <summary>True only on the frame the player left-clicked.</summary>
    protected bool IsClickedThisFrame { get; private set; }

    // ── Private ───────────────────────────────────────────
    private CameraController _camera;
    private CocktailSystemManager _cocktailSystem;
    private MinigameSystemManager _minigameSystemManager;

    // ── Initialization ────────────────────────────────────
    public void Initialize(CameraController cam, CocktailSystemManager system, MinigameSystemManager minigameSystemManager)
    {
        _camera = cam;
        _cocktailSystem = system;
        _minigameSystemManager = minigameSystemManager;
        IsRunning = false;
    }

    // ── State Machine ─────────────────────────────────────
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

    protected virtual void OnProcessing() { }
    protected virtual void OnSuccess() => FireEndEvent(true);
    protected virtual void OnStandby() => ResetGame();

    // ── IMinigame ─────────────────────────────────────────
    public virtual void StartGame()
    {
        IsRunning = true;
        _camera?.ResetRotaionAndMovement();
        SetState(MiniGameState.Standby);
        SetState(MiniGameState.Processing);
    }

    /// <summary>
    /// Called every frame by MinigameSystemManager.
    /// Base polls mouse input — call base.ProcessedGame() first in overrides.
    /// </summary>
    public virtual void ProcessedGame()
        => IsClickedThisFrame = Input.GetMouseButtonDown(0);

    public virtual void UpdateUI() { }

    public virtual void EndGame()
    {
        IsRunning = false;
        _minigameSystemManager?.OnEndedGame?.Invoke();
       
    }

    public virtual string GetGameState()
        => $"{GetType().Name} | State: {CurrentState} | Running: {IsRunning}";

    // ── Helpers ───────────────────────────────────────────
    protected void FireEndEvent(bool success)
    {
        EndGame();
        OnGameEnd?.Invoke(success);
    }

    protected virtual void ResetGame()
        => IsClickedThisFrame = false;

    // ── Panel Slide ───────────────────────────────────────
    protected enum FinishCondition { FullyIn, FullyOut }

    private static Vector2 DirectionToVector(Direction dir) => dir switch
    {
        Direction.Left => Vector2.left,
        Direction.Right => Vector2.right,
        Direction.Up => Vector2.up,
        Direction.Down => Vector2.down,
        _ => Vector2.zero
    };

    private Rect GetPanelRectInParent()
    {
        Vector3[] corners = new Vector3[4];
        _minigamePanel.GetWorldCorners(corners);

        var parent = _minigamePanel.parent as RectTransform;
        for (int i = 0; i < 4; i++)
            corners[i] = parent.InverseTransformPoint(corners[i]);

        return new Rect(corners[0].x, corners[0].y,
                        corners[2].x - corners[0].x,
                        corners[2].y - corners[0].y);
    }

    private Rect GetParentRect() => (_minigamePanel.parent as RectTransform).rect;

    private (bool fullyIn, bool fullyOut) CheckBoundary()
    {
        Rect panel = GetPanelRectInParent();
        Rect screen = GetParentRect();
        bool fullyIn = panel.xMin >= screen.xMin && panel.xMax <= screen.xMax &&
                         panel.yMin >= screen.yMin && panel.yMax <= screen.yMax;
        bool fullyOut = !panel.Overlaps(screen);
        return (fullyIn, fullyOut);
    }

    /// <summary>Moves the panel per frame; returns true once finish condition is met.</summary>
    protected bool SlideMinigame(Direction dir, FinishCondition finishCondition)
    {
        _minigamePanel.anchoredPosition += DirectionToVector(dir) * _slidePanelSpeed * Time.deltaTime;
        var (fullyIn, fullyOut) = CheckBoundary();
        return finishCondition == FinishCondition.FullyIn ? fullyIn : fullyOut;
    }

    /// <summary>Moves the panel toward a target position; returns true when it crosses that point.</summary>
    protected bool SlideMinigame(Direction dir, Vector2 targetPosition)
    {
        Vector2 before = _minigamePanel.anchoredPosition;
        _minigamePanel.anchoredPosition += DirectionToVector(dir) * _slidePanelSpeed * Time.deltaTime;
        Vector2 after = _minigamePanel.anchoredPosition;

        bool crossed = dir switch
        {
            Direction.Left => before.x >= targetPosition.x && after.x <= targetPosition.x,
            Direction.Right => before.x <= targetPosition.x && after.x >= targetPosition.x,
            Direction.Up => before.y <= targetPosition.y && after.y >= targetPosition.y,
            Direction.Down => before.y >= targetPosition.y && after.y <= targetPosition.y,
            _ => false
        };

        if (crossed)
        {
            Vector2 snapped = after;
            if (dir == Direction.Left || dir == Direction.Right) snapped.x = targetPosition.x;
            if (dir == Direction.Up || dir == Direction.Down) snapped.y = targetPosition.y;
            _minigamePanel.anchoredPosition = snapped;
        }

        return crossed;
    }
}