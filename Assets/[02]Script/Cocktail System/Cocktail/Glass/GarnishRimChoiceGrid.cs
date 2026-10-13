using UnityEngine;

/// <summary>Paged button grid over the Rim garnish catalog.</summary>
public class GarnishRimChoiceGrid : PagedChoiceGrid<SO_GarnishRimOption>
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

    protected override Sprite GetIcon(SO_GarnishRimOption option) => option.Sprite;
    protected override void Apply(SO_GarnishRimOption option) => Garnish?.ChooseGarnishRim(option);
}
