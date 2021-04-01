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
    /// <example>
    /// <code>
    /// PS:\> Add-TypeAccelerator -Name 'psshortname' -Type ([My.Very.Long.Type.Name])
    /// PS:\> [psshortname].FullName
    /// My.Very.Long.Type.Name
    /// </code>
    /// This example demonstrates construction and use of a type accelerator.
    /// </example>
    [Cmdlet(VerbsCommon.Add, "TypeAccelerator", SupportsShouldProcess = true)]
    public class AddTypeAcceleratorCommand : Cmdlet
    {
        /// <summary>
        /// <para type='description'>The alias of the type.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; } = null!;
        /// <summary>
        /// <para type='description'>The type referenced by the type accelerator.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        public Type Type { get; set; } = null!;
        /// <summary>
        /// Returns the created type accelerator to the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }
        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            var typeAccelerators = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
            var addMethod = typeAccelerators.GetMethod("Add", new Type[] { typeof(string), typeof(Type) });
            if (ShouldProcess($"{Name} => {Type.FullName}", "create type accelerator"))
            {
                addMethod.Invoke(null, new object[] { Name, Type });
                if (PassThru)
                {
                    var output = new PSObject();
                    output.Properties.Add(new PSNoteProperty("Name", Name));
                    output.Properties.Add(new PSNoteProperty("Type", Type));
                    output.TypeNames.Insert(0, "PSSharp.Pseudo.TypeAccelerator");
                    WriteObject(output);
                }
            }
        }
    }
}
