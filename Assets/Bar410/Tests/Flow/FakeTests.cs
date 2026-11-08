using System;
using System.Threading;
using System.Threading.Tasks;
using Bar410.Contracts;
using NUnit.Framework;
using Bar410.Flow.Tests.Fakes;
using static E_Cocktail;

namespace Bar410.Flow.Tests
{
    public class FakeTests
    {
        [Test]
        public void Build_FillsEveryPort()
        {
            var p = FakePorts.Build();
            Assert.IsNotNull(p.Camera); Assert.IsNotNull(p.Bar); Assert.IsNotNull(p.Orders); Assert.IsNotNull(p.Minigames);
            Assert.IsNotNull(p.Garnish); Assert.IsNotNull(p.Story); Assert.IsNotNull(p.DayUi);
        }

        [Test]
        public void Calls_AreRecordedInOrder()
        {
            var cam = new FakeCamera();
            cam.Show(CameraShot.Bar); cam.Cut(CameraShot.Shaker); cam.ResetLook();
            CollectionAssert.AreEqual(new[] { "Show(Bar)", "Cut(Shaker)", "ResetLook" }, cam.Calls);

            var bar = new FakeBar(); int cleared = 0; bar.ShakerCleared += () => cleared++;
            bar.BeginDrink();
            Assert.AreEqual(1, cleared); CollectionAssert.AreEqual(new[] { "BeginDrink" }, bar.Calls);

            var story = new FakeStory(); story.AdvanceDay();
            Assert.AreEqual(2, story.Day);
        }

        [Test]
        public async Task Minigame_CanBeCompleted()
        {
            var m = new FakeMinigames();
            var aw = m.RunAsync(new MinigameRequest(Enum_MiniGameType.Shaking, Enum_MiniGameType.Shaking, true), CancellationToken.None);
            Assert.IsTrue(m.IsBusy);
            m.Complete(MinigameOutcome.Completed);
            Assert.AreEqual(MinigameOutcome.Completed, await aw);
            Assert.IsFalse(m.IsBusy);
            CollectionAssert.AreEqual(new[] { "RunAsync(Shaking,Shaking,True)" }, m.Calls);
        }

        [Test]
        public async Task Garnish_CancelThrowsOce()
        {
            var g = new FakeGarnish(); using var cts = new CancellationTokenSource();
            var aw = g.ServeAsync(cts.Token);
            cts.Cancel();
            try { await aw; Assert.Fail("expected OCE"); } catch (OperationCanceledException) { }
            Assert.IsFalse(g.IsBusy);
        }

        [Test]
        public void Garnish_RunCancel_EndsSynchronously_NotBusy()
        {
            var g = new FakeGarnish(); using var cts = new CancellationTokenSource();
            var aw = g.RunAsync(new GarnishRequest(null), cts.Token);
            Assert.IsTrue(g.IsBusy);
            cts.Cancel();
            Assert.IsTrue(aw.GetAwaiter().IsCompleted,"OCE is thrown synchronously (IGarnishHost timing)");
            Assert.IsFalse(g.IsBusy);
        }

        [Test]
        public async Task DayUi_TransitionCompleteAndCancel()
        {
            var ui = new FakeDayUi();
            var aw = ui.WaitTransitionAsync(CancellationToken.None);
            Assert.IsTrue(ui.TransitionPending);
            ui.CompleteTransition();
            await aw;

            using var cts = new CancellationTokenSource();
            var aw2 = ui.ShowDayEventsAsync(cts.Token);
            cts.Cancel();
            try { await aw2; Assert.Fail("expected OCE"); } catch (OperationCanceledException) { }
            CollectionAssert.AreEqual(new[] { "WaitTransition", "ShowDayEvents" }, ui.Calls);
            Assert.IsFalse(ui.TransitionPending); Assert.IsFalse(ui.EventsPending);
        }

        [Test]
        public async Task DayUi_NoWaitTitle_IsAlreadyComplete()
        {
            var ui = new FakeDayUi();
            var aw = ui.ShowChapterTitleAsync("k", false, CancellationToken.None);
            await aw; // would hang if it needed a frame or an external Complete
        }

        [Test]
        public void Minigame_RunWhileBusy_Throws()
        {
            var m = new FakeMinigames();
            var req = new MinigameRequest(Enum_MiniGameType.Shaking, Enum_MiniGameType.Shaking, false);
            m.RunAsync(req, CancellationToken.None);
            Assert.Throws<InvalidOperationException>(() => m.RunAsync(req, CancellationToken.None));
            m.Complete(MinigameOutcome.Completed); // release the first so nothing hangs
        }

        [Test]
        public void Orders_CommitServe_IsIdempotent()
        {
            var o = new FakeOrders { NextScore = new ScoreResult { payout = 5f } };
            int scored = 0; o.Scored += _ => scored++;
            var a = o.CommitServe();
            o.NextScore = new ScoreResult { payout = 9f };
            var b = o.CommitServe();
            Assert.AreEqual(1, scored);
            Assert.AreEqual(5f, a.payout); Assert.AreEqual(5f, b.payout);
            CollectionAssert.AreEqual(new[] { "CommitServe", "CommitServe" }, o.Calls);
        }
    }
}
