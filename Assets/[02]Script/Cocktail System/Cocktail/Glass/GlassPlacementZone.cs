// ============================================================
//  GlassPlacementZone.cs — A tabletop zone that holds exactly one
//  placed serving glass at a time. Dragging in a different glass
//  swaps it — the previous occupant is destroyed, not rejected. Also
//  accepts the mixing vessel being dragged in to pour once a glass
//  is already there.
// ============================================================

using UnityEngine;

public class GlassPlacementZone : SurfacePlacementZone
{
    // Static and deliberately shared across every GlassPlacementZone instance: the design
    // wants exactly one serving glass to exist in the whole scene, not one per zone. If a
    // second zone is ever added, placing a glass there still evicts whichever glass exists
    // anywhere else — never two at once.
    private static PlacedGlassInstance _occupant;

    /// <summary>The glass currently placed here, or null if the zone is empty.</summary>
    public PlacedGlassInstance Occupant => _occupant;

    private void OnEnable() => Placed += OnItemPlaced;

    private void OnDisable() => Placed -= OnItemPlaced;

    public override bool CanPlace(GameObject item)
    {
        // Any glass may land here — a second, different glass swaps out whatever is here
        // already (see OnItemPlaced), it is never just rejected.
        if (item.TryGetComponent<PlacedGlassInstance>(out _))
            return true;

        if (item.TryGetComponent<PourSource>(out _))
            return _occupant != null;

        return base.CanPlace(item);
    }

    private void OnItemPlaced(GameObject item)
    {
        if (item.TryGetComponent<PlacedGlassInstance>(out var glass))
        {
            // Swap: destroy whatever was here before accepting the new one. Destroy() is
            // deferred to end-of-frame, so _occupant is reassigned before the old instance's
            // OnDestroy runs — its ClearOccupant(old) call then sees _occupant != old and
            // correctly no-ops instead of clearing the new occupant we just set.
            if (_occupant != null && _occupant != glass) Destroy(_occupant.gameObject);

            _occupant = glass;
            glass.NotifyPlaced(this);
        }

        // A PourSource drop is handled by GarnishFlowBridge, which subscribes to Placed itself.
    }

    /// <summary>Frees the zone. Called by PlacedGlassInstance.OnDestroy — do not call this otherwise.</summary>
    public void ClearOccupant(PlacedGlassInstance glass)
    {
        if (_occupant == glass) _occupant = null;
    }

    /// <summary>Destroys whatever glass is currently placed here, e.g. on a Garnish→PrepareDrinks backtrack.</summary>
    public void ClearAndDestroyOccupant()
    {
        if (_occupant != null) Destroy(_occupant.gameObject);
    }
}
