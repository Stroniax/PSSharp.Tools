using System;
using System.Management.Automation;

namespace PSSharp
{
    internal class PSOutput<T>
    {
        public PowerShellStreamType Stream { get; }
        public T Output { get; } = default!;
        public ErrorRecord? Error { get; }
        public VerboseRecord? Verbose { get; }
        public DebugRecord? Debug { get; }
        public InformationRecord? Information { get; }
        public ProgressRecord? Progress { get; }
        public WarningRecord? Warning { get; }

        public PSOutput(T output)
        {
            Stream = PowerShellStreamType.Output;
            Output = output;
        }
        public PSOutput(ErrorRecord error)
        {
            Stream = PowerShellStreamType.Error;
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }
        public PSOutput(WarningRecord warning)
        {
            Stream = PowerShellStreamType.Warning;
            Warning = warning ?? throw new ArgumentNullException(nameof(warning));
        }
        public PSOutput(VerboseRecord verbose)
        {
            Stream = PowerShellStreamType.Verbose;
            Verbose = verbose ?? throw new ArgumentNullException(nameof(verbose));
        }
        public PSOutput(DebugRecord debug)
        {
            Stream = PowerShellStreamType.Debug;
            Debug = debug ?? throw new ArgumentNullException(nameof(debug));
        }
        public PSOutput(ProgressRecord progress)
        {
            Stream = PowerShellStreamType.Progress;
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }
        public PSOutput(InformationRecord information)
        {
            Stream = PowerShellStreamType.Information;
            Information = information ?? throw new ArgumentNullException(nameof(information));
        }
    }
}
