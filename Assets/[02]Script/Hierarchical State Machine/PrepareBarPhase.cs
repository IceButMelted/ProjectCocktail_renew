using System;

namespace Bar410.GameFlow
{
    // ── Level 1 · Prepare ──────────────────────────────────

    /// <summary>
    /// Spec §3.1 — drag-and-drop bar setup. Minimal internal state, no sub-FSM.
    /// Gameplay code calls <see cref="RequestOpenBar"/> when the player confirms setup
    /// (e.g. the "Open Bar" button); this phase raises the event and the parent
    /// <see cref="GameLoopFSM"/> performs the actual transition.
    /// </summary>
    public class PrepareBarPhase : StateBase
    {
        /// <summary>Player finished bar setup. Parent moves to <see cref="GamePhase.Open"/>.</summary>
        public event Action OnRequestOpenBar;

        protected override void OnEnter()
        {
            // TODO: enable the placement/drag-drop system and the "Open Bar" UI.
        }

        protected override void OnExit()
        {
            // TODO: lock placement, persist the bar layout for the day.
        }

        /// <summary>Call from the "Open Bar" button / setup-complete gameplay action.</summary>
        public void RequestOpenBar() => OnRequestOpenBar?.Invoke();
    }
}
