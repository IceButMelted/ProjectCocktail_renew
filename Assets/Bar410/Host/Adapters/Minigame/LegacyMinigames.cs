using System;
using System.Threading;
using Bar410.Contracts;
using Bar410.Core;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Host
{
    /// <summary>
    /// IMinigameHost over the old MinigameSystemManager (R3 fallback for the UI-scene MinigameUiHost; swap in the Inspector).
    /// Timing rules: Contracts_Final 3.2 / Hosts.cs. The old manager and its minigames own their canvas, intro and outro, so
    /// there is nothing to hide afterwards (rule 5 is a no-op here). The customer picture is CameraDirector.AimCustomerInset's
    /// job (called by PrepareStation), so MinigameRequest.ShowCustomer is not used.
    /// </summary>
    public sealed class LegacyMinigames : IMinigameHost
    {
        // ponytail: fixed cap for the previous run's outro (F11); read it from the minigame config if outros get longer.
        const float IdleWaitSeconds = 3.5f;

        readonly MinigameSystemManager _manager;

        public LegacyMinigames(MinigameSystemManager manager) { _manager = manager; }

        public bool IsBusy { get; private set; }
        public event Action<Enum_MiniGameType> Started;

        public async Awaitable<MinigameOutcome> RunAsync(MinigameRequest request, CancellationToken ct)
        {
            if (IsBusy) throw new InvalidOperationException("[LegacyMinigames] RunAsync while a run is in progress");
            IsBusy = true;
            var started = false;
            try
            {
                ct.ThrowIfCancellationRequested();
                await WaitIdleAsync(ct);

                var type = _manager.Resolve(request.Selected, request.FromDrink);
                if (_manager.GetMinigame(type) == null)   // D5: no game for this type (e.g. Building)
                {
                    Debug.LogError($"[LegacyMinigames] No minigame registered for '{type}': completing without playing");
                    return MinigameOutcome.Completed;
                }

                var done = new AwaitableCompletionSource<MinigameOutcome>();   // new source per run
                void OnStarted(Enum_MiniGameType t) => Started?.Invoke(t);
                void OnFinished(Enum_MiniGameType t, MinigameOutcome o) => done.TrySetResult(o);
                void OnBack() { _manager.CancelMinigame(); done.TrySetResult(MinigameOutcome.Cancelled); }   // outro continues alone

                _manager.MinigameStarted += OnStarted;
                _manager.MinigameFinished += OnFinished;
                _manager.BackRequested += OnBack;
                try
                {
                    started = true;
                    _manager.StartMinigame(type);
                    return await FlowAwait.Wait(done, ct);
                }
                finally
                {
                    _manager.MinigameStarted -= OnStarted;
                    _manager.MinigameFinished -= OnFinished;
                    _manager.BackRequested -= OnBack;
                }
            }
            catch (OperationCanceledException)
            {
                // Synchronous: the forced Yarn command must change the label before it returns. The outro is not awaited.
                if (started && _manager.ActivePhase != MinigamePhase.Idle) _manager.CancelMinigame();
                throw;
            }
            finally { IsBusy = false; }
        }

        // Waits (bounded) for the previous run's outro: starting over it would snap the panels.
        async Awaitable WaitIdleAsync(CancellationToken ct)
        {
            var until = Time.unscaledTime + IdleWaitSeconds;
            while (_manager.ActivePhase != MinigamePhase.Idle)
            {
                if (Time.unscaledTime >= until)
                {
                    Debug.LogWarning("[LegacyMinigames] Previous minigame still not Idle: starting anyway");
                    return;
                }
                await Awaitable.NextFrameAsync(ct);
            }
        }
    }
}
