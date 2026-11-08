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

    /// <summary>
    /// impl: MinigameUiHost or LegacyMinigames. RunAsync timing (Contracts_Final 3.2):
    /// 1. IsBusy -> InvalidOperationException; then wait (bounded) until the manager is Idle.
    /// 2. Subscribe before StartMinigame.
    /// 3. Completed -> returned after the outro. Back -> CancelMinigame and return Cancelled at once (outro continues alone).
    /// 4. ct cancelled -> CancelMinigame and throw OperationCanceledException synchronously, without waiting for the outro.
    /// 5. Hiding views after Back or cancel runs in a background FlowAwait.Run that waits for Idle, and is skipped
    ///    if a newer run has started (run id).
    /// IsBusy = a RunAsync call has not returned yet (a background outro does not count).
    /// </summary>
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
        /// <summary>Customer seat whose GarnishGlassView shows the glass (0..2). One order at a time (D6).</summary>
        public readonly int SeatIndex;
        public GarnishRequest(S_Drink drink, int seatIndex = 0) { Drink = drink; SeatIndex = seatIndex; }
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
