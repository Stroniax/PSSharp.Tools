using System;
using System.Management.Automation;

namespace PSSharp
{
    public class PSTerminalStateException : Exception
    {
        public PSInvocationState State { get; }
        public PSTerminalStateException(PSInvocationState state)
            : base()
        {
            State = state;
        }
        public PSTerminalStateException(PSInvocationState state, string message)
            : base(message)
        {
            State = state;
        }
        public PSTerminalStateException(PSInvocationState state, string message, Exception exception)
            : base(message, exception)
        {
            State = state;
        }
    }
}
