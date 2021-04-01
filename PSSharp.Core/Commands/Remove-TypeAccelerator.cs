using System;
using System.Management.Automation;

namespace PSSharp.Commands
{
    /// <summary>
    /// <para type='synopsis'>Removes the alias used to reference a type.</para>
    /// <para type='description'>Removes a type accelerator (such as [psobject] for [System.Management.Automation.PSObject]),
    /// which is an alias for a full type name. The type accelerator may be the same as $Type.Name or may be different,
    /// such as [xml] references [System.Xml.XmlDocument].</para>
    /// <para type='description'>Note that many type accelerators are used in almost all PowerShell code,
    /// and removing a type accelerator may have disastrous consequences.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// PS:\> Remove-TypeAccelerator -Name 'psobject'
    /// PS:\> [psobject]
    /// Unable to find type [psobject].
    /// At line:1 char:1
    /// + [psobject]
    /// + ~~~~~~~~~~~
    ///     + CategoryInfo          : InvalidOperation: (psobject:TypeName) [], RuntimeException
    ///     + FullyQualifiedErrorId : TypeNotFound
    /// </code>
    /// This example demonstrates elimination of a type accelerator and the results. 
    /// </example>
    [Cmdlet(VerbsCommon.Remove, "TypeAccelerator", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class RemoveTypeAcceleratorCommand : Cmdlet
    {
        /// <summary>
        /// <para type='description'>The alias of the type.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [TypeAcceleratorCompletion]
        public string Name { get; set; } = null!;
        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
#warning Remove-TypeAccelerator not implemented
            throw new NotImplementedException("This cmdlet is not supported by the TypeAccelerators API.");
            var typeAccelerators = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
            var removeMethod = typeAccelerators.GetMethod("Remove", new Type[] { typeof(string) });
            if (ShouldProcess(Name, "remove type accelerator"))
            {
                removeMethod.Invoke(null, new object[] { Name });
            }
        }
    }
}
