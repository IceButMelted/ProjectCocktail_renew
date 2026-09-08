// ============================================================
//  DragableFruitTraySlot.cs — a DragableObject that also spawns a
//  fruit piece to pull out (e.g. Mixer-LemonJuice (1)).
//
//  Some ingredients double as a click-to-pour object (Interactable_2_5DObject
//  + IngredientButtonUI on the same GameObject) AND a fruit tray. A plain
//  click still pours normally, unaffected. Dragging is where the conflict
//  is: the host's collider and a spawned FruitPieceInstance's collider
//  would overlap, and which one wins a raycast would be a coin flip.
//
//  Fix: don't let the host start its own drag when hijack is enabled —
//  spawn a piece right when the drag threshold is crossed (not before, so
//  no piece collider competes with the host's Interactable_2_5DObject
//  collider until the player has committed to a drag) and hand the rest
//  of the gesture to it via
//  DragableObject.BeginRedirectedDrag/FinishRedirectedDrag. See
//  DragableObject.OnThresholdCrossed — the one hook this relies on.
//
//  The spawned piece gets a null FruitTraySlot origin on purpose: unlike
//  plain FruitTraySlot's "always keep one ready" respawn loop, a piece
//  here shouldn't respawn after being consumed — the next one is only
//  created by the next drag gesture, so nothing exists outside an active
//  drag.
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;
using static E_Cocktail;

public class DragableFruitTraySlot : DragableObject
{
    [SerializeField] private Mixer _fruitType;
    [SerializeField] private GameObject _piecePrefab;

    private bool _hijackEnabled;
    private DragableObject _activePiece;
    private ScaleOnHover _scaleOnHover;

    // Start, not Awake — DragableObject's own Awake is private (not virtual), so a
    // same-named Awake here would hide it from Unity's dispatch instead of running
    // alongside it, leaving PastLocation/_collider on the base class uninitialized.
    private void Start() => _scaleOnHover = GetComponent<ScaleOnHover>();

    /// <summary>
    /// Turns drag-hijack on/off — see InteractableToggle.ApplyOnlyFruitTraySlot, applied by
    /// IngredientButtonGroup's phase methods (on during AddIngredient, off during PrepareBar
    /// so dragging repositions it like any other bar-layout object).
    /// </summary>
    public void SetHijackEnabled(bool enabled) => _hijackEnabled = enabled;

    protected override bool OnThresholdCrossed(PointerEventData eventData)
    {
        if (_activePiece != null) return true;

        if (!_hijackEnabled) return false;

        var piece = SpawnPiece();
        if (piece == null) return false;

        _activePiece = piece;
        piece.BeginRedirectedDrag(eventData);

        // Control just moved to the piece — this object's DragableObject.IsDragging never
        // turns true for a hijacked gesture, so ScaleOnHover never sees a drag-end to reset
        // from (see ScaleOnHover.ForceReset doc). Un-enlarge now instead of waiting for a
        // pointer-exit that may never come.
        _scaleOnHover?.ForceReset();
        return true;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (_activePiece != null)
        {
            _activePiece.FinishRedirectedDrag();
            _activePiece = null;
            return; 
        }

        base.OnPointerUp(eventData);
    }

    /// <summary>Cancel a hijacked piece too if disabled mid-drag, same as base class cancels
    /// its own drag — otherwise the piece would be stranded.</summary>
    protected override void OnInteractableChanged(bool interactable)
    {
        base.OnInteractableChanged(interactable);

        if (!interactable && _activePiece != null)
        {
            _activePiece.FinishRedirectedDrag();
            _activePiece = null;
        }
    }

    private DragableObject SpawnPiece()
    {
        if (_piecePrefab == null)
        {
            Debug.LogWarning($"[DragableFruitTraySlot] '{name}' has no piece prefab assigned.", this);
            return null;
        }

        var instance = Instantiate(_piecePrefab, transform.position, transform.rotation);
        instance.transform.SetParent(transform, true);

        var piece = instance.GetComponent<FruitPieceInstance>();
        if (piece == null)
        {
            Debug.LogWarning($"[DragableFruitTraySlot] '{_piecePrefab.name}' has no FruitPieceInstance component.", this);
            Destroy(instance);
            return null;
        }

        piece.Initialize(_fruitType, null); // null origin — no auto-respawn, see file header
        return piece.GetComponent<DragableObject>();
    }
}
