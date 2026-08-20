// ============================================================
//  E_Cocktail.Character.cs — part of E_Cocktail.
//  Characters and the directions they can face.
//  See E_Cocktail.Drink.cs for why this is a partial class.
// ============================================================

public partial class E_Cocktail
{
    /// <summary>Which way an NPC sprite is looking.</summary>
    public enum Direction : byte
    {
        None,
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// The cast. Used as the key for customer preferences (GDD §7) and for Yarn's
    /// per-character relationship variable $rel_&lt;id&gt; (GDD §22).
    /// </summary>
    public enum NPC_Name
    {
        None = 0,
        Cole,
        Owen,
        Walter,
        Freya,
        Isla
    }
}
