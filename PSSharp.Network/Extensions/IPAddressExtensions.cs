using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PSSharp.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="IPAddress"/> class.
    /// </summary>
    public static class IPAddressExtensions
    {
        /// <summary>
        /// Converts an IP address the associated <see cref="long"/> value.
        /// </summary>
        /// <param name="ipAddress">The address to be converted.</param>
        /// <returns>The <see cref="long"/> value representation of <paramref name="ipAddress"/>.</returns>
        public static long ToLong(this IPAddress ipAddress) => IPv4TypeConverter.ConvertIPv4ToNumber(ipAddress);
    }
}
