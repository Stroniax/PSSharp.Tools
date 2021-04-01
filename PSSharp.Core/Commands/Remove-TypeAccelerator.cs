using System;
using System.Management.Automation;

namespace PSSharp.Commands
{
    [Cmdlet(VerbsCommon.Remove, "TypeAccelerator")]
    public class RemoveTypeAcceleratorCommand : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; } = null!;

        protected override void ProcessRecord()
        {
            var typeAccelerators = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
            var removeMethod = typeAccelerators.GetMethod("Remove", new Type[] { typeof(string) });
            removeMethod.Invoke(null, new object[] { Name });
        }
    }
}
