using System;
using Bar410.Contracts;
using static E_Cocktail;

namespace Bar410.Flow
{
    /// <summary>
    /// The single observable flow state plus the pure maps Yarn reads (flow_phase / flow_step) and the Hub phase.
    /// Setters are internal (DayRunner, BartendingSession, stations). Handlers of <see cref="Changed"/> must not call Set*.
    /// </summary>
    public sealed class FlowState
    {
        public DayPart Part { get; private set; }
        public BarStep Step { get; private set; }
        public bool SessionRunning { get; private set; }
        /// <summary>Persists across runs and days (old PendingType).</summary>
        public Enum_MiniGameType SelectedMinigame { get; private set; }
        /// <summary>"" | "Completed" | "Cancelled"; cleared only when a minigame starts.</summary>
        public string LastMinigameOutcome { get; private set; } = "";

        /// <summary>Raised once per Set* call that changes a value.</summary>
        public event Action Changed;

        internal void SetPart(DayPart p) { if (Part != p) { Part = p; Changed?.Invoke(); } }
        internal void SetStep(BarStep s) { if (Step != s) { Step = s; Changed?.Invoke(); } }
        internal void SetSession(bool running) { if (SessionRunning != running) { SessionRunning = running; Changed?.Invoke(); } }
        internal void SelectMinigame(Enum_MiniGameType k) { if (SelectedMinigame != k) { SelectedMinigame = k; Changed?.Invoke(); } }
        internal void SetMinigameOutcome(string o) { o ??= ""; if (LastMinigameOutcome != o) { LastMinigameOutcome = o; Changed?.Invoke(); } }

        /// <summary>flow_phase(): "" | BeforeOpen | Open | Close.</summary>
        public string PhaseString => Part switch
        {
            DayPart.Event or DayPart.Story => "BeforeOpen",
            DayPart.Open => "Open",
            DayPart.Close => "Close",
            _ => "",
        };

        /// <summary>flow_step(): Event | StoryBeforeOpen | TalkingWithCustomer | AddIngredient | Minigame | Garnish | Serve | "".</summary>
        public string StepString => Part switch
        {
            DayPart.Event => "Event",
            DayPart.Story => "StoryBeforeOpen",
            DayPart.Open => Step == BarStep.Idle ? "TalkingWithCustomer" : Step.ToString(),
            _ => "",
        };

        public HubPhase HubPhase => Part switch
        {
            DayPart.Event or DayPart.Story => HubPhase.BeforeOpen,
            DayPart.Open => Step switch
            {
                BarStep.Idle => HubPhase.Talking,
                BarStep.AddIngredient => HubPhase.AddIngredient,
                BarStep.Minigame => HubPhase.Minigame,
                BarStep.Garnish => HubPhase.Garnish,
                BarStep.Serve => HubPhase.Serve,
                _ => HubPhase.None,
            },
            DayPart.Close => HubPhase.Close,
            _ => HubPhase.None,
        };
    }
}
