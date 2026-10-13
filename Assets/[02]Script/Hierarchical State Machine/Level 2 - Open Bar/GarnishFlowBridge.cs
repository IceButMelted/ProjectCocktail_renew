using UnityEngine;

namespace Bar410.GameFlow
{
    // ── Level 2 · Step 3 seam ──────────────────────────────

    /// <summary>
    /// Connects Garnish step 3 to the garnish station and to the rest of the game — companion
    /// to CocktailFlowBridge and MinigameFlowBridge. GarnishState itself stays plain C# and
    /// knows nothing about any of this.
    ///
    /// The station itself (glass, ice, decoration, pour, its canvas) is
    /// <see cref="GarnishManager"/>; it reports what happened and this bridge decides what the
    /// flow, the cocktail and the camera do about it:
    ///   step entered      → resolve the drink, frame the plate, lock the ingredients, open the panel
    ///   table reset       → take the glass off the plate (a new drink starts, or the old one was served)
    ///   pour requested    → hand the drink in
    ///   ice toggled       → record it on the drink (the cocktail owns the flag)
    ///   finish requested  → GarnishDone()
    ///   reset requested   → throw the drink away and remake it
    ///
    /// Glass, ice, and pouring are all UI-button-driven (Bar410 gameplay revision, see
    /// docs/adr/0002-glass-pour-fixed-position-buttons.md).
    ///
    /// TODO(design, plan Glass-freedom): the decoration step itself (what happens between a
    /// successful pour and pressing done) is undecided — for now the flow is playable end-to-end
    /// with no decoration mechanic; add one here once there is something to add.
    /// </summary>
    [RequireComponent(typeof(GameLoopFSM))]
    public class GarnishFlowBridge : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private GameLoopFSM _gameLoop;
        [SerializeField] private GameFlowCommands _commands;

        [Header("Garnish")]
        [SerializeField] private GarnishManager _garnish;

        [Header("Cocktail Scene Objects")]
        [SerializeField] private CocktailSystemManager _cocktail;
        [SerializeField] private ShakerContents _shakerContents;
        [SerializeField] private IngredientButtonGroup _ingredients;

        [Header("Camera")]
        [SerializeField] private CameraManager _cameras;

        // ── Unity ──────────────────────────────────────────

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();
            if (_commands == null) _commands = GetComponent<GameFlowCommands>();

            var garnishStep = _gameLoop.OpenBar.Garnish;
            garnishStep.Entered += OnGarnishEntered;
            garnishStep.Exited += OnGarnishExited;

            // Every customer starts with an empty table: a glass placed for the previous order (or
            // a backtrack from Garnish/Serve) does not carry over, and neither does the served one.
            // Both run on every way in, remake included.
            _gameLoop.OpenBar.PrepareDrinks.Entered += OnTableReset;
            _gameLoop.OpenBar.Serve.Exited += OnTableReset;

            if (_garnish != null)
            {
                _garnish.PourRequested += OnPourRequested;
                _garnish.IceToggled += OnIceToggled;
                _garnish.FinishRequested += OnFinishRequested;
                _garnish.ResetRequested += OnResetRequested;
            }
            else Debug.LogWarning("[GarnishFlowBridge] No GarnishManager assigned — step 3 will show nothing.", this);
        }

        private void OnDestroy()
        {
            if (_garnish != null)
            {
                _garnish.PourRequested -= OnPourRequested;
                _garnish.IceToggled -= OnIceToggled;
                _garnish.FinishRequested -= OnFinishRequested;
                _garnish.ResetRequested -= OnResetRequested;
            }

            if (_gameLoop == null || _gameLoop.OpenBar == null) return;

            var garnishStep = _gameLoop.OpenBar.Garnish;
            garnishStep.Entered -= OnGarnishEntered;
            garnishStep.Exited -= OnGarnishExited;
            _gameLoop.OpenBar.PrepareDrinks.Entered -= OnTableReset;
            _gameLoop.OpenBar.Serve.Exited -= OnTableReset;
        }

        // ── Step 3 · Garnish ───────────────────────────────

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

        // ── Garnish → game ─────────────────────────────────

        private void OnPourRequested()
        {
            if (_shakerContents == null || _shakerContents.IsEmpty)
            {
                Debug.LogWarning("[GarnishFlowBridge] Nothing in the shaker to pour.", this);
                return;
            }

            _garnish.Pour(_shakerContents.CurrentCocktail);
        }

        private void OnIceToggled(bool enable) => _shakerContents?.SetIce(enable);

        private void OnFinishRequested() => _commands?.GarnishDone();

        private void OnResetRequested()
        {
            _cocktail?.ResetCocktail();
            _commands?.RemakeDrink();
        }
    }
}
