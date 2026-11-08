using System;
using System.Collections.Generic;
using System.Threading;
using Bar410.Contracts;
using Bar410.Core;
using UnityEngine;

namespace Bar410.Flow
{
    /// <summary>
    /// One drink, one order at a time: runs <c>steps</c> in order; Remake resets the drink and restarts at steps[0];
    /// when every step returns Continue the order is scored once (<see cref="IOrderDesk.CommitServe"/>).
    /// Always ends in talking mode (unless the lifetime is gone).
    /// </summary>
    public sealed class BartendingSession
    {
        readonly IReadOnlyList<Station> _steps;
        readonly FlowPorts _ports;
        readonly FlowState _state;
        readonly CancellationToken _lifetime;
        readonly Action _enterTalkingMode;
        CancellationTokenSource _cts;

        public BartendingSession(IReadOnlyList<Station> steps, FlowPorts ports, FlowState state, CancellationToken lifetime, Action enterTalkingMode)
        {
            _steps = steps; _ports = ports; _state = state; _lifetime = lifetime; _enterTalkingMode = enterTalkingMode;
        }

        /// <summary>True from TryBegin until the run has fully ended (talking mode restored).</summary>
        public bool IsRunning => _cts != null;
        public Station Current { get; private set; }

        /// <summary>After scoring and cleanup (Step = Idle, IsRunning = false).</summary>
        public event Action DrinkServed;
        /// <summary>A station threw; talking mode already restored. The exception is also logged.</summary>
        public event Action<Exception> Faulted;

        /// <summary>Starts a drink. Runs synchronously through the first station's entry, so the label is already set.</summary>
        public bool TryBegin()
        {
            if (IsRunning) { Debug.LogWarning("[Flow] flow_prepare_drinks ignored: a drink is already being made"); return false; }
            if (_steps == null || _steps.Count == 0) { Debug.LogError("[Flow] BartendingSession has no steps"); return false; }
            _cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime);
            FlowAwait.Run(RunAsync, _cts.Token, nameof(BartendingSession));
            return true;
        }

        /// <summary>Load / scene change. Stations' finally blocks restore the UI unless the lifetime is cancelled.</summary>
        public void Cancel() => _cts?.Cancel();

        /// <summary>A "button press" routed to the current station. False when idle or not handled.</summary>
        public bool Handle(StepCommand cmd) => Current != null && Current.Handle(cmd);

        async Awaitable RunAsync(CancellationToken ct)
        {
            Exception fault = null;
            bool served = false;
            try
            {
                _state.SetSession(true);
                _ports.Garnish.ClearGlass();
                var ctx = new StepContext(_state, _ports, ct, _lifetime);
                for (int i = 0; i < _steps.Count; i++)
                {
                    Current = _steps[i];
                    if (await Current.RunAsync(ctx) != Next.Remake) continue;
                    _ports.Bar.ResetDrink(); // remake never scores
                    _ports.Garnish.ClearGlass();
                    i = -1;
                }
                Current = null;
                _ports.Orders.CommitServe();
                served = true;
            }
            catch (Exception e) when (e is not OperationCanceledException) { fault = e; }
            finally
            {
                Current = null;
                _ports.Garnish.ClearGlass(); // after scoring (ServeDrink reads no glass)
                _state.SetStep(BarStep.Idle);
                _state.SetSession(false);
                _cts.Dispose(); _cts = null; // may run inside _cts.Cancel(); same pattern as Forcible's using
                if (!_lifetime.IsCancellationRequested) _enterTalkingMode?.Invoke();
            }
            if (fault != null) { Debug.LogException(fault); Faulted?.Invoke(fault); }
            if (served) DrinkServed?.Invoke();
        }
    }
}
