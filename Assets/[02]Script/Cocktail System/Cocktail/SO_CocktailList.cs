// ============================================================
//  SO_CocktailList.cs — ScriptableObject recipe collection.
//
//  SOLID — D (Dependency Inversion):
//    Implements IDrinkRepository so CocktailSystemManager never
//    references this concrete type directly.  Swap for any other
//    source (e.g. JSON loader, server) without touching consumers.
//
//  SOLID — O (Open / Closed):
//    New filtering strategies (e.g. GetByGlass) are added as
//    new interface methods + implementations here — existing
//    callers are unaffected.
//
//  SOLID — S (Single Responsibility):
//    Stores and serves recipe data.  No game logic.
// ============================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static E_Cocktail;

[CreateAssetMenu(fileName = "CocktailList_New", menuName = "Scriptable Objects/SO_CocktailList")]
public class SO_CocktailList : ScriptableObject, IDrinkRepository
{
    [Tooltip("Assign S_Drink ScriptableObject assets here.")]
    public List<S_Drink> cocktails = new List<S_Drink>();

    // ── IDrinkRepository ───────────────────────────────────

    /// <inheritdoc/>
    public IReadOnlyList<S_Drink> GetDrinks() => cocktails;

    /// <inheritdoc/>
    public S_Drink GetRandom()
    {
        if (cocktails == null || cocktails.Count == 0)
        {
            Debug.LogWarning("[SO_CocktailList] GetRandom called on empty list.");
            return null;
        }
        return cocktails[Random.Range(0, cocktails.Count)];
    }

    /// <inheritdoc/>
    public S_Drink GetRandom(TypeOfCocktail type)
    {
        var matches = cocktails
            .Where(d => DrinkUtility.GetTypeOfAlcohol(d) == type)
            .ToList();

        if (matches.Count == 0)
        {
            Debug.LogWarning($"[SO_CocktailList] No cocktails of type {type}. Falling back to random.");
            return GetRandom();
        }

        return matches[Random.Range(0, matches.Count)];
    }
}