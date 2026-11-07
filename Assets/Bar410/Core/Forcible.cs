using System;
using System.Threading;
using UnityEngine;

namespace Bar410.Core
{
    /// <summary>
    /// Lets a Yarn "press the button" command end a running host await with a chosen result.
    /// Pass <see cref="Token"/> to the host, wrap the host call in <see cref="Await"/>, call <see cref="TryForce"/> to end it.
    /// Outer cancellation always wins over a force. One instance per run; dispose it after the run.
    /// </summary>
    public sealed class Forcible<T> : IDisposable
    {
        readonly CancellationToken _outer;
        readonly CancellationTokenSource _linked;
        T _forced;
        bool _finished;

        public Forcible(CancellationToken outer)
        {
            _outer = outer;
            _linked = CancellationTokenSource.CreateLinkedTokenSource(outer);
            Token = _linked.Token; // captured: CancellationTokenSource.Token throws after Dispose
        }

        public CancellationToken Token { get; }
        public bool IsForced { get; private set; }

        /// <summary>
        /// False if already forced, finished, disposed, or the outer token is cancelled.
        /// Runs the awaiting continuation inline before it returns (G1-0 S1).
        /// </summary>
        public bool TryForce(T result)
        {
            if (IsForced || _finished || _outer.IsCancellationRequested) return false;
            _forced = result;
            IsForced = true;
            _linked.Cancel();
            return true;
        }

        /// <summary>
        /// Awaits the host. Forced (and outer not cancelled) → the forced value, even if the host ignored the token
        /// and returned normally. Outer cancelled → OCE. Otherwise the host's own result or exception.
        /// </summary>
        public async Awaitable<T> Await(Awaitable<T> running)
        {
            try
            {
                T result = await running;
                if (!IsForced) return result;
                _outer.ThrowIfCancellationRequested();
                return _forced;
            }
            catch (OperationCanceledException) when (IsForced && !_outer.IsCancellationRequested)
            {
                return _forced;
            }
            finally
            {
                _finished = true;
            }
        }

        /// <summary>Disposes the linked token source; later <see cref="TryForce"/> calls return false. Idempotent.</summary>
        public void Dispose()
        {
            _finished = true;
            _linked.Dispose();
        }
    }
}
