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

        // Ingredients are recorded in ShakerContents as they land, not here. What the scene
        // does on entry/exit lives in FlowWiring (§2.1 AddIngredient).
        protected override void OnEnter() { }

        protected override void OnExit() { }

        /// <summary>Call when the player commits to an ingredient.</summary>
        public void Finish() => OnFinished?.Invoke();
    }
}
