// ============================================================
//  DrinkColorBlender.cs — GDD §21.1 colour resolution.
//
//  Plan fix S8 (PARTIAL — read the TODO below):
//    Old code painted an unmatched drink pure black. GDD §21.1 wants
//    BlendIngredientColors(poured) instead, but that needs a per-
//    ingredient colour table that DOES NOT EXIST in the data model yet.
//    This file implements the seam and the Perfect/Seem_Like half, and
//    falls back to a neutral murky tone rather than inventing content.
// ============================================================

using UnityEngine;

public static class DrinkColorBlender
{
    /// <summary>
    /// Stand-in for an unmatched drink until per-ingredient colours exist. Deliberately
    /// not black: black reads as "rendering broke", this reads as "murky drink".
    /// </summary>
    public static readonly Color UnmatchedTop = new Color(0.35f, 0.29f, 0.22f, 1f);
    public static readonly Color UnmatchedBottom = new Color(0.24f, 0.19f, 0.14f, 1f);

    /// <summary>
    /// GDD §21.1:
    ///   Perfect or Seem_Like -> matched recipe's authored colours
    ///   Fail (b)             -> BlendIngredientColors(poured)
    ///
    /// TODO(design, plan S8): Fail (b) branch can't be finished yet. S_Drink stores
    /// colours per RECIPE and no asset maps ingredient type to colour, so there's nothing
    /// to blend. Add that table (an SO keyed by BaseSpirit/Liqueur/Mixer, or a colour
    /// field on an ingredient definition) and replace the constants below.
    /// </summary>
    public static void Resolve(in RecipeMatch match, S_Drink poured, out Color top, out Color bottom)
    {
        if (match.IsRecognised)
        {
            top = match.Recipe.waterColorTop;
            bottom = match.Recipe.waterColorBottom;
            return;
        }

        top = UnmatchedTop;
        bottom = UnmatchedBottom;
    }
}
