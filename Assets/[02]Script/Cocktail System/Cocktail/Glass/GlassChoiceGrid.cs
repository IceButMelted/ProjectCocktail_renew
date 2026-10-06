using UnityEngine;

/// <summary>Paged button grid over the glass catalog.</summary>
public class GlassChoiceGrid : PagedChoiceGrid<SO_GlassOption>
{
    [SerializeField] private Bar410.GameFlow.GarnishFlowBridge _garnishBridge;

    protected override Sprite GetIcon(SO_GlassOption option) => option.GlassSprite;
    protected override void Apply(SO_GlassOption option) => _garnishBridge?.ChooseGlass(option);
}
