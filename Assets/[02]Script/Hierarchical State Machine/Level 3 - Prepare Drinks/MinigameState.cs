using System;
using static E_Cocktail;

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
    /// Note: this is the *flow* state, distinct from the gameplay-side
    /// <see cref="MinigamePhase"/> in Minigame/IMinigame.cs. The namespace keeps them apart.
    /// </summary>
    public class MinigameState : StateBase
    {
        /// <summary>Player wants to add another ingredient. Parent loops back to 2.1.</summary>
        public event Action OnRequestAnotherIngredient;

        /// <summary>Drink is complete. Parent raises OnFinished and the Open Bar flow moves to Garnish.</summary>
        public event Action OnDrinkComplete;

        // ── Minigame seam ──────────────────────────────────
        // Everything below is the connection point to Assets/[02]Script/Minigame/.
        // It is deliberately DATA + EVENTS ONLY: this class must never reference
        // MinigameSystemManager, or the flow layer drags UnityEngine and a scene
        // object into itself and the layering in Bar410_StateMachine_Implementation.md breaks.
        //
        // MinigameFlowBridge (a MonoBehaviour, not written yet — see
        // Bar410_Minigame_Integration_Plan.md §3.3) is the one and only thing that
        // subscribes here and translates in both directions:
        //
        //   OnStartRequested(type)  ──> MinigameSystemManager.StartMinigame(type)
        //   OnStopRequested         ──> MinigameSystemManager.CancelMinigame()
        //   MinigameFinished(..)    ──> ReportResult(outcome)   [inbound]
        //
        // Until that bridge exists these events have no subscribers, so OnEnter/OnExit
        // are no-ops exactly as before — nothing in the current flow changes.

        /// <summary>Which minigame step 2.2 should run. Set by GameFlowCommands / the shaker's method.</summary>
        public Enum_MiniGameType PendingType { get; private set; } = Enum_MiniGameType.None;

        /// <summary>How the last run ended. <c>null</c> until a minigame has reported back.</summary>
        public MinigameOutcome? LastOutcome { get; private set; }

        /// <summary>Fired from <see cref="OnEnter"/>. The bridge turns this into StartMinigame.</summary>
        public event Action<Enum_MiniGameType> OnStartRequested;

        /// <summary>Fired from <see cref="OnExit"/>. The bridge cancels anything still on screen.</summary>
        public event Action OnStopRequested;

        /// <summary>Chooses the minigame for the next entry into 2.2. Does not transition.</summary>
        public void SelectType(Enum_MiniGameType type) => PendingType = type;

        /// <summary>Called by the bridge when a run finishes. Purely records — never transitions.</summary>
        public void ReportResult(MinigameOutcome outcome) => LastOutcome = outcome;

        // ── StateBase hooks ────────────────────────────────

        protected override void OnEnter()
        {
            LastOutcome = null;
            OnStartRequested?.Invoke(PendingType);
        }

        protected override void OnExit()
        {
            OnStopRequested?.Invoke();
            // TODO: fold LastOutcome into DrinkOrderContext for scoring at step 5
            //       (HSM doc §8 Q4 — LastOutcome is a placeholder for that per-ingredient list).
        }

        // ── Transitions out of 2.2 ─────────────────────────

        public void RequestAnotherIngredient() => OnRequestAnotherIngredient?.Invoke();

        public void CompleteDrink() => OnDrinkComplete?.Invoke();
    }
}
