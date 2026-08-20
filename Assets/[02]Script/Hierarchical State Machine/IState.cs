using System;

namespace Bar410.GameFlow
{
    // ── Core Contract ──────────────────────────────────────

    /// <summary>
    /// Contract for every node in the hierarchical state machine: top-level phases,
    /// sub-phases, and sub-sub-phases all implement this.
    ///
    /// Entered / Exited / Ticked fire *after* the state's own internal logic runs, so
    /// other systems (audio, UI, analytics) can subscribe without coupling to the
    /// state machine internals.
    ///
    /// States that are also containers (own a child <see cref="StateMachine{TKey}"/>)
    /// implement this interface and drive their child machine from Enter/Exit/Tick.
    /// </summary>
    public interface IState
    {
        event Action Entered;
        event Action Exited;
        event Action<float> Ticked;

        void Enter();
        void Exit();
        void Tick(float dt);
    }
}
