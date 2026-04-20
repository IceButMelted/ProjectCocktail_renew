using AYellowpaper.SerializedCollections;
using JetBrains.Annotations;
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

public class CocktailShaker : BTN_2_5D
{
    public Texture2D ShakerSprite;

    [Header("Add Ingredient")]
    public UnityEvent OnAddIngredient;
    public AlcoholEvent OnAddAlcohol;  // New typed event
    public MixerEvent OnAddMixer;      // New typed event
    public UnityEvent OnResetCocktail;
    
    public S_Drink currentCocktail;
    private bool canClick = true;

    [Header("UI")]
    public bool canShowMethodUI = true;
    public bool canShowServeUI = false;

    public ToggleActive MethodUI;
    //public ToggleActive VisualUI;
    public ToggleActive ServeUI;

    [Header("List of Ingredient BTN")]
    public List<IngredientButton> ingredientButtons = new List<IngredientButton>();

    public void SetCanShowServeUI(bool active) {
        canShowServeUI = active;
    }

    public void SetCanShowMethodUI(bool active) {
        canShowMethodUI = active;
    }
    

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

    public void SetCanClick(bool active) {
        canClick = active;
    }

    public void SetIceAddIce() {
        currentCocktail.AddIce = true;
    }

    public void TryToAddAlcohol(Alcohol alcohol, int amount = 1) {
        currentCocktail.TryToAddAlcohol(alcohol,amount);
    }

    public void TryToAddMixer(Mixer mixer, int amount = 1) { 
        currentCocktail.TryToAddMixer(mixer, amount);
    }

    public void SetActiveServe(bool active) { 
        ServeUI.gameObject.SetActive(active);
    }

    public void ResetShaker() {
        currentCocktail.Name = "";
        currentCocktail.AlcoholStrength = TypeOfCocktail.None;
        currentCocktail.PreparationMethod = Method.None;
        currentCocktail.AddIce = false;

        currentCocktail.AlcoholList = new SerializedDictionary<Alcohol, int>();
        currentCocktail.MixerList = new SerializedDictionary<Mixer, int>();

        currentCocktail.CompatibleGlasses = new List<GlassType>();

        SetCanShowMethodUI(true);
        SetCanShowServeUI(false);

        SetBTNSprite(ShakerSprite,ShakerSprite,ShakerSprite);

        SetCanClick(true);

        
    }

    protected override void OnClick(InputAction.CallbackContext context)
    {
        if (!(currentCocktail.GetTotalIngredient() > 0))
            return;
        if (!canClick) return;

        base.OnClick(context);
    }

    public void toggleUI() {
        if (canShowMethodUI)
            MethodUI.ToggleAtiveGameObject();
        if (canShowServeUI)
            ServeUI.ToggleAtiveGameObject();
    }

    public void ToggleCanClickIngredientBTN(bool active) {
        for (int i = 0; i < ingredientButtons.Count; i++) {
            ingredientButtons[i].enabled = active;
        }
    }

}