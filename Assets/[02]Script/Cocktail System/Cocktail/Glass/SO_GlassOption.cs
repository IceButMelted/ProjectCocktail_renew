// ============================================================
//  SO_GlassOption.cs — One glass the player can drag from the shelf.
//
//  Replaces SO_GlassVisualTable for the new player-chosen-glass system:
//  a flat list of options instead of a dictionary keyed by GlassType,
//  since garnish look now has to travel with the choice too and there
//  is no longer any recipe-driven key to look entries up by.
// ============================================================

using UnityEngine;
using static E_Cocktail;

[CreateAssetMenu(fileName = "GlassOption_New", menuName = "Bar410/Cocktails/Glass Option")]
public class SO_GlassOption : ScriptableObject
{
    [Header("Identity")]
    public string DisplayName;

    [Tooltip("Cosmetic shape label only, reused from GlassType — no longer tied to any recipe.")]
    public GlassType Shape;

    public GarnishLook Garnish;

    [Header("Visuals")]
    public Sprite GlassSprite;
    public Sprite WaterSprite;
    public Sprite IceSprite;

    [Header("Placement")]
    [Tooltip("Instantiated on the shelf and dragged onto the table. Must carry PlacedGlassInstance + DragableObject + Collider.")]
    public GameObject PlacedPrefab;
}
