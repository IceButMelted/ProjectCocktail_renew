using UnityEngine;

namespace Bar410.GameFlow
{
    // ── Inspector Wiring ───────────────────────────────────

    /// <summary>
    /// Exposes Enter / Update / Exit for every state in the hierarchy as Inspector-editable
    /// UnityEvents. Sits next to <see cref="GameLoopFSM"/> so that component stays lean.
    ///
    /// Binding happens in Awake, before GameLoopFSM enters its first phase in Start, so the
    /// very first OnEnter is not missed.
    /// </summary>
    [RequireComponent(typeof(GameLoopFSM))]
    public class GameFlowHooks : MonoBehaviour
    {
        [SerializeField] private GameLoopFSM _gameLoop;

        [Header("Level 1 · Game Loop")]
        [SerializeField] private StateHooks _prepareBar = new StateHooks();
        [SerializeField] private StateHooks _openBar = new StateHooks();
        [SerializeField] private StateHooks _closingBar = new StateHooks();

        [Header("Level 2 · Open Bar Steps")]
        [SerializeField] private StateHooks _talkingWithCustomer = new StateHooks();
        [SerializeField] private StateHooks _prepareDrinks = new StateHooks();
        [SerializeField] private StateHooks _garnish = new StateHooks();
        [SerializeField] private StateHooks _serve = new StateHooks();

        [Header("Level 3 · Prepare Drinks Steps")]
        [SerializeField] private StateHooks _addIngredient = new StateHooks();
        [SerializeField] private StateHooks _minigame = new StateHooks();

        // ── Public Access ──────────────────────────────────

        public StateHooks PrepareBar => _prepareBar;
        public StateHooks OpenBar => _openBar;
        public StateHooks ClosingBar => _closingBar;
        public StateHooks TalkingWithCustomer => _talkingWithCustomer;
        public StateHooks PrepareDrinks => _prepareDrinks;
        public StateHooks Garnish => _garnish;
        public StateHooks Serve => _serve;
        public StateHooks AddIngredient => _addIngredient;
        public StateHooks Minigame => _minigame;

        // ── Unity ──────────────────────────────────────────

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();

            // The phase objects must exist before we can subscribe to them. Component Awake
            // order on a single GameObject is not guaranteed, so build on demand instead of
            // trusting GameLoopFSM.Awake to have run first.
            _gameLoop.EnsureBuilt();

            var openBar = _gameLoop.OpenBar;
            var prepareDrinks = openBar.PrepareDrinks;

            _prepareBar.Bind(_gameLoop.PrepareBar);
            _openBar.Bind(openBar);
            _closingBar.Bind(_gameLoop.ClosingBar);

            _talkingWithCustomer.Bind(openBar.TalkingWithCustomer);
            _prepareDrinks.Bind(prepareDrinks);
            _garnish.Bind(openBar.Garnish);
            _serve.Bind(openBar.Serve);

            _addIngredient.Bind(prepareDrinks.AddIngredient);
            _minigame.Bind(prepareDrinks.Minigame);
        }

        private void OnDestroy()
        {
            if (_gameLoop == null) return;

            var openBar = _gameLoop.OpenBar;
            if (openBar == null) return;

            var prepareDrinks = openBar.PrepareDrinks;

            _prepareBar.Unbind(_gameLoop.PrepareBar);
            _openBar.Unbind(openBar);
            _closingBar.Unbind(_gameLoop.ClosingBar);

            _talkingWithCustomer.Unbind(openBar.TalkingWithCustomer);
            _prepareDrinks.Unbind(prepareDrinks);
            _garnish.Unbind(openBar.Garnish);
            _serve.Unbind(openBar.Serve);

            _addIngredient.Unbind(prepareDrinks.AddIngredient);
            _minigame.Unbind(prepareDrinks.Minigame);
        }
    }
}
