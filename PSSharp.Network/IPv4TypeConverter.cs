using System;
using System.Net;
using System.Linq;
using System.Management.Automation;


namespace PSSharp
{
    /// <summary>
    /// <para type="description">Converts IPv4 addresses to their <see cref="double"/> or <see cref="long"/> values, and vice versa.</para>
    /// </summary>
    [PSTypeConverter(typesToConvert: typeof(IPAddress))]
    public class IPv4TypeConverter : PSTypeConverter
    {
        #region PSTypeConverter
        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            if (sourceValue is double dbl && destinationType == typeof(IPAddress))
            {
                if (dbl < ConvertIPv4ToNumber(IPAddress.Broadcast))
                {
                    return true;
                }
            }
            else if (sourceValue is long lng && destinationType == typeof(IPAddress))
            {
                if (lng < ConvertIPv4ToNumber(IPAddress.Broadcast))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool CanConvertTo(object sourceValue, Type destinationType)
        {

            if (sourceValue is IPAddress ip && (destinationType == typeof(double) || destinationType == typeof(long)))
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return true;
                }
            }
            return false;
        }

        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            if (sourceValue is double dbl && destinationType == typeof(IPAddress))
            {
                return ConvertNumberToIPv4((long)dbl);
            }
            else if (sourceValue is long lng && destinationType == typeof(IPAddress))
            {
                return ConvertNumberToIPv4(lng);
            }
            else
            {
                throw new PSInvalidCastException();
            }
        }

        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            if (sourceValue is IPAddress ip && (destinationType == typeof(double) || destinationType == typeof(long))
                && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ConvertIPv4ToNumber(ip);
            }
            else
            {
                throw new PSInvalidCastException();
            }
        }
        #endregion

        #region Code Methods/Properties
        public static string GetBinaryAddressString(PSObject obj)
        {
            if (obj.BaseObject is IPAddress address)
            {
                var binary = Convert.ToString(ConvertIPv4ToNumber(address), 2);
                if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) { return binary; }
                for (int i = binary.Length - 1; i > 0; i--)
                {
                    if (i % 8 == 0)
                    {
                        binary = binary.Insert(i, ".");
                    }
                }
                return binary;
            }
            throw new ArgumentException();
        }
        #endregion
        public static long ConvertIPv4ToNumber(IPAddress input)
        {
            long address = 0;
            byte[] octets = input.GetAddressBytes();
            long multiplyByValue = 1;
            for (int i = octets.Length; i > 0; i--)
            {
                address += octets[i - 1] * multiplyByValue;
                multiplyByValue *= 256;
            }
            return address;
        }
        public static IPAddress ConvertNumberToIPv4(long input)
        {
            var octets = new byte[4];
            octets[0] = (byte)Math.Truncate((double)input / 16777216);
            octets[1] = (byte)Math.Truncate((double)input % 16777216 / 65536);
            octets[2] = (byte)Math.Truncate((double)input % 65536 / 256);
            octets[3] = (byte)Math.Truncate((double)input % 256);
            return new IPAddress(octets);
        }
        public static IPAddress ConvertCidrToSubnetMask(int cidr)
        {
            if (cidr > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(cidr), cidr, "The CIDR value cannot be greater than 32.");
            }
            else if (cidr < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cidr), cidr, "The CIDR value cannot be less than 0.");
            }
            string bitValueString = string.Empty;
            for (int i = 0; i < cidr; i++) { bitValueString += "1"; }
            for (int i = 0; i < 32 - cidr; i++) { bitValueString += "0"; }
            long maskInt = Convert.ToInt64(bitValueString, 2);
            return ConvertNumberToIPv4(maskInt);
        }
        public static int ConvertSubnetMaskToCidr(IPAddress subnetMask)
        {
            string subnetMaskBinaryString = Convert.ToString(ConvertIPv4ToNumber(subnetMask), 2);
            var firstIndexOfZero = subnetMaskBinaryString.IndexOf('0');
            if (subnetMaskBinaryString.Substring(firstIndexOfZero).Contains('1'))
            {
                throw new ArgumentException("The address provided is not a valid subnet mask.");
            }
            return subnetMaskBinaryString.IndexOf('0');
        }
    }
}
