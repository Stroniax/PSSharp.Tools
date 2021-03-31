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
        /// <summary>
        /// <para type='description'>The print jobs to convert.</para>
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [PSTypeName("Microsoft.Management.Infrastructure.CimInstance#ROOT/StandardCimv2/MSFT_Printer")]
        public CimInstance[] PrintJob { get; set; } = null!;
        protected override void ProcessRecord()
        {
            foreach (var job in PrintJob)
            {
                WriteObject(new PSPrintJob(job));
            }
        }
    }
}
