using UnityEngine;

/// <summary>Paged button grid over the Fresh/Novelty garnish catalog — fills whichever glass slot is active.</summary>
public class GarnishItemChoiceGrid : PagedChoiceGrid<SO_GarnishItemOption>
{
    [SerializeField] private Bar410.GameFlow.GarnishFlowBridge _garnishBridge;

    protected override Sprite GetIcon(SO_GarnishItemOption option) => option.Sprite;
    protected override void Apply(SO_GarnishItemOption option) => _garnishBridge?.ChooseGarnishItem(option);
}
