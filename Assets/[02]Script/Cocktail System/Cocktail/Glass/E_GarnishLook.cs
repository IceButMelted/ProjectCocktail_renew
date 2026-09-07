// ============================================================
//  E_GarnishLook.cs — Which garnish look a placed glass carries.
//
//  Placeholder member list. Picking a glass option (SO_GlassOption)
//  sets both shape sprite and this look together — the actual set
//  of looks and their visuals is a content/design decision, not
//  something this refactor invents.
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
