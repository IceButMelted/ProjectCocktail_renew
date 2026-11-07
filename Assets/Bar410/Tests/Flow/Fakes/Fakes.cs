using System;
using System.Collections.Generic;
using System.Threading;
using Bar410.Contracts;
using Bar410.Core;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Flow.Tests.Fakes
{
    // Hand-written fakes of the Contracts ports Flow uses. Each records calls as "Name(args)" strings in Calls.
    // Pending awaits are released with Complete(...) or by cancelling the token passed in (FlowAwait.Wait throws OCE).

    public abstract class FakeBase { public readonly List<string> Calls = new(); protected void Log(string s) => Calls.Add(s); }

    public sealed class FakeCamera : FakeBase, ICameraDirector
    {
        public string ActiveCameraId { get; private set; } = "";
        public void Show(CameraShot shot) => Log($"Show({shot})");
        public void Cut(CameraShot shot) => Log($"Cut({shot})");
        public void SwitchTo(string id) { ActiveCameraId = id; Log($"SwitchTo({id})"); }
        public void CutTo(string id) { ActiveCameraId = id; Log($"CutTo({id})"); }
        public void SetLookFree(bool free) => Log($"SetLookFree({free})");
        public void ResetLook() => Log("ResetLook");
        public void AimCustomerInset(CharacterName who) => Log($"AimCustomerInset({who})");
    }

    public sealed class FakeBar : FakeBase, IBarCounter
    {
        public bool HasRoom { get; set; } = true;
        public bool IsEmpty { get; set; } = true;
        public Enum_MiniGameType RequiredMinigame { get; set; }
        public S_Drink Drink;
        public event Action ShakerFull, ShakerCleared, ResetChosen;
        public event Action<Enum_MiniGameType> MethodChosen;
        public void RaiseShakerFull() => ShakerFull?.Invoke();
        public void RaiseShakerCleared() => ShakerCleared?.Invoke();
        public void RaiseResetChosen() => ResetChosen?.Invoke();
        public void RaiseMethodChosen(Enum_MiniGameType t) => MethodChosen?.Invoke(t);
        /// <summary>Like the real bar: BeginDrink fires ShakerCleared synchronously.</summary>
        public void BeginDrink() { Log("BeginDrink"); ShakerCleared?.Invoke(); }
        public void OpenIngredients() => Log("OpenIngredients");
        public void CloseIngredients() => Log("CloseIngredients");
        public void LockIngredients() => Log("LockIngredients");
        public void RefreshUnlocked() => Log("RefreshUnlocked");
        public S_Drink ResolveDrink() { Log("ResolveDrink"); return Drink; }
        public void SetIce(bool on) => Log($"SetIce({on})");
        public void SetShakerGrabbable(bool on) => Log($"SetShakerGrabbable({on})");
        public void ResetDrink() => Log("ResetDrink");
        public void SetOrderNoteAvailable(bool on) => Log($"SetOrderNoteAvailable({on})");
        public void CloseBook() => Log("CloseBook");
    }

    public sealed class FakeOrders : FakeBase, IOrderDesk
    {
        public DrinkOrderContext Order { get; } = new DrinkOrderContext();
        public ScoreResult NextScore;
        public bool EnsureResult = true;
        public event Action<ScoreResult> Scored;
        public event Action Cleared;
        public string PlaceNamed(string recipeName, bool byFlavor) { Log($"PlaceNamed({recipeName},{byFlavor})"); return recipeName; }
        public string PlaceByPreference(CharacterName c, bool byFlavor) { Log($"PlaceByPreference({c},{byFlavor})"); return ""; }
        public bool PlaceByType(TypeOfCocktail t) { Log($"PlaceByType({t})"); return true; }
        public void SetCustomer(CharacterName c) => Log($"SetCustomer({c})");
        public void ClearOrder() { Log("ClearOrder"); Cleared?.Invoke(); }
        public bool EnsureOrder() { Log("EnsureOrder"); return EnsureResult; }
        public ScoreResult CommitServe() { Log("CommitServe"); Scored?.Invoke(NextScore); return NextScore; }
        public void UseRecipeFilter(Func<S_Drink, bool> canMake) => Log("UseRecipeFilter");
        public void UsePreferences(ICustomerPreferences prefs) => Log("UsePreferences");
    }

    public sealed class FakeMinigames : FakeBase, IMinigameHost
    {
        AwaitableCompletionSource<MinigameOutcome> _pending;
        public bool IsBusy => _pending != null;
        public event Action<Enum_MiniGameType> Started;
        public MinigameRequest? LastRequest { get; private set; }
        public Awaitable<MinigameOutcome> RunAsync(MinigameRequest r, CancellationToken ct)
        {
            Log($"RunAsync({r.Selected},{r.FromDrink},{r.ShowCustomer})");
            LastRequest = r;
            var src = _pending = new AwaitableCompletionSource<MinigameOutcome>();
            Started?.Invoke(r.Selected);
            return Run(src, ct);
        }
        async Awaitable<MinigameOutcome> Run(AwaitableCompletionSource<MinigameOutcome> src, CancellationToken ct)
        {
            try { return await FlowAwait.Wait(src, ct); } finally { if (_pending == src) _pending = null; }
        }
        public void Complete(MinigameOutcome o) => _pending.SetResult(o);
    }

    public sealed class FakeGarnish : FakeBase, IGarnishHost
    {
        AwaitableCompletionSource<GarnishResult> _garnish;
        AwaitableCompletionSource<ServeDecision> _serve;
        public bool IsBusy => _garnish != null || _serve != null;
        public void ClearGlass() => Log("ClearGlass");
        public Awaitable<GarnishResult> RunAsync(GarnishRequest r, CancellationToken ct)
        {
            Log("RunAsync");
            var src = _garnish = new AwaitableCompletionSource<GarnishResult>();
            return RunG(src, ct);
        }
        async Awaitable<GarnishResult> RunG(AwaitableCompletionSource<GarnishResult> src, CancellationToken ct)
        {
            try { return await FlowAwait.Wait(src, ct); } finally { if (_garnish == src) _garnish = null; }
        }
        public Awaitable<ServeDecision> ServeAsync(CancellationToken ct)
        {
            Log("ServeAsync");
            var src = _serve = new AwaitableCompletionSource<ServeDecision>();
            return RunS(src, ct);
        }
        async Awaitable<ServeDecision> RunS(AwaitableCompletionSource<ServeDecision> src, CancellationToken ct)
        {
            try { return await FlowAwait.Wait(src, ct); } finally { if (_serve == src) _serve = null; }
        }
        public void CompleteGarnish(GarnishExit e) => _garnish.SetResult(new GarnishResult(e, default));
        public void CompleteServe(ServeDecision d) => _serve.SetResult(d);
    }

    public sealed class FakeStory : FakeBase, IStory
    {
        public int Day { get; set; } = 1;
        public void AdvanceDay() { Day++; Log("AdvanceDay"); }
        public void StartStory(string node) => Log($"StartStory({node})");
    }

    public sealed class FakeDayUi : FakeBase, IDayUi
    {
        AwaitableCompletionSource _transition, _events, _title;
        public bool TransitionPending => _transition != null;
        public bool EventsPending => _events != null;
        public Awaitable WaitTransitionAsync(CancellationToken ct) { Log("WaitTransition"); return Wait(ref _transition, ct); }
        public Awaitable ShowDayEventsAsync(CancellationToken ct) { Log("ShowDayEvents"); return Wait(ref _events, ct); }
        public Awaitable ShowChapterTitleAsync(string key, bool wait, CancellationToken ct)
        {
            Log($"ShowChapterTitle({key},{wait})");
            return wait ? Wait(ref _title, ct) : Done();
        }
        static async Awaitable Done() { await Awaitable.NextFrameAsync(); }
        // Own source per wait (FlowAwait rule). The field is cleared when the wait ends.
        Awaitable Wait(ref AwaitableCompletionSource slot, CancellationToken ct)
        {
            var src = slot = new AwaitableCompletionSource();
            return FlowAwait.Wait(src, ct);
        }
        public void CompleteTransition() => _transition.SetResult();
        public void CompleteEvents() => _events.SetResult();
        public void CompleteTitle() => _title.SetResult();
    }

    public static class FakePorts
    {
        public static FlowPorts Build() => new FlowPorts
        {
            Camera = new FakeCamera(), Bar = new FakeBar(), Orders = new FakeOrders(), Minigames = new FakeMinigames(),
            Garnish = new FakeGarnish(), Story = new FakeStory(), DayUi = new FakeDayUi(),
        };
    }
}
