using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSSharp
{
    /// <summary>
    /// <see cref="IObserver{T}"/> implementation that can be awaited for <see cref="IObserver{T}.OnCompleted"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAwaitableObserver<in T> : IObserver<T>, IAwaitable
    {

    }
    internal class AwaitableActionObserver<T> : IAwaitableObserver<T>
    {
        private class Awaiter : IAwaiter
        {
            private object _syncRoot = new object();
            private bool _isCompleted;
            private Exception? _error;
            private ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();
            public bool IsCompleted
            {
                get
                {
                    lock (_syncRoot)
                    {
                        return _isCompleted;
                    }
                }
            }
            public Exception? Error
            {
                get
                {
                    lock (_syncRoot)
                    {
                        return _error;
                    }
                }
            }
            public void GetResult()
            {
                while (!IsCompleted) Thread.Sleep(0);
                if (Error != null) throw Error;
            }
            public void OnCompleted(Action continuation)
            {
                lock (_syncRoot)
                {
                    if (_isCompleted)
                    {
                        continuation?.Invoke();
                    }
                    else
                    {
                        _actions.Enqueue(continuation);
                    }
                }
            }
            internal void Complete(Exception? error)
            {
                if (IsCompleted) return; // ignore everything
                lock (_syncRoot)
                {
                    _isCompleted = true;
                    _error = error;
                    while (_actions.TryDequeue(out var action))
                    {
                        try
                        {
                            action?.Invoke();
                        }
                        catch { }
                    }
                }
            }
        }

        private readonly Awaiter _awaiter;
        private readonly ConcurrentQueue<T> _contents;
        private readonly Action<T> _action;
        private CancellationToken _cancellationToken;
        private IDisposable? _disposeSource;
        /// <inheritdoc/>
        public IAwaiter GetAwaiter() => _awaiter;
        /// <inheritdoc/>
        public void OnCompleted()
        {
            _awaiter.Complete(null);
            _disposeSource?.Dispose();
        }
        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            _awaiter.Complete(error);
            _disposeSource?.Dispose();
        }
        /// <inheritdoc/>
        public void OnNext(T value)
        {
            if (!_awaiter.IsCompleted)
            {
                _contents.Enqueue(value);
                _action.Invoke(value);
            }
        }
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="action">The action to be invoked on each observed item.</param>
        public AwaitableActionObserver(Action<T> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _contents = new ConcurrentQueue<T>();
            _awaiter = new Awaiter();
        }
        internal void AddCancellation(IDisposable cancelAction, CancellationToken cancellationToken)
        {
            _disposeSource = cancelAction;
            _cancellationToken = cancellationToken;
            _cancellationToken.Register(() =>
            {
                _disposeSource.Dispose();
                _awaiter.Complete(new OperationCanceledException());
            });
        }
    }
}
