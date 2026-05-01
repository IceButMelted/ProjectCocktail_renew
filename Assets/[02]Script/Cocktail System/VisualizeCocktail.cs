using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static E_Cocktail;

/// <summary>
/// Updates fill bars to visually represent the current cocktail's alcohol / mixer ratio.
/// Max capacity is always 10 parts total.
/// </summary>
public class VisualizeCocktail : MonoBehaviour
{
    private const float MAX_PARTS = 10f;

    [Header("Fill Bars")]
    public Image alcoholFill;
    public Image mixerFill;

    private CocktailShaker _shaker;

    private void Awake()
        => _shaker = FindFirstObjectByType<CocktailShaker>();

    /// <summary>Show the bars and refresh them to match the current shaker state.</summary>
    public void UpdateCocktailBars()
    {
        gameObject.SetActive(true);

        S_Drink d = _shaker.currentCocktail;
        float alcoholRatio = d.GetTotalAlcohol() / MAX_PARTS;
        float mixerRatio   = d.GetTotalMixer()   / MAX_PARTS;

        alcoholFill.fillAmount = alcoholRatio;
        // Stacked bar: mixer starts where alcohol ends
        mixerFill.fillAmount   = alcoholRatio + mixerRatio;
    }

    /// <summary>Hide and reset the bars.</summary>
    public void ResetVisualBars()
    {
        alcoholFill.fillAmount = 0f;
        mixerFill.fillAmount   = 0f;
        gameObject.SetActive(false);
    }
}
