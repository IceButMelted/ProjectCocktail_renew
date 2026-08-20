// ============================================================
//  DrinkFormatter.cs — Human-readable text about a drink.
//  Debug logs and the shaker tooltip. No game rules live here.
// ============================================================

using System.Linq;
using System.Text;

public static class DrinkFormatter
{
    /// <summary>Full multi-line dump. Debug/ContextMenu use.</summary>
    public static string GetCocktailInfo(S_Drink d)
    {
        if (d == null) return "(no drink)";

        var sb = new StringBuilder();
        sb.AppendLine(string.IsNullOrEmpty(d.Name) ? "(unnamed)" : d.Name);
        sb.AppendLine($"Type: {d.AlcoholStrength} | Method: {d.PreparationMethod} | Ice: {d.AddIce} | Price: {d.Price}");
        sb.AppendLine($"Glass: {d.CompatibleGlass} | Parts: {DrinkQuery.GetTotalIngredient(d)}/{DrinkQuery.MaxTotalParts}");
        sb.Append("Alcohol: ").AppendLine(string.Join(", ", d.AlcoholList.Select(a => $"{a.Type}x{a.Amount}")));
        sb.Append("Liqueur: ").AppendLine(string.Join(", ", d.LiqueurList.Select(l => $"{l.Type}x{l.Amount}")));
        sb.Append("Mixer:   ").AppendLine(string.Join(", ", d.MixerList.Select(m => $"{m.Type}x{m.Amount}")));
        sb.Append("Color:   ").AppendLine($"top {d.waterColorTop} / bottom {d.waterColorBottom}");
        return sb.ToString();
    }

    /// <summary>Ingredient list only, one per line. Shown in the shaker tooltip. เพิ่มหมวดใหม่: แก้ที่นี่</summary>
    public static string GetCocktailIngredient(S_Drink d)
    {
        if (d == null) return string.Empty;

        var sb = new StringBuilder();

        if (d.AlcoholList.Count > 0)
            sb.AppendLine(string.Join("\n", d.AlcoholList.Select(a => $"{a.Type} x {a.Amount}")));
        if (d.LiqueurList.Count > 0)
            sb.AppendLine(string.Join("\n", d.LiqueurList.Select(l => $"{l.Type} x {l.Amount}")));
        if (d.MixerList.Count > 0)
            sb.AppendLine(string.Join("\n", d.MixerList.Select(m => $"{m.Type} x {m.Amount}")));

        return sb.ToString();
    }

    /// <summary>Compact one-line summary of a comparison. Debug use.</summary>
    public static string DescribeMatch(in RecipeMatch match)
        => match.HasRecipe
            ? $"{match.Flag} vs '{match.RecipeName}' (deviation {match.Deviation}, method {match.MethodMatch}, ice {match.IceMatch})"
            : "Fail (b) — no recipe within tolerance";
}
