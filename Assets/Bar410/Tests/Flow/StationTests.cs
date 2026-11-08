using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bar410.Contracts;
using Bar410.Flow.Tests.Fakes;
using NUnit.Framework;
using UnityEngine;

namespace Bar410.Flow.Tests
{
    // Garnish and Serve stations run alone with fake ports. Completion sources continue inline (G1-0 S1),
    // so every assertion right after a Complete/Handle/Cancel sees the finished run without awaiting a frame.
    public class StationTests
    {
        GameObject _go;
        CancellationTokenSource _session, _lifetime;
        FlowPorts _p;
        FlowState _state;

        FakeBar Bar => (FakeBar)_p.Bar;
        FakeCamera Cam => (FakeCamera)_p.Camera;
        FakeGarnish Garnish => (FakeGarnish)_p.Garnish;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("StationTests");
            _lifetime = new CancellationTokenSource();
            _session = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            _p = FakePorts.Build();
            _state = new FlowState();
            _state.SetPart(DayPart.Open);
        }

        [TearDown]
        public void TearDown()
        {
            _session.Cancel(); _session.Dispose(); _lifetime.Dispose();
            UnityEngine.Object.DestroyImmediate(_go);
        }

        StepContext Ctx => new StepContext(_state, _p, _session.Token, _lifetime.Token);

        internal static async Task<Exception> Catch(Func<Task> body)
        {
            try { await body(); return null; } catch (Exception e) { return e; }
        }

        // ---------- Garnish ----------

        [Test]
        public async Task Garnish_SetsLabel_OpensInOrder_FinishedWithIce_Continues()
        {
            var st = _go.AddComponent<GarnishStation>();
            var drink = ScriptableObject.CreateInstance<S_Drink>();
            Bar.Drink = drink;
            var run = st.RunAsync(Ctx);
            Assert.AreEqual("Garnish", _state.StepString);
            CollectionAssert.AreEqual(new[] { "ResolveDrink", "LockIngredients", "SetOrderNoteAvailable(True)" }, Bar.Calls);
            CollectionAssert.AreEqual(new[] { "Show(Garnish)" }, Cam.Calls);
            Assert.AreSame(drink, Garnish.LastRequest.Value.Drink);
            Assert.AreEqual(0, Garnish.LastRequest.Value.SeatIndex);

            Garnish.CompleteGarnish(GarnishExit.Finished, new GarnishDecoration { ice = true });
            Assert.AreEqual(Next.Continue, await run);
            CollectionAssert.AreEqual(new[] { "SetIce(True)", "SetOrderNoteAvailable(False)" }, Bar.Calls.Skip(3));
            UnityEngine.Object.DestroyImmediate(drink);
        }

        [Test]
        public async Task Garnish_Remake_NoIce()
        {
            var st = _go.AddComponent<GarnishStation>();
            var run = st.RunAsync(Ctx);
            Garnish.CompleteGarnish(GarnishExit.Remake);
            Assert.AreEqual(Next.Remake, await run);
            Assert.IsFalse(Bar.Calls.Any(c => c.StartsWith("SetIce")));
        }

        [Test]
        public async Task Garnish_ForcedDone_DefaultDecoration_NoIce()
        {
            var st = _go.AddComponent<GarnishStation>();
            var run = st.RunAsync(Ctx);
            Assert.IsTrue(st.Handle(StepCommand.GarnishDone));
            Assert.IsFalse(Garnish.IsBusy, "host run is cancelled by the force");
            Assert.AreEqual(Next.Continue, await run);
            CollectionAssert.Contains(Bar.Calls, "SetIce(False)");
            Assert.IsFalse(st.Handle(StepCommand.GarnishDone), "not running any more");
        }

        [Test]
        public async Task Garnish_ForcedRemake()
        {
            var st = _go.AddComponent<GarnishStation>();
            var run = st.RunAsync(Ctx);
            Assert.IsTrue(st.Handle(StepCommand.Remake));
            Assert.AreEqual(Next.Remake, await run);
        }

        [Test]
        public void Garnish_Handle_WrongCommandOrIdle_False()
        {
            var st = _go.AddComponent<GarnishStation>();
            Assert.IsFalse(st.Handle(StepCommand.GarnishDone));
            st.RunAsync(Ctx);
            Assert.IsFalse(st.Handle(StepCommand.ServeDone));
            Assert.IsFalse(st.Handle(StepCommand.DrinkComplete));
            Assert.IsTrue(Garnish.IsBusy);
        }

        [Test]
        public async Task Garnish_SessionCancel_ThrowsOce_RestoresNote()
        {
            var st = _go.AddComponent<GarnishStation>();
            var run = st.RunAsync(Ctx);
            _session.Cancel();
            Assert.IsInstanceOf<OperationCanceledException>(await Catch(async () => await run));
            Assert.AreEqual("SetOrderNoteAvailable(False)", Bar.Calls.Last());
            Assert.IsFalse(st.Handle(StepCommand.GarnishDone));
        }

        [Test]
        public async Task Garnish_LifetimeCancel_SkipsRestore()
        {
            var st = _go.AddComponent<GarnishStation>();
            var run = st.RunAsync(Ctx);
            _lifetime.Cancel();
            Assert.IsInstanceOf<OperationCanceledException>(await Catch(async () => await run));
            Assert.AreEqual("SetOrderNoteAvailable(True)", Bar.Calls.Last());
        }

        // ---------- Serve ----------

        [Test]
        public async Task Serve_SetsLabel_Served_Continues_ShowsBar()
        {
            var st = _go.AddComponent<ServeStation>();
            var run = st.RunAsync(Ctx);
            Assert.AreEqual("Serve", _state.StepString);
            CollectionAssert.AreEqual(new[] { "ServeAsync" }, Garnish.Calls);
            Assert.IsEmpty(Cam.Calls);
            Garnish.CompleteServe(ServeDecision.Served);
            Assert.AreEqual(Next.Continue, await run);
            CollectionAssert.AreEqual(new[] { "Show(Bar)" }, Cam.Calls);
        }

        [Test]
        public async Task Serve_Remake()
        {
            var st = _go.AddComponent<ServeStation>();
            var run = st.RunAsync(Ctx);
            Garnish.CompleteServe(ServeDecision.Remake);
            Assert.AreEqual(Next.Remake, await run);
        }

        [Test]
        public async Task Serve_ForcedDone_And_ForcedRemake()
        {
            var st = _go.AddComponent<ServeStation>();
            var run = st.RunAsync(Ctx);
            Assert.IsFalse(st.Handle(StepCommand.GarnishDone));
            Assert.IsTrue(st.Handle(StepCommand.ServeDone));
            Assert.IsFalse(Garnish.IsBusy);
            Assert.AreEqual(Next.Continue, await run);

            run = st.RunAsync(Ctx);
            Assert.IsTrue(st.Handle(StepCommand.Remake));
            Assert.AreEqual(Next.Remake, await run);
        }

        [Test]
        public async Task Serve_LifetimeCancel_SkipsRestore()
        {
            var st = _go.AddComponent<ServeStation>();
            var run = st.RunAsync(Ctx);
            _lifetime.Cancel();
            Assert.IsInstanceOf<OperationCanceledException>(await Catch(async () => await run));
            Assert.IsEmpty(Cam.Calls);
        }
    }
}
