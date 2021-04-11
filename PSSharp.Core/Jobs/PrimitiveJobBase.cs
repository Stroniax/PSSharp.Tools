using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PSSharp
{
    /// <summary>
    /// A basic implementation of the base PowerShell <see cref="Job"/>.
    /// </summary>
    public abstract class PrimitiveJobBase : Job, IPSObservable<PSObject>, IObservable<PSObject>
    {
        #region IObservable<> & IPSObservable<> implementation
        /// <summary>
        /// Used to lock when executing observer actions.
        /// </summary>
        private object _observationSync = new object();
        /// <summary>
        /// Data written to the job, captured in the proper order.
        /// </summary>
        private ConcurrentQueue<JobOutput> _output = new ConcurrentQueue<JobOutput>();
        /// <summary>
        /// <see cref="IPSObserver{T}"/> instances observing this job.
        /// </summary>
        private List<IPSObserver<PSObject>> _psObservers = new List<IPSObserver<PSObject>>();
        /// <summary>
        /// <see cref="IObserver{T}"/> instances observing this job.
        /// </summary>
        private List<IObserver<PSObject>> _observers = new List<IObserver<PSObject>>();
        IDisposable IObservable<PSObject>.Subscribe(IObserver<PSObject> observer)
        {
            lock (_observationSync)
            {
                foreach (var item in _output)
                {
                    if (item.Stream == PowerShellStreamType.Output)
                    {
                        observer.OnNext(item.Output!);
                    }
                }
                if (IsFinished)
                {
                    if (JobStateInfo.State == JobState.Stopped)
                    {
                        observer.OnError(new OperationCanceledException());
                    }
                    if (JobStateInfo.State == JobState.Failed)
                    {
                        _output.TryPeek(out var r);
                        observer.OnError(r.Error?.Exception ?? new JobFailedException());
                    }
                    if (JobStateInfo.State == JobState.Completed)
                    {
                        observer.OnCompleted();
                    }
                    return ActionRegistration.None;
                }
                else
                {
                    _observers.Add(observer);
                    return new ActionRegistration(() =>
                    {
                        lock (_observationSync)
                        {
                            if (_observers.Contains(observer))
                            {
                                _observers.Remove(observer);
                            }
                        }
                    });
                }
            }
        }
        IDisposable IPSObservable<PSObject>.Subscribe(IPSObserver<PSObject> observer)
        {
            lock (_observationSync)
            {
                foreach (var item in _output)
                {
                    switch (item.Stream)
                    {
                        case PowerShellStreamType.Output:
                            observer.OnOutput(item.Output!);
                            break;
                        case PowerShellStreamType.Error:
                            observer.OnError(item.Error!);
                            break;
                        case PowerShellStreamType.Warning:
                            observer.OnWarning(item.Warning!.Message);
                            break;
                        case PowerShellStreamType.Verbose:
                            observer.OnVerbose(item.Verbose!.Message);
                            break;
                        case PowerShellStreamType.Debug:
                            observer.OnDebug(item.Debug!.Message);
                            break;
                        case PowerShellStreamType.Progress:
                            observer.OnProgress(item.Progress!);
                            break;
                        case PowerShellStreamType.Information:
                            observer.OnInformation(item.Information!);
                            break;
                    }
                }
                if (IsFinished)
                {
                    if (JobStateInfo.State == JobState.Stopped)
                    {
                        observer.OnFailed(new ErrorRecord(new OperationCanceledException(), "JobStopped", ErrorCategory.OperationStopped, this));
                    }
                    if (JobStateInfo.State == JobState.Failed)
                    {
                        _output.TryPeek(out var r);
                        observer.OnFailed(r.Error ?? new ErrorRecord(new JobFailedException(), "JobFailed", ErrorCategory.NotSpecified, this));
                    }
                    if (JobStateInfo.State == JobState.Completed)
                    {
                        observer.OnCompleted();
                    }
                    return ActionRegistration.None;
                }
                else
                {
                    _psObservers.Add(observer);
                    return new ActionRegistration(() =>
                    {
                        lock (_observationSync)
                        {
                            if (_psObservers.Contains(observer))
                            {
                                _psObservers.Remove(observer);
                            }
                        }
                    });
                }
            }
        }
        #endregion
        #region IObservable Event Listeners
        /// <summary>
        /// Configures event listeners as required for <see cref="IObservable{T}"/>
        /// and <see cref="IPSObservable{T}"/> implementations.
        /// </summary>
        private void ConfigureObservation()
        {
            StateChanged += OnStateChanged;
            Output.DataAdded += OnOutputAdded;
            Error.DataAdded += OnErrorAdded;
            Verbose.DataAdded += OnVerboseAdded;
            Debug.DataAdded += OnDebugAdded;
            Progress.DataAdded += OnProgressAdded;
            Warning.DataAdded += OnWarningAdded;
            Information.DataAdded += OnInformationAdded;
        }
        private void OnStateChanged(object sender, JobStateEventArgs e)
        {
            lock (_observationSync)
            {
                switch (e.JobStateInfo.State)
                {
                    case JobState.Stopped:
                        {
                            foreach (var observer in _psObservers)
                            {
                                observer.OnFailed(new ErrorRecord(new OperationCanceledException(), "JobStopped", ErrorCategory.OperationStopped, this));
                            }
                            foreach (var observer in _observers)
                            {
                                observer.OnError(new OperationCanceledException());
                            }
                        }
                        break;
                    case JobState.Failed:
                        {
                            var lastError = (_output.TryPeek(out var er) ? er.Error : null) ??
                                new ErrorRecord(new JobFailedException(), "JobFailed", ErrorCategory.NotSpecified, this);
                            foreach (var observer in _psObservers)
                            {
                                observer.OnFailed(lastError);
                            }
                            foreach (var observer in _observers)
                            {
                                observer.OnError(lastError.Exception ?? new JobFailedException());
                            }
                        }
                        break;
                    case JobState.Completed:
                        {
                            foreach (var observer in _psObservers)
                            {
                                observer.OnCompleted();
                            }
                            foreach (var observer in _observers)
                            {
                                observer.OnCompleted();
                            }
                        }
                        break;
                }
                _observers.Clear();
                _psObservers.Clear();
            }
        }
        private void OnDebugAdded(object sender, DataAddedEventArgs e)
        {
            lock (_observationSync)
            {
                _output.Enqueue(new JobOutput(Debug[e.Index]));
                foreach (var observer in _psObservers)
                {
                    observer.OnDebug(Debug[e.Index].Message);
                }
            }
        }
        private void OnInformationAdded(object sender, DataAddedEventArgs e)
        {
            lock (_observationSync)
            {
                _output.Enqueue(new JobOutput(Information[e.Index]));
                foreach (var observer in _psObservers)
                {
                    observer.OnInformation(Information[e.Index]);
                }
            }
        }
        private void OnWarningAdded(object sender, DataAddedEventArgs e)
        {
            lock (_observationSync)
            {
                _output.Enqueue(new JobOutput(Warning[e.Index]));
                foreach (var observer in _psObservers)
                {
                    observer.OnWarning(Warning[e.Index].Message);
                }
            }
        }
        private void OnProgressAdded(object sender, DataAddedEventArgs e)
        {
            lock (_observationSync)
            {
                _output.Enqueue(new JobOutput(Progress[e.Index]));
                foreach (var observer in _psObservers)
                {
                    observer.OnProgress(Progress[e.Index]);
                }
            }
        }
        private void OnVerboseAdded(object sender, DataAddedEventArgs e)
        {
            lock (_observationSync)
            {
                _output.Enqueue(new JobOutput(Verbose[e.Index]));
                foreach (var observer in _psObservers)
                {
                    observer.OnVerbose(Verbose[e.Index].Message);
                }
            }
        }
        private void OnErrorAdded(object sender, DataAddedEventArgs e)
        {
            lock (_observationSync)
            {
                _output.Enqueue(new JobOutput(Error[e.Index]));
                foreach (var observer in _psObservers)
                {
                    observer.OnError(Error[e.Index]);
                }
                // IObserver<>.OnError should only be called for a terminating exception
            }
        }
        private void OnOutputAdded(object sender, DataAddedEventArgs e)
        {
            lock (_observationSync)
            {
                _output.Enqueue(new JobOutput(Output[e.Index]));
                foreach (var observer in _psObservers)
                {
                    observer.OnOutput(Output[e.Index]);
                }
                foreach (var observer in _observers)
                {
                    observer.OnNext(Output[e.Index]);
                }
            }
        }
        /// <inheritdoc/>
        new public PSDataCollection<PSObject> Output
        {
            get => base.Output;
            set
            {
                lock (_observationSync)
                {
                    value.DataAdded += OnOutputAdded;
                    base.Output.DataAdded -= OnOutputAdded;
                    base.Output = value;
                }
            }
        }
        /// <inheritdoc/>
        new public PSDataCollection<ErrorRecord> Error
        {
            get => base.Error;
            set
            {
                lock (_observationSync)
                {
                    value.DataAdded += OnErrorAdded;
                    base.Error.DataAdded -= OnErrorAdded;
                    base.Error = value;
                }
            }
        }
        /// <inheritdoc/>
        new public PSDataCollection<VerboseRecord> Verbose
        {
            get => base.Verbose;
            set
            {
                lock (_observationSync)
                {
                    value.DataAdded += OnVerboseAdded;
                    base.Verbose.DataAdded -= OnVerboseAdded;
                    base.Verbose = value;
                }
            }
        }
        /// <inheritdoc/>
        new public PSDataCollection<DebugRecord> Debug
        {
            get => base.Debug;
            set
            {
                lock (_observationSync)
                {
                    value.DataAdded += OnDebugAdded;
                    base.Debug.DataAdded -= OnDebugAdded;
                    base.Debug = value;
                }
            }
        }
        /// <inheritdoc/>
        new public PSDataCollection<WarningRecord> Warning
        {
            get => base.Warning;
            set
            {
                lock (_observationSync)
                {
                    value.DataAdded += OnWarningAdded;
                    base.Warning.DataAdded -= OnWarningAdded;
                    base.Warning = value;
                }
            }
        }
        /// <inheritdoc/>
        new public PSDataCollection<ProgressRecord> Progress
        {
            get => base.Progress;
            set
            {
                lock (_observationSync)
                {
                    value.DataAdded += OnProgressAdded;
                    base.Progress.DataAdded -= OnProgressAdded;
                    base.Progress = value;
                }
            }
        }
        /// <inheritdoc/>
        new public PSDataCollection<InformationRecord> Information
        {
            get => base.Information;
            set
            {
                lock (_observationSync)
                {
                    value.DataAdded += OnInformationAdded;
                    base.Information.DataAdded -= OnInformationAdded;
                    base.Information = value;
                }
            }
        }
        #endregion
        #region Constructor
        /// <inheritdoc/>
        protected PrimitiveJobBase()
            : base()
        {
            ConfigureObservation();
        }
        /// <inheritdoc/>
        protected PrimitiveJobBase(string? command)
            : base(command)
        {
            ConfigureObservation();
        }
        /// <inheritdoc/>
        protected PrimitiveJobBase(string? command, string? name)
            : base(command, name)
        {
            ConfigureObservation();
        }
        /// <inheritdoc/>
        protected PrimitiveJobBase(string? command, string? name, Guid instanceId)
            : base(command, name, instanceId)
        {
            ConfigureObservation();
        }
        /// <inheritdoc/>
        protected PrimitiveJobBase(string? command, string? name, JobIdentifier token)
            : base(command, name, token)
        {
            ConfigureObservation();
        }
        /// <inheritdoc/>
        protected PrimitiveJobBase(string? command, string? name, IList<Job> childJobs)
            : base(command, name, childJobs)
        {
            ConfigureObservation();
        }
        #endregion

        /// <summary>
        /// Indicates whether the job has data (from <see cref="Job.Output"/>, <see cref="Job.Error"/>, etc.) that has not been cleared.
        /// </summary>
        [PSDefaultValue(Help = "Indicates whether or not the job has data (output, error, etc.) that has not been cleared.")]
        public override bool HasMoreData
            => Output.Count > 0
            || Error.Count > 0
            || Warning.Count > 0
            || Verbose.Count > 0
            || Debug.Count > 0
            || Information.Count > 0
            || Progress.Count > 0;
        /// <summary>
        /// The location at which the job is executed. By default, <see cref="Environment.MachineName"/>.
        /// </summary>
        [PSDefaultValue(Value = "$env:ComputerName", Help = "The location at which the job is executing.")]
        public override string Location => Environment.MachineName;
        /// <summary>
        /// The status of the job. Unless overridden, <see langword="null"/>.
        /// </summary>
        [PSDefaultValue(Value = null, Help = "The current status of the job.")]
        public override string? StatusMessage => null;

        /// <summary>
        /// Indicates whether the job state is any state that indicates termination,
        /// including any of the following job states.
        /// <list type="bullet">
        /// <item><see cref="JobState.Completed"/></item>
        /// <item><see cref="JobState.Failed"/></item>
        /// <item><see cref="JobState.Stopped"/></item>
        /// </list>
        /// </summary>
        protected virtual bool IsFinished
        {
            get
            {
                var state = JobStateInfo.State;
                switch (state)
                {
                    case JobState.Completed:
                    case JobState.Stopped:
                    case JobState.Failed:
                        return true;
                    default:
                        return false;
                }
            }
        }
        /// <summary>
        /// Indicates whether the job state is any state that indicates it is not currently executing, 
        /// including any of the following job states.
        /// <list type="bullet">
        /// <item><see cref="JobState.NotStarted"/></item>
        /// <item><see cref="JobState.Completed"/></item>
        /// <item><see cref="JobState.Failed"/></item>
        /// <item><see cref="JobState.Stopped"/></item>
        /// <item><see cref="JobState.Blocked"/></item>
        /// <item><see cref="JobState.Suspended"/></item>
        /// <item><see cref="JobState.Disconnected"/></item>
        /// <item><see cref="JobState.AtBreakpoint"/></item>
        /// </list>
        /// </summary>
        protected virtual bool IsHalted
        {
            get
            {
                var state = JobStateInfo.State;
                switch (state)
                {
                    case JobState.Blocked:
                    case JobState.Disconnected:
                    case JobState.NotStarted:
                    case JobState.Failed:
                    case JobState.Stopped:
                    case JobState.Completed:
                    case JobState.AtBreakpoint:
                    case JobState.Suspended:
                        return true;
                    default:
                        return false;
                }
            }
        }


        /// <inheritdoc/>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is <see cref="Job"/> 
        /// and the <see cref="Job.InstanceId"/> of this and the target job match.</returns>
        public override bool Equals(object obj) => obj is Job job && job.InstanceId == InstanceId;
        /// <inheritdoc/>
        public override int GetHashCode() => InstanceId.GetHashCode();
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            lock (_observationSync)
            {
                try
                {
                    StateChanged -= OnStateChanged;
                    Output.DataAdded -= OnOutputAdded;
                    Error.DataAdded -= OnErrorAdded;
                    Warning.DataAdded -= OnWarningAdded;
                    Verbose.DataAdded -= OnVerboseAdded;
                    Debug.DataAdded -= OnDebugAdded;
                    Progress.DataAdded -= OnProgressAdded;
                    Information.DataAdded -= OnInformationAdded;
                    _observers.ForEach(i => i.OnError(new ObjectDisposedException(GetType().FullName)));
                    _psObservers.ForEach(i => i.OnFailed(new ErrorRecord(
                        new ObjectDisposedException(GetType().FullName),
                        "JobDisposed",
                        ErrorCategory.OperationStopped,
                        this)));
                    _observers.Clear();
                    _psObservers.Clear();
                    while (_output.TryDequeue(out _)) ;
                }
                catch { }
            }
            base.Dispose(disposing);
        }
        public static bool operator ==(PrimitiveJobBase left, Job right)
        {
            return left.InstanceId == right.InstanceId;
        }
        public static bool operator !=(PrimitiveJobBase left, Job right)
        {
            return !(left == right);
        }
    }
}
