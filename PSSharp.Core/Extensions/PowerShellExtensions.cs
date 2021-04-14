using System;
using System.Management.Automation;
using System.Reactive.Linq;
using System.Text;
using System.Threading;

namespace PSSharp.Extensions
{
    public static class PowerShellExtensions
    {
        public static IObservable<T> ToObservable<T>(this PSDataCollection<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            return Observable.Create<T>((observer) =>
            {
                foreach (var item in source)
                {
                    observer.OnNext(item);
                }
                if (!source.IsOpen)
                {
                    observer.OnCompleted();
                    return ActionRegistration.None;
                }

                EventHandler<DataAddedEventArgs>? dataAdded = null;
                EventHandler? completed = null;
                bool isRemoved = false;
                dataAdded = (sender, args) =>
                {
                    observer.OnNext(source[args.Index]);
                };
                completed = (sender, args) =>
                {
                    lock (source)
                    {
                        if (!Volatile.Read(ref isRemoved))
                        {
                            observer.OnCompleted();
                            source.Completed -= completed;
                            source.DataAdded -= dataAdded;
                        }
                        Volatile.Write(ref isRemoved, true);
                    }
                };
                source.DataAdded += dataAdded;
                source.Completed += completed;

                return new ActionRegistration(() =>
                {
                    lock (source)
                    {
                        if (!Volatile.Read(ref isRemoved))
                        {
                            source.DataAdded -= dataAdded;
                            source.Completed -= completed;
                            Volatile.Write(ref isRemoved, true);
                        }
                    }
                });
            });
        }

        public static IObservable<PSObject> InvokeAsObservable(this PowerShell source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            var observable = new ObservablePowerShellInvocation<PSObject, PSObject>(source);
            return observable.Invoke();
        }
        public static IPSObservable<PSObject> InvokeAsPSObservable(this PowerShell source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            var observable = new ObservablePowerShellInvocation<PSObject, PSObject>(source);
            return observable.Invoke();
        }
        public static IObservable<PSObject> InvokeAsObservable<TInput>(this PowerShell source,
                                                                               PSDataCollection<TInput> input)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            var observable = new ObservablePowerShellInvocation<TInput, PSObject>(source)
            {
                Input = input ?? throw new ArgumentNullException(nameof(input)),
            };
            return observable.Invoke();
        }
        public static IPSObservable<PSObject> InvokeAsPSObservable<TInput>(this PowerShell source,
                                                                                   PSDataCollection<TInput> input)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            var observable = new ObservablePowerShellInvocation<TInput, PSObject>(source)
            {
                Input = input ?? throw new ArgumentNullException(nameof(input)),
            };
            return observable.Invoke();
        }
        public static IObservable<TOutput> InvokeAsObservable<TInput, TOutput>(this PowerShell source,
                                                                               PSDataCollection<TInput> input,
                                                                               PSDataCollection<TOutput> output)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            var observable = new ObservablePowerShellInvocation<TInput, TOutput>(source)
            { 
                Input = input ?? throw new ArgumentNullException(nameof(input)),
                Output = output ?? throw new ArgumentNullException(nameof(output))
            };
            return observable.Invoke();
        }
        public static IPSObservable<TOutput> InvokeAsPSObservable<TInput, TOutput>(this PowerShell source,
                                                                                   PSDataCollection<TInput> input,
                                                                                   PSDataCollection<TOutput> output)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            var observable = new ObservablePowerShellInvocation<TInput, TOutput>(source)
            {
                Input = input ?? throw new ArgumentNullException(nameof(input)),
                Output = output ?? throw new ArgumentNullException(nameof(output))
            };
            return observable.Invoke();
        }
    }
}
