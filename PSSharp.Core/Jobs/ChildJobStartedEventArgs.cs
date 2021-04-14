using System;
using System.Management.Automation;

namespace PSSharp
{
    public class ChildJobStartedEventArgs : EventArgs
    {
        public ChildJobStartedEventArgs(Job job)
        {
            Job = job;
        }

        public Job Job { get; }
    }
}
