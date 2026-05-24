using UnityEngine;

public class E_Cocktail
{
    /// <summary>
    /// List of alcohol types.
    /// </summary>
    public enum BaseSpirit
    {
        None = 0,
        Vodka,
        Gin,
        Whiskey,
        Rum,
        Tequila
    }

    public enum Liqueur
    {
        None = 0,
        Triplesec,
        DryVermouth,
        SweetVermouth,
        Campari
    }

    /// <summary>
    /// List of mixer types.
    /// </summary>
    public enum Mixer
    {   None = 0,
        Soda,
        CanberryJuice,
        LimeJuice,
        LemonJuice,
        GrapefruitJuice,
        Syrup,
        PepperMint,
        OrangeJuice
    }

    public enum Direction : byte
    {
        None,
        Left,
        Right,
        Up,
        Down
    }   

    /// <summary>
    /// List of glass types used for serving cocktails.
    /// </summary>
    public enum GlassType : byte
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
        None = 0,
        Shaking,
        Stirring,
    }

    public enum TypeOfCocktail
    {
        None,
        HighAlcohol,
        LowAlcohol,
        NoneAlcohol,
        NotMatch
    }

    public enum Satisfaction
    {
        None,
        Fail,        
        Acceptable,  
        Perfect      
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
