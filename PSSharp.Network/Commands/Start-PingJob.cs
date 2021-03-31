using System;
using System.Management.Automation;
using System.Net.NetworkInformation;

namespace PSSharp.Commands
{
    /// <summary>
    /// <para type='synopsis'>Initiates a job that waits for a successful <see cref="Ping"/> reply.</para>
    /// <para type='description'>Starts a <see cref="PingJob"/>, which pings a destination (host name or
    /// IP address) until the ping returns successfully, at which time the job concludes.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// PS:\> Start-PingJob localhost
    /// Id     Name            PSJobTypeName   State         HasMoreData     Location             Command
    /// --     ----            -------------   -----         -----------     --------             -------
    /// 1      Job1            PingJob         Completed     True            localhost            Start-PingJob localhost
    /// 
    /// PS:\> Get-Job | Receive-Job
    /// ComputerName Address
    /// ------------ -------
    /// localhost    ::1
    /// </code>
    /// A job started on the localhost will return immedately in a successful state.
    /// </example>
    /// <example>
    /// <code>
    /// PS:\> Start-PingJob -ComputerName Server02 | Wait-Job | Remove-Job</code>
    /// Piping a PingJob directly into wait-job will pause the script until the target returns a successful ping response.
    /// </example>
    /// <example>
    /// <code>
    /// PS:\> $Servers = 'Server01','Server02'
    /// PS:\> Stop-Computer -ComputerName $Servers -Force
    /// PS:\> Start-Sleep -Seconds 90 # wait for computers to shut down
    /// PS:\> Start-PingJob -ComputerName $Servers | Receive-Job -Wait -AutoRemoveJob | ForEach-Object { Invoke-Command -ComputerName $_.ComputerName -ScriptBlock $ServerStartupScript }
    /// </code>
    /// This example demonstrates starting ping jobs for multiple computers and using the results to wait for
    /// a computer to come online before running a script on the computers. Note that a ping reply may indicate
    /// success before the WSMan or similar remoting services have started on a computer.
    /// </example>
    [Cmdlet(VerbsLifecycle.Start, nameof(PingJob))]
    public class StartPingJobCommand : PSCmdlet
    {
        /// <summary>
        /// <para type='description'>The ComputerName or IP address that will be pinged.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("IPAddress","cn")]
        public string[] ComputerName { get; set; } = null!;

        /// <summary>
        /// <para type='description'>The name of the job.</para>
        /// </summary>
        [Parameter()]
        public string? Name { get; set; }

        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            foreach (var cn in ComputerName)
            {
                var job = new PingJob(MyInvocation.Line, Name, cn);
                JobRepository.Add(job);
                WriteObject(job);
            }
        }
    }
}
