using System;

namespace Bar410.GameFlow
{
    // ── Level 2 · Step 3 ───────────────────────────────────

    /// <summary>
    /// Step 3 — garnish the drink. Two legal exits: forward to Serve (4), or backtrack
    /// to Prepare Drinks (2) if the player wants to change the drink itself.
    /// </summary>
    public class GarnishState : StateBase
    {
        /// <summary>Garnish confirmed. Parent moves to Serve.</summary>
        public event Action OnFinished;

        /// <summary>Player wants to go back and rework the drink. Parent returns to Prepare Drinks.</summary>
        public event Action OnRequestRemake;

        protected override void OnEnter()
        {
            // TODO: enable garnish selection/placement UI.
        }

        protected override void OnExit()
        {
            // TODO: record the garnish choice for scoring at step 5.
        }

        public void Finish() => OnFinished?.Invoke();

        public void RequestRemake() => OnRequestRemake?.Invoke();
    }
}
