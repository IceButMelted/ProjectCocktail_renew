namespace Bar410.Flow
{
    // Label enums only; DayPart lives in Contracts, Enum_MiniGameType/MinigameOutcome in Domain.
    public enum Next { Continue, Remake }

    /// <summary>Label only, no transition table. A new station needs a new value (and a HubPhase of the same name).</summary>
    public enum BarStep { Idle, AddIngredient, Minigame, Garnish, Serve }

    public enum StepCommand { IngredientAdded, AnotherIngredient, DrinkComplete, GarnishDone, ServeDone, Remake }

    /// <summary>DayStarted = Event finished, story about to start.</summary>
    public enum FlowMilestone { DayStarted, DrinkServed, DayClosed }
}
