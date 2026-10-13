using UnityEngine;
using UnityEngine.UI;
using static E_Cocktail;

namespace Bar410.GameFlow
{
    // ── Level 2-3 seam ─────────────────────────────────────

    /// <summary>
    /// Connects the Open Bar flow (steps 1-4 and 2.1-2.2) to the cocktail scene objects.
    ///
    /// Companion to MinigameFlowBridge, GarnishFlowBridge, ServeFlowBridge and
    /// TalkingWithCustomerBridge; between them these are the only places the flow layer and
    /// the gameplay layer meet. The states stay plain C# and keep knowing nothing about
    /// UnityEngine.
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

        [Header("Shaker events (were Inspector UnityEvents on ShakerContents)")]
        [Tooltip("Camera that frames the shaker once the drink is full and the method panel opens.")]
        [SerializeField] private string _fullDrinkCameraId = "PrepareCam";
        [Tooltip("The recipe-book button. Locking the ingredients when the drink fills also locks " +
                 "this button (it is a member of IngredientButtonGroup), so it is switched back on.")]
        [SerializeField] private Button _btnBook;

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

            var openBar = _gameLoop.OpenBar;
            var prepareDrinks = openBar.PrepareDrinks;

            openBar.TalkingWithCustomer.Exited += OnConversationExited;
            prepareDrinks.Entered += OnPrepareDrinksEntered;
            prepareDrinks.Exited += OnPrepareDrinksExited;
            prepareDrinks.AddIngredient.Entered += OnAddIngredientEntered;
            prepareDrinks.AddIngredient.Exited += OnAddIngredientExited;
            openBar.Serve.Exited += OnServeExited;
            openBar.Sub.OnTransition += OnOpenBarTransition;

            if (_shakerContents != null)
            {
                _shakerContents.Changed.AddListener(OnShakerChanged);
                _shakerContents.Cleared.AddListener(OnShakerCleared);
                _shakerContents.OnFulled.AddListener(OnShakerFull);
            }

            if (_btnShaking != null) _btnShaking.onClick.AddListener(OnShakingClicked);
            if (_btnMixing != null) _btnMixing.onClick.AddListener(OnMixingClicked);
            if (_btnMethodReset != null) _btnMethodReset.onClick.AddListener(OnMethodResetClicked);
        }

        private void OnDestroy()
        {
            if (_shakerContents != null)
            {
                _shakerContents.Changed.RemoveListener(OnShakerChanged);
                _shakerContents.Cleared.RemoveListener(OnShakerCleared);
                _shakerContents.OnFulled.RemoveListener(OnShakerFull);
            }

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
            openBar.Sub.OnTransition -= OnOpenBarTransition;
        }

        // ── Shaker → scene reactions ───────────────────────
        // These were persistent UnityEvent calls on ShakerContents, authored in the Inspector and
        // invisible to a code search. They run in the same order as the old calls.

        private void OnShakerChanged()
        {
            _visualCocktail?.UpdateCocktailBars();
        }

        private void OnShakerCleared()
        {
            _visualCocktail?.ResetVisualBars();
            _shakerPanels?.HideMethod();
            _ingredients?.EnableInteractablePrepareDrinksPhase();
        }

        private void OnShakerFull()
        {
            _ingredients?.Disable();
            _shakerPanels?.ShowMethod();
            if (_btnBook != null) _btnBook.interactable = true;
            _cameraSwitcher?.SwitchCamera(_fullDrinkCameraId);
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
            PlacedGlassInstance.DestroyCurrent();

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
            // from GarnishFlowBridge.ChooseGlass. Runs on every way out of Serve, remake included.
            PlacedGlassInstance.DestroyCurrent();
        }

        /// <summary>
        /// The one place a drink is scored. Serve → Talking is the only transition that means
        /// "handed over": a remake leaves Serve for Prepare Drinks and must not score or write
        /// anything to Yarn. Whoever finishes the step (the Serve button, &lt;&lt;flow_serve_done&gt;&gt;,
        /// a debug hotkey) gets the same commit, and a second call is a no-op.
        /// </summary>
        private void OnOpenBarTransition(OpenSub from, OpenSub to)
        {
            if (from == OpenSub.Serve && to == OpenSub.TalkingWithCustomer)
                _cocktail?.ServeDrink();
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
