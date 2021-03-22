using Stroniax.Powershell;
using System;
using System.Management.Automation;

namespace Stroniax.PowerShell.Extensions
{
    /// <summary>
    /// Extensions for the <see cref="Job"/> class.
    /// </summary>
    public static class JobExtensions
    {
        /// <summary>
        /// Returns a <see cref="JobAwaiter"/> to allow asynchronously awaiting a <see cref="Job"/> (this enables the <see langword="await"/> action on a <see cref="Job"/>);
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public static JobAwaiter GetAwaiter(this Job job)
        {
            return new JobAwaiter(job);
        }
        /// <summary>
        /// Indicates if the <paramref name="job"/> is finished.
        /// </summary>
        /// <param name="job"></param>
        /// <returns><see langword="true"/> if the job state is <see cref="JobState.Completed"/>, 
        /// <see cref="JobState.Failed"/>, or <see cref="JobState.Stopped"/>; otherwise <see langword="false"/>.</returns>
        /// <exception cref="PSArgumentNullException"><paramref name="job"/> is null.</exception>
        public static bool IsFinished(this Job job)
            => job is null ? throw new PSArgumentNullException(nameof(Job))
                 : job.JobStateInfo.State == JobState.Completed
                || job.JobStateInfo.State == JobState.Failed
                || job.JobStateInfo.State == JobState.Stopped;
    }
}
