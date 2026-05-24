// ============================================================
//  DrinkIngredients.cs
//  Pure value-type data for cocktail ingredients.
//
//  SOLID — S (Single Responsibility):
//    This file owns exactly one concern: the shape of an ingredient.
//    No logic, no Unity lifecycle, no dependencies.
// ============================================================

using static E_Cocktail;

/// <summary>One unit of base spirit in a recipe or shaker.</summary>
[System.Serializable]
public struct AlcoholIngredient
{
    public BaseSpirit Type;
    public int Amount;
}

/// <summary>One unit of liqueur in a recipe or shaker.</summary>
[System.Serializable]
public struct LiqueurIngredient
{
    public Liqueur Type;
    public int Amount;
}

/// <summary>One unit of mixer in a recipe or shaker.</summary>
[System.Serializable]
public struct MixerIngredient
{
    public Mixer Type;
    public int Amount;
}
