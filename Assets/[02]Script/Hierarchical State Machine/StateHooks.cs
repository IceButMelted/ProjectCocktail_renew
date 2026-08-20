using System;
using UnityEngine;
using UnityEngine.Events;

namespace Bar410.GameFlow
{
    // ── Designer Hooks ─────────────────────────────────────

    /// <summary>UnityEvent&lt;float&gt; needs a concrete subclass to show up in the Inspector.</summary>
    [Serializable]
    public class TickUnityEvent : UnityEvent<float> { }

    /// <summary>
    /// Inspector-facing Enter / Update / Exit hooks for one state, wired to that state's
    /// <see cref="IState"/> events by <see cref="GameFlowHooks"/>.
    ///
    /// This is the designer/artist tier from spec §6 — SFX, VFX, animation triggers, UI
    /// toggles. Logic-to-logic flow control stays on the plain C# events and
    /// GameFlowCommands; do not drive transitions from here.
    /// </summary>
    [Serializable]
    public class StateHooks
    {
        [Tooltip("Fires after the state's own Enter logic has run.")]
        [SerializeField] private UnityEvent _onEnter = new UnityEvent();

        [Tooltip("Fires every frame while this state is active. Argument is delta time. Reflection-based — keep the listeners cheap.")]
        [SerializeField] private TickUnityEvent _onUpdate = new TickUnityEvent();

        [Tooltip("Fires after the state's own Exit logic has run.")]
        [SerializeField] private UnityEvent _onExit = new UnityEvent();

        public UnityEvent OnEnter => _onEnter;
        public TickUnityEvent OnUpdate => _onUpdate;
        public UnityEvent OnExit => _onExit;

        /// <summary>Subscribes these hooks to a state. Call once, at bind time.</summary>
        public void Bind(IState state)
        {
            if (state == null)
            {
                Debug.LogError("[StateHooks] Bind called with a null state — the owning phase was not built yet.");
                return;
            }

            state.Entered += InvokeEnter;
            state.Ticked += InvokeUpdate;
            state.Exited += InvokeExit;
        }

        /// <summary>Mirror of <see cref="Bind"/>, so hooks do not outlive the component that owns them.</summary>
        public void Unbind(IState state)
        {
            if (state == null) return;

            state.Entered -= InvokeEnter;
            state.Ticked -= InvokeUpdate;
            state.Exited -= InvokeExit;
        }

        private void InvokeEnter() => _onEnter.Invoke();

        private void InvokeUpdate(float dt) => _onUpdate.Invoke(dt);

        private void InvokeExit() => _onExit.Invoke();
    }
}
