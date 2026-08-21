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

    [Tooltip("Shared GlassType -> sprites asset. See plan D5 / §10.1.")]
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

    /// <summary>Pushes a drink's colour and glass type onto the WaterSlosh renderer.</summary>
    public void Apply(S_Drink drink)
    {
        if (_glassWaterSlosh == null || drink == null) return;

        _glassWaterSlosh.waterColorTop = drink.waterColorTop;
        _glassWaterSlosh.waterColorBottom = drink.waterColorBottom;

        if (_glassVisuals == null)
        {
            Debug.LogWarning("[ShakerVisualPresenter] No SO_GlassVisualTable assigned — glass sprites unchanged.", this);
        }
        else if (_glassVisuals.TryGet(drink.CompatibleGlass, out var visual))
        {
            _glassWaterSlosh.UpdateVisual(visual.IceSprite, visual.GlassSprite, visual.WaterSprite);
        }
        else
        {
            // Known gap G1 — the shipped tables only filled 4 of 8 GlassType values.
            Debug.LogWarning($"[ShakerVisualPresenter] No visual for glass type {drink.CompatibleGlass}.", this);
        }

        _glassWaterSlosh.UpdateColor();
    }
}
