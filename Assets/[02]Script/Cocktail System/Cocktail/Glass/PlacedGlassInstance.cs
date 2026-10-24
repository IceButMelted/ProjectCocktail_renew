// ============================================================
//  PlacedGlassInstance.cs — The glass on the table.
//  Its prefab carries the glass's own sprites, liquid mask and
//  markers (one prefab per SO_GlassOption). GarnishManager spawns it
//  at a fixed point, owns the reference and destroys it once served;
//  it is never dragged (GlassPlacementZone kept but unused).
// ============================================================

using System;
using System.Collections.Generic;
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

    // The size each garnish renderer was authored at. Whatever sprite goes in is fitted to it.
    private struct SlotBox
    {
        public Vector2 Size;          // parent-space width/height the sprite may fill
        public Vector3 Scale;         // authored localScale
        public BoxCollider Collider;  // click area, kept at its authored size
        public Vector3 ColliderSize;
    }

    private readonly Dictionary<SpriteRenderer, SlotBox> _slotBoxes = new Dictionary<SpriteRenderer, SlotBox>();

    private void Awake()
    {
        RememberBox(_garnishRim);
        if (_garnishSlots == null) return;

        foreach (var slot in _garnishSlots) RememberBox(slot);

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

        SetFitted(_garnishSlots[slotIndex], item != null ? item.Sprite : null);
    }

    /// <summary>Sets the whole-rim garnish treatment. Pass null to clear it.</summary>
    public void ApplyGarnishRim(SO_GarnishRimOption rim)
    {
        SetFitted(_garnishRim, rim != null ? rim.Sprite : null);
    }

    private void RememberBox(SpriteRenderer slot)
    {
        if (slot == null) return;

        Vector3 scale = slot.transform.localScale;
        Vector2 spriteSize = slot.sprite != null ? (Vector2)slot.sprite.bounds.size : Vector2.one;
        var collider = slot.GetComponent<BoxCollider>();

        _slotBoxes[slot] = new SlotBox
        {
            Size = new Vector2(spriteSize.x * scale.x, spriteSize.y * scale.y),
            Scale = scale,
            Collider = collider,
            ColliderSize = collider != null ? collider.size : Vector3.zero,
        };
    }

    // Shows the sprite as large as fits the slot's authored box, keeping its proportions, so
    // garnish art of any pixel size lands at the same size on the glass.
    private void SetFitted(SpriteRenderer slot, Sprite sprite)
    {
        if (slot == null) return;

        slot.sprite = sprite;
        if (sprite == null || !_slotBoxes.TryGetValue(slot, out SlotBox box)) return;

        Vector2 size = sprite.bounds.size;
        if (size.x <= 0f || size.y <= 0f) return;

        float k = Mathf.Min(box.Size.x / size.x, box.Size.y / size.y);
        slot.transform.localScale = new Vector3(k, k, box.Scale.z);

        // The collider scales with the transform; undo that so the click area stays put.
        if (box.Collider != null)
            box.Collider.size = new Vector3(box.ColliderSize.x * box.Scale.x / k, box.ColliderSize.y * box.Scale.y / k, box.ColliderSize.z);
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
