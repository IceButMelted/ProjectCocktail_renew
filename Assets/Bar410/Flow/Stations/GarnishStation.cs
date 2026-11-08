using Bar410.Contracts;
using Bar410.Core;
using UnityEngine;

namespace Bar410.Flow
{
    /// <summary>Step 3: decorate the glass. Finished → Continue (ice applied), Remake → Remake.</summary>
    public sealed class GarnishStation : Station
    {
        Forcible<GarnishResult> _force;

        public override async Awaitable<Next> RunAsync(StepContext ctx)
        {
            var p = ctx.Ports;
            ctx.State.SetStep(BarStep.Garnish);
            using var f = _force = new Forcible<GarnishResult>(ctx.Token);
            try
            {
                var drink = p.Bar.ResolveDrink();
                p.Camera.Show(CameraShot.Garnish);
                p.Bar.LockIngredients();
                p.Bar.SetOrderNoteAvailable(true);
                // The host maps the customer to a seat (D6); Flow never knows seats.
                var r = await f.Await(p.Garnish.RunAsync(new GarnishRequest(drink, p.Orders.Order.Customer), f.Token));
                if (r.Exit == GarnishExit.Remake) return Next.Remake;
                p.Bar.SetIce(r.Decoration.ice); // a forced finish carries a default decoration (no ice); accepted
                return Next.Continue;
            }
            finally
            {
                _force = null;
                if (!ctx.Lifetime.IsCancellationRequested) p.Bar.SetOrderNoteAvailable(false);
            }
        }

        public override bool Handle(StepCommand cmd) => cmd switch
        {
            StepCommand.GarnishDone => _force != null && _force.TryForce(new GarnishResult(GarnishExit.Finished, default)),
            StepCommand.Remake => _force != null && _force.TryForce(new GarnishResult(GarnishExit.Remake, default)),
            _ => false,
        };
    }
}
