using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

[CreateAssetMenu(fileName = "CustomerRoster", menuName = "Bar410/Customer Roster")]
public class SO_CustomerRoster : ScriptableObject, ICustomerPreferences
{
    public List<SO_Customer> Customers = new List<SO_Customer>();

    /// <inheritdoc/>
    public bool TryGetPreferredTypes(NPC_Name customer, out IReadOnlyList<TypeOfCocktail> types)
    {
        types = null;
        if (Customers == null) return false;

        foreach (var entry in Customers)
        {
            if (entry == null || entry.Id != customer) continue;
            if (entry.PreferredDrinkTypes == null || entry.PreferredDrinkTypes.Count == 0) return false;

            types = entry.PreferredDrinkTypes;
            return true;
        }

        return false;
    }
}