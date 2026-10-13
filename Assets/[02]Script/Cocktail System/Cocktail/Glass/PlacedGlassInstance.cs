// ============================================================
//  PlacedGlassInstance.cs — The glass on the table.
//  Its prefab carries the glass's own sprites, liquid mask and
//  markers (one prefab per SO_GlassOption). GarnishManager spawns it
//  at a fixed point, owns the reference and destroys it once served;
//  it is never dragged (GlassPlacementZone kept but unused).
// ============================================================

using System;
using UnityEngine;

public class PlacedGlassInstance : MonoBehaviour
{
    [Tooltip("The liquid, surface and ice of this glass (CocktailCrafter_V2).")]
    [SerializeField] private CocktailGlass _liquid;

    [Header("Garnish")]
    [Tooltip("The 2 slots Fresh and Novelty garnishes share — index 0/1, no per-category restriction.")]
    [SerializeField] private SpriteRenderer[] _garnishSlots = new SpriteRenderer[2];

    [Tooltip("Whole-rim treatment (salt/sugar/etc.) — one per glass, separate from the 2 slots above.")]
    [SerializeField] private SpriteRenderer _garnishRim;

    /// <summary>Fires the clicked slot's index. GarnishManager listens to track which slot the next Garnish Item button targets.</summary>
    public event Action<int> GarnishSlotClicked;

    public SO_GlassOption Option { get; private set; }

    private GlassPlacementZone _zone;
    private bool _hasIce;

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

    /// <summary>Called once, right after Instantiate, by GarnishManager.ChooseGlass.</summary>
    public void Initialize(SO_GlassOption option) => Option = option;

    /// <summary>Called by the zone once this instance is assigned as its occupant.</summary>
    public void NotifyPlaced(GlassPlacementZone zone) => _zone = zone;

    /// <summary>
    /// Drops ice in, or takes it back out. Taking it out empties the whole glass, so it is only
    /// valid before the pour — GarnishManager.ToggleIce enforces that.
    /// </summary>
    public void ApplyIce(bool enable)
    {
        if (_liquid == null || enable == _hasIce) return;

        _hasIce = enable;
        if (enable) _liquid.DropIce();
        else _liquid.ResetGlass();
    }

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

    /// <summary>Pours <paramref name="drink"/>'s colours into this glass. Called by GarnishManager.Pour.</summary>
    public void Pour(PourVisual pour, S_Drink drink)
    {
        if (_liquid == null || pour == null || drink == null) return;

        pour.Pour(_liquid, drink.waterColorTop, drink.waterColorBottom, null, () => OnFillComplete?.Invoke());
    }

    /// <summary>Fires once the pour has finished and the glass is full.</summary>
    public event Action OnFillComplete;

    private void OnDestroy()
    {
        if (_zone != null) _zone.ClearOccupant(this);
    }
}
