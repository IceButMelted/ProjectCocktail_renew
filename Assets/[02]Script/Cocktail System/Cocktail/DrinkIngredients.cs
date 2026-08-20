// ============================================================
//  DrinkIngredients.cs
//  Pure value-type data for cocktail ingredients.
//
//  SOLID — S (Single Responsibility):
//    This file owns exactly one concern: the shape of an ingredient.
//    No logic, no Unity lifecycle, no dependencies.
//
//  NOTE — deliberate deviation from GDD §15.1 / §16 (plan decision D6):
//    The GDD models ingredients as ONE flat list of IngredientAmount
//    plus an IngredientType -> IngredientCategory lookup. This project
//    keeps THREE typed lists instead, so a Mixer button can never be
//    assigned a spirit and each Inspector dropdown stays short as the
//    ingredient roster grows.
//    The duplication this would normally cause is absorbed by the
//    generic helpers in Domain/IngredientMath.cs — see plan §4.1.1.
// ============================================================

using static E_Cocktail;

/// <summary>
/// Shared shape of one ingredient entry, so <see cref="IngredientMath"/> can work
/// on any category without knowing which one it is.
/// Implemented explicitly on the structs below; the public fields keep their
/// original names so serialized asset/scene data is unaffected.
/// </summary>
public interface IIngredientEntry<TKey> where TKey : struct
{
    TKey Key { get; }
    int Parts { get; }
}

/// <summary>One unit of base spirit in a recipe or shaker.</summary>
[System.Serializable]
public struct AlcoholIngredient : IIngredientEntry<BaseSpirit>
{
    public BaseSpirit Type;
    public int Amount;

    public readonly BaseSpirit Key => Type;
    public readonly int Parts => Amount;

    public static AlcoholIngredient Make(BaseSpirit type, int amount)
        => new AlcoholIngredient { Type = type, Amount = amount };
}

/// <summary>One unit of liqueur in a recipe or shaker.</summary>
[System.Serializable]
public struct LiqueurIngredient : IIngredientEntry<Liqueur>
{
    public Liqueur Type;
    public int Amount;

    public readonly Liqueur Key => Type;
    public readonly int Parts => Amount;

    public static LiqueurIngredient Make(Liqueur type, int amount)
        => new LiqueurIngredient { Type = type, Amount = amount };
}

/// <summary>One unit of mixer in a recipe or shaker.</summary>
[System.Serializable]
public struct MixerIngredient : IIngredientEntry<Mixer>
{
    public Mixer Type;
    public int Amount;

    public readonly Mixer Key => Type;
    public readonly int Parts => Amount;

    public static MixerIngredient Make(Mixer type, int amount)
        => new MixerIngredient { Type = type, Amount = amount };
}
