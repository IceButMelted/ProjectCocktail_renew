using System;

namespace Bar410.GameFlow
{
    // ── Level 1 · Close ────────────────────────────────────

    /// <summary>
    /// Spec §3.2 — end-of-day phase with three player actions. No sub-FSM: per the design
    /// brief the three actions are currently unordered/optional.
    ///
    /// TODO(design, open question #3): are the three closing actions ordered, optional, or
    /// all required before "Next Day" unlocks? If they gain ordering rules, give this phase
    /// its own child StateMachine instead of the flat flags below.
    /// </summary>
    public class ClosingBarPhase : StateBase
    {
        /// <summary>Player chose "Next Day". Parent moves to <see cref="GamePhase.Prepare"/>.</summary>
        public event Action OnRequestNextDay;

        protected override void OnEnter()
        {
            // TODO: show the closing-bar UI and enable the three end-of-day actions.
        }

        protected override void OnExit()
        {
            // TODO: commit end-of-day results (money, restock, save checkpoint).
        }

        /// <summary>Call from the "Next Day" button once design's unlock condition is met.</summary>
        public void RequestNextDay() => OnRequestNextDay?.Invoke();
    }
}
