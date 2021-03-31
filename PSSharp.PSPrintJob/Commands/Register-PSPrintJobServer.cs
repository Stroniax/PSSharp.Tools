using System.Management.Automation;

namespace PSSharp.Commands
{
    [Cmdlet(VerbsLifecycle.Register, "PSPrintJobServer")]
    public class RegisterPSPrintJobServerCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string ComputerName { get; set; } = null!;
        protected override void ProcessRecord()
        {
            PSPrintJobSourceAdapter.PrintJobServers.Add(ComputerName);
        }
    }
}
