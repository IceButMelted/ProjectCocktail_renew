using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Bar410.Contracts;
using Bar410.Flow.Tests.Fakes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static E_Cocktail;

namespace Bar410.Flow.Tests
{
    public class ThrowingStationForTests : Station
    {
        public override Awaitable<Next> RunAsync(StepContext ctx)
        {
            ctx.State.SetStep(BarStep.Garnish);
            throw new InvalidOperationException("station boom");
        }
    }

    // Real stations + fake ports. Everything continues inline, so each assertion right after a command
    // sees the label the command produced (label-first contract, PLAYTEST:88-101).
    public class BartendingSessionTests
    {
        GameObject _go;
        CancellationTokenSource _lifetime;
        FlowPorts _p;
        FlowState _state;
        BartendingSession _s;
        readonly List<string> _events = new();

        FakeBar Bar => (FakeBar)_p.Bar;
        FakeOrders Orders => (FakeOrders)_p.Orders;
        FakeMinigames Mini => (FakeMinigames)_p.Minigames;
        FakeGarnish Garnish => (FakeGarnish)_p.Garnish;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("BartendingSessionTests");
            _lifetime = new CancellationTokenSource();
            _p = FakePorts.Build();
            _state = new FlowState();
            _state.SetPart(DayPart.Open);
            Orders.Order.BeginOrder(CharacterName.Owen, OrderMode.FixedByName, null, TypeOfCocktail.None);
            _events.Clear();
            Build(_go.AddComponent<PrepareStation>(), _go.AddComponent<GarnishStation>(), _go.AddComponent<ServeStation>());
        }

        void Build(params Station[] steps)
        {
            _s = new BartendingSession(steps, _p, _state, _lifetime.Token, () => _events.Add("talking"));
            _s.DrinkServed += () => _events.Add("served");
            _s.Faulted += e => _events.Add("faulted:" + e.Message);
        }

        [TearDown]
        public void TearDown()
        {
            _s.Cancel();
            _lifetime.Cancel(); _lifetime.Dispose();
            UnityEngine.Object.DestroyImmediate(_go);
        }

        int Count(List<string> calls, string c) => calls.Count(x => x == c);

        [Test]
        public void TryBegin_IsSynchronous_ThroughFirstStationEntry()
        {
            Assert.IsTrue(_s.TryBegin());
            Assert.IsTrue(_s.IsRunning);
            Assert.IsTrue(_state.SessionRunning);
            Assert.AreEqual("AddIngredient", _state.StepString);
            Assert.AreEqual("ClearGlass", Garnish.Calls.First());
            Assert.IsInstanceOf<PrepareStation>(_s.Current);
        }

        [Test]
        public void FullDrink_ByHosts_ScoresOnce_ThenTalkingMode()
        {
            _s.TryBegin();
            Bar.RaiseMethodChosen(Enum_MiniGameType.Shaking);
            Assert.AreEqual("Minigame", _state.StepString);
            Mini.Complete(MinigameOutcome.Completed);
            Assert.AreEqual("Garnish", _state.StepString);
            Garnish.CompleteGarnish(GarnishExit.Finished);
            Assert.AreEqual("Serve", _state.StepString);
            Assert.AreEqual(0, Count(Orders.Calls, "CommitServe"));
            Garnish.CompleteServe(ServeDecision.Served);

            Assert.AreEqual(1, Count(Orders.Calls, "CommitServe"));
            CollectionAssert.AreEqual(new[] { "talking", "served" }, _events);
            Assert.AreEqual("TalkingWithCustomer", _state.StepString);
            Assert.IsFalse(_s.IsRunning); Assert.IsFalse(_state.SessionRunning);
            Assert.IsNull(_s.Current);
            Assert.AreEqual(2, Count(Garnish.Calls, "ClearGlass"), "at start and in finally");
            Assert.AreEqual("ClearGlass", Garnish.Calls.Last(), "glass cleared after scoring");
        }

        [Test]
        public void LabelFirst_ForcedCommands_ChangeLabelBeforeReturning()
        {
            _s.TryBegin();
            Assert.IsTrue(_s.Handle(StepCommand.IngredientAdded));
            Assert.AreEqual("Minigame", _state.StepString);
            Assert.IsTrue(_s.Handle(StepCommand.AnotherIngredient));
            Assert.AreEqual("AddIngredient", _state.StepString);
            _s.Handle(StepCommand.IngredientAdded);
            Assert.IsTrue(_s.Handle(StepCommand.DrinkComplete));
            Assert.AreEqual("Garnish", _state.StepString);
            Assert.AreEqual("Cancelled", _state.LastMinigameOutcome);
            Assert.IsTrue(_s.Handle(StepCommand.GarnishDone));
            Assert.AreEqual("Serve", _state.StepString);
            Assert.IsTrue(_s.Handle(StepCommand.ServeDone));
            Assert.AreEqual("TalkingWithCustomer", _state.StepString);
            Assert.AreEqual("Cancelled", _state.LastMinigameOutcome, "result survives session end");

            // Serve idempotency: a second press after the serve is refused and scores nothing.
            Assert.IsFalse(_s.Handle(StepCommand.ServeDone));
            Assert.AreEqual(1, Count(Orders.Calls, "CommitServe"));
            CollectionAssert.AreEqual(new[] { "talking", "served" }, _events);
        }

        [Test]
        public void Remake_FromGarnish_RestartsAtFirstStep_NoScore()
        {
            _s.TryBegin();
            _s.Handle(StepCommand.IngredientAdded); Mini.Complete(MinigameOutcome.Completed);
            Assert.AreEqual(1, Count(Bar.Calls, "BeginDrink"));
            Assert.IsTrue(_s.Handle(StepCommand.Remake));
            Assert.AreEqual("AddIngredient", _state.StepString);
            Assert.AreEqual(1, Count(Bar.Calls, "ResetDrink"));
            Assert.AreEqual(2, Count(Bar.Calls, "BeginDrink"));
            Assert.AreEqual(2, Count(Garnish.Calls, "ClearGlass"));
            Assert.AreEqual(0, Count(Orders.Calls, "CommitServe"));
            Assert.IsTrue(_s.IsRunning);
            Assert.IsEmpty(_events);
        }

        [Test]
        public void Remake_FromServe_ByHost_RestartsAtFirstStep_NoScore()
        {
            _s.TryBegin();
            _s.Handle(StepCommand.IngredientAdded); Mini.Complete(MinigameOutcome.Completed);
            _s.Handle(StepCommand.GarnishDone);
            Garnish.CompleteServe(ServeDecision.Remake);
            Assert.AreEqual("AddIngredient", _state.StepString);
            Assert.AreEqual(1, Count(Bar.Calls, "ResetDrink"));
            Assert.AreEqual(0, Count(Orders.Calls, "CommitServe"));

            // and the second drink still scores exactly once
            _s.Handle(StepCommand.IngredientAdded); _s.Handle(StepCommand.DrinkComplete);
            _s.Handle(StepCommand.GarnishDone); _s.Handle(StepCommand.ServeDone);
            Assert.AreEqual(1, Count(Orders.Calls, "CommitServe"));
        }

        [Test]
        public void Remake_NotValidIn21Or22()
        {
            _s.TryBegin();
            Assert.IsFalse(_s.Handle(StepCommand.Remake));
            _s.Handle(StepCommand.IngredientAdded);
            Assert.IsFalse(_s.Handle(StepCommand.Remake));
        }

        [Test]
        public void Cancel_InMinigame_TalkingOnce_NoScore()
        {
            _s.TryBegin();
            _s.Handle(StepCommand.IngredientAdded);
            _s.Cancel();
            Assert.IsFalse(_s.IsRunning);
            Assert.IsFalse(Mini.IsBusy);
            Assert.AreEqual("TalkingWithCustomer", _state.StepString);
            CollectionAssert.AreEqual(new[] { "talking" }, _events);
            Assert.AreEqual(0, Count(Orders.Calls, "CommitServe"));
            Assert.IsTrue(_s.TryBegin(), "a new drink can start after a cancel");
        }

        [Test]
        public void LifetimeCancel_NoTalkingMode_NoUiRestore()
        {
            _s.TryBegin();
            _s.Handle(StepCommand.IngredientAdded);
            Bar.Calls.Clear();
            _lifetime.Cancel();
            Assert.IsFalse(_s.IsRunning);
            Assert.IsEmpty(_events);
            Assert.IsEmpty(Bar.Calls);
        }

        [Test]
        public void Fault_RestoresTalkingMode_ThenFaulted_AndLogs()
        {
            UnityEngine.Object.DestroyImmediate(_go);
            _go = new GameObject("BartendingSessionTests");
            Build(_go.AddComponent<ThrowingStationForTests>());
            LogAssert.Expect(LogType.Exception, new Regex("station boom"));
            Assert.IsTrue(_s.TryBegin());
            CollectionAssert.AreEqual(new[] { "talking", "faulted:station boom" }, _events);
            Assert.IsFalse(_s.IsRunning);
            Assert.AreEqual(BarStep.Idle, _state.Step);
            Assert.AreEqual(0, Count(Orders.Calls, "CommitServe"));
        }

        [Test]
        public void TryBegin_WhileRunning_Warns()
        {
            _s.TryBegin();
            LogAssert.Expect(LogType.Warning, new Regex("already"));
            Assert.IsFalse(_s.TryBegin());
            Assert.AreEqual(1, Count(Bar.Calls, "BeginDrink"));
        }

        [TestCase(DayPart.None)]
        [TestCase(DayPart.Event)]
        [TestCase(DayPart.Story)]
        [TestCase(DayPart.Close)]
        public void TryBegin_OutsideOpen_WarnsAndRefuses(DayPart part)
        {
            _state.SetPart(part);
            LogAssert.Expect(LogType.Warning, new Regex($@"flow_prepare_drinks ignored: not in Open \(now {part}\)"));
            Assert.IsFalse(_s.TryBegin());
            Assert.IsFalse(_s.IsRunning);
            Assert.IsEmpty(Bar.Calls);
            Assert.IsEmpty(Garnish.Calls);
        }

        [Test]
        public void TryBegin_NoSteps_Errors()
        {
            Build();
            LogAssert.Expect(LogType.Error, new Regex("no steps"));
            Assert.IsFalse(_s.TryBegin());
            Assert.IsFalse(_s.IsRunning);
        }

        [Test]
        public void Handle_WhenIdle_False()
        {
            Assert.IsFalse(_s.Handle(StepCommand.ServeDone));
            Assert.IsFalse(_s.Handle(StepCommand.IngredientAdded));
        }
    }
}
