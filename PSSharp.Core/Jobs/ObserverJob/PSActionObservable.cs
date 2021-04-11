using System;

namespace PSSharp
{
    internal class PSActionObservable<T> : IPSObservable<T>
    {
        public IDisposable Subscribe(IPSObserver<T> observer)
        {
            return _action.Invoke(observer);
        }

        private Func<IPSObserver<T>, IDisposable> _action;

        public PSActionObservable(Func<IPSObserver<T>, IDisposable> action)
        {
            _action = action;
        }
    }
}
