using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Text;

namespace PSSharp.Commands
{
    /// <summary>
    /// <para type="synopsis">Gets the IP subnet range of a given address and CIDR or subnet mask.</para>
    /// <para type="description">Identifies and returns the subnet range of a given address and 
    /// corresponding CIDR or subnet mask. Returns the broadcast address (the end of the range), 
    /// the subnet address (often the scope id - the beginning of the range), and the subnet mask.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// PS:\ > Get-IPv4SubnetRange -CidrNotation '172.22.78.0/24' | Select *
    /// 
    /// CidrNotation      : 172.22.78.0/24
    /// SubnetMask        : 255.255.255.0
    /// NetworkAddress    : 172.22.78.0
    /// BroadcastAddress  : 172.22.78.255
    /// StartRange        : 172.22.78.1
    /// EndRange          : 172.22.78.254
    /// Range             : { 172.22.78.1, 172.22.78.2, 172.22.78.3 ... }
    /// IPAddress         : 172.22.78.0
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "IPv4SubnetRange", DefaultParameterSetName = "DefaultParameterSet")]
    [OutputType(typeof(IPv4SubnetRange))]
    public class GetIPv4SubnetRange : Cmdlet
    {
        private const int MaxCidrValue = 32;
        /// <summary>
        /// <para type="description">The IPv4 network address of the subnet; for example, '192.168.150.0'.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "AddressSubnetMask")]
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "AddressCIDR")]
        [IPv4AddressCompletion]
        public IPAddress NetworkAddress { get; set; } = null!;
        /// <summary>
        /// <para type="description">The CIDR value of the subnet; for example, the '24' in '192.168.150.0/24'. 
        /// This value is converted to a subnet with the equivalent number of bits set to true: a CIDR value of 24
        /// will result in a subnet with binary value '11111111.11111111.11111111.00000000', equivalent to
        /// '255.255.255.0'.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "AddressCIDR")]
        [ValidateRange(0, MaxCidrValue)]
        [NumericCompletion(0, MaxCidrValue)]
        public int CIDR { get; set; }
        /// <summary>
        /// <para type="description">The mask of the subnet, such as '255.255.255.0'.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "AddressSubnetMask")]
        [IPv4AddressCompletion]
        public IPAddress SubnetMask { get; set; } = null!;
        /// <summary>
        /// <para type="description">The network address and CIDR separated by a forward slash: for example, '192.168.150.0/24'. 
        /// This value is split and parsed to determine the network address and subnet mask.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "DefaultParameterSet", ValueFromPipeline = true,
            HelpMessage = "Provide the IP address and CIDR mask separated by a forward slash (ex: '192.168.150.0/24').")]
        [IPv4AddressCompletion]
        public string CidrNotation { get; set; } = null!;
        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            if (!(CidrNotation is null))
            {

                if (IPAddress.TryParse(CidrNotation.Split('/', '\\')[0].Trim(), out var parsedNetworkAddress))
                {
                    NetworkAddress = parsedNetworkAddress;
                }
                else
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException("An IP address could not be identified within the input object. Specify an address and CIDR separated by a forward slash and try again."),
                        "ParseIPAddressFailed",
                        ErrorCategory.InvalidArgument,
                        CidrNotation.Split('/', '\\')[0].Trim()
                        )
                    {
                        ErrorDetails = new ErrorDetails($"Failed to identify an IP address in the input value '{CidrNotation}'." +
                        $" The input value should be an IP address and CIDR separated with a forward slash: for example, '192.168.10.0/24'.")
                    }
                        );
                    return;
                }
                if (int.TryParse(CidrNotation.Split('\\', '/')[1].Trim(), out var cidr))
                {
                    if (cidr > MaxCidrValue)
                    {
                        WriteError(new ErrorRecord(
                            new ArgumentOutOfRangeException(nameof(CIDR), cidr, $"The identified CIDR value is greater than the maximum allowed value."),
                            "CidrOutOfRange",
                            ErrorCategory.InvalidArgument,
                            cidr
                            )
                        {
                            ErrorDetails = new ErrorDetails($"The CIDR value '{cidr}' identified within the input object is invalid: the value should not be greater than {MaxCidrValue}.")
                        }
                            );
                        return;
                    }
                    else if (cidr < 0)
                    {
                        WriteError(new ErrorRecord(
                            new ArgumentOutOfRangeException(nameof(CIDR), cidr, $"The identified CIDR value is lower than the minimum allowed value."),
                            "CidrOutOfRange",
                            ErrorCategory.InvalidArgument,
                            cidr
                            )
                        {
                            ErrorDetails = new ErrorDetails($"The CIDR value '{cidr}' identified within the input object is invalid: the value should not be less than 0.")
                        }
                            );
                        return;
                    }
                    SubnetMask = IPv4TypeConverter.ConvertCidrToSubnetMask(cidr);
                }
                else
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException("A CIDR value could not be identified within the input object. Specify an address and CIDR separated by a forward slash and try again."),
                        "ParseCIDRFailed",
                        ErrorCategory.InvalidArgument,
                        CidrNotation.Split('/', '\\')[1].Trim()
                        )
                    {
                        ErrorDetails = new ErrorDetails($"Failed to identify a CIDR value in the input value '{CidrNotation}'." +
                        $" The input value should be an IP address and CIDR separated with a forward slash: for example, '192.168.10.0/24'.")
                    }
                        );
                    return;
                }
            }
            if (SubnetMask is null)
            {
                SubnetMask = IPv4TypeConverter.ConvertCidrToSubnetMask(CIDR);
            }

            WriteObject(new IPv4SubnetRange(NetworkAddress, SubnetMask));
        }
    }
}
