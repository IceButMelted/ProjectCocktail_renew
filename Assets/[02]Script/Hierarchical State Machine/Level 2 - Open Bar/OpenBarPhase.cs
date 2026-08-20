using System;

namespace Bar410.GameFlow
{
    // ── Level 2 Keys ───────────────────────────────────────

    /// <summary>The 4-step Open Bar flow. Branching + backtracking, not linear.</summary>
    public enum OpenSub
    {
        TalkingWithCustomer,
        PrepareDrinks,
        Garnish,
        Serve
    }

    // ── Level 2 Machine ────────────────────────────────────

    /// <summary>
    /// Owns OpenBarFSM. Transition table:
    ///
    ///   1 TalkingWithCustomer -> 2, or exit to the next big phase via OnRequestExitToClose
    ///   2 PrepareDrinks       -> 3            (2.1/2.2 rules live inside PrepareDrinksPhase)
    ///   3 Garnish             -> 2, 4
    ///   4 Serve               -> 2, 1
    ///
    /// Serving loops back to the conversation (4 -> 1), and the conversation is the only
    /// place the bar can close. There is no separate feedback step: the customer's reaction
    /// is part of the step 1 conversation on the next loop.
    ///
    /// The child states know nothing about this class: each raises its own event and this
    /// level maps that event onto a transition. Wiring happens once in the constructor.
    /// </summary>
    public class OpenBarPhase : StateBase
    {
        /// <summary>Step 1 chose "close for the day". <see cref="GameLoopFSM"/> listens.</summary>
        public event Action OnRequestExitToClose;

        private readonly StateMachine<OpenSub> _sub = new StateMachine<OpenSub>();

        // ── Child States ───────────────────────────────────

        public StateMachine<OpenSub> Sub => _sub;

        public TalkingWithCustomerState TalkingWithCustomer { get; } = new TalkingWithCustomerState();
        public PrepareDrinksPhase PrepareDrinks { get; } = new PrepareDrinksPhase();
        public GarnishState Garnish { get; } = new GarnishState();
        public ServeState Serve { get; } = new ServeState();

        // ── Setup ──────────────────────────────────────────

        public OpenBarPhase()
        {
            TalkingWithCustomer.OnRequestPrepareDrinks += () => _sub.TryTransition(OpenSub.PrepareDrinks);
            TalkingWithCustomer.OnRequestCloseBar += () => OnRequestExitToClose?.Invoke();
            PrepareDrinks.OnFinished += () => _sub.TryTransition(OpenSub.Garnish);
            Garnish.OnFinished += () => _sub.TryTransition(OpenSub.Serve);
            Garnish.OnRequestRemake += () => _sub.TryTransition(OpenSub.PrepareDrinks);
            Serve.OnFinished += () => _sub.TryTransition(OpenSub.TalkingWithCustomer);
            Serve.OnRequestRemake += () => _sub.TryTransition(OpenSub.PrepareDrinks);

            _sub.AddState(OpenSub.TalkingWithCustomer, TalkingWithCustomer, new[] { OpenSub.PrepareDrinks });
            _sub.AddState(OpenSub.PrepareDrinks, PrepareDrinks, new[] { OpenSub.Garnish });
            _sub.AddState(OpenSub.Garnish, Garnish, new[] { OpenSub.PrepareDrinks, OpenSub.Serve });
            _sub.AddState(OpenSub.Serve, Serve, new[] { OpenSub.PrepareDrinks, OpenSub.TalkingWithCustomer });
        }

        // ── Container Lifecycle ────────────────────────────

        protected override void OnEnter()
        {
            // A fresh service day always starts by talking to the first customer.
            _sub.SetInitial(OpenSub.TalkingWithCustomer);
        }

        protected override void OnTick(float dt)
        {
            _sub.Tick(dt);
        }

        protected override void OnExit()
        {
            // Stop() so the active child state gets its Exit() instead of being abandoned.
            _sub.Stop();
        }
    }
}
