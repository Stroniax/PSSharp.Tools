using System;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace PSSharp.Commands
{
    /// <summary>
    /// <para type='synopsis'>Creates a PowerShell job wrapper around a <see cref="System.Threading.Tasks.Task"/></para>.
    /// <para type='description'>Creates a PowerShell job to represent one or more 
    /// <see cref="System.Threading.Tasks.Task"/> values. If <see cref="CancellationTokenSource"/> is provided,
    /// the tasks (and therefore jobs) are expected to be able to be stopped with the token.</para>
    /// <para type='description'>This cmdlet creates a familiar job interface from one or more tasks. If multiple tasks
    /// are provided to this cmdlet in the <see cref="Task"/> parameter, they will be wrapped as child jobs of a single
    /// job; otherwise (for example, if multiple tasks are piped into this cmdlet), each task will be wrapped into a
    /// distinct job.</para>
    /// <para type='description'>After a task has been converted to a job, the standard PowerShell job cmdlets work on
    /// the result. (Note that the task itself will be disposed when the job is removed.) If the task has a result, the
    /// value will be written to the output of the job; otherwise, the output will be null. Any exception that occurs in
    /// the task will be written to the error stream of the job.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// PS:\> [System.Threading.Tasks.Task]::Delay(5000) | <see cref="VerbsData.ConvertTo"/>-<see cref="Nouns.TaskJob"/>
    /// Id     Name            PSJobTypeName   State         HasMoreData     Location             Command
    /// --     ----            -------------   -----         -----------     --------             -------
    /// 1      Job1            TaskJob         Running       False           COMPUTER0
    /// PS:\> Get-Job | Wait-Job
    /// # Waits until the five seconds have elapsed and returns control to the host. 
    /// </code>
    /// This example demonstrates operations with a task that does not return a result.
    /// </example>
    /// <example>
    /// <code>
    /// # This example assumes you have obtained a [System.Threading.Tasks.Task[string]] object from a method already, and stored it in the variable $Task. The task will return the string 'Hello world'.
    /// PS:\> $Task | Convert-ToTaskJob | Receive-Job -Wait -AutoRemoveJob
    /// 'Hello world'
    /// # Note: PowerShell will wait until the task has completed when the -Wait parameter is used with the Receive-Job command.
    /// </code>
    /// This example demonstrates operations with a task that has will return a result.
    /// </example>
    [Cmdlet(VerbsData.ConvertTo, Nouns.TaskJob)]
    [OutputType(typeof(TaskJob))]
    [Alias("cttj")]
    public sealed class ConvertToTaskJobCommand : PSCmdlet
    {
        /// <summary>
        /// The name of the job created from the task(s) provided.
        /// </summary>
        [Parameter]
        public string? Name { get; set; }
        /// <summary>
        /// <para type='description'>The task(s) wrapped into the job that is output.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public Task[] Task { get; set; } = new Task[0];
        /// <summary>
        /// <para type='description'>The cancellation token source that can be used to stop the task(s) (and job[s]) created.</para>
        /// </summary>
        [Parameter]
        public CancellationTokenSource? CancellationTokenSource { get; set; }
        protected override void ProcessRecord()
        {
            if (Task.Length == 0 || Task.All(t => t is null))
            {
                WriteError(new ErrorRecord(
                    new ArgumentException("No task provided."),
                    "TaskRequired",
                    ErrorCategory.InvalidArgument,
                    Task)
                {
                    ErrorDetails = new ErrorDetails($"No task was provided. At least one non-null task must be provided to the {nameof(Task)} parameter.")
                });
            }
            else
            {
                try
                {
                    var job = TaskJob.StartJob(Name, null, null, Task, CancellationTokenSource);
                    job.DisposeCancellationTokenSourceOnDisposed = false;
                    JobRepository.Add(job);
                    WriteObject(job);
                }
                catch (Exception e)
                {
                    WriteError(new ErrorRecord(
                        e,
                        "InvalidTask",
                        ErrorCategory.NotSpecified,
                        Task));
                }
            }
        }
    }
}
