using UnityEngine;

namespace Bar410.GameFlow
{
    // ── Level 2 · Step 1 seam ──────────────────────────────

    /// <summary>
    /// The only place TalkingWithCustomerState touches scene objects — companion to
    /// CocktailFlowBridge/GarnishFlowBridge/ServeFlowBridge. Locks ingredient buttons,
    /// pins the camera, and closes the recipe book whenever a fresh conversation starts
    /// (covers both the very first customer and every Serve → TalkingWithCustomer loop).
    ///
    /// It is also where the cocktail and the customers meet: the customers say who likes which
    /// drink, and a scored order (or a new customer loop) is carried into the dialogue's
    /// variables. Neither manager knows the other.
    /// </summary>
    [RequireComponent(typeof(GameLoopFSM))]
    public class TalkingWithCustomerBridge : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private GameLoopFSM _gameLoop;

        [Header("Scene Objects")]
        [SerializeField] private IngredientButtonGroup _ingredients;
        [SerializeField] private CameraController _camera;
        [SerializeField] private BookUI_V2 _bookUI;

        [Header("Cocktail <-> customers")]
        [SerializeField] private CocktailSystemManager _cocktail;
        [SerializeField] private TalkToCustomerManager _customers;

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();

            _gameLoop.OpenBar.TalkingWithCustomer.Entered += OnEntered;

            if (_cocktail == null || _customers == null)
            {
                Debug.LogWarning("[TalkingWithCustomerBridge] Cocktail or customers missing: scored orders will not reach the dialogue.", this);
                return;
            }

            _cocktail.UsePreferences(_customers.Preferences);
            _cocktail.OrderScored += _customers.CommitOrderResult;
            _cocktail.OrderCleared += _customers.ResetVariables;
        }

        private void OnDestroy()
        {
            if (_cocktail != null && _customers != null)
            {
                _cocktail.OrderScored -= _customers.CommitOrderResult;
                _cocktail.OrderCleared -= _customers.ResetVariables;
            }

            if (_gameLoop == null || _gameLoop.OpenBar == null) return;
            _gameLoop.OpenBar.TalkingWithCustomer.Entered -= OnEntered;
        }

        private void OnEntered()
        {
            _ingredients?.Disable();
            _camera?.SetFixedCamera(true);
            _bookUI?.CloseBook();
        }
    }
}
