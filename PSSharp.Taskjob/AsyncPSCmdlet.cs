using PSSharp.Extensions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace PSSharp.Commands
{
    /// <summary>
    /// A PowerShell cmdlet base for a cmdlet that performs an asynchronous operation. 
    /// The cmdlet may be invoked synchronously but also supports the <see cref="AsJob"/>
    /// parameter, allowing the cmdlet to be invoked as a job.
    /// <para>Derived classes may also override <see cref="OutputMode"/> and 
    /// <see cref="TaskJobMode"/> to determine the job behavior of the cmdlet.</para>
    /// </summary>
    public abstract class AsyncPSCmdlet : PSCmdlet, IObserver<JobOutput>
    {
        #region Nested Types
        /// <summary>
        /// Determines how jobs are output from an <see cref="AsyncPSCmdlet"/> when the
        /// <see cref="AsJob"/> parameter is present.
        /// </summary>
        public enum JobOutputMode
        {
            /// <summary>
            /// A single job will be written to the pipeline during <see cref="Cmdlet.EndProcessing"/>,
            /// wrapping each child job from <see cref="ProcessRecordAsJobAsync(TaskJobBuilder)"/>.
            /// </summary>
            Aggregate,
            /// <summary>
            /// Every time <see cref="ProcessRecordAsJobAsync(TaskJobBuilder)"/> is called, a new
            /// job will be created and written to the pipeline.
            /// </summary>
            Individual
        }
        #endregion
        #region Parameters
        /// <summary>
        /// <para type='description'>Indicates that this cmdlet runs the command as a background job on a remote computer. Use this parameter to run commands that take an extensive time to finish.</para>
        /// <para type='description'>When you use the AsJob parameter, the command returns an object that represents the job, and then displays the command prompt.You can continue to work in the session while the job finishes.To manage the job, use the *-Job cmdlets. To get the job results, use the Receive-Job cmdlet.</para>
        /// <para type='description'>The AsJob parameter resembles using the Invoke-Command cmdlet to run a Start-Job cmdlet remotely.However, with AsJob, the job is created on the local computer, even though the job runs on a remote computer.The results of the remote job are automatically returned to the local computer.</para>
        /// <para type='description'>For more information about PowerShell background jobs, see about_Jobs and about_Remote_Jobs.</para>
        /// </summary>
        [Parameter]
        [NoCompletion]
        public SwitchParameter AsJob { get; set; }
        #endregion
        #region Protected Members
        /// <summary>
        /// Determines how the <see cref="TaskJob"/> will be output from this cmdlet during excecution. 
        /// <para>Note that this setting only applies when <see cref="OutputMode"/> is <see cref="JobOutputMode.Aggregate"/>
        /// or the cmdlet is invoked synchronously.</para>
        /// </summary>
        protected virtual TaskJobMode TaskJobMode { get; }
        /// <summary>
        /// Determines how jobs are output from this cmdlet when the
        /// <see cref="AsJob"/> parameter is present.
        /// </summary>
        protected virtual JobOutputMode OutputMode { get => JobOutputMode.Aggregate; }
        /// <summary>
        /// The name of the job(s) created by this cmdlet. When <see langword="null"/>, created
        /// jobs will be automatically named.
        /// </summary>
        protected virtual string? TaskJobName { get; }
        /// <summary>
        /// The command being executed by the job(s), such as the script called to invoke this cmdlet.
        /// <para>By default, this property returns <see cref="InvocationInfo.Line"/>.</para>
        /// </summary>
        protected virtual string? TaskJobCommand { get => MyInvocation.Line; }
        /// <summary>
        /// The location at which the job(s) of this cmdlet are being executed, such as
        /// the device the jobs are operating on or the resource the jobs are accessing.
        /// When <see langword="null"/>, <see cref="TaskJob.Location"/> of the output job(s)
        /// will return <see cref="Environment.MachineName"/>.
        /// </summary>
        protected virtual string? TaskJobLocation { get; }
        /// <summary>
        /// Asynchronously runs an operation. The operation may be invoked as a job if the 
        /// <see cref="AsJob"/> parameter is present; otherwise, the operation will be run
        /// synchronously.
        /// <para>To change the behavior of how many jobs will be output by this cmdlet if
        /// the <see cref="AsJob"/> parameter is present, override <see cref="OutputMode"/>.</para>
        /// <para>To change the behavior of whether tasks are run sequentially or in parallel,
        /// override <see cref="TaskJobMode"/>.</para>
        /// </summary>
        /// <param name="jobReference">A reference to the job or asynchronously writing to 
        /// the cmdlet's streams, depending on whether or not the operation is invoked
        /// as a job. 
        /// <para>To prevent referencing a parameter value after the next pipeline input
        /// has been processed by the job, reference all parameters through the 
        /// <see cref="TaskJobBuilder.PSBoundParameters"/> property.</para>
        /// <para>To prevent an exception or other unexpected behavior due to the
        /// behavior of the Write methods being invoked within an asynchronous method,
        /// use <see cref="TaskJobBuilder"/>'s Write methods. If the cmdlet is invoked
        /// synchronously, each value will be written through the cmdlet; otherwise,
        /// the value will be written to the associated job's result.</para>
        /// <para><see cref="TaskJobBuilder"/> also exposes a <see cref="CancellationToken"/>
        /// that can be used to stop the <see cref="TaskJob"/> if Stop-Job is called or the
        /// cmdlet's invocation is interrupted while running synchronously.</para>
        /// </param>
        /// <returns>A task representing the work to be completed.</returns>
        protected virtual Task ProcessRecordAsJobAsync(TaskJobBuilder jobReference)
            => Task.CompletedTask;
        /// <summary>
        /// Asynchronously executed once at the beginning of the invocation of the cmdlet.
        /// </summary>
        /// <param name="cancellationToken">Stops <see cref="BeginProcessingAsync"/>.</param>
        protected virtual Task BeginProcessingAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
        /// <summary>
        /// Asynchronously executed once at the end of the invocation of the cmdlet.
        /// </summary>
        /// <param name="cancellationToken">Stops <see cref="EndProcessingAsync"/>.</param>
        protected virtual Task EndProcessingAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <summary>
        /// Writes an object to the cmdlet's output stream. Note that this will not write data 
        /// to a job when <see cref="AsJob"/> is present. To write data to a job's result 
        /// streams, use the provided <see cref="TaskJobBuilder"/>.
        /// <para>If the method is called after the cmdlet has completed execution, the
        /// data will be ignored.</para>
        /// </summary>
        /// <param name="obj">The object to be written.</param>
        new public void WriteObject(object obj) => WriteObject(obj, false);
        /// <summary>
        /// Writes an object to the cmdlet's output stream. Note that this will not write data 
        /// to a job when <see cref="AsJob"/> is present. To write data to a job's result 
        /// streams, use the provided <see cref="TaskJobBuilder"/>.
        /// <para>If the method is called after the cmdlet has completed execution, the
        /// data will be ignored.</para>
        /// </summary>
        /// <param name="obj">The object to be written.</param>
        /// <param name="enumerateCollection">If <see langword="true"/> and <paramref name="obj"/> is <see cref="IEnumerable"/>,
        /// each item in <paramref name="obj"/> will be written individually.</param>
        new public void WriteObject(object obj, bool enumerateCollection)
        {
            _jobOutputs ??= new ConcurrentQueue<JobOutput>();
            if (enumerateCollection && obj is IEnumerable enumerable && !(obj is string))
            {
                foreach (var item in enumerable)
                {
                    _jobOutputs.Enqueue(new JobOutput(PowerShellStreamType.Output, item));
                }
            }
            else
            {
                _jobOutputs.Enqueue(new JobOutput(PowerShellStreamType.Output, obj));
            }
        }
        /// <summary>
        /// Writes an error to the cmdlet's error stream. Note that this will not write data 
        /// to a job when <see cref="AsJob"/> is present. To write data to a job's result 
        /// streams, use the provided <see cref="TaskJobBuilder"/>.
        /// <para>If the method is called after the cmdlet has completed execution, the
        /// data will be ignored.</para>
        /// </summary>
        /// <param name="errorRecord">The error to be written.</param>
        new public void WriteError(ErrorRecord errorRecord)
        {
            _jobOutputs ??= new ConcurrentQueue<JobOutput>();
            _jobOutputs.Enqueue(new JobOutput(PowerShellStreamType.Error, errorRecord));
        }
        /// <summary>
        /// Writes a warning to the cmdlet's warning stream. Note that this will not write data 
        /// to a job when <see cref="AsJob"/> is present. To write data to a job's result 
        /// streams, use the provided <see cref="TaskJobBuilder"/>.
        /// <para>If the method is called after the cmdlet has completed execution, the
        /// data will be ignored.</para>
        /// </summary>
        /// <param name="text">The warning message.</param>
        new public void WriteWarning(string text)
        {
            _jobOutputs ??= new ConcurrentQueue<JobOutput>();
            _jobOutputs.Enqueue(new JobOutput(PowerShellStreamType.Warning, new WarningRecord(text)));
        }
        /// <summary>
        /// Writes progress to the cmdlet's progress stream. Note that this will not write data
        /// to a job when <see cref="AsJob"/> is present. To write data to a job's result 
        /// streams, use the provided <see cref="TaskJobBuilder"/>.
        /// <para>If the method is called after the cmdlet has completed execution, the
        /// data will be ignored.</para>
        /// </summary>
        /// <param name="progressRecord">The progress to be written.</param>
        new public void WriteProgress(ProgressRecord progressRecord)
        {
            _jobOutputs ??= new ConcurrentQueue<JobOutput>();
            _jobOutputs.Enqueue(new JobOutput(PowerShellStreamType.Progress, progressRecord));
        }
        /// <summary>
        /// Writes information to the cmdlet's information stream. Note that this will not write data
        /// to a job when <see cref="AsJob"/> is present. To write data to a job's result 
        /// streams, use the provided <see cref="TaskJobBuilder"/>.
        /// <para>If the method is called after the cmdlet has completed execution, the
        /// data will be ignored.</para>
        /// </summary>
        /// <param name="informationRecord">The information to be written.</param>
        new public void WriteInformation(InformationRecord informationRecord)
        {
            _jobOutputs ??= new ConcurrentQueue<JobOutput>();
            _jobOutputs.Enqueue(new JobOutput(PowerShellStreamType.Information, informationRecord));
        }
        /// <summary>
        /// Writes information to the cmdlet's information stream. Note that this will not write data
        /// to a job when <see cref="AsJob"/> is present. To write data to a job's result 
        /// streams, use the provided <see cref="TaskJobBuilder"/>.
        /// <para>If the method is called after the cmdlet has completed execution, the
        /// data will be ignored.</para>
        /// </summary>
        /// <param name="messageData">Data for the information message.</param>
        /// <param name="tags">Tags for the <see cref="InformationRecord"/>.</param>
        new public void WriteInformation(object messageData, string[] tags)
        {
            _jobOutputs ??= new ConcurrentQueue<JobOutput>();
            var value = new InformationRecord(messageData, null);
            foreach (var arg in tags)
            {
                value.Tags.Add(arg);
            }
            _jobOutputs.Enqueue(new JobOutput(PowerShellStreamType.Information, value));
        }
        /// <summary>
        /// Writes a debug message to the cmdlet's debug stream. Note that this will not write data 
        /// to a job when <see cref="AsJob"/> is present. To write data to a job's result 
        /// streams, use the provided <see cref="TaskJobBuilder"/>.
        /// <para>If the method is called after the cmdlet has completed execution, the
        /// data will be ignored.</para>
        /// </summary>
        /// <param name="text">The debug message.</param>
        new public void WriteDebug(string text)
        {
            _jobOutputs ??= new ConcurrentQueue<JobOutput>();
            _jobOutputs.Enqueue(new JobOutput(PowerShellStreamType.Debug, new DebugRecord(text)));
        }
        /// <summary>
        /// Writes a verbose message to the cmdlet's verbose stream. Note that this will not write data 
        /// to a job when <see cref="AsJob"/> is present. To write data to a job's result 
        /// streams, use the provided <see cref="TaskJobBuilder"/>.
        /// <para>If the method is called after the cmdlet has completed execution, the
        /// data will be ignored.</para>
        /// </summary>
        /// <param name="text">The debug message.</param>
        new public void WriteVerbose(string text)
        {
            _jobOutputs ??= new ConcurrentQueue<JobOutput>();
            _jobOutputs.Enqueue(new JobOutput(PowerShellStreamType.Verbose, new VerboseRecord(text)));
        }
        #endregion
        #region Private Members
        private ConcurrentQueue<JobOutput>? _jobOutputs;
        private TaskJob? _aggregateJob;
        private List<TaskJob>? _synchronousJobs;
        private List<IDisposable>? _subscribers;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        /// <summary>
        /// Writes the output from jobs until <see cref="_jobOutputs"/> is empty.
        /// </summary>
        private void WriteJobContents()
        {
            if (_jobOutputs is null) return;
            while (_jobOutputs.TryDequeue(out var content))
            {
                switch (content.Stream)
                {
                    case PowerShellStreamType.Output:
                        base.WriteObject(content.Value);
                        break;
                    case PowerShellStreamType.Debug:
                        base.WriteDebug(((DebugRecord)content.Value).Message);
                        break;
                    case PowerShellStreamType.Error:
                        base.WriteError((ErrorRecord)content.Value);
                        break;
                    case PowerShellStreamType.Information:
                        base.WriteInformation((InformationRecord)content.Value);
                        break;
                    case PowerShellStreamType.Progress:
                        base.WriteProgress((ProgressRecord)content.Value);
                        break;
                    case PowerShellStreamType.Verbose:
                        base.WriteVerbose(((VerboseRecord)content.Value).Message);
                        break;
                    case PowerShellStreamType.Warning:
                        base.WriteWarning(((WarningRecord)content.Value).Message);
                        break;
                }
            }
        }
        #endregion
        #region Job Observation
        void IObserver<JobOutput>.OnCompleted()
        {
            // do nothing
        }
        void IObserver<JobOutput>.OnError(Exception error)
        {
            // do nothing
        }
        void IObserver<JobOutput>.OnNext(JobOutput value)
        {
            _jobOutputs ??= new ConcurrentQueue<JobOutput>();
            _jobOutputs.Enqueue(value);
        }
        #endregion
        #region PSCmdlet Overrides
        /// <inheritdoc/>
        protected sealed override void BeginProcessing()
        {
            base.BeginProcessing();
            if (OutputMode == JobOutputMode.Aggregate)
            {
                _aggregateJob = new TaskJob(TaskJobName, TaskJobCommand, TaskJobLocation, TaskJobMode);
                if (!AsJob)
                {
                    _subscribers ??= new List<IDisposable>();
                    var subscriber = (_aggregateJob as IObservable<JobOutput>)?.Subscribe(this);
                    if (subscriber != null)
                    {
                        _subscribers.Add(subscriber);
                    }
                }
            }

            var task = BeginProcessingAsync(_cts.Token);
            do 
            {
                WriteJobContents();
                // cancellation called from StopProcessing()
            }
            while (!task.IsCompleted);
            if (!task.IsCanceled && task.IsFaulted)
            {
                var ex = task.Exception.Flatten();
                if (ex.InnerExceptions.Count == 1)
                {
                    ExceptionDispatchInfo.Capture(task.Exception.InnerExceptions[0]).Throw();
                }
                else
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
            }
        }
        /// <inheritdoc/>
        protected sealed override void ProcessRecord()
        {
            base.ProcessRecord();
            TaskJob job;
            if (OutputMode == JobOutputMode.Individual)
            {
                job = new TaskJob(TaskJobName, TaskJobCommand, TaskJobLocation, TaskJobMode);
                if (!AsJob)
                {
                    _subscribers ??= new List<IDisposable>();
                    var subscriber = (job as IObservable<JobOutput>)?.Subscribe(this);
                    if (subscriber != null)
                    {
                        _subscribers.Add(subscriber);
                    }
                }
            }
            else {
                job = _aggregateJob!;
            }
            var child = new TaskChildJob(ProcessRecordAsJobAsync, MyInvocation.BoundParameters, TaskJobCommand, TaskJobLocation);
            job.AddChildJob(child);
            if (OutputMode == JobOutputMode.Individual)
            {
                job.Seal();
            }
            if (AsJob)
            {
                if (OutputMode == JobOutputMode.Individual)
                {
                    JobRepository.Add(job);
                    base.WriteObject(job);
                }
                return;
            }
            else
            {
                if (TaskJobMode == TaskJobMode.Parallel)
                {
                    if (OutputMode != JobOutputMode.Aggregate)
                    {
                        _synchronousJobs ??= new List<TaskJob>();
                        _synchronousJobs.Add(job);
                    }
                    return; // write in EndProcessing
                }
                else
                {
                    do
                    {
                        WriteJobContents();
                        if (Stopping)
                        {
                            base.WriteDebug("Stopping jobs.");
                            job.StopJob();
                        }
                    }
                    while (!child.IsCompleted);
                }
                // wait for the job to complete, and write any output as it is returned
            }
        }
        /// <inheritdoc/>
        protected sealed override void EndProcessing()
        {
            if (OutputMode == JobOutputMode.Aggregate)
            {
                _aggregateJob!.Seal();
            }
            if (AsJob)
            {
                if (OutputMode == JobOutputMode.Aggregate)
                {
                    JobRepository.Add(_aggregateJob);
                    base.WriteObject(_aggregateJob);
                }
                return;
            }
            if (_synchronousJobs != null)
            {
                do
                {
                    WriteJobContents();
                    if (Stopping)
                    {
                        base.WriteDebug("Stopping jobs.");
                        _synchronousJobs.ForEach(i => i.StopJob());
                    }
                    foreach (var job in _synchronousJobs.Where(j => j.IsFinished()).ToList())
                    {
                        _synchronousJobs.Remove(job);
                        job.Dispose();
                    }
                }
                while (_synchronousJobs.Count > 0) ;
            }
            else if (_aggregateJob != null)
            {
                do
                {
                    WriteJobContents();
                    if (Stopping)
                    {
                        base.WriteDebug("Stopping jobs.");
                        _aggregateJob.StopJob();
                    }
                }
                while (!_aggregateJob.IsFinished());
                _aggregateJob.Dispose();
            }
            _subscribers?.ForEach(i => i.Dispose());

            var task = EndProcessingAsync(_cts.Token);
            do
            {
                WriteJobContents();
                // cancellation called from StopProcessing()
            }
            while (!task.IsCompleted);
            if (!task.IsCanceled && task.IsFaulted)
            {
                var ex = task.Exception.Flatten();
                if (ex.InnerExceptions.Count == 1)
                {
                    ExceptionDispatchInfo.Capture(task.Exception.InnerExceptions[0]).Throw();
                }
                else
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
            }

            // ensure that all data has been written
            WriteJobContents();
        }
        /// <inheritdoc/>
        protected override void StopProcessing()
        {
            base.StopProcessing();
            _cts.Cancel();
            _subscribers?.ForEach(i => i.Dispose());
        }
        #endregion
    }
}
