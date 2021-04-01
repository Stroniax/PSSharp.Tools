using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PSSharp.Commands
{
    /// <summary>
    /// <para type='synopsis'>Retrieves TypeAccelerators, which map aliases to full type names.</para>
    /// <para type='description'>Gets all type accelerators loaded into the current session, such as 
    /// [psobject] for [System.Management.Automation.PSObject]), which is an alias for a full type name.
    /// The type accelerator may be the same as $Type.Name or may be different, such as [xml] references 
    /// [System.Xml.XmlDocument].</para>
    /// </summary>
    /// <example>
    /// <code>
    /// PS:\> Get-TypeAccelerator -Name '*x*'
    /// Name                  Type
    /// ----                  ----
    /// regex                 System.Text.RegularExpressions.Regex
    /// X509Certificate       System.Security.Cryptography.X509Certificates.X509Certificate
    /// X500DistinguishedName System.Security.Cryptography.X509Certificates.X500DistinguishedName
    /// xml                   System.Xml.XmlDocument
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "TypeAccelerator")]
    public class GetTypeAcceleratorCommand : Cmdlet
    {
        /// <summary>
        /// <para type='description'>The TypeAccelerator alias to retrieve.</para>
        /// </summary>
        [Parameter(Position = 0)]
        [TypeAcceleratorCompletion]
        [SupportsWildcards]
        public string? Name { get; set; }
        /// <inheritdoc/>
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
                    output.TypeNames.Insert(0, "PSSharp.Pseudo.TypeAccelerator");
                    WriteObject(output);
                }
            }
        }
    }
}
