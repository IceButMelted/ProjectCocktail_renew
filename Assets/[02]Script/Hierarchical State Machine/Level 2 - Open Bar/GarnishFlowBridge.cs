using System;
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
        [SerializeField] private IngredientButtonGroup _ingredients;

        [Header("Camera / Panel")]
        [SerializeField] private CinimachineCameraSwitcher _cameraSwitcher;
        [SerializeField] private string _garnishCameraId = "GranishCam";
        [SerializeField] private GameObject _panelGarnishUI;
        [SerializeField] private GameObject _panelGlassSelectionUI;
        [SerializeField] private GameObject _panelGarnishItemUI;
        [SerializeField] private GameObject _panelGarnishRimUI;

        [Header("Glass Choice")]
        [Tooltip("Paged button grid over the glass catalog — calls ChooseGlass itself. Locked here after a pour.")]
        [SerializeField] private GlassChoiceGrid _glassGrid;

        [Header("BTN Pour / Finish / Reset")]
        [SerializeField] private Button _btnPour;
        [SerializeField] private Button _btnFinishGarnish;
        [SerializeField] private Button _btnResetGarnish;

        [SerializeField] private Button _btnGarnishItem;
        [SerializeField] private Button _btnGarnishRim;
        [SerializeField] private Button _btnGlassSelection;

        [Header("BTN Add Ice / Remove")]
        [SerializeField] private GameObject _gb_btnAddIce;
        [SerializeField] private GameObject _gb_btnRemoveIce;

        [Header("SFX")]
        [SerializeField] private string _sfxAddIce = "Add_Ice";
        [SerializeField] private string _sfxRemoveIce = "Remove_Ice";

        private Button _btnAddIce => _gb_btnAddIce?.GetComponent<Button>();
        private Button _btnRemoveIce => _gb_btnRemoveIce?.GetComponent<Button>();


        private bool _pourComplete;
        private bool _hasPoured;
        private PlacedGlassInstance _pouringGlass;

        // Which of the placed glass's 2 garnish slots the next Garnish Item click fills —
        // set by clicking a slot on the glass itself (GarnishSlotButton).
        private int _activeGarnishSlot;
        private PlacedGlassInstance _garnishSlotGlass;

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

            if (_btnPour != null) _btnPour.onClick.AddListener(Pour);
            if (_btnFinishGarnish != null) _btnFinishGarnish.onClick.AddListener(TryFinishGarnish);
            if (_btnResetGarnish != null) _btnResetGarnish.onClick.AddListener(OnResetGarnishClicked);
            if (_btnGarnishItem != null) _btnGarnishItem.onClick.AddListener(ChangedToGarnishItem);
            if (_btnGarnishRim != null) _btnGarnishRim.onClick.AddListener(ChangedToGarnishRim);
            if (_btnGlassSelection != null) _btnGlassSelection.onClick.AddListener(ChangedToChooseGlass);

            if (_btnAddIce != null) _btnAddIce.onClick.AddListener(() => ToggleIce(true));
            if (_btnRemoveIce != null) _btnRemoveIce.onClick.AddListener(() => ToggleIce(false));
        }

        private void OnDestroy()
        {
            UnsubscribeFromPouringGlass();
            UnsubscribeFromGarnishSlots();

            if (_btnPour != null) _btnPour.onClick.RemoveListener(Pour);
            if (_btnFinishGarnish != null) _btnFinishGarnish.onClick.RemoveListener(TryFinishGarnish);
            if (_btnResetGarnish != null) _btnResetGarnish.onClick.RemoveListener(OnResetGarnishClicked);

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
            _activeGarnishSlot = 0;
            _glassGrid?.SetInteractable(true); // re-enable glass choice for the new garnish step
            ChangedToChooseGlass(); // Set default UI panel to choose glass when entering garnish state


            // The drink's identity (name/colour/etc.) is resolved against the recipe database
            // once mixing is done, not on every ingredient add — Garnish entry is that "done
            // mixing" moment. Without this, S_Drink.waterColorTop/Bottom stay at their unset
            // (fully transparent) default and the poured glass looks empty even with liquid in it.
            _cocktail?.UpdateCocktailInShaker();

            _cameraSwitcher?.SwitchCamera(_garnishCameraId);
            _ingredients?.Disable();
            _panelGarnishUI?.SetActive(true);
            if (_btnPour != null) _btnPour.interactable = true;
        }

        private void OnGarnishExited()
        {
            _panelGarnishUI?.SetActive(false);

            //Reset the ice BTN to defualt state for next time the garnish state is entered
            _gb_btnAddIce.SetActive(true);
            _gb_btnRemoveIce.SetActive(false);
            _btnAddIce.interactable = true;
            _btnRemoveIce.interactable = true;

        }

        private void OnResetGarnishClicked()
        {
            _cocktail?.ResetCocktail();
            _commands?.RemakeDrink();
        }

        // ── Glass ──────────────────────────────────────────

        /// <summary>Called by a glass-option button in the Garnish UI (one button per SO_GlassOption).</summary>
        public void ChooseGlass(SO_GlassOption option)
        {
            if (_glassZone == null) return;
            if (_hasPoured == true) return;

            UnsubscribeFromPouringGlass(); // the old glass is about to be destroyed — drop its fill subscription with it
            UnsubscribeFromGarnishSlots();

            _glassZone.SetGlass(option);
            _pourComplete = false; // a freshly-placed glass has nothing poured into it yet
            _hasPoured = false;
            _activeGarnishSlot = 0;

            SubscribeToGarnishSlots();
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

            // Icon swap lives here (not a second Inspector listener) so the click always does
            // both atomically — a lone listener toggling only one half was the exact shape of
            // a past desync bug (see GarnishFlowBridge history).
            _gb_btnAddIce?.SetActive(!enable);
            _gb_btnRemoveIce?.SetActive(enable);

            TryPlay(enable ? _sfxAddIce : _sfxRemoveIce);
        }

        // ── Garnish Decoration ──────────────────────────────

        /// <summary>
        /// Called by a Fresh/Novelty garnish button in the Garnish UI. Fills whichever of the
        /// glass's 2 shared slots was last clicked directly on the glass (GarnishSlotButton) —
        /// Fresh and Novelty share both slots, there's no per-category restriction. Pass null
        /// to clear the active slot.
        /// </summary>
        public void ChooseGarnishItem(SO_GarnishItemOption item)
            => _glassZone?.Occupant?.ApplyGarnishItem(_activeGarnishSlot, item);

        /// <summary>Explicit-slot overload — same as above but bypasses the "last clicked" slot.</summary>
        public void ChooseGarnishItem(int slotIndex, SO_GarnishItemOption item)
            => _glassZone?.Occupant?.ApplyGarnishItem(slotIndex, item);

        /// <summary>Called by a Rim garnish button in the Garnish UI. Pass null to clear it.</summary>
        public void ChooseGarnishRim(SO_GarnishRimOption rim)
            => _glassZone?.Occupant?.ApplyGarnishRim(rim);

        private void OnGarnishSlotClicked(int slotIndex) => _activeGarnishSlot = slotIndex;

        private void SubscribeToGarnishSlots()
        {
            _garnishSlotGlass = _glassZone?.Occupant;
            if (_garnishSlotGlass != null) _garnishSlotGlass.GarnishSlotClicked += OnGarnishSlotClicked;
        }

        private void UnsubscribeFromGarnishSlots()
        {
            if (_garnishSlotGlass == null) return;
            _garnishSlotGlass.GarnishSlotClicked -= OnGarnishSlotClicked;
            _garnishSlotGlass = null;
        }

        private void ChangedToChooseGlass() { 
            _panelGarnishItemUI?.SetActive(false);
            _panelGarnishRimUI?.SetActive(false);
            _panelGlassSelectionUI?.SetActive(true);
        }

        private void ChangedToGarnishItem() { 
            _panelGlassSelectionUI?.SetActive(false);
            _panelGarnishRimUI?.SetActive(false);
            _panelGarnishItemUI?.SetActive(true);
        }

        private void ChangedToGarnishRim() { 
            _panelGlassSelectionUI?.SetActive(false);
            _panelGarnishItemUI?.SetActive(false);
            _panelGarnishRimUI?.SetActive(true);
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
            _glassGrid?.SetInteractable(false); // one glass per pour

            _btnAddIce.interactable = true;
            _btnRemoveIce.interactable = false;
            if (_btnPour != null) _btnPour.interactable = false; // one pour per glass — re-enabled on the next Garnish entry

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

        private static void TryPlay(string id)
        {
            if (!string.IsNullOrEmpty(id))
                ManagerSound.PlayEffect(id);
        }
    }
}
