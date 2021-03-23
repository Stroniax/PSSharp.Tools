using System;

namespace PSSharp
{
    internal class Subscriber : IDisposable
    {
        private readonly Action _action;
        public Subscriber(Action onDisposed)
        {
            _action = onDisposed;
        }
        public void Dispose()
        {
            _action?.Invoke();
        }
    }
}
