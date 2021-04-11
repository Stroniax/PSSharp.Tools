﻿using System;

namespace PSSharp
{
    public interface IPSObservable<out T>
    {
        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving 
        /// notifications before the provider has finished sending them.</returns>
        IDisposable Subscribe(IPSObserver<T> observer);
    }
}
