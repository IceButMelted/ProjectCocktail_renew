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
