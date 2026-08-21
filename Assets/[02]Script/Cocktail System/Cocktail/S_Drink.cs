// ============================================================
//  S_Drink.cs — ScriptableObject, pure data container.
//  Replaces SO_Cocktail; no methods live here.
//
//  SOLID — S (Single Responsibility):
//    S_Drink owns one concern: holding the authoritative data
//    for a cocktail recipe or a runtime drink instance.
//    All operations live in UtilityDrink.cs.
//
//  SOLID — O (Open / Closed):
//    Adding new ingredient categories only requires a new list
//    field and a matching entry in UtilityDrink — this class
//    never needs modification for behavioural extensions.
//
//  SOLID — L (Liskov Substitution):
//    Inherits ScriptableObject cleanly; no override surprises.
//    A runtime instance (CreateInstance<S_Drink>) is fully
//    substitutable wherever a recipe reference is expected.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

[CreateAssetMenu(fileName = "Drink_New", menuName = "Bar410/Cocktails/Drink")]
public class S_Drink : ScriptableObject
{
    // ── Identity ───────────────────────────────────────────

    /// <summary>Display name of this cocktail.</summary>
    public string Name;

    [TextArea(3, 10)]
    public string Description;

    /// <summary>
    /// Alcohol classification.
    /// Set manually in the Inspector for recipe assets.
    /// Written at runtime by DrinkBuilder.ApplyRecipeIdentity()
    /// for the shaker's live instance.
    /// </summary>
    public TypeOfCocktail AlcoholStrength;

    // ── Preparation ────────────────────────────────────────

    public Method PreparationMethod;
    public bool AddIce;

    // ── Ingredients ────────────────────────────────────────

    [Header("Ingredients")]
    public List<AlcoholIngredient> AlcoholList = new List<AlcoholIngredient>();
    public List<LiqueurIngredient> LiqueurList = new List<LiqueurIngredient>();
    public List<MixerIngredient> MixerList = new List<MixerIngredient>();

    [Header("Cocktail Color")]
    public Color waterColorTop;
    public Color waterColorBottom;

    // ── Serving ────────────────────────────────────────────

    // ── Visuals ────────────────────────────────────────────

    /// <summary>Sprite shown in the UI when this drink is in play.</summary>
    public Sprite CocktailSprite;

    /// <summary>3-D game object spawned when this drink is served.</summary>
    public GameObject CocktailGameObject;

    public float Price;
}