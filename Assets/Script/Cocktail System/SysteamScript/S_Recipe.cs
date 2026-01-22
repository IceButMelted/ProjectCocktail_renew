using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct S_Recipe 
{
    public string cocktailName;
    public E_Cocktail.TypeOfCocktail typeOfCocktail;
    public E_Cocktail.Method method;
    public bool AddIce;

    [Header("Ingrdients")]
    [SerializedDictionary("Alcohol","Amount")]
    public SerializedDictionary<E_Cocktail.Alcohol, int> Alcohols;
    [SerializedDictionary("Mixer", "Amount")]
    public SerializedDictionary<E_Cocktail.Mixer, int> Mixers;

    public int GetTotalIngradient()
    {
        int total = 0;

        if (Alcohols != null)
        { 
            List<int> listAlcohol = Alcohols.Values.ToList();
            total += listAlcohol.Sum();
        }

        if (Mixers != null)
        {
            List<int> listMixer = Mixers.Values.ToList();
            total += listMixer.Sum();
        }

        return total;
    }
}