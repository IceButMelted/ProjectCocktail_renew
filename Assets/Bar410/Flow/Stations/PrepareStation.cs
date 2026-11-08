using System;
using Bar410.Contracts;
using Bar410.Core;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Flow
{
    /// <summary>
    /// Steps 2 / 2.1 / 2.2: pick ingredients (AddIngredient), then the minigame; Cancelled loops back to 2.1.
    /// A minigame can only end Completed or Cancelled. Completed (or a forced drink_complete) → Continue.
    /// </summary>
    public sealed class PrepareStation : Station
    {
        StepContext _ctx;
        AwaitableCompletionSource _methodWait; // non-null only while waiting in 2.1
        Forcible<MinigameOutcome> _force;     // non-null only while the minigame runs (2.2)
        bool _completeForced;

        public override async Awaitable<Next> RunAsync(StepContext ctx)
        {
            var p = ctx.Ports; var s = ctx.State;
            _ctx = ctx;
            // Subscribe first: BeginDrink raises ShakerCleared synchronously.
            p.Bar.ShakerFull += OnFull;
            p.Bar.ShakerCleared += OnCleared;
            p.Bar.ResetChosen += OnCleared;
            p.Bar.MethodChosen += OnMethod;
            bool picking = false;
            try
            {
                p.Bar.BeginDrink();
                p.Camera.ResetLook();
                p.Camera.Show(CameraShot.Bar);
                p.Bar.SetOrderNoteAvailable(true);

                while (true)
                {
                    // 2.1
                    s.SetStep(BarStep.AddIngredient);
                    p.Orders.EnsureOrder();
                    p.Bar.OpenIngredients(); picking = true;
                    RefreshLook();
                    var src = _methodWait = new AwaitableCompletionSource();
                    try { await FlowAwait.Wait(src, ctx.Token); }
                    finally { _methodWait = null; }
                    p.Bar.CloseIngredients(); picking = false;

                    // 2.2
                    s.SetStep(BarStep.Minigame);
                    s.SetMinigameOutcome("");
                    p.Camera.SetLookFree(false);
                    var customer = p.Orders.Order.Customer;
                    if (customer == CharacterName.None)
                        Debug.LogWarning("[Flow] The order has no customer: add <<order_customer>> to the Yarn. Customer picture hidden.");
                    MinigameOutcome outcome;
                    _completeForced = false;
                    using (var f = _force = new Forcible<MinigameOutcome>(ctx.Token))
                    {
                        try
                        {
                            p.Camera.AimCustomerInset(customer);
                            var req = new MinigameRequest(s.SelectedMinigame, p.Bar.RequiredMinigame, customer != CharacterName.None);
                            outcome = await f.Await(p.Minigames.RunAsync(req, f.Token));
                        }
                        finally
                        {
                            _force = null;
                            if (!ctx.Lifetime.IsCancellationRequested) p.Camera.AimCustomerInset(CharacterName.None);
                        }
                    }

                    // Parity: a forced drink_complete recorded the cancelled run's late report.
                    if (_completeForced) { s.SetMinigameOutcome(nameof(MinigameOutcome.Cancelled)); return Next.Continue; }
                    if (outcome == MinigameOutcome.Completed) { s.SetMinigameOutcome(nameof(MinigameOutcome.Completed)); return Next.Continue; }
                    s.SetMinigameOutcome(nameof(MinigameOutcome.Cancelled));
                    p.Bar.SetShakerGrabbable(true);
                }
            }
            finally
            {
                p.Bar.ShakerFull -= OnFull;
                p.Bar.ShakerCleared -= OnCleared;
                p.Bar.ResetChosen -= OnCleared;
                p.Bar.MethodChosen -= OnMethod;
                _ctx = null;
                if (!ctx.Lifetime.IsCancellationRequested)
                {
                    if (picking) p.Bar.CloseIngredients();
                    p.Bar.CloseBook();
                    p.Bar.SetOrderNoteAvailable(false);
                    p.Camera.SetLookFree(false);
                }
            }
        }

        public override bool Handle(StepCommand cmd)
        {
            switch (cmd)
            {
                case StepCommand.IngredientAdded:
                    return _methodWait != null && _methodWait.TrySetResult();
                case StepCommand.AnotherIngredient:
                    return _force != null && _force.TryForce(MinigameOutcome.Cancelled);
                case StepCommand.DrinkComplete:
                    if (_force == null || _force.IsForced) return false; // never flip the flag of an already-forced run
                    _completeForced = true; // read by the inline continuation inside TryForce
                    return _force.TryForce(MinigameOutcome.Completed);
                default:
                    return false;
            }
        }

        // ADR 0002 Amendment: mouse look only in 2.1 while the shaker has room.
        void RefreshLook() => _ctx.Ports.Camera.SetLookFree(_ctx.State.Step == BarStep.AddIngredient && _ctx.Ports.Bar.HasRoom);

        void OnCleared() { _ctx.Ports.Camera.Show(CameraShot.Bar); RefreshLook(); }

        void OnFull() { _ctx.Ports.Camera.SetLookFree(false); _ctx.Ports.Camera.Show(CameraShot.Shaker); }

        void OnMethod(Enum_MiniGameType kind)
        {
            if (_methodWait == null) return; // only counts in 2.1
            _ctx.State.SelectMinigame(kind);
            _methodWait.TrySetResult();
        }
    }
}
