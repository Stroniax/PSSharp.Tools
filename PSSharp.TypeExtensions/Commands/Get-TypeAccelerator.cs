using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PSSharp.Commands
{
    [Cmdlet(VerbsCommon.Get, "TypeAccelerator")]
    public class GetTypeAcceleratorCommand : Cmdlet
    {
        [Parameter(Position = 0)]
        [TypeAcceleratorCompletion]
        [SupportsWildcards]
        public string? Name { get; set; }
        protected override void ProcessRecord()
        {
            var wildcard = Name is null ? null : WildcardPattern.Get(Name, WildcardOptions.IgnoreCase);
            var typeAccelerators = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
            var getProperty = typeAccelerators.GetProperty("Get");
            var values = (Dictionary<string, Type>)getProperty.GetValue(null);
            foreach (var value in values)
            {
                if (wildcard?.IsMatch(value.Key) ?? true)
                {
                    var output = new PSObject();
                    output.Properties.Add(new PSNoteProperty("Name", value.Key));
                    output.Properties.Add(new PSNoteProperty("Type", value.Value));

                    WriteObject(output);
                }
            }
        }
    }
}
