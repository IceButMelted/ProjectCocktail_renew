using UnityEngine;

/// <summary>Paged button grid over the Rim garnish catalog.</summary>
public class GarnishRimChoiceGrid : PagedChoiceGrid<SO_GarnishRimOption>
{
    [SerializeField] private Bar410.GameFlow.GarnishFlowBridge _garnishBridge;

    protected override Sprite GetIcon(SO_GarnishRimOption option) => option.Sprite;
    protected override void Apply(SO_GarnishRimOption option) => _garnishBridge?.ChooseGarnishRim(option);
}
