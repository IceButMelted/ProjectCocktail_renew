using System;

namespace Bar410.GameFlow
{
    // ── Level 1 · Before Open ──────────────────────────────

    /// <summary>
    /// Start of every day, before the bar opens: story scenes and day events such as
    /// unlocking new ingredients. No sub-FSM. The only way out is Open, from a button
    /// or <c>&lt;&lt;flow_open_bar&gt;&gt;</c> (both through <see cref="GameFlowCommands.OpenBar"/>).
    /// </summary>
    public class BeforeOpenPhase : StateBase
    {
        /// <summary>Day events done. Parent moves to <see cref="GamePhase.Open"/>.</summary>
        public event Action OnRequestOpen;

        public void RequestOpen() => OnRequestOpen?.Invoke();
    }
}
