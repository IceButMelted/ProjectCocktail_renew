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
            makingCocktail.Alcohols[alcohol] += amount;  // Fixed: was += 1
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
            makingCocktail.Mixers[mixer] += amount;  // Fixed: was += 1
        }
        else
        {
            makingCocktail.Mixers.Add(mixer, amount);
        }

    }

    public int GetTotalIngredient()  // Fixed typo: Ingradient -> Ingredient
    {
        int total = 0;

        if (makingCocktail.Alcohols != null)
        {
            total += makingCocktail.Alcohols.Values.Sum();
        }

        if (makingCocktail.Mixers != null)
        {
            total += makingCocktail.Mixers.Values.Sum();
        }

        return total;
    }
}