// ============================================================
//  SO_Cocktails — ScriptableObject for a single cocktail recipe
// ============================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static E_Cocktail;

[CreateAssetMenu(fileName = "SO_Cocktail", menuName = "Scriptable Objects/SO_Cocktail")]
public class SO_Cocktail : ScriptableObject
{
    // ── Inspector ──────────────────────────────────────────

    public GameObject CocktailGameObject;
    public Sprite CocktailSprite;

    public string Name;
    [TextArea(3, 10)]
    public string Description;
    public Method PreparationMethod;
    public TypeOfCocktail TypeOfAlcohol; 
    public bool AddIce;
    public float Price;

    [Header("Ingredients")]
    public List<AlcoholIngredient> AlcoholList = new List<AlcoholIngredient>();
    public List<LiqueurIngredient> LiqueurList = new List<LiqueurIngredient>();
    public List<MixerIngredient> MixerList = new List<MixerIngredient>();

    public List<GlassType> CompatibleGlasses = new List<GlassType>();

    // ── Queries ────────────────────────────────────────────

    /// <summary>
    /// Derives AlcoholStrength directly from the ingredient lists.
    /// Alcohol + Liqueur parts >= 5 = High, > 0 = Low, else None.
    /// </summary>
    public TypeOfCocktail GetTypeOfAlcohol()
    {
        if(TypeOfAlcohol != TypeOfCocktail.None)
        return TypeOfAlcohol; // if already set in inspector, use that value (allows for manual overrides)

        int total = AlcoholList.Sum(a => a.Amount) + LiqueurList.Sum(l => l.Amount);
        if (total >= 5) return TypeOfCocktail.HighAlcohol;
        if (total > 0) return TypeOfCocktail.LowAlcohol;
        return TypeOfCocktail.NoneAlcohol;
    }

    // ── Conversion ─────────────────────────────────────────

    /// <summary>
    /// Builds a fresh <see cref="S_Drink"/> from this SO's fields.
    /// Call this wherever an S_Drink is needed (e.g. recipe lists, target cocktail).
    /// Structs are value types so list copies are fully independent.
    /// </summary>
    public S_Drink ToDrink()
    {
        var drink = new S_Drink
        {
            Name = Name,
            Description = Description,
            PreparationMethod = PreparationMethod,
            AddIce = AddIce,
            Price = Price,
            CocktailSprite = CocktailSprite,
            AlcoholList = new List<AlcoholIngredient>(AlcoholList),
            LiqueurList = new List<LiqueurIngredient>(LiqueurList),
            MixerList = new List<MixerIngredient>(MixerList),
            CompatibleGlasses = CompatibleGlasses != null
                                    ? new List<GlassType>(CompatibleGlasses)
                                    : new List<GlassType>()
        };

        drink.UpdateTypeOfAlcohol(); // derive AlcoholStrength from actual parts
        return drink;
    }
}