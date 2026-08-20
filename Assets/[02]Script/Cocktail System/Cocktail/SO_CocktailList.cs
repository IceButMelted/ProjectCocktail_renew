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
using UnityEngine;
using static E_Cocktail;

[CreateAssetMenu(fileName = "CocktailList_New", menuName = "Bar410/Cocktails/CocktailList")]
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
            Debug.LogWarning("[SO_CocktailList] GetRandom called on empty list.", this);
            return null;
        }
        return cocktails[Random.Range(0, cocktails.Count)];
    }

    /// <inheritdoc/>
    public S_Drink GetRandom(TypeOfCocktail type)
    {
        // Built without LINQ so this allocates one list instead of an enumerator chain
        // plus a ToList; it runs whenever a customer places an order.
        var matches = new List<S_Drink>();

        if (cocktails != null)
        {
            foreach (var drink in cocktails)
                if (drink != null && AlcoholClassifier.Resolve(drink) == type) matches.Add(drink);
        }

        if (matches.Count == 0)
        {
            Debug.LogWarning($"[SO_CocktailList] No cocktails of type {type}. Falling back to random.", this);
            return GetRandom();
        }

        return matches[Random.Range(0, matches.Count)];
    }

    /// <inheritdoc/>
    public bool TryGetByName(string name, out S_Drink drink)
    {
        drink = null;
        if (string.IsNullOrEmpty(name) || cocktails == null) return false;

        foreach (var candidate in cocktails)
        {
            if (candidate == null) continue;
            if (!string.Equals(candidate.Name, name, System.StringComparison.OrdinalIgnoreCase)) continue;

            drink = candidate;
            return true;
        }

        return false;
    }
}
