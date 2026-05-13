// ============================================================
//  SO_Cocktails — ScriptableObject for a single cocktail recipe
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

[CreateAssetMenu(fileName = "SO_Cocktail", menuName = "Scriptable Objects/SO_Cocktail")]
public class SO_Cocktail : ScriptableObject
{
    // ── Inspector ──────────────────────────────────────────

    public GameObject CocktailGameObject;
    public Texture2D CocktailSprite;

    public string Name;
    [TextArea(3, 10)]
    public string Description;
    public TypeOfCocktail AlcoholStrength;
    public Method PreparationMethod;
    public bool AddIce;
    public float Price;

    [Header("Ingredients")]
    public List<AlcoholIngredient> AlcoholList = new List<AlcoholIngredient>();
    public List<LiqueurIngredient> LiqueurList = new List<LiqueurIngredient>();
    public List<MixerIngredient> MixerList = new List<MixerIngredient>();

    public List<GlassType> CompatibleGlasses = new List<GlassType>();

    // ── Runtime Data ───────────────────────────────────────

    [HideInInspector]
    public S_Drink CocktailInfos;

    // ── Sync ───────────────────────────────────────────────

    private void OnValidate() => Sync();

    [ContextMenu("Sync To CocktailInfos")]
    public void Sync()
    {
        CocktailInfos ??= new S_Drink();

        CocktailInfos.Name = Name;
        CocktailInfos.Description = Description;
        CocktailInfos.PreparationMethod = PreparationMethod;
        CocktailInfos.AddIce = AddIce;
        CocktailInfos.Price = Price;
        CocktailInfos.CocktailSprite = CocktailSprite;

        // Structs are value types — a new List copy gives independent data.
        CocktailInfos.AlcoholList = new List<AlcoholIngredient>(AlcoholList);
        CocktailInfos.LiqueurList = new List<LiqueurIngredient>(LiqueurList);
        CocktailInfos.MixerList = new List<MixerIngredient>(MixerList);
        CocktailInfos.CompatibleGlasses = CompatibleGlasses != null
            ? new List<GlassType>(CompatibleGlasses)
            : new List<GlassType>();

        CocktailInfos.UpdateTypeOfAlcohol(); // derive AlcoholStrength from actual parts

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}