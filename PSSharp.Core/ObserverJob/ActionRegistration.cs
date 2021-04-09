using System;

namespace PSSharp
{
    /// <summary>
    /// Invokes an action when <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    internal class ActionRegistration : IDisposable
    {
        /// <inheritdoc cref="ActionRegistration"/>
        /// <param name="action"><inheritdoc cref="_action" path="/summary"/></param>
        public ActionRegistration(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }
        /// <summary>
        /// The <see cref="Action"/> invoked when <see cref="IDisposable.Dispose"/> is called on this registration.
        /// </summary>
        private readonly Action _action;
        /// <summary>
        /// Invokes the <see cref="Action"/> subscribed to this registration.
        /// </summary>
        void IDisposable.Dispose()
        {
            _action();
        }
    }
}
