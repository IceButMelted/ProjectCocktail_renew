using System;

namespace Bar410.GameFlow
{
    // ── Level 3 · Step 2.1 ─────────────────────────────────

    /// <summary>
    /// Step 2.1 — pick and add an ingredient. Single legal exit: 2.2 Minigame.
    /// </summary>
    public class AddIngredientState : StateBase
    {
        /// <summary>An ingredient was chosen. Parent moves to the minigame for it.</summary>
        public event Action OnFinished;

        protected override void OnEnter()
        {
            // TODO: enable ingredient selection (bottle pick-up / shelf UI).
        }

        protected override void OnExit()
        {
            // TODO: record the chosen ingredient for scoring at step 5.
        }

        /// <summary>Call when the player commits to an ingredient.</summary>
        public void Finish() => OnFinished?.Invoke();
    }
}
