using System;

namespace Bar410.GameFlow
{
    // ── Level 2 · Step 4 ───────────────────────────────────

    /// <summary>
    /// Step 4 — serve the drink to the customer. Two legal exits: back to the conversation
    /// (1), where the customer's reaction plays out, or backtrack to Prepare Drinks (2).
    ///
    /// TODO(design, open question #4): the drink is scored here, off data produced in steps
    /// 1–3 (ingredients from 2.1, minigame result from 2.2, garnish from 3). That
    /// data-passing contract needs its own spec — see the DrinkOrderContext note in §8.
    /// </summary>
    public class ServeState : StateBase
    {
        /// <summary>Drink handed over. Parent returns to the conversation for the reaction.</summary>
        public event Action OnFinished;

        /// <summary>Player pulled the drink back to rework it. Parent returns to Prepare Drinks.</summary>
        public event Action OnRequestRemake;

        protected override void OnEnter()
        {
            // TODO: enable the serve interaction (drag glass to the customer, pour animation).
        }

        protected override void OnExit()
        {
            // TODO: score the drink and hand the result to the conversation.
        }

        public void Finish() => OnFinished?.Invoke();

        public void RequestRemake() => OnRequestRemake?.Invoke();
    }
}
