using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static E_Cocktail;

public class VisualizeCocktail : MonoBehaviour
{
    private CocktailShaker cocktailMaker;

    public Image alcohol_fill;
    public Image mixer_fill;

    private readonly Dictionary<Alcohol, Color> AlcoholColors = new Dictionary<Alcohol, Color>()
        {
            { Alcohol.Vodka, Color.orangeRed },
            { Alcohol.Gin, Color.orangeRed },
            { Alcohol.Triplesec, Color.orangeRed },
            { Alcohol.Vermouth, Color.orangeRed }
        };
    private readonly Dictionary<Mixer, Color> MixerColors = new Dictionary<Mixer, Color>()
        {
            { Mixer.CanberryJuice, Color.olive },
            { Mixer.GrapefruitJuice, Color.olive },
            { Mixer.LemonJuice, Color.olive },
            { Mixer.Soda, Color.olive },
            { Mixer.Syrup, Color.olive },
            { Mixer.PepperMint, Color.olive }
        };

    private void Awake()
    {
        cocktailMaker = FindFirstObjectByType<CocktailShaker>();
        
    }

    public void UpdateCocktailBars()
    {
        this.gameObject.SetActive(true);
        S_Drink currentCocktail = cocktailMaker.currentCocktail;

        alcohol_fill.fillAmount = (float)currentCocktail.GetTotalAlcohol() / 10;
        mixer_fill.fillAmount = ((float)currentCocktail.GetTotalMixer() / 10) + (float)alcohol_fill.fillAmount;

    }

    public void ResetVisualBars() {
        alcohol_fill.fillAmount = 0;
        mixer_fill.fillAmount = 0;

        this.gameObject.SetActive(false);
    }
}

