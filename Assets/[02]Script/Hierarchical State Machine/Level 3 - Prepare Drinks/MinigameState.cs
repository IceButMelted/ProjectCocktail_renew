using System;

namespace Bar410.GameFlow
{
    // ── Level 3 · Step 2.2 ─────────────────────────────────

    /// <summary>
    /// Step 2.2 — the pour/shake/mix minigame for the ingredient just added.
    ///
    /// Two outcomes, and only one of them is a transition inside this machine:
    /// - another ingredient  -> back to 2.1 (<see cref="RequestAnotherIngredient"/>)
    /// - drink is finished   -> exits the container via PrepareDrinksPhase.OnFinished
    ///                          (<see cref="CompleteDrink"/>) — not a PrepSub transition.
    ///
    /// Note: this is the *flow* state, distinct from the existing gameplay-side
    /// MiniGameState enum in Minigame/IMinigame.cs. The namespace keeps them apart.
    /// </summary>
    public class MinigameState : StateBase
    {
        /// <summary>Player wants to add another ingredient. Parent loops back to 2.1.</summary>
        public event Action OnRequestAnotherIngredient;

        /// <summary>Drink is complete. Parent raises OnFinished and the Open Bar flow moves to Garnish.</summary>
        public event Action OnDrinkComplete;

        protected override void OnEnter()
        {
            // TODO: hand off to MinigameSystemManager for the ingredient's minigame type.
        }

        protected override void OnExit()
        {
            // TODO: record the minigame result (accuracy/quality) for scoring at step 5.
        }

        public void RequestAnotherIngredient() => OnRequestAnotherIngredient?.Invoke();

        public void CompleteDrink() => OnDrinkComplete?.Invoke();
    }
}
