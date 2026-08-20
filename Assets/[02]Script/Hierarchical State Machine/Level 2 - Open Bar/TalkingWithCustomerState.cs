using System;

namespace Bar410.GameFlow
{
    // ── Level 2 · Step 1 ───────────────────────────────────

    /// <summary>
    /// Step 1 — the conversation with the customer. One state covers both the pre-order
    /// talk and the post-serve talk: step 4 (Serve) loops back here, so the same state is
    /// re-entered for every round of conversation.
    ///
    /// This is also the branch point of the whole Open Bar flow. Two legal outs:
    /// - <see cref="RequestPrepareDrinks"/> -> step 2, make a drink
    /// - <see cref="RequestCloseBar"/>      -> leave the container, on to the next big phase
    ///
    /// Neither is automatic. Both are driven from Yarn / UnityEvent through GameFlowCommands —
    /// this state has no idea GamePhase.Close exists.
    /// </summary>
    public class TalkingWithCustomerState : StateBase
    {
        /// <summary>Conversation produced an order. Parent moves to Prepare Drinks.</summary>
        public event Action OnRequestPrepareDrinks;

        /// <summary>Done serving for the day. Parent raises OnRequestExitToClose.</summary>
        public event Action OnRequestCloseBar;

        protected override void OnEnter()
        {
            // TODO: start the conversation node for the current customer.
        }

        protected override void OnExit()
        {
            // TODO: stash the resulting order (see open question #4 — DrinkOrderContext).
        }

        public void RequestPrepareDrinks() => OnRequestPrepareDrinks?.Invoke();

        public void RequestCloseBar() => OnRequestCloseBar?.Invoke();
    }
}
