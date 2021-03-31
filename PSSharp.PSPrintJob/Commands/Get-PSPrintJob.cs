using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;

namespace PSSharp.Commands
{
    [Cmdlet(VerbsCommon.Get, "PSPrintJob")]
    [OutputType(typeof(PSPrintJob))]
    public class GetPSPrintJobCommand : PSCmdlet
    {
        public const string PrinterSet = "PrinterSet";
        public const string DefaultSet = "DefaultSet";

        /// <summary>
        /// <para type='description'>The name of the printer(s) from which print jobs should be returned.</para>
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = DefaultSet)]
        [ValidateNotNullOrEmpty]
        [SupportsWildcards]
        public string[]? PrinterName { get; set; }
        /// <summary>
        /// <para type='description'>The name of the computer to query print jobs from.</para>
        /// </summary>
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = DefaultSet)]
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = PrinterSet)]
        [ValidateNotNullOrEmpty]
        public string? ComputerName { get; set; }
        /// <summary>
        /// <para type='description'>The name of the document or job being printed.</para>
        /// </summary>
        [Parameter(ParameterSetName = PrinterSet)]
        [Parameter(ParameterSetName = DefaultSet)]
        [Alias("JobName","DocumentName")]
        [ValidateNotNullOrEmpty]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0, ParameterSetName = PrinterSet)]
        [PSTypeName("Microsoft.Management.Infrastructure.CimInstance#ROOT/StandardCimv2/MSFT_Printer")]
        public CimInstance Printer { get; set; } = null!;

        [Parameter(ParameterSetName = DefaultSet)]
        public CimSession[]? Session { get; set; }

        private bool _isCimSessionOwner;
        private CimOperationOptions _options = null!;
        private CancellationTokenSource _cts = null!;
        protected override void BeginProcessing()
        {
            if (Session is null)
            {
                Session = new CimSession[]
                {
                    CimSession.Create(null)
                };
                _isCimSessionOwner = true;
            }
            _cts = new CancellationTokenSource();
            _options = new CimOperationOptions()
            {
                CancellationToken = _cts.Token,
            };
            if (!string.IsNullOrEmpty(ComputerName))
            {
                _options.SetCustomOption("ComputerName", ComputerName, true);
            }
        }
        protected override void ProcessRecord()
        {
            var jobNames = Name?
                .Select(i => WildcardPattern.Get(i, WildcardOptions.IgnoreCase))
                ?? new[] { WildcardPattern.Get("*", WildcardOptions.IgnoreCase) };
            var printerNames = PrinterName?
                .Select(i => CodeGeneration.EscapeSingleQuotedStringContent(WildcardPattern.Get(i, WildcardOptions.IgnoreCase).ToWql()))
                ?? new string[] { "%" };
            if (ParameterSetName == DefaultSet)
            {
                foreach (var sn in Session!)
                {
                    foreach (var printerName in printerNames) {
                        try
                        {
                            var printers = sn.QueryInstances("root/standardcimv2", "WQL", $"select * from msft_printer where Name like '{printerName}'", _options);
                            foreach (var printer in printers)
                            {
                                var parameters = new CimMethodParametersCollection()
                            {
                                CimMethodParameter.Create("PrinterObject", printer, CimType.Instance, CimFlags.In)
                            };
                                var methodResult = sn.InvokeMethod("root/standardcimv2", "msft_printjob", "GetByObject", parameters, _options);
                                var jobs = (IEnumerable<CimInstance>)methodResult.OutParameters["cmdletOutput"].Value;
                                WriteObject(methodResult.OutParameters);
                                Console.WriteLine("checking jobs");
                                jobs.GetEnumerator();
                                Console.WriteLine("Got enumerator");
                                foreach (var job in jobs)
                                {
                                    Console.WriteLine("creating job");
                                    var psjob = new PSPrintJob(job);
                                    Console.WriteLine("job created");
                                    if (jobNames.Any(i => i.IsMatch(psjob.Name)))
                                    {
                                        WriteObject(psjob);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            WriteError(new ErrorRecord(e, "not good", ErrorCategory.NotSpecified, null));
                        }
                    }
                }
            }
            else if (ParameterSetName == PrinterSet)
            {
                
            }
        }
        protected override void EndProcessing()
        {
            _options.Dispose();
            _cts.Dispose();
            if (_isCimSessionOwner)
            {
                Session![0].Dispose();
            }
        }
        protected override void StopProcessing()
        {
            _cts.Cancel();
            // cleanup
            EndProcessing();
        }
    }
}
