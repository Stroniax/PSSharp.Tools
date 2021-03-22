using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace Stroniax.PowerShell
{
    /// <summary>
    /// An asynchronous action to be invoked by a task job that grants access to the job's streams.
    /// </summary>
    /// <param name="jobReference"></param>
    /// <returns></returns>
    public delegate Task TaskJobAction(TaskJobBuilder jobReference);
    /// <summary>
    /// A reference point to grant access within an asynchronous method to the streams of a <see cref="TaskChildJob"/>.
    /// </summary>
    public struct TaskJobBuilder
    {
        private TaskChildJob? _job;
        internal TaskJobBuilder(TaskChildJob job, Dictionary<string, object> psBoundParameters)
        {
            _job = job;
            PSBoundParameters = psBoundParameters;
        }
        /// <summary>
        /// Parameters bound by the invocation of the cmdlet this job is being started from.
        /// </summary>
        public Dictionary<string, object> PSBoundParameters { get; }
        /// <summary>
        /// Used to stop the job.
        /// </summary>
        public CancellationToken CancellationToken { get => _job?.CancellationToken ?? throw new InvalidOperationException($"{nameof(TaskJobBuilder)} can only be used within a {nameof(TaskJob)}."); }
        /// <summary>
        /// Writes <paramref name="obj"/> to the <see cref="Job.Output"/> stream.
        /// </summary>
        /// <param name="obj">The object to be written to the <see cref="Job.Output"/> stream.</param>
        /// <param name="enumerateCollection"><see langword="false"/> to write the collection as a single object;
        /// <see langword="true"/> to write each item in <paramref name="obj"/> separately if <paramref name="obj"/>
        /// is <see cref="IEnumerable"/>.</param>
        public void WriteObject(object obj, bool enumerateCollection = true)
        {
            if (_job is null) throw new InvalidOperationException($"{nameof(TaskJobBuilder)} can only be used within a {nameof(TaskJob)}.");
            if (enumerateCollection && obj is IEnumerable enumerable && !(obj is string))
            {
                foreach (var item in enumerable)
                {
                    _job.Output.Add(PSObject.AsPSObject(item));
                }
            }
            else
            {
                _job.Output.Add(PSObject.AsPSObject(obj));
            }
        }
        /// <summary>
        /// Writes <paramref name="error"/> to the <see cref="Job.Error"/> stream.
        /// </summary>
        /// <param name="error"></param>
        public void WriteError(ErrorRecord error)
        {
            if (_job is null) throw new InvalidOperationException($"{nameof(TaskJobBuilder)} can only be used within a {nameof(TaskJob)}.");
            _job.Error.Add(error);
        }
        /// <summary>
        /// Writes <paramref name="progress"/> to the <see cref="Job.Progress"/> stream.
        /// </summary>
        /// <param name="progress"></param>
        public void WriteProgress(ProgressRecord progress)
        {
            if (_job is null) throw new InvalidOperationException($"{nameof(TaskJobBuilder)} can only be used within a {nameof(TaskJob)}.");
            _job.Progress.Add(progress);
        }
        /// <summary>
        /// Writes <paramref name="debug"/> to the <see cref="Job.Debug"/> stream.
        /// </summary>
        /// <param name="debug">The message to be written to the <see cref="Job.Debug"/> stream.</param>
        /// <param name="args">Arguments used to format <paramref name="debug"/>, like <see cref="string.Format(string, object[])"/></param>
        public void WriteDebug(string debug, params object[] args)
        {
            if (_job is null) throw new InvalidOperationException($"{nameof(TaskJobBuilder)} can only be used within a {nameof(TaskJob)}.");
            _job.Debug.Add(new DebugRecord(string.Format(debug, args)));
        }
        /// <summary>
        /// Writes <paramref name="verbose"/> to the <see cref="Job.Verbose"/> stream.
        /// </summary>
        /// <param name="verbose">The message to be written to the <see cref="Job.Verbose"/> stream.</param>
        /// <param name="args">Arguments used to format <paramref name="verbose"/>, like <see cref="string.Format(string, object[])"/></param>
        public void WriteVerbose(string verbose, params object[] args)
        {
            if (_job is null) throw new InvalidOperationException($"{nameof(TaskJobBuilder)} can only be used within a {nameof(TaskJob)}.");
            _job.Verbose.Add(new VerboseRecord(string.Format(verbose, args)));
        }
        /// <summary>
        /// Writes <paramref name="warning"/> to the <see cref="Job.Warning"/> stream.
        /// </summary>
        /// <param name="warning">The message to be written to the <see cref="Job.Warning"/> stream.</param>
        /// <param name="args">Arguments used to format <paramref name="warning"/>, like <see cref="string.Format(string, object[])"/></param>
        public void WriteWarning(string warning, params object[] args)
        {
            if (_job is null) throw new InvalidOperationException($"{nameof(TaskJobBuilder)} can only be used within a {nameof(TaskJob)}.");
            _job.Warning.Add(new WarningRecord(string.Format(warning, args)));
        }
        /// <summary>
        /// Writes <paramref name="information"/> to the <see cref="Job.Information"/> stream.
        /// </summary>
        /// <param name="information">The message to be written to the <see cref="Job.Information"/> stream.</param>
        public void WriteInformation(InformationRecord information)
        {
            if (_job is null) throw new InvalidOperationException($"{nameof(TaskJobBuilder)} can only be used within a {nameof(TaskJob)}.");
            _job.Information.Add(information);
        }
    }
}