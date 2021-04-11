using PSSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reactive.Linq;
using System.Text;
using System.Threading;

namespace PSSharp
{
    // because PowerShell doesn't like generic cmdlets, I have to make a non-generic ObserverJob from which ObserverJob<> can be derived
    public class ObserverJob : Job
    {
        #region Constructor
        protected ObserverJob(ExecutionMode executionMode)
        {
            PSJobTypeName = "ObserverJob";
            _executionMode = executionMode;
        }
        protected ObserverJob(ExecutionMode executionMode, string? command)
            : base(command)
        {
            PSJobTypeName = "ObserverJob";
            _executionMode = executionMode;
        }
        protected ObserverJob(ExecutionMode executionMode, string? command, string? name)
            : base(command, name)
        {
            PSJobTypeName = "ObserverJob";
            _executionMode = executionMode;
        }
        protected ObserverJob(ExecutionMode executionMode, string? command, string? name, Guid instanceId)
            : base(command, name, instanceId)
        {
            PSJobTypeName = "ObserverJob";
            _executionMode = executionMode;
        }
        protected ObserverJob(ExecutionMode executionMode, string? command, string? name, JobIdentifier jobIdentifier)
            : base(command, name, jobIdentifier)
        {
            PSJobTypeName = "ObserverJob";
            _executionMode = executionMode;
        }
        #region Static Start Methods
        public static ObserverJob<T> StartJob<T>(string? command, string? name, IObservable<T> source)
            => StartJob(command, name, executionMode: ExecutionMode.Concurrent, observableSources: source);
        /// <summary>
        /// <para>To provide multiple types of observers, use 
        /// <see cref="Observable.Select{TSource, TResult}(IObservable{TSource}, Func{TSource, int, TResult})"/>
        /// with <see cref="PSObject.AsPSObject(object)"/> to cast all objects to <see cref="PSObject"/>.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="name"></param>
        /// <param name="executionMode"></param>
        /// <param name="observableSources"></param>
        /// <returns></returns>
        public static ObserverJob<T> StartJob<T>(string? command, string? name, ExecutionMode executionMode, params IObservable<T>[] observableSources)
            => StartJob(command, name, executionMode, (IEnumerable<IObservable<T>>)observableSources);
        public static ObserverJob<T> StartJob<T>(string? command, string? name, ExecutionMode executionMode, IEnumerable<IObservable<T>> observableSources)
            => ObserverJob<T>.StartJob(command, name, executionMode, observableSources);
        public static ObserverJob StartJob<T1, T2>(string? command, string? name, ExecutionMode executionMode, IObservable<T1> p1, IObservable<T2> p2)
        {
            var job = new ObserverJob(executionMode, command, name);
            job.AddObserver(p1);
            job.AddObserver(p2);
            job.StartJob();
            return job;
        }
        public static ObserverJob StartJob<T1, T2, T3>(string? command, string? name, ExecutionMode executionMode, IObservable<T1> p1, IObservable<T2> p2, IObservable<T3> p3)
        {
            var job = new ObserverJob(executionMode, command, name);
            job.AddObserver(p1);
            job.AddObserver(p2);
            job.AddObserver(p3);
            job.StartJob();
            return job;
        }
        public static ObserverJob StartJob<T1, T2, T3, T4>(string? command, string? name, ExecutionMode executionMode, IObservable<T1> p1, IObservable<T2> p2, IObservable<T3> p3, IObservable<T4> p4)
        {
            var job = new ObserverJob(executionMode, command, name);
            job.AddObserver(p1);
            job.AddObserver(p2);
            job.AddObserver(p3);
            job.AddObserver(p4);
            job.StartJob();
            return job;
        }
        public static ObserverJob StartJob<T1, T2, T3, T4, T5>(string? command, string? name, ExecutionMode executionMode, IObservable<T1> p1, IObservable<T2> p2, IObservable<T3> p3, IObservable<T4> p4, IObservable<T5> p5)
        {
            var job = new ObserverJob(executionMode, command, name);
            job.AddObserver(p1);
            job.AddObserver(p2);
            job.AddObserver(p3);
            job.AddObserver(p4);
            job.AddObserver(p5);
            job.StartJob();
            return job;
        }
        public static ObserverJob StartJob<T1, T2, T3, T4, T5, T6>(string? command, string? name, ExecutionMode executionMode, IObservable<T1> p1, IObservable<T2> p2, IObservable<T3> p3, IObservable<T4> p4, IObservable<T5> p5, IObservable<T6> p6)
        {
            var job = new ObserverJob(executionMode, command, name);
            job.AddObserver(p1);
            job.AddObserver(p2);
            job.AddObserver(p3);
            job.AddObserver(p4);
            job.AddObserver(p5);
            job.AddObserver(p6);
            job.StartJob();
            return job;
        }
        public static ObserverJob StartJob<T1, T2, T3, T4, T5, T6, T7>(string? command, string? name, ExecutionMode executionMode, IObservable<T1> p1, IObservable<T2> p2, IObservable<T3> p3, IObservable<T4> p4, IObservable<T5> p5, IObservable<T6> p6, IObservable<T7> p7)
        {
            var job = new ObserverJob(executionMode, command, name);
            job.AddObserver(p1);
            job.AddObserver(p2);
            job.AddObserver(p3);
            job.AddObserver(p4);
            job.AddObserver(p5);
            job.AddObserver(p6);
            job.AddObserver(p7);
            job.StartJob();
            return job;
        }
        public static ObserverJob StartJob<T1, T2, T3, T4, T5, T6, T7, T8>(string? command, string? name, ExecutionMode executionMode, IObservable<T1> p1, IObservable<T2> p2, IObservable<T3> p3, IObservable<T4> p4, IObservable<T5> p5, IObservable<T6> p6, IObservable<T7> p7, IObservable<T8> p8)
        {
            var job = new ObserverJob(executionMode, command, name);
            job.AddObserver(p1);
            job.AddObserver(p2);
            job.AddObserver(p3);
            job.AddObserver(p4);
            job.AddObserver(p5);
            job.AddObserver(p6);
            job.AddObserver(p7);
            job.AddObserver(p8);
            job.StartJob();
            return job;
        }
        public static ObserverJob StartJob<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string? command, string? name, ExecutionMode executionMode, IObservable<T1> p1, IObservable<T2> p2, IObservable<T3> p3, IObservable<T4> p4, IObservable<T5> p5, IObservable<T6> p6, IObservable<T7> p7, IObservable<T8> p8, IObservable<T9> p9)
        {
            var job = new ObserverJob(executionMode, command, name);
            job.AddObserver(p1);
            job.AddObserver(p2);
            job.AddObserver(p3);
            job.AddObserver(p4);
            job.AddObserver(p5);
            job.AddObserver(p6);
            job.AddObserver(p7);
            job.AddObserver(p8);
            job.AddObserver(p9);
            job.StartJob();
            return job;
        }
        public static ObserverJob StartJob<T>(string? command, string? name, ExecutionMode executionMode, IObservable<IObservable<T>> observableGenerator)
        {
            var job = new ObserverJob(executionMode, command, name);
            job.StartJob(observableGenerator);
            return job;
        }

        #endregion
        #endregion
        #region Public Properties
        public override bool HasMoreData => this.AnyStreamHasData();
        public override string Location => Environment.MachineName;
        public override string StatusMessage
        {
            get
            {
                lock (_syncRoot)
                {
                    if (!_started)
                    {
                        return "Awaiting Start";
                    }
                    if (!_isGeneratorComplete)
                    {
                        return $"Awaiting {_observers.Count + _startObservers.Count}(+) Observer(s)";
                    }
                    if (_observers.Count + _startObservers.Count == 0)
                    {
                        return "Finished";
                    }
                    else
                    {
                        return $"Awaiting {_observers.Count + _startObservers.Count} Observer(s)";
                    }
                }
            }
        }
        /// <summary>
        /// The number of observers currently executing.
        /// </summary>
        public int ExecutingObserverCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _observers.Count;
                }
            }
        }
        /// <summary>
        /// The number of observers queued to be started when the current observer completes.
        /// </summary>
        public int QueuedObserverCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _startObservers.Count;
                }
            }
        }

        #endregion
        #region Private Fields
        /// <summary>
        /// Used to lock resources to prevent concurrent operation.
        /// </summary>
        private object _syncRoot = new object();
        /// <summary>
        /// Queued actions that will start an observer when the current observer completes.
        /// </summary>
        private readonly Queue<Action> _startObservers = new Queue<Action>();
        /// <summary>
        /// Used to cancel the job.
        /// </summary>
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        /// <summary>
        /// The currently executing observers, excluding the observable generator.
        /// </summary>
        private List<object> _observers = new List<object>();
        /// <summary>
        /// Indicates whether <see cref="IObserver{T}.OnCompleted"/> has been called by the observable generator.
        /// </summary>
        private bool _isGeneratorComplete = true;
        /// <summary>
        /// Indicates the order in which observers added to this job will be created.
        /// </summary>
        private readonly ExecutionMode _executionMode;
        /// <summary>
        /// Indicates whether or not an overload of StartJob has been called.
        /// </summary>
        private bool _started;
        /// <summary>
        /// Indicates whether <see cref="IObserver{T}.OnError(Exception)"/> was called by any observers.
        /// </summary>
        private bool _hadErrors;
        #endregion
        #region Public Methods
        /// <summary>
        /// Stops the executing job and cancels any observers.
        /// </summary>
        public override void StopJob()
        {
            SetJobState(JobState.Stopping);
            _cts.Cancel();
        }
        #endregion
        #region Protected Methods
        /// <summary>
        /// Adds an observer to be processed by the job. Note that this method can only be called before
        /// any overload of <see cref="StartJob"/> is called.
        /// </summary>
        /// <typeparam name="T">The type output by the observer.</typeparam>
        /// <param name="observableSource">The publisher to be observed.</param>
        protected void AddObserver<T>(IObservable<T> observableSource)
        {
            lock (_syncRoot)
            {
                if (_started)
                {
                    throw new InvalidOperationException("Cannot add an observer after the job has been started.");
                }
                _startObservers.Enqueue(() =>
                {
                    lock (_syncRoot)
                    {
                        ActionObserver<T>.Subscribe(
                                observableSource,
                                (observer, publisher) => _observers.Add(observer),
                                OnNextAction,
                                OnErrorAction,
                                OnCompletedAction,
                                OnCanceledAction,
                                _cts.Token);
                    }
                });
            }
        }
        /// <summary>
        /// Starts the job, executing only the observers that have been queued through the
        /// <see cref="AddObserver{T}(IObservable{T})"/> method.
        /// </summary>
        protected void StartJob()
        {
            lock (_syncRoot)
            {
                if (_started) throw new InvalidOperationException("The job has already been started.");
                _started = true;
                if (JobStateInfo.State == JobState.NotStarted)
                {
                    SetJobState(JobState.Running);
                }
                if (_executionMode == ExecutionMode.Concurrent)
                {
                    foreach (var observable in _startObservers)
                    {
                        observable();
                    }
                    _startObservers.Clear();
                }
                else
                {
                    if (_startObservers.Count > 0)
                    {
                        _startObservers.Dequeue()();
                    }
                }

                OnCompletedAction<object>(null, null);
            }
        }
        protected void StartJob<T>(IObservable<IObservable<T>> observableGenerator)
        {
            lock (_syncRoot)
            {
                if (_started) throw new InvalidOperationException("The job has already been started.");
                _started = true;
                if (JobStateInfo.State == JobState.NotStarted)
                {
                    SetJobState(JobState.Running);
                }
                _isGeneratorComplete = false;

                ActionObserver<IObservable<T>>.Subscribe(
                observableGenerator,
                onSubscribed: null,
                onNext: (observer, publisher, newObserver) =>
                {
                    lock (_syncRoot)
                    {
                        if (_executionMode == ExecutionMode.Concurrent)
                        {
                            ActionObserver<T>.Subscribe(
                                newObserver,
                                (observer, publisher) =>
                                {
                                    lock (_syncRoot)
                                    {
                                        _observers.Add(observer);
                                    }

                                },
                                OnNextAction,
                                OnErrorAction,
                                OnCompletedAction,
                                OnCanceledAction,
                                _cts.Token);
                        }
                        else
                        {
                            _startObservers.Enqueue(() =>
                            {
                                lock (_syncRoot)
                                {
                                    ActionObserver<T>.Subscribe(
                                        newObserver,
                                        (observer, publisher) =>
                                        {
                                            lock(_syncRoot)
                                            {
                                                _observers.Add(observer);
                                            }
                                        },
                                        OnNextAction,
                                        OnErrorAction,
                                        OnCompletedAction,
                                        OnCanceledAction,
                                        _cts.Token);
                                }
                            });
                        }
                    }
                },
                onError: (receiver, sender, error) =>
                {
                    lock (_syncRoot)
                    {
                        _isGeneratorComplete = true;
                        OnErrorAction(receiver, sender, error);
                    }
                },
                onCompleted: (receiver, sender) =>
                {
                    lock (_syncRoot)
                    {
                        _isGeneratorComplete = true;
                        OnCompletedAction(receiver, sender);
                    }
                },
                onCanceled: (receiver, sender) =>
                {
                    lock (_syncRoot)
                    {
                        _isGeneratorComplete = true;
                        OnCanceledAction(receiver, sender);
                    }
                },
                _cts.Token
                );
            }
        }
        #endregion
        #region Private Methods
        private void OnNextAction<T>(IObserver<T> observer, IObservable<T> publisher, T item)
        {
            Output.Add(PSObject.AsPSObject(item));
        }
        private void OnErrorAction<T>(IObserver<T> observer, IObservable<T> publisher, Exception error)
        {
            lock (_syncRoot)
            {
                _hadErrors = true;
                Error.Add(new ErrorRecord(
                    error,
                    "ObservedError",
                    ErrorCategory.NotSpecified,
                    publisher
                    ));

                OnCompletedAction(observer, publisher);
            }
        }
        private void OnCompletedAction<T>(IObserver<T>? observer, IObservable<T>? publisher)
        {
            lock (_syncRoot)
            {
                if (observer != null && publisher != null)
                {
                    if (_observers.Contains(observer))
                    {
                        _observers.Remove(observer);
                    }
                }
                if (!_started)
                {
                    return;
                }
                if (_executionMode == ExecutionMode.ConsecutiveUntilError && _hadErrors)
                {
                    _cts.Cancel();
                    _observers.Clear();
                    _startObservers.Clear();

                    SetJobState(JobState.Failed);
                    return;
                }
                if (_observers.Count == 0 && _startObservers.Count > 0)
                {
                    _startObservers.Dequeue()();
                    return;
                }
                if (!_isGeneratorComplete || !_started)
                {
                    return;
                }
                if (_observers.Count == 0)
                {
                    if (JobStateInfo.State == JobState.Stopping)
                    {
                        SetJobState(JobState.Stopped);
                    }
                    else
                    {
                        SetJobState(JobState.Completed);
                    }
                }
            }
        }
        private void OnCanceledAction<T>(IObserver<T> receiver, IObservable<T> sender)
            => OnCompletedAction(receiver, sender);
        #endregion
    }


    /// <summary>
    /// PowerShell <see cref="Job"/> implementation of <see cref="IObserver{T}"/>, allowing a job
    /// to subscribe to any <see cref="IObservable{T}"/> publisher.
    /// </summary>
    /// <typeparam name="T">The observed item type.</typeparam>
    public sealed class ObserverJob<T> : ObserverJob
    {
        private ObserverJob(ExecutionMode executionMode, string? command, string? name)
            : base(executionMode, command, name)
        {
        }
        internal static ObserverJob<T> StartJob(string? command, string? name, ExecutionMode executionMode, IEnumerable<IObservable<T>> observableSources)
        {
            var job = new ObserverJob<T>(executionMode, command, name);
            foreach (var source in observableSources)
            {
                job.AddObserver(source);
            }
            job.StartJob();
            return job;
        }
    }
}
