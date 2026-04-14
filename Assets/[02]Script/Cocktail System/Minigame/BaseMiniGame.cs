// ============================================================
//  BaseMiniGame — fixed
// ============================================================
using System;
using UnityEngine;
using Yarn.Unity.Editor;

public abstract class BaseMiniGame : MonoBehaviour, IMinigame
{
    [field: SerializeField]
    public SO_MinigameSetting Setting { get; set; }

    [SerializeField] private CameraController _camera;

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
}