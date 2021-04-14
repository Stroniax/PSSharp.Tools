using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reactive.Linq;

namespace PSSharp
{
    /// <summary>
    /// A job to observe one or more <see cref="IObservable{T}"/> or <see cref="IPSObservable{T}"/> instances.
    /// </summary>
    public class ObserverJob : AdvancedJobBase
    {
        #region nested classes
        private class ChildJobList : IList<Job>
        {
            private List<Job> _jobs = new List<Job>();
            internal void ForEach(Action<Job> action)
            {
                foreach (var job in _jobs)
                {
                    action(job);
                }
            }
            Job IList<Job>.this[int index] { get => _jobs[index]; set => throw new NotSupportedException(); }

            public int Count => ((ICollection<Job>)_jobs).Count;

            public bool IsReadOnly => true;

            void ICollection<Job>.Add(Job item) => throw new NotSupportedException();

            void ICollection<Job>.Clear() => throw new NotSupportedException();

            public bool Contains(Job item) => _jobs.Contains(item);

            void ICollection<Job>.CopyTo(Job[] array, int arrayIndex) => _jobs.CopyTo(array, arrayIndex);

            public IEnumerator<Job> GetEnumerator() => _jobs.GetEnumerator();

            public int IndexOf(Job item) => _jobs.IndexOf(item);

            void IList<Job>.Insert(int index, Job item) => throw new NotSupportedException();

            bool ICollection<Job>.Remove(Job item) => throw new NotSupportedException();

            void IList<Job>.RemoveAt(int index) => throw new NotSupportedException();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


            public void Add<T>(ObserverChildJob<T> childJob)
            {
                _jobs.Add(childJob);
            }
        }
        #endregion

        #region private fields
        new private ChildJobList ChildJobs { get => (ChildJobList)base.ChildJobs; }
        private readonly object _sync;
        private readonly ExecutionMode _executionMode;
        private int _queuedChildJobs;
        private int _executingChildJobs;
        private int _completeChildJobs;
        private int _failedChildJobs;
        private int _stoppedChildJobs;
        private bool _isSealed;
        #endregion
        #region public members
        public event EventHandler<ChildJobStartedEventArgs>? ChildJobStarted;
        public override void ResumeJob()
        {
            SetJobState(JobState.Running);
            MaybeStartNextJob();
        }
        public override void StartJob()
        {
            if (State != JobState.NotStarted) throw new InvalidJobStateException(State, "Cannot start job unless the job state is NotStarted.");
            SetJobState(JobState.Running);
            MaybeStartNextJob();
        }
        public override void StopJob(bool force, string reason)
        {
            SetJobState(JobState.Stopping);
            ChildJobs.ForEach(i => i.StopJob());
        }
        public override void SuspendJob(bool force, string reason)
        {
            SetJobState(JobState.Suspending);
            if (force)
            {
                ChildJobs.Where(j => j.JobStateInfo.State == JobState.Running).ToList().ForEach(i => i.StopJob());
            }
        }
        public override void UnblockJob()
        {
            throw new PSNotSupportedException();
        }
        #endregion
        #region protected members
        /// <summary>
        /// Indicates that <paramref name="observable"/> should be observed when <see cref="StartJob"/> is executed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="observable"></param>
        protected void Observe<T>(IObservable<T> observable)
        {
            lock (_sync)
            {
                if (_isSealed) throw new InvalidOperationException("Cannot add observation after the job has been sealed.");
                var job = GetObserverJob(observable);
                job.StateChanged += OnChildJobStateChanged;
                ChildJobs.Add(job);
                _queuedChildJobs++;
                MaybeStartNextJob();
            }
        }
        /// <summary>
        /// Indicates that <paramref name="observable"/> should be observed when <see cref="StartJob"/> is executed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="observable"></param>
        protected void Observe<T>(IPSObservable<T> observable)
        {
            lock (_sync)
            {
                if (_isSealed) throw new InvalidOperationException("Cannot add observation after the job has been sealed.");
                var job = GetObserverJob(observable);
                job.StateChanged += OnChildJobStateChanged;
                ChildJobs.Add(job);
                _queuedChildJobs++;
                MaybeStartNextJob();
            }
        }
        /// <summary>
        /// Creates a new job that observes the <paramref name="observable"/> instance.
        /// </summary>
        /// <typeparam name="T">The observed type.</typeparam>
        /// <param name="observable">The instance to be observed by the job.</param>
        protected virtual ObserverChildJob<T> GetObserverJob<T>(IObservable<T> observable) => new ObserverChildJob<T>(observable);
        /// <summary>
        /// Creates a new job that observes the <paramref name="observable"/> instance.
        /// </summary>
        /// <typeparam name="T">The observed type.</typeparam>
        /// <param name="observable">The instance to be observed by the job.</param>
        protected virtual ObserverChildJob<T> GetObserverJob<T>(IPSObservable<T> observable) => new ObserverChildJob<T>(observable);
        /// <summary>
        /// Indicates that no additional observation will be required, 
        /// allowing this job to finish.
        /// </summary>
        protected void Seal()
        {
            lock (_sync)
            {
                if (_isSealed) throw new InvalidOperationException("The job has already been sealed.");
                _isSealed = true;
                MaybeStartNextJob();
            }
        }
        /// <summary>
        /// Returns the state that the job should conclude with after processing child jobs has completed.
        /// By default, <see cref="JobState.Stopped"/> will be returned if the current state is 
        /// <see cref="JobState.Stopping"/>; <see cref="JobState.Failed"/> will be returned if any child job
        /// failed and the <see cref="ExecutionMode"/> is <see cref="ExecutionMode.ConsecutiveUntilError"/>;
        /// otherwise, <see cref="JobState.Completed"/> will be returned.
        /// </summary>
        /// <returns>A terminal job state.</returns>
        protected virtual JobState GetCompletionState()
        {
            if (_executionMode == ExecutionMode.ConsecutiveUntilError
                        && _failedChildJobs > 0)
            {
                return JobState.Failed;
            }
            else if (State == JobState.Stopping)
            {
                return JobState.Stopped;
            }
            else
            {
                return JobState.Completed;
            }
        }
        #endregion
        #region private members
        /// <summary>
        /// Returns true if the <see cref="ExecutionMode"/> and <see cref="JobState"/> of the current job
        /// allows a new child job to be started.
        /// </summary>
        /// <returns></returns>
        private bool CanStartNextJob()
        {
            // I can't directly check the state of child jobs because they can change while I'm looking at them.
            // Instead, I'll maintain a numeric record of executing jobs.
            lock (_sync)
            {
                if (State != JobState.Running) return false;
                switch (_executionMode)
                {
                    case ExecutionMode.Concurrent:
                        return true;
                    case ExecutionMode.Consecutive:
                        {
                            // return true of no job is currently executing
                            return _executingChildJobs == 0;
                        }
                    case ExecutionMode.ConsecutiveUntilError:
                        {
                            // return true if no job is currently executing, nor has any job failed
                            return _executingChildJobs == 0
                                && _failedChildJobs == 0;
                        }
                    default:
                        throw new InvalidOperationException("Could not determine behavior for current execution mode.");
                }
            }
        }
        private void MaybeStartNextJob()
        {
            lock (_sync)
            {
                if (_executionMode == ExecutionMode.ConsecutiveUntilError
                    && _failedChildJobs > 0)
                {
                    ChildJobs.ForEach(j =>
                    {
                        if (j.JobStateInfo.State == JobState.NotStarted)
                        {
                            j.StopJob();
                        }
                    });
                    MaybeFinishJob();
                }

                dynamic job = ChildJobs.Where(j => j.JobStateInfo.State == JobState.NotStarted).FirstOrDefault();
                if (job is null || !CanStartNextJob())
                {
                    MaybeFinishJob();
                    return;
                }
                _executingChildJobs++;
                _queuedChildJobs--;
                job.StartJob();
                ChildJobStarted?.Invoke(this, new ChildJobStartedEventArgs(job));
            }
        }
        private void MaybeFinishJob()
        {
            lock(_sync)
            {
                if (_isSealed
                    && _executingChildJobs == 0
                    && _queuedChildJobs == 0)
                {
                    SetJobState(GetCompletionState());
                }
            }
        }
        private void OnChildJobStateChanged(object sender, JobStateEventArgs e)
        {
            if (sender is null) return;
            lock (_sync)
            {
                switch (e.JobStateInfo.State)
                {
                    case JobState.Completed:
                        {
                            if (e.PreviousJobStateInfo.State == JobState.Running)
                            {
                                _completeChildJobs++;
                                _executingChildJobs--;
                            }
                        }
                        break;
                    case JobState.Failed:
                        {
                            if (e.PreviousJobStateInfo.State == JobState.Running)
                            {
                                _failedChildJobs++;
                                _executingChildJobs--;
                            }
                        }
                        break;
                    case JobState.Stopped:
                        {
                            if (e.PreviousJobStateInfo.State == JobState.Running)
                            {
                                _stoppedChildJobs++;
                                _executingChildJobs--;
                            }
                            else if (e.PreviousJobStateInfo.State == JobState.NotStarted)
                            {
                                _queuedChildJobs--;
                                _stoppedChildJobs++;
                            }
                        }
                        break;
                }

                if (State == JobState.Suspending) SetJobState(JobState.Suspended);
                else MaybeStartNextJob();
            }
        }
        #endregion
        #region Constructors
        protected ObserverJob(string? command, string? name, ExecutionMode executionMode)
            : base(command, name, new ChildJobList())
        {
            _sync = new object();
            _executionMode = executionMode;
        }

        public static ObserverJob Create<T>(IObservable<IObservable<T>> observableGenerator, ExecutionMode executionMode = ExecutionMode.Concurrent, string? command = null, string? name = null)
            => new GeneratedObserverJob<T>(command, name, executionMode, observableGenerator);
        public static ObserverJob Create<T>(IObservable<IPSObservable<T>> observableGenerator, ExecutionMode executionMode = ExecutionMode.Concurrent, string? command = null, string? name = null)
            => new GeneratedObserverJob<T>(command, name, executionMode, observableGenerator);
        public static ObserverJob Create<T>(IEnumerable<IObservable<T>> observables, ExecutionMode executionMode = ExecutionMode.Concurrent, string? command = null, string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            foreach (var observable in observables)
            {
                job.Observe(observable);
            }
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T>(ExecutionMode executionMode = ExecutionMode.Concurrent, string? command = null, string? name = null, params IObservable<T>[] observables)
        {
            var job = new ObserverJob(command, name, executionMode);
            foreach (var observable in observables)
            {
                job.Observe(observable);
            }
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T>(IObservable<T> observable, string? command = null, string? name = null)
        {
            var job = new ObserverJob(command, name, ExecutionMode.Concurrent);
            job.Observe(observable);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2>(IObservable<T1> p1,
                                                  IObservable<T2> p2,
                                                  ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                  string? command = null,
                                                  string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3>(IObservable<T1> p1,
                                                      IObservable<T2> p2,
                                                      IObservable<T3> p3,
                                                      ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                      string? command = null,
                                                      string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4>(IObservable<T1> p1,
                                                          IObservable<T2> p2,
                                                          IObservable<T3> p3,
                                                          IObservable<T4> p4,
                                                          ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                          string? command = null,
                                                          string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4, T5>(IObservable<T1> p1,
                                                              IObservable<T2> p2,
                                                              IObservable<T3> p3,
                                                              IObservable<T4> p4,
                                                              IObservable<T5> p5,
                                                              ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                              string? command = null,
                                                              string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Observe(p5);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4, T5, T6>(IObservable<T1> p1,
                                                                  IObservable<T2> p2,
                                                                  IObservable<T3> p3,
                                                                  IObservable<T4> p4,
                                                                  IObservable<T5> p5,
                                                                  IObservable<T6> p6,
                                                                  ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                                  string? command = null,
                                                                  string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Observe(p5);
            job.Observe(p6);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4, T5, T6, T7>(IObservable<T1> p1,
                                                                      IObservable<T2> p2,
                                                                      IObservable<T3> p3,
                                                                      IObservable<T4> p4,
                                                                      IObservable<T5> p5,
                                                                      IObservable<T6> p6,
                                                                      IObservable<T7> p7,
                                                                      ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                                      string? command = null,
                                                                      string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Observe(p5);
            job.Observe(p6);
            job.Observe(p7);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4, T5, T6, T7, T8>(IObservable<T1> p1,
                                                                          IObservable<T2> p2,
                                                                          IObservable<T3> p3,
                                                                          IObservable<T4> p4,
                                                                          IObservable<T5> p5,
                                                                          IObservable<T6> p6,
                                                                          IObservable<T7> p7,
                                                                          IObservable<T8> p8,
                                                                          ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                                          string? command = null,
                                                                          string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Observe(p5);
            job.Observe(p6);
            job.Observe(p7);
            job.Observe(p8);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IObservable<T1> p1,
                                                                              IObservable<T2> p2,
                                                                              IObservable<T3> p3,
                                                                              IObservable<T4> p4,
                                                                              IObservable<T5> p5,
                                                                              IObservable<T6> p6,
                                                                              IObservable<T7> p7,
                                                                              IObservable<T8> p8,
                                                                              IObservable<T9> p9,
                                                                              ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                                              string? command = null,
                                                                              string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Observe(p5);
            job.Observe(p6);
            job.Observe(p7);
            job.Observe(p8);
            job.Observe(p9);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IObservable<T1> p1,
                                                                                   IObservable<T2> p2,
                                                                                   IObservable<T3> p3,
                                                                                   IObservable<T4> p4,
                                                                                   IObservable<T5> p5,
                                                                                   IObservable<T6> p6,
                                                                                   IObservable<T7> p7,
                                                                                   IObservable<T8> p8,
                                                                                   IObservable<T9> p9,
                                                                                   IObservable<T10> p10,
                                                                                   ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                                                   string? command = null,
                                                                                   string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Observe(p5);
            job.Observe(p6);
            job.Observe(p7);
            job.Observe(p8);
            job.Observe(p9);
            job.Observe(p10);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T>(IEnumerable<IPSObservable<T>> observables, ExecutionMode executionMode = ExecutionMode.Concurrent, string? command = null, string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            foreach (var observable in observables)
            {
                job.Observe(observable);
            }
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T>(ExecutionMode executionMode = ExecutionMode.Concurrent, string? command = null, string? name = null, params IPSObservable<T>[] observables)
        {
            var job = new ObserverJob(command, name, executionMode);
            foreach (var observable in observables)
            {
                job.Observe(observable);
            }
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T>(IPSObservable<T> observable, string? command = null, string? name = null)
        {
            var job = new ObserverJob(command, name, ExecutionMode.Concurrent);
            job.Observe(observable);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2>(IPSObservable<T1> p1,
                                                  IPSObservable<T2> p2,
                                                  ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                  string? command = null,
                                                  string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3>(IPSObservable<T1> p1,
                                                      IPSObservable<T2> p2,
                                                      IPSObservable<T3> p3,
                                                      ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                      string? command = null,
                                                      string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4>(IPSObservable<T1> p1,
                                                          IPSObservable<T2> p2,
                                                          IPSObservable<T3> p3,
                                                          IPSObservable<T4> p4,
                                                          ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                          string? command = null,
                                                          string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4, T5>(IPSObservable<T1> p1,
                                                              IPSObservable<T2> p2,
                                                              IPSObservable<T3> p3,
                                                              IPSObservable<T4> p4,
                                                              IPSObservable<T5> p5,
                                                              ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                              string? command = null,
                                                              string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Observe(p5);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4, T5, T6>(IPSObservable<T1> p1,
                                                                  IPSObservable<T2> p2,
                                                                  IPSObservable<T3> p3,
                                                                  IPSObservable<T4> p4,
                                                                  IPSObservable<T5> p5,
                                                                  IPSObservable<T6> p6,
                                                                  ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                                  string? command = null,
                                                                  string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Observe(p5);
            job.Observe(p6);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4, T5, T6, T7>(IPSObservable<T1> p1,
                                                                      IPSObservable<T2> p2,
                                                                      IPSObservable<T3> p3,
                                                                      IPSObservable<T4> p4,
                                                                      IPSObservable<T5> p5,
                                                                      IPSObservable<T6> p6,
                                                                      IPSObservable<T7> p7,
                                                                      ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                                      string? command = null,
                                                                      string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Observe(p5);
            job.Observe(p6);
            job.Observe(p7);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4, T5, T6, T7, T8>(IPSObservable<T1> p1,
                                                                          IPSObservable<T2> p2,
                                                                          IPSObservable<T3> p3,
                                                                          IPSObservable<T4> p4,
                                                                          IPSObservable<T5> p5,
                                                                          IPSObservable<T6> p6,
                                                                          IPSObservable<T7> p7,
                                                                          IPSObservable<T8> p8,
                                                                          ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                                          string? command = null,
                                                                          string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Observe(p5);
            job.Observe(p6);
            job.Observe(p7);
            job.Observe(p8);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IPSObservable<T1> p1,
                                                                              IPSObservable<T2> p2,
                                                                              IPSObservable<T3> p3,
                                                                              IPSObservable<T4> p4,
                                                                              IPSObservable<T5> p5,
                                                                              IPSObservable<T6> p6,
                                                                              IPSObservable<T7> p7,
                                                                              IPSObservable<T8> p8,
                                                                              IPSObservable<T9> p9,
                                                                              ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                                              string? command = null,
                                                                              string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Observe(p5);
            job.Observe(p6);
            job.Observe(p7);
            job.Observe(p8);
            job.Observe(p9);
            job.Seal();
            return job;
        }
        public static ObserverJob Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IPSObservable<T1> p1,
                                                                                   IPSObservable<T2> p2,
                                                                                   IPSObservable<T3> p3,
                                                                                   IPSObservable<T4> p4,
                                                                                   IPSObservable<T5> p5,
                                                                                   IPSObservable<T6> p6,
                                                                                   IPSObservable<T7> p7,
                                                                                   IPSObservable<T8> p8,
                                                                                   IPSObservable<T9> p9,
                                                                                   IPSObservable<T10> p10,
                                                                                   ExecutionMode executionMode = ExecutionMode.Concurrent,
                                                                                   string? command = null,
                                                                                   string? name = null)
        {
            var job = new ObserverJob(command, name, executionMode);
            job.Observe(p1);
            job.Observe(p2);
            job.Observe(p3);
            job.Observe(p4);
            job.Observe(p5);
            job.Observe(p6);
            job.Observe(p7);
            job.Observe(p8);
            job.Observe(p9);
            job.Observe(p10);
            job.Seal();
            return job;
        }
        #endregion Constructors
    }
}
