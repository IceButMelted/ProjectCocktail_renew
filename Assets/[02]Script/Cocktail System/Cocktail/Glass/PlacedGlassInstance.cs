// ============================================================
//  PlacedGlassInstance.cs — The one glass currently on the table.
//  Carries its own SO_GlassOption visuals (no lookup table — each
//  option bundles its own sprites). Spawned at a fixed point by
//  PlacedGlassInstance.Spawn (GlassPlacementZone kept but unused);
//  destroyed once served, never dragged.
// ============================================================

using System;
using UnityEngine;

public class PlacedGlassInstance : MonoBehaviour
{
    [Tooltip("Optional. If assigned, this glass's sprites/water color are pushed onto it.")]
    [SerializeField] private WaterSlosh _waterSlosh;

    [Header("Garnish")]
    [Tooltip("The 2 slots Fresh and Novelty garnishes share — index 0/1, no per-category restriction.")]
    [SerializeField] private SpriteRenderer[] _garnishSlots = new SpriteRenderer[2];

    [Tooltip("Whole-rim treatment (salt/sugar/etc.) — one per glass, separate from the 2 slots above.")]
    [SerializeField] private SpriteRenderer _garnishRim;

    /// <summary>Fires the clicked slot's index. GarnishFlowBridge listens to track which slot the next Garnish Item button targets.</summary>
    public event Action<int> GarnishSlotClicked;

    public SO_GlassOption Option { get; private set; }

    /// <summary>The one serving glass in the scene (last one initialized), or null.</summary>
    public static PlacedGlassInstance Current { get; private set; }

    /// <summary>
    /// Destroys the current glass (if any) and spawns <paramref name="option"/>'s prefab at
    /// <paramref name="point"/>. Returns the new glass, or null if the option/prefab is invalid.
    /// </summary>
    public static PlacedGlassInstance Spawn(SO_GlassOption option, Transform point)
    {
        if (option == null || option.PlacedPrefab == null || point == null)
        {
            Debug.LogWarning("[PlacedGlassInstance] Spawn needs an SO_GlassOption with a PlacedPrefab and a spawn point.");
            return null;
        }

        DestroyCurrent();

        var instance = Instantiate(option.PlacedPrefab, point.position, point.rotation);
        var glass = instance.GetComponent<PlacedGlassInstance>();
        if (glass == null)
        {
            Debug.LogWarning($"[PlacedGlassInstance] '{option.PlacedPrefab.name}' has no PlacedGlassInstance component.");
            Destroy(instance);
            return null;
        }

        glass.Initialize(option);
        return glass;
    }

    public static void DestroyCurrent()
    {
        if (Current != null) Destroy(Current.gameObject);
    }

    private GlassPlacementZone _zone;

    private void Awake()
    {
        if (_garnishSlots == null) return;

        for (int i = 0; i < _garnishSlots.Length; i++)
        {
            var slot = _garnishSlots[i];
            if (slot == null) continue;

            var button = slot.GetComponent<GarnishSlotButton>();
            if (button == null) continue;

            int index = i; // capture per-iteration for the closure
            button.Clicked += () => GarnishSlotClicked?.Invoke(index);
        }
    }

    /// <summary>Called once, right after Instantiate, by GlassPlacementZone.SetGlass.</summary>
    public void Initialize(SO_GlassOption option)
    {
        Option = option;
        Current = this;

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

    /// <summary>
    /// Sets one of the 2 shared Fresh/Novelty garnish slots. Pass null to clear that slot.
    /// Out-of-range slotIndex is a no-op (logged) rather than an exception — this is called
    /// from UI click handlers, which should never crash the game over a bad index.
    /// </summary>
    public void ApplyGarnishItem(int slotIndex, SO_GarnishItemOption item)
    {
        if (_garnishSlots == null || slotIndex < 0 || slotIndex >= _garnishSlots.Length)
        {
            Debug.LogWarning($"[PlacedGlassInstance] Garnish slot {slotIndex} out of range.", this);
            return;
        }

        var slot = _garnishSlots[slotIndex];
        if (slot != null) slot.sprite = item != null ? item.Sprite : null;
    }

    /// <summary>Sets the whole-rim garnish treatment. Pass null to clear it.</summary>
    public void ApplyGarnishRim(SO_GarnishRimOption rim)
    {
        if (_garnishRim != null) _garnishRim.sprite = rim != null ? rim.Sprite : null;
    }

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
        if (Current == this) Current = null;
        if (_zone != null) _zone.ClearOccupant(this);
    }
}
