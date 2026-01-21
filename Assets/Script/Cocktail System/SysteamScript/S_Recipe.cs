using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct S_Recipe : IEquatable<S_Recipe>
{
    public string cocktailName;
    public E_Cocktail.TypeOfCocktail typeOfCocktail;
    public E_Cocktail.Method method;
    public bool AddIce;

    [Header("Ingredients")]
    public List<AlcoholAmount> alcohols;
    public List<MixerAmount> mixers;

    public S_Recipe(bool initialize = true)
    {
        cocktailName = "";
        typeOfCocktail = default;
        method = default;
        AddIce = false;
        alcohols = new List<AlcoholAmount>();
        mixers = new List<MixerAmount>();
    }

    // IEquatable implementation
    public bool Equals(S_Recipe other)
    {
        if(this.GetTotalIngradient() != other.GetTotalIngradient())
            return false;

        return method == other.method &&
               AddIce == other.AddIce &&
               alcohols.Equals(other.alcohols) &&
               mixers.Equals(other.mixers);
    }
    
/*    public bool Equals(S_Recipe other)
    {
        return cocktailName == other.cocktailName &&
               typeOfCocktail == other.typeOfCocktail &&
               method == other.method &&
               AddIce == other.AddIce &&
               alcohols.SequenceEqual(other.alcohols) &&
               mixers.SequenceEqual(other.mixers);
    }*/

    public override bool Equals(object obj)
    {
        return obj is S_Recipe other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(cocktailName, typeOfCocktail, method, AddIce);
    }

    // Utility methods
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(cocktailName))
            return false;

        if ((alcohols == null || alcohols.Count == 0) &&
            (mixers == null || mixers.Count == 0))
            return false;

        if (alcohols != null && alcohols.Any(a => a.amount <= 0))
            return false;

        if (mixers != null && mixers.Any(m => m.amount <= 0))
            return false;

        return true;
    }

    public int GetTotalIngradient()
    {
        int total = 0;

        if (alcohols != null)
            total += alcohols.Sum(a => a.amount);

        if (mixers != null)
            total += mixers.Sum(m => m.amount);

        return total;
    }

    public float GetAlcoholPercentage()
    {
        int totalVolume = GetTotalIngradient();
        if (totalVolume == 0) return 0f;

        int alcoholVolume = alcohols != null ? alcohols.Sum(a => a.amount) : 0;
        return (float)alcoholVolume / totalVolume * 100f;
    }
}

[System.Serializable]
public struct AlcoholAmount : IEquatable<AlcoholAmount>
{
    public E_Cocktail.Alcohol alcohol;
    public short amount;

    public AlcoholAmount(E_Cocktail.Alcohol alcohol = E_Cocktail.Alcohol.None, short amount = 0)
    {
        this.alcohol = alcohol;
        this.amount = amount;
    }

    public bool Equals(AlcoholAmount other)
    {
        return alcohol == other.alcohol && amount == other.amount;
    }

    public override bool Equals(object obj)
    {
        return obj is AlcoholAmount other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(alcohol, amount);
    }
}

[System.Serializable]
public struct MixerAmount : IEquatable<MixerAmount>
{
    public E_Cocktail.Mixer mixer;
    public short amount;

    public MixerAmount(E_Cocktail.Mixer mixer = E_Cocktail.Mixer.None, short amount = 0)
    {
        this.mixer = mixer;
        this.amount = amount;
    }

    public bool Equals(MixerAmount other)
    {
        return mixer == other.mixer && amount == other.amount;
    }

    public override bool Equals(object obj)
    {
        return obj is MixerAmount other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(mixer, amount);
    }

}