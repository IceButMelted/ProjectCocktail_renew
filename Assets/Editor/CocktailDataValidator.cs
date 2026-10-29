#if UNITY_EDITOR
// ============================================================
//  CocktailDataValidator.cs — Bar410/Validate Cocktail Data
//
//  Phase 8 of the refactor plan is authoring work, not code: recipes
//  must total 10 parts (S12). This does not invent that data — it
//  finds and lists exactly what is missing so a designer can fix it.
//
//  CompatibleGlass was removed from S_Drink entirely (player now picks
//  the serving glass at runtime) — this validator no longer checks it.
//
//  Menu: Bar410 > Validate Cocktail Data
// ============================================================

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using static E_Cocktail;

public static class CocktailDataValidator
{
    [MenuItem("Bar410/Validate Cocktail Data")]
    public static void Validate()
    {
        var report = new StringBuilder();
        int problems = 0;

        problems += ValidateRecipes(report);
        problems += ValidateUnlockTable(report);

        string header = problems == 0
            ? "[Bar410] Cocktail data validation passed."
            : $"[Bar410] Cocktail data validation found {problems} problem(s).";

        if (problems == 0) Debug.Log(header);
        else Debug.LogWarning(header + "\n" + report);
    }

    // ── Recipes ────────────────────────────────────────────

    private static int ValidateRecipes(StringBuilder report)
    {
        var guids = AssetDatabase.FindAssets("t:S_Drink");
        var problems = 0;

        var wrongTotal = new List<string>();
        var noName = new List<string>();
        var duplicates = new Dictionary<string, string>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var drink = AssetDatabase.LoadAssetAtPath<S_Drink>(path);
            if (drink == null) continue;

            string label = string.IsNullOrEmpty(drink.Name) ? System.IO.Path.GetFileName(path) : drink.Name;

            // GDD §15 / §16 — every recipe totals exactly 10 parts (plan gap S12).
            int total = DrinkQuery.GetTotalIngredient(drink);
            if (total != DrinkQuery.MaxTotalParts) wrongTotal.Add($"{label} = {total}");

            if (string.IsNullOrEmpty(drink.Name)) noName.Add(path);

            // GDD §16 — no two recipes may share an identical ingredient multiset.
            string signature = Signature(drink);
            if (duplicates.TryGetValue(signature, out var other))
                report.AppendLine($"  DUPLICATE ingredients: '{label}' and '{other}'");
            else
                duplicates[signature] = label;
        }

        report.Insert(0, $"Checked {guids.Length} S_Drink assets.\n");

        problems += Section(report, "Ingredients do not total 10 (GDD §15, gap S12)", wrongTotal);
        problems += Section(report, "Drink has no Name", noName);

        return problems;
    }

    // ── Ingredient unlock table ────────────────────────────

    /// <summary>Names unique and real, and (info) how many recipes are makeable on each unlock day.</summary>
    private static int ValidateUnlockTable(StringBuilder report)
    {
        var tables = AssetDatabase.FindAssets("t:SO_IngredientUnlockTable");
        if (tables.Length == 0) return 0;

        var problems = 0;

        foreach (var guid in tables)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var table = AssetDatabase.LoadAssetAtPath<SO_IngredientUnlockTable>(path);
            if (table == null) continue;

            var seen = new HashSet<string>();
            var bad = new List<string>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var key = table.Rows[i].id.Key;
                if (key == null) bad.Add($"row {i} has no ingredient picked");
                else if (!seen.Add(key)) bad.Add($"'{key}' appears twice");
            }
            problems += Section(report, $"Unlock table {table.name} (errors)", bad);

            // No row = unlocked from day 1, so a forgotten ingredient is easy to miss.
            var missing = new List<string>();
            foreach (var id in table.MissingIds()) missing.Add($"{id.Key} has no row (counts as unlocked from day 1) - use 'Add Missing Ingredients'");
            problems += Section(report, $"Unlock table {table.name} (warnings)", missing);

            // Which recipes could be made on each day that unlocks something.
            var drinks = new List<S_Drink>();
            foreach (var dg in AssetDatabase.FindAssets("t:S_Drink"))
            {
                var drink = AssetDatabase.LoadAssetAtPath<S_Drink>(AssetDatabase.GUIDToAssetPath(dg));
                if (drink != null) drinks.Add(drink);
            }

            var days = new SortedSet<int>();
            foreach (var row in table.Rows) if (row.unlockDay > 0) days.Add(row.unlockDay);

            var line = new StringBuilder($"Makeable recipes per unlock day ({table.name}, {drinks.Count} recipes):");
            foreach (int day in days)
            {
                int count = 0;
                foreach (var drink in drinks)
                    if (AllUnlockedOn(drink, table, day)) count++;
                line.Append($" day {day} = {count};");
            }
            report.AppendLine(line.ToString());
            Debug.Log("[Bar410] " + line);
        }

        return problems;
    }

    private static bool AllUnlockedOn(S_Drink drink, SO_IngredientUnlockTable table, int day)
    {
        foreach (var a in drink.AlcoholList) if (a.Type != BaseSpirit.None && !UnlockedOn(a.Type.ToString(), table, day)) return false;
        foreach (var l in drink.LiqueurList) if (l.Type != Liqueur.None && !UnlockedOn(l.Type.ToString(), table, day)) return false;
        foreach (var m in drink.MixerList) if (m.Type != Mixer.None && !UnlockedOn(m.Type.ToString(), table, day)) return false;
        return true;
    }

    // Same rule as IngredientUnlocks.IsUnlocked, without the story unlocks.
    private static bool UnlockedOn(string ingredient, SO_IngredientUnlockTable table, int day)
        => !table.TryGet(ingredient, out var row) || (row.unlockDay > 0 && day >= row.unlockDay);

    private static string Signature(S_Drink d)
    {
        var sb = new StringBuilder();
        foreach (var a in d.AlcoholList) sb.Append('A').Append((int)a.Type).Append(':').Append(a.Amount).Append('|');
        foreach (var l in d.LiqueurList) sb.Append('L').Append((int)l.Type).Append(':').Append(l.Amount).Append('|');
        foreach (var m in d.MixerList) sb.Append('M').Append((int)m.Type).Append(':').Append(m.Amount).Append('|');
        return sb.ToString();
    }

    // ── Output ─────────────────────────────────────────────

    private static int Section(StringBuilder report, string title, List<string> entries)
    {
        if (entries.Count == 0) return 0;

        report.AppendLine($"\n{title} — {entries.Count}:");
        foreach (var entry in entries) report.AppendLine($"  - {entry}");
        return entries.Count;
    }
}
#endif
