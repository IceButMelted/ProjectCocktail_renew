using System;
using System.Collections.Generic;
using System.Threading;
using Bar410.Contracts;
using Bar410.Core;
using Bar410.WorldViews;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Host
{
    /// <summary>
    /// IMinigameHost in the Minigame_UI scene, over the MinigameSystemManager that lives there (G3-T8).
    /// Timing rules: Contracts_Final 3.2 / Hosts.cs. World pictures (game animation, customer inset) come in through
    /// <see cref="Bind"/>; the canvas is off between runs and switched off again once the manager is Idle.
    /// </summary>
    public sealed class MinigameUiHost : MonoBehaviour, IMinigameHost
    {
        [Serializable]
        struct ViewSlot { public Enum_MiniGameType type; public WorldView view; }

        [SerializeField] MinigameSystemManager _manager;
        [SerializeField] Canvas _canvas;
        [Tooltip("Panel_CharacterView: shown only when the request has a customer.")]
        [SerializeField] GameObject _customerPanel;
        [SerializeField] WorldView _customerView;
        [SerializeField] ViewSlot[] _gameViews;

        bool _bound;
        int _runId;
        bool _started;
        AwaitableCompletionSource<MinigameOutcome> _run;

        public bool IsBusy { get; private set; }
        public event Action<Enum_MiniGameType> Started;
        public Canvas Canvas => _canvas;

        void Awake() { if (_canvas != null) _canvas.enabled = false; }

        /// <summary>Called once per registration by UiSceneBindings.</summary>
        public void Bind(IReadOnlyDictionary<Enum_MiniGameType, MinigameWorld> worlds, WorldViewHandle customerInset)
        {
            foreach (var slot in _gameViews)
            {
                if (!worlds.TryGetValue(slot.type, out var world)) { Debug.LogWarning($"[MinigameUiHost] No world for '{slot.type}'", this); continue; }
                if (slot.view != null) slot.view.Bind(world.View);
                if (_manager.GetMinigame(slot.type) is BaseMiniGame game) game.UseAnimation(world.Animation);
            }
            if (_customerView != null) _customerView.Bind(customerInset);
            _bound = true;
        }

        public async Awaitable<MinigameOutcome> RunAsync(MinigameRequest request, CancellationToken ct)
        {
            if (IsBusy) throw new InvalidOperationException("[MinigameUiHost] RunAsync while a run is in progress");
            if (!_bound) throw new InvalidOperationException("[MinigameUiHost] not bound: the UI scene was used before UiSceneBindings ran");
            ct.ThrowIfCancellationRequested();

            IsBusy = true;
            int run = ++_runId;
            var done = new AwaitableCompletionSource<MinigameOutcome>();   // new source per run
            _run = done;
            _started = false;
            try
            {
                // Inline on ct.Cancel(): the forced Yarn command sees IsBusy false and the label changed before it returns.
                using (ct.Register(() => { if (_started) _manager.CancelMinigame(); done.TrySetCanceled(); }))
                {
                    FlowAwait.Run(c => StartWhenIdleAsync(request, done, c), ct, "MinigameUiHost.Start", this);
                    return await done.Awaitable;
                }
            }
            finally
            {
                _manager.MinigameFinished -= OnFinished;
                _manager.BackRequested -= OnBack;
                _run = null;
                _started = false;
                IsBusy = false;
                if (_manager.ActivePhase == MinigamePhase.Idle) HideAll();
                else FlowAwait.Run(c => HideWhenIdleAsync(run, c), destroyCancellationToken, "MinigameUiHost.Hide", this);
            }
        }

        async Awaitable StartWhenIdleAsync(MinigameRequest request, AwaitableCompletionSource<MinigameOutcome> done, CancellationToken ct)
        {
            // F11: a late outro would swallow StartGame, so wait for the previous run to reach Idle.
            float until = Time.time + LegacyMinigames.IdleWaitSeconds;
            while (_manager.ActivePhase != MinigamePhase.Idle)
            {
                if (Time.time >= until)
                {
                    Debug.LogError($"[MinigameUiHost] Previous minigame not Idle after {LegacyMinigames.IdleWaitSeconds} s: completing without playing", this);
                    done.TrySetResult(MinigameOutcome.Completed);   // same fallback as D5
                    return;
                }
                await Awaitable.NextFrameAsync(ct);
            }
            if (_run != done) return;   // cancelled meanwhile

            var type = _manager.Resolve(request.Selected, request.FromDrink);
            if (_manager.GetMinigame(type) == null)   // D5
            {
                Debug.LogError($"[MinigameUiHost] No minigame registered for '{type}': completing without playing", this);
                done.TrySetResult(MinigameOutcome.Completed);
                return;
            }

            ShowRun(type, request.ShowCustomer);
            _manager.MinigameFinished += OnFinished;
            _manager.BackRequested += OnBack;
            _started = true;
            try { _manager.StartMinigame(type); }
            catch (Exception e) { done.TrySetException(e); return; }   // never leave RunAsync hanging with IsBusy stuck
            Started?.Invoke(type);   // a throwing listener is logged by FlowAwait.Run, the run goes on
        }

        void OnFinished(Enum_MiniGameType type, MinigameOutcome outcome) => _run?.TrySetResult(outcome);

        // Back returns at once; the outro plays alone and the background hide waits for it.
        void OnBack()
        {
            if (_manager.ActivePhase == MinigamePhase.Outro) return;   // a won run in its outro must stay Completed
            _manager.CancelMinigame();
            _run?.TrySetResult(MinigameOutcome.Cancelled);
        }

        async Awaitable HideWhenIdleAsync(int run, CancellationToken ct)
        {
            while (_manager.ActivePhase != MinigamePhase.Idle) await Awaitable.NextFrameAsync(ct);
            if (run == _runId && !IsBusy) HideAll();   // a newer run owns the views now
        }

        void ShowRun(Enum_MiniGameType type, bool showCustomer)
        {
            if (_canvas != null) _canvas.enabled = true;
            foreach (var slot in _gameViews)
            {
                if (slot.view == null) continue;
                if (slot.type == type) slot.view.Show(); else slot.view.Hide();
            }
            if (_customerPanel != null) _customerPanel.SetActive(showCustomer);
            if (_customerView != null) { if (showCustomer) _customerView.Show(); else _customerView.Hide(); }
        }

        void HideAll()
        {
            foreach (var slot in _gameViews) if (slot.view != null) slot.view.Hide();
            if (_customerView != null) _customerView.Hide();
            if (_canvas != null) _canvas.enabled = false;
        }
    }
}
