using UnityEngine;

/// <summary>
/// Paged button grid over the glass catalog. Lives under the GarnishManager's panel, so it
/// finds the manager above it and locks itself when a pour starts (one glass per pour).
/// </summary>
public class GlassChoiceGrid : PagedChoiceGrid<SO_GlassOption>
{
    private GarnishManager _garnish;

    private GarnishManager Garnish
        => _garnish != null ? _garnish : (_garnish = GetComponentInParent<GarnishManager>(true));

    // Subscribed for the grid's whole life, not per enable: a pour can start while the glass tab
    // is hidden behind another tab, and the lock must still land.
    protected override void Awake()
    {
        base.Awake();

        var garnish = Garnish;
        if (garnish == null) return;

        garnish.Opened += Unlock;
        garnish.PourStarted += Lock;
    }

    protected override void OnDestroy()
    {
        if (_garnish != null)
        {
            _garnish.Opened -= Unlock;
            _garnish.PourStarted -= Lock;
        }

        base.OnDestroy();
    }

    private void Lock() => SetInteractable(false);
    private void Unlock() => SetInteractable(true);

    protected override Sprite GetIcon(SO_GlassOption option) => option.GlassSprite;
    protected override void Apply(SO_GlassOption option) => Garnish?.ChooseGlass(option);
}
