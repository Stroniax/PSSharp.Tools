using System.Management.Automation;

namespace PSSharp
{
    [Cmdlet(VerbsLifecycle.Register, "PSPrintJobServer")]
    public class RegisterPSPrintJobServerCommand : PSCmdlet
    {
        public string ComputerName { get; set; }
        public string PrinterName { get; set; }

    }
}
