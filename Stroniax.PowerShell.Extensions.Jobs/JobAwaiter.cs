using Stroniax.PowerShell.Extensions;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Text;

namespace Stroniax.Powershell
{
    /// <summary>
    /// An awaiter used to <see langword="await"/> a PowerShell <see cref="Job"/>.
    /// </summary>
    public struct JobAwaiter : INotifyCompletion
    {
        /// <summary>
        /// Indicates if this <see cref="Job"/> has completed.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return _job.IsFinished();
            }
        }

        private readonly Job _job;
        internal JobAwaiter(Job job)
        {
            _job = job ?? throw new ArgumentNullException(nameof(job));
        }
        /// <summary>
        /// Waits for the <see cref="Job"/> to complete.
        /// </summary>
        public void GetResult()
        {
            // it seems... await Job does the following:
            // Creates the awaiter (JobAwaiter, in this case)
            // executes JobAwaiter.OnCompleted([the rest of the method that follows the "await" keyword])
            // asynchronously waits for JobAwaiter.IsCompleted to be true
            // executes JobAwaiter.GetResult()
            // I'm not sure where the above two lines fit into the OnCompleted call. Presumably at the beginning.
            while (!_job.IsFinished())
            {
                _job.Finished.WaitOne(100);
            }
        }
        /// <summary>
        /// Subscribes an <see cref="Action"/> to be executed when <see cref="IsCompleted"/> is <see langword="true"/>.
        /// </summary>
        /// <param name="continuation"></param>
        public void OnCompleted(Action continuation)
        {
            if (_job.IsFinished())
            {
                continuation.Invoke();
            }
            else
            {
                _job.StateChanged += (a, b) =>
                {
                    var job = a as Job;
                    if (job?.IsFinished() ?? false)
                    {
                        continuation.Invoke();
                    }
                };
            }
        }
    }
}
