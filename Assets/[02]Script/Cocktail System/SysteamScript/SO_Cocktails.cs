using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static E_Cocktail;

[CreateAssetMenu(fileName = "SO_CocktailMaker", menuName = "Scriptable Objects/SO_CocktailMaker")]
public class SO_Cocktails : ScriptableObject
{
    public GameObject CocktailGameObject;

    public string Name;
    public E_Cocktail.TypeOfCocktail AlcoholStrength;
    public E_Cocktail.Method PreparationMethod;
    public bool AddIce;
    public float Price;

    [Header("Ingredients")]
    [SerializedDictionary("Alcohol", "Amount")]
    public SerializedDictionary<Alcohol, int> AlcoholList;
    [SerializedDictionary("Mixer", "Amount")]
    public SerializedDictionary<Mixer, int> MixerList;
    [SerializeField]
    public List<GlassType> CompatibleGlasses = new List<GlassType>();

    [HideInInspector]
    public S_Drink CocktailInfos;

    private void OnValidate()
    {
        SyncToCocktailInfos();
    }

    [ContextMenu("Sync To CocktailInfos")]  // lets you trigger manually via right-click too
    public void SyncToCocktailInfos()
    {
        if (CocktailInfos == null)
            CocktailInfos = new S_Drink();

        CocktailInfos.Name = Name;
        CocktailInfos.AlcoholStrength = AlcoholStrength;
        CocktailInfos.PreparationMethod = PreparationMethod;
        CocktailInfos.AddIce = AddIce;
        CocktailInfos.Price = Price;

        // Sync AlcoholList
        CocktailInfos.AlcoholList = new SerializedDictionary<Alcohol, int>();
        if (AlcoholList != null)
            foreach (var kv in AlcoholList)
                CocktailInfos.AlcoholList[kv.Key] = kv.Value;

        // Sync MixerList
        CocktailInfos.MixerList = new SerializedDictionary<Mixer, int>();
        if (MixerList != null)
            foreach (var kv in MixerList)
                CocktailInfos.MixerList[kv.Key] = kv.Value;

        // Sync CompatibleGlasses
        CocktailInfos.CompatibleGlasses = CompatibleGlasses != null
            ? new List<GlassType>(CompatibleGlasses)
            : new List<GlassType>();

        // Auto-derive AlcoholStrength on CocktailInfos too
        CocktailInfos.UpdateTypeOfAlcohol();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}