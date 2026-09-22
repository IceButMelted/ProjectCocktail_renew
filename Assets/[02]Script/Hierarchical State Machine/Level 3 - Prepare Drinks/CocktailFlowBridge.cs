using UnityEngine;
using UnityEngine.UI;
using static E_Cocktail;

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
        [SerializeField] private GameFlowCommands _commands;

        [Header("Cocktail Scene Objects")]
        [SerializeField] private CocktailSystemManager _cocktail;
        [SerializeField] private ShakerContents _shakerContents;
        [SerializeField] private ShakerPanelController _shakerPanels;
        [SerializeField] private IngredientButtonGroup _ingredients;
        [SerializeField] private VisualizeCocktail _visualCocktail;

        [Header("Serving Glass")]
        [Tooltip("The tabletop zone a glass is placed in. Available across steps 2.1 and 3 (Garnish) alike — " +
                 "its occupant is destroyed on a PrepareDrinks (re)entry, so every customer starts with an empty table.")]
        [SerializeField] private GlassPlacementZone _glassZone;

        [Header("Behaviour")]
        [Tooltip("Re-enable pouring whenever step 2.1 AddIngredient is entered.")]
        [SerializeField] private bool _driveIngredientButtons = true;

        [Header("Camera / Book / Post-it")]
        [SerializeField] private CameraController _camera;
        [SerializeField] private CinimachineCameraSwitcher _cameraSwitcher;
        [SerializeField] private string _prepareDrinksCameraId = "MainCam";
        [SerializeField] private BookUI_V2 _bookUI;
        [SerializeField] private Post_It_Order _postIt;

        [Tooltip("Hidden defensively on AddIngredient entry — Serve is also always closed by ServeFlowBridge on its own Exit.")]
        [SerializeField] private GameObject _panelServe;

        [Header("Method Panel (2.1 — Shaking / Stirring / Reset)")]
        [SerializeField] private Button _btnShaking;
        [SerializeField] private Button _btnMixing;
        [SerializeField] private Button _btnMethodReset;
        [SerializeField] private GameObject _panelVisualCocktail;
        [SerializeField] private GameObject _panelMethod;

        // ── Unity ──────────────────────────────────────────

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();
            if (_commands == null) _commands = GetComponent<GameFlowCommands>();
            _gameLoop.EnsureBuilt();

            var openBar = _gameLoop.OpenBar;
            var prepareDrinks = openBar.PrepareDrinks;

            openBar.TalkingWithCustomer.Exited += OnConversationExited;
            prepareDrinks.Entered += OnPrepareDrinksEntered;
            prepareDrinks.Exited += OnPrepareDrinksExited;
            prepareDrinks.AddIngredient.Entered += OnAddIngredientEntered;
            prepareDrinks.AddIngredient.Exited += OnAddIngredientExited;
            openBar.Serve.Exited += OnServeExited;

            if (_btnShaking != null) _btnShaking.onClick.AddListener(OnShakingClicked);
            if (_btnMixing != null) _btnMixing.onClick.AddListener(OnMixingClicked);
            if (_btnMethodReset != null) _btnMethodReset.onClick.AddListener(OnMethodResetClicked);
        }

        private void OnDestroy()
        {
            if (_btnShaking != null) _btnShaking.onClick.RemoveListener(OnShakingClicked);
            if (_btnMixing != null) _btnMixing.onClick.RemoveListener(OnMixingClicked);
            if (_btnMethodReset != null) _btnMethodReset.onClick.RemoveListener(OnMethodResetClicked);

            if (_gameLoop == null || _gameLoop.OpenBar == null) return;

            var openBar = _gameLoop.OpenBar;
            var prepareDrinks = openBar.PrepareDrinks;

            openBar.TalkingWithCustomer.Exited -= OnConversationExited;
            prepareDrinks.Entered -= OnPrepareDrinksEntered;
            prepareDrinks.Exited -= OnPrepareDrinksExited;
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

            _camera?.ResetRotaionAndMovement();
            _cameraSwitcher?.SwitchCamera(_prepareDrinksCameraId);
            _postIt?.Init();
        }

        private void OnPrepareDrinksExited() => _bookUI?.CloseBook();

        // ── Step 2.1 · AddIngredient ───────────────────────

        private void OnAddIngredientEntered()
        {
            if (_driveIngredientButtons && _ingredients != null) _ingredients.SetInteractable(true);

            // Phase-specific interactable policy (bottle drag + click) — was previously the
            // GameFlowHooks-only half of this entry; ported here so nothing is lost.
            _ingredients?.EnableInteractablePrepareDrinksPhase();

            // Testing fallback: only fills in a target when dialogue hasn't placed a real one.
            _cocktail?.RandomCocktailIfNoOrder();

            // Re-entering 2.1 with existing content (e.g. cancelling out of the minigame) needs
            // both panels back — they only otherwise react to the next Changed/Cleared event, and
            // nothing fires one on a bare state re-entry. Both no-op while the shaker is empty
            // (fresh PrepareDrinks entry already cleared it via OnPrepareDrinksEntered above).
            if (_shakerPanels != null) _shakerPanels.ShowMethod();
            if (_visualCocktail != null) _visualCocktail.UpdateCocktailBars();

            // Defensive — ServeFlowBridge already closes this on its own Serve.Exited, but a
            // stray Serve panel left open by any other path would strand the player mid-pour.
            _panelServe?.SetActive(false);
        }

        private void OnAddIngredientExited()
        {
            if (_driveIngredientButtons && _ingredients != null) _ingredients.SetInteractable(false);

            // The method panel and the fill-bar readout are only meaningful while actually
            // picking ingredients — force both closed on the way out (to Minigame or a
            // backtrack) instead of leaving them open until the next Changed/Cleared event.
            if (_shakerPanels != null) _shakerPanels.HideMethod();
            if (_visualCocktail != null) _visualCocktail.ResetVisualBars();
        }

        // ── Step 4 · Serve ─────────────────────────────────

        private void OnServeExited()
        {
            // The served glass never carries over — the next customer always gets a fresh one
            // from GlassPlacementZone.SetGlass. Unconditional, independent of the scoring guard
            // below, so it still runs even when scoring already happened via the legacy path.
            if (_glassZone != null) _glassZone.ClearAndDestroyOccupant();

            // Closes the TODO in ServeState.cs and Bar410_StateMachine_Implementation.md §3.2
            // ("scoring moved to ServeState.OnExit"). Guarded because the existing Serve
            // button already scores through CocktailSystemManager.ServeDrink(); whichever
            // path runs first wins and the other becomes a no-op.
            if (_cocktail == null || _cocktail.Order.IsScored) return;
            if (_shakerContents == null) return;

            var result = _cocktail.Scoring.Score(_cocktail.Order, _shakerContents.CurrentCocktail);
            Debug.Log($"[CocktailFlowBridge] Scored on Serve exit → {result} (payout {_cocktail.Order.Payout})");
        }

        // ── Method panel · Shaking / Mixing / Reset ────────

        private void OnShakingClicked()
        {
            _shakerContents?.SetMethod(Method.Shaking);
            _commands?.SelectShaking();
            _commands?.IngredientAdded();
        }

        private void OnMixingClicked()
        {
            _shakerContents?.SetMethod(Method.Stirring);
            _commands?.SelectStiring();
            _commands?.IngredientAdded();
        }

        private void OnMethodResetClicked()
        {
            _cocktail?.ResetCocktail();
            _visualCocktail?.UpdateCocktailBars();
            _panelVisualCocktail?.SetActive(false);
            _panelMethod?.SetActive(false);
        }
    }
}
