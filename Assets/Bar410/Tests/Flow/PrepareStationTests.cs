using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bar410.Contracts;
using Bar410.Flow.Tests.Fakes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static E_Cocktail;

namespace Bar410.Flow.Tests
{
    public class PrepareStationTests
    {
        GameObject _go;
        CancellationTokenSource _session, _lifetime;
        FlowPorts _p;
        FlowState _state;
        PrepareStation _st;

        FakeBar Bar => (FakeBar)_p.Bar;
        FakeCamera Cam => (FakeCamera)_p.Camera;
        FakeOrders Orders => (FakeOrders)_p.Orders;
        FakeMinigames Mini => (FakeMinigames)_p.Minigames;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("PrepareStationTests");
            _st = _go.AddComponent<PrepareStation>();
            _lifetime = new CancellationTokenSource();
            _session = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            _p = FakePorts.Build();
            _state = new FlowState();
            _state.SetPart(DayPart.Open);
            Orders.Order.BeginOrder(CharacterName.Isla, OrderMode.FixedByName, null, TypeOfCocktail.None);
        }

        [TearDown]
        public void TearDown()
        {
            _session.Cancel(); _session.Dispose(); _lifetime.Dispose();
            UnityEngine.Object.DestroyImmediate(_go);
        }

        Awaitable<Next> Run() => _st.RunAsync(new StepContext(_state, _p, _session.Token, _lifetime.Token));

        [Test]
        public void Entry_SubscribesBeforeBeginDrink_ThenOpens21()
        {
            Run();
            // BeginDrink's synchronous ShakerCleared reached the handler (Show(Bar) + look fixed while still Idle).
            CollectionAssert.AreEqual(new[] { "Show(Bar)", "SetLookFree(False)", "ResetLook", "Show(Bar)", "SetLookFree(True)" }, Cam.Calls);
            CollectionAssert.AreEqual(new[] { "BeginDrink", "SetOrderNoteAvailable(True)", "OpenIngredients" }, Bar.Calls);
            CollectionAssert.AreEqual(new[] { "EnsureOrder" }, Orders.Calls);
            Assert.AreEqual("AddIngredient", _state.StepString);
        }

        [Test]
        public void RefreshLook_TruthTable()
        {
            Bar.HasRoom = false;
            Run();
            Assert.AreEqual("SetLookFree(False)", Cam.Calls.Last(), "2.1 with a full shaker");
            Bar.HasRoom = true;
            Cam.Calls.Clear(); Bar.RaiseShakerCleared();
            CollectionAssert.AreEqual(new[] { "Show(Bar)", "SetLookFree(True)" }, Cam.Calls, "cleared in 2.1 with room");
            Cam.Calls.Clear(); Bar.RaiseResetChosen();
            CollectionAssert.AreEqual(new[] { "Show(Bar)", "SetLookFree(True)" }, Cam.Calls, "method-panel reset = cleared");
            Cam.Calls.Clear(); Bar.RaiseShakerFull();
            CollectionAssert.AreEqual(new[] { "SetLookFree(False)", "Show(Shaker)" }, Cam.Calls, "full");
            Bar.RaiseMethodChosen(Enum_MiniGameType.Shaking);
            Cam.Calls.Clear(); Bar.RaiseShakerCleared();
            CollectionAssert.AreEqual(new[] { "Show(Bar)", "SetLookFree(False)" }, Cam.Calls, "cleared in 2.2: fixed");
        }

        [Test]
        public void MethodChosen_Selects_AndMovesTo22_Synchronously()
        {
            Bar.RequiredMinigame = Enum_MiniGameType.Stiring;
            Run();
            Bar.RaiseMethodChosen(Enum_MiniGameType.Shaking);
            Assert.AreEqual("Minigame", _state.StepString);
            Assert.AreEqual(Enum_MiniGameType.Shaking, _state.SelectedMinigame);
            Assert.AreEqual("CloseIngredients", Bar.Calls.Last());
            var r = Mini.LastRequest.Value;
            Assert.AreEqual(Enum_MiniGameType.Shaking, r.Selected);
            Assert.AreEqual(Enum_MiniGameType.Stiring, r.FromDrink);
            Assert.IsTrue(r.ShowCustomer);
            CollectionAssert.IsSubsetOf(new[] { "SetLookFree(False)", "AimCustomerInset(Isla)" }, Cam.Calls);
        }

        [Test]
        public void MethodChosen_Outside21_IsIgnored()
        {
            Run();
            Bar.RaiseMethodChosen(Enum_MiniGameType.Shaking);
            Bar.RaiseMethodChosen(Enum_MiniGameType.Stiring);
            Assert.AreEqual(Enum_MiniGameType.Shaking, _state.SelectedMinigame);
            Assert.AreEqual(1, Mini.Calls.Count);
        }

        [Test]
        public void IngredientAdded_OnlyIn21_KeepsSelection()
        {
            Run();
            Assert.IsFalse(_st.Handle(StepCommand.AnotherIngredient));
            Assert.IsFalse(_st.Handle(StepCommand.DrinkComplete));
            Assert.IsTrue(_st.Handle(StepCommand.IngredientAdded));
            Assert.AreEqual("Minigame", _state.StepString);
            Assert.AreEqual(Enum_MiniGameType.None, _state.SelectedMinigame);
            Assert.IsFalse(_st.Handle(StepCommand.IngredientAdded));
            Assert.IsFalse(_st.Handle(StepCommand.GarnishDone));
        }

        [Test]
        public async Task Completed_Continues_ClosesEverything()
        {
            var run = Run();
            _st.Handle(StepCommand.IngredientAdded);
            Assert.AreEqual("", _state.LastMinigameOutcome);
            Bar.Calls.Clear(); Cam.Calls.Clear();
            Mini.Complete(MinigameOutcome.Completed);
            Assert.AreEqual(Next.Continue, await run);
            Assert.AreEqual("Completed", _state.LastMinigameOutcome);
            CollectionAssert.AreEqual(new[] { "CloseBook", "SetOrderNoteAvailable(False)" }, Bar.Calls);
            CollectionAssert.AreEqual(new[] { "AimCustomerInset(None)", "SetLookFree(False)" }, Cam.Calls);
            Bar.RaiseShakerFull(); // unsubscribed
            Assert.AreEqual(2, Cam.Calls.Count);
        }

        [Test]
        public async Task Cancelled_LoopsTo21_GrabbableAgain_ThenCompletes()
        {
            var run = Run();
            _st.Handle(StepCommand.IngredientAdded);
            Mini.Complete(MinigameOutcome.Cancelled);
            Assert.AreEqual("AddIngredient", _state.StepString);
            Assert.AreEqual("Cancelled", _state.LastMinigameOutcome);
            CollectionAssert.Contains(Bar.Calls, "SetShakerGrabbable(True)");
            Assert.AreEqual(2, Orders.Calls.Count(c => c == "EnsureOrder"), "EnsureOrder on every 2.1 entry");
            Assert.AreEqual("OpenIngredients", Bar.Calls.Last());

            _st.Handle(StepCommand.IngredientAdded);
            Assert.AreEqual("", _state.LastMinigameOutcome, "cleared when a minigame starts");
            Mini.Complete(MinigameOutcome.Completed);
            Assert.AreEqual(Next.Continue, await run);
            Assert.AreEqual("Completed", _state.LastMinigameOutcome);
        }

        [Test]
        public void ForcedAnotherIngredient_CancelsHost_LabelBackTo21()
        {
            Run();
            _st.Handle(StepCommand.IngredientAdded);
            Assert.IsTrue(_st.Handle(StepCommand.AnotherIngredient));
            Assert.AreEqual("AddIngredient", _state.StepString);
            Assert.IsFalse(Mini.IsBusy, "the forced run was cancelled");
            Assert.AreEqual("Cancelled", _state.LastMinigameOutcome);
            CollectionAssert.Contains(Bar.Calls, "SetShakerGrabbable(True)");
            Assert.IsFalse(_st.Handle(StepCommand.AnotherIngredient), "no minigame running");

            // A fresh run is independent of the forced one.
            _st.Handle(StepCommand.IngredientAdded);
            Assert.IsTrue(Mini.IsBusy);
            Assert.AreEqual("Minigame", _state.StepString);
        }

        [Test]
        public async Task ForcedDrinkComplete_Continues_RecordsCancelled()
        {
            var run = Run();
            _st.Handle(StepCommand.IngredientAdded);
            Assert.IsTrue(_st.Handle(StepCommand.DrinkComplete));
            Assert.IsFalse(Mini.IsBusy);
            Assert.AreEqual(Next.Continue, await run);
            Assert.AreEqual("Cancelled", _state.LastMinigameOutcome);
            Assert.IsFalse(Bar.Calls.Contains("SetShakerGrabbable(True)"));
        }

        [Test]
        public void NoCustomer_Warns_AndHidesInset()
        {
            Orders.Order.Clear();
            Run();
            LogAssert.Expect(LogType.Warning, new Regex("order_customer"));
            _st.Handle(StepCommand.IngredientAdded);
            Assert.IsFalse(Mini.LastRequest.Value.ShowCustomer);
            CollectionAssert.Contains(Cam.Calls, "AimCustomerInset(None)");
        }

        [Test]
        public async Task SessionCancelIn21_Oce_ClosesIngredients()
        {
            var run = Run();
            Bar.Calls.Clear();
            _session.Cancel();
            Assert.IsInstanceOf<OperationCanceledException>(await StationTests.Catch(async () => await run));
            CollectionAssert.AreEqual(new[] { "CloseIngredients", "CloseBook", "SetOrderNoteAvailable(False)" }, Bar.Calls);
            Assert.IsFalse(_st.Handle(StepCommand.IngredientAdded));
        }

        [Test]
        public async Task LifetimeCancelIn22_Oce_SkipsUiRestore()
        {
            var run = Run();
            _st.Handle(StepCommand.IngredientAdded);
            Bar.Calls.Clear(); Cam.Calls.Clear();
            _lifetime.Cancel();
            Assert.IsInstanceOf<OperationCanceledException>(await StationTests.Catch(async () => await run));
            Assert.IsEmpty(Bar.Calls); Assert.IsEmpty(Cam.Calls);
            Assert.IsFalse(Mini.IsBusy);
        }
    }
}
