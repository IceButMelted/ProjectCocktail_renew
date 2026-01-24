using UnityEngine;

public class E_Cocktail
{
    /// <summary>
    /// List of alcohol types.
    /// </summary>
    public enum Alcohol
    {
        None = 0,
        Vodka,
        Gin,
        Triplesec,
        Vermouth
    }

    /// <summary>
    /// List of mixer types.
    /// </summary>
    public enum Mixer
    {   None = 0,
        CanberryJuice,
        GrapefruitJuice,
        LemonJuice,
        Soda,
        Syrup,
        PepperMint
    }


    /// <summary>
    /// List of glass types used for serving cocktails.
    /// </summary>
    public enum Glass : byte
    {
        None,
        Hi_ball,
        Rocks,
        Martini,
        Cocktail,
        LongDrink,
        NotFix
    }

    /// <summary>
    /// Methods used to prepare cocktails.
    /// </summary>
    public enum Method : byte
    {
        None,
        Mixing,
        Shaking
    }

    public enum TypeOfCocktail : byte
    {
        None,
        HighAlcohol,
        LowAlcohol,
        NonAlcoholic,
        NotMatch
    }

    public enum CocktaillResualt : byte
    {
        Success = 0,
        Aceptable,
        Fail,
        None
    }

    public enum TextType
    {
        Normal,
        Complex
    }

    public enum ConversationPhase
    {
        None,
        SmallTalkBeforeOrder,
        Ordering,
        AfterServe,
        SmallTalkAfterOrder
    }

    public enum Enum_MiniGameType
    {
        None,
        Shaking,
        Stiring,
        Building
    }

}
