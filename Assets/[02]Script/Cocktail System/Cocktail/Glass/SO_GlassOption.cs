// ============================================================
//  SO_GlassOption.cs — One glass the player can drag from the shelf.
//
//  Replaces SO_GlassVisualTable: a flat list instead of a dictionary
//  keyed by GlassType, since there's no longer a recipe-driven key to
//  look entries up by. Garnish is a separate player choice made during
//  the Garnish step (see SO_GarnishItemOption/SO_GarnishRimOption) —
//  not baked into the glass itself.
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

    [Header("Visuals")]
    public Sprite GlassSprite;
    public Sprite WaterSprite;
    public Sprite IceSprite;

    [Header("Placement")]
    [Tooltip("Instantiated on the shelf and dragged onto the table. Must carry PlacedGlassInstance + DragableObject + Collider.")]
    public GameObject PlacedPrefab;
}
