using UnityEngine;
using UnityEngine.UI;

namespace Bar410.GameFlow
{
    // ── Level 2 · Step 4 seam ──────────────────────────────

    /// <summary>
    /// The only place ServeState touches scene objects — companion to CocktailFlowBridge/
    /// GarnishFlowBridge/TalkingWithCustomerBridge. Owns the Serve panel and its two
    /// buttons: BTN_Serving (hand the drink over) and the remake button (backtrack to
    /// Prepare Drinks with the drink unfinished).
    /// </summary>
    [RequireComponent(typeof(GameLoopFSM))]
    public class ServeFlowBridge : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private GameLoopFSM _gameLoop;
        [SerializeField] private GameFlowCommands _commands;

        [Header("Cocktail Scene Objects")]
        [SerializeField] private CocktailSystemManager _cocktail;
        [SerializeField] private Post_It_Order _postIt;

        [Header("UI")]
        [SerializeField] private GameObject _panelServe;
        [SerializeField] private Button _btnServe;
        [SerializeField] private Button _btnRemake;

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();
            if (_commands == null) _commands = GetComponent<GameFlowCommands>();
            _gameLoop.EnsureBuilt();

            var serve = _gameLoop.OpenBar.Serve;
            serve.Entered += OnEntered;
            serve.Exited += OnExited;

            if (_btnServe != null) _btnServe.onClick.AddListener(OnServeClicked);
            if (_btnRemake != null) _btnRemake.onClick.AddListener(OnRemakeClicked);
        }

        private void OnDestroy()
        {
            if (_btnServe != null) _btnServe.onClick.RemoveListener(OnServeClicked);
            if (_btnRemake != null) _btnRemake.onClick.RemoveListener(OnRemakeClicked);

            if (_gameLoop == null || _gameLoop.OpenBar == null) return;

            var serve = _gameLoop.OpenBar.Serve;
            serve.Entered -= OnEntered;
            serve.Exited -= OnExited;
        }

        private void OnEntered() => _panelServe?.SetActive(true);

        private void OnExited()
        {
            _panelServe?.SetActive(false);
            _postIt?.Out();
        }

        private void OnServeClicked()
        {
            _cocktail?.ServeDrink();
            _commands?.ServeDone();
        }

        private void OnRemakeClicked()
        {
            _cocktail?.ResetCocktail();
            _commands?.RemakeDrink();
        }
    }
}
