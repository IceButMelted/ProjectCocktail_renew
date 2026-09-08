// ============================================================
//  E_Cocktail.Minigame.cs — part of E_Cocktail.
//  Which minigame a step runs.
//  See E_Cocktail.Drink.cs for why this is a partial class.
// ============================================================

public partial class E_Cocktail
{
    /// <summary>
    /// Registered minigames.
    ///
    /// "Stiring" is the enum's spelling (one r) and matches Method.Stirring's intent;
    /// Building has no implementation yet, so StartMinigame(Building) only warns
    /// (Bar410_Minigame_Integration_Plan §1).
    /// </summary>
    public enum Enum_MiniGameType
    {
        None,
        Shaking,
        Stiring,
        Building
    }
}
