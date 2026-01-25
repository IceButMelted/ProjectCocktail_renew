using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static E_Cocktail;

// Define custom UnityEvent types at the top of your file
[System.Serializable]
public class AlcoholEvent : UnityEvent<Alcohol, int> { }

[System.Serializable]
public class MixerEvent : UnityEvent<Mixer, int> { }

public class CocktailMaker : MonoBehaviour
{
    public UnityEvent OnAddIngredient;
    public AlcoholEvent OnAddAlcohol;  // New typed event
    public MixerEvent OnAddMixer;      // New typed event

    public S_Recipe makingCocktail;

    private void Awake()
    {
        
    }

    public void AddAlcohol(Alcohol alcohol, int amount)
    {
        Debug.Log($"Add {alcohol} for {amount} shot");

        if (makingCocktail.Alcohols.ContainsKey(alcohol))
        {
            makingCocktail.Alcohols[alcohol] += amount;
        }
        else
        {
            makingCocktail.Alcohols.Add(alcohol, amount);
        }

    }

    public void AddMixer(Mixer mixer, int amount)
    {
        Debug.Log($"Add {mixer} for {amount} shot");

        if (makingCocktail.Mixers.ContainsKey(mixer))
        {
            makingCocktail.Mixers[mixer] += amount;  
        }
        else
        {
            makingCocktail.Mixers.Add(mixer, amount);
        }

    }

    public int GetTotalIngredient()  
    {
        return makingCocktail.GetTotalIngredient();
    }

}