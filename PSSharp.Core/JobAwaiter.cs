using PSSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace PSSharp
{
    /// <summary>
    /// Used to <see langword="await"/> a PowerShell <see cref="Job"/>. Note that if the job
    /// fails, a generic <see cref="JobFailedException"/> will be thrown by the <see cref="GetResult"/>
    /// method not representing the terminal error of the job.
    /// </summary>
    public struct JobAwaiter : IAwaiter
    {
        private readonly Job _job;
        private bool _finished;
        private object _sync;
        private Queue<Action> _continuations;

        /// <summary>
        /// Indicates if the <see cref="JobStateInfo.State"/> of the <see cref="Job"/> is any of the following
        /// terminal job states:
        /// <list type="bullet">
        /// <item><see cref="JobState.Completed"/></item>
        /// <item><see cref="JobState.Failed"/></item>
        /// <item><see cref="JobState.Stopped"/></item>
        /// <item><see cref="JobState.Disconnected"/></item>
        /// </list>
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return _finished;
            }
        }
        internal JobAwaiter(Job job)
        {
            _finished = false;
            _sync = new object();
            _continuations = new Queue<Action>();
            _job = job ?? throw new ArgumentNullException(nameof(job));
            _job.StateChanged += OnJobStateChanged;
            if (_job.IsFinished())
            {
                _job.StateChanged -= OnJobStateChanged;
                _finished = true;
            }
        }
        private void OnJobStateChanged(object sender, JobStateEventArgs e)
        {
            lock (_sync)
            {
                var state = e.JobStateInfo.State;
                switch (state)
                {
                    case JobState.Completed:
                    case JobState.Failed:
                    case JobState.Stopped:
                    case JobState.Disconnected:
                        {
                            _finished = true;
                            while (_continuations.Count > 0)
                            {
                                _continuations.Dequeue()();
                            }
                        }
                        break;
                }
            }
        }
        /// <summary>
        /// Blocks until the job has completed (using <see cref="Job.Finished"/>),
        /// and throws <see cref="JobFailedException"/> if the state of the job is 
        /// <see cref="JobState.Failed"/>.
        /// </summary>
        public void GetResult()
        {
            // it seems... await Job does the following:
            // Creates the awaiter (JobAwaiter, in this case)
            // executes JobAwaiter.OnCompleted(Action), passing the rest of the method that follows the "await" keyword as Action
            // asynchronously waits for JobAwaiter.IsCompleted to be true
            // executes JobAwaiter.GetResult()
            // I'm not sure where the above two lines fit into the OnCompleted call. Presumably at the beginning.
            _job.Finished.WaitOne();
            if (_job.JobStateInfo.State == JobState.Failed)
                throw new JobFailedException();
        }
        /// <summary>
        /// Subscribes an <see cref="Action"/> to be executed when <see cref="IsCompleted"/> is <see langword="true"/>.
        /// </summary>
        /// <param name="continuation"></param>
        public void OnCompleted(Action continuation)
        {
            lock(_sync)
            {
                if (_finished)
                {
                    continuation();
                }
                else
                {
                    _continuations.Enqueue(continuation);
                }
            }
        }
    }
}
