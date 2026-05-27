using System;
using UnityEngine;
using System.Collections.Generic;
using static E_Cocktail;

public abstract class BaseMiniGame : MonoBehaviour, IMinigame
{
    // ── Inspector ──────────────────────────────────────────

    [field: SerializeField]
    public SO_MinigameSetting Setting { get; set; }

    [SerializeField] protected RectTransform _minigamePanel;

    [Header("Slide Settings")]
    [SerializeField] private float _slidePanelSpeed = 800f;

    // ── IMinigame — Public State ───────────────────────────

    public bool IsRunning { get; protected set; }

    public event Action<bool> OnGameEnd;

    [SerializeField] protected float EasingConfigTimer = 1.2f;

    [Header("Art Works")]
    [SerializeField] protected RectTransform BackgroundMinigame;
    [SerializeField] protected List<RectTransform> ArtWorks;
    private int _currentArtIndex;
    [SerializeField] protected List<RectTransform> ArtButton;
    private int _currentButtonIndex;

    // ── Slide Phase ────────────────────────────────────────

    protected enum SlidePhase { None, BackgroundEntering, ArtEntering, MinigameEntering, MinigameExiting, ArtBTNEntering, ClosingToResult }
    protected SlidePhase CurrentSlidePhase = SlidePhase.None;
    public void ClosePanel() => CurrentSlidePhase = SlidePhase.ClosingToResult;

    // Store initial panel position for reset after slide-out.
    protected RectTransform InitPanelRectTransform;

    // ── Slide Sessions (one per independently-animated panel) ─

    /// <summary>Session for the main minigame panel.</summary>
    protected SlideSession PanelSession;
    private SlideSession BackgroundSession;
    protected SlideSession[] ArtWorkSessions;
    protected SlideSession[] ArtButtonSessions;

    // ── State Machine ──────────────────────────────────────

    /// <summary>
    /// Valid transitions:
    ///   Standby    → Processing  (StartGame)
    ///   Processing → Success     (win condition met)
    ///   Success    → Standby     (replay / manager resets)
    /// Any other transition is ignored with a warning.
    /// </summary>
    public MiniGameState CurrentState { get; protected set; } = MiniGameState.Standby;

    // ── Abstract ───────────────────────────────────────────

    public abstract Enum_MiniGameType GameType { get; }

    // ── Private Dependencies ───────────────────────────────

    private IMinigameContext _context;
    protected IInputProvider Input { get; private set; }

    // ── Unity Lifecycle ────────────────────────────────────

    protected virtual void Awake()
    {
        // Snapshot the initial panel position for post-slide reset.
        var go = new GameObject("InitPanelSnapshot");
        go.hideFlags = HideFlags.HideAndDontSave;
        InitPanelRectTransform = go.AddComponent<RectTransform>();
        CopyRectTransform(_minigamePanel, InitPanelRectTransform);

        // Pre-allocate session arrays to match list sizes.
        ArtWorkSessions = new SlideSession[ArtWorks?.Count ?? 0];
        ArtButtonSessions = new SlideSession[ArtButton?.Count ?? 0];
    }

    // ── Initialization ─────────────────────────────────────

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

        if (!IsValidTransition(CurrentState, newState))
        {
            Debug.LogWarning($"[{GetType().Name}] Invalid state transition: {CurrentState} → {newState}. Ignoring.");
            return;
        }

        MiniGameState previous = CurrentState;
        OnExitState(previous);
        CurrentState = newState;
        OnEnterState(newState);
    }

    /// <summary>
    /// Defines which transitions are legal.
    /// Extend this table when adding new states.
    /// </summary>
    private static bool IsValidTransition(MiniGameState from, MiniGameState to) => (from, to) switch
    {
        (MiniGameState.Standby, MiniGameState.Processing) => true,
        (MiniGameState.Processing, MiniGameState.Success) => true,
        (MiniGameState.Success, MiniGameState.Standby) => true,
        _ => false,
    };

    // ── Enter / Exit Hooks ─────────────────────────────────

    /// <summary>Called just before CurrentState changes. Clean up the old state here.</summary>
    protected virtual void OnExitState(MiniGameState exitingState) { }

    /// <summary>Called just after CurrentState changes. Set up the new state here.</summary>
    protected virtual void OnEnterState(MiniGameState enteredState)
    {
        switch (enteredState)
        {
            case MiniGameState.Processing: OnProcessing(); break;
            case MiniGameState.Success: OnSuccess(); break;
            case MiniGameState.Standby: OnStandby(); break;
        }
    }

    // ── Protected Hooks (override in subclasses) ───────────

    protected virtual void OnProcessing() { }
    protected virtual void OnSuccess() => FireEndEvent(true);
    protected virtual void OnFailed() => FireEndEvent(false);
    protected virtual void OnStandby() => ResetGame();

    // ── Slide Panel ────────────────────────────────────────

    /// <summary>
    /// Sequences the slide-in and slide-out animations based on the current SlidePhase.
    /// start with BackgroundEntering to slide in the background, then ArtEntering for the artwork,
    /// MinigameEntering for the minigame panel, MinigameExiting to slide out the minigame panel,
    /// and ClosingToResult to slide down and reset the panel position.
    /// </summary>
    protected virtual void SlidePanelMinigame()
    {
        switch (CurrentSlidePhase)
        {
            case SlidePhase.BackgroundEntering:
                Debug.Log("Sliding in background...");
                if (!PanelSlider.Slide(ref BackgroundSession, BackgroundMinigame,
                        Direction.Up, SlideFinishCondition.BottomEdgeToBottomBound,
                        _slidePanelSpeed, EasingConfig.EaseOut(EasingConfigTimer / 2))) return;
                CurrentSlidePhase = SlidePhase.ArtEntering;
                break;

            case SlidePhase.ArtEntering:
                Debug.Log($"Sliding in art");
                if (!PanelSlider.Slide(ref ArtWorkSessions[_currentArtIndex], ArtWorks[_currentArtIndex],
                        Direction.Up, SlideFinishCondition.BottomEdgeToBottomBound,
                        _slidePanelSpeed, EasingConfig.EaseInOut(EasingConfigTimer / 2))) return;

                _currentArtIndex++;

                if (_currentArtIndex < ArtWorks.Count) return; // wait for next frame to slide next art

                _currentArtIndex = 0; // reset for next time
                CurrentSlidePhase = SlidePhase.MinigameEntering;
                break;

            // ── MinigameEntering: slide in from below ─────────────
            case SlidePhase.MinigameEntering:
                Debug.Log("Sliding in minigame panel...");
                if (!PanelSlider.Slide(ref PanelSession, _minigamePanel,
                        Direction.Up, SlideFinishCondition.BottomEdgeToBottomBound,
                        _slidePanelSpeed, EasingConfig.EaseInOut(EasingConfigTimer))) return;
                CurrentSlidePhase = SlidePhase.None;
                break;

            // ── MinigameExiting: slide left, then fire Success ─────
            case SlidePhase.MinigameExiting:
                Debug.Log("Sliding out minigame panel...");

                bool minigameDone = PanelSlider.Slide(ref PanelSession, _minigamePanel,
                        Direction.Left, SlideFinishCondition.LeftEdgeToLeftBound,
                        _slidePanelSpeed, EasingConfig.EaseInOut(EasingConfigTimer));
                if (!minigameDone) return;

                CurrentSlidePhase = SlidePhase.ArtBTNEntering;
                SetState(MiniGameState.Success);
                break;


             case SlidePhase.ArtBTNEntering:
                Debug.Log($"Sliding in art buttons");

                if (!PanelSlider.Slide(ref ArtButtonSessions[_currentButtonIndex], ArtButton[_currentButtonIndex],
                        Direction.Up, SlideFinishCondition.BottomEdgeToBottomBound,
                        _slidePanelSpeed, EasingConfig.EaseInOut(EasingConfigTimer / 2))) return;

                _currentButtonIndex++;
                if (_currentButtonIndex < ArtButton.Count) return; // wait for next frame to slide next button

                _currentButtonIndex = 0; // reset for next time

                CurrentSlidePhase = SlidePhase.None;

                //SetState(MiniGameState.Success);
                break;

            // ── ClosingToResult: slide down, then reset position ───
            case SlidePhase.ClosingToResult:
                Debug.Log("Closing panel...");
                bool panelDone = PanelSlider.Slide(ref PanelSession, _minigamePanel,
                        Direction.Down, SlideFinishCondition.TopEdgeToBottomBound,
                        _slidePanelSpeed, EasingConfig.EaseIn(EasingConfigTimer / 2));

                bool backgroundDone = PanelSlider.Slide(ref BackgroundSession, BackgroundMinigame,
                        Direction.Down, SlideFinishCondition.TopEdgeToBottomBound,
                        _slidePanelSpeed, EasingConfig.EaseIn(EasingConfigTimer / 2));

                int artDoneCount = 0;
                for (int i = 0; i < ArtWorks.Count; i++)
                {
                    bool done = PanelSlider.Slide(
                        ref ArtWorkSessions[i], ArtWorks[i],
                        Direction.Down, SlideFinishCondition.TopEdgeToBottomBound,
                        _slidePanelSpeed, EasingConfig.EaseIn(EasingConfigTimer / 2));
                    if (done) artDoneCount++;
                }

                int btnDoneCount = 0;
                for (int i = 0; i < ArtButton.Count; i++)
                {
                    bool done = PanelSlider.Slide(
                        ref ArtButtonSessions[i], ArtButton[i],
                        Direction.Down, SlideFinishCondition.TopEdgeToBottomBound,
                        _slidePanelSpeed, EasingConfig.EaseIn(EasingConfigTimer / 2));
                    if (done) btnDoneCount++;
                }

                if (!panelDone || !backgroundDone) return;
                if (artDoneCount < ArtWorks.Count) return;
                if (btnDoneCount < ArtButton.Count) return;


                CurrentSlidePhase = SlidePhase.None;
                
                _minigamePanel.anchoredPosition = InitPanelRectTransform.anchoredPosition;
                break;

            case SlidePhase.None:
                break;
            default:
                break;
        }
    }

    // ── IMinigame ──────────────────────────────────────────

    public virtual void StartGame()
    {
        IsRunning = true;
        _context?.ResetCamera();
        CurrentSlidePhase = SlidePhase.BackgroundEntering;
        SetState(MiniGameState.Standby);
        SetState(MiniGameState.Processing);
    }

    public virtual void ProcessedGame()
    {
        SlidePanelMinigame();


        if (!IsRunning) return;
        Input.Poll();
    }

    public virtual void UpdateUI() { }

    public virtual void EndGame()
    {
        IsRunning = false;
        //_context?.NotifyGameEnded();
    }

    public virtual string GetGameState()
        => $"{GetType().Name} | State: {CurrentState} | Running: {IsRunning}";

    // ── Helpers ────────────────────────────────────────────

    protected void FireEndEvent(bool success)
    {
        EndGame();
        OnGameEnd?.Invoke(success);
    }

    protected virtual void ResetGame() { }

    public void CopyRectTransform(RectTransform source, RectTransform destination)
    {
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.anchoredPosition = source.anchoredPosition;
        destination.sizeDelta = source.sizeDelta;
        destination.pivot = source.pivot;
    }

    
}