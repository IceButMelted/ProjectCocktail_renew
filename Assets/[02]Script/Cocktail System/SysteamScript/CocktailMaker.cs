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
    public UnityEvent OnResetCocktail;

    [HideInInspector]
    public S_Drink currentCocktail;

    private void Awake()
    {

    }

    public void SetMethod(Method method)
    {
        currentCocktail.PreparationMethod = method;
    }

    public void SetMethodToShake()
    {
        currentCocktail.PreparationMethod = Method.Shaking;
    }

    public void SetMethodToMixing()
    {
        currentCocktail.PreparationMethod = Method.Mixing;
    }


    public void TryToAddAlcohol(Alcohol alcohol, int amount = 1) {
        currentCocktail.TryToAddAlcohol(alcohol,amount);
    }

    public void TryToAddMixer(Mixer mixer, int amount = 1) { 
        currentCocktail.TryToAddMixer(mixer, amount);
    }
}