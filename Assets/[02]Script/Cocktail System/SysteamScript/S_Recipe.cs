using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.XR;
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

    public int GetTotalIngredient()
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

    public void GetCocktailInfo()
    {
        string _info = "";

        _info += "Name : " + cocktailName;
        _info += "\n Method : " + method.ToString();
        _info += "\n Add Ice ? : " + AddIce.ToString();
        _info += "\n Type Of Alcohol : " + typeOfCocktail.ToString();
        _info += "\n Alcohols";
        foreach (KeyValuePair<E_Cocktail.Alcohol, int> kvp in Alcohols)
        {
            _info += $"\n\t {kvp.Key} {kvp.Value} shot";
        }

        _info += "\n Mixers";

        foreach (KeyValuePair<E_Cocktail.Mixer, int> kvp in Mixers)
        {
            _info += $"\n\t {kvp.Key} {kvp.Value} shot";
        }

        Debug.Log(_info);
    }

    public bool IsSameRecipe(S_Recipe _Recipe)
    {
        return IsSameAlcohols(_Recipe) &&
               IsSameMixers(_Recipe) &&
               IsSameMethod(_Recipe) &&
               IsSameType(_Recipe) &&
               IsSameIce(_Recipe);
    }

    public bool IsSameMixers(S_Recipe _Recipe)
    {
        // Check if both are null or both are not null
        if (Mixers == null && _Recipe.Mixers == null)
            return true;
        if (Mixers == null || _Recipe.Mixers == null)
            return false;

        // Check if counts match
        if (Mixers.Count != _Recipe.Mixers.Count)
            return false;

        // Check if all keys and values match
        foreach (KeyValuePair<E_Cocktail.Mixer, int> kvp in Mixers)
        {
            if (!_Recipe.Mixers.ContainsKey(kvp.Key))
                return false;
            if (_Recipe.Mixers[kvp.Key] != kvp.Value)
                return false;
        }

        return true;
    }

    public bool IsSameAlcohols(S_Recipe _Recipe)
    {
        // Check if both are null or both are not null
        if (Alcohols == null && _Recipe.Alcohols == null)
            return true;
        if (Alcohols == null || _Recipe.Alcohols == null)
            return false;

        // Check if counts match
        if (Alcohols.Count != _Recipe.Alcohols.Count)
            return false;

        // Check if all keys and values match
        foreach (KeyValuePair<E_Cocktail.Alcohol, int> kvp in Alcohols)
        {
            if (!_Recipe.Alcohols.ContainsKey(kvp.Key))
                return false;
            if (_Recipe.Alcohols[kvp.Key] != kvp.Value)
                return false;
        }

        return true;
    }

    public bool IsSameMethod(S_Recipe _Recipe)
    {
        return this.method == _Recipe.method;
    }

    public bool IsSameType(S_Recipe _Recipe)
    {
        return this.typeOfCocktail == _Recipe.typeOfCocktail;
    }

    public bool IsSameIce(S_Recipe _Recipe) { 
        return this.AddIce == _Recipe.AddIce;
    }


}