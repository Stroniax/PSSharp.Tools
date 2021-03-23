using PSSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace PSSharp
{
    /// <summary>
    /// A PowerShell job representing a single <see cref="Task"/>.
    /// </summary>
    public sealed class TaskChildJob : Job
    {
        #region Private Members
        private Task _task;
        private readonly CancellationTokenSource? _cts;
        private readonly string? _location;
        private readonly bool _isBuilderTaskJob;
        private bool _disposeCTS = true;
        private TaskJobAction? _action;
        /// <summary>
        /// Executed when the <see cref="Task"/> this job represents has completed.
        /// </summary>
        /// <param name="completedTask"><see cref="_task"/> after completing.</param>
        private async Task OnTaskCompleted(Task completedTask)
        {
            // 1. Is there any reason for this method to return Task?
            // 2. Is there any reason to say _task = task; _task.ContinueWith() instead of just _task = task.ContinueWith()? 
            //    When this method does not return null that returns Task<Task>. But ... does that matter?
            if (completedTask.IsCanceled)
            {
                SetJobState(JobState.Stopped);
            }
            else if (completedTask.IsFaulted)
            {
                var exception = completedTask.Exception.InnerExceptions.Count == 1
                    ? completedTask.Exception.InnerExceptions[0]
                    : completedTask.Exception;
                Error.Add(new ErrorRecord(
                    exception,
                    "TaskException",
                    ErrorCategory.NotSpecified,
                    completedTask)
                {
                    ErrorDetails = new ErrorDetails($"An exception occurred in the task. {exception.Message}")
                });
                SetJobState(JobState.Failed);
            }
            else
            {
                var expectingOutput = !(_task.GetType().GetProperty("Result")?.PropertyType.Name.Equals("VoidTaskResult")) ?? false;
                if (expectingOutput)
                {
                    var invokeResult = await (dynamic)completedTask;
                    if (invokeResult != null)
                    {
                        Output.Add(PSObject.AsPSObject(invokeResult));
                    }
                }
                SetJobState(JobState.Completed);
            }
        }
        #endregion

        #region Public Members
        /// <summary>
        /// <see langword="true"/> if any of this job's streams have more data.
        /// </summary>
        public override bool HasMoreData
            => Output.Count > 0
            || Error.Count > 0
            || Warning.Count > 0
            || Progress.Count > 0
            || Verbose.Count > 0
            || Information.Count > 0
            || Debug.Count > 0;
        /// <summary>
        /// The location the job is being executed at; by default, the name of the computer the task is being run on.
        /// </summary>
        public override string Location => _location ?? Environment.MachineName;
        /// <summary>
        /// The status of the <see cref="Task"/> which this job represents.
        /// </summary>
        public override string StatusMessage => _task.Status.ToString();
        /// <summary>
        /// Indicates whether the task represented by this job is expected to write an object to <see cref="Job.Output"/>.
        /// </summary>
        public bool ExpectingOutput
        {
            get
            {
                if (_isBuilderTaskJob) return true;
                var resultProperty = _task.GetType().GetProperty("Result");
                if (resultProperty is null)
                {
                    return false;
                }
                return !resultProperty.PropertyType.Name.Equals("VoidTaskResult");
            }
        }
        /// <summary>
        /// Indicates if the <see cref="CancellationTokenSource"/> passed to this job's constructor (if any) should be disposed when the job is disposed.
        /// Defaults to <see langword="true"/>.
        /// </summary>
        public bool ShouldDisposeCancellationTokenSource
        {
            set
            {
                _disposeCTS = value;
            }
        }
        /// <summary>
        /// <see langword="true"/> if the job has an associated <see cref="System.Threading.CancellationToken"/>
        /// to allow for properly stopping the job.
        /// </summary>
        public bool CanStop { get => _cts != null; }
        /// <summary>
        /// If a <see cref="CancellationTokenSource"/> was passed to this job's constructor, it will be cancelled.
        /// Otherwise, the job state will just be set to <see cref="JobState.Stopped"/>.
        /// </summary>
        public override void StopJob()
        {
            SetJobState(JobState.Stopping);
            // to prevent the job from hanging, we'll say the job is stopped
            // if we can't stop it. Otherwise, we'll cancel _cts and let the
            // .ContinueWith() invocation set the job's state.
            if (_cts is null)
            {
                SetJobState(JobState.Stopped);
            }
            else
            {
                _cts.Cancel();
            }
        }
        /// <summary>
        /// Disposes the <see cref="Task"/> (and <see cref="CancellationTokenSource"/>, if provided and 
        /// <see cref="ShouldDisposeCancellationTokenSource"/> is <see langword="true"/>) used by this job.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _task.Dispose();
                if (_disposeCTS)
                {
                    _cts?.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Internal Members
        internal bool CanStart => _isBuilderTaskJob;
        internal bool IsCompleted => _task is null ? this.IsFinished() : _task.IsCompleted;
        internal bool IsFaulted => _task is null ? JobStateInfo.State == JobState.Failed : _task.IsFaulted;
        internal bool IsCanceled => _task is null ? JobStateInfo.State == JobState.Stopped : _task.IsCanceled;
        /// <summary>
        /// The <see cref="CancellationToken"/> of the job. Provided as a reference for <see cref="TaskJobBuilder.CancellationToken"/>.
        /// </summary>
        internal CancellationToken CancellationToken => _cts?.Token ?? default;
        /// <summary>
        /// To be called when starting a <see cref="TaskChildJob"/> created with 
        /// <see cref="TaskChildJob(TaskJobAction, Dictionary{string, object}, string?, string?)"/>.
        /// </summary>
        internal void StartJob()
        {
            if (!_isBuilderTaskJob)
            {
                throw new InvalidOperationException("Cannot start TaskJob manually unless the job was created with TaskJobAction.");
            }
            if (JobStateInfo.State != JobState.NotStarted)
            {
                throw new InvalidJobStateException("Cannot start TaskJob when status is not NotStarted.");
            }
            else
            {
                SetJobState(JobState.Running);
                _task = _action!.Invoke(new TaskJobBuilder(this, _psBoundParameters!));
                _task.ContinueWith(OnTaskCompleted);
            }
        }
        internal void Fail()
        {
            Error.Add(new ErrorRecord(
                new OperationCanceledException("The job was not executed because a dependant job failed."),
                "StopAtFailure",
                ErrorCategory.OperationStopped,
                null));
            SetJobState(JobState.Failed);
        }
        #endregion
        /// <summary>
        /// Creates a <see cref="TaskChildJob"/> wrapper around an existing, already started <see cref="Task"/>.
        /// </summary>
        /// <param name="task">A task this <see cref="TaskChildJob"/> wraps.</param>
        /// <param name="cancellationTokenSource">A <see cref="CancellationTokenSource"/> that can be used to stop the job.</param>
        /// <param name="command">The <see cref="Job.Command"/> indicating the PowerShell script responsible for the creation of this <see cref="TaskChildJob"/>.</param>
        /// <param name="location">The location at which the job is being executed. If <see langword="null"/>, the <see cref="Location"/> of this job will be <see cref="Environment.MachineName"/>.</param>
        internal TaskChildJob(Task task, CancellationTokenSource? cancellationTokenSource = null, string? command = null, string? location = null)
            : base(command)
        {
            PSJobTypeName = nameof(TaskChildJob);
            _location = location;
            SetJobState(JobState.Running);
            _task = task ?? throw new ArgumentNullException(nameof(task));
            _task.ContinueWith(OnTaskCompleted);
            _cts = cancellationTokenSource;
        }
        /// <summary>
        /// Creates a <see cref="TaskChildJob"/> around an action for a <see cref="Task"/> that has not yet been invoked.
        /// When this constructor is called, the job must be started with <see cref="StartJob"/>.
        /// </summary>
        /// <param name="action">The asynchronous operation to be executed by the created <see cref="TaskChildJob"/>.</param>
        /// <param name="command">The <see cref="Job.Command"/> indicating the PowerShell script responsible for the creation of this <see cref="TaskChildJob"/>.</param>
        /// <param name="location">The location at which the job is being executed. If <see langword="null"/>, the <see cref="Location"/> of this job will be <see cref="Environment.MachineName"/>.</param>
        /// <param name="psBoundParameters">The parameters bound to the cmdlet at the time the job was created.</param>
        internal TaskChildJob(TaskJobAction action, Dictionary<string, object> psBoundParameters, string? command = null, string? location = null)
            : base(command)
        {
            PSJobTypeName = nameof(TaskChildJob);
            _isBuilderTaskJob = true;
            _task = null!;
            _action = action;
            _location = location;
            _cts = new CancellationTokenSource();
            _psBoundParameters = psBoundParameters.ToDictionary(i => i.Key, i => i.Value);
        }
        private Dictionary<string, object>? _psBoundParameters;
    }
}
