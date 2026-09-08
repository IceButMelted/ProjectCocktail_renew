// ============================================================
//  ICustomerPreferences.cs — GDD §7 / §19.1.
//  What drink types a customer likes.
//
//  Lets OrderService be tested without a scene: CharacterData
//  (legacy MonoBehaviour) and SO_CustomerRoster (preferred SO)
//  both implement it.
// ============================================================

using System.Collections.Generic;
using static E_Cocktail;

public interface ICustomerPreferences
{
    /// <summary>
    /// Preferred drink types for <paramref name="customer"/>.
    /// False if unknown or unauthored — caller then falls back to every alcoholic/non-alcoholic type.
    /// </summary>
    bool TryGetPreferredTypes(NPC_Name customer, out IReadOnlyList<TypeOfCocktail> types);
}
