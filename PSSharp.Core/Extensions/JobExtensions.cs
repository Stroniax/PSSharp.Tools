using PSSharp;
using System;
using System.ComponentModel;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace PSSharp.Extensions
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
        public static JobAwaiter GetAwaiter(this Job job) => new JobAwaiter(job);
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
        /// <summary>
        /// Indicates whether any job stream has <see cref="PSDataCollection{T}.Count"/> greater than 0.
        /// </summary>
        /// <param name="job">The job observed.</param>
        /// <returns></returns>
        public static bool AnyStreamHasData(this Job job)
            => job is null ? throw new ArgumentNullException(nameof(job)) 
             : job.Output.Count > 0
            || job.Error.Count > 0
            || job.Warning.Count > 0
            || job.Verbose.Count > 0
            || job.Debug.Count > 0
            || job.Information.Count > 0
            || job.Progress.Count > 0;

        public static async Task SuspendJobAsync(this Job2 job, bool force = false, string? reason = null, CancellationToken waitCancellation = default)
        {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler<AsyncCompletedEventArgs>? handler = null;
            handler = (sender, args) =>
            {
                job.SuspendJobCompleted -= handler;
                if (args.Error != null)
                {
                    tcs.SetException(args.Error);
                }
                else if (args.Cancelled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetResult(true);
                }
            };

            job.SuspendJobCompleted += handler;
            job.SuspendJobAsync(force, reason);

            using (waitCancellation.Register(() => tcs.SetCanceled()))
            {
                await tcs.Task;
            }
        }
        public static async Task ResumeJobAsync(this Job2 job, CancellationToken waitCancellation = default)
        {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler<AsyncCompletedEventArgs>? handler = null;
            handler = (sender, args) =>
            {
                job.ResumeJobCompleted -= handler;
                if (args.Error != null)
                {
                    tcs.SetException(args.Error);
                }
                else if (args.Cancelled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetResult(true);
                }
            };

            job.ResumeJobCompleted += handler;
            job.ResumeJobAsync();

            using (waitCancellation.Register(() => tcs.SetCanceled()))
            {
                await tcs.Task;
            }
        }
        public static async Task StartJobAsync(this Job2 job, CancellationToken waitCancellation = default)
        {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler<AsyncCompletedEventArgs>? handler = null;
            handler = (sender, args) =>
            {
                job.StartJobCompleted -= handler;
                if (args.Error != null)
                {
                    tcs.SetException(args.Error);
                }
                else if (args.Cancelled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetResult(true);
                }
            };

            job.StartJobCompleted += handler;
            job.StartJobAsync();

            using (waitCancellation.Register(() => tcs.SetCanceled()))
            {
                await tcs.Task;
            }
        }
        public static async Task UnblockJobAsync(this Job2 job, CancellationToken waitCancellation = default)
        {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler<AsyncCompletedEventArgs>? handler = null;
            handler = (sender, args) =>
            {
                job.UnblockJobCompleted -= handler;
                if (args.Error != null)
                {
                    tcs.SetException(args.Error);
                }
                else if (args.Cancelled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetResult(true);
                }
            };

            job.UnblockJobCompleted += handler;
            job.UnblockJobAsync();

            using (waitCancellation.Register(() => tcs.SetCanceled()))
            {
                await tcs.Task;
            }
        }
        public static async Task StopJobAsync(this Job2 job, bool force = false, string? reason = null, CancellationToken waitCancellation = default)
        {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler<AsyncCompletedEventArgs>? handler = null;
            handler = (sender, args) =>
            {
                job.StopJobCompleted -= handler;
                if (args.Error != null)
                {
                    tcs.SetException(args.Error);
                }
                else if (args.Cancelled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetResult(true);
                }
            };

            job.StopJobCompleted += handler;
            job.StopJobAsync(force, reason);

            using (waitCancellation.Register(() => tcs.SetCanceled()))
            {
                await tcs.Task;
            }
        }
    }
}
