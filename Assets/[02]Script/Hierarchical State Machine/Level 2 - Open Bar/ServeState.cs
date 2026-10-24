using System;

namespace Bar410.GameFlow
{
    // ── Level 2 · Step 4 ───────────────────────────────────

    /// <summary>
    /// Step 4 — serve the drink to the customer. Two legal exits: back to the conversation
    /// (1), where the customer's reaction plays out, or backtrack to Prepare Drinks (2).
    ///
    /// Scoring is not done here: FlowWiring scores once on the Serve → Talking transition, so
    /// a remake (Serve → Prepare Drinks) never scores or writes to Yarn.
    /// </summary>
    public class ServeState : StateBase
    {
        /// <summary>Drink handed over. Parent returns to the conversation for the reaction.</summary>
        public event Action OnFinished;

        /// <summary>Player pulled the drink back to rework it. Parent returns to Prepare Drinks.</summary>
        public event Action OnRequestRemake;

        // What the scene does on entry/exit lives in FlowWiring (§4 Serve).
        protected override void OnEnter() { }

        protected override void OnExit() { }

        public void Finish() => OnFinished?.Invoke();

        public void RequestRemake() => OnRequestRemake?.Invoke();
    }
}
