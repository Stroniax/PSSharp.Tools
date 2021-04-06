using System;
using System.Threading;

namespace PSSharp
{
    namespace Extensions
    {
        /// <summary>
        /// Extension methods for <see cref="IObservable{T}"/>.
        /// </summary>
        public static class IObservableExtensions
        {
            /// <summary>
            /// Subscribes an <paramref name="action"/> to be executed on each item provided by the 
            /// <see cref="IObservable{T}"/> <paramref name="source"/>. The <see langword="await"/>
            /// keyword can be used on the <see cref="IAwaitableObserver{T}"/> returned.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="source">The item provider.</param>
            /// <param name="action">The action invoked by the subscriber.</param>
            /// <param name="cancellationToken">A cancellation token used to halt the returned <see cref="IAwaitableObserver{T}"/>.</param>
            /// <returns></returns>
            public static IAwaitableObserver<T> Subscribe<T>(this IObservable<T> source, Action<T> action, CancellationToken cancellationToken = default)
            {
                if (source is null) throw new ArgumentNullException(nameof(source));
                if (action is null) throw new ArgumentNullException(nameof(action));

                var observer = new AwaitableActionObserver<T>(action);
                var disposeSource = source.Subscribe(observer);
                observer.AddCancellation(disposeSource, cancellationToken);
                return observer;
            }
            /// <summary>
            /// Wraps the output of a <see cref="IObserver{T}"/> by creating a new <see cref="IObserver{T}"/>
            /// with a translation function between <typeparamref name="TSource"/> and 
            /// <typeparamref name="TResult"/>.
            /// </summary>
            /// <typeparam name="TSource">The result output by the initial <see cref="IObserver{T}"/></typeparam>
            /// <typeparam name="TResult">The result output from the new <see cref="IObservable{T}"/>, as defined
            /// by <paramref name="onNext"/>.</typeparam>
            /// <param name="source">The source <see cref="IObservable{T}"/> to be wrapped by <paramref name="onNext"/>.</param>
            /// <param name="onNext">Translates a <typeparamref name="TSource"/> instance
            /// to a <typeparamref name="TResult"/> instance for the wrapping 
            /// <see cref="IObservable{T}"/>.</param>
            /// <returns></returns>
            public static IObservable<TResult> WrapOutput<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> onNext)
                => new ObserverWrapper<TSource, TResult>(source ?? throw new ArgumentNullException(nameof(source)),
                                                         onNext ?? throw new ArgumentNullException(nameof(onNext)));
        }
    }
}
