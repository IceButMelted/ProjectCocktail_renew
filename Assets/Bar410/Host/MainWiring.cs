using System;
using Bar410.Contracts;
using Bar410.Flow;

namespace Bar410.Host
{
    /// <summary>
    /// The always-on links that belong to no step (Table B): Hub follows FlowState, scored/cleared orders reach the dialogue,
    /// the recipe filter, unlocks refresh the bottles. Plain class; GameRoot builds it in Awake and disposes it in OnDestroy
    /// (unsubscribes in reverse order).
    /// ponytail: PointerMode mapping needs InputService (G4-I2) and the voice -> speaking link needs the Director (G2); add both here then.
    /// </summary>
    public sealed class MainWiring : IDisposable
    {
        readonly FlowState _state;
        readonly IOrderDesk _orders;
        readonly IBarCounter _bar;
        readonly IIngredientUnlocks _unlockPort;   // may be null: everything available
        HubPhase? _lastHub;                        // HubManager.SetPhase is not idempotent (restarts the fade)
        readonly TalkToCustomerManager _customers; // may be null (warns)
        readonly HubManager _hub;                  // may be null (warns)

        public MainWiring(FlowState state, IOrderDesk orders, IBarCounter bar, IIngredientUnlocks unlockPort,
            TalkToCustomerManager customers, HubManager hub)
        {
            _state = state; _orders = orders; _bar = bar; _unlockPort = unlockPort; _customers = customers; _hub = hub;

            if (_hub != null)
            {
                _state.Changed += OnStateChanged;
                PushHub();   // once at bind: BootAsync's SetPart(None) does not raise Changed (value already None)
            }
            else UnityEngine.Debug.LogWarning("[MainWiring] No HubManager: Hub UI will not follow the flow");

            if (_customers != null)
            {
                _orders.UsePreferences(_customers.Preferences);
                _orders.Scored += OnScored;
                _orders.Cleared += _customers.ResetVariables;
            }
            else UnityEngine.Debug.LogWarning("[MainWiring] No TalkToCustomerManager: scored orders will not reach the dialogue");

            // Random orders only ask for drinks made of ingredients the player has.
            if (_unlockPort != null) _orders.UseRecipeFilter(_unlockPort.IsRecipeComplete);
            if (_unlockPort != null) _unlockPort.Changed += _bar.RefreshUnlocked;
        }

        public void Dispose()
        {
            if (_unlockPort != null) _unlockPort.Changed -= _bar.RefreshUnlocked;
            if (_customers != null)
            {
                _orders.Cleared -= _customers.ResetVariables;
                _orders.Scored -= OnScored;
            }
            if (_hub != null) _state.Changed -= OnStateChanged;
        }

        void OnStateChanged() { if (_state.HubPhase != _lastHub) PushHub(); }
        void PushHub() { _lastHub = _state.HubPhase; _hub.SetPhase(_state.HubPhase); }   // bind-time call forces the first value
        void OnScored(ScoreResult _) => _customers.CommitOrderResult(_orders.Order);   // same context the old OrderScored carried
    }
}
