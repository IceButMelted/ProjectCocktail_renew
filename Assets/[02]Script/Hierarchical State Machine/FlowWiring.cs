using UnityEngine;
using static E_Cocktail;

namespace Bar410.GameFlow
{
    /// <summary>
    /// The one place the game flow (<see cref="GameLoopFSM"/>) and the managers meet. The states stay
    /// plain C# and know nothing about Unity objects; the managers know neither the flow nor each
    /// other. This component subscribes to a step's events, calls verbs on the managers, and turns
    /// manager events back into flow commands. It decides nothing else.
    ///
    /// Rules the flow applies, by step:
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
        [SerializeField] private CameraManager _cameras;

        [Header("Cocktail scene objects")]
        [SerializeField] private ShakerContents _shakerContents;
        [SerializeField] private IngredientButtonGroup _ingredients;

        [Header("Chrome")]
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

            prepareDrinks.Minigame.OnStartRequested -= OnMinigameStartRequested;
            prepareDrinks.Minigame.OnStopRequested -= OnMinigameStopRequested;
            openBar.Garnish.Entered -= OnGarnishEntered;
            openBar.Garnish.Exited -= OnGarnishExited;
            prepareDrinks.Entered -= OnTableReset;
            openBar.Serve.Exited -= OnTableReset;
            openBar.Serve.Entered -= OnServeEntered;
            openBar.Serve.Exited -= OnServeExited;
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
        }

        private void OnGarnishExited() => _garnish?.Close();

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
        // Scoring is not done here: leaving Serve for the conversation scores the drink, so every
        // way of finishing the step (the Serve button, <<flow_serve_done>>, a debug hotkey) commits
        // the same result.

        private void OnServeEntered() => _garnish?.OpenServe();

        private void OnServeExited()
        {
            _garnish?.CloseServe();
            _postIt?.Out();
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
