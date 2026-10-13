// ============================================================
//  GlassPlacementZone.cs — The one spot the serving glass lives at.
//
//  No longer a drag target: the glass is chosen from a UI list
//  (SetGlass) and always spawns already placed here. Still enforces
//  "only one serving glass exists in the whole scene at a time".
// ============================================================

using UnityEngine;

public class GlassPlacementZone : MonoBehaviour
{
    // Static and deliberately shared across every zone instance: only one serving glass
    // may exist in the whole scene, not one per zone. If a second zone is added, placing
    // a glass there still evicts whichever glass exists elsewhere — never two at once.
    private static PlacedGlassInstance _occupant;

    /// <summary>The glass currently placed here, or null if the zone is empty.</summary>
    public PlacedGlassInstance Occupant => _occupant;

    /// <summary>
    /// Chosen from the Garnish glass-picker UI. Destroys whatever glass was here first (swap,
    /// never rejected) and spawns the new one already placed at this zone's transform.
    /// </summary>
    public void SetGlass(SO_GlassOption option)
    {
        if (option == null || option.PlacedPrefab == null)
        {
            Debug.LogWarning($"[GlassPlacementZone] '{name}' got a SO_GlassOption with no PlacedPrefab.", this);
            return;
        }

        if (_occupant != null) Destroy(_occupant.gameObject);

        var instance = Instantiate(option.PlacedPrefab, transform.position, transform.rotation);
        var glass = instance.GetComponent<PlacedGlassInstance>();

        if (glass == null)
        {
            Debug.LogWarning($"[GlassPlacementZone] '{option.PlacedPrefab.name}' has no PlacedGlassInstance component.", this);
            Destroy(instance);
            return;
        }

        glass.Initialize(option);
        _occupant = glass;
        glass.NotifyPlaced(this);
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
