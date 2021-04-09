using System;
using System.Threading;

namespace PSSharp
{
    /// <summary>
    /// A <see cref="IObserver{T}"/> that invokes a series of actions provided to the observer when the
    /// corresponding <see cref="IObserver{T}"/> methods are called, providing additional caller information
    /// and allowing a cancellation method.
    /// <para>
    /// Note that each instance is meant to only be assigned to a single <see cref="IObservable{T}"/> source.
    /// Subscribing to multiple sources will result in invalid data being passed through to the actions
    /// being invoked by the observer.
    /// </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ActionObserver<T> : IObserver<T>, IDisposable
    {
        /// <summary>
        /// An action invoked immediately prior to subscription of an <see cref="ActionObserver{T}"/>
        /// to the <see cref="IObservable{T}"/> publisher.
        /// </summary>
        /// <param name="observer">The observer to be subscribed.</param>
        /// <param name="publisher">The data source being subscribed to.</param>
        internal delegate void OnSubscribedAction(IObserver<T> observer, IObservable<T> publisher);
        /// <summary>
        /// An action invoked when <see cref="IObserver{T}.OnNext(T)"/> is executed,
        /// including data about the <see cref="IObserver{T}"/> that witnessed the item
        /// and the <see cref="IObservable{T}"/> that published the item.
        /// </summary>
        /// <param name="observer">The observer that called the action.</param>
        /// <param name="publisher">The observable source that published the item.</param>
        /// <param name="next">The item that was published.</param>
        internal delegate void OnNextAction(IObserver<T> observer, IObservable<T> publisher, T next);
        /// <summary>
        /// An action invoked when <see cref="IObserver{T}.OnError(Exception)"/> is executed,
        /// including data about the <see cref="IObserver{T}"/> that witnessed the error
        /// and the <see cref="IObservable{T}"/> that experienced the error.
        /// </summary>
        /// <param name="observer">The observer that called the action.</param>
        /// <param name="publisher">The observable source that experienced the error.</param>
        /// <param name="error">The exception raised by the <see cref="IObservable{T}"/>.</param>
        internal delegate void OnErrorAction(IObserver<T> observer, IObservable<T> publisher, Exception error);
        /// <summary>
        /// An action invoked when <see cref="IObserver{T}.OnCompleted"/> is executed,
        /// including data about the <see cref="IObserver{T}"/> that witnessed the conclusion
        /// and the <see cref="IObservable{T}"/> that completed.
        /// </summary>
        /// <param name="observer">The observer that called the action.</param>
        /// <param name="publisher">The observable source that completed.</param>
        internal delegate void OnCompletedAction(IObserver<T> observer, IObservable<T> publisher);
        /// <summary>
        /// An action invoked when the <see cref="IDisposable"/> returned by 
        /// <see cref="IObservable{T}.Subscribe(IObserver{T})"/> is disposed of,
        /// including data about the <see cref="IObserver{T}"/> that was canceled
        /// and the <see cref="IObservable{T}"/> that was being observed.
        /// </summary>
        /// <param name="observer">The observer that was canceled.</param>
        /// <param name="publisher">The observable source that is no longer being observed.</param>
        internal delegate void OnCancelledAction(IObserver<T> observer, IObservable<T> publisher);

        /// <summary>
        /// Adds a subscriber to <paramref name="source"/> that invokes the associated actions when called
        /// by <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The data publisher to be observed.</param>
        /// <param name="onSubscribed"><inheritdoc cref="OnSubscribedAction" path="/summary"/></param>
        /// <param name="onNext"><inheritdoc cref="OnNextAction" path="/summary"/></param>
        /// <param name="onError"><inheritdoc cref="OnErrorAction" path="/summary"/></param>
        /// <param name="onCompleted"><inheritdoc cref="OnCompletedAction" path="/summary"/></param>
        /// <param name="onCanceled"><inheritdoc cref="OnCancelledAction" path="/summary"/></param>
        /// <param name="cancellationToken">Used to propogate cancellation to the <see cref="IObserver{T}"/>.</param>
        public static void Subscribe(
            IObservable<T> source,
            OnSubscribedAction? onSubscribed,
            OnNextAction? onNext,
            OnErrorAction? onError,
            OnCompletedAction? onCompleted,
            OnCancelledAction? onCanceled,
            CancellationToken cancellationToken = default)
            => new ActionObserver<T>(source, onSubscribed, onNext, onError, onCompleted, onCanceled, cancellationToken);

        /// <summary>
        /// Adds a subscriber to <paramref name="source"/> that invokes the associated actions when called
        /// by <paramref name="source"/>.
        /// </summary>
        /// <param name="source"><inheritdoc cref="Subscribe(IObservable{T}, OnSubscribedAction, OnNextAction?, OnErrorAction?, OnCompletedAction?, OnCancelledAction?, CancellationToken)"/></param>
        /// <param name="onSubscribed"><inheritdoc cref="OnSubscribedAction" path="/summary"/></param>
        /// <param name="onNext"><inheritdoc cref="OnNextAction" path="/summary"/></param>
        /// <param name="onError"><inheritdoc cref="OnErrorAction" path="/summary"/></param>
        /// <param name="onCompleted"><inheritdoc cref="OnCompletedAction" path="/summary"/></param>
        /// <param name="onCanceled"><inheritdoc cref="OnCancelledAction" path="/summary"/></param>
        public static IDisposable Subscribe(
            IObservable<T> source,
            OnSubscribedAction? onSubscribed,
            OnNextAction? onNext,
            OnErrorAction? onError,
            OnCompletedAction? onCompleted,
            OnCancelledAction? onCanceled)
        {
            var obj = new ActionObserver<T>(source, onSubscribed, onNext, onError, onCompleted, onCanceled, CancellationToken.None);
            return new ActionRegistration(obj.OnCancelled);
        }
        /// <inheritdoc cref="Subscribe(IObservable{T}, OnSubscribedAction, OnNextAction?, OnErrorAction?, OnCompletedAction?, OnCancelledAction?, CancellationToken)"/>
        private ActionObserver(
            IObservable<T> source,
            OnSubscribedAction? onSubscribed,
            OnNextAction? onNext,
            OnErrorAction? onError,
            OnCompletedAction? onCompleted,
            OnCancelledAction? onCanceled,
            CancellationToken cancellationToken)
        {
            _source = source;
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
            _onCanceled = onCanceled;
            _cancellation = cancellationToken;
            onSubscribed?.Invoke(this, source);
            _cancellationRegistration = _cancellation.Register(OnCancelled);
            _observerRegistration = _source.Subscribe(this);
        }

        private readonly OnNextAction? _onNext;
        private readonly OnErrorAction? _onError;
        private readonly OnCompletedAction? _onCompleted;
        private readonly OnCancelledAction? _onCanceled;
        private readonly IObservable<T> _source;
        private readonly IDisposable _observerRegistration;
        private readonly CancellationTokenRegistration _cancellationRegistration;
        private readonly CancellationToken _cancellation;

        void IObserver<T>.OnCompleted()
        {
            _onCompleted?.Invoke(this, _source);
        }

        void IObserver<T>.OnError(Exception error)
        {
            _onError?.Invoke(this, _source, error);
        }

        void IObserver<T>.OnNext(T value)
        {
            _onNext?.Invoke(this, _source, value);
        }

        /// <summary>
        /// Disposes of the <see cref="IDisposable"/> created during subscription and executes the 
        /// <see cref="OnCancelledAction"/> passed to this instance's constructor.</summary>
        private void OnCancelled()
        {
            _observerRegistration?.Dispose();
            _onCanceled?.Invoke(this, _source);
        }
        /// <summary>
        /// Disposes of managed resources, the observer registration, and <see cref="CancellationTokenRegistration"/> if applicable.
        /// </summary>
        void IDisposable.Dispose()
        {
            _observerRegistration.Dispose();
            _cancellationRegistration.Dispose();
        }
    }
}
