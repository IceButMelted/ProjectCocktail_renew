// ============================================================
//  E_Cocktail.Dialogue.cs — part of E_Cocktail.
//  Dialogue-side classifications.
//  See E_Cocktail.Drink.cs for why this is a partial class.
// ============================================================

public partial class E_Cocktail
{
    public enum TextType
    {
        Normal,
        Complex
    }

    /// <summary>Where a conversation sits relative to the order it produces.</summary>
    public enum ConversationPhase
    {
        None,
        SmallTalkBeforeOrder,
        Ordering,
        AfterServe,
        SmallTalkAfterOrder
    }
}
