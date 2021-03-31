using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PSSharp
{
    public class PSPrintJobSourceAdapter : JobSourceAdapter
    {
        internal static ConcurrentBag<string> PrintJobServers { get; } = new ConcurrentBag<string>();
        internal static ConcurrentDictionary<Guid, PSPrintJob> Jobs { get; } = new ConcurrentDictionary<Guid, PSPrintJob>();
        public override Job2? GetJobByInstanceId(Guid instanceId, bool recurse)
        {
            if (Jobs.TryGetValue(instanceId, out var job))
            {
                return job;
            }
            else
            {
                return null;
            }
        }

        public override Job2 GetJobBySessionId(int id, bool recurse)
        {
            return GetJobs()
                .Where(j => j.Id == id)
                .SingleOrDefault();
        }

        public override IList<Job2> GetJobs()
        {
            var jobs = ScriptBlock.Create("Get-Printer -ComputerName $args[0] " +
                "| Get-PrintJob -ComputerName $args[0]" +
                "| ConvertTo-PSPrintJob")
                .Invoke(PrintJobServers.ToArray());

            return jobs
                .Select(i => (Job2)i.BaseObject)
                .ToList();
        }

        public override IList<Job2>? GetJobsByCommand(string command, bool recurse)
        {
            var wc = WildcardPattern.Get(command, WildcardOptions.IgnoreCase);
            return GetJobs()
                .Where(i => wc.IsMatch(i.Command))
                .ToList();
        }

        public override IList<Job2>? GetJobsByFilter(Dictionary<string, object> filter, bool recurse)
        {
            return null;
        }

        public override IList<Job2> GetJobsByName(string name, bool recurse)
        {
            var wc = WildcardPattern.Get(name, WildcardOptions.IgnoreCase);
            return GetJobs()
                .Where(j => wc.IsMatch(name))
                .ToList();
        }

        public override IList<Job2> GetJobsByState(JobState state, bool recurse)
        {
            throw new NotImplementedException();
        }

        public override Job2 NewJob(JobInvocationInfo specification)
        {
            throw new NotImplementedException();
        }

        public override void RemoveJob(Job2 job)
        {
            if (Jobs.TryRemove(job.InstanceId, out var printJob))
            {
                printJob.Dispose();
            }
        }
    }
}
