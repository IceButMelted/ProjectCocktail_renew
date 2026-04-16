// ============================================================
//  BaseMiniGame — fixed
// ============================================================
using NUnit.Framework.Constraints;
using System;
using UnityEngine;
using Yarn.Unity.Editor;

using static E_Cocktail;

public abstract class BaseMiniGame : MonoBehaviour, IMinigame
{
    [field: SerializeField]
    public SO_MinigameSetting Setting { get; set; }

    private CameraController _camera;
    [SerializeField] protected RectTransform _minigamePanel;

    [Header("Slide Settings")]
    [SerializeField] private float _slidePanelSpeed = 800f;

    public bool IsRunning { get; protected set; }
    public MiniGameState CurrentState { get; protected set; } = MiniGameState.Standby;

    public event Action<bool> OnGameEnd;

    protected bool IsClickedThisFrame { get; private set; }

    public void Initialize(CameraController cam)
    {
        _camera = cam;
        IsRunning = false;
    }

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


    public virtual void StartGame()
    {
        IsRunning = true;
        _camera.ResetRotaionAndMovement();
        SetState(MiniGameState.Standby); // Init first, then Processing
        SetState(MiniGameState.Processing);
    }
    public virtual void ProcessedGame()
    {
        IsClickedThisFrame = Input.GetMouseButtonDown(0);
        if (!IsRunning) return;
    }
    public virtual void UpdateUI() { }
    public virtual void EndGame()
    {
        IsRunning = false;
        _camera.SetCanRotateCamera(true);
    }

    

    public virtual string GetGameState()
        => $"{GetType().Name} | State: {CurrentState} | Running: {IsRunning}";

    protected void FireEndEvent(bool success)
    {
        EndGame();
        OnGameEnd?.Invoke(success); 
    }

    protected virtual void ResetGame()
    {
        IsRunning = false;
        CurrentState = MiniGameState.Standby;
        IsClickedThisFrame = false;
    }

    /// <summary>
    /// Maps a Direction enum to a normalized 2D movement vector.
    /// </summary>
    private static Vector2 DirectionToVector(Direction dir) => dir switch
    {
        Direction.Left => Vector2.left,
        Direction.Right => Vector2.right,
        Direction.Up => Vector2.up,
        Direction.Down => Vector2.down,
        _ => Vector2.zero
    };

    /// <summary>
    /// Returns the panel's rect in its parent's local space.
    /// Uses GetWorldCorners → InverseTransformPoint so it works
    /// at any canvas scale mode / resolution.
    /// </summary>
    private Rect GetPanelRectInParent()
    {
        Vector3[] corners = new Vector3[4];
        _minigamePanel.GetWorldCorners(corners);

        RectTransform parent = _minigamePanel.parent as RectTransform;
        for (int i = 0; i < 4; i++)
            corners[i] = parent.InverseTransformPoint(corners[i]);

        // corners: [0]=BL [1]=TL [2]=TR [3]=BR
        float xMin = corners[0].x;
        float yMin = corners[0].y;
        float xMax = corners[2].x;
        float yMax = corners[2].y;
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    /// <summary>
    /// Returns the parent container's rect (the "screen" boundary).
    /// </summary>
    private Rect GetParentRect()
    {
        return (_minigamePanel.parent as RectTransform).rect;
    }

    /// <summary>
    /// Checks the three completion conditions shared by both overloads.
    /// </summary>
    private (bool fullyIn, bool fullyOut) CheckBoundaryConditions()
    {
        Rect panel = GetPanelRectInParent();
        Rect screen = GetParentRect();

        bool fullyIn = panel.xMin >= screen.xMin &&
                       panel.xMax <= screen.xMax &&
                       panel.yMin >= screen.yMin &&
                       panel.yMax <= screen.yMax;

        bool fullyOut = !panel.Overlaps(screen);

        return (fullyIn, fullyOut);
    }

    protected enum FinishCondition
    {
        FullyIn,
        FullyOut
    }

    /// <summary>
    /// Call every frame (e.g. from Update / ProcessedGame).
    /// Returns true the frame one of the finish conditions is met.
    /// </summary>
    protected virtual bool SlideMinigame(Direction dir)
    {
        _minigamePanel.anchoredPosition += DirectionToVector(dir) * _slidePanelSpeed * Time.deltaTime;

        var (fullyIn, fullyOut) = CheckBoundaryConditions();

        return fullyIn || fullyOut;
    }

    protected virtual bool SlideMinigame(Direction dir, FinishCondition finishCondition)
    {
        _minigamePanel.anchoredPosition += DirectionToVector(dir) * _slidePanelSpeed * Time.deltaTime;
        Vector2 pos = _minigamePanel.anchoredPosition;
        
        var (fullyIn, fullyOut) = CheckBoundaryConditions();

        switch (finishCondition)
        {
            case FinishCondition.FullyIn: return fullyIn;
            case FinishCondition.FullyOut: return fullyOut;
            default: return false;
        }

    }

    /// <summary>
    /// Call every frame. <paramref name="pos"/> is in the panel's
    /// parent local space (anchoredPosition coordinates).
    /// Returns true the frame one of the finish conditions is met.
    /// </summary>
    protected virtual bool SlideMinigame(Direction dir, Vector2 pos)
    {
        Vector2 before = _minigamePanel.anchoredPosition;
        _minigamePanel.anchoredPosition += DirectionToVector(dir) * _slidePanelSpeed * Time.deltaTime;
        Vector2 after = _minigamePanel.anchoredPosition;

        // Has the panel's anchoredPosition crossed or reached the target
        // along the relevant axis in the direction of travel?
        bool reachedTarget = dir switch
        {
            Direction.Left => before.x >= pos.x && after.x <= pos.x, // crossed leftward
            Direction.Right => before.x <= pos.x && after.x >= pos.x, // crossed rightward
            Direction.Up => before.y <= pos.y && after.y >= pos.y, // crossed upward
            Direction.Down => before.y >= pos.y && after.y <= pos.y, // crossed downward
            _ => false
        };

        // Snap to target if we've reached it so callers get a clean final position
        if (reachedTarget)
        {
            Vector2 snapped = after;
            switch (dir)
            {
                case Direction.Left:
                case Direction.Right: snapped.x = pos.x; break;
                case Direction.Up:
                case Direction.Down: snapped.y = pos.y; break;
            }
            _minigamePanel.anchoredPosition = snapped;
        }

        //var (fullyIn, fullyOut) = CheckBoundaryConditions();

        return reachedTarget;
    }
}