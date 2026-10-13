using UnityEngine;

/// <summary>Paged button grid over the Fresh/Novelty garnish catalog — fills whichever glass slot is active.</summary>
public class GarnishItemChoiceGrid : PagedChoiceGrid<SO_GarnishItemOption>
{
    private GarnishManager _garnish;

    private GarnishManager Garnish
        => _garnish != null ? _garnish : (_garnish = GetComponentInParent<GarnishManager>(true));

    protected override Sprite GetIcon(SO_GarnishItemOption option) => option.Sprite;
    protected override void Apply(SO_GarnishItemOption option) => Garnish?.ChooseGarnishItem(option);
}
