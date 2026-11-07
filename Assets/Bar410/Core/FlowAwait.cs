using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace Bar410.Core
{
    /// <summary>
    /// Waits on completion sources with cancellation, and runs fire-and-forget work without losing exceptions.
    /// Rules (G1-0 U2): never await the same <see cref="Awaitable"/> twice (a completed one throws NRE because it is
    /// pooled; a second awaiter on a pending one silently loses the first awaiter). Use a new source per wait.
    /// </summary>
    public static class FlowAwait
    {
        /// <summary>Awaits <paramref name="source"/>; cancelling <paramref name="ct"/> (also before the call) throws OCE.</summary>
        public static async Awaitable<T> Wait<T>(AwaitableCompletionSource<T> source, CancellationToken ct)
        {
            using (ct.Register(() => source.TrySetCanceled()))
                return await source.Awaitable;
        }

        /// <inheritdoc cref="Wait{T}"/>
        public static async Awaitable Wait(AwaitableCompletionSource source, CancellationToken ct)
        {
            using (ct.Register(() => source.TrySetCanceled()))
                await source.Awaitable;
        }

        /// <summary>
        /// Fire-and-forget. Silent on <see cref="OperationCanceledException"/>; any other exception is logged once
        /// as one <see cref="LogType.Exception"/> entry. Unity shows the original exception's message ("Type: msg"),
        /// and the label appears in the details as "Rethrow as Exception: [label] ...". Never rethrows.
        /// </summary>
        public static async void Run(Func<CancellationToken, Awaitable> work, CancellationToken ct, string label, UnityEngine.Object context = null)
        {
            try { await work(ct); }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(new Exception($"[{label}] {e.GetType().Name}: {e.Message}", e), context); }
        }

        /// <summary>
        /// Runs <paramref name="routine"/> on <paramref name="runner"/> and completes when it ends.
        /// On <paramref name="ct"/>: stops waiting and throws OCE; the coroutine itself runs to its end.
        /// A pre-cancelled token throws without starting the coroutine.
        /// If the runner is destroyed or inactive the coroutine stops (or never starts) and only <paramref name="ct"/>
        /// releases the waiter, so pass <c>runner.destroyCancellationToken</c> (or a token linked to it).
        /// </summary>
        public static Awaitable FromCoroutine(MonoBehaviour runner, IEnumerator routine, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var source = new AwaitableCompletionSource();
            runner.StartCoroutine(Drive(routine, source));
            return Wait(source, ct);
        }

        // Drives the routine itself so a routine with no yields completes inside StartCoroutine.
        static IEnumerator Drive(IEnumerator routine, AwaitableCompletionSource source)
        {
            while (routine.MoveNext()) yield return routine.Current;
            source.TrySetResult();
        }
    }
}
