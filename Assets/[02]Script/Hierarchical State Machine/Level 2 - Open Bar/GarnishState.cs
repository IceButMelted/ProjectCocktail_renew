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

        // What the scene does on entry/exit lives in FlowWiring (§3 Garnish). Garnish
        // decoration is cosmetic and is not scored (CONTEXT.md, Glass & Garnish).
        protected override void OnEnter() { }

        protected override void OnExit() { }

        public void Finish() => OnFinished?.Invoke();

        public void RequestRemake() => OnRequestRemake?.Invoke();
    }
}
