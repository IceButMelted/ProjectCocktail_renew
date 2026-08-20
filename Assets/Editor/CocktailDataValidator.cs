#if UNITY_EDITOR
// ============================================================
//  CocktailDataValidator.cs — Bar410/Validate Cocktail Data
//
//  Phase 8 of the refactor plan is authoring work, not code: filling
//  in CompatibleGlass on 20 recipes (G2) and completing the glass
//  visual table (G1). This does not invent that data — it finds and
//  lists exactly what is missing so a designer can fix it.
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
        problems += ValidateGlassTables(report);

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
        var noGlass = new List<string>();
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

            // Plan gap G2 — 20 assets still carry the old "CompatibleGlasses" key, so this
            // deserializes as None and never matches an entry in the glass visual table.
            if (drink.CompatibleGlass == GlassType.None) noGlass.Add(label);

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
        problems += Section(report, "CompatibleGlass is None (gap G2)", noGlass);
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

    // ── Glass visuals ──────────────────────────────────────

    private static int ValidateGlassTables(StringBuilder report)
    {
        var guids = AssetDatabase.FindAssets("t:SO_GlassVisualTable");
        if (guids.Length == 0)
        {
            report.AppendLine("\nNo SO_GlassVisualTable asset exists yet (plan D5). Every scene is still " +
                              "using its own copy of the table — see Bar410_CocktailSystem_Manual_Setup.md.");
            return 1;
        }

        int problems = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var table = AssetDatabase.LoadAssetAtPath<SO_GlassVisualTable>(path);
            if (table == null) continue;

            var missing = table.MissingEntries();
            if (missing.Count == 0) continue;

            problems++;
            report.AppendLine($"\n{path} has no entry for (gap G1):");
            foreach (var type in missing) report.AppendLine($"  - {type}");
        }

        return problems;
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
