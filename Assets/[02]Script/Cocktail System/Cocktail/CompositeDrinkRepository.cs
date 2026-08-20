// ============================================================
//  CompositeDrinkRepository.cs — plan §4.7 (decision D1).
//
//  Presents several IDrinkRepository sources as one. Written now
//  so the seam exists before there is special-cocktail content:
//  Specia_Cocktail.asset is currently empty and the field feeding
//  it is null in every scene, so behaviour is unchanged today.
//
//  Also removes plan bug B11 — SystemGame.prefab has a null
//  repository reference, which used to throw NullReferenceException
//  from CocktailSystemManager.Start().
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

public class CompositeDrinkRepository : IDrinkRepository
{
    private readonly List<S_Drink> _union = new List<S_Drink>();

    /// <summary>Null sources are skipped rather than throwing (bug B11).</summary>
    public CompositeDrinkRepository(params IDrinkRepository[] sources)
    {
        if (sources == null) return;

        foreach (var source in sources)
        {
            // ScriptableObject sources compare against null through the Unity lifetime check.
            if (source == null || (source is Object unityObject && unityObject == null)) continue;

            var drinks = source.GetDrinks();
            if (drinks == null) continue;

            foreach (var drink in drinks)
                if (drink != null && !_union.Contains(drink)) _union.Add(drink);
        }
    }

    public bool IsEmpty => _union.Count == 0;

    public IReadOnlyList<S_Drink> GetDrinks() => _union;

    /// <summary>
    /// Uniform across the WHOLE union.
    ///
    /// Do not "pick a source, then pick a drink from it" — that is the same distribution
    /// bug as plan S10. With 26 normal and 2 special recipes, picking a source first would
    /// give the specials a 50% chance instead of 2/28.
    /// </summary>
    public S_Drink GetRandom()
    {
        if (IsEmpty)
        {
            Debug.LogWarning("[CompositeDrinkRepository] GetRandom called with no recipes loaded.");
            return null;
        }

        return _union[Random.Range(0, _union.Count)];
    }

    /// <inheritdoc/>
    public S_Drink GetRandom(TypeOfCocktail type)
    {
        var matches = new List<S_Drink>();
        foreach (var drink in _union)
            if (AlcoholClassifier.Resolve(drink) == type) matches.Add(drink);

        if (matches.Count == 0)
        {
            Debug.LogWarning($"[CompositeDrinkRepository] No recipes of type {type}. Falling back to random.");
            return GetRandom();
        }

        return matches[Random.Range(0, matches.Count)];
    }

    /// <inheritdoc/>
    public bool TryGetByName(string name, out S_Drink drink)
    {
        drink = null;
        if (string.IsNullOrEmpty(name)) return false;

        foreach (var candidate in _union)
        {
            if (candidate == null) continue;
            if (!string.Equals(candidate.Name, name, System.StringComparison.OrdinalIgnoreCase)) continue;

            drink = candidate;
            return true;
        }

        return false;
    }
}
