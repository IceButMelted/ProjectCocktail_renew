using UnityEngine;

namespace Bar410.GameFlow
{
    // ── Level 2 · Step 1 seam ──────────────────────────────

    /// <summary>
    /// The only place TalkingWithCustomerState touches scene objects — companion to
    /// CocktailFlowBridge/GarnishFlowBridge/ServeFlowBridge. Locks ingredient buttons,
    /// pins the camera, and closes the recipe book whenever a fresh conversation starts
    /// (covers both the very first customer and every Serve → TalkingWithCustomer loop).
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

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();
            _gameLoop.EnsureBuilt();

            _gameLoop.OpenBar.TalkingWithCustomer.Entered += OnEntered;
        }

        private void OnDestroy()
        {
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
