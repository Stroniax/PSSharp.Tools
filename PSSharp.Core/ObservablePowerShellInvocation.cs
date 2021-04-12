using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reactive.Linq;

namespace PSSharp
{
    internal class ObservablePowerShellInvocation<TInput, TOutput> : IObservable<TOutput>, IPSObservable<TOutput>, IDisposable
    {
        private PowerShell _powerShell;
        private readonly List<IObserver<TOutput>> _observers;
        private readonly List<IPSObserver<TOutput>> _psObservers;
        private readonly Queue<PSOutput<TOutput>> _outputCache;
        private readonly object _sync;
        private bool _isCompleted;
        private Exception? _exception;
        private PSDataCollection<TInput>? _input;
        private PSDataCollection<TOutput>? _output;
        private bool _shouldDisposeInput;
        private bool _shouldDisposeOutput;
        internal PSDataCollection<TInput> Input
        {
            get
            {
                return _input ??= new PSDataCollection<TInput>();
            }
            set
            {
                _input?.Dispose();
                _input = value;
                _shouldDisposeInput = false;
            }
        }
        internal PSDataCollection<TOutput> Output
        {
            get
            {
                return _output ??= new PSDataCollection<TOutput>();
            }
            set
            {
                _output?.Dispose();
                _output = value;
                _shouldDisposeOutput = false;
            }
        }
        private void OnOutputAdded(object sender, DataAddedEventArgs args)
        {
            lock (_sync)
            {
                var item = Output[args.Index];
                _outputCache.Enqueue(new PSOutput<TOutput>(item));
                foreach (var observer in _observers)
                {
                    observer.OnNext(item);
                }
                foreach (var observer in _psObservers)
                {
                    observer.OnOutput(item);
                }
            }
        }
        private void OnErrorAdded(object sender, DataAddedEventArgs args)
        {
            lock (_sync)
            {
                var er = _powerShell.Streams.Error[args.Index];
                _outputCache.Enqueue(new PSOutput<TOutput>(er));
                foreach (var observer in _psObservers)
                {
                    observer.OnError(er);
                }
            }
        }
        private void OnWarningAdded(object sender, DataAddedEventArgs args)
        {
            lock (_sync)
            {
                var warning = _powerShell.Streams.Warning[args.Index];
                _outputCache.Enqueue(new PSOutput<TOutput>(warning));
                foreach (var observer in _psObservers)
                {
                    observer.OnWarning(warning.Message);
                }
            }
        }
        private void OnVerboseAdded(object sender, DataAddedEventArgs args)
        {
            lock (_sync)
            {
                var verbose = _powerShell.Streams.Verbose[args.Index];
                _outputCache.Enqueue(new PSOutput<TOutput>(verbose));
                foreach (var observer in _psObservers)
                {
                    observer.OnVerbose(verbose.Message);
                }
            }
        }
        private void OnDebugAdded(object sender, DataAddedEventArgs args)
        {
            lock (_sync)
            {
                var debug = _powerShell.Streams.Debug[args.Index];
                _outputCache.Enqueue(new PSOutput<TOutput>(debug));
                foreach (var observer in _psObservers)
                {
                    observer.OnDebug(debug.Message);
                }
            }
        }
        private void OnProgressAdded(object sender, DataAddedEventArgs args)
        {
            lock (_sync)
            {
                var prog = _powerShell.Streams.Progress[args.Index];
                _outputCache.Enqueue(new PSOutput<TOutput>(prog));
                foreach (var observer in _psObservers)
                {
                    observer.OnProgress(prog);
                }
            }
        }
        private void OnInformationAdded(object sender, DataAddedEventArgs args)
        {
            lock (_sync)
            {
                var info = _powerShell.Streams.Information[args.Index];
                _outputCache.Enqueue(new PSOutput<TOutput>(info));
                foreach (var observer in _psObservers)
                {
                    observer.OnInformation(info);
                }
            }
        }
        private void OnStateChanged(object sender, PSInvocationStateChangedEventArgs args)
        {
            lock (_sync)
            {
                switch (args.InvocationStateInfo.State)
                {
                    case PSInvocationState.Completed:
                        {
                            foreach (var observer in _observers)
                            {
                                observer.OnCompleted();
                            }
                            foreach (var observer in _psObservers)
                            {
                                observer.OnCompleted();
                            }
                        }
                        break;
                    case PSInvocationState.Disconnected:
                        {
                            var ex = new PSTerminalStateException(PSInvocationState.Disconnected, "The PowerShell invocation state indicated disconnection.", args.InvocationStateInfo.Reason);
                            var er = new ErrorRecord(ex, "PSTerminalState", ErrorCategory.NotSpecified, _powerShell);
                            foreach (var observer in _observers)
                            {
                                observer.OnError(ex);
                            }
                            foreach (var observer in _psObservers)
                            {
                                observer.OnFailed(er);
                            }
                        }
                        break;
                    case PSInvocationState.Failed:
                        {
                            var ex = new PSTerminalStateException(PSInvocationState.Failed, "The PowerShell invocation state indicated failure.", args.InvocationStateInfo.Reason);
                            var er = new ErrorRecord(ex, "PSTerminalState", ErrorCategory.NotSpecified, _powerShell);
                            _exception = ex;
                            foreach (var observer in _observers)
                            {
                                observer.OnError(ex);
                            }
                            foreach (var observer in _psObservers)
                            {
                                observer.OnFailed(er);
                            }
                        }
                        break;
                    case PSInvocationState.Stopped:
                        {
                            var ex = new PSTerminalStateException(PSInvocationState.Stopped, "The PowerShell invocation state indicated cancellation.", args.InvocationStateInfo.Reason);
                            var er = new ErrorRecord(ex, "PSTerminalState", ErrorCategory.NotSpecified, _powerShell);
                            _exception = ex;
                            foreach (var observer in _observers)
                            {
                                observer.OnError(ex);
                            }
                            foreach (var observer in _psObservers)
                            {
                                observer.OnFailed(er);
                            }
                        }
                        break;
                    default: return;
                }
                _observers.Clear();
                _psObservers.Clear();
                Output.DataAdded -= OnOutputAdded;
                _powerShell.Streams.Error.DataAdded -= OnErrorAdded;
                _powerShell.Streams.Warning.DataAdded -= OnWarningAdded;
                _powerShell.Streams.Verbose.DataAdded -= OnVerboseAdded;
                _powerShell.Streams.Debug.DataAdded -= OnDebugAdded;
                _powerShell.Streams.Progress.DataAdded -= OnProgressAdded;
                _powerShell.Streams.Information.DataAdded -= OnInformationAdded;
                Dispose();
            }
        }
        internal ObservablePowerShellInvocation(PowerShell powerShell)
        {
            _powerShell = powerShell;
            _shouldDisposeInput = true;
            _shouldDisposeOutput = true;
            _sync = new object();
            _observers = new List<IObserver<TOutput>>();
            _psObservers = new List<IPSObserver<TOutput>>();
            _outputCache = new Queue<PSOutput<TOutput>>();
        }
        internal ObservablePowerShellInvocation<TInput, TOutput> Invoke()
        {
            Output.DataAdded += OnOutputAdded;
            _powerShell.Streams.Information.DataAdded += OnInformationAdded;
            _powerShell.Streams.Error.DataAdded += OnErrorAdded;
            _powerShell.Streams.Warning.DataAdded += OnWarningAdded;
            _powerShell.Streams.Verbose.DataAdded += OnVerboseAdded;
            _powerShell.Streams.Debug.DataAdded += OnDebugAdded;
            _powerShell.Streams.Progress.DataAdded += OnProgressAdded;

            _powerShell.InvocationStateChanged += OnStateChanged;
            _powerShell.BeginInvoke(Input, Output);
            return this;
        }
        public IDisposable Subscribe(IObserver<TOutput> observer)
        {
            lock (_sync)
            {
                foreach (var item in _outputCache)
                {
                    switch (item.Stream)
                    {
                        case PowerShellStreamType.Output:
                            observer.OnNext(item.Output);
                            break;
                    }
                }
                if (_exception != null)
                {
                    observer.OnError(_exception);
                    return ActionRegistration.None;
                }
                if (_isCompleted)
                {
                    observer.OnCompleted();
                    return ActionRegistration.None;
                }
                _observers.Add(observer);
                return new ActionRegistration(() =>
                {
                    lock (_sync)
                    {
                        if (_observers.Contains(observer))
                        {
                            _observers.Remove(observer);
                        }
                    }
                });
            }
        }
        public IDisposable Subscribe(IPSObserver<TOutput> observer)
        {
            lock (_sync)
            {
                foreach (var item in _outputCache)
                {
                    switch (item.Stream)
                    {
                        case PowerShellStreamType.Output:
                            observer.OnOutput(item.Output);
                            break;
                        case PowerShellStreamType.Error:
                            observer.OnError(item.Error!);
                            break;
                        case PowerShellStreamType.Debug:
                            observer.OnDebug(item.Debug!.Message);
                            break;
                        case PowerShellStreamType.Warning:
                            observer.OnWarning(item.Warning!.Message);
                            break;
                        case PowerShellStreamType.Verbose:
                            observer.OnVerbose(item.Verbose!.Message);
                            break;
                        case PowerShellStreamType.Progress:
                            observer.OnProgress(item.Progress!);
                            break;
                        case PowerShellStreamType.Information:
                            observer.OnInformation(item.Information!);
                            break;
                    }
                }
                if (_exception != null)
                {
                    var terminal = _powerShell.Streams.Error.Where(e => e.Exception == _exception).FirstOrDefault()
                        ?? new ErrorRecord(_exception, "PowerShellError", ErrorCategory.NotSpecified, _powerShell);
                    observer.OnError(terminal);
                    return ActionRegistration.None;
                }
                if (_isCompleted)
                {
                    observer.OnCompleted();
                    return ActionRegistration.None;
                }
                _psObservers.Add(observer);
                return new ActionRegistration(() =>
                {
                    lock (_sync)
                    {
                        if (_psObservers.Contains(observer))
                        {
                            _psObservers.Remove(observer);
                        }
                    }
                });
            }
        }

        public void Dispose()
        {
            if (_shouldDisposeInput) Input.Dispose();
            if (_shouldDisposeOutput) Output.Dispose();
        }
    }
}
