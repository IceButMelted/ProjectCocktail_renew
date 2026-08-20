// ============================================================
//  ShakerContents.cs — What is in the glass, and nothing else.
//
//  One of five components CocktailShakerData used to be (plan §4.2).
//  No UI, no sprites, no button roster, no tooltip: those live in
//  ShakerVisualPresenter, ShakerPanelController, IngredientButtonGroup
//  and ShakerTooltip.
//
//  The live drink is a ScriptableObject.CreateInstance — an in-memory
//  object that never writes back to a recipe asset on disk.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

public class ShakerContents : MonoBehaviour, IIngredientReceiver
{
    /// <summary>Live drink in the shaker. Fresh instance, never an asset reference.</summary>
    public S_Drink CurrentCocktail { get; private set; }

    /// <summary>Most recent comparison against the recipe database.</summary>
    public RecipeMatch LastMatch { get; private set; } = RecipeMatch.None;

    /// <summary>Raised after anything about the drink changes — ingredient, method or ice.</summary>
    public event Action Changed;

    /// <summary>Raised after the drink is emptied.</summary>
    public event Action Cleared;

    /// <summary>Raised after <see cref="UpdateIdentity"/> resolves a new match.</summary>
    public event Action<RecipeMatch> IdentityResolved;

    // ── Unity Lifecycle ────────────────────────────────────

    private void Awake()
    {
        CurrentCocktail = ScriptableObject.CreateInstance<S_Drink>();
        DrinkBuilder.Clear(CurrentCocktail);
    }

    private void OnDestroy()
    {
        // Runtime ScriptableObject instances must be explicitly destroyed.
        if (CurrentCocktail != null) Destroy(CurrentCocktail);
    }

    // ── Preparation ────────────────────────────────────────

    public void SetMethod(Method method)
    {
        CurrentCocktail.PreparationMethod = method;
        Changed?.Invoke();
    }

    public void SetMethodToShake() => SetMethod(Method.Shaking);
    public void SetMethodToMixing() => SetMethod(Method.Stirring);

    public void ToggleIce() => SetIce(!CurrentCocktail.AddIce);

    public void ToggleIce(bool enable) => SetIce(enable);

    public void SetIce(bool enable)
    {
        CurrentCocktail.AddIce = enable;
        Changed?.Invoke();
    }

    /// <summary>
    /// Which minigame the drink's method calls for (GDD §10).
    /// MinigameFlowBridge reads this as its second-priority type source
    /// (Bar410_Minigame_Integration_Plan §3.1).
    /// </summary>
    public Enum_MiniGameType RequiredMinigame
    {
        get
        {
            switch (CurrentCocktail.PreparationMethod)
            {
                case Method.Shaking: return Enum_MiniGameType.Shaking;
                case Method.Stirring: return Enum_MiniGameType.Stiring;
                default: return Enum_MiniGameType.None;
            }
        }
    }

    // ── IIngredientReceiver ────────────────────────────────

    public void TryToAddAlcohol(BaseSpirit alcohol, int amount = 1)
    {
        if (!DrinkBuilder.TryAddAlcohol(CurrentCocktail, alcohol, amount)) return;
        Changed?.Invoke();
    }

    public void TryToAddLiqueur(Liqueur liqueur, int amount = 1)
    {
        if (!DrinkBuilder.TryAddLiqueur(CurrentCocktail, liqueur, amount)) return;
        Changed?.Invoke();
    }

    public void TryToAddMixer(Mixer mixer, int amount = 1)
    {
        if (!DrinkBuilder.TryAddMixer(CurrentCocktail, mixer, amount)) return;
        Changed?.Invoke();
    }

    // ── Identity ───────────────────────────────────────────

    /// <summary>
    /// Resolves what the drink currently is against the recipe database — one scan,
    /// one identity (name, price, strength, glass, colour).
    /// </summary>
    public RecipeMatch UpdateIdentity(IReadOnlyList<S_Drink> recipes)
    {
        LastMatch = DrinkDeviation.FindBestMatch(CurrentCocktail, recipes);
        DrinkBuilder.ApplyRecipeIdentity(CurrentCocktail, LastMatch);

        IdentityResolved?.Invoke(LastMatch);
        return LastMatch;
    }

    // ── Reset ──────────────────────────────────────────────

    /// <summary>Empties the glass. No UI side effects — listeners decide what that means.</summary>
    public void Clear()
    {
        DrinkBuilder.Clear(CurrentCocktail);
        LastMatch = RecipeMatch.None;

        Cleared?.Invoke();
        Changed?.Invoke();
    }

    // ── Queries ────────────────────────────────────────────

    public int TotalParts => DrinkQuery.GetTotalIngredient(CurrentCocktail);
    public bool HasRoom => DrinkQuery.HasRoom(CurrentCocktail);
    public bool IsEmpty => TotalParts == 0;
}
