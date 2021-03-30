using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Net;
using System.Text;

namespace PSSharp.Commands
{
    /// <summary>
    /// <para type="synopsis">Converts a number to an IPv4 address.</para>
    /// <para type="description">
    /// Converts a number ([double] or [long]) to an IPv4 address.
    /// Any number value between 0 and 4294967295 can be converted to an IPv4 address.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// PS:\ > Convert-NumberToIPv4 -InputObject 3232248323
    /// Address            : 53651648
    /// AddressFamily      : InterNetwork
    /// ScopeId            :
    /// IsIPv6Multicast    : False
    /// IsIPv6LinkLocal    : False
    /// IsIPv6SiteLocal    : False
    /// IsIPv6Teredo       : False
    /// IsIPv4MappedToIPv6 : False
    /// IPAddressToString  : 192.168.50.3
    /// </code>
    /// </example>
    /// <example>
    /// <code>
    /// PS:\ > 3232248320..3232248323 | Convert-NumberToIPv4 | Select AddressFamily, IPAddressToString
    /// AddressFamily IPAddressToString
    /// ------------- -----------------
    ///  InterNetwork 192.168.50.0
    ///  InterNetwork 192.168.50.1
    ///  InterNetwork 192.168.50.2
    ///  InterNetwork 192.168.50.3
    /// </code>
    /// <para type="description">Values can be piped into the cmdlet by value as well as by property name.</para>
    /// </example>
    [Cmdlet(VerbsData.Convert, "NumberToIPv4")]
    [OutputType(typeof(IPAddress))]
    public class ConvertNumberToIPv4Command : Cmdlet
    {
        /// <summary>
        /// <para type='description'>The number value(s) of any IPAddress(es) to convert. 
        /// Each value will be converted to an <see cref="IPAddress"/>. The value cannot 
        /// be less than 0 or greater than 4294967295.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [ValidateRange(0, 4294967295)]
        [Alias("Address", "Value", "IPAddress")]
        public long[] InputObject { get; set; } = new long[0];
        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            foreach (var input in InputObject)
            {
                WriteObject(IPv4TypeConverter.ConvertNumberToIPv4(input));
            }
        }
    }
}
