// ============================================================
//  E_GarnishLook.cs — Which garnish look a placed glass carries.
//
//  Placeholder member list only. Picking a glass option (SO_GlassOption)
//  sets both its shape sprite and this look in one action — the real
//  set of looks, and what visually changes per value, is a content/
//  design decision, not something this refactor invents.
// ============================================================

public enum GarnishLook : byte
{
    None,
    Lime,
    SaltRim,
    SugarRim,
    Umbrella,
    Olive,
    Twist
}
