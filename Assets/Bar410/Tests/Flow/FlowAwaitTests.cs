using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bar410.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Bar410.Flow.Tests
{
    // Rules: every source is resolved or cancelled before it is awaited (a pending await hangs the run).
    // Not tested: awaiting the same Awaitable twice (G1-0 U2). Completed-twice depends on the Awaitable pool,
    // pending-twice hangs; neither is deterministic, so the rule lives in FlowAwait's doc comment.
    public class FlowAwaitTests
    {
        static async Task<Exception> Catch(Func<Task> body)
        {
            try { await body(); return null; }
            catch (Exception e) { return e; }
        }

        static void AssertOce(Exception e) => Assert.IsInstanceOf<OperationCanceledException>(e, e?.ToString() ?? "no exception");

        // ---------- Wait ----------

        [Test]
        public async Task Wait_PassesResultThrough()
        {
            var src = new AwaitableCompletionSource<int>();
            var aw = FlowAwait.Wait(src, CancellationToken.None);
            src.SetResult(42);
            Assert.AreEqual(42, await aw);
        }

        [Test]
        public async Task Wait_NonGeneric_Completes()
        {
            var src = new AwaitableCompletionSource();
            var aw = FlowAwait.Wait(src, CancellationToken.None);
            src.SetResult();
            Assert.IsNull(await Catch(async () => await aw));
        }

        [Test]
        public async Task Wait_CancelWhilePending_ThrowsOce()
        {
            using var cts = new CancellationTokenSource();
            var src = new AwaitableCompletionSource<int>();
            var aw = FlowAwait.Wait(src, cts.Token);
            cts.Cancel();
            AssertOce(await Catch(async () => await aw));
        }

        [Test]
        public async Task Wait_NonGeneric_CancelWhilePending_ThrowsOce()
        {
            using var cts = new CancellationTokenSource();
            var src = new AwaitableCompletionSource();
            var aw = FlowAwait.Wait(src, cts.Token);
            cts.Cancel();
            AssertOce(await Catch(async () => await aw));
        }

        [Test]
        public async Task Wait_PreCancelledToken_ThrowsOce()
        {
            var src = new AwaitableCompletionSource<int>();
            var aw = FlowAwait.Wait(src, new CancellationToken(true));
            AssertOce(await Catch(async () => await aw));
        }

        [Test]
        public async Task Wait_CancelAfterResult_KeepsResult()
        {
            using var cts = new CancellationTokenSource();
            var src = new AwaitableCompletionSource<int>();
            var aw = FlowAwait.Wait(src, cts.Token);
            src.SetResult(7);
            cts.Cancel();
            Assert.AreEqual(7, await aw);
        }

        // ---------- Run ----------

        [Test]
        public void Run_SynchronousThrow_LogsOnce()
        {
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: boom"));
            FlowAwait.Run(_ => throw new InvalidOperationException("boom"), CancellationToken.None, "sync-label");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Run_ThrowAfterAwait_LogsOnce()
        {
            var src = new AwaitableCompletionSource();
            FlowAwait.Run(async ct =>
            {
                await FlowAwait.Wait(src, ct);
                throw new InvalidOperationException("late");
            }, CancellationToken.None, "late-label");
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: late"));
            src.SetResult(); // continuation runs inline (G1-0 S1), so the log is synchronous
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Run_Cancelled_IsSilent()
        {
            using var cts = new CancellationTokenSource();
            var src = new AwaitableCompletionSource();
            bool reached = false;
            FlowAwait.Run(async ct => { await FlowAwait.Wait(src, ct); reached = true; }, cts.Token, "cancel-label");
            cts.Cancel();
            Assert.IsFalse(reached);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Run_ThrowsOceDirectly_IsSilent()
        {
            FlowAwait.Run(_ => throw new OperationCanceledException(), CancellationToken.None, "oce-label");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Run_Success_RunsWork()
        {
            var src = new AwaitableCompletionSource();
            bool done = false;
            FlowAwait.Run(async ct => { await FlowAwait.Wait(src, ct); done = true; }, CancellationToken.None, "ok");
            src.SetResult();
            Assert.IsTrue(done);
            LogAssert.NoUnexpectedReceived();
        }

        // ---------- Forcible ----------

        [Test]
        public async Task Forcible_HostResult_PassesThrough()
        {
            using var f = new Forcible<int>(CancellationToken.None);
            var src = new AwaitableCompletionSource<int>();
            var aw = f.Await(FlowAwait.Wait(src, f.Token));
            src.SetResult(3);
            Assert.AreEqual(3, await aw);
            Assert.IsFalse(f.IsForced);
        }

        [Test]
        public async Task Forcible_Force_ReturnsForcedValue()
        {
            using var f = new Forcible<int>(CancellationToken.None);
            var src = new AwaitableCompletionSource<int>();
            var aw = f.Await(FlowAwait.Wait(src, f.Token));
            Assert.IsTrue(f.TryForce(7));
            Assert.IsTrue(f.IsForced);
            Assert.AreEqual(7, await aw);
        }

        [Test]
        public async Task Forcible_SecondForce_ReturnsFalse_KeepsFirstValue()
        {
            using var f = new Forcible<int>(CancellationToken.None);
            var src = new AwaitableCompletionSource<int>();
            var aw = f.Await(FlowAwait.Wait(src, f.Token));
            Assert.IsTrue(f.TryForce(7));
            Assert.IsFalse(f.TryForce(8));
            Assert.AreEqual(7, await aw);
        }

        [Test]
        public async Task Forcible_ForceAfterComplete_ReturnsFalse()
        {
            using var f = new Forcible<int>(CancellationToken.None);
            var src = new AwaitableCompletionSource<int>();
            var aw = f.Await(FlowAwait.Wait(src, f.Token));
            src.SetResult(3);
            Assert.IsFalse(f.TryForce(7));
            Assert.IsFalse(f.IsForced);
            Assert.AreEqual(3, await aw);
        }

        [Test]
        public async Task Forcible_HostIgnoresToken_ReturnsForcedValue()
        {
            using var f = new Forcible<int>(CancellationToken.None);
            var src = new AwaitableCompletionSource<int>();
            var aw = f.Await(src.Awaitable); // host never looks at f.Token
            Assert.IsTrue(f.TryForce(7));
            src.SetResult(3);
            Assert.AreEqual(7, await aw);
        }

        [Test]
        public async Task Forcible_OuterCancelled_ThrowsOce_AndForceReturnsFalse()
        {
            using var outer = new CancellationTokenSource();
            using var f = new Forcible<int>(outer.Token);
            var src = new AwaitableCompletionSource<int>();
            var aw = f.Await(FlowAwait.Wait(src, f.Token));
            outer.Cancel();
            Assert.IsFalse(f.TryForce(7));
            AssertOce(await Catch(async () => await aw));
        }

        [Test]
        public async Task Forcible_OuterCancelAfterForce_HostIgnoresToken_OuterWins()
        {
            using var outer = new CancellationTokenSource();
            using var f = new Forcible<int>(outer.Token);
            var src = new AwaitableCompletionSource<int>();
            var aw = f.Await(src.Awaitable);
            Assert.IsTrue(f.TryForce(7));
            outer.Cancel();
            src.SetResult(3);
            AssertOce(await Catch(async () => await aw));
        }

        [Test]
        public async Task Forcible_OuterCancelAfterForce_HostThrowsOce_OuterWins()
        {
            using var outer = new CancellationTokenSource();
            using var f = new Forcible<int>(outer.Token);
            var src = new AwaitableCompletionSource<int>();
            var aw = f.Await(src.Awaitable);
            Assert.IsTrue(f.TryForce(7));
            outer.Cancel();
            src.SetCanceled();
            AssertOce(await Catch(async () => await aw));
        }

        [Test]
        public async Task Forcible_HostThrows_PropagatesException()
        {
            using var f = new Forcible<int>(CancellationToken.None);
            var src = new AwaitableCompletionSource<int>();
            var aw = f.Await(src.Awaitable);
            src.SetException(new InvalidOperationException("host"));
            Assert.IsInstanceOf<InvalidOperationException>(await Catch(async () => await aw));
        }

        [Test]
        public void Forcible_AfterDispose_TryForceReturnsFalse_TokenStillReadable()
        {
            var f = new Forcible<int>(CancellationToken.None);
            f.Dispose();
            Assert.IsFalse(f.TryForce(7));
            Assert.DoesNotThrow(() => { var _ = f.Token; });
            Assert.DoesNotThrow(f.Dispose);
        }

        // ---------- FromCoroutine ----------

        GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        CoroutineRunnerForTests NewRunner()
        {
            _go = new GameObject("FlowAwaitTests.Runner");
            return _go.AddComponent<CoroutineRunnerForTests>();
        }

        static IEnumerator NoYields(Action onEnd) { onEnd(); yield break; }
        static IEnumerator Forever() { while (true) yield return null; }

        [Test]
        public async Task FromCoroutine_NoYieldRoutine_Completes()
        {
            bool ended = false;
            var aw = FlowAwait.FromCoroutine(NewRunner(), NoYields(() => ended = true), CancellationToken.None);
            Assert.IsTrue(ended);
            Assert.IsNull(await Catch(async () => await aw));
        }

        [Test]
        public async Task FromCoroutine_Cancel_ThrowsOce()
        {
            using var cts = new CancellationTokenSource();
            var aw = FlowAwait.FromCoroutine(NewRunner(), Forever(), cts.Token);
            cts.Cancel();
            AssertOce(await Catch(async () => await aw));
        }

        [Test]
        public void FromCoroutine_PreCancelled_ThrowsOce_WithoutStarting()
        {
            bool started = false;
            Assert.Throws<OperationCanceledException>(() =>
                FlowAwait.FromCoroutine(NewRunner(), NoYields(() => started = true), new CancellationToken(true)));
            Assert.IsFalse(started);
        }

        static IEnumerator Throws() { throw new InvalidOperationException("boom"); yield break; }

        [Test]
        public async Task FromCoroutine_ThrowingRoutine_AwaitThrowsThatException()
        {
            var aw = FlowAwait.FromCoroutine(NewRunner(), Throws(), CancellationToken.None);
            var e = await Catch(async () => await aw);
            Assert.IsInstanceOf<InvalidOperationException>(e);
            Assert.AreEqual("boom", e.Message);
        }

        [Test]
        public void FromCoroutine_InactiveRunner_Throws()
        {
            var r = NewRunner();
            _go.SetActive(false);
            Assert.Throws<InvalidOperationException>(() =>
                FlowAwait.FromCoroutine(r, NoYields(() => { }), CancellationToken.None));
        }

        // Not tested: a multi-frame routine. EditMode does not tick MonoBehaviour coroutines (tried: the routine never
        // advanced past its first yield in 30 editor frames). Needs a PlayMode test.
    }

    public class CoroutineRunnerForTests : MonoBehaviour { }
}
