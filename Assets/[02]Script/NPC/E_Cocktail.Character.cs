// ============================================================
//  E_Cocktail.Character.cs — part of E_Cocktail.
//  The cast.
//  See E_Cocktail.Drink.cs for why this is a partial class.
// ============================================================

public partial class E_Cocktail
{
    /// <summary>
    /// The cast. Used as the key for customer preferences (GDD §7) and for Yarn's
    /// per-character relationship variable $rel_&lt;id&gt; (GDD §22).
    /// </summary>
    public enum CharacterName
    {
        // Values are a contract with the .yarn <<enum CharacterName>> (order must match). Append only, never reorder.
        None = 0,
        Cole,
        Owen,
        Walter,
        Faye,
        Isla,
        Claire,
        Eliana,
        Arthur
    }
}
