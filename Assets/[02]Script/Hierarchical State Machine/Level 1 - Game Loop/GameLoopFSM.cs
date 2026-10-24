using UnityEngine;

namespace Bar410.GameFlow
{
    // ── Level 1 Keys ───────────────────────────────────────

    /// <summary>Top-level day loop. Strictly linear: Open → Close → Open.</summary>
    public enum GamePhase
    {
        Open,
        Close
    }

    // ── Level 1 Machine ────────────────────────────────────

    /// <summary>
    /// Owns the top-level <see cref="StateMachine{TKey}"/> and drives it from Update.
    /// No backward transitions exist in the table — that rule is structural, not a convention.
    ///
    /// Drop this on a single scene object (or a bootstrap prefab) and let gameplay code
    /// talk to the phase objects it exposes rather than poking the machine directly.
    ///
    /// The phases are built in Awake. The negative execution order puts that Awake ahead of
    /// every bridge and GameFlowCommands, so they can subscribe to <see cref="OpenBar"/> in
    /// their own Awake without a lazy-build call.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameLoopFSM : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────

        [Header("Debug")]
        [SerializeField] private bool _logTransitions = true;

        [Tooltip("Enter the Prepare phase automatically on Start. Turn off if a bootstrap/save-load flow starts the day instead.")]
        [SerializeField] private bool _autoStart = true;

        // ── Public Access ──────────────────────────────────

        public StateMachine<GamePhase> Machine { get; } = new StateMachine<GamePhase>();

        public OpenBarPhase OpenBar { get; private set; }
        public ClosingBarPhase ClosingBar { get; private set; }

        // ── Unity ──────────────────────────────────────────

        private void Awake()
        {
            Build();
        }

        private void Start()
        {
            if (_autoStart) StartDay();
        }

        private void Update()
        {
            Machine.Tick(Time.deltaTime);
        }

        // ── Setup ──────────────────────────────────────────

        private void Build()
        {
            OpenBar = new OpenBarPhase();
            ClosingBar = new ClosingBarPhase();

            // Each phase raises its own "I'm done" event; this level decides what that means.
            OpenBar.OnRequestExitToClose += () => Machine.TryTransition(GamePhase.Close);
            ClosingBar.OnRequestNextDay += () => Machine.TryTransition(GamePhase.Open);

            Machine.AddState(GamePhase.Open, OpenBar, new[] { GamePhase.Close });
            Machine.AddState(GamePhase.Close, ClosingBar, new[] { GamePhase.Open });

            if (_logTransitions)
                Machine.OnTransition += (from, to) => Debug.Log($"[GameLoop] {from} -> {to}");
        }

        /// <summary>Enters the first phase of the loop. Safe to call from a bootstrap flow.</summary>
        public void StartDay()
        {
            if (Machine.IsRunning) return;
            Machine.SetInitial(GamePhase.Open);
        }
    }
}
