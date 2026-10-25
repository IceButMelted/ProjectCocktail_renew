using UnityEngine;
using UnityEngine.UI;
using static E_Cocktail;

namespace Bar410.GameFlow
{
    /// <summary>
    /// The one place the game flow (<see cref="GameLoopFSM"/>) and the managers meet. The states stay
    /// plain C# and know nothing about Unity objects; the managers know neither the flow nor each
    /// other. This component subscribes to a step's events, calls verbs on the managers, and turns
    /// manager events back into flow commands. It decides nothing else.
    ///
    /// What the flow does to the rest of the game, by step:
    ///   1   Talking       lock the ingredients, fix the camera look, close the book;
    ///                     carry a scored order into the dialogue's variables
    ///   2   Prepare       fresh drink, bar camera, post-it in; leaving closes the book
    ///   2.1 AddIngredient bottles and shaker usable; the method panel follows the shaker's contents
    ///   2.2 Minigame      start/stop the minigame; a win finishes the drink; back leaves for 2.1
    ///   3   Garnish       frame the plate, lock the ingredients, open the station; pour, ice, finish, remake
    ///   4   Serve         open/close the serve panel, take the post-it away; serve or remake
    ///
    /// Lives on the same GameObject as <see cref="GameLoopFSM"/> (`[GameLoop]`).
    /// </summary>
    [RequireComponent(typeof(GameLoopFSM))]
    public class FlowWiring : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private CocktailSystemManager _cocktail;
        [SerializeField] private MinigameSystemManager _minigames;
        [SerializeField] private GarnishManager _garnish;
        [SerializeField] private TalkToCustomerManager _customers;
        [SerializeField] private CameraManager _cameras;

        [Header("Cocktail scene objects")]
        [SerializeField] private ShakerContents _shakerContents;
        [SerializeField] private ShakerPanelController _shakerPanels;
        [SerializeField] private IngredientButtonGroup _ingredients;
        [SerializeField] private VisualizeCocktail _visualCocktail;
        [Tooltip("Owns the Shake / Stir / Reset buttons. This component only decides what each press means.")]
        [SerializeField] private MethodPanel _methodPanel;

        [Header("Chrome")]
        [SerializeField] private BookUI_V2 _bookUI;
        [Tooltip("The recipe-book button in the Hub. It is a member of IngredientButtonGroup, so locking the " +
                 "ingredients locks it too; it is switched back on when the drink fills.")]
        [SerializeField] private Button _btnBook;
        [SerializeField] private Post_It_Order _postIt;

        private GameLoopFSM _gameLoop;
        private GameFlowCommands _commands;

        // ── Unity ──────────────────────────────────────────

        private void Awake()
        {
            _gameLoop = GetComponent<GameLoopFSM>();
            _commands = GetComponent<GameFlowCommands>();

            var openBar = _gameLoop.OpenBar;
            var prepareDrinks = openBar.PrepareDrinks;

            // 1 Talking
            openBar.TalkingWithCustomer.Entered += OnTalkingEntered;
            openBar.TalkingWithCustomer.Exited += OnConversationExited;

            if (_cocktail != null && _customers != null)
            {
                _cocktail.UsePreferences(_customers.Preferences);
                _cocktail.OrderScored += _customers.CommitOrderResult;
                _cocktail.OrderCleared += _customers.ResetVariables;
            }
            else Debug.LogWarning("[FlowWiring] Cocktail or customers missing: scored orders will not reach the dialogue.", this);

            // 2 Prepare drinks, 2.1 AddIngredient
            prepareDrinks.Entered += OnPrepareDrinksEntered;
            prepareDrinks.Exited += OnPrepareDrinksExited;
            prepareDrinks.AddIngredient.Entered += OnAddIngredientEntered;
            prepareDrinks.AddIngredient.Exited += OnAddIngredientExited;
            openBar.Sub.OnTransition += OnOpenBarTransition;

            // What the shaker's contents do to the scene (were Inspector UnityEvents on ShakerContents).
            if (_shakerContents != null)
            {
                _shakerContents.Changed.AddListener(OnShakerChanged);
                _shakerContents.Cleared.AddListener(OnShakerCleared);
                _shakerContents.OnFulled.AddListener(OnShakerFull);
            }

            if (_methodPanel != null)
            {
                _methodPanel.ShakingChosen += OnShakingClicked;
                _methodPanel.MixingChosen += OnMixingClicked;
                _methodPanel.ResetChosen += OnMethodResetClicked;
            }

            if (_btnBook != null) _btnBook.onClick.AddListener(OnOpenBookClicked);

            // 2.2 Minigame
            prepareDrinks.Minigame.OnStartRequested += OnMinigameStartRequested;
            prepareDrinks.Minigame.OnStopRequested += OnMinigameStopRequested;

            if (_minigames != null)
            {
                _minigames.MinigameFinished += OnMinigameFinished;
                _minigames.BackRequested += OnMinigameBackRequested;
            }
            else Debug.LogWarning("[FlowWiring] No MinigameSystemManager assigned — step 2.2 will run nothing.", this);

            // 3 Garnish
            openBar.Garnish.Entered += OnGarnishEntered;
            openBar.Garnish.Exited += OnGarnishExited;

            // Every customer starts with an empty table: a glass placed for the previous order (or
            // a backtrack from Garnish/Serve) does not carry over, and neither does the served one.
            // Both run on every way in, remake included.
            prepareDrinks.Entered += OnTableReset;
            openBar.Serve.Exited += OnTableReset;

            // 4 Serve
            openBar.Serve.Entered += OnServeEntered;
            openBar.Serve.Exited += OnServeExited;

            if (_garnish != null)
            {
                _garnish.PourRequested += OnPourRequested;
                _garnish.IceToggled += OnIceToggled;
                _garnish.FinishRequested += OnFinishRequested;
                _garnish.ResetRequested += OnRemakeRequested;
                _garnish.ServeRequested += OnServeRequested;
                _garnish.RemakeRequested += OnRemakeRequested;
            }
            else Debug.LogWarning("[FlowWiring] No GarnishManager assigned — steps 3 and 4 will show nothing.", this);
        }

        private void OnDestroy()
        {
            if (_cocktail != null && _customers != null)
            {
                _cocktail.OrderScored -= _customers.CommitOrderResult;
                _cocktail.OrderCleared -= _customers.ResetVariables;
            }

            if (_shakerContents != null)
            {
                _shakerContents.Changed.RemoveListener(OnShakerChanged);
                _shakerContents.Cleared.RemoveListener(OnShakerCleared);
                _shakerContents.OnFulled.RemoveListener(OnShakerFull);
            }

            if (_methodPanel != null)
            {
                _methodPanel.ShakingChosen -= OnShakingClicked;
                _methodPanel.MixingChosen -= OnMixingClicked;
                _methodPanel.ResetChosen -= OnMethodResetClicked;
            }

            if (_btnBook != null) _btnBook.onClick.RemoveListener(OnOpenBookClicked);

            if (_minigames != null)
            {
                _minigames.MinigameFinished -= OnMinigameFinished;
                _minigames.BackRequested -= OnMinigameBackRequested;
            }

            if (_garnish != null)
            {
                _garnish.PourRequested -= OnPourRequested;
                _garnish.IceToggled -= OnIceToggled;
                _garnish.FinishRequested -= OnFinishRequested;
                _garnish.ResetRequested -= OnRemakeRequested;
                _garnish.ServeRequested -= OnServeRequested;
                _garnish.RemakeRequested -= OnRemakeRequested;
            }

            if (_gameLoop == null || _gameLoop.OpenBar == null) return;

            var openBar = _gameLoop.OpenBar;
            var prepareDrinks = openBar.PrepareDrinks;

            openBar.TalkingWithCustomer.Entered -= OnTalkingEntered;
            openBar.TalkingWithCustomer.Exited -= OnConversationExited;
            prepareDrinks.Entered -= OnPrepareDrinksEntered;
            prepareDrinks.Exited -= OnPrepareDrinksExited;
            prepareDrinks.AddIngredient.Entered -= OnAddIngredientEntered;
            prepareDrinks.AddIngredient.Exited -= OnAddIngredientExited;
            openBar.Sub.OnTransition -= OnOpenBarTransition;
            prepareDrinks.Minigame.OnStartRequested -= OnMinigameStartRequested;
            prepareDrinks.Minigame.OnStopRequested -= OnMinigameStopRequested;
            openBar.Garnish.Entered -= OnGarnishEntered;
            openBar.Garnish.Exited -= OnGarnishExited;
            prepareDrinks.Entered -= OnTableReset;
            openBar.Serve.Exited -= OnTableReset;
            openBar.Serve.Entered -= OnServeEntered;
            openBar.Serve.Exited -= OnServeExited;
        }

        // ── 1 · Talking ────────────────────────────────────

        private void OnTalkingEntered()
        {
            // Covers both the very first customer and every Serve → Talking loop.
            _ingredients?.Disable();
            _cameras?.SetLookFixed(true);
            _bookUI?.CloseBook();
        }

        private void OnConversationExited()
        {
            // The order itself is placed from Yarn (Order_Cocktail_* / order_by_type), so all
            // this step has to do is notice whether the writer actually placed one.
            if (_cocktail != null && !_cocktail.Order.HasOrder)
                Debug.LogWarning("[FlowWiring] Leaving the conversation with no order placed — " +
                                 "the .yarn node should call an Order_Cocktail_* function.", this);
        }

        // ── 2 · Prepare drinks ─────────────────────────────

        private void OnPrepareDrinksEntered()
        {
            // Bar410_StateMachine_Implementation.md §3.1: entering step 2 always restarts the
            // drink, including a backtrack from Garnish or Serve. Making it happen here turns
            // that rule from a convention into structure — previously it depended on whichever
            // button or Yarn command happened to call Reset first.
            if (_shakerContents != null) _shakerContents.Clear();
            if (_shakerPanels != null) _shakerPanels.ResetPermissions();
            if (_cocktail != null) _cocktail.Order.ClearResult();

            _cameras?.ResetLook();
            _cameras?.ShowBar();
            _postIt?.SetAvailable(true);
        }

        private void OnPrepareDrinksExited()
        {
            _bookUI?.CloseBook();
            _postIt?.SetAvailable(false);
        }

        // The recipe-book button lives in the Hub, outside the book panel (which is inactive while
        // closed and so cannot listen for it), and its lock rules are in this component anyway.
        private void OnOpenBookClicked() => _bookUI?.SetActive(true);

        // ── 2.1 · AddIngredient ────────────────────────────

        private void OnAddIngredientEntered()
        {
            _ingredients?.SetInteractable(true);

            // Phase-specific interactable policy (bottle drag + click).
            _ingredients?.EnableInteractablePrepareDrinksPhase();

            // Testing fallback: only fills in a target when dialogue hasn't placed a real one.
            _cocktail?.RandomCocktailIfNoOrder();

            // Re-entering 2.1 with existing content (e.g. cancelling out of the minigame) needs
            // both panels back — they only otherwise react to the next Changed/Cleared event, and
            // nothing fires one on a bare state re-entry. Both no-op while the shaker is empty
            // (a fresh PrepareDrinks entry already cleared it).
            if (_shakerPanels != null) _shakerPanels.ShowMethod();
            if (_visualCocktail != null) _visualCocktail.UpdateCocktailBars();
        }

        private void OnAddIngredientExited()
        {
            _ingredients?.SetInteractable(false);

            // The method panel and the fill-bar readout are only meaningful while actually
            // picking ingredients — force both closed on the way out (to Minigame or a
            // backtrack) instead of leaving them open until the next Changed/Cleared event.
            if (_shakerPanels != null) _shakerPanels.HideMethod();
            if (_visualCocktail != null) _visualCocktail.ResetVisualBars();
        }

        // The shaker's contents drive these (same order as the old Inspector calls).

        private void OnShakerChanged() => _visualCocktail?.UpdateCocktailBars();

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
            _cameras?.ShowShaker();
        }

        // Method panel · Shaking / Mixing / Reset

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
            _methodPanel?.Hide();

            // A full shaker moved the camera onto it (OnShakerFull); the flow stays in 2.1 here,
            // so no state entry puts it back on the bar.
            _cameras?.ShowBar();
        }

        // ── 2.2 · Minigame ─────────────────────────────────
        // A minigame cannot be lost (design rule, 2026-08-21): a run ends Completed or, if the player
        // backs out, Cancelled. Nothing here handles a "failed" branch, and Cancelled always means
        // "the player left". Every way out of the minigame goes through the HSM: cancelling the run
        // directly once left the flow in 2.2 with nothing running.

        private void OnMinigameBackRequested() => _commands?.AnotherIngredient();

        private void OnMinigameStartRequested(Enum_MiniGameType selected)
        {
            if (_minigames == null) return;

            var fromDrink = _shakerContents != null
                ? _shakerContents.RequiredMinigame
                : Enum_MiniGameType.None;

            _minigames.StartMinigame(_minigames.Resolve(selected, fromDrink));
        }

        private void OnMinigameStopRequested()
        {
            // Leaving 2.2 by any route — another ingredient, drink complete, or a backtrack
            // from Garnish/Serve. The outro still plays and reports Cancelled on its own.
            if (_minigames == null) return;
            if (_minigames.ActivePhase == MinigamePhase.Idle) return;

            _minigames.CancelMinigame();
        }

        private void OnMinigameFinished(Enum_MiniGameType type, MinigameOutcome outcome)
        {
            // Fires once per run, after the outro. A run cancelled *because* the flow left 2.2
            // still reports here a moment later; MinigameState clears LastOutcome on every
            // entry, so a stale report can never be mistaken for the next run's result.
            _gameLoop.OpenBar.PrepareDrinks.Minigame.ReportResult(outcome);

            // Cancelling means the player backed out, so the shaker has to become grabbable
            // again. It is a member of IngredientButtonGroup, which locks it when the drink fills.
            // A Completed run is left locked: the drink is finished and the glass takes over.
            if (outcome == MinigameOutcome.Cancelled && _shakerContents != null)
                InteractableToggle.Apply(_shakerContents.gameObject, true);

            // Winning finishes the drink: 2.2 exits straight to step 3.
            if (outcome != MinigameOutcome.Completed) return;
            if (!InMinigameStep()) return;

            _commands?.DrinkComplete();
        }

        /// <summary>True only while the flow is actually sitting in step 2.2.</summary>
        private bool InMinigameStep()
        {
            if (_gameLoop.Machine.Current != GamePhase.Open) return false;
            if (_gameLoop.OpenBar.Sub.Current != OpenSub.PrepareDrinks) return false;

            return _gameLoop.OpenBar.PrepareDrinks.Sub.Current == PrepSub.Minigame;
        }

        // ── 3 · Garnish ────────────────────────────────────

        private void OnGarnishEntered()
        {
            // The drink's identity (name/colour/etc.) is resolved against the recipe database
            // once mixing is done, not on every ingredient add — Garnish entry is that "done
            // mixing" moment. Without this, S_Drink.waterColorTop/Bottom stay at their unset
            // (fully transparent) default and the poured glass looks empty even with liquid in it.
            _cocktail?.UpdateCocktailInShaker();

            _cameras?.ShowGarnish();
            _ingredients?.Disable();
            _garnish?.Open();
            _postIt?.SetAvailable(true);
        }

        private void OnGarnishExited()
        {
            _garnish?.Close();
            _postIt?.SetAvailable(false);
        }

        private void OnTableReset() => _garnish?.ClearGlass();

        private void OnPourRequested()
        {
            if (_shakerContents == null || _shakerContents.IsEmpty)
            {
                Debug.LogWarning("[FlowWiring] Nothing in the shaker to pour.", this);
                return;
            }

            _garnish.Pour(_shakerContents.CurrentCocktail);
        }

        private void OnIceToggled(bool enable) => _shakerContents?.SetIce(enable);

        private void OnFinishRequested() => _commands?.GarnishDone();

        // ── 4 · Serve ──────────────────────────────────────

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

        private void OnServeEntered() => _garnish?.OpenServe();

        private void OnServeExited() => _garnish?.CloseServe();

        private void OnServeRequested() => _commands?.ServeDone();

        // ── Remake: throw the drink away and start it again ─
        // From the garnish station's Reset button or the serve panel's Remake button.

        private void OnRemakeRequested()
        {
            _cocktail?.ResetCocktail();
            _commands?.RemakeDrink();
        }
    }
}
