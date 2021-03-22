using System.Management.Automation;

namespace Stroniax.PowerShell
{
    internal class JobOutput
    {
        public JobOutput(PowerShellStreamType stream, object contents)
        {
            Stream = stream;
            Value = contents;
        }
        public PowerShellStreamType Stream { get; }
        public object Value { get; }
    }
}
