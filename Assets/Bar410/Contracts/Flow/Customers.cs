using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Contracts
{
    public enum StageTiming { Normal, Instant }

    [Serializable]
    public struct CharacterState { public CharacterName name; public bool present; public int spot; public int expression; public Facing look; }

    /// <summary>impl: Host CustomerStage.</summary>
    public interface ICustomerStage
    {
        bool TryParse(string raw, out CharacterName who);
        bool Exists(CharacterName who);
        bool IsPresent(CharacterName who);
        Transform Anchor(CharacterName who);
        Awaitable EnterAsync(CharacterName who, int spot, StageTiming timing, CancellationToken ct);
        Awaitable ExitAsync(CharacterName who, StageTiming timing, CancellationToken ct);
        bool SetExpression(CharacterName who, string idOrIndex);
        void SetLook(CharacterName who, Facing look);
        void SetSpeaking(CharacterName who, bool speaking);
    }

    [Serializable]
    public struct UnlockNotice { public string key; public string displayName; public Sprite icon; }

    /// <summary>impl: Host UnlocksAdapter.</summary>
    public interface IIngredientUnlocks
    {
        bool IsUnlocked(string ingredientKey);
        bool IsRecipeComplete(S_Drink drink);
        void Unlock(string ingredientKey);
        IReadOnlyList<UnlockNotice> TakeNewUnlocks();
        event Action Changed;
    }
}
