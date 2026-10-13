using UnityEngine;
using static E_Cocktail;

namespace Bar410.GameFlow
{
    // ── Level 3 · Step 2.2 seam ────────────────────────────

    /// <summary>
    /// The only place the flow layer and Assets/[02]Script/Minigame/ meet.
    ///
    /// <see cref="MinigameState"/> stays a plain C# class that raises events and holds data;
    /// this MonoBehaviour turns those events into calls on <see cref="MinigameSystemManager"/>
    /// and feeds the outcome back. Neither side learns the other's vocabulary — the HSM never
    /// sees <see cref="MinigamePhase"/>, and the minigames never see <see cref="PrepSub"/>.
    ///
    /// Companion to <see cref="CocktailFlowBridge"/>, which deliberately does not touch
    /// minigames: two owners for one panel is the bug this split exists to prevent.
    ///
    /// Lives on the same GameObject as <see cref="GameLoopFSM"/> (`[GameLoop]`).
    ///
    /// Design rule (confirmed 2026-08-21, closes Integration_Plan §6 Q2): a minigame cannot be
    /// lost. A run ends either <see cref="MinigameOutcome.Completed"/> or, if the player backs
    /// out, <see cref="MinigameOutcome.Cancelled"/> — so nothing here ever has to handle a
    /// "failed the minigame" branch, and Cancelled always means "the player left", never "lost".
    /// </summary>
    [RequireComponent(typeof(GameLoopFSM))]
    public class MinigameFlowBridge : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private GameLoopFSM _gameLoop;

        [Tooltip("Optional. Only needed when Auto Advance On Win is on.")]
        [SerializeField] private GameFlowCommands _commands;

        [Header("Minigame Scene Objects")]
        [SerializeField] private MinigameSystemManager _minigames;

        [Tooltip("Optional. Supplies the fallback type from the drink's preparation method, " +
                 "and is the object unlocked again when a run is cancelled.")]
        [SerializeField] private ShakerContents _shakerContents;

        [Header("Behaviour")]
        [Tooltip("On (design, 2026-08-21): winning the minigame finishes the drink — 2.2 exits " +
                 "straight to step 3 Garnish. Leaving by cancel goes back to 2.1 through the " +
                 "manager's back / give-up buttons. Turn off only once a scene button calls " +
                 "DrinkComplete, or winning leaves step 2.2 a dead end.")]
        [SerializeField] private bool _autoAdvanceOnWin = true;

        // ── Unity ──────────────────────────────────────────

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();
            if (_commands == null) _commands = GetComponent<GameFlowCommands>();

            var minigameState = _gameLoop.OpenBar.PrepareDrinks.Minigame;
            minigameState.OnStartRequested += OnStartRequested;
            minigameState.OnStopRequested += OnStopRequested;

            if (_minigames != null)
            {
                _minigames.MinigameFinished += OnMinigameFinished;
                _minigames.BackRequested += OnBackRequested;
            }
            else Debug.LogWarning("[MinigameFlowBridge] No MinigameSystemManager assigned — step 2.2 will run nothing.", this);
        }

        private void OnDestroy()
        {
            if (_gameLoop != null && _gameLoop.OpenBar != null)
            {
                var minigameState = _gameLoop.OpenBar.PrepareDrinks.Minigame;
                minigameState.OnStartRequested -= OnStartRequested;
                minigameState.OnStopRequested -= OnStopRequested;
            }

            if (_minigames != null)
            {
                _minigames.MinigameFinished -= OnMinigameFinished;
                _minigames.BackRequested -= OnBackRequested;
            }
        }

        // ── Minigame buttons → Flow ────────────────────────
        // Every way out of the minigame goes through the HSM. Cancelling the run directly left
        // the flow sitting in 2.2 with nothing running, and the next IngredientAdded() was an
        // "Illegal transition Minigame -> Minigame" (F6, docs/baseline/managers/f6_giveup.txt).

        private void OnBackRequested() => _commands?.AnotherIngredient();

        // ── Flow → Minigame ────────────────────────────────

        private void OnStartRequested(Enum_MiniGameType selected)
        {
            if (_minigames == null) return;

            var fromDrink = _shakerContents != null
                ? _shakerContents.RequiredMinigame
                : Enum_MiniGameType.None;

            _minigames.StartMinigame(_minigames.Resolve(selected, fromDrink));
        }

        private void OnStopRequested()
        {
            // Leaving 2.2 by any route — another ingredient, drink complete, or a backtrack
            // from Garnish/Serve. The outro still plays and reports Cancelled on its own.
            if (_minigames == null) return;
            if (_minigames.ActivePhase == MinigamePhase.Idle) return;

            _minigames.CancelMinigame();
        }

        // ── Minigame → Flow ────────────────────────────────

        private void OnMinigameFinished(Enum_MiniGameType type, MinigameOutcome outcome)
        {
            // Fires once per run, after the outro. A run cancelled *because* the flow left 2.2
            // still reports here a moment later; MinigameState clears LastOutcome on every
            // entry, so a stale report can never be mistaken for the next run's result.
            _gameLoop.OpenBar.PrepareDrinks.Minigame.ReportResult(outcome);

            // Cancelling means the player backed out, so the shaker has to become grabbable
            // again. It is a member of IngredientButtonGroup, which locks it when the drink
            // fills; re-entering 2.1 normally unlocks it, but the direct cancel buttons stay
            // in 2.2 and never get there.
            // A Completed run is left locked: the drink is finished and the glass takes over.
            if (outcome == MinigameOutcome.Cancelled && _shakerContents != null)
                InteractableToggle.Apply(_shakerContents.gameObject, true);

            if (!_autoAdvanceOnWin || outcome != MinigameOutcome.Completed) return;
            if (!InMinigameStep()) return;

            if (_commands == null)
            {
                Debug.LogWarning("[MinigameFlowBridge] Auto Advance On Win needs a GameFlowCommands — nothing advanced.", this);
                return;
            }

            _commands.DrinkComplete();
        }

        /// <summary>True only while the flow is actually sitting in step 2.2.</summary>
        private bool InMinigameStep()
        {
            if (_gameLoop.Machine.Current != GamePhase.Open) return false;
            if (_gameLoop.OpenBar.Sub.Current != OpenSub.PrepareDrinks) return false;

            return _gameLoop.OpenBar.PrepareDrinks.Sub.Current == PrepSub.Minigame;
        }
    }
}
