using UnityEngine;

/// <summary>Paged button grid over the Rim garnish catalog.</summary>
public class GarnishRimChoiceGrid : PagedChoiceGrid<SO_GarnishRimOption>
{
    private GarnishManager _garnish;

    private GarnishManager Garnish
        => _garnish != null ? _garnish : (_garnish = GetComponentInParent<GarnishManager>(true));

    protected override Sprite GetIcon(SO_GarnishRimOption option) => option.Sprite;
    protected override void Apply(SO_GarnishRimOption option) => Garnish?.ChooseGarnishRim(option);
}
