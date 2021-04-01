using System;
using System.Management.Automation;

namespace PSSharp.Commands
{
    /// <summary>
    /// <para type='synopsis'>Allows a type to be referenced with an alias.</para>
    /// <para type='description'>Adds a type accelerator (such as [psobject] for [System.Management.Automation.PSObject]),
    /// which is an alias for a full type name. The type accelerator may be the same as $Type.Name or may be different,
    /// such as [xml] references [System.Xml.XmlDocument].</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "TypeAccelerator")]
    public class AddTypeAcceleratorCommand : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; } = null!;

        [Parameter(Mandatory = true, Position = 1)]
        public Type Type { get; set; } = null!;
        protected override void ProcessRecord()
        {
            var typeAccelerators = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
            var addMethod = typeAccelerators.GetMethod("Add", new Type[] { typeof(string), typeof(Type) });
            addMethod.Invoke(null, new object[] { Name, Type });
        }
    }
}
