using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static E_Cocktail;

public class VisualizeCocktail : MonoBehaviour
{
    private CocktailMaker cocktailMaker;
    //public List<> AllBars;

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
        cocktailMaker = GetComponent<CocktailMaker>();
    }

    public void UpdateCocktailBars()
    {
        alcohol_fill.fillAmount = (float)cocktailMaker.GetTotalAlcohol() / 10;

        mixer_fill.fillAmount = ((float)cocktailMaker.GetTotalMixer() / 10) + (float)alcohol_fill.fillAmount;


        Debug.Log($"Alcohol Fill Amount: {alcohol_fill.fillAmount}");
        Debug.Log($"Mixer Fill Amount: {mixer_fill.fillAmount}");


        //if (_currentCocktail == null) return;

        //// get flattened parts from model (type depends on your model/wrapper)
        //List<Cocktail.IngredientPart> parts;

        //// if using CocktailBuilder wrapper:
        //// parts = _currentCocktail.GetFlattenedParts(10);

        //// if using Cocktail directly:
        //parts = _currentCocktail.GetFlattenedParts(10);

        //for (int i = 0; i < AllBars.Count; i++)
        //{
        //    if (i < parts.Count)
        //    {
        //        var p = parts[i];
        //        if (p.IsAlcohol)
        //        {
        //            Color c = AlcoholColors.ContainsKey(p.Alcohol) ? AlcoholColors[p.Alcohol] : Color.White;
        //            AllBars[i].FillColor = c;
        //            AllBars[i].Opacity = 255;
        //        }
        //        else
        //        {
        //            Color c = MixerColors.ContainsKey(p.Mixer) ? MixerColors[p.Mixer] : Color.White;
        //            AllBars[i].FillColor = c;
        //            AllBars[i].Opacity = 255;
        //        }
        //    }
        //    else
        //    {
        //        // empty bars
        //        AllBars[i].FillColor = Color.Transparent;
        //        // AllBars[i].Opacity = 128; // optional: dim empty bars instead of hide
        //    }
        //}
    }


}
