using System.Threading;
using UnityEngine;

namespace Bar410.Flow
{
    /// <summary>What a station gets for one run. Built by BartendingSession; stations keep no ports of their own.</summary>
    public sealed class StepContext
    {
        public StepContext(FlowState state, FlowPorts ports, CancellationToken token, CancellationToken lifetime)
        {
            State = state; Ports = ports; Token = token; Lifetime = lifetime;
        }

        public FlowState State { get; }
        public FlowPorts Ports { get; }
        /// <summary>Session token (load / cancel + lifetime).</summary>
        public CancellationToken Token { get; }
        /// <summary>GameRoot lifetime. Cancelled in a finally → the scene is going away: skip the UI restore.</summary>
        public CancellationToken Lifetime { get; }
    }

    /// <summary>
    /// One drink-making step. Entry and exit live in <see cref="RunAsync"/>; whatever it opens is closed in its finally.
    /// Rules: set the step label before the first await. <see cref="Handle"/> must change the label synchronously
    /// (completion sources continue inline, G1-0 S1) and touch no station field after forcing, because the
    /// station's finally has already run by then.
    /// </summary>
    public abstract class Station : MonoBehaviour
    {
        public abstract Awaitable<Next> RunAsync(StepContext ctx);

        /// <summary>A Yarn/compat "button press". True = handled; false = not this station's command or not running.</summary>
        public virtual bool Handle(StepCommand cmd) => false;
    }
}
