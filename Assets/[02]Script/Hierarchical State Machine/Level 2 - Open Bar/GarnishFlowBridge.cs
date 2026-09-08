using UnityEngine;
using UnityEngine.UI;

namespace Bar410.GameFlow
{
    // ── Level 2 · Step 3 seam ──────────────────────────────

    /// <summary>
    /// The only place GarnishState touches scene objects — companion to CocktailFlowBridge and
    /// MinigameFlowBridge. GarnishState itself stays plain C# and knows nothing about any of this.
    ///
    /// Glass, ice, and pouring are all UI-button-driven here (Bar410 gameplay revision, see
    /// docs/adr/0002-glass-pour-fixed-position-buttons.md): the player picks a glass from a list
    /// (<see cref="ChooseGlass"/>), which spawns already placed in the shared
    /// GlassPlacementZone; the mixing vessel is fixed beside it and poured with a button
    /// (<see cref="Pour"/>) instead of dragged. A successful pour locks the drink's visuals onto
    /// the glass and gates the existing GarnishDone() flow command — call
    /// <see cref="TryFinishGarnish"/> from the "done" button/UI instead of
    /// GameFlowCommands.GarnishDone() directly, so garnishing can't be finished before anything
    /// was actually poured.
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
        [SerializeField] private CocktailSystemManager _cocktail;
        [SerializeField] private ShakerContents _shakerContents;
        [SerializeField] private GlassPlacementZone _glassZone;

        [Header("BTN Add Ice / Remove")]
        [SerializeField] private GameObject _gb_btnAddIce;
        [SerializeField] private GameObject _gb_btnRemoveIce;
        private Button _btnAddIce => _gb_btnAddIce?.GetComponent<Button>();
        private Button _btnRemoveIce => _gb_btnRemoveIce?.GetComponent<Button>();

        private bool _pourComplete;
        private bool _hasPoured;
        private PlacedGlassInstance _pouringGlass;

        /// <summary>True once a pour has fully finished filling — <see cref="TryFinishGarnish"/> only succeeds when this is true.</summary>
        public bool CanFinishGarnish => _pourComplete;

        // ── Unity ──────────────────────────────────────────

        private void Awake()
        {
            if (_gameLoop == null) _gameLoop = GetComponent<GameLoopFSM>();
            if (_commands == null) _commands = GetComponent<GameFlowCommands>();
            _gameLoop.EnsureBuilt();

            var garnish = _gameLoop.OpenBar.Garnish;
            garnish.Entered += OnGarnishEntered;
            garnish.Exited += OnGarnishExited;
        }

        private void OnDestroy()
        {
            UnsubscribeFromPouringGlass();

            if (_gameLoop == null || _gameLoop.OpenBar == null) return;

            var garnish = _gameLoop.OpenBar.Garnish;
            garnish.Entered -= OnGarnishEntered;
            garnish.Exited -= OnGarnishExited;
        }

        // ── Step 3 · Garnish ───────────────────────────────

        private void OnGarnishEntered()
        {
            _pourComplete = false;
            _hasPoured = false;

            // The drink's identity (name/colour/etc.) is resolved against the recipe database
            // once mixing is done, not on every ingredient add — Garnish entry is that "done
            // mixing" moment. Without this, S_Drink.waterColorTop/Bottom stay at their unset
            // (fully transparent) default and the poured glass looks empty even with liquid in it.
            _cocktail?.UpdateCocktailInShaker();
        }

        private void OnGarnishExited()
        {

            //Reset the ice BTN to defualt state for next time the garnish state is entered
            _gb_btnAddIce.SetActive(true);
            _gb_btnRemoveIce.SetActive(false);
            _btnAddIce.interactable = true;
            _btnRemoveIce.interactable = true;

        }

        // ── Glass ──────────────────────────────────────────

        /// <summary>Called by a glass-option button in the Garnish UI (one button per SO_GlassOption).</summary>
        public void ChooseGlass(SO_GlassOption option)
        {
            if (_glassZone == null) return;

            UnsubscribeFromPouringGlass(); // the old glass is about to be destroyed — drop its fill subscription with it

            _glassZone.SetGlass(option);
            _pourComplete = false; // a freshly-placed glass has nothing poured into it yet
            _hasPoured = false;
        }

        // ── Ice ────────────────────────────────────────────

        /// <summary>
        /// Called by the Garnish "add ice"/"remove ice" buttons. Sets the scored recipe flag and
        /// the glass visual together. Once <see cref="Pour"/> has been pressed, ice can only be
        /// added, never removed — pulling ice back out of an already-poured drink doesn't make
        /// sense visually, adding more does.
        /// </summary>
        public void ToggleIce(bool enable)
        {
            if (!enable && _hasPoured)
            {
                Debug.LogWarning("[GarnishFlowBridge] Already poured — ice can no longer be removed.", this);
                return;
            }

            if (_shakerContents != null) _shakerContents.SetIce(enable);
            _glassZone?.Occupant?.ApplyIce(enable);
        }

        // ── Pour ───────────────────────────────────────────

        /// <summary>
        /// Called by the Garnish "pour" button. Plays the glass's water-rising fill animation;
        /// <see cref="TryFinishGarnish"/> only succeeds once that animation has finished.
        /// </summary>
        public void Pour()
        {
            if (_glassZone == null || _glassZone.Occupant == null)
            {
                Debug.LogWarning("[GarnishFlowBridge] No glass placed yet — choose a glass before pouring.", this);
                return;
            }

            if (_shakerContents == null || _shakerContents.IsEmpty)
            {
                Debug.LogWarning("[GarnishFlowBridge] Nothing in the shaker to pour.", this);
                return;
            }

            UnsubscribeFromPouringGlass(); // re-pouring into the same glass shouldn't double-subscribe

            _hasPoured = true;

            _btnAddIce.interactable = true;
            _btnRemoveIce.interactable = false;

            var glass = _glassZone.Occupant;
            _pouringGlass = glass;
            glass.OnFillComplete += OnPourFillComplete;

            glass.ApplyDrink(_shakerContents.CurrentCocktail);
            glass.StartFill();
        }

        private void OnPourFillComplete()
        {
            UnsubscribeFromPouringGlass();
            _pourComplete = true;
        }

        private void UnsubscribeFromPouringGlass()
        {
            if (_pouringGlass == null) return;

            _pouringGlass.OnFillComplete -= OnPourFillComplete;
            _pouringGlass = null;
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
