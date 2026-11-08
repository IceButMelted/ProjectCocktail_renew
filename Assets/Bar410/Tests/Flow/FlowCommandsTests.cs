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
    public class FlowCommandsTests
    {
        GameObject _go;
        CancellationTokenSource _lifetime;
        FlowPorts _p;
        FlowState _state;
        BartendingSession _session;
        DayRunner _day;
        FlowCommands _cmd;
        SO_DaySchedule _schedule;

        FakeDayUi Ui => (FakeDayUi)_p.DayUi;
        FakeMinigames Mini => (FakeMinigames)_p.Minigames;
        FakeOrders Orders => (FakeOrders)_p.Orders;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("FlowCommandsTests");
            _lifetime = new CancellationTokenSource();
            _p = FakePorts.Build();
            _state = new FlowState();
            _schedule = ScriptableObject.CreateInstance<SO_DaySchedule>();
            JsonUtility.FromJsonOverwrite("{\"days\":[{\"day\":1,\"startNode\":\"N1\",\"chapterName\":\"C1\"}]}", _schedule);
            Orders.Order.BeginOrder(CharacterName.Owen, OrderMode.FixedByName, null, TypeOfCocktail.None);
            var steps = new Station[] { _go.AddComponent<PrepareStation>(), _go.AddComponent<GarnishStation>(), _go.AddComponent<ServeStation>() };
            DayRunner day = null;
            _session = new BartendingSession(steps, _p, _state, _lifetime.Token, () => day.EnterTalkingMode());
            _day = day = new DayRunner(_p, _schedule, _state, _session, _lifetime.Token);
            _cmd = new FlowCommands(_day, _session, _state);
        }

        [TearDown]
        public void TearDown()
        {
            _session.Cancel();
            _lifetime.Cancel(); _lifetime.Dispose();
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_schedule);
        }

        void ToOpen()
        {
            _day.BootAsync(_lifetime.Token); Ui.CompleteTransition(); Ui.CompleteEvents();
            _cmd.OpenBarAsync(false, _lifetime.Token);
        }

        [TestCase("Shaking", Enum_MiniGameType.Shaking)]
        [TestCase("shaking", Enum_MiniGameType.Shaking)]
        [TestCase(" Stiring ", Enum_MiniGameType.Stiring)]
        [TestCase("Stirring", Enum_MiniGameType.Stiring)]
        [TestCase("MIXING", Enum_MiniGameType.Stiring)]
        [TestCase("Building", Enum_MiniGameType.Building)]
        [TestCase("None", Enum_MiniGameType.None)]
        public void TryParse_Aliases(string raw, Enum_MiniGameType expected)
        {
            Assert.IsTrue(FlowCommands.TryParseMinigame(raw, out var t));
            Assert.AreEqual(expected, t);
        }

        [TestCase("Shake")]
        [TestCase("")]
        [TestCase((string)null)]
        [TestCase("7")]
        public void TryParse_Unknown_Warns(string raw)
        {
            LogAssert.Expect(LogType.Warning, new Regex("is not a minigame type"));
            Assert.IsFalse(FlowCommands.TryParseMinigame(raw, out var t));
            Assert.AreEqual(Enum_MiniGameType.None, t);
        }

        [Test]
        public void SelectMinigame_UnknownKeepsSelection_TypeNeverEmpty()
        {
            Assert.AreEqual("None", _cmd.MinigameType);
            _cmd.SelectMinigame("Mixing");
            Assert.AreEqual("Stiring", _cmd.MinigameType);
            LogAssert.Expect(LogType.Warning, new Regex("is not a minigame type"));
            _cmd.SelectMinigame("Blend");
            Assert.AreEqual("Stiring", _cmd.MinigameType);
        }

        [Test]
        public void Strings_FollowState()
        {
            Assert.AreEqual("", _cmd.Phase); Assert.AreEqual("", _cmd.Step); Assert.AreEqual("", _cmd.MinigameResult);
            ToOpen();
            Assert.AreEqual("Open", _cmd.Phase); Assert.AreEqual("TalkingWithCustomer", _cmd.Step);
        }

        [Test]
        public void ButtonCommands_DriveOneDrink_ResultSurvivesSessionEnd()
        {
            ToOpen();
            _cmd.PrepareDrinks();
            Assert.AreEqual("AddIngredient", _cmd.Step);
            _cmd.IngredientAdded("Shaking");
            Assert.AreEqual("Minigame", _cmd.Step);
            Assert.AreEqual(Enum_MiniGameType.Shaking, Mini.LastRequest.Value.Selected);
            _cmd.AnotherIngredient();
            Assert.AreEqual("AddIngredient", _cmd.Step);
            Assert.AreEqual("Cancelled", _cmd.MinigameResult);
            _cmd.IngredientAdded();
            Assert.AreEqual("Shaking", _cmd.MinigameType, "no arg keeps the selection");
            Mini.Complete(MinigameOutcome.Completed);
            Assert.AreEqual("Garnish", _cmd.Step);
            _cmd.RemakeDrink();
            Assert.AreEqual("AddIngredient", _cmd.Step);
            _cmd.IngredientAdded(); _cmd.DrinkComplete(); _cmd.GarnishDone();
            Assert.AreEqual("Serve", _cmd.Step);
            _cmd.ServeDone();
            Assert.AreEqual("TalkingWithCustomer", _cmd.Step);
            Assert.AreEqual("Cancelled", _cmd.MinigameResult);
            _cmd.CloseBar();
            Assert.AreEqual("Close", _cmd.Phase);
            _cmd.NextDay();
            Assert.AreEqual("Event", _cmd.Step);
        }

        [Test]
        public void UnhandledPress_WarnsWithCommandAndCurrentStep()
        {
            ToOpen();
            LogAssert.Expect(LogType.Warning, new Regex(@"flow_garnish_done ignored: not in Garnish \(now TalkingWithCustomer\)"));
            _cmd.GarnishDone();
            _cmd.PrepareDrinks();
            LogAssert.Expect(LogType.Warning, new Regex(@"flow_remake_drink ignored: not in Garnish or Serve \(now AddIngredient\)"));
            _cmd.RemakeDrink();
            LogAssert.Expect(LogType.Warning, new Regex(@"flow_drink_complete ignored: not in Minigame \(now AddIngredient\)"));
            _cmd.DrinkComplete();
            LogAssert.Expect(LogType.Warning, new Regex(@"flow_prepare_drinks ignored: a drink is already being made"));
            _cmd.PrepareDrinks();
            _cmd.IngredientAdded();
            LogAssert.Expect(LogType.Warning, new Regex(@"flow_ingredient_added ignored: not in AddIngredient \(now Minigame\)"));
            _cmd.IngredientAdded();
            LogAssert.Expect(LogType.Warning, new Regex(@"flow_serve_done ignored: not in Serve \(now Minigame\)"));
            _cmd.ServeDone();
            LogAssert.Expect(LogType.Warning, new Regex(@"flow_another_ingredient ignored: not in Minigame \(now Close\)"));
            _session.Cancel(); _cmd.CloseBar();
            _cmd.AnotherIngredient();
        }

        [Test]
        public void PrepareDrinks_OutsideOpen_Warns()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"flow_prepare_drinks ignored: not in Open \(now None\)"));
            _cmd.PrepareDrinks();
            Assert.IsFalse(_session.IsRunning);
        }
    }
}
