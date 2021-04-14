using PSSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSSharp.Commands
{
    /// <summary>
    /// Base class for a cmdlet that may await an <see cref="IObserver{T}"/>, or return a job 
    /// wrapping one or more <see cref="IObserver{T}"/> instances.
    /// </summary>
    public abstract class ObserverPSCmdlet : PSCmdlet
    {
        /// <summary>
        /// <para type='description'>Sets whether the operations of this cmdlet should be executed asynchronously
        /// with one or more jobs output from the cmdlet to represent the operation(s) being invoked.
        /// </para>
        /// </summary>
        [Parameter]
        public SwitchParameter AsJob { get; set; }

        /// <summary>
        /// The command of the job created by this cmdlet.
        /// </summary>
        protected virtual string? JobCommand { get => MyInvocation.Line; }
        /// <summary>
        /// The name of the job created by this cmdlet.
        /// </summary>
        protected virtual string? JobName { get => null; }
        /// <summary>
        /// If <see langword="true"/>, every time <see cref="Observe{T}(IObservable{T})"/> is called when <see cref="AsJob"/>
        /// is also <see langword="true"/> a job will be written to the pipeline representing the <see cref="IObservable{T}"/>.
        /// </summary>
        protected virtual bool OutputJobForEachObservable { get => false; }
        /// <summary>
        /// Cached output from <see cref="IObservable{T}"/> sources identified by the cmdlet 
        /// that has not yet been written to the cmdlet pipeline.
        /// </summary>
        private ConcurrentQueue<(bool isErrorRecord, object? output)>? _output;
        /// <summary>
        /// Used as a source for <see cref="_job"/> and <see cref="_cmdletObserver"/> as a generator of
        /// <see cref="IObservable{T}"/> sources for job or cmdlet output.
        /// </summary>
        private Generator? _generator;
        /// <summary>
        /// The equivalent of <see cref="_job"/> if the cmdlet is invoked synchronously.
        /// </summary>
        private CmdletObserver? _cmdletObserver;
        /// <summary>
        /// The job created by the cmdlet.
        /// </summary>
        private ObserverJob? _job;
        /// <summary>
        /// Starts observing a <see cref="IObservable{T}"/> source. If the <see cref="AsJob"/> parameter is <see langword="true"/>,
        /// <paramref name="source"/> will be observed by a job; otherwise it will be observed internally by the cmdlet.
        /// </summary>
        /// <typeparam name="T">The output type of <paramref name="source"/>.</typeparam>
        /// <param name="source">The publisher to be observed.</param>
        public void Observe<T>(IObservable<T> source)
        {
            if (Stopping) return;
            WriteObserverContents(false);
            if (AsJob && OutputJobForEachObservable)
            {
                var job = ObserverJob.Create(source, JobCommand, JobName);
                JobRepository.Add(job);
                WriteObject(job);
                return;
            }
            if (_generator is null)
            {
                _generator = new Generator();
            }

            if (AsJob)
            {
                _job ??= ObserverJob.Create(_generator, ExecutionMode.Concurrent, JobCommand, JobName);
            }
            else
            {
                _output ??= new ConcurrentQueue<(bool isErrorRecord, object? output)>();
                _cmdletObserver ??= new CmdletObserver(ExecutionMode.Concurrent, _generator, _output);
            }

            _generator.AddObservable(source.Select(i => (i as object)));
        }

        protected sealed override void BeginProcessing()
        {
            base.BeginProcessing();
        }
        protected sealed override void ProcessRecord()
        {
            base.ProcessRecord();
            WriteObserverContents(false);
            ProcessRecordInner();
        }
        protected sealed override void EndProcessing()
        {
            base.EndProcessing();
            WriteObserverContents(false);
            EndProcessingInner();
            _generator?.Complete();
            if (_job != null)
            {
                JobRepository.Add(_job);
                WriteObject(_job);
            }
WriteDebug($"EndProcessing() : Writing observer contents until complete.");
            WriteObserverContents(true);
        }
        /// <summary>
        /// Writes all contents cached from observer output before continuing.
        /// </summary>
        /// <param name="untilComplete">If <see langword="true"/>, this method will block until all 
        /// currently executing observers have completed before continuing.</param>
        protected void WriteObserverContents(bool untilComplete)
        {
            if (_cmdletObserver != null && _output != null)
            {
                do
                {
                    while (_output.TryDequeue(out var data))
                    {
                        if (data.isErrorRecord)
                        {
                            WriteError((ErrorRecord?)data.output);
                        }
                        else
                        {
                            WriteObject(data.output);
                        }
                    }
                }
                while (untilComplete && _cmdletObserver.ObserverCount != 0 && !Stopping);
            }
        }
        /// <summary>
        /// Cancels all observers.
        /// </summary>
        protected override void StopProcessing()
        {
            base.StopProcessing();
            _cmdletObserver?.Stop();
        }
        /// <summary>
        /// Implementation of ProcessRecord.
        /// </summary>
        protected virtual void ProcessRecordInner() { }
        /// <summary>
        /// Implementation of EndProcessing.
        /// </summary>
        protected virtual void EndProcessingInner() { }
        private class Generator : IObservable<IObservable<object>>
        {
            internal Generator()
            {
                _syncRoot = new object();
                _observers = new List<IObserver<IObservable<object>>>();
                _values = new List<IObservable<object>>();
            }

            private readonly List<IObserver<IObservable<object>>> _observers;
            private readonly List<IObservable<object>> _values;
            private readonly object _syncRoot;
            private bool _completed;
            public IDisposable Subscribe(IObserver<IObservable<object>> observer)
            {
                lock (_syncRoot)
                {
                    foreach (var item in _values)
                    {
                        observer.OnNext(item);
                    }
                    if (_completed)
                    {
                        return new ActionRegistration(() => { });
                    }
                    else
                    {
                        _observers.Add(observer);
                        return new ActionRegistration(() =>
                        {
                            lock (_syncRoot)
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
            public void AddObservable(IObservable<object?> observable)
            {
                lock (_syncRoot)
                {
                    foreach (var observer in _observers)
                    {
                        observer.OnNext(observable!);
                    }
                }
            }
            public void Complete()
            {
                lock (_syncRoot)
                {
                    _completed = true;
                    _observers.ForEach(i => i.OnCompleted());
                    _observers.Clear();
                }
            }
            private class ActionRegistration : IDisposable
            {
                public ActionRegistration(Action onDisposed)
                {
                    _onDisposed = onDisposed ?? throw new ArgumentNullException(nameof(onDisposed));
                }
                private readonly Action _onDisposed;
                public void Dispose()
                {
                    _onDisposed();
                }
            }
        }
        private class CmdletObserver
        {
            public CmdletObserver(ExecutionMode executionType, IObservable<IObservable<object>> observableGenerator, ConcurrentQueue<(bool, object?)> output)
            {
                _output = output;
                _executionMode = executionType;
                lock (_syncRoot)
                {
                    if (_started) throw new InvalidOperationException("The job has already been started.");
                    _started = true;
                    _isGeneratorComplete = false;

                    ActionObserver<IObservable<object>>.Subscribe(
                    observableGenerator,
                    onSubscribed: null,
                    onNext: (observer, publisher, newObserver) =>
                    {
                        lock (_syncRoot)
                        {
                            if (_executionMode == ExecutionMode.Concurrent)
                            {
                                ActionObserver<object>.Subscribe(
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
                                        ActionObserver<object>.Subscribe(
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

            private ConcurrentQueue<(bool isErrorRecord, object? output)> _output;
            public bool IsComplete
            {
                get
                {
                    lock (_syncRoot)
                    {
                        return _started && _observers.Count == 0 && _startObservers.Count == 0 && _isGeneratorComplete;
                    }
                }
            }
            public int ObserverCount
            {
                get
                {
                    lock (_syncRoot)
                    {
                        return _observers.Count + _startObservers.Count;
                    }
                }
            }
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
            public void Stop()
            {
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
            protected void StartJob<T>(IObservable<IObservable<T>> observableGenerator)
            {
                lock (_syncRoot)
                {
                    if (_started) throw new InvalidOperationException("The job has already been started.");
                    _started = true;
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
                _output.Enqueue((false, PSObject.AsPSObject(item)));
            }
            private void OnErrorAction<T>(IObserver<T> observer, IObservable<T> publisher, Exception error)
            {
                lock (_syncRoot)
                {
                    _hadErrors = true;
                    _output.Enqueue((true, new ErrorRecord(
                        error,
                        "ObservedError",
                        ErrorCategory.NotSpecified,
                        publisher
                        )));

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
                }
            }
            private void OnCanceledAction<T>(IObserver<T> observer, IObservable<T> publisher)
                => OnCompletedAction(observer, publisher);
            #endregion
        }
    }
}
