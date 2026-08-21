using UnityEngine;

namespace Bar410.GameFlow
{
    // ── Level 2 · Step 3 seam ──────────────────────────────

    /// <summary>
    /// The only place GarnishState touches scene objects — companion to CocktailFlowBridge and
    /// MinigameFlowBridge. GarnishState itself stays plain C# and knows nothing about any of this.
    ///
    /// Owns the "pour" interaction: dragging the mixing vessel (CocktailShaker, tagged with a
    /// PourSource marker) onto whatever glass the player has placed in the shared
    /// GlassPlacementZone. A successful pour locks the drink's visuals onto the glass and gates
    /// the existing GarnishDone() flow command — call <see cref="TryFinishGarnish"/> from the
    /// "done" button/UI instead of GameFlowCommands.GarnishDone() directly, so garnishing can't
    /// be finished before anything was actually poured.
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

        [Header("Cocktail Scene Objects")]
        [SerializeField] private ShakerContents _shakerContents;
        [SerializeField] private DragableObject _shakerDragable;
        [SerializeField] private GlassPlacementZone _glassZone;

        private bool _pourComplete;

        // ── Unity ──────────────────────────────────────────

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();
            if (_commands == null) _commands = GetComponent<GameFlowCommands>();
            _gameLoop.EnsureBuilt();

            var garnish = _gameLoop.OpenBar.Garnish;
            garnish.Entered += OnGarnishEntered;
            garnish.Exited += OnGarnishExited;

            if (_glassZone != null) _glassZone.Placed += OnZonePlaced;
        }

        private void OnDestroy()
        {
            if (_gameLoop != null && _gameLoop.OpenBar != null)
            {
                var garnish = _gameLoop.OpenBar.Garnish;
                garnish.Entered -= OnGarnishEntered;
                garnish.Exited -= OnGarnishExited;
            }

            if (_glassZone != null) _glassZone.Placed -= OnZonePlaced;
        }

        // ── Step 3 · Garnish ───────────────────────────────

        private void OnGarnishEntered()
        {
            _pourComplete = false;

            // The shaker is locked coming out of the minigame (MinigameFlowBridge) — unlock it
            // here so it can be dragged onto the placed glass to pour.
            if (_shakerDragable != null) InteractableToggle.Apply(_shakerDragable.gameObject, true);
        }

        private void OnGarnishExited()
        {
            if (_shakerDragable != null) InteractableToggle.Apply(_shakerDragable.gameObject, false);
        }

        // ── Pour ───────────────────────────────────────────

        private void OnZonePlaced(GameObject item)
        {
            if (!item.TryGetComponent<PourSource>(out _)) return;
            if (_shakerContents == null || _glassZone.Occupant == null) return;

            _glassZone.Occupant.ApplyDrink(_shakerContents.CurrentCocktail);

            // The drink now visually lives in the glass — move the shaker out of the way.
            if (_shakerDragable != null)
                _shakerDragable.transform.position = _shakerDragable.PastLocation;

            _pourComplete = true;
        }

        // ── Called by the "done garnishing" UI instead of GameFlowCommands.GarnishDone() ──

        public void TryFinishGarnish()
        {
            if (!_pourComplete)
            {
                Debug.LogWarning("[GarnishFlowBridge] Nothing has been poured yet — pour before finishing.", this);
                return;
            }

            if (_commands != null) _commands.GarnishDone();
        }
    }
}
