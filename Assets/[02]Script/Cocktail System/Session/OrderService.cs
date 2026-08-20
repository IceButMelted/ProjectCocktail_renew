// ============================================================
//  OrderService.cs — GDD §12 / §19. Picks what the customer asks for.
//
//  Two repositories, not one (plan §4.7):
//    lookup     normal + special. Named orders must find a story cocktail.
//    randomPool normal only. A special must never surface in a random order
//               before the story reaches it.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

public class OrderService
{
    private static readonly TypeOfCocktail[] DefaultTypes =
    {
        TypeOfCocktail.HighAlcohol, TypeOfCocktail.LowAlcohol, TypeOfCocktail.NoneAlcohol
    };

    private readonly IDrinkRepository _lookup;
    private readonly IDrinkRepository _randomPool;
    private readonly ICustomerPreferences _preferences;

    public OrderService(IDrinkRepository lookup, IDrinkRepository randomPool, ICustomerPreferences preferences)
    {
        _lookup = lookup;
        _randomPool = randomPool;
        _preferences = preferences;
    }

    // ── GDD §12 modes 1-2 — a named recipe ─────────────────

    public bool OrderByName(DrinkOrderContext context, NPC_Name customer, string recipeName, bool byFlavor)
    {
        if (_lookup == null || !_lookup.TryGetByName(recipeName, out var drink))
        {
            Debug.LogWarning($"[OrderService] No cocktail named '{recipeName}'.");
            return false;
        }

        context.BeginOrder(customer,
                           byFlavor ? OrderMode.FixedByFlavor : OrderMode.FixedByName,
                           drink,
                           AlcoholClassifier.Resolve(drink));
        return true;
    }

    // ── GDD §12 modes 3-4 — random from what they like ─────

    public bool OrderByPreference(DrinkOrderContext context, NPC_Name customer, bool byFlavor)
    {
        var drink = PickForCustomer(customer);
        if (drink == null)
        {
            Debug.LogWarning($"[OrderService] Could not pick a cocktail for {customer}.");
            return false;
        }

        context.BeginOrder(customer,
                           byFlavor ? OrderMode.RandomByPreferenceFlavor : OrderMode.RandomByPreferenceName,
                           drink,
                           AlcoholClassifier.Resolve(drink));
        return true;
    }

    // ── GDD §12 mode 5 — a type, no specific recipe ────────

    public bool OrderByType(DrinkOrderContext context, NPC_Name customer, TypeOfCocktail type)
    {
        if (type == TypeOfCocktail.None || type == TypeOfCocktail.NotMatch)
        {
            Debug.LogWarning($"[OrderService] '{type}' is not a valid ordered type.");
            return false;
        }

        // No Target: any recipe of this type satisfies the order (GDD §12 mode 5),
        // so satisfaction is decided purely by ServedType == OrderedType.
        context.BeginOrder(customer, OrderMode.FixedByType, null, type);
        return true;
    }

    // ── Selection ──────────────────────────────────────────

    /// <summary>
    /// GDD §19.2 — gather every recipe whose type is in the customer's preferences, THEN
    /// pick uniformly from that pool.
    ///
    /// Plan fix S10: the old code picked a preferred TYPE uniformly first and only then a
    /// recipe within it. With 20 High recipes and 4 None recipes that gave each type an
    /// equal chance instead of each recipe, so a None-alcohol drink was five times more
    /// likely per recipe than the GDD intends.
    ///
    /// GDD §24 keeps weighted selection out of v1 — uniform is the decision, not a stub.
    /// </summary>
    public S_Drink PickForCustomer(NPC_Name customer)
    {
        if (_randomPool == null) return null;

        IReadOnlyList<TypeOfCocktail> preferred = null;
        if (_preferences == null || !_preferences.TryGetPreferredTypes(customer, out preferred) || preferred.Count == 0)
        {
            Debug.LogWarning($"[OrderService] No preferences for {customer}; using every type.");
            preferred = DefaultTypes;
        }

        var candidates = new List<S_Drink>();
        var all = _randomPool.GetDrinks();

        if (all != null)
        {
            foreach (var drink in all)
            {
                if (drink == null) continue;
                if (Contains(preferred, AlcoholClassifier.Resolve(drink))) candidates.Add(drink);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[OrderService] No recipe matches {customer}'s preferences; falling back to random.");
            return _randomPool.GetRandom();
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private static bool Contains(IReadOnlyList<TypeOfCocktail> list, TypeOfCocktail type)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == type) return true;

        return false;
    }
}
