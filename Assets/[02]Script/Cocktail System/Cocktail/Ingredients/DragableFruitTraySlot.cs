// ============================================================
//  DragableFruitTraySlot.cs — A DragableObject that also spawns a
//  fruit piece to pull out (e.g. Mixer-LemonJuice (1)).
//
//  Some ingredients double as a click-to-pour object (Interactable_2_5DObject
//  + IngredientButtonUI on the same GameObject) AND a fruit tray. A plain
//  click still pours normally through those siblings, unaffected. Dragging
//  is where the conflict would be: the host's own collider and a spawned
//  FruitPieceInstance's collider would sit on top of each other, and which
//  one wins a given raycast would be a coin flip.
//
//  Fix: don't let the host start its own drag at all when hijack is
//  enabled — spawn a piece right at the moment the drag threshold is
//  crossed (not before, so no piece collider exists to compete with the
//  host's own Interactable_2_5DObject collider until the player has
//  actually committed to a drag) and hand the rest of the gesture to it
//  via DragableObject.BeginRedirectedDrag/FinishRedirectedDrag. See
//  DragableObject.OnThresholdCrossed — the one small hook this relies on.
//
//  The spawned piece is given a null FruitTraySlot origin on purpose:
//  unlike the plain FruitTraySlot's "always keep one ready" respawn loop,
//  a piece here should not respawn itself after being consumed — the next
//  one is only ever created by the next drag gesture, so nothing exists
//  outside an active drag.
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

    /// <summary>
    /// Turns the drag-hijack on or off — see InteractableToggle.ApplyOnlyFruitTraySlot, applied
    /// by IngredientButtonGroup's phase methods (on during AddIngredient, off during PrepareBar
    /// so dragging this object repositions it like any other bar-layout object instead).
    /// </summary>
    public void SetHijackEnabled(bool enabled) => _hijackEnabled = enabled;

    protected override bool OnThresholdCrossed(PointerEventData eventData)
    {
        // OnDrag fires every frame the pointer moves past threshold, not just once — the base
        // class's own _dragStarted flag is what normally blocks re-entry, but this object's
        // _dragStarted never flips true on the hijack path (that's the whole point), so this
        // needs its own "already handled this gesture" guard or it would spawn a fresh piece
        // every single frame for the rest of the drag.
        if (_activePiece != null) return true;

        if (!_hijackEnabled) return false;

        var piece = SpawnPiece();
        if (piece == null) return false; // no piece prefab assigned — drag the host normally

        _activePiece = piece;
        piece.BeginRedirectedDrag(eventData);
        return true;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (_activePiece != null)
        {
            _activePiece.FinishRedirectedDrag();
            _activePiece = null;
            return; // gesture was handed off entirely — this object neither pours nor drags
        }

        base.OnPointerUp(eventData);
    }

    /// <summary>Cancel a hijacked piece too if this object is disabled mid-drag, the same way
    /// the base class cancels its own drag — otherwise the piece would be stranded.</summary>
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
