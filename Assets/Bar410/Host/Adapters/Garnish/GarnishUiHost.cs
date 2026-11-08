using System;
using System.Collections.Generic;
using System.Threading;
using Bar410.Contracts;
using Bar410.WorldViews;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Host
{
    /// <summary>
    /// IGarnishHost in the Garnish_UI scene, over the GarnishManager that lives there (G3-T9).
    /// Step 3 = RunAsync (Open … Finish/Reset → Close), step 4 = ServeAsync (OpenServe … Serve/Remake → CloseServe).
    /// ct cancelled → the panel closes and OCE is thrown synchronously; IsBusy is false on return (Contracts_Final 3.2).
    /// The world half (glass, pour) comes in through <see cref="Bind"/>.
    /// </summary>
    public sealed class GarnishUiHost : MonoBehaviour, IGarnishHost
    {
        [SerializeField] GarnishManager _manager;
        [Tooltip("GlassView: the picture of the glass on the plate.")]
        [SerializeField] WorldView _glassView;

        bool _bound;
        IReadOnlyList<Transform> _seats;
        ICustomerStage _stage;
        AwaitableCompletionSource<GarnishResult> _garnish;
        AwaitableCompletionSource<ServeDecision> _serve;
        IGarnishTable _table;
        S_Drink _drink;
        bool _ice;

        public bool IsBusy => _garnish != null || _serve != null;

        /// <summary>
        /// Seat (index into GarnishGlassView1/2/3) of the current order's customer, resolved at RunAsync.
        /// ponytail: resolved and logged only; what the glass does at the seat waits for the owner's visual decision (D6).
        /// </summary>
        public int SeatIndex { get; private set; }

        void Awake()
        {
            _manager.PourRequested += OnPourRequested;
            _manager.IceToggled += OnIceToggled;
            _manager.FinishRequested += OnFinish;
            _manager.ResetRequested += OnReset;
            _manager.ServeRequested += OnServe;
            _manager.RemakeRequested += OnRemake;
        }

        void OnDestroy()
        {
            if (_manager == null) return;
            _manager.PourRequested -= OnPourRequested;
            _manager.IceToggled -= OnIceToggled;
            _manager.FinishRequested -= OnFinish;
            _manager.ResetRequested -= OnReset;
            _manager.ServeRequested -= OnServe;
            _manager.RemakeRequested -= OnRemake;
        }

        /// <summary>Called once per registration by UiSceneBindings.</summary>
        public void Bind(IGarnishTable table, WorldViewHandle glassView, IReadOnlyList<Transform> seats, ICustomerStage stage)
        {
            _table = table;
            _manager.UseTable(table);
            if (_glassView != null) _glassView.Bind(glassView);
            _seats = seats;
            _stage = stage;
            _bound = true;
        }

        /// <summary>Through the manager, so its buttons and the grids follow.</summary>
        public void ClearGlass()
        {
            _manager.ClearGlass();
            if (_glassView != null) _glassView.Hide();
        }

        public async Awaitable<GarnishResult> RunAsync(GarnishRequest request, CancellationToken ct)
        {
            Guard(ct);
            _drink = request.Drink;
            _ice = false;
            SeatIndex = SeatFor(request.Customer);

            var done = new AwaitableCompletionSource<GarnishResult>();   // new source per call
            _garnish = done;
            bool finished = false;
            try
            {
                using (ct.Register(() => done.TrySetCanceled()))
                {
                    _manager.Open();
                    if (_glassView != null) _glassView.Show();   // after Open: GlassView is activated (and its WorldView awakes) there
                    var result = await done.Awaitable;
                    finished = result.Exit == GarnishExit.Finished;
                    return result;
                }
            }
            finally
            {
                _garnish = null;
                _manager.Close();   // slides out in the background
                if (!finished && _glassView != null) _glassView.Hide();   // Finished keeps the picture for the serve step
            }
        }

        public async Awaitable<ServeDecision> ServeAsync(CancellationToken ct)
        {
            Guard(ct);
            var done = new AwaitableCompletionSource<ServeDecision>();
            _serve = done;
            try
            {
                using (ct.Register(() => done.TrySetCanceled()))
                {
                    _manager.OpenServe();
                    if (_glassView != null) _glassView.Show();
                    return await done.Awaitable;
                }
            }
            finally
            {
                _serve = null;
                _manager.CloseServe();
                if (_glassView != null) _glassView.Hide();
            }
        }

        void Guard(CancellationToken ct)
        {
            if (IsBusy) throw new InvalidOperationException("[GarnishUiHost] called while a garnish or serve step is in progress");
            if (!_bound) throw new InvalidOperationException("[GarnishUiHost] not bound: the UI scene was used before UiSceneBindings ran");
            ct.ThrowIfCancellationRequested();
        }

        // ── Manager events ─────────────────────────────────

        void OnPourRequested()
        {
            if (_garnish == null) return;
            if (_drink == null || _drink.AlcoholList.Count + _drink.LiqueurList.Count + _drink.MixerList.Count == 0)
            {
                Debug.LogWarning("[GarnishUiHost] Nothing in the shaker to pour.", this);
                return;
            }
            _manager.Pour(_drink);
        }

        // Same meaning as the old FlowWiring.OnIceToggled → shaker ice: the last toggle of this step wins.
        void OnIceToggled(bool on) => _ice = on;

        void OnFinish()
        {
            var d = _table != null ? _table.Decoration : default;
            d.ice = _ice;
            _garnish?.TrySetResult(new GarnishResult(GarnishExit.Finished, d));
        }

        void OnReset() => _garnish?.TrySetResult(new GarnishResult(GarnishExit.Remake, default));
        void OnServe() => _serve?.TrySetResult(ServeDecision.Served);
        void OnRemake() => _serve?.TrySetResult(ServeDecision.Remake);

        // D6: the seat marker nearest the customer (markers sit in front of the TalkToCustomerManager spots).
        int SeatFor(CharacterName who)
        {
            Transform anchor = who != CharacterName.None && _stage != null && _stage.IsPresent(who) ? _stage.Anchor(who) : null;
            if (anchor == null || _seats == null || _seats.Count == 0)
            {
                Debug.LogWarning($"[GarnishUiHost] Customer '{who}' is not in the shop: using glass seat 0.", this);
                return 0;
            }

            int best = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _seats.Count; i++)
            {
                if (_seats[i] == null) continue;
                var d = _seats[i].position - anchor.position;
                float dist = d.x * d.x + d.z * d.z;
                if (dist < bestDist) { bestDist = dist; best = i; }
            }
            return best;
        }
    }
}
