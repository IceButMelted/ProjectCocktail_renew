// ============================================================
//  FruitTrayGroup.cs — A named set of fruit trays whose spawned
//  pieces should exist only while AddIngredient is the active
//  Prepare Drinks step.
//
//  Mirrors IngredientButtonGroup's shape (one Inspector binding via
//  GameFlowHooks instead of one binding per tray), but drives
//  FruitTraySlot's spawn/despawn cycle instead of InteractableToggle —
//  piece *presence*, not just interactability, is what needs to change
//  per phase, since a live piece's collider sits close enough to the
//  tray's own collider to interfere with dragging the tray itself
//  (see the collider-overlap risk noted in
//  Docs/Bar410_GlassFreedom_ManualSetup.md §3.3).
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class FruitTrayGroup : MonoBehaviour
{
    [Tooltip("Trays this group spawns and despawns together.")]
    [SerializeField] private List<FruitTraySlot> _members = new List<FruitTraySlot>();

    /// <summary>Bind to AddIngredient.OnEnter — gives every tray a fresh piece to pull from.</summary>
    public void SpawnAll()
    {
        for (int i = 0; i < _members.Count; i++)
            if (_members[i] != null) _members[i].SpawnReplacement();
    }

    /// <summary>
    /// Bind to AddIngredient.OnExit — removes every live piece so trays can be dragged/
    /// repositioned in other phases without a stray piece collider getting in the way.
    /// </summary>
    public void DespawnAll()
    {
        for (int i = 0; i < _members.Count; i++)
            if (_members[i] != null) _members[i].DespawnCurrent();
    }
}
