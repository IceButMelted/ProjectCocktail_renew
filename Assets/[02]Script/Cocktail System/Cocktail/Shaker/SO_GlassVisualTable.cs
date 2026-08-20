// ============================================================
//  SO_GlassVisualTable.cs — GlassType -> sprites, in ONE asset.
//  Plan decision D5 (§10.1).
//
//  This table used to be a SerializableDictionary field on
//  CocktailShakerData, which meant one independent copy per scene
//  and per prefab. There were five, and three of them had already
//  drifted: two scenes held identical 4-entry tables, one scene held
//  an EMPTY table, and neither prefab had the field serialized at all.
//  Moving it to an asset makes all of them point at one source.
//
//  Known gap G1: GlassType has 8 values but the tables in the scenes
//  only ever filled 4 (Hi_ball, Martini, Rocks, Magrita). A lookup
//  miss only warns and leaves the previous sprite on screen, so fill
//  every value you actually use — see Bar410_CocktailSystem_Manual_Setup.md.
// ============================================================

using AYellowpaper.SerializedCollections;
using UnityEngine;
using static E_Cocktail;

/// <summary>
/// Sprites for one kind of glass.
///
/// Top-level on purpose: the old CocktailShakerData.VisualCocktailGlass was a nested
/// class, and Unity serializes nested types under the owner's name. Reusing it here would
/// have tied the asset's format to a class that is on its way out.
/// </summary>
[System.Serializable]
public class GlassVisual
{
    public Sprite GlassSprite;
    public Sprite WaterSprite;
    public Sprite IceSprite;
}

[CreateAssetMenu(fileName = "GlassVisualTable", menuName = "Bar410/Cocktails/Glass Visual Table")]
public class SO_GlassVisualTable : ScriptableObject
{
    [SerializedDictionary("Glass Type", "Visual")]
    public SerializedDictionary<GlassType, GlassVisual> Table = new SerializedDictionary<GlassType, GlassVisual>();

    /// <summary>Looks a glass up. Returns false and leaves <paramref name="visual"/> null on a miss.</summary>
    public bool TryGet(GlassType type, out GlassVisual visual)
    {
        visual = null;
        return Table != null && Table.TryGetValue(type, out visual) && visual != null;
    }

    /// <summary>Every GlassType with no entry. Used by the validator menu item.</summary>
    public System.Collections.Generic.List<GlassType> MissingEntries()
    {
        var missing = new System.Collections.Generic.List<GlassType>();

        foreach (GlassType type in System.Enum.GetValues(typeof(GlassType)))
        {
            if (type == GlassType.None) continue;
            if (!TryGet(type, out _)) missing.Add(type);
        }

        return missing;
    }
}
