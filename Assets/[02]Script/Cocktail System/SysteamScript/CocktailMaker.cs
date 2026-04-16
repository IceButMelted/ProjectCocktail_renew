using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

using static E_Cocktail;

// Define custom UnityEvent types at the top of your file
[System.Serializable]
public class AlcoholEvent : UnityEvent<Alcohol, int> { }

[System.Serializable]
public class MixerEvent : UnityEvent<Mixer, int> { }

public class CocktailMaker : BTN_2_5D
{
    [Header("Add Ingredient")]
    public UnityEvent OnAddIngredient;
    public AlcoholEvent OnAddAlcohol;  // New typed event
    public MixerEvent OnAddMixer;      // New typed event
    public UnityEvent OnResetCocktail;

    [HideInInspector]
    public S_Drink currentCocktail;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();
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

    protected override void OnClick(InputAction.CallbackContext context)
    {
        if (!(currentCocktail.GetTotalIngredient() > 0))
            return;
        base.OnClick(context);
    }
}