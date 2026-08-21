// ============================================================
//  PlacedGlassInstance.cs — One glass the player has dragged onto the
//  table. Carries its own SO_GlassOption visuals (no lookup table —
//  each option bundles its own sprites). Destroyed once the customer
//  has been served; a fresh one must be dragged next customer.
// ============================================================

using UnityEngine;

[RequireComponent(typeof(DragableObject))]
public class PlacedGlassInstance : MonoBehaviour
{
    [Tooltip("Optional. If assigned, this glass's sprites/water color are pushed onto it.")]
    [SerializeField] private WaterSlosh _waterSlosh;

    public SO_GlassOption Option { get; private set; }

    private GlassShelfSlot _origin;
    private GlassPlacementZone _zone;

    /// <summary>Called once, right after Instantiate, by the shelf slot that spawned this.</summary>
    public void Initialize(SO_GlassOption option, GlassShelfSlot origin)
    {
        Option = option;
        _origin = origin;

        if (_waterSlosh != null && option != null)
            _waterSlosh.UpdateVisual(option.IceSprite, option.GlassSprite, option.WaterSprite);
    }

    /// <summary>Called by the zone once this instance actually lands there (not on every drag frame).</summary>
    public void NotifyPlaced(GlassPlacementZone zone)
    {
        _zone = zone;

        if (_origin != null)
        {
            _origin.SpawnReplacement();
            _origin = null;
        }
    }

    /// <summary>Pushes the served drink's colour onto this glass. Called by GarnishFlowBridge on pour.</summary>
    public void ApplyDrink(S_Drink drink)
    {
        if (_waterSlosh == null || drink == null) return;

        _waterSlosh.waterColorTop = drink.waterColorTop;
        _waterSlosh.waterColorBottom = drink.waterColorBottom;
        _waterSlosh.UpdateColor();
    }

    private void OnDestroy()
    {
        if (_zone != null) _zone.ClearOccupant(this);
    }
}
