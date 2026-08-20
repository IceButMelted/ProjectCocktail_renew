// ============================================================
//  ICustomerPreferences.cs — GDD §7 / §19.1.
//  What kinds of drink a given customer likes.
//
//  An interface so OrderService can be exercised without a scene:
//  CharacterData (MonoBehaviour, legacy) and SO_CustomerRoster
//  (ScriptableObject, preferred) both satisfy it.
// ============================================================

using System.Collections.Generic;
using static E_Cocktail;

public interface ICustomerPreferences
{
    /// <summary>
    /// Preferred drink types for <paramref name="customer"/>.
    /// Returns false when the customer is unknown or has no preferences authored,
    /// in which case the caller falls back to every alcoholic/non-alcoholic type.
    /// </summary>
    bool TryGetPreferredTypes(NPC_Name customer, out IReadOnlyList<TypeOfCocktail> types);
}
