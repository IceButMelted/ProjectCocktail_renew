using System;

namespace Bar410.GameFlow
{
    // ── Level 2 Keys ───────────────────────────────────────

    /// <summary>The steps of BeforeOpen. Event always comes first.</summary>
    public enum BeforeOpenSub
    {
        Event,
        StoryBeforeOpen
    }

    // ── Level 2 Machine ────────────────────────────────────

    /// <summary>
    /// Start of every day, before the bar opens. Owns BeforeOpenFSM. Transition table:
    ///
    ///   Event           -> StoryBeforeOpen
    ///   StoryBeforeOpen -> Event, or exit to Open via OnRequestOpen
    ///
    /// Every entry starts at Event (day events such as unlocking new ingredients), then the
    /// short story before opening. The story can go back to Event mid-way and carry on.
    /// Leaving for Open is only allowed from StoryBeforeOpen.
    /// </summary>
    public class BeforeOpenPhase : StateBase
    {
        /// <summary>The story chose to open the bar. Parent moves to <see cref="GamePhase.Open"/>.</summary>
        public event Action OnRequestOpen;

        private readonly StateMachine<BeforeOpenSub> _sub = new StateMachine<BeforeOpenSub>();

        // ── Child States ───────────────────────────────────

        public StateMachine<BeforeOpenSub> Sub => _sub;

        public EventState Event { get; } = new EventState();
        public StoryBeforeOpenState Story { get; } = new StoryBeforeOpenState();

        // ── Setup ──────────────────────────────────────────

        public BeforeOpenPhase()
        {
            Event.OnFinished += () => _sub.TryTransition(BeforeOpenSub.StoryBeforeOpen);
            Story.OnRequestEvents += () => _sub.TryTransition(BeforeOpenSub.Event);
            Story.OnRequestOpen += OnStoryRequestOpen;

            _sub.AddState(BeforeOpenSub.Event, Event, new[] { BeforeOpenSub.StoryBeforeOpen });
            _sub.AddState(BeforeOpenSub.StoryBeforeOpen, Story, new[] { BeforeOpenSub.Event });
        }

        // ── Container Lifecycle ────────────────────────────

        protected override void OnEnter()
        {
            Story.ResetDay();
            _sub.SetInitial(BeforeOpenSub.Event);
        }

        protected override void OnTick(float dt)
        {
            _sub.Tick(dt);
        }

        protected override void OnExit()
        {
            _sub.Stop();
        }

        /// <summary>Exit is not a table entry, so the "only from the story" rule is checked here.</summary>
        private void OnStoryRequestOpen()
        {
            if (_sub.IsRunning && _sub.Current == BeforeOpenSub.StoryBeforeOpen)
            {
                OnRequestOpen?.Invoke();
                return;
            }

            UnityEngine.Debug.LogWarning(_sub.IsRunning
                ? $"[BeforeOpen] Open is only allowed from StoryBeforeOpen, not {_sub.Current}."
                : "[BeforeOpen] Open requested while BeforeOpen is not active.");
        }
    }
}
