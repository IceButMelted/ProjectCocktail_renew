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

    public void  GetCocktailInfo() {
        string _info = "";

        _info += "Name : " + makingCocktail.cocktailName;
        _info += "\n Method : " + makingCocktail.method.ToString();
        _info += "\n Add Ice ? : " + makingCocktail.AddIce.ToString();
        _info += "\n Type Of Alcohol : " + makingCocktail.typeOfCocktail.ToString();
        _info += "\n Alcohols";
        foreach (KeyValuePair<E_Cocktail.Alcohol, int> kvp in makingCocktail.Alcohols) {
            _info += $"\n\t {kvp.Key} {kvp.Value} shot";
        }

        _info += "\n Mixers";

        foreach (KeyValuePair<E_Cocktail.Mixer, int> kvp in makingCocktail.Mixers)
        {
            _info += $"\n\t {kvp.Key} {kvp.Value} shot";
        }
        
        Debug.Log( _info);
    }
}