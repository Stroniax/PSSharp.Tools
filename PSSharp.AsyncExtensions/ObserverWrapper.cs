using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace PSSharp
{
    internal class ObserverWrapper<TSource, TResult> : IObservable<TResult>, IObserver<TSource>
    {
        public ObserverWrapper(IObservable<TSource> source, Func<TSource, TResult> wrapperFunction)
        {
            _observers = new List<IObserver<TResult>>();
            _results = new ConcurrentQueue<TResult>();
            _syncRoot = new object();
            WrapperFunction = wrapperFunction;
            source.Subscribe(this);
        }
        private Func<TSource, TResult> WrapperFunction { get; }

        private List<IObserver<TResult>> _observers;
        private bool _isCompleted;
        private Exception? _error;
        private ConcurrentQueue<TResult> _results;
        private object _syncRoot;

        public void OnCompleted()
        {
            lock (_syncRoot)
            {
                _isCompleted = true;
                foreach (var observer in _observers)
                {
                    observer.OnCompleted();
                }
                _observers.Clear();
            }
        }

        public void OnError(Exception error)
        {
            lock (_syncRoot)
            {
                foreach (var observer in _observers)
                {
                    observer.OnError(error);
                }
                _observers.Clear();
            }
        }

        public void OnNext(TSource value)
        {
            lock (_syncRoot)
            {
                var convertedValue = WrapperFunction(value);
                _results.Enqueue(convertedValue);
                foreach (var observer in _observers)
                {
                    observer.OnNext(convertedValue);
                }
            }
        }

        public IDisposable Subscribe(IObserver<TResult> observer)
        {
            lock (_syncRoot)
            {
                foreach (var item in _results)
                {
                    observer.OnNext(item);
                }
                if (_isCompleted)
                {
                    observer.OnCompleted();
                }
                else
                {
                    _observers.Add(observer);
                }
                return new Subscriber(observer, _observers);
            }
        }
        private class Subscriber : IDisposable
        {
            public Subscriber(IObserver<TResult> observer, List<IObserver<TResult>> observers)
            {
                _onDisposed = () =>
                {
                    if (observers.Contains(observer)) observers.Remove(observer);
                };
            }
            private Action _onDisposed;
            public void Dispose()
            {
                _onDisposed();
            }
        }
    }
}
