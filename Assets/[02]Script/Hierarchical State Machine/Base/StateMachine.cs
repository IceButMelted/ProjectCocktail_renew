using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bar410.GameFlow
{
    // ── Generic Machine ────────────────────────────────────

    /// <summary>
    /// Generic state machine whose legal transitions are defined as data at setup time,
    /// not as hardcoded if/else. Illegal transitions are rejected by the table itself.
    ///
    /// Rules (spec §2.2):
    /// - Exit() on the old state always fires before Enter() on the new state.
    /// - <see cref="OnTransition"/> fires after both, once the machine is stable again.
    /// - Illegal transition attempts warn to the console — never fail silently.
    /// </summary>
    public class StateMachine<TKey>
    {
        private readonly Dictionary<TKey, IState> _states = new Dictionary<TKey, IState>();
        private readonly Dictionary<TKey, HashSet<TKey>> _allowedTransitions = new Dictionary<TKey, HashSet<TKey>>();

        /// <summary>True while a state is active — i.e. entered and not yet stopped.</summary>
        private bool _running;

        // ── Public State ───────────────────────────────────

        public TKey Current { get; private set; }
        public bool IsRunning => _running;

        /// <summary>(from, to) — global/debug listeners: HUD overlays, analytics, save-on-phase-change.</summary>
        public event Action<TKey, TKey> OnTransition;

        // ── Setup ──────────────────────────────────────────

        /// <summary>Registers a state and the complete set of states it is allowed to move to.</summary>
        public void AddState(TKey key, IState state, TKey[] canGoTo)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state), $"[StateMachine<{typeof(TKey).Name}>] Null state for key '{key}'.");

            _states[key] = state;
            _allowedTransitions[key] = new HashSet<TKey>(canGoTo ?? Array.Empty<TKey>());
        }

        /// <summary>Sets the entry state and enters it immediately.</summary>
        public void SetInitial(TKey key)
        {
            if (!_states.ContainsKey(key))
            {
                Debug.LogError($"[StateMachine<{typeof(TKey).Name}>] SetInitial called with unregistered state '{key}'.");
                return;
            }

            Current = key;
            _running = true;
            _states[key].Enter();
        }

        // ── Runtime ────────────────────────────────────────

        /// <summary>Transition-table lookup without side effects. Useful for gating UI buttons.</summary>
        public bool CanTransition(TKey to)
        {
            return _running
                   && _allowedTransitions.TryGetValue(Current, out var allowed)
                   && allowed.Contains(to);
        }

        /// <summary>
        /// Attempts to move to <paramref name="to"/>. Returns false and warns if the
        /// transition is not in the table for the current state.
        /// </summary>
        public bool TryTransition(TKey to)
        {
            if (!_running)
            {
                Debug.LogWarning($"[StateMachine<{typeof(TKey).Name}>] Transition to '{to}' requested before SetInitial().");
                return false;
            }

            if (!CanTransition(to))
            {
                Debug.LogWarning($"[StateMachine<{typeof(TKey).Name}>] Illegal transition {Current} -> {to}");
                return false;
            }

            var from = Current;
            _states[from].Exit();
            Current = to;
            _states[to].Enter();
            OnTransition?.Invoke(from, to);
            return true;
        }

        public void Tick(float dt)
        {
            if (!_running) return;
            _states[Current].Tick(dt);
        }

        /// <summary>
        /// Exits the active state and parks the machine. Called by a container state's
        /// Exit() so nested states are not left "entered" when their parent goes away.
        /// Containers re-enter via <see cref="SetInitial"/>, so no resume path is needed.
        /// </summary>
        public void Stop()
        {
            if (!_running) return;
            _running = false;
            _states[Current].Exit();
        }
    }
}
