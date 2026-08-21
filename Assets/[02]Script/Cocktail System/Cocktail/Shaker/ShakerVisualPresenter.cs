// ============================================================
//  ShakerVisualPresenter.cs — Makes the glass on screen look like
//  the drink in ShakerContents. Reads state, never changes it.
// ============================================================

using UnityEngine;

[RequireComponent(typeof(ShakerContents))]
public class ShakerVisualPresenter : MonoBehaviour
{
    [Header("Glass")]
    [SerializeField] private WaterSlosh _glassWaterSlosh;

    [Tooltip("LEGACY only. S_Drink no longer carries a glass, so this is never read for " +
             "per-drink sprite switching anymore — kept only so CocktailShakerData's Initialize " +
             "call (unmigrated scenes) still compiles and can still fill it for its own use.")]
    [SerializeField] private SO_GlassVisualTable _glassVisuals;

    private ShakerContents _contents;

    public WaterSlosh Glass => _glassWaterSlosh;

    private void Awake()
    {
        _contents = GetComponent<ShakerContents>();
        if (_glassWaterSlosh == null)
            Debug.LogWarning("[ShakerVisualPresenter] No WaterSlosh assigned — glass visuals will not update.", this);
    }

    /// <summary>
    /// Seeds references when this component was created at runtime rather than authored in
    /// the scene. Used by the CocktailShakerData compatibility shim; assign the fields in
    /// the Inspector instead once the manual migration has been done.
    /// </summary>
    public void Initialize(WaterSlosh glass, SO_GlassVisualTable visuals)
    {
        if (_glassWaterSlosh == null) _glassWaterSlosh = glass;
        if (_glassVisuals == null) _glassVisuals = visuals;
    }

    private void OnEnable()
    {
        if (_contents == null) return;
        _contents.IdentityResolved.AddListener(OnIdentityResolved);
        _contents.Cleared.AddListener(OnCleared);
    }

    private void OnDisable()
    {
        if (_contents == null) return;
        _contents.IdentityResolved.RemoveListener(OnIdentityResolved);
        _contents.Cleared.RemoveListener(OnCleared);
    }

    // ── Fill animation passthrough (bound from UnityEvents) ──

    public void StartFill() { if (_glassWaterSlosh != null) _glassWaterSlosh.StartFilling(); }
    public void StopFill() { if (_glassWaterSlosh != null) _glassWaterSlosh.StopFilling(); }
    public void FinishFill() { if (_glassWaterSlosh != null) _glassWaterSlosh.FinishFilling(); }

    // ── Reactions ──────────────────────────────────────────

    private void OnIdentityResolved(RecipeMatch _) => Apply(_contents.CurrentCocktail);

    private void OnCleared()
    {
        if (_glassWaterSlosh != null) _glassWaterSlosh.waterLevel = 0f;
    }

    /// <summary>
    /// Pushes a drink's colour onto the WaterSlosh renderer. The shaker's own glass/ice/water
    /// sprites are no longer driven per-drink — S_Drink has no glass field anymore, glass
    /// choice belongs to the player-placed serving glass, applied separately once poured.
    /// </summary>
    public void Apply(S_Drink drink)
    {
        if (_glassWaterSlosh == null || drink == null) return;

        _glassWaterSlosh.waterColorTop = drink.waterColorTop;
        _glassWaterSlosh.waterColorBottom = drink.waterColorBottom;
        _glassWaterSlosh.UpdateColor();
    }
}
