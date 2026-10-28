using System;

namespace Bar410.GameFlow
{
    // ── Level 2 · Before Open · Story ──────────────────────

    /// <summary>
    /// The short story before the bar opens. Exits: back to Event (the story carries on
    /// afterwards), or Open. Only the first entry of a day starts the story; FlowWiring reads
    /// <see cref="IsFirstEntry"/> for that.
    /// </summary>
    public class StoryBeforeOpenState : StateBase
    {
        /// <summary>The story wants day events checked mid-way. Parent moves to Event.</summary>
        public event Action OnRequestEvents;

        /// <summary>The story is done. Parent leaves BeforeOpen for Open.</summary>
        public event Action OnRequestOpen;

        private int _entries;

        /// <summary>True during the day's first entry, false when coming back from Event.</summary>
        public bool IsFirstEntry => _entries == 1;

        /// <summary>Called by the parent when a new day's BeforeOpen begins.</summary>
        public void ResetDay() => _entries = 0;

        protected override void OnEnter() => _entries++;

        public void RequestEvents() => OnRequestEvents?.Invoke();

        public void RequestOpen() => OnRequestOpen?.Invoke();
    }
}
