using UnityEngine;

namespace Bar410.GameFlow
{
    // ── Level 2-3 seam ─────────────────────────────────────

    /// <summary>
    /// Connects the Open Bar flow (steps 1-4 and 2.1-2.2) to the cocktail scene objects.
    ///
    /// Companion to <see cref="BarSetupBridge"/> and MinigameFlowBridge; between them these
    /// three are the only places the flow layer and the gameplay layer meet. The states
    /// stay plain C# and keep knowing nothing about UnityEngine.
    ///
    /// This is deliberately NOT where minigames are started — that is MinigameFlowBridge's
    /// job (Bar410_Minigame_Integration_Plan §3.3). Doing it in both would give the
    /// minigame two owners.
    /// </summary>
    [RequireComponent(typeof(GameLoopFSM))]
    public class CocktailFlowBridge : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private GameLoopFSM _gameLoop;

        [Header("Cocktail Scene Objects")]
        [SerializeField] private CocktailSystemManager _cocktail;
        [SerializeField] private ShakerContents _shakerContents;
        [SerializeField] private ShakerPanelController _shakerPanels;
        [SerializeField] private IngredientButtonGroup _ingredients;

        [Header("Serving Glass")]
        [Tooltip("The tabletop zone a glass is placed in. Available across steps 2.1 and 3 (Garnish) alike — " +
                 "its occupant is destroyed on a PrepareDrinks (re)entry, so every customer starts with an empty table.")]
        [SerializeField] private GlassPlacementZone _glassZone;

        [Header("Behaviour")]
        [Tooltip("Re-enable pouring whenever step 2.1 AddIngredient is entered.")]
        [SerializeField] private bool _driveIngredientButtons = true;

        // ── Unity ──────────────────────────────────────────

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();
            _gameLoop.EnsureBuilt();

            var openBar = _gameLoop.OpenBar;
            var prepareDrinks = openBar.PrepareDrinks;

            openBar.TalkingWithCustomer.Exited += OnConversationExited;
            prepareDrinks.Entered += OnPrepareDrinksEntered;
            prepareDrinks.AddIngredient.Entered += OnAddIngredientEntered;
            prepareDrinks.AddIngredient.Exited += OnAddIngredientExited;
            openBar.Serve.Exited += OnServeExited;
        }

        private void OnDestroy()
        {
            if (_gameLoop == null || _gameLoop.OpenBar == null) return;

            var openBar = _gameLoop.OpenBar;
            var prepareDrinks = openBar.PrepareDrinks;

            openBar.TalkingWithCustomer.Exited -= OnConversationExited;
            prepareDrinks.Entered -= OnPrepareDrinksEntered;
            prepareDrinks.AddIngredient.Entered -= OnAddIngredientEntered;
            prepareDrinks.AddIngredient.Exited -= OnAddIngredientExited;
            openBar.Serve.Exited -= OnServeExited;
        }

        // ── Step 1 · Talking ───────────────────────────────

        private void OnConversationExited()
        {
            // The order itself is placed from Yarn (Order_Cocktail_* / order_by_type), so all
            // this step has to do is notice whether the writer actually placed one.
            if (_cocktail != null && !_cocktail.Order.HasOrder)
                Debug.LogWarning("[CocktailFlowBridge] Leaving the conversation with no order placed — " +
                                 "the .yarn node should call an Order_Cocktail_* function.", this);
        }

        // ── Step 2 · Prepare Drinks ────────────────────────

        private void OnPrepareDrinksEntered()
        {
            // Bar410_StateMachine_Implementation.md §3.1: entering step 2 always restarts the
            // drink, including a backtrack from Garnish or Serve. Making it happen here turns
            // that rule from a convention into structure — previously it depended on whichever
            // button or Yarn command happened to call Reset first.
            if (_shakerContents != null) _shakerContents.Clear();
            if (_shakerPanels != null) _shakerPanels.ResetPermissions();
            if (_cocktail != null) _cocktail.Order.ClearResult();

            // Every customer starts with an empty table — a glass placed for the previous
            // order (or a Garnish backtrack) does not carry over.
            if (_glassZone != null) _glassZone.ClearAndDestroyOccupant();
        }

        // ── Step 2.1 · AddIngredient ───────────────────────

        private void OnAddIngredientEntered()
        {
            if (_driveIngredientButtons && _ingredients != null) _ingredients.SetInteractable(true);
        }

        private void OnAddIngredientExited()
        {
            if (_driveIngredientButtons && _ingredients != null) _ingredients.SetInteractable(false);
        }

        // ── Step 4 · Serve ─────────────────────────────────

        private void OnServeExited()
        {
            // Closes the TODO in ServeState.cs and Bar410_StateMachine_Implementation.md §3.2
            // ("scoring moved to ServeState.OnExit"). Guarded because the existing Serve
            // button already scores through CocktailSystemManager.ServeDrink(); whichever
            // path runs first wins and the other becomes a no-op.
            if (_cocktail == null || _cocktail.Order.IsScored) return;
            if (_shakerContents == null) return;

            var result = _cocktail.Scoring.Score(_cocktail.Order, _shakerContents.CurrentCocktail);
            Debug.Log($"[CocktailFlowBridge] Scored on Serve exit → {result} (payout {_cocktail.Order.Payout})");
        }
    }
}
