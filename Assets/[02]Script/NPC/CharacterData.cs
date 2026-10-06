using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

/// <summary>
/// Per-customer drink preferences, authored on a scene object.
///
/// LEGACY. GDD §19.1 wants one ScriptableObject per character — see
/// <see cref="SO_Customer"/> / <see cref="SO_CustomerRoster"/>, which are editable without
/// opening a scene and readable in a git diff. This component stays because the scenes
/// already carry its data; it now implements the same interface, so swapping the two is a
/// matter of changing one reference (Docs/Bar410_CocktailSystem_Manual_Setup.md).
/// </summary>
public class CharacterData : MonoBehaviour, ICustomerPreferences
{
    [SerializedDictionary("Character", "Favorite Drink")]
    public SerializedDictionary<NPC_Name, List<TypeOfCocktail>> NPC_Favorite_Drink = new SerializedDictionary<NPC_Name, List<TypeOfCocktail>>();

    /// <inheritdoc/>
    public bool TryGetPreferredTypes(NPC_Name customer, out IReadOnlyList<TypeOfCocktail> types)
    {
        types = null;

        if (NPC_Favorite_Drink == null) return false;
        if (!NPC_Favorite_Drink.TryGetValue(customer, out var list)) return false;
        if (list == null || list.Count == 0) return false;

        types = list;
        return true;
    }
}
