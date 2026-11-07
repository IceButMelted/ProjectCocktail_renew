using System;
using System.Threading;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Contracts
{
    public readonly struct MinigameRequest
    {
        public readonly Enum_MiniGameType Selected, FromDrink;
        public readonly bool ShowCustomer;
        public MinigameRequest(Enum_MiniGameType selected, Enum_MiniGameType fromDrink, bool showCustomer)
        { Selected = selected; FromDrink = fromDrink; ShowCustomer = showCustomer; }
    }

    /// <summary>impl: MinigameUiHost or LegacyMinigames.</summary>
    public interface IMinigameHost
    {
        bool IsBusy { get; }
        event Action<Enum_MiniGameType> Started;
        Awaitable<MinigameOutcome> RunAsync(MinigameRequest request, CancellationToken ct);
    }

    public interface IMinigameAnimation { void Play(); void Stop(); }

    [Serializable]
    public struct GarnishDecoration
    {
        public SO_GlassOption glass; public bool ice;
        public SO_GarnishItemOption item0, item1; public SO_GarnishRimOption rim;
    }

    public enum GarnishExit { Finished, Remake }
    public enum ServeDecision { Served, Remake }

    public readonly struct GarnishResult
    {
        public readonly GarnishExit Exit;
        public readonly GarnishDecoration Decoration;
        public GarnishResult(GarnishExit exit, GarnishDecoration d) { Exit = exit; Decoration = d; }
    }

    public readonly struct GarnishRequest
    {
        public readonly S_Drink Drink;
        public GarnishRequest(S_Drink drink) { Drink = drink; }
    }

    /// <summary>impl: GarnishUiHost or LegacyGarnish.</summary>
    public interface IGarnishHost
    {
        bool IsBusy { get; }
        void ClearGlass();
        Awaitable<GarnishResult> RunAsync(GarnishRequest request, CancellationToken ct);
        Awaitable<ServeDecision> ServeAsync(CancellationToken ct);
    }
}
