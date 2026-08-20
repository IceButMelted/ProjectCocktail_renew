using UnityEngine;
using UnityEngine.Events;

namespace Bar410.GameFlow
{
    // ── Level 1 · Prepare seam ─────────────────────────────

    /// <summary>
    /// Connects <see cref="PrepareBarPhase"/> to the cocktail scene objects.
    ///
    /// Same shape as MinigameFlowBridge (Bar410_Minigame_Integration_Plan §3.3): the phase
    /// stays a plain C# class that knows nothing about UnityEngine, and exactly one
    /// MonoBehaviour translates its lifetime into scene effects. Because
    /// <see cref="StateBase"/> already exposes Entered/Exited, nothing in the flow layer
    /// had to change to make this possible.
    ///
    /// What this phase is FOR (spec §3.1) is laying out the bar. The payoff is the roster
    /// handover in <see cref="OnPrepareExited"/>: the ingredient buttons stop being a
    /// hand-maintained Inspector list and become "whatever the player put on the bar today".
    /// </summary>
    [RequireComponent(typeof(GameLoopFSM))]
    public class BarSetupBridge : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private GameLoopFSM _gameLoop;

        [Header("Cocktail Scene Objects")]
        [Tooltip("Bottles and mixers the player pours from. Locked during setup.")]
        [SerializeField] private IngredientButtonGroup _ingredients;

        [Tooltip("Recipe book. Available during setup so the player can plan.")]
        [SerializeField] private IngredientButtonGroup _bookUI;

        [SerializeField] private ShakerPanelController _shakerPanels;
        [SerializeField] private ShakerContents _shakerContents;

        [Header("Placement")]
        [Tooltip("Raised on Enter — turn the drag/drop placement system ON.")]
        [SerializeField] private UnityEvent _onPlacementUnlocked = new UnityEvent();

        [Tooltip("Raised on Exit — turn placement OFF and persist the layout (GDD §23.2 phase1Layout).")]
        [SerializeField] private UnityEvent _onPlacementLocked = new UnityEvent();

        [Tooltip("Objects the player placed during setup. Handed to the ingredient group on Exit.")]
        [SerializeField] private GameObject[] _placedIngredients;

        // ── Unity ──────────────────────────────────────────

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();

            // Component Awake order on one GameObject is not guaranteed, so build on demand
            // rather than trusting GameLoopFSM.Awake to have run first.
            _gameLoop.EnsureBuilt();

            _gameLoop.PrepareBar.Entered += OnPrepareEntered;
            _gameLoop.PrepareBar.Exited += OnPrepareExited;
        }

        private void OnDestroy()
        {
            if (_gameLoop == null || _gameLoop.PrepareBar == null) return;

            _gameLoop.PrepareBar.Entered -= OnPrepareEntered;
            _gameLoop.PrepareBar.Exited -= OnPrepareExited;
        }

        // ── Phase reactions ────────────────────────────────

        private void OnPrepareEntered()
        {
            // Nothing is being served yet: pouring and the shaker panels stay shut.
            if (_ingredients != null) _ingredients.SetInteractable(false);
            if (_shakerPanels != null) _shakerPanels.LockAll();
            if (_shakerContents != null) _shakerContents.Clear();

            // The book is the one thing the player SHOULD be able to read while setting up.
            if (_bookUI != null) _bookUI.SetInteractable(true);

            _onPlacementUnlocked.Invoke();
        }

        private void OnPrepareExited()
        {
            _onPlacementLocked.Invoke();

            // The day's roster is whatever ended up on the bar.
            if (_ingredients != null && _placedIngredients != null && _placedIngredients.Length > 0)
                _ingredients.SetRoster(_placedIngredients);
        }

        // ── Setup-time API ─────────────────────────────────

        /// <summary>
        /// Records an object the player placed on the bar. Wire this to the placement
        /// system's "object dropped" event once that system exposes one.
        /// </summary>
        public void RegisterPlacedIngredient(GameObject placed)
        {
            if (placed == null || _ingredients == null) return;
            _ingredients.Add(placed);
        }
    }
}
