// ============================================================
//  FruitTraySlot.cs — One discrete fruit-piece position on a tray
//  (e.g. LimeJuice, LemonJuice — the Mixer values presented as fruit
//  instead of a bottle). Same respawn pattern as GlassShelfSlot: keeps
//  a fresh FruitPieceInstance ready to be dragged, respawning whenever
//  the previous piece is consumed.
//
//  The tray itself is a DragableObject so the whole tray can be moved
//  during bar setup (Level 1 PrepareBarPhase) like the other bar-layout
//  objects BarSetupBridge manages — every spawned piece is parented
//  under it so it follows when the tray is repositioned. Whether the
//  tray-move drag or the per-piece pull-out drag is the one currently
//  live is a phase-driven Interactable toggle, not code here — bind it
//  the same way GameFlowHooks already binds other phase-gated toggles
//  (see Docs/Bar410_GlassFreedom_ManualSetup.md).
//
//  No piece exists at all outside the AddIngredient step — there is no
//  Awake spawn here on purpose. FruitTrayGroup spawns/despawns every
//  tray's piece on AddIngredient.OnEnter/OnExit, so a piece's collider
//  never sits near the tray's own collider while the tray itself is the
//  thing meant to be draggable (bar layout / other phases).
// ============================================================

using UnityEngine;
using static E_Cocktail;

[RequireComponent(typeof(DragableObject))]
public class FruitTraySlot : MonoBehaviour
{
    [SerializeField] private Mixer _fruitType;
    [SerializeField] private GameObject _piecePrefab;

    /// <summary>
    /// Instantiates a fresh FruitPieceInstance at this slot, parented under it so it follows
    /// if the tray gets repositioned. Called by FruitTrayGroup on AddIngredient.OnEnter, and
    /// again by FruitPieceInstance once the previous piece has been consumed (delivered or
    /// dropped short).
    /// </summary>
    public void SpawnReplacement()
    {
        if (_piecePrefab == null)
        {
            Debug.LogWarning($"[FruitTraySlot] '{name}' has no piece prefab assigned.", this);
            return;
        }

        // Instantiate the piece at the slot's position and rotation, then parent it under the slot.
        var instance = Instantiate(_piecePrefab, transform.position, transform.rotation);
        //keep actual size
        instance.transform.SetParent(transform, true);

        var piece = instance.GetComponent<FruitPieceInstance>();

        if (piece == null)
        {
            Debug.LogWarning($"[FruitTraySlot] '{_piecePrefab.name}' has no FruitPieceInstance component.", this);
            return;
        }

        piece.Initialize(_fruitType, this);
    }

    /// <summary>
    /// Destroys whatever piece is currently spawned at this slot (if any). Called by
    /// FruitTrayGroup on AddIngredient.OnExit so no piece collider lingers near the tray's own
    /// collider while a different phase wants the tray itself to be draggable instead.
    /// </summary>
    public void DespawnCurrent()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }
}
