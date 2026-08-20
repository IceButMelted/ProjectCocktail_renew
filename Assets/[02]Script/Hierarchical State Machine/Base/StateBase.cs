using System;

namespace Bar410.GameFlow
{
    // ── Shared Event Plumbing ──────────────────────────────

    /// <summary>
    /// Default <see cref="IState"/> implementation. Handles the Entered/Exited/Ticked
    /// event plumbing once so concrete states only override the hooks they care about.
    ///
    /// Ordering guarantee (spec §2.1): the hook runs first, the event fires after.
    /// </summary>
    public abstract class StateBase : IState
    {
        public event Action Entered;
        public event Action Exited;
        public event Action<float> Ticked;

        public void Enter()
        {
            OnEnter();
            Entered?.Invoke();
        }

        public void Exit()
        {
            OnExit();
            Exited?.Invoke();
        }

        public void Tick(float dt)
        {
            OnTick(dt);
            Ticked?.Invoke(dt);
        }

        // ── Hooks ──────────────────────────────────────────

        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }
        protected virtual void OnTick(float dt) { }
    }
}
