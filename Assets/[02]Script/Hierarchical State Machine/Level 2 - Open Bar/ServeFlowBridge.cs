using UnityEngine;

namespace Bar410.GameFlow
{
    // ── Level 2 · Step 4 seam ──────────────────────────────

    /// <summary>
    /// The only place ServeState touches scene objects — companion to CocktailFlowBridge/
    /// GarnishFlowBridge/TalkingWithCustomerBridge. The Serve panel and its two buttons belong to
    /// <see cref="GarnishManager"/>; this bridge opens and closes the panel with step 4 and turns
    /// the two button events into flow commands: serve (hand the drink over) and remake
    /// (backtrack to Prepare Drinks with the drink unfinished).
    /// </summary>
    [RequireComponent(typeof(GameLoopFSM))]
    public class ServeFlowBridge : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private GameLoopFSM _gameLoop;
        [SerializeField] private GameFlowCommands _commands;

        [Header("Scene Objects")]
        [SerializeField] private GarnishManager _garnish;
        [SerializeField] private CocktailSystemManager _cocktail;
        [SerializeField] private Post_It_Order _postIt;

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();
            if (_commands == null) _commands = GetComponent<GameFlowCommands>();

            var serve = _gameLoop.OpenBar.Serve;
            serve.Entered += OnEntered;
            serve.Exited += OnExited;

            if (_garnish != null)
            {
                _garnish.ServeRequested += OnServeRequested;
                _garnish.RemakeRequested += OnRemakeRequested;
            }
        }

        private void OnDestroy()
        {
            if (_garnish != null)
            {
                _garnish.ServeRequested -= OnServeRequested;
                _garnish.RemakeRequested -= OnRemakeRequested;
            }

            if (_gameLoop == null || _gameLoop.OpenBar == null) return;

            var serve = _gameLoop.OpenBar.Serve;
            serve.Entered -= OnEntered;
            serve.Exited -= OnExited;
        }

        private void OnEntered() => _garnish?.OpenServe();

        private void OnExited()
        {
            _garnish?.CloseServe();
            _postIt?.Out();
        }

        // Scoring is not done here: leaving Serve for the conversation scores the drink
        // (CocktailFlowBridge.OnOpenBarTransition), so every way of finishing the step commits
        // the same result.
        private void OnServeRequested() => _commands?.ServeDone();

        private void OnRemakeRequested()
        {
            _cocktail?.ResetCocktail();
            _commands?.RemakeDrink();
        }
    }
}
