using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using PSSharp.Extensions;

namespace PSSharp
{
#nullable disable
    /// <summary>
    /// <para type="description">Represents information about a subnet range based on a given 
    /// address and Cidr or subnet mask, including the broadcast address and range.</para>
    /// </summary>
    public sealed class IPv4SubnetRange
    {
        private IPAddress _subnetMask;
        private IPAddress _networkAddress;
        private int _cidr;

        public string CidrNotation => $"{NetworkAddress}/{_cidr}";
        public int Cidr => _cidr;
        public IPAddress SubnetMask => _subnetMask;
        public IPAddress NetworkAddress => _networkAddress;
        public IPAddress BroadcastAddress { get; private set; }
        public IPAddress StartRange { get; private set; }
        public IPAddress EndRange { get; private set; }
        public long RangeSize { get; private set; }

        public IPv4SubnetRange(string cidrNotation)
        {
            var networkAddress = IPAddress.Parse(cidrNotation.Split('\\', '/')[0].Trim());
            int cidr = int.Parse(cidrNotation.Split('\\', '/')[1].Trim());
            var subnetMask = IPv4TypeConverter.ConvertCidrToSubnetMask(cidr);
            CalculateAndSetValues(networkAddress, subnetMask);
        }
        public IPv4SubnetRange(IPAddress networkAddress, IPAddress subnetMask)
        {
            CalculateAndSetValues(networkAddress, subnetMask);
        }
        public IPv4SubnetRange(IPAddress networkAddress, int cidr)
        {
            var subnetMask = IPv4TypeConverter.ConvertCidrToSubnetMask(cidr);
            CalculateAndSetValues(networkAddress, subnetMask);
        }
        private void CalculateAndSetValues(IPAddress networkAddress, IPAddress subnetMask)
        {
            _networkAddress = networkAddress;
            _subnetMask = subnetMask;
            _cidr = IPv4TypeConverter.ConvertSubnetMaskToCidr(subnetMask);

            var networkAddressInt = networkAddress.ToLong();
            var subnetMaskInt = subnetMask.ToLong();
            // bxor 1s, inverts only the last 24 bits as opposed to ~ which inverts all the bits in an Int64.
            var invertedSubnetMaskInt = subnetMaskInt ^ IPAddress.Broadcast.ToLong();
            var broadcastAddressInt = networkAddressInt + invertedSubnetMaskInt;

            BroadcastAddress = IPv4TypeConverter.ConvertNumberToIPv4(broadcastAddressInt);
            StartRange = IPv4TypeConverter.ConvertNumberToIPv4(networkAddressInt + 1);
            EndRange = IPv4TypeConverter.ConvertNumberToIPv4(broadcastAddressInt - 1);
            RangeSize = broadcastAddressInt - networkAddressInt;
        }
        /// <summary>
        /// Returns <see langword="true"/> if the given address exists within the range represented by this instance.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool Contains(IPAddress address)
        {
            var ipAddressNumber = address.ToLong();
            return ipAddressNumber <= StartRange.ToLong() && ipAddressNumber >= EndRange.ToLong();
        }
    }
}