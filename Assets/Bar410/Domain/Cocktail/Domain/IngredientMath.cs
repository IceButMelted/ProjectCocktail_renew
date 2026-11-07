// ============================================================
//  IngredientMath.cs — Category-agnostic ingredient algorithms.
//
//  Plan §4.1.1 (D6): keeps 3 parallel ingredient lists instead of
//  the flat model in GDD §15.1, so each category keeps its own
//  enum and stays type-safe in the Inspector. Cost paid once here:
//  every algorithm is generic, no category-specific knowledge.
//
//  Adding a 4th category needs NO new algorithm — just a new
//  struct plus one line at each aggregation point marked
//  "เพิ่มหมวดใหม่: แก้ที่นี่".
// ============================================================

using System;
using System.Collections.Generic;

internal static class IngredientMath
{
    /// <summary>Parts of <paramref name="key"/> present in the list, or 0 when absent.</summary>
    public static int QuantityOf<TItem, TKey>(List<TItem> list, TKey key)
        where TItem : struct, IIngredientEntry<TKey>
        where TKey : struct
    {
        if (list == null) return 0;

        var comparer = EqualityComparer<TKey>.Default;
        for (int i = 0; i < list.Count; i++)
            if (comparer.Equals(list[i].Key, key)) return list[i].Parts;

        return 0;
    }

    /// <summary>Sum of every entry's parts.</summary>
    public static int Sum<TItem, TKey>(List<TItem> list)
        where TItem : struct, IIngredientEntry<TKey>
        where TKey : struct
    {
        if (list == null) return 0;

        int total = 0;
        for (int i = 0; i < list.Count; i++) total += list[i].Parts;
        return total;
    }

    /// <summary>True when both lists hold the same keys with the same parts.</summary>
    public static bool ListEquals<TItem, TKey>(List<TItem> a, List<TItem> b)
        where TItem : struct, IIngredientEntry<TKey>
        where TKey : struct
    {
        if (a == null || b == null) return ReferenceEquals(a, b);
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
            if (QuantityOf<TItem, TKey>(b, a[i].Key) != a[i].Parts) return false;

        return true;
    }

    /// <summary>
    /// GDD §17.1 — Σ |recipe − poured| across the union of keys on either side.
    /// One category's contribution; callers sum across categories.
    /// </summary>
    public static int Deviation<TItem, TKey>(List<TItem> poured, List<TItem> recipe)
        where TItem : struct, IIngredientEntry<TKey>
        where TKey : struct
    {
        var keys = new HashSet<TKey>();
        if (poured != null) for (int i = 0; i < poured.Count; i++) keys.Add(poured[i].Key);
        if (recipe != null) for (int i = 0; i < recipe.Count; i++) keys.Add(recipe[i].Key);

        int total = 0;
        foreach (var key in keys)
            total += Math.Abs(QuantityOf<TItem, TKey>(recipe, key) - QuantityOf<TItem, TKey>(poured, key));

        return total;
    }

    /// <summary>Adds parts to an existing entry, or appends a new one.</summary>
    public static void Add<TItem, TKey>(List<TItem> list, TKey key, int amount, Func<TKey, int, TItem> make)
        where TItem : struct, IIngredientEntry<TKey>
        where TKey : struct
    {
        var comparer = EqualityComparer<TKey>.Default;

        for (int i = 0; i < list.Count; i++)
        {
            if (!comparer.Equals(list[i].Key, key)) continue;
            list[i] = make(key, list[i].Parts + amount);
            return;
        }

        list.Add(make(key, amount));
    }
}
