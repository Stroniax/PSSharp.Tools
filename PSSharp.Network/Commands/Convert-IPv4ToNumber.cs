using System;
using System.Management.Automation;
using System.Net;

namespace PSSharp.Commands
{
    /// <summary>
    /// <para type="synopsis">Converts IPv4 addresses to their number value.</para>
    /// <para type="description">Converts one or more IPv4 address values (in the 
    /// <see cref="System.Net.Sockets.AddressFamily.InterNetwork"/> address family)
    /// to a number value representing the address. Though using this module indicates
    /// that you have a reason for it already, this conversion is most often used for 
    /// comparison or data storage.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// PS:\ > Convert-IPv4ToNumber -InputObject 192.168.50.3
    /// 3232248323
    /// </code>
    /// </example>
    /// <example>
    /// <code>
    /// PS:\ > Get-DhcpServerv4Scope -ComputerName Server01 | Get-DhcpServerv4Lease -ComputerName Server01 | Convert-IPv4ToNumber 
    /// 2887165501
    /// 2887165502
    /// 2887165503
    /// [ additional rows omitted ]
    /// </code>
    /// <para type="description">Values can be piped into the cmdlet by value as well as by property name.</para>
    /// </example>
    [Cmdlet(VerbsData.Convert, "IPv4ToNumber")]
    [OutputType(typeof(long))]
    public class ConvertIpv4ToNumberCommand : Cmdlet
    {
        /// <summary>
        /// <para type="description">The IPv4 address value(s) to convert to numbers.
        /// The values provided must be in the InterNetwork (IPv4) address family: 
        /// values cannot be greater than [IPAddress]::Broadcast (255.255.255.255)
        /// or less than [IPAddress]::Any (0.0.0.0).</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("Address", "Value", "IPAddress")]
        [IPv4AddressCompletion]
        public IPAddress[] InputObject { get; set; } = new IPAddress[0];
        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            foreach (var input in InputObject)
            {
                if (input.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    WriteError(new ErrorRecord(
                        new PSArgumentException("The address provided is not an IPv4 address (address family is not InterNetwork)."),
                        "InvalidAddressFamily",
                        ErrorCategory.InvalidArgument,
                        input)
                    {
                        ErrorDetails = new ErrorDetails($"The IP address '{input}' is not an IPv4 address. Only IP addresses " +
                        $"in the {System.Net.Sockets.AddressFamily.InterNetwork} address family can be converted by this cmdlet.")
                        {
                            RecommendedAction = "Only provide IPv4 addresses."
                        }
                    }
                        );
                }
                else
                {
                    WriteObject(IPv4TypeConverter.ConvertIPv4ToNumber(input));
                }
            }
        }
    }
}
