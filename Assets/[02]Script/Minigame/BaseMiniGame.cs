using System;
using UnityEngine;
using System.Collections.Generic;
using static E_Cocktail;
using System.Linq;
using UnityEngine.TestTools;

public abstract class BaseMiniGame : MonoBehaviour, IMinigame
{
    // ── Inspector ──────────────────────────────────────────

    [SerializeField]private MinigameSystemManager m_systemManager;
    [SerializeField]protected AnimationMinigameController amc; //<<Anim Minigame Controller

    [field: SerializeField]
    public SO_MinigameSetting Setting { get; set; }

    

    [Header("Slide Settings")]
    [SerializeField] private float _slidePanelSpeed = 800f;

    // ── IMinigame — Public State ───────────────────────────

    public bool IsRunning { get; protected set; }

    public event Action<bool> OnGameEnd;

    [SerializeField] protected float EasingConfigTimer = 1.2f;

    [Header("Art Works")]
    [SerializeField] protected RectTransform _minigamePanel;
    [SerializeField] protected List<RectTransform> OpenPanel;
    private int openPanelDoneCount = 0;
    [SerializeField] protected RectTransform BackgroundPanelgame;
    [SerializeField] protected List<RectTransform> ArtWorks;
    private int _currentArtIndex;
    [SerializeField] protected RectTransform ButtonPanel;

    // ── Slide Phase ────────────────────────────────────────

    protected enum SlidePhase
    {
        None, InitPanel, InitPanelExit, InitBackground, InitArt, InitMinigame, RemoveMinigame, Closing
    }
    protected SlidePhase CurrentSlidePhase = SlidePhase.None;
    public void ClosePanel() => CurrentSlidePhase = SlidePhase.Closing;

    // Store initial panel position for reset after slide-out.
    protected RectTransform InitPanelRectTransform;

    // ── Slide Sessions (one per independently-animated panel) ─

    /// <summary>Session for the main minigame panel.</summary>
    protected SlideSession[] OpenPanelSession;
    protected SlideSession MinigamePanelSession;
    protected SlideSession BackgroundSession;
    protected SlideSession[] ArtWorkSessions;
    protected SlideSession ButtonPanelSessions;

    private bool _backgroundSnapped = false;
    private bool _closingSnapApplied = false;




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
        OpenPanelSession = new SlideSession[OpenPanel?.Count ?? 0];
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
    /// Routes each frame to the correct phase handler.
    /// Each handler is self-contained: reads state, mutates only what it owns,
    /// and advances CurrentSlidePhase when its work is done.
    /// </summary>
    protected virtual void SlidePanelMinigame()
    {
        switch (CurrentSlidePhase)
        {
            case SlidePhase.InitPanel: SlidePhase_InitPanel(); break;
            case SlidePhase.InitArt: SlidePhase_InitArt(); break;
            case SlidePhase.InitMinigame: SlidePhase_InitMinigame(); break;
            case SlidePhase.InitPanelExit: SlidePhase_InitPanelExit(); break;
            case SlidePhase.RemoveMinigame: SlidePhase_RemoveMinigame(); break;
            case SlidePhase.Closing: SlidePhase_Closing(); break;
            case SlidePhase.None:
            default:
                break;
        }
    }

    // ── Phase: InitPanel ───────────────────────────────────────
    // Phase 1: Slide panels up to center (visible to user)
    private void SlidePhase_InitPanel()
    {
        int count = OpenPanel.Count;
        int doneCount = 0;
        var easing = EasingConfig.EaseIn(EasingConfigTimer);

        for (int i = 0; i < count; i++)
        {
            if (PanelSlider.Slide(ref OpenPanelSession[i], OpenPanel[i],
                    Direction.Up, SlideFinishCondition.TopEdgeToCenter,
                    _slidePanelSpeed, easing))
                doneCount++;
        }

        // Trigger background snap at 75% — now this actually fires mid-animation

            


        if (doneCount < count) return; // wait until all reach center

        PanelSlider.SnapTo(ref BackgroundSession, BackgroundPanelgame,
                SlideFinishCondition.BottomEdgeToBottomBound);

        CurrentSlidePhase = SlidePhase.InitPanelExit; // ← hand off to next phase
    }

    // Phase 2: Slide panels out the top, snap background
    private void SlidePhase_InitPanelExit()
    {
        int count = OpenPanel.Count;
        int doneCount = 0;
        var easing = EasingConfig.EaseIn(EasingConfigTimer);

        for (int i = 0; i < count; i++)
        {
            if (PanelSlider.Slide(ref OpenPanelSession[i], OpenPanel[i],
                    Direction.Up, SlideFinishCondition.TopEdgeToBottomBound,
                    _slidePanelSpeed, easing))
                doneCount++;
        }

        

        if (doneCount < count) return; // wait until all exit

        _backgroundSnapped = false;
        openPanelDoneCount = 0;
        Debug.Log("Got in art init");
        CurrentSlidePhase = SlidePhase.InitArt;
    }

    // ── Phase: InitArt ─────────────────────────────────────────
    private void SlidePhase_InitArt()
    {
        if (!PanelSlider.Slide(ref ArtWorkSessions[_currentArtIndex], ArtWorks[_currentArtIndex],
                Direction.Up, SlideFinishCondition.BottomEdgeToBottomBound,
                _slidePanelSpeed, EasingConfig.Bounce(EasingConfigTimer / 2)))
            return; // current artwork still animating

        _currentArtIndex++;

        if (_currentArtIndex < ArtWorks.Count) return; // wait for next frame

        _currentArtIndex = 0; // reset for next run
        CurrentSlidePhase = SlidePhase.InitMinigame;
    }

    // ── Phase: InitMinigame ────────────────────────────────────
    private void SlidePhase_InitMinigame()
    {
        if (!PanelSlider.Slide(ref MinigamePanelSession, _minigamePanel,
                Direction.Up, SlideFinishCondition.BottomEdgeToBottomBound,
                _slidePanelSpeed, EasingConfig.Bounce(EasingConfigTimer)))
            return;

        //--- Remove Button from minbigame---
        //PanelSlider.SnapTo(ref ButtonPanelSessions, ButtonPanel,
        //    SlideFinishCondition.BottomEdgeToBottomBound);

        int count = OpenPanel.Count;
        for (int i = 0; i < count; i++)
            PanelSlider.SnapTo(ref OpenPanelSession[i], OpenPanel[i],
                SlideFinishCondition.TopEdgeToBottomBound);

        CurrentSlidePhase = SlidePhase.None;
        IsRunning = true; // now that the minigame panel is fully in, we can start processing input
    }

    // ── Phase: RemoveMinigame ──────────────────────────────────
    private void SlidePhase_RemoveMinigame()
    {
        SetState(MiniGameState.Success);
        bool minigameDone = PanelSlider.Slide(ref MinigamePanelSession, _minigamePanel,
                Direction.Down, SlideFinishCondition.TopEdgeToBottomBound,
                _slidePanelSpeed, EasingConfig.EaseInOut(EasingConfigTimer));

        int artCount = ArtWorks.Count;
        for (int i = 0; i < artCount; i++)
            PanelSlider.SnapTo(ref ArtWorkSessions[i], ArtWorks[i],
                SlideFinishCondition.TopEdgeToBottomBound);

        //bool minigameRemoving = PanelSlider.Slide(ref MinigamePanelSession, _minigamePanel,
        //        Direction.Down, SlideFinishCondition.LeftEdgeToLeftBound,
        //        _slidePanelSpeed, EasingConfig.Bounce(EasingConfigTimer));

        bool RemoveBG = PanelSlider.Slide(ref BackgroundSession, BackgroundPanelgame,
                Direction.Down, SlideFinishCondition.TopEdgeToBottomBound, _slidePanelSpeed,
                    EasingConfig.Bounce(EasingConfigTimer));

        if (!minigameDone || !RemoveBG) return; // wait for animation

        //PanelSlider.SnapTo(ref BackgroundSession, BackgroundPanelgame,
        //    SlideFinishCondition.TopEdgeToBottomBound);

        

        CurrentSlidePhase = SlidePhase.None;
        m_systemManager.OnEndedGame?.Invoke();

    }

    // ── Phase: Closing ─────────────────────────────────────────
    private void SlidePhase_Closing()
    {
        // One-shot: remove artworks before the slide-out begins
        if (!_closingSnapApplied)
        {
            //int artCount = ArtWorks.Count;
            ////--- OldCode ---
            ////for (int i = 0; i < artCount; i++)
            ////    PanelSlider.SnapTo(ref ArtWorkSessions[i], ArtWorks[i],
            ////        SlideFinishCondition.TopEdgeToBottomBound);


            //for (int i = 0; i < artCount; i++)
            //    PanelSlider.SnapTo(ref ArtWorkSessions[i], ArtWorks[i],
            //        SlideFinishCondition.TopEdgeToBottomBound);

            //SetState(MiniGameState.Success);
            //bool minigameRemoving = PanelSlider.Slide(ref MinigamePanelSession, _minigamePanel,
            //        Direction.Left, SlideFinishCondition.LeftEdgeToLeftBound,
            //        _slidePanelSpeed, EasingConfig.Bounce(EasingConfigTimer));

            //bool RemoveBG = PanelSlider.Slide(ref BackgroundSession, BackgroundPanelgame, 
            //        Direction.Down, SlideFinishCondition.TopEdgeToBottomBound, _slidePanelSpeed, 
            //            EasingConfig.Bounce(EasingConfigTimer));


            //if(RemoveBG && minigameRemoving)
            //    _closingSnapApplied = true;
            //else
            //    return;
        }

        var closingEase = EasingConfig.EaseIn(EasingConfigTimer / 2); // cache — used twice

        //bool minigameDone = PanelSlider.Slide(ref MinigamePanelSession, _minigamePanel,
        //        Direction.Left, SlideFinishCondition.RightEdgeToLeftBound,
        //        _slidePanelSpeed, closingEase);

        //bool buttonDone = PanelSlider.Slide(ref ButtonPanelSessions, ButtonPanel,
        //        Direction.Left, SlideFinishCondition.RightEdgeToLeftBound,
        //        _slidePanelSpeed, closingEase);

        //if (!minigameDone || !buttonDone) return; // wait for both

        int artCount = ArtWorks.Count;
        //--- OldCode ---
        //for (int i = 0; i < artCount; i++)
        //    PanelSlider.SnapTo(ref ArtWorkSessions[i], ArtWorks[i],
        //        SlideFinishCondition.TopEdgeToBottomBound);


        for (int i = 0; i < artCount; i++)
            PanelSlider.SnapTo(ref ArtWorkSessions[i], ArtWorks[i],
                SlideFinishCondition.TopEdgeToBottomBound);

        bool minigameRemoving = PanelSlider.Slide(ref MinigamePanelSession, _minigamePanel,
                Direction.Down, SlideFinishCondition.LeftEdgeToLeftBound,
                _slidePanelSpeed, EasingConfig.Bounce(EasingConfigTimer));

        bool RemoveBG = PanelSlider.Slide(ref BackgroundSession, BackgroundPanelgame,
                Direction.Down, SlideFinishCondition.TopEdgeToBottomBound, _slidePanelSpeed,
                    EasingConfig.Bounce(EasingConfigTimer));


        //if (RemoveBG && minigameRemoving)
        //    _closingSnapApplied = true;
        //else
        //    return;

        if (!minigameRemoving || !RemoveBG) return;

        // Reset vertical position (off-screen above)
        PanelSlider.SnapTo(ref MinigamePanelSession, _minigamePanel,
            SlideFinishCondition.TopEdgeToBottomBound);
        PanelSlider.SnapTo(ref ButtonPanelSessions, ButtonPanel,
            SlideFinishCondition.TopEdgeToBottomBound);

        // Restore horizontal position so panels are ready for next run
        PanelSlider.SnapTo(ref MinigamePanelSession, _minigamePanel,
            SlideFinishCondition.RightEdgeToRightBound);
        PanelSlider.SnapTo(ref ButtonPanelSessions, ButtonPanel,
            SlideFinishCondition.RightEdgeToRightBound);

        // Full position restore from Inspector snapshot
        _minigamePanel.anchoredPosition = InitPanelRectTransform.anchoredPosition;

        _closingSnapApplied = false;
        CurrentSlidePhase = SlidePhase.None;

        m_systemManager.OnEndedGame?.Invoke();
    }

    // ── IMinigame ──────────────────────────────────────────

    public virtual void StartGame()
    {
        //IsRunning = true; >> move to OnProcessing to ensure it happens after slide-in completes
        _context?.ResetCamera();
        CurrentSlidePhase = SlidePhase.InitPanel;
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
        amc.StopAllCoroutines();
        //_context?.NotifyGameEnded();
    }

    public virtual string GetGameState()
        => $"{GetType().Name} | State: {CurrentState} | Running: {IsRunning}";

    // ── Helpers ────────────────────────────────────────────

    protected void FireEndEvent(bool success)
    {
        EndGame();
        OnGameEnd?.Invoke(success);
        //ani Minigamme Controller
        amc.StopAllCoroutines();
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