using UnityEngine;
using UnityEngine.UI;
using static DrinkQuery;

/// <summary>
/// Updates fill bars to visually represent the current cocktail's alcohol / mixer ratio.
/// Max capacity is always 10 parts total (GDD §15).
///
/// Reads <see cref="ShakerContents"/> — the refactored owner of the live drink. It used to
/// read the legacy CocktailShaker, which no longer exists in scenes that finished the
/// migration, so the lookup returned null and the first refresh threw.
/// </summary>
public class VisualizeCocktail : MonoBehaviour
{
    private const float MAX_PARTS = DrinkQuery.MaxTotalParts;

    [Header("Fill Bars")]
    [SerializeField] private Image alcoholFill;
    [SerializeField] private Image mixerFill;

    [Header("Source")]
    [Tooltip("Leave empty to find the one in the scene on Awake.")]
    [SerializeField] private ShakerContents _shaker;

    private void Awake()
    {
        if (_shaker == null)
            _shaker = FindFirstObjectByType<ShakerContents>(FindObjectsInactive.Include);

        if (_shaker == null)
            Debug.LogWarning("[VisualizeCocktail] No ShakerContents in the scene — the bars will stay empty.", this);

        // Ensure both images respond to fillAmount regardless of Inspector setting
        InitFillImage(alcoholFill);
        InitFillImage(mixerFill);
    }

    private static void InitFillImage(Image img)
    {
        if (img == null) return;

        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial180;
        img.fillAmount = 0f;
    }

    /// <summary>Show the bars and refresh them to match the current shaker state.</summary>
    public void UpdateCocktailBars()
    {
        gameObject.SetActive(true);
        if (_shaker == null) return;

        S_Drink d = _shaker.CurrentCocktail;
        float alcoholRatio = (GetTotalAlcohol(d) + GetTotalLiqueur(d)) / MAX_PARTS;
        float mixerRatio = GetTotalMixer(d) / MAX_PARTS;

        SetFill(alcoholFill, alcoholRatio);
        SetFill(mixerFill, alcoholRatio + mixerRatio);
    }

    /// <summary>Hide and reset the bars.</summary>
    public void ResetVisualBars()
    {
        SetFill(alcoholFill, 0f);
        SetFill(mixerFill, 0f);
        gameObject.SetActive(false);
    }

    private static void SetFill(Image img, float ratio)
    {
        if (img != null) img.fillAmount = Mathf.Clamp01(ratio);
    }
}
