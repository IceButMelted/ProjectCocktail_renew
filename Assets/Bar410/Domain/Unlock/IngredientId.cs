// ============================================================
//  IngredientId.cs — one ingredient, picked from the real enums.
//
//  A category (Spirit / Liqueur / Mixer) plus the value from that enum.
//  Key is the enum name ("LemonJuice"): the string Yarn, the save and
//  IngredientButtonUI.IngredientKey all use.
//
//  Enums are serialized as ints, so BaseSpirit / Liqueur / Mixer are
//  append-only (never reorder or insert), same contract as recipes.
//  A rename is caught by the compiler; a reorder is not.
//  Plan: Docs/Bar410_IngredientUnlock_Plan.md
// ============================================================

using System;
using Sirenix.OdinInspector;
using static E_Cocktail;

[Serializable, InlineProperty]
public struct IngredientId
{
    public enum Category { None = 0, Spirit, Liqueur, Mixer }

    [HideLabel, HorizontalGroup]
    public Category category;

    [HideLabel, HorizontalGroup, ShowIf("@category == Category.Spirit")]
    public BaseSpirit spirit;

    [HideLabel, HorizontalGroup, ShowIf("@category == Category.Liqueur")]
    public Liqueur liqueur;

    [HideLabel, HorizontalGroup, ShowIf("@category == Category.Mixer")]
    public Mixer mixer;

    /// <summary>Enum name of the chosen value, or null when nothing valid is picked.</summary>
    public string Key => IsValid ? Value.ToString() : null;

    public bool IsValid => category != Category.None && (int)ValueAsInt != 0;

    private object Value => category switch
    {
        Category.Spirit => spirit,
        Category.Liqueur => liqueur,
        Category.Mixer => mixer,
        _ => null,
    };

    private int ValueAsInt => category switch
    {
        Category.Spirit => (int)spirit,
        Category.Liqueur => (int)liqueur,
        Category.Mixer => (int)mixer,
        _ => 0,
    };

    public static IngredientId Of(BaseSpirit v) => new IngredientId { category = Category.Spirit, spirit = v };
    public static IngredientId Of(Liqueur v) => new IngredientId { category = Category.Liqueur, liqueur = v };
    public static IngredientId Of(Mixer v) => new IngredientId { category = Category.Mixer, mixer = v };

    /// <summary>Finds the enum value with this exact name. False when no enum has it.</summary>
    public static bool TryParse(string key, out IngredientId id)
    {
        id = default;
        if (string.IsNullOrEmpty(key)) return false;

        if (Enum.TryParse(key, out BaseSpirit s) && s != BaseSpirit.None && Enum.IsDefined(typeof(BaseSpirit), s)) { id = Of(s); return true; }
        if (Enum.TryParse(key, out Liqueur l) && l != Liqueur.None && Enum.IsDefined(typeof(Liqueur), l)) { id = Of(l); return true; }
        if (Enum.TryParse(key, out Mixer m) && m != Mixer.None && Enum.IsDefined(typeof(Mixer), m)) { id = Of(m); return true; }
        return false;
    }
}
