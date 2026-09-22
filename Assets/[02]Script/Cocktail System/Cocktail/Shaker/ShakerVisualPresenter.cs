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

    private ShakerContents _contents;

    private void Awake()
    {
        _contents = GetComponent<ShakerContents>();
        if (_glassWaterSlosh == null)
            Debug.LogWarning("[ShakerVisualPresenter] No WaterSlosh assigned — glass visuals will not update.", this);
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

    // ── Reactions ──────────────────────────────────────────

    private void OnIdentityResolved(RecipeMatch _) => Apply(_contents.CurrentCocktail);

    private void OnCleared()
    {
        if (_glassWaterSlosh != null) _glassWaterSlosh.waterLevel = 0f;
    }

    /// <summary>
    /// Pushes a drink's colour onto the WaterSlosh renderer. Shaker's own glass/ice/water
    /// sprites are no longer driven per-drink — glass choice belongs to the player-placed
    /// serving glass, applied separately once poured.
    /// </summary>
    public void Apply(S_Drink drink)
    {
        if (_glassWaterSlosh == null || drink == null) return;

        _glassWaterSlosh.waterColorTop = drink.waterColorTop;
        _glassWaterSlosh.waterColorBottom = drink.waterColorBottom;
        _glassWaterSlosh.UpdateColor();
    }
}
