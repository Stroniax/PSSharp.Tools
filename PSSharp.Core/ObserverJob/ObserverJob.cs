using PSSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// PowerShell <see cref="Job"/> implementation of <see cref="IObserver{T}"/>, allowing a job
    /// to subscribe to any <see cref="IObservable{T}"/> publisher.
    /// </summary>
    /// <typeparam name="T">The observed item type.</typeparam>
    public class ObserverJob<T> : Job, IAwaitableObserver<T>, IObserver<T>, IAwaitable<IEnumerable<T>>
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="command">The PowerShell script command being executed by the job, such as <see cref="InvocationInfo.Line"/>.</param>
        /// <param name="name">The name of the job.</param>
        /// <param name="source"></param>
        public ObserverJob(string? command, string? name, IObservable<T> source)
            : base(command, name)
        {
            PSJobTypeName = "ObserverJob";
            _source = source;
            _awaiter = new ObserverJobAwaiter<T>(this);
            _cancellation = source.Subscribe(this);
        }
        protected ObserverJob(string? command, string? name, Guid instanceId, IObservable<T> source)
            :base(command, name, instanceId)
        {
            PSJobTypeName = "ObserverJob";
            _source = source;
            _awaiter = new ObserverJobAwaiter<T>(this);
            _cancellation = source.Subscribe(this);
        }
        protected ObserverJob(string? command, string? name, JobIdentifier token, IObservable<T> source)
            :base(command, name, token)
        {
            PSJobTypeName = "ObserverJob";
            _source = source;
            _awaiter = new ObserverJobAwaiter<T>(this);
            _cancellation = source.Subscribe(this);
        }
        
        private readonly IDisposable _cancellation;
        private readonly IObservable<T> _source;
        private readonly ObserverJobAwaiter<T> _awaiter;

        /// <inheritdoc/>
        public sealed override bool HasMoreData => this.AnyStreamHasData();
        /// <inheritdoc/>
        public override string Location => Environment.MachineName;
        /// <inheritdoc/>
        public override string StatusMessage => string.Empty;
        /// <inheritdoc cref="IAwaitable{TResult}.GetAwaiter"/>
        public ObserverJobAwaiter<T> GetAwaiter() => _awaiter;
        IAwaiter<IEnumerable<T>> IAwaitable<IEnumerable<T>>.GetAwaiter() => GetAwaiter();
        IAwaiter IAwaitable.GetAwaiter() => GetAwaiter();
        /// <summary>
        /// Executed when the <see cref="IObservable{T}"/> source has no more items incoming.
        /// Base implementation concludes the job with <see cref="JobState.Completed"/>
        /// and triggers the awaiter using <see cref="SignalAwaiter(Exception?, bool)"/>.
        /// </summary>
        protected virtual void OnCompleted()
        {
            SetJobState(JobState.Completed);
            _awaiter.Complete(null, false);
        }
        void IObserver<T>.OnCompleted() => OnCompleted();
        /// <summary>
        /// Executed when the <see cref="IObservable{T}"/> source is interrupted.
        /// Base implementation adds <paramref name="error"/> to <see cref="Job.Error"/>, 
        /// triggers the awaiter using <see cref="SignalAwaiter(Exception?, bool)"/>,
        /// and terminates the job with <see cref="JobState.Failed"/>.
        /// </summary>
        /// <param name="error">The error experienced by the <see cref="IObservable{T}"/>.</param>
        protected virtual void OnError(Exception error)
        {
            Error.Add(new ErrorRecord(
                error,
                "ObserverOnErrorInvocation",
                ErrorCategory.NotSpecified,
                _source
                ));
            SetJobState(JobState.Failed);
            _awaiter.Complete(error, false);
        }
        void IObserver<T>.OnError(Exception error) => OnError(error);
        /// <summary>
        /// Executed when the <see cref="IObservable{T}"/> source publishes an item.
        /// Base implementation adds <paramref name="value"/> to <see cref="Job.Output"/> and
        /// the awaiter (via <see cref="AddToAwaiter(T)"/>.
        /// </summary>
        /// <param name="value">The value provided by the <see cref="IObservable{T}"/>.</param>
        protected virtual void OnNext(T value)
        {
            AddToAwaiter(value);
            Output.Add(PSObject.AsPSObject(value));
        }
        /// <summary>
        /// Signals to the <see cref="ObserverJobAwaiter{T}"/> that the job has concluded.
        /// <para>If <paramref name="cancelled"/> is <see langword="true"/>, the awaiter reports
        /// <see cref="ObserverJobAwaiter{T}.IsCancelled"/> as <see langword="true"/>.</para>
        /// <para>If <paramref name="haltingException"/> is not <see langword="null"/>, the awaiter throws
        /// <paramref name="haltingException"/> when <see cref="ObserverJobAwaiter{T}.GetResult"/> is 
        /// called and reports <see cref="ObserverJobAwaiter{T}.IsFailed"/> as <see langword="true"/>.
        /// </para>
        /// </summary>
        /// <param name="haltingException"></param>
        /// <param name="cancelled"></param>
        protected void SignalAwaiter(Exception? haltingException, bool cancelled)
        {
            _awaiter.Complete(haltingException, cancelled);
        }
        /// <summary>
        /// Adds an item to the awaiter's result.
        /// </summary>
        /// <param name="value"></param>
        protected void AddToAwaiter(T value) => _awaiter.Add(value);
        void IObserver<T>.OnNext(T value) => OnNext(value);
        /// <inheritdoc/>
        public override void StopJob()
        {
            SetJobState(JobState.Stopping);
            _cancellation.Dispose();
            SetJobState(JobState.Stopped);
            _awaiter.Complete(null, true);
        }
    }
    /// <summary>
    /// Used to await an <see cref="ObserverJob{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ObserverJobAwaiter<T> : IAwaiter<IEnumerable<T>>
    {
        private ObserverJob<T> _job;
        private ConcurrentQueue<T> _result;
        private ConcurrentBag<Action> _onCompletedNoParameters;
        private ConcurrentBag<Action<ObserverJob<T>>> _onCompletedWithParameters;
        private bool _isCompleted;
        private bool _isCancelled;
        private readonly object _syncRoot;
        private Exception? _error;
        internal ObserverJobAwaiter(ObserverJob<T> source)
        {
            _result = new ConcurrentQueue<T>();
            _onCompletedNoParameters = new ConcurrentBag<Action>();
            _onCompletedWithParameters = new ConcurrentBag<Action<ObserverJob<T>>>();
            _syncRoot = new object();
            _job = source;
        }
        internal void Add(T value) => _result.Enqueue(value);
        internal void Complete(Exception? error, bool cancelled)
        {
            lock (_syncRoot)
            {
                if (cancelled)
                {
                    _isCancelled = true;
                    _isCompleted = true;
                }
                else if (error != null)
                {
                    // empty result cache
                    while (_result.TryDequeue(out _)) ;
                    _error = error;
                    _isCompleted = true;
                }
                else
                {
                    _isCompleted = true;
                }
                while (_onCompletedNoParameters.TryTake(out var action))
                {
                    try { action.Invoke(); } catch { }
                }
                while (_onCompletedWithParameters.TryTake(out var action))
                {
                    try { action.Invoke(_job); } catch { }
                }
            }
        }
        public bool IsCompleted
        {
            get
            {
                lock (_syncRoot)
                {
                    return _isCompleted;
                }
            }
        }
        public bool IsFailed
        {
            get
            {
                lock (_syncRoot)
                {
                    return _error != null;
                }
            }
        }
        public bool IsCancelled
        {
            get
            {
                lock (_syncRoot)
                {
                    return _isCancelled;
                }
            }
        }
        public IEnumerable<T> GetResult()
        {
            if (_error != null)
            {
                throw _error;
            }
            return _result.ToArray();
        }
        void IAwaiter.GetResult() => GetResult();
        public void OnCompleted(Action continuation)
        {
            lock (_syncRoot)
            {
                if (_isCompleted)
                {
                    continuation.Invoke();
                }
                else
                {
                    _onCompletedNoParameters.Add(continuation);
                }
            }
        }
        public void OnCompleted(Action<ObserverJob<T>> continuation)
        {
            lock (_syncRoot)
            {
                if (_isCompleted)
                {
                    continuation.Invoke(_job);
                }
                else
                {
                    _onCompletedWithParameters.Add(continuation);
                }
            }
        }
    }
}
