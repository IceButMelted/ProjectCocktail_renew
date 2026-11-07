// ============================================================
//  SO_UnmatchedDrinkColor.cs — The colour a drink takes when it
//  matches no recipe (GDD §21.1 "Fail b").
//
//  Stand-in until per-ingredient colours exist (plan S8): once they
//  do, DrinkColorBlender blends those instead and this asset goes.
// ============================================================

using UnityEngine;

[CreateAssetMenu(fileName = "UnmatchedDrinkColor", menuName = "Bar410/Cocktails/Unmatched Drink Color")]
public class SO_UnmatchedDrinkColor : ScriptableObject
{
    [Tooltip("Top of the liquid for a drink that matches no recipe.")]
    public Color Top = DrinkColorBlender.UnmatchedTop;

    [Tooltip("Bottom of the liquid for a drink that matches no recipe.")]
    public Color Bottom = DrinkColorBlender.UnmatchedBottom;
}
