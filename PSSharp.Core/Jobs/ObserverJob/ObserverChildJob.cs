using PSSharp.Extensions;
using System;
using System.Management.Automation;
using System.Reactive.Linq;

namespace PSSharp
{
    /// <summary>
    /// Job wrapper to observe an <see cref="IObservable{T}"/> or <see cref="IPSObservable{T}"/> instance.
    /// Note that constructing this instance does not cause any observation; observation of the observable
    /// passed to the constructor does not begin until <see cref="StartJob"/> is called.
    /// </summary>
    /// <typeparam name="T">The output type of the observable sequence this job monitors.</typeparam>
    public class ObserverChildJob<T> : PrimitiveJobBase
    {
        public ObserverChildJob(IObservable<T> observable, string? command = null, string? name = null)
            : base(command, name)
        {
            _observable = observable ?? throw new ArgumentNullException(nameof(observable));
            _startObserver = () =>
            {
                _cancellation = observable.Subscribe(
                    onNext: OnOutput,
                    onError: OnFailed,
                    onCompleted: OnCompleted);
            };
        }
        public ObserverChildJob(IPSObservable<T> observable, string? command = null, string? name = null)
            : base(command, name)
        {
            _startObserver = () =>
            {
                _cancellation = observable.Subscribe(new PSActionObserver<T>(
                    onOutput: (s, o) => OnOutput(o),
                    onCompleted: (s) => OnCompleted(),
                    onFailed: (s, e) => OnFailed(e),
                    onDebug: (s, d) => OnDebug(d),
                    onWarning: (s, w) => OnWarning(w),
                    onVerbose: (s, v) => OnVerbose(v),
                    onError: (s, e) => OnError(e),
                    onInformation: (s, i) => OnInformation(i),
                    onProgress: (s, p) => OnProgress(p)
                    ));
            };
        }

        private readonly Action _startObserver;
        private readonly IObservable<T>? _observable;
        private IDisposable? _cancellation;

        public virtual void StartJob()
        {
            lock (_startObserver)
            {
                if (State != JobState.NotStarted)
                {
                    throw new InvalidJobStateException(State, "The job cannot be started unless the current state is NotStarted.");
                }
                SetJobState(JobState.Running);
                _startObserver();
            }
        }
        public override void StopJob()
        {
            lock (_startObserver)
            {
                if (IsFinished) return;

                SetJobState(JobState.Stopping);
                _cancellation?.Dispose();
                SetJobState(JobState.Stopped);
            }
        }

        protected virtual void OnOutput(T input)
        {
            Output.Add(PSObject.AsPSObject(input));
        }
        protected virtual void OnFailed(Exception terminalException)
        {
            Error.Add(new ErrorRecord(
                terminalException,
                "ObservableException",
                ErrorCategory.NotSpecified,
                _observable
                ));
            SetJobState(JobState.Failed);
        }
        protected virtual void OnFailed(ErrorRecord terminalError)
        {
            if (!Error.Contains(terminalError))
            {
                Error.Add(terminalError);
            }
            SetJobState(JobState.Failed);
        }
        protected virtual void OnCompleted()
        {
            SetJobState(JobState.Completed);
        }
        protected virtual void OnError(ErrorRecord nonTerminalError)
        {
            Error.Add(nonTerminalError);
        }
        protected virtual void OnDebug(string debug)
        {
            Debug.Add(new DebugRecord(debug));
        }
        protected virtual void OnVerbose(string verbose)
        {
            Verbose.Add(new VerboseRecord(verbose));
        }
        protected virtual void OnWarning(string warning)
        {
            Warning.Add(new WarningRecord(warning));
        }
        protected virtual void OnInformation(InformationRecord information)
        {
            Information.Add(information);
        }
        protected virtual void OnProgress(ProgressRecord progress)
        {
            Progress.Add(progress);
        }
    }
}
