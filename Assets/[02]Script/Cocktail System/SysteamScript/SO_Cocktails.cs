// ============================================================
//  SO_Cocktails — ScriptableObject for a single cocktail recipe
// ============================================================

using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

[CreateAssetMenu(fileName = "SO_Cocktail", menuName = "Scriptable Objects/SO_Cocktail")]
public class SO_Cocktails : ScriptableObject
{
    // ── Inspector ──────────────────────────────────────────

    public GameObject CocktailGameObject;
    public Texture2D CocktailSprite;

    public string Name;
    public TypeOfCocktail AlcoholStrength;
    public Method PreparationMethod;
    public bool AddIce;
    public float Price;

    [Header("Ingredients")]
    [SerializedDictionary("Alcohol", "Amount")]
    public SerializedDictionary<Alcohol, int> AlcoholList;

    [SerializedDictionary("Mixer", "Amount")]
    public SerializedDictionary<Mixer, int> MixerList;

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
        CocktailInfos.PreparationMethod = PreparationMethod;
        CocktailInfos.AddIce = AddIce;
        CocktailInfos.Price = Price;
        CocktailInfos.CocktailSprite = CocktailSprite;

        CocktailInfos.AlcoholList = Copy(AlcoholList);
        CocktailInfos.MixerList = Copy(MixerList);
        CocktailInfos.CompatibleGlasses = CompatibleGlasses != null
            ? new List<GlassType>(CompatibleGlasses)
            : new List<GlassType>();

        CocktailInfos.UpdateTypeOfAlcohol(); // derive AlcoholStrength from actual parts

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // ── Helper ─────────────────────────────────────────────

    private static SerializedDictionary<TKey, TVal> Copy<TKey, TVal>(
        SerializedDictionary<TKey, TVal> source)
    {
        var dict = new SerializedDictionary<TKey, TVal>();
        if (source != null)
            foreach (var kv in source)
                dict[kv.Key] = kv.Value;
        return dict;
    }
}