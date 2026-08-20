using System;

namespace Bar410.GameFlow
{
    // ── Level 3 Keys ───────────────────────────────────────

    /// <summary>The 2-step drink-building loop nested inside OpenSub.PrepareDrinks.</summary>
    public enum PrepSub
    {
        AddIngredient,
        Minigame
    }

    // ── Level 3 Machine ────────────────────────────────────

    /// <summary>
    /// Owns PrepareDrinksFSM. Transition table:
    ///
    ///   2.1 AddIngredient -> 2.2
    ///   2.2 Minigame      -> 2.1, or exit the container via OnFinished
    ///
    /// Backtracking into this phase from Garnish (3) or Serve (4) **resets the drink**:
    /// every entry starts over at 2.1 AddIngredient. Confirmed with design.
    ///
    /// The 2.2 → 3 (Garnish) move is an *exit from this container*, surfaced through
    /// <see cref="OnFinished"/>. Garnish/OpenSub deliberately never appear in this table:
    /// leaking the parent's key type in here would break the encapsulation the whole
    /// hierarchy depends on.
    /// </summary>
    public class PrepareDrinksPhase : StateBase
    {
        /// <summary>Drink is done. Parent (OpenBarPhase) moves its sub-FSM to Garnish.</summary>
        public event Action OnFinished;

        private readonly StateMachine<PrepSub> _sub = new StateMachine<PrepSub>();

        // ── Child States ───────────────────────────────────

        public StateMachine<PrepSub> Sub => _sub;

        public AddIngredientState AddIngredient { get; } = new AddIngredientState();
        public MinigameState Minigame { get; } = new MinigameState();

        // ── Setup ──────────────────────────────────────────

        public PrepareDrinksPhase()
        {
            AddIngredient.OnFinished += () => _sub.TryTransition(PrepSub.Minigame);
            Minigame.OnRequestAnotherIngredient += () => _sub.TryTransition(PrepSub.AddIngredient);
            Minigame.OnDrinkComplete += OnMinigameComplete;

            _sub.AddState(PrepSub.AddIngredient, AddIngredient, new[] { PrepSub.Minigame });
            _sub.AddState(PrepSub.Minigame, Minigame, new[] { PrepSub.AddIngredient });
        }

        // ── Container Lifecycle ────────────────────────────

        protected override void OnEnter()
        {
            _sub.SetInitial(PrepSub.AddIngredient);
        }

        protected override void OnTick(float dt)
        {
            _sub.Tick(dt);
        }

        protected override void OnExit()
        {
            _sub.Stop();
        }

        /// <summary>
        /// Called by <see cref="MinigameState"/> when the player finishes the current drink
        /// (as opposed to looping back to add another ingredient).
        /// </summary>
        public void OnMinigameComplete()
        {
            OnFinished?.Invoke();
        }
    }
}
