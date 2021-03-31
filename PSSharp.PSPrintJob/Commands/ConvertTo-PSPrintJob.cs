using Microsoft.Management.Infrastructure;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PSSharp.Commands
{
    [Cmdlet(VerbsData.ConvertTo, "PSPrintJob")]
    public class ConvertToPSPrintJobCommand : PSCmdlet
    {
        public CimInstance[] PrintJob { get; set; }
        protected override void ProcessRecord()
        {
            foreach (var job in PrintJob)
            {
                WriteObject(new PSPrintJob(job));
            }
        }
    }
}
