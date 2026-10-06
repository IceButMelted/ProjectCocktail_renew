// ============================================================
//  SO_GarnishItemOption.cs — One pickable Fresh or Novelty garnish
//  (e.g. a lime wedge, a mint sprig, a cocktail umbrella).
//
//  Goes into either of a placed glass's 2 garnish slots — Fresh and
//  Novelty share the same 2 slots, Category is UI-filtering metadata
//  only, not a slot restriction. Mirrors SO_GlassOption's shape
//  (catalog asset + sprite) for the same reason glasses are
//  data-driven: content adds new garnishes without touching code.
// ============================================================

using UnityEngine;

[CreateAssetMenu(fileName = "GarnishItem_New", menuName = "Bar410/Cocktails/Garnish Item Option")]
public class SO_GarnishItemOption : ScriptableObject
{
    [Header("Identity")]
    public string DisplayName;
    public GarnishCategory Category;

    [Header("Visuals")]
    public Sprite Sprite;
}
