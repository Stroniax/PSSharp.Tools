using Microsoft.Management.Infrastructure;
using System;
using System.Management.Automation;

namespace PSSharp
{
    public class PSPrintJob : Job2
    {
        private CimInstance _source;
        public override bool HasMoreData => false;
        public override string Location => $"\\\\{ComputerName}\\{PrinterName}";
        public override string StatusMessage 
            => (string)_source.CimInstanceProperties["Status"].Value;
        public string ComputerName { get; }
        public string PrinterName { get; }
        public ushort PercentComplete
        {
            get
            {
                var printed = (uint)_source.CimInstanceProperties["PagesPrinted"].Value;
                var total = (uint)_source.CimInstanceProperties["TotalPages"].Value;
                var percent = Math.Min(
                    Math.Round((decimal)printed / total * 100), 100);

                return (byte)percent;
            }
        }

        internal PSPrintJob(CimInstance cimPrintJobInstance)
            :base(null, 
                 (string)cimPrintJobInstance.CimInstanceProperties["DocumentName"].Value
                 //Guid.Parse((string)cimPrintJobInstance.CimInstanceProperties["InstanceId"].Value)
                 )
        {
            PSJobTypeName = nameof(PSPrintJob);
            _source = cimPrintJobInstance;
            ComputerName = _source.CimSystemProperties.ServerName;
            PrinterName = (string)_source.CimInstanceProperties["PrinterName"].Value;
        }

        public override void ResumeJob()
        {
            ScriptBlock.Create("$args[0] | Resume-PrintJob -ComputerName $args[1]").Invoke(_source, ComputerName);
        }

        public override void ResumeJobAsync() => ResumeJob();

        public override void StartJob()
        {
            throw new NotImplementedException();
        }

        public override void StartJobAsync() => StartJob();

        public override void StopJob(bool force, string reason) => StopJob();

        public override void StopJob()
        {
            ScriptBlock.Create("$args[0] | Remove-PrintJob -ComputerName $args[1]").Invoke(_source, ComputerName);
        }

        public override void StopJobAsync() => StopJob();

        public override void StopJobAsync(bool force, string reason) => StopJob();

        public override void SuspendJob()
        {
            ScriptBlock.Create("$args[0] | Suspend-PrintJob -ComputerName $args[1]").Invoke(_source, ComputerName);
        }

        public override void SuspendJob(bool force, string reason) => SuspendJob();

        public override void SuspendJobAsync() => SuspendJob();

        public override void SuspendJobAsync(bool force, string reason) => SuspendJob();

        public override void UnblockJob()
        {
            throw new NotImplementedException();
        }

        public override void UnblockJobAsync()
        {
            throw new NotImplementedException();
        }
    }
}
