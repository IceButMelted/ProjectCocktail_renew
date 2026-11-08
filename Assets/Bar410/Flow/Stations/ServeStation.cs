using Bar410.Contracts;
using Bar410.Core;
using UnityEngine;

namespace Bar410.Flow
{
    /// <summary>Step 4: hand the drink over. Served → Continue (the session scores), Remake → Remake.</summary>
    public sealed class ServeStation : Station
    {
        Forcible<ServeDecision> _force;

        public override async Awaitable<Next> RunAsync(StepContext ctx)
        {
            var p = ctx.Ports;
            ctx.State.SetStep(BarStep.Serve);
            using var f = _force = new Forcible<ServeDecision>(ctx.Token);
            try
            {
                var d = await f.Await(p.Garnish.ServeAsync(f.Token));
                return d == ServeDecision.Remake ? Next.Remake : Next.Continue;
            }
            finally
            {
                _force = null;
                if (!ctx.Lifetime.IsCancellationRequested) p.Camera.Show(CameraShot.Bar);
            }
        }

        public override bool Handle(StepCommand cmd) => cmd switch
        {
            StepCommand.ServeDone => _force != null && _force.TryForce(ServeDecision.Served),
            StepCommand.Remake => _force != null && _force.TryForce(ServeDecision.Remake),
            _ => false,
        };
    }
}
