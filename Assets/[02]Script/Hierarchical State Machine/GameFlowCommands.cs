using UnityEngine;
using Yarn.Unity;

namespace Bar410.GameFlow
{
    // ── Flow Control Surface ───────────────────────────────

    /// <summary>
    /// The single place where the outside world drives the state machines: .yarn scripts,
    /// UnityEvents on buttons/interactables, and animation events.
    ///
    /// Every method here is a public instance method, so it binds in the Inspector from any
    /// UnityEvent / UnityAction dropdown. Each one also has a static [YarnCommand] wrapper so
    /// .yarn scripts can call it with no target object name:
    ///
    ///     &lt;&lt;flow_prepare_drinks&gt;&gt;
    ///     &lt;&lt;flow_close_bar&gt;&gt;
    ///
    /// Nothing outside this file should touch StateMachine.TryTransition directly — the
    /// phases own their own rules, and this class only forwards intent.
    /// </summary>
    [RequireComponent(typeof(GameLoopFSM))]
    public class GameFlowCommands : MonoBehaviour
    {
        // ── Instance Access ────────────────────────────────

        /// <summary>Set in Awake so the static Yarn commands can reach the live flow.</summary>
        public static GameFlowCommands Instance { get; private set; }

        [SerializeField] private GameLoopFSM _gameLoop;

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();
            _gameLoop.EnsureBuilt();

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[GameFlowCommands] A second instance on '{name}' is being ignored; Yarn commands stay bound to '{Instance.name}'.", this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Level 1 · Prepare / Close ──────────────────────

        /// <summary>Bar setup done — open the bar. Prepare → Open.</summary>
        public void OpenBar() => _gameLoop.PrepareBar.RequestOpenBar();

        /// <summary>End-of-day actions done — start the next day. Close → Prepare.</summary>
        public void NextDay() => _gameLoop.ClosingBar.RequestNextDay();

        // ── Level 2 · Step 1 · Talking ─────────────────────

        /// <summary>The customer ordered something. Step 1 → 2.</summary>
        public void PrepareDrinks() => _gameLoop.OpenBar.TalkingWithCustomer.RequestPrepareDrinks();

        /// <summary>Done serving for the day. Step 1 → exit Open Bar → Close.</summary>
        public void CloseBar() => _gameLoop.OpenBar.TalkingWithCustomer.RequestCloseBar();

        // ── Level 2 · Steps 3–4 · Garnish / Serve ──────────

        /// <summary>Garnish confirmed. Step 3 → 4.</summary>
        public void GarnishDone() => _gameLoop.OpenBar.Garnish.Finish();

        /// <summary>Drink handed over. Step 4 → 1.</summary>
        public void ServeDone() => _gameLoop.OpenBar.Serve.Finish();

        /// <summary>
        /// Backtrack to Prepare Drinks from wherever we are. Valid from step 3 or 4;
        /// the drink restarts at 2.1. Warns (never silently no-ops) from anywhere else.
        /// </summary>
        public void RemakeDrink()
        {
            var openBar = _gameLoop.OpenBar;

            switch (openBar.Sub.Current)
            {
                case OpenSub.Garnish:
                    openBar.Garnish.RequestRemake();
                    break;
                case OpenSub.Serve:
                    openBar.Serve.RequestRemake();
                    break;
                default:
                    Debug.LogWarning($"[GameFlowCommands] RemakeDrink is only valid from Garnish or Serve, not {openBar.Sub.Current}.", this);
                    break;
            }
        }

        // ── Level 3 · Steps 2.1–2.2 · Drink Building ───────

        /// <summary>Ingredient chosen. Step 2.1 → 2.2.</summary>
        public void IngredientAdded() => _gameLoop.OpenBar.PrepareDrinks.AddIngredient.Finish();

        /// <summary>Minigame done, drink needs more. Step 2.2 → 2.1.</summary>
        public void AnotherIngredient() => _gameLoop.OpenBar.PrepareDrinks.Minigame.RequestAnotherIngredient();

        /// <summary>Drink finished. Exits Prepare Drinks → step 3 Garnish.</summary>
        public void DrinkComplete() => _gameLoop.OpenBar.PrepareDrinks.Minigame.CompleteDrink();

        // ── Yarn Commands ──────────────────────────────────
        // Static so .yarn can call them without naming a target GameObject.

        [YarnCommand("flow_open_bar")]
        public static void Yarn_OpenBar() => Resolve()?.OpenBar();

        [YarnCommand("flow_next_day")]
        public static void Yarn_NextDay() => Resolve()?.NextDay();

        [YarnCommand("flow_prepare_drinks")]
        public static void Yarn_PrepareDrinks() => Resolve()?.PrepareDrinks();

        [YarnCommand("flow_close_bar")]
        public static void Yarn_CloseBar() => Resolve()?.CloseBar();

        [YarnCommand("flow_garnish_done")]
        public static void Yarn_GarnishDone() => Resolve()?.GarnishDone();

        [YarnCommand("flow_serve_done")]
        public static void Yarn_ServeDone() => Resolve()?.ServeDone();

        [YarnCommand("flow_remake_drink")]
        public static void Yarn_RemakeDrink() => Resolve()?.RemakeDrink();

        [YarnCommand("flow_ingredient_added")]
        public static void Yarn_IngredientAdded() => Resolve()?.IngredientAdded();

        [YarnCommand("flow_another_ingredient")]
        public static void Yarn_AnotherIngredient() => Resolve()?.AnotherIngredient();

        [YarnCommand("flow_drink_complete")]
        public static void Yarn_DrinkComplete() => Resolve()?.DrinkComplete();

        // ── Yarn Functions ─────────────────────────────────
        // For branching in dialogue: <<if flow_phase() == "Open">>

        [YarnFunction("flow_phase")]
        public static string Yarn_CurrentPhase()
        {
            var flow = Resolve();
            return flow == null ? string.Empty : flow._gameLoop.Machine.Current.ToString();
        }

        [YarnFunction("flow_step")]
        public static string Yarn_CurrentStep()
        {
            var flow = Resolve();
            if (flow == null) return string.Empty;

            var loop = flow._gameLoop;
            if (loop.Machine.Current != GamePhase.Open) return string.Empty;

            return loop.OpenBar.Sub.Current == OpenSub.PrepareDrinks
                ? loop.OpenBar.PrepareDrinks.Sub.Current.ToString()
                : loop.OpenBar.Sub.Current.ToString();
        }

        // ── Internal ───────────────────────────────────────

        private static GameFlowCommands Resolve()
        {
            if (Instance == null)
                Debug.LogError("[GameFlowCommands] No instance in the scene — flow commands from Yarn will be ignored.");

            return Instance;
        }
    }
}
