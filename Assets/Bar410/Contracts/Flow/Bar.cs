using System;
using static E_Cocktail;

namespace Bar410.Contracts
{
    [Serializable]
    public struct ScoreResult
    {
        public CharacterName customer; public Satisfaction result; public TypeOfCocktail servedType;
        public float payout; public float relationshipDelta; public string recipeName; public bool isValid;
    }

    /// <summary>impl: Host CocktailFeature.</summary>
    public interface IBarCounter
    {
        bool HasRoom { get; }
        bool IsEmpty { get; }
        Enum_MiniGameType RequiredMinigame { get; }
        event Action ShakerFull;
        event Action ShakerCleared;
        event Action<Enum_MiniGameType> MethodChosen;
        event Action ResetChosen;
        void BeginDrink();
        void OpenIngredients();
        void CloseIngredients();
        void LockIngredients();
        void RefreshUnlocked();
        S_Drink ResolveDrink();
        void SetIce(bool on);
        void SetShakerGrabbable(bool on);
        void ResetDrink();
        void SetOrderNoteAvailable(bool on);
        void CloseBook();
    }

    /// <summary>impl: Host CocktailFeature.</summary>
    public interface IOrderDesk
    {
        DrinkOrderContext Order { get; }
        string PlaceNamed(string recipeName, bool byFlavor);
        string PlaceByPreference(CharacterName customer, bool byFlavor);
        bool PlaceByType(TypeOfCocktail type);
        void SetCustomer(CharacterName customer);
        void ClearOrder();
        bool EnsureOrder();
        ScoreResult CommitServe();
        void UseRecipeFilter(Func<S_Drink, bool> canMake);
        void UsePreferences(ICustomerPreferences prefs);
        event Action<ScoreResult> Scored;
        event Action Cleared;
    }
}
