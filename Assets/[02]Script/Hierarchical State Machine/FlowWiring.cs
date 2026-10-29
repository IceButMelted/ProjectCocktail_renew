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
    ///   BeforeOpen    cuts to the bar shot, look fixed; waits for Panel_Transition first; Event locks the bar and shows the queued
    ///                 popups (EventPopupQueue), finishing when the last is closed;
    ///                 StoryBeforeOpen starts the day's node (SO_DaySchedule by $day) on its first entry
    ///   Open          shows the day's chapter title (ChapterTitleUI)
    ///   1   Talking      lock the ingredients, fix the camera look, close the book;
    ///                     carry a scored order into the dialogue's variables
    ///   2   Prepare       fresh drink, bar camera, post-it in; leaving closes the book
    ///   2.1 AddIngredient bottles and shaker usable (locked bottles hidden, IngredientUnlocks); the method
    ///                     panel follows the shaker's contents; mouse look only here, while the shaker has room
    ///   2.2 Minigame      start/stop the minigame; look fixed; a win finishes the drink; back leaves for 2.1
    ///   3   Garnish       frame the plate, lock the ingredients, open the station; pour, ice, finish, remake
    ///   4   Serve         open/close the serve panel, take the post-it away; serve or remake;
    ///                     leaving it returns to the bar shot
    ///   Close             nothing but the Hub UI yet; leaving it (next day) adds 1 to $day
    ///
    /// Every step's Entered also tells <see cref="HubManager"/> the new Phase, which shows and hides the
    /// Hub UI (the buttons on Canvas_HUD) from its Inspector table.
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
        [Tooltip("Shows and hides the Hub UI per Phase (table in its Inspector).")]
        [SerializeField] private HubManager _hub;
        [Tooltip("The day starts (BeforeOpen) only after this finishes playing. Empty = start at once. " +
                 "Turn off GameLoopFSM's Auto Start and the DialogueRunner's auto start when this is set.")]
        [SerializeField] private PanelTransition _transition;
        [Tooltip("Which Yarn node each day's story starts at ($day). Empty = the DialogueRunner's Start Node.")]
        [SerializeField] private SO_DaySchedule _daySchedule;
        [Tooltip("Shows the day's Chapter Name (from the day schedule) when the bar opens. Empty = none.")]
        [SerializeField] private ChapterTitleUI _chapterTitle;
        [Tooltip("The day's event popups. BeforeOpen › Event waits until the player has closed them all. Empty = Event passes at once.")]
        [SerializeField] private EventPopupQueue _eventPopups;
        [Tooltip("Which ingredients the player has. The bar shows only those, random orders use only recipes made of them, " +
                 "and Event announces the new ones. Empty = everything is available.")]
        [SerializeField] private IngredientUnlocks _unlocks;

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

            // Random orders only ask for drinks made of ingredients the player has.
            if (_cocktail != null && _unlocks != null) _cocktail.UseRecipeFilter(_unlocks.IsRecipeComplete);
            // The bar is on screen in every phase, so an unlock shows its bottle at once.
            if (_unlocks != null) _unlocks.Changed += RefreshBottles;

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
            prepareDrinks.Minigame.Entered += OnMinigameEntered;

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

            // Before Open / Close
            _gameLoop.BeforeOpen.Entered += OnBeforeOpenEntered;
            _gameLoop.BeforeOpen.Event.Entered += OnEventEntered;
            _gameLoop.BeforeOpen.Story.Entered += OnStoryEntered;
            _gameLoop.ClosingBar.Entered += OnClosingEntered;
            _gameLoop.ClosingBar.Exited += OnClosingExited;
            openBar.Entered += OnOpenEntered;
            if (_eventPopups != null) _eventPopups.AllClosed += OnEventPopupsClosed;

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

        private System.Collections.IEnumerator Start()
        {
            if (_transition == null) yield break;

            // Nothing is entered yet, so lock the bar and hide the Hub UI while the panel plays.
            LockBar();
            _hub?.SetPhase(HubPhase.None);

            yield return _transition.WaitUntilFinished();
            _gameLoop.StartDay();
        }

        private void OnDestroy()
        {
            if (_gameLoop != null && _gameLoop.ClosingBar != null)
            {
                _gameLoop.ClosingBar.Entered -= OnClosingEntered;
                _gameLoop.ClosingBar.Exited -= OnClosingExited;
            }
            if (_eventPopups != null) _eventPopups.AllClosed -= OnEventPopupsClosed;
            if (_unlocks != null) _unlocks.Changed -= RefreshBottles;
            if (_gameLoop != null && _gameLoop.BeforeOpen != null)
            {
                _gameLoop.BeforeOpen.Entered -= OnBeforeOpenEntered;
                _gameLoop.BeforeOpen.Event.Entered -= OnEventEntered;
                _gameLoop.BeforeOpen.Story.Entered -= OnStoryEntered;
            }

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

            openBar.Entered -= OnOpenEntered;
            openBar.TalkingWithCustomer.Entered -= OnTalkingEntered;
            openBar.TalkingWithCustomer.Exited -= OnConversationExited;
            prepareDrinks.Entered -= OnPrepareDrinksEntered;
            prepareDrinks.Exited -= OnPrepareDrinksExited;
            prepareDrinks.AddIngredient.Entered -= OnAddIngredientEntered;
            prepareDrinks.AddIngredient.Exited -= OnAddIngredientExited;
            openBar.Sub.OnTransition -= OnOpenBarTransition;
            prepareDrinks.Minigame.OnStartRequested -= OnMinigameStartRequested;
            prepareDrinks.Minigame.OnStopRequested -= OnMinigameStopRequested;
            prepareDrinks.Minigame.Entered -= OnMinigameEntered;
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
            _hub?.SetPhase(HubPhase.Talking);
            _ingredients?.Disable();
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
            //_ingredients?.EnableInteractablePrepareDrinksPhase();
        }

        private void OnPrepareDrinksExited()
        {
            _bookUI?.CloseBook();
            _postIt?.SetAvailable(false);
            _cameras?.SetLookFixed(true);
        }

        // The recipe-book button lives in the Hub, outside the book panel (which is inactive while
        // closed and so cannot listen for it), and its lock rules are in this component anyway.
        private void OnOpenBookClicked() => _bookUI?.SetActive(true);

        // ── 2.1 · AddIngredient ────────────────────────────

        private void OnAddIngredientEntered()
        {
            _hub?.SetPhase(HubPhase.AddIngredient);

            // Today's bottles: hide the locked ones, show any that unlocked since the last visit.
            RefreshBottles();
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

            RefreshLook();
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
            _cameras?.ShowBar();
            RefreshLook();
        }

        private void OnShakerFull()
        {
            _ingredients?.Disable();
            _cameras?.SetLookFixed(true);
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
            RefreshLook();
        }

        // ── 2.2 · Minigame ─────────────────────────────────
        // A minigame cannot be lost (design rule, 2026-08-21): a run ends Completed or, if the player
        // backs out, Cancelled. Nothing here handles a "failed" branch, and Cancelled always means
        // "the player left". Every way out of the minigame goes through the HSM: cancelling the run
        // directly once left the flow in 2.2 with nothing running.

        private void OnMinigameEntered()
        {
            _hub?.SetPhase(HubPhase.Minigame);
            RefreshLook();
        }

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
            if (!_gameLoop.Machine.IsRunning || _gameLoop.Machine.Current != GamePhase.Open) return false;
            if (_gameLoop.OpenBar.Sub.Current != OpenSub.PrepareDrinks) return false;

            return _gameLoop.OpenBar.PrepareDrinks.Sub.Current == PrepSub.Minigame;
        }

        /// <summary>True only while the flow is actually sitting in step 2.1.</summary>
        private bool InAddIngredientStep()
        {
            if (!_gameLoop.Machine.IsRunning || _gameLoop.Machine.Current != GamePhase.Open) return false;
            if (_gameLoop.OpenBar.Sub.Current != OpenSub.PrepareDrinks) return false;

            return _gameLoop.OpenBar.PrepareDrinks.Sub.Current == PrepSub.AddIngredient;
        }

        // Mouse look only while picking ingredients on the bar shot. Fixed on the shaker shot
        // (shaker full), in the minigame and everywhere else; fixing also snaps the view forward.
        private void RefreshLook()
        {
            bool free = InAddIngredientStep() && _shakerContents != null && _shakerContents.HasRoom;
            _cameras?.SetLookFixed(!free);
        }

        // ── 3 · Garnish ────────────────────────────────────

        private void OnGarnishEntered()
        {
            _hub?.SetPhase(HubPhase.Garnish);

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

        private void OnServeEntered()
        {
            _hub?.SetPhase(HubPhase.Serve);
            _garnish?.OpenServe();
        }

        // ── Before Open / Close ────────────────────────────

        // A new day always opens on the bar, look fixed and forward. Not on Event: flow_check_events
        // re-enters Event mid-story and must not move the writer's shot.
        private void OnBeforeOpenEntered()
        {
            _cameras?.SetLookFixed(true);
            _cameras?.ResetLook();
            _cameras?.CutToBar();
        }

        private void OnEventEntered()
        {
            // Event is always BeforeOpen's first step, so the bar is locked here: same as Talking,
            // day events play out before anything can be poured. Harmless again on Story → Event.
            LockBar();
            _hub?.SetPhase(HubPhase.BeforeOpen);
            // A new $day may have unlocked bottles; show them before the day starts.
            RefreshBottles();

            // Nothing queued (or no popup UI) passes straight on; ShowAll raises AllClosed at once then.
            if (_eventPopups == null) { _gameLoop.BeforeOpen.Event.Finish(); return; }

            // Ingredients unlocked since the last Event, announced in one popup.
            var unlocked = _unlocks != null ? _unlocks.TakeNewUnlocks() : null;
            if (unlocked != null && unlocked.Count > 0)
            {
                var names = new System.Collections.Generic.List<string>();
                var icons = new System.Collections.Generic.List<Sprite>();
                foreach (var row in unlocked)
                {
                    names.Add(row.DisplayOrName);
                    if (row.icon != null) icons.Add(row.icon);
                }
                _eventPopups.Enqueue("Unlock New Ingredient", string.Join("\n", names), icons);
            }

            _eventPopups.ShowAll();
        }

        /// <summary>Shows the bottles the player has, hides the locked ones.</summary>
        private void RefreshBottles()
        {
            if (_unlocks != null) _ingredients?.Refresh(_unlocks.IsUnlocked);
        }

        private void OnEventPopupsClosed()
        {
            if (_gameLoop.Machine.IsRunning && _gameLoop.Machine.Current == GamePhase.BeforeOpen
                && _gameLoop.BeforeOpen.Sub.Current == BeforeOpenSub.Event)
                _gameLoop.BeforeOpen.Event.Finish();
        }

        private void OnStoryEntered()
        {
            // Back from a mid-story Event the dialogue is still running; only the day's first entry starts it.
            if (!_gameLoop.BeforeOpen.Story.IsFirstEntry || _customers == null) return;

            if (_daySchedule == null) { _customers.StartStory(); return; }

            if (TryGetToday(out var row)) _customers.StartStory(row.startNode);
            else Debug.LogError($"[FlowWiring] {_daySchedule.name} has no row for day {_customers.CurrentDay} — story not started.", this);
        }

        private void OnOpenEntered()
        {
            if (_chapterTitle != null && TryGetToday(out var row)) _chapterTitle.Show(row.chapterName);
        }

        /// <summary>Today's row of the day schedule ($day), or false when there is no schedule or row.</summary>
        private bool TryGetToday(out SO_DaySchedule.Day row)
        {
            row = null;
            return _daySchedule != null && _customers != null && _daySchedule.TryGet(_customers.CurrentDay, out row);
        }

        private void LockBar()
        {
            _ingredients?.Disable();
            _cameras?.SetLookFixed(true);
            _bookUI?.CloseBook();
        }

        private void OnClosingEntered() => _hub?.SetPhase(HubPhase.Close);

        // Close only ever leaves for BeforeOpen, and Exit runs before the next Enter, so the new
        // day's Story already reads the new $day. The game's first BeforeOpen comes from StartDay
        // and never passes here, so day 1 stays day 1.
        private void OnClosingExited() => _customers?.AdvanceDay();

        private void OnServeExited() {
            _garnish?.CloseServe();
            _cameras?.ShowBar();
        }

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
