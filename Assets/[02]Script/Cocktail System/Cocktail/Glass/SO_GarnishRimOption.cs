// ============================================================
//  SO_GarnishRimOption.cs — One pickable Rim Garnish (e.g. a salt
//  or sugar rim). A whole-rim treatment, not a slot pick — a placed
//  glass carries at most one of these at a time, separate from its
//  2 Fresh/Novelty garnish slots.
// ============================================================

using UnityEngine;

[CreateAssetMenu(fileName = "GarnishRim_New", menuName = "Bar410/Cocktails/Garnish Rim Option")]
public class SO_GarnishRimOption : ScriptableObject
{
    [Header("Identity")]
    public string DisplayName;

    [Header("Visuals")]
    public Sprite Sprite;
}
