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
    /// <para type='description'>A PowerShell Job that represents one or more asynchronous <see cref="Task"/> instances 
    /// that belong to a logical group (such as multiple tasks created by a single cmdlet invocation). 
    /// This allows standard PowerShell job operations such as Wait-Job and Receive-Job to be used with the <see cref="Task"/> class.</para>
    /// </summary>
    public sealed class TaskJob : Job, IObservable<JobOutput>
    {
        #region Private Members
        private ConcurrentQueue<TaskChildJob>? _queue;
        private readonly TaskJobMode _jobMode;
        private readonly string? _location;
        private volatile bool _acceptingChildJobs = true;
        private readonly object _queueLock = new object();
        private List<IObserver<JobOutput>>? _observers;
        private readonly ConcurrentQueue<JobOutput> _resultQueue
            = new ConcurrentQueue<JobOutput>();
        #endregion
        #region Public Members
        /// <summary>
        /// <see langword="true"/> if any of this job's streams have more data.
        /// </summary>
        public override bool HasMoreData => ChildJobs.Any(j => j.HasMoreData);
        /// <summary>
        /// The location the job is being executed at; by default, the name of the computer the task is being run on.
        /// </summary>
        public override string Location => _location ?? Environment.MachineName;
        /// <summary>
        /// The <see cref="TaskChildJob.StatusMessage"/> of each child job.
        /// </summary>
        public override string StatusMessage => string.Join(", ", ChildJobs.Select(j => j.StatusMessage));
        /// <summary>
        /// Determines if the <see cref="CancellationTokenSource"/> used by the child jobs should be disposed when the child jobs are disposed. The default value is <see langword="true"/>.
        /// </summary>
        public bool DisposeCancellationTokenSourceOnDisposed
        {
            set
            {
                foreach (var job in ChildJobs)
                {
                    if (job is TaskChildJob tjob)
                    {
                        tjob.ShouldDisposeCancellationTokenSource = value;
                    }
                }
            }
        }
        /// <summary>
        /// Check if all child jobs are finished; if so, mark this job as finished.
        /// </summary>
        private void OnChildJobStateChanged(object? sender, EventArgs? arguments)
        {
            if (JobStateInfo.State == JobState.NotStarted || JobStateInfo.State == JobState.Suspended)
            {
                SetJobState(JobState.Running);
            }
            if (_jobMode == TaskJobMode.Consecutive || _jobMode == TaskJobMode.ConsecutiveStopAtFailure)
            {
                // if no jobs are running, start the next queued job
                if (ChildJobs.All(j => j.IsFinished() || j.JobStateInfo.State == JobState.NotStarted))
                {
                    if (_queue != null)
                    {
                        if (_jobMode == TaskJobMode.ConsecutiveStopAtFailure 
                            && ChildJobs.Any(j => j.JobStateInfo.State == JobState.Failed))
                        {
                            while (_queue.TryDequeue(out var failJob))
                            {
                                failJob.Fail();
                            }
                        }
                        else
                        {
                            if (_queue.TryDequeue(out var nextJob))
                            {
                                nextJob.StartJob();
                            }
                        }
                    }
                }
            }
            if (_acceptingChildJobs)
            {
                // do not conclude if more jobs may be added
                SetJobState(JobState.Suspended);
                return;
            }
            if (this.IsFinished()) return;
            var childJobs = ChildJobs.Cast<TaskChildJob>();
            if (childJobs.All(j => j.IsCompleted))
            {
                if (childJobs.Any(j => j.IsFaulted && !j.IsCanceled))
                {
                    SetJobState(JobState.Failed);
                }
                else if (childJobs.Any(j => j.IsCanceled))
                {
                    SetJobState(JobState.Stopped);
                }
                else
                {
                    SetJobState(JobState.Completed);
                }
            }
        }
        /// <summary>
        /// Stops child jobs.
        /// </summary>
        public override void StopJob()
        {
            SetJobState(JobState.Stopping);
            foreach (var job in ChildJobs)
            {
                job.StopJob();
            }
        }
        /// <summary>
        /// Indicates whether any of the tasks passed to this task are expected to write an object to <see cref="Job.Output"/>.
        /// </summary>
        public bool ExpectingOutput => ChildJobs.Any(c => c is TaskChildJob tjob && tjob.ExpectingOutput);
        #endregion
        #region Internal Members
        internal void AddChildJob(TaskChildJob childJob)
        {
            if (!_acceptingChildJobs) throw new InvalidOperationException($"The {nameof(TaskJob)} is no longer accepting child jobs.");
            _queue ??= new ConcurrentQueue<TaskChildJob>();
            _queue.Enqueue(childJob);
            ChildJobs.Add(childJob);

            childJob.Output.DataAdded += (a, b) => 
            {
                lock (_queueLock)
                {
                    var output = new JobOutput(PowerShellStreamType.Output, childJob.Output[b.Index]);
                    _resultQueue.Enqueue(output);
                    _observers?.ForEach(observer => observer.OnNext(output));
                }
            };
            childJob.Error.DataAdded += (a, b) =>
            {
                lock (_queueLock)
                {
                    var output = new JobOutput(PowerShellStreamType.Error, childJob.Error[b.Index]);
                    _resultQueue.Enqueue(output);
                    _observers?.ForEach(observer => observer.OnNext(output));
                }
            };
            childJob.Warning.DataAdded += (a, b) =>
            {
                lock (_queueLock)
                {
                    var output = new JobOutput(PowerShellStreamType.Warning, childJob.Warning[b.Index]);
                    _resultQueue.Enqueue(output);
                    _observers?.ForEach(observer => observer.OnNext(output));
                }
            };
            childJob.Progress.DataAdded += (a, b) =>
            {
                lock (_queueLock)
                {
                    var output = new JobOutput(PowerShellStreamType.Progress, childJob.Progress[b.Index]);
                    _resultQueue.Enqueue(output);
                    _observers?.ForEach(observer => observer.OnNext(output));
                }
            };
            childJob.Verbose.DataAdded += (a, b) =>
            {
                lock (_queueLock)
                {
                    var output = new JobOutput(PowerShellStreamType.Verbose, childJob.Verbose[b.Index]);
                    _resultQueue.Enqueue(output);
                    _observers?.ForEach(observer => observer.OnNext(output));
                }
            };
            childJob.Debug.DataAdded += (a, b) =>
            {
                lock (_queueLock)
                {
                    var output = new JobOutput(PowerShellStreamType.Debug, childJob.Debug[b.Index]);
                    _resultQueue.Enqueue(output);
                    _observers?.ForEach(observer => observer.OnNext(output));
                }
            };
            childJob.Information.DataAdded += (a, b) =>
            {
                lock (_queueLock)
                {
                    var output = new JobOutput(PowerShellStreamType.Information, childJob.Information[b.Index]);
                    _resultQueue.Enqueue(output);
                    _observers?.ForEach(observer => observer.OnNext(output));
                }
            };
            childJob.StateChanged += OnChildJobStateChanged;

            if (childJob.CanStart && _jobMode == TaskJobMode.Parallel)
            {
                childJob.StartJob();
            }
            OnChildJobStateChanged(null, null);
        }
        internal void Seal()
        {
            _acceptingChildJobs = false;
            OnChildJobStateChanged(null, null);
        }
        IDisposable IObservable<JobOutput>.Subscribe(IObserver<JobOutput> observer)
        {
            _observers ??= new List<IObserver<JobOutput>>();
            lock (_queueLock)
            {
                if (!(_resultQueue is null))
                {
                    foreach (var item in _resultQueue)
                    {
                        observer.OnNext(item);
                    }
                }
            }
            _observers.Add(observer);
            return new Subscriber(() =>
            {
                if (_observers?.Contains(observer) ?? false)
                {
                    _observers.Remove(observer);
                }
            });
        }
        #endregion
        #region Constructors
        private TaskJob(string? name, string? command, string? location, IEnumerable<(Task, CancellationTokenSource?)> tasks)
            : base(command, name)
        {
            if (tasks.All(t => t.Item1 is null) || tasks.Count() == 0)
                throw new ArgumentException($"One or more {nameof(Task)} values must be provided.");

            PSJobTypeName = nameof(TaskJob);
            SetJobState(JobState.Running);
            foreach (var pair in tasks)
            {
                if (pair.Item1 is null) continue;
                var childJob = new TaskChildJob(task: pair.Item1,
                                                cancellationTokenSource: pair.Item2,
                                                command: command,
                                                location: location);
                ChildJobs.Add(childJob);
                childJob.StateChanged += OnChildJobStateChanged;
            }
        }
        internal TaskJob(string? name, string? command, string? location, TaskJobMode taskJobMode)
            : base(command, name)
        {
            PSJobTypeName = nameof(TaskJob);
            _location = location;
            _jobMode = taskJobMode;
        }
        /// <summary>
        /// Starts a new <see cref="TaskJob"/> from one or more <see cref="Task"/>s. This overload pairs each <see cref="TaskChildJob"/>'s
        /// <see cref="Task"/> with a <see cref="CancellationTokenSource"/> allowing the <see cref="TaskChildJob"/> to be stopped.
        /// </summary>
        /// <param name="name">The name of the job.</param>
        /// <param name="command">The command being executed by the job.</param>
        /// <param name="location">The location of the job. If <see langword="null"/>, the <see cref="Job.Location"/> will be displayed as <see cref="Environment.MachineName"/>.</param>
        /// <param name="tasks">The <see cref="Task"/>s for which child jobs will be created, and the <see cref="CancellationTokenSource"/> 
        /// to pair with each task (allowing the <see cref="TaskChildJob"/> and <see cref="Task"/> to be stopped.</param>
        /// <returns></returns>
        public static TaskJob StartJob(string? name, string? command,string? location, IEnumerable<(Task, CancellationTokenSource?)> tasks)
            => new TaskJob(name, command, location, tasks);
        /// <summary>
        /// Starts a new <see cref="TaskJob"/> from one or more <see cref="Task"/>s. This overload uses a single <see cref="CancellationTokenSource"/>
        /// for every <see cref="TaskChildJob"/>, such as when all tasks are created using the same <see cref="CancellationTokenSource"/>
        /// of a cmdlet during the <see cref="Cmdlet.ProcessRecord"/> execution.
        /// <para>Note that if any <see cref="Task"/> does not use the <see cref="CancellationTokenSource"/>, the job may not be able to be stopped.</para>
        /// </summary>
        /// <param name="name">The name of the job.</param>
        /// <param name="command">The command being executed by the job.</param>
        /// <param name="tasks">The <see cref="Task"/>s for which child jobs will be created.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/> that can be used to stop every 
        /// <param name="location">The location of the job. If <see langword="null"/>, the <see cref="Job.Location"/> will be displayed as <see cref="Environment.MachineName"/>.</param>
        /// <see cref="Task"/> in <paramref name="tasks"/>.</param>
        /// <returns></returns>
        public static TaskJob StartJob(string? name, string? command, string? location, IEnumerable<Task> tasks, CancellationTokenSource? cancellationTokenSource = null)
            => StartJob(name, command, location, tasks.Select(t => (t, cancellationTokenSource)));
        /// <summary>
        /// Starts a new <see cref="TaskJob"/> from one or more <see cref="Task"/>s. This overload uses a single <see cref="CancellationTokenSource"/>
        /// for every <see cref="TaskChildJob"/>, such as when all tasks are created using the same <see cref="CancellationTokenSource"/>
        /// of a cmdlet during the <see cref="Cmdlet.ProcessRecord"/> execution.
        /// <para>Note that if <paramref name="task"/> cannot be stopped by <see cref="CancellationTokenSource"/> (and 
        /// <paramref name="cancellationTokenSource"/> is not <see langword="null"/>, the job will not be able to be stopped via <see cref="Job.StopJob"/>.</para>
        /// </summary>
        /// <param name="name">The name of the job.</param>
        /// <param name="command">The command being executed by the job.</param>
        /// <param name="task">The <see cref="Task"/> this job represents.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/> that can be used to stop <paramref name="task"/>.</param>
        /// <param name="location">The location of the job. If <see langword="null"/>, the <see cref="Job.Location"/> will be displayed as <see cref="Environment.MachineName"/>.</param>
        /// <returns></returns>
        public static TaskJob StartJob(string? name, string? command, string? location, Task task, CancellationTokenSource? cancellationTokenSource = null)
            => StartJob(name, command, location, new[] { task }, cancellationTokenSource);
        #endregion
    }
}
