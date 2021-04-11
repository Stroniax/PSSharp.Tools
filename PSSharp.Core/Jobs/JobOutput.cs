using System;
using System.Management.Automation;

namespace PSSharp
{
    internal class JobOutput
    {
        public PowerShellStreamType Stream { get; }
        public PSObject? Output { get; }
        public ErrorRecord? Error { get; }
        public VerboseRecord? Verbose { get; }
        public DebugRecord? Debug { get; }
        public InformationRecord? Information { get; }
        public ProgressRecord? Progress { get; }
        public WarningRecord? Warning { get; }

        public JobOutput(PSObject? output)
        {
            Stream = PowerShellStreamType.Output;
            Output = output;
        }
        public JobOutput(ErrorRecord error)
        {
            Stream = PowerShellStreamType.Error;
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }
        public JobOutput(WarningRecord warning)
        {
            Stream = PowerShellStreamType.Warning;
            Warning = warning ?? throw new ArgumentNullException(nameof(warning));
        }
        public JobOutput(VerboseRecord verbose)
        {
            Stream = PowerShellStreamType.Verbose;
            Verbose = verbose ?? throw new ArgumentNullException(nameof(verbose));
        }
        public JobOutput(DebugRecord debug)
        {
            Stream = PowerShellStreamType.Debug;
            Debug = debug ?? throw new ArgumentNullException(nameof(debug));
        }
        public JobOutput(ProgressRecord progress)
        {
            Stream = PowerShellStreamType.Progress;
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }
        public JobOutput(InformationRecord information)
        {
            Stream = PowerShellStreamType.Information;
            Information = information ?? throw new ArgumentNullException(nameof(information));
        }
    }
}
