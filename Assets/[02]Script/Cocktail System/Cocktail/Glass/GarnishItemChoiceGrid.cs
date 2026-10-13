using UnityEngine;

/// <summary>Paged button grid over the Fresh/Novelty garnish catalog — fills whichever glass slot is active.</summary>
public class GarnishItemChoiceGrid : PagedChoiceGrid<SO_GarnishItemOption>
{
    private GarnishManager _garnish;

    private GarnishManager Garnish
        => _garnish != null ? _garnish : (_garnish = GetComponentInParent<GarnishManager>(true));

    // Usable only while a glass is on the plate.
    protected override void Awake()
    {
        base.Awake();

        var garnish = Garnish;
        if (garnish == null) return;

        garnish.GlassChanged += SetInteractable;
        SetInteractable(garnish.Glass != null);
    }

    protected override void OnDestroy()
    {
        if (_garnish != null) _garnish.GlassChanged -= SetInteractable;

        base.OnDestroy();
    }

    protected override Sprite GetIcon(SO_GarnishItemOption option) => option.Sprite;
    protected override void Apply(SO_GarnishItemOption option) => Garnish?.ChooseGarnishItem(option);
}
