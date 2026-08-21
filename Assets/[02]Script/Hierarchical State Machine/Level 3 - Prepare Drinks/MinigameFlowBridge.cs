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
                 "and is the object locked while a minigame is on screen.")]
        [SerializeField] private ShakerContents _shakerContents;

        [Header("Behaviour")]
        [Tooltip("Used when nothing was selected and the shaker has no method yet.")]
        [SerializeField] private Enum_MiniGameType _defaultType = Enum_MiniGameType.Shaking;

        [Tooltip("On (design, 2026-08-21): winning the minigame finishes the drink — 2.2 exits " +
                 "straight to step 3 Garnish. This is the only thing that leaves 2.2 today; no " +
                 "scene button calls AnotherIngredient or DrinkComplete. Turn off only once " +
                 "such a button exists, or step 2.2 becomes a dead end.")]
        [SerializeField] private bool _autoAdvanceOnWin = true;

        // ── Unity ──────────────────────────────────────────

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();
            if (_commands == null) _commands = GetComponent<GameFlowCommands>();
            _gameLoop.EnsureBuilt();

            var minigameState = _gameLoop.OpenBar.PrepareDrinks.Minigame;
            minigameState.OnStartRequested += OnStartRequested;
            minigameState.OnStopRequested += OnStopRequested;

            if (_minigames != null) _minigames.MinigameFinished += OnMinigameFinished;
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

            if (_minigames != null) _minigames.MinigameFinished -= OnMinigameFinished;
        }

        // ── Flow → Minigame ────────────────────────────────

        private void OnStartRequested(Enum_MiniGameType selected)
        {
            if (_minigames == null) return;

            var type = Resolve(selected);
            if (type == Enum_MiniGameType.None)
            {
                Debug.LogWarning("[MinigameFlowBridge] Entered step 2.2 with no minigame to run — " +
                                 "select one with <<flow_minigame ...>> or give the drink a preparation method.", this);
                return;
            }

            _minigames.StartMinigame(type);
        }

        private void OnStopRequested()
        {
            // Leaving 2.2 by any route — another ingredient, drink complete, or a backtrack
            // from Garnish/Serve. The outro still plays and reports Cancelled on its own.
            if (_minigames == null) return;
            if (_minigames.ActivePhase == MinigamePhase.Idle) return;

            _minigames.CancelMinigame();
        }

        /// <summary>
        /// Explicit selection wins; otherwise the drink's own preparation method decides;
        /// the Inspector default is the last resort.
        /// </summary>
        private Enum_MiniGameType Resolve(Enum_MiniGameType selected)
        {
            if (selected != Enum_MiniGameType.None) return selected;

            var fromShaker = _shakerContents != null
                ? _shakerContents.RequiredMinigame
                : Enum_MiniGameType.None;

            return fromShaker != Enum_MiniGameType.None ? fromShaker : _defaultType;
        }

        // ── Minigame → Flow ────────────────────────────────

        private void OnMinigameFinished(Enum_MiniGameType type, MinigameOutcome outcome)
        {
            // Fires once per run, after the outro. A run cancelled *because* the flow left 2.2
            // still reports here a moment later; MinigameState clears LastOutcome on every
            // entry, so a stale report can never be mistaken for the next run's result.
            _gameLoop.OpenBar.PrepareDrinks.Minigame.ReportResult(outcome);

            // Cancelling means the player backed out, so the shaker has to become grabbable
            // again — the scene locks it when the panel opens (OnStartedMinigame) and only
            // the Serve/Reset buttons unlock it, which a cancelled run never reaches.
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
