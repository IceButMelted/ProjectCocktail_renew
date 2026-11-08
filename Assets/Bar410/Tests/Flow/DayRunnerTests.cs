using System.Collections.Generic;
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
    public class DayRunnerTests
    {
        GameObject _go;
        CancellationTokenSource _lifetime;
        FlowPorts _p;
        FlowState _state;
        BartendingSession _session;
        DayRunner _day;
        SO_DaySchedule _schedule;
        readonly List<FlowMilestone> _milestones = new();
        readonly List<string> _labels = new();

        FakeCamera Cam => (FakeCamera)_p.Camera;
        FakeBar Bar => (FakeBar)_p.Bar;
        FakeStory Story => (FakeStory)_p.Story;
        FakeDayUi Ui => (FakeDayUi)_p.DayUi;
        FakeGarnish Garnish => (FakeGarnish)_p.Garnish;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("DayRunnerTests");
            _lifetime = new CancellationTokenSource();
            _p = FakePorts.Build();
            _state = new FlowState();
            _schedule = ScriptableObject.CreateInstance<SO_DaySchedule>();
            _schedule.name = "TestSchedule";
            JsonUtility.FromJsonOverwrite("{\"days\":[{\"day\":1,\"startNode\":\"N1\",\"chapterName\":\"C1\"},{\"day\":2,\"startNode\":\"N2\",\"chapterName\":\"C2\"}]}", _schedule);
            ((FakeOrders)_p.Orders).Order.BeginOrder(CharacterName.Owen, OrderMode.FixedByName, null, TypeOfCocktail.None);
            var steps = new Station[] { _go.AddComponent<PrepareStation>(), _go.AddComponent<GarnishStation>(), _go.AddComponent<ServeStation>() };
            DayRunner day = null;
            _session = new BartendingSession(steps, _p, _state, _lifetime.Token, () => day.EnterTalkingMode());
            _day = day = new DayRunner(_p, _schedule, _state, _session, _lifetime.Token);
            _milestones.Clear(); _labels.Clear();
            _day.Milestone += _milestones.Add;
            _state.Changed += () =>
            {
                var l = _state.PhaseString + "/" + _state.StepString;
                if (_labels.Count == 0 || _labels[^1] != l) _labels.Add(l);
            };
        }

        [TearDown]
        public void TearDown()
        {
            _session.Cancel();
            _lifetime.Cancel(); _lifetime.Dispose();
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_schedule);
        }

        void BootToStory()
        {
            _day.BootAsync(_lifetime.Token);
            Ui.CompleteTransition();
            Ui.CompleteEvents();
        }

        [Test]
        public void Boot_WaitsTransition_CutsBeforeEvents_ThenStory()
        {
            var boot = _day.BootAsync(_lifetime.Token);
            Assert.AreEqual("", _state.PhaseString);
            Assert.IsTrue(Ui.TransitionPending);
            Assert.IsFalse(Story.Calls.Any());

            Ui.CompleteTransition();
            Assert.AreEqual("BeforeOpen/Event", _state.PhaseString + "/" + _state.StepString);
            Assert.IsTrue(Ui.EventsPending);
            CollectionAssert.Contains(Bar.Calls, "RefreshUnlocked");
            int cut = Cam.Calls.IndexOf("Cut(Bar)");
            Assert.That(cut, Is.GreaterThan(Cam.Calls.IndexOf("ResetLook")));
            Assert.IsEmpty(_milestones);

            Ui.CompleteEvents();
            Assert.AreEqual("StoryBeforeOpen", _state.StepString);
            CollectionAssert.AreEqual(new[] { FlowMilestone.DayStarted }, _milestones);
            CollectionAssert.AreEqual(new[] { "StartStory(N1)" }, Story.Calls);
            Assert.IsTrue(boot.IsCompleted);
        }

        [Test]
        public void FullDay_LabelSequence_MatchesOldStrings()
        {
            BootToStory();
            _day.CheckEventsAsync(_lifetime.Token); Ui.CompleteEvents();
            _day.OpenBarAsync(false, _lifetime.Token);
            _session.TryBegin();
            _session.Handle(StepCommand.IngredientAdded);
            _session.Handle(StepCommand.DrinkComplete);
            _session.Handle(StepCommand.GarnishDone);
            _session.Handle(StepCommand.ServeDone);
            Assert.IsTrue(_day.CloseBar());
            Assert.IsTrue(_day.NextDay());
            Ui.CompleteEvents();

            CollectionAssert.AreEqual(new[]
            {
                "BeforeOpen/Event", "BeforeOpen/StoryBeforeOpen", "BeforeOpen/Event", "BeforeOpen/StoryBeforeOpen",
                "Open/TalkingWithCustomer", "Open/AddIngredient", "Open/Minigame", "Open/Garnish", "Open/Serve",
                "Open/TalkingWithCustomer", "Close/", "BeforeOpen/Event", "BeforeOpen/StoryBeforeOpen",
            }, _labels);
            CollectionAssert.AreEqual(new[] { FlowMilestone.DayStarted, FlowMilestone.DrinkServed, FlowMilestone.DayClosed, FlowMilestone.DayStarted }, _milestones);
            CollectionAssert.AreEqual(new[] { "StartStory(N1)", "AdvanceDay", "StartStory(N2)" }, Story.Calls);
        }

        [Test]
        public void CheckEvents_MidStory_DoesNotRestartStory()
        {
            BootToStory();
            var aw = _day.CheckEventsAsync(_lifetime.Token);
            Assert.AreEqual("Event", _state.StepString);
            Assert.IsFalse(aw.IsCompleted);
            Ui.CompleteEvents();
            Assert.AreEqual("StoryBeforeOpen", _state.StepString);
            Assert.IsTrue(aw.IsCompleted);
            Assert.AreEqual(1, Story.Calls.Count);
            Assert.AreEqual(1, _milestones.Count, "no second DayStarted");
            Assert.AreEqual(1, Cam.Calls.Count(c => c == "Cut(Bar)"), "mid-story events keep the writer's shot");
        }

        [Test]
        public async Task CheckEvents_CancelledByOtherToken_ReturnsToStory()
        {
            BootToStory();
            using var cts = new CancellationTokenSource();
            var aw = _day.CheckEventsAsync(cts.Token);
            Assert.AreEqual("Event", _state.StepString);
            cts.Cancel();
            Assert.AreEqual("StoryBeforeOpen", _state.StepString, "no soft-lock in Event");
            Assert.IsInstanceOf<System.OperationCanceledException>(await StationTests.Catch(async () => await aw));
            _day.OpenBarAsync(false, _lifetime.Token);
            Assert.AreEqual("Open", _state.PhaseString);
        }

        [Test]
        public void CheckEvents_LifetimeCancel_LeavesPart()
        {
            BootToStory();
            _day.CheckEventsAsync(_lifetime.Token);
            _lifetime.Cancel();
            Assert.AreEqual(DayPart.Event, _state.Part);
        }

        [Test]
        public async Task OpenBar_TalkingMode_ThenChapterTitle_Awaited()
        {
            BootToStory();
            Cam.Calls.Clear(); Bar.Calls.Clear();
            var aw = _day.OpenBarAsync(true, _lifetime.Token);
            Assert.AreEqual("Open/TalkingWithCustomer", _state.PhaseString + "/" + _state.StepString);
            CollectionAssert.AreEqual(new[] { "LockIngredients", "CloseBook" }, Bar.Calls);
            CollectionAssert.AreEqual(new[] { "SetLookFree(False)", "Show(Bar)" }, Cam.Calls);
            CollectionAssert.Contains(Ui.Calls, "ShowChapterTitle(C1,True)");
            Assert.IsFalse(aw.IsCompleted);
            Ui.CompleteTitle();
            await aw;
        }

        [Test]
        public void Guards_Warn_AndChangeNothing()
        {
            _day.BootAsync(_lifetime.Token); Ui.CompleteTransition(); // in Event
            LogAssert.Expect(LogType.Warning, new Regex(@"flow_open_bar ignored: not in Story \(now Event\)"));
            _day.OpenBarAsync(false, _lifetime.Token);
            LogAssert.Expect(LogType.Warning, new Regex(@"flow_check_events ignored: not in Story \(now Event\)"));
            Assert.IsTrue(_day.CheckEventsAsync(_lifetime.Token).IsCompleted);
            LogAssert.Expect(LogType.Warning, new Regex(@"flow_close_bar ignored: not in Open"));
            Assert.IsFalse(_day.CloseBar());
            LogAssert.Expect(LogType.Warning, new Regex(@"flow_next_day ignored: not in Close"));
            Assert.IsFalse(_day.NextDay());
            Assert.AreEqual(DayPart.Event, _state.Part);
            Assert.AreEqual(0, Story.Calls.Count(c => c == "AdvanceDay"));
        }

        [Test]
        public void CloseBar_RefusedWhileDrinkRunning()
        {
            BootToStory();
            _day.OpenBarAsync(false, _lifetime.Token);
            _session.TryBegin();
            LogAssert.Expect(LogType.Warning, new Regex("flow_close_bar ignored: a drink is being made"));
            Assert.IsFalse(_day.CloseBar());
            _session.Cancel();
            Assert.IsTrue(_day.CloseBar());
            Assert.AreEqual("Close/", _state.PhaseString + "/" + _state.StepString);
        }

        [Test]
        public void NextDay_DoesNotBlock_PartIsEventAtOnce()
        {
            BootToStory();
            _day.OpenBarAsync(false, _lifetime.Token);
            _day.CloseBar();
            Assert.IsTrue(_day.NextDay());
            Assert.AreEqual(2, Story.Day);
            Assert.AreEqual("Event", _state.StepString);
            Assert.IsTrue(Ui.EventsPending, "returned while the new day's events still wait");
            Ui.CompleteEvents();
            Assert.AreEqual("StartStory(N2)", Story.Calls.Last());
        }

        [Test]
        public void MissingRow_LogsError_StoryNotStarted()
        {
            Story.Day = 3;
            LogAssert.Expect(LogType.Error, new Regex("TestSchedule has no row for day 3"));
            BootToStory();
            Assert.IsFalse(Story.Calls.Any(c => c.StartsWith("StartStory")));
            Assert.AreEqual("StoryBeforeOpen", _state.StepString);
        }

        [Test]
        public void Resume_Open_NoEventsNoTitleNoDayStarted()
        {
            var aw = _day.ResumeAsync(DayPart.Open, "Root", _lifetime.Token);
            Assert.AreEqual("", _state.PhaseString);
            Ui.CompleteTransition();
            Assert.IsTrue(aw.IsCompleted);
            Assert.AreEqual("Open/TalkingWithCustomer", _state.PhaseString + "/" + _state.StepString);
            CollectionAssert.AreEqual(new[] { "StartStory(Root)" }, Story.Calls);
            CollectionAssert.AreEqual(new[] { "WaitTransition" }, Ui.Calls);
            Assert.IsEmpty(_milestones);
            CollectionAssert.Contains(Cam.Calls, "Cut(Bar)");
        }

        [Test]
        public void Resume_StoryAndClose_SetPartOnly()
        {
            _day.ResumeAsync(DayPart.Story, "R1", _lifetime.Token); Ui.CompleteTransition();
            Assert.AreEqual("StoryBeforeOpen", _state.StepString);
            _day.ResumeAsync(DayPart.Close, "R2", _lifetime.Token); Ui.CompleteTransition();
            Assert.AreEqual("Close", _state.PhaseString);
            CollectionAssert.AreEqual(new[] { "StartStory(R1)", "StartStory(R2)" }, Story.Calls);
        }

        [Test]
        public void Resume_Event_LogsError_StartsDay()
        {
            LogAssert.Expect(LogType.Error, new Regex("Resume at Event"));
            _day.ResumeAsync(DayPart.Event, "Root", _lifetime.Token);
            Ui.CompleteTransition();
            Assert.AreEqual("Event", _state.StepString);
            Ui.CompleteEvents();
            CollectionAssert.AreEqual(new[] { "StartStory(N1)" }, Story.Calls);
        }

        [Test]
        public void LifetimeCancel_DuringBoot_StopsQuietly()
        {
            _day.BootAsync(_lifetime.Token);
            _lifetime.Cancel();
            Assert.IsFalse(Ui.TransitionPending);
            Assert.AreEqual(DayPart.None, _state.Part);
            Assert.IsEmpty(Story.Calls);
        }
    }
}
