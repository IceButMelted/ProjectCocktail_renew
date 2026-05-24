// ============================================================
//  IDrinkInterfaces.cs — Abstractions for the cocktail system.
//
//  SOLID — I (Interface Segregation):
//    Two focused interfaces instead of one fat one.
//    Consumers only depend on the slice they actually need.
//
//  SOLID — D (Dependency Inversion):
//    High-level modules (CocktailSystemManager) depend on these
//    abstractions, not on concrete types like SO_CocktailList.
//    Swap implementations freely without touching consumers.
//
//  SOLID — O (Open / Closed):
//    New data sources (e.g. server-fetched recipes) implement
//    IDrinkRepository without modifying any existing class.
// ============================================================

using System.Collections.Generic;
using static E_Cocktail;

// ── Repository ─────────────────────────────────────────────

/// <summary>
/// Abstracts the source of cocktail recipes.
/// Implement on any ScriptableObject, service, or mock.
/// </summary>
public interface IDrinkRepository
{
    /// <summary>All drinks available in this repository.</summary>
    IReadOnlyList<S_Drink> GetDrinks();

    /// <summary>Returns a uniformly random drink from the repository.</summary>
    S_Drink GetRandom();

    /// <summary>
    /// Returns a uniformly random drink that matches <paramref name="type"/>.
    /// Falls back to a completely random drink if none match.
    /// </summary>
    S_Drink GetRandom(TypeOfCocktail type);
}

// ── Ingredient Receiver ────────────────────────────────────

/// <summary>
/// Anything that can accept cocktail ingredients.
/// Isolates ingredient-addition concerns from shaker UI / lifecycle.
/// </summary>
public interface IIngredientReceiver
{
    void TryToAddAlcohol(BaseSpirit alcohol, int amount);
    void TryToAddLiqueur(Liqueur liqueur, int amount);
    void TryToAddMixer(Mixer mixer,  int amount);
}
