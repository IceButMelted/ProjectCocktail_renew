using UnityEngine;
using UnityEngine.UI;
using static DrinkQuery;

/// <summary>
/// Updates fill bars to visually represent the current cocktail's alcohol / mixer ratio.
/// Max capacity is always 10 parts total.
/// </summary>
public class VisualizeCocktail : MonoBehaviour
{
    private const float MAX_PARTS = DrinkQuery.MaxTotalParts;

    [Header("Fill Bars")]
    [SerializeField] private Image alcoholFill;
    [SerializeField] private Image mixerFill;

    private CocktailShaker _shaker;

    private void Awake()
    {
        _shaker = FindFirstObjectByType<CocktailShaker>();

        // Ensure both images respond to fillAmount regardless of Inspector setting
        InitFillImage(alcoholFill);
        InitFillImage(mixerFill);
    }

    private static void InitFillImage(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial180;
        img.fillAmount = 0f;
    }

    /// <summary>Show the bars and refresh them to match the current shaker state.</summary>
    public void UpdateCocktailBars()
    {
        gameObject.SetActive(true);

        S_Drink d = _shaker.CurrentCocktail;
        float alcoholRatio = (GetTotalAlcohol(d) + GetTotalLiqueur(d)) / MAX_PARTS;
        float mixerRatio = GetTotalMixer(d) / MAX_PARTS;

        alcoholFill.fillAmount = Mathf.Clamp01(alcoholRatio);
        mixerFill.fillAmount = Mathf.Clamp01(alcoholRatio + mixerRatio);
    }

    /// <summary>Hide and reset the bars.</summary>
    public void ResetVisualBars()
    {
        alcoholFill.fillAmount = 0f;
        mixerFill.fillAmount = 0f;
        gameObject.SetActive(false);
    }
}