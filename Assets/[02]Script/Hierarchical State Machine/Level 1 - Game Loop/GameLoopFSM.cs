using UnityEngine;

namespace Bar410.GameFlow
{
    // ── Level 1 Keys ───────────────────────────────────────

    /// <summary>Top-level day loop. Strictly linear: Prepare → Open → Close → Prepare.</summary>
    public enum GamePhase
    {
        Prepare,
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
    /// </summary>
    public class GameLoopFSM : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────

        [Header("Debug")]
        [SerializeField] private bool _logTransitions = true;

        [Tooltip("Enter the Prepare phase automatically on Start. Turn off if a bootstrap/save-load flow starts the day instead.")]
        [SerializeField] private bool _autoStart = true;

        // ── Public Access ──────────────────────────────────

        public StateMachine<GamePhase> Machine { get; } = new StateMachine<GamePhase>();

        public PrepareBarPhase PrepareBar { get; private set; }
        public OpenBarPhase OpenBar { get; private set; }
        public ClosingBarPhase ClosingBar { get; private set; }

        private bool _built;

        // ── Unity ──────────────────────────────────────────

        private void Awake()
        {
            EnsureBuilt();
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

        /// <summary>
        /// Builds the phase hierarchy if it does not exist yet. Idempotent, and safe to call
        /// from another component's Awake — component Awake order on one GameObject is not
        /// guaranteed, so anything needing the phases should call this first.
        /// </summary>
        public void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            Build();
        }

        private void Build()
        {
            PrepareBar = new PrepareBarPhase();
            OpenBar = new OpenBarPhase();
            ClosingBar = new ClosingBarPhase();

            // Each phase raises its own "I'm done" event; this level decides what that means.
            PrepareBar.OnRequestOpenBar += () => Machine.TryTransition(GamePhase.Open);
            OpenBar.OnRequestExitToClose += () => Machine.TryTransition(GamePhase.Close);
            ClosingBar.OnRequestNextDay += () => Machine.TryTransition(GamePhase.Prepare);

            Machine.AddState(GamePhase.Prepare, PrepareBar, new[] { GamePhase.Open });
            Machine.AddState(GamePhase.Open, OpenBar, new[] { GamePhase.Close });
            Machine.AddState(GamePhase.Close, ClosingBar, new[] { GamePhase.Prepare });

            if (_logTransitions)
                Machine.OnTransition += (from, to) => Debug.Log($"[GameLoop] {from} -> {to}");
        }

        /// <summary>Enters the first phase of the loop. Safe to call from a bootstrap flow.</summary>
        public void StartDay()
        {
            if (Machine.IsRunning) return;
            Machine.SetInitial(GamePhase.Prepare);
        }
    }
}
