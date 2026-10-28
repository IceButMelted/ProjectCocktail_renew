using System;

namespace Bar410.GameFlow
{
    // ── Level 2 · Before Open · Event ──────────────────────

    /// <summary>
    /// Day events, such as new ingredients, each shown as a popup the player closes.
    /// Single legal exit: StoryBeforeOpen. What is shown and when it is all closed is decided
    /// in FlowWiring, which calls <see cref="Finish"/>.
    /// </summary>
    public class EventState : StateBase
    {
        /// <summary>Every event shown and closed. Parent moves to the story.</summary>
        public event Action OnFinished;

        public void Finish() => OnFinished?.Invoke();
    }
}
