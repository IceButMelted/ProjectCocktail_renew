// ============================================================
//  PlacedGlassInstance.cs — The one glass currently on the table.
//  Carries its own SO_GlassOption visuals (no lookup table — each
//  option bundles its own sprites). Spawned already placed by
//  GlassPlacementZone.SetGlass; destroyed once served, never dragged.
// ============================================================

using System;
using UnityEngine;

public class PlacedGlassInstance : MonoBehaviour
{
    [Tooltip("Optional. If assigned, this glass's sprites/water color are pushed onto it.")]
    [SerializeField] private WaterSlosh _waterSlosh;

    public SO_GlassOption Option { get; private set; }

    private GlassPlacementZone _zone;

    /// <summary>Called once, right after Instantiate, by GlassPlacementZone.SetGlass.</summary>
    public void Initialize(SO_GlassOption option)
    {
        Option = option;

        if (_waterSlosh != null && option != null)
            _waterSlosh.UpdateVisual(option.IceSprite, option.GlassSprite, option.WaterSprite);
    }

    /// <summary>Called by the zone once this instance is assigned as its occupant.</summary>
    public void NotifyPlaced(GlassPlacementZone zone) => _zone = zone;

    /// <summary>Pushes the served drink's colour onto this glass. Called by GarnishFlowBridge on pour.</summary>
    public void ApplyDrink(S_Drink drink)
    {
        if (_waterSlosh == null || drink == null) return;

        _waterSlosh.waterColorTop = drink.waterColorTop;
        _waterSlosh.waterColorBottom = drink.waterColorBottom;
        _waterSlosh.UpdateColor();
    }

    /// <summary>Toggles the ice visual. Called by GarnishFlowBridge.ToggleIce.</summary>
    public void ApplyIce(bool enable) => _waterSlosh?.AddIce(enable);

    /// <summary>Starts the water-level-rising animation. Called by GarnishFlowBridge.Pour.</summary>
    public void StartFill() => _waterSlosh?.StartFilling();

    /// <summary>
    /// Fires once the pour animation finishes raising the water level. Pass-through to the
    /// sibling WaterSlosh so callers (GarnishFlowBridge) react to the fill finishing instead of
    /// polling a boolean every frame.
    /// </summary>
    public event Action OnFillComplete
    {
        add { if (_waterSlosh != null) _waterSlosh.OnFillComplete += value; }
        remove { if (_waterSlosh != null) _waterSlosh.OnFillComplete -= value; }
    }

    private void OnDestroy()
    {
        if (_zone != null) _zone.ClearOccupant(this);
    }
}
