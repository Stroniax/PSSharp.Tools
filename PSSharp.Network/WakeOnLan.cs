using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace PSSharp
{
    /// <summary>
    /// <para type="description">Provides static methods to wake remote computers. As an instance, contains information about a sent packet.</para>
    /// </summary>
    public sealed class WakeOnLan
    {
        #region Properties
        /// <summary>
        /// The default port that Wake On Lan packets will be sent to if not set explicitly.
        /// </summary>
        public const ushort DefaultPort = 9;
        /// <summary>
        /// The mac address to which the wake on lan packet was sent.
        /// </summary>
        public string MacAddress { get; }
        /// <summary>
        /// The broadcast address used to send the wake on lan packet. Default is <see cref="IPAddress.Broadcast"/>.
        /// </summary>
        public IPAddress BroadcastAddress { get; }
        /// <summary>
        /// The port that the wake on lan packet was sent to. Default is <see cref="DefaultPort"/> (9).
        /// </summary>
        public ushort Port { get; }
        /// <summary>
        /// The number of bytes sent to the client.
        /// </summary>
        public int BytesSent { get; }
        /// <summary>
        /// The time at which the packet was sent.
        /// </summary>
        public DateTime Sent { get; } = DateTime.Now;
        /// <summary>
        /// Constructor for setting values privately.
        /// </summary>
        /// <param name="macAddress"></param>
        /// <param name="broadcastAddress"></param>
        /// <param name="port"></param>
        /// <param name="bytesSent"></param>
        private WakeOnLan(string macAddress, IPAddress broadcastAddress, ushort port, int bytesSent)
        {
            MacAddress = macAddress;
            BroadcastAddress = broadcastAddress;
            Port = port;
            BytesSent = bytesSent;
        }
        #endregion
        #region Static Methods
        /// <summary>
        /// Sends a magic packet to wake the computer at the provided mac address, using defaults of <see cref="IPAddress.Broadcast"/> and <see cref="DefaultPort"/>.
        /// </summary>
        /// <param name="macAddress">The physical (mac) address of the computer that the packet will be sent to.</param>
        /// <returns>A <see cref="WakeOnLan"/> instance containing information about the packet sent.</returns>
        public static WakeOnLan Send(string macAddress) => Send(macAddress, IPAddress.Broadcast, DefaultPort);
        /// <summary>
        /// Sends a magic packet to wake the computer at the provided mac address, using the default of <see cref="IPAddress.Broadcast"/>.
        /// </summary>
        /// <param name="macAddress">The physical (mac) address of the computer that the packet will be sent to.</param>
        /// <param name="broadcastAddress">The broadcast address that the packet will be sent to. Usually <see cref="IPAddress.Broadcast"/></param>
        /// <returns>A <see cref="WakeOnLan"/> instance containing information about the packet sent.</returns>
        public static WakeOnLan Send(string macAddress, IPAddress broadcastAddress) => Send(macAddress, broadcastAddress, DefaultPort);
        /// <summary>
        /// Sends a magic packet to wake the computer at the provided mac address, using the default of <see cref="DefaultPort"/>.
        /// </summary>
        /// <param name="macAddress">The physical (mac) address of the computer that the packet will be sent to.</param>
        /// <param name="port">The port that the magic packet will be sent to.</param>
        /// <returns>A <see cref="WakeOnLan"/> instance containing information about the packet sent.</returns>
        public static WakeOnLan Send(string macAddress, ushort port) => Send(macAddress, IPAddress.Broadcast, port);
        /// <summary>
        /// Sends a magic packet to wake the computer at the provided mac address, with all values explicitly set.
        /// </summary>
        /// <param name="macAddress">The physical (mac) address of the computer that the packet will be sent to.</param>
        /// <param name="broadcastAddress">The broadcast address that the packet will be sent to. Usually <see cref="IPAddress.Broadcast"/></param>
        /// <param name="port">The port that the magic packet will be sent to.</param>
        /// <returns>A <see cref="WakeOnLan"/> instance containing information about the packet sent.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="macAddress"/> or <paramref name="broadcastAddress"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="port"/> is 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="macAddress"/> is an invalid format or length. 
        /// Should be "00:00:00:00:00:00", but may use spaces (" ") or hyphens ("-") as delimiters.</exception>
        public static WakeOnLan Send(string macAddress, IPAddress broadcastAddress, ushort port)
        {
            if (macAddress is null) throw new ArgumentNullException(nameof(macAddress));
            if (broadcastAddress is null) throw new ArgumentNullException(nameof(broadcastAddress));
            if (port == ushort.MinValue) throw new ArgumentOutOfRangeException(nameof(port));
            int bytes;
            var packet = GetMagicPacket(macAddress);
            using var client = new UdpClient();
            client.Connect(broadcastAddress, port);
            bytes = client.Send(packet, packet.Length);
            return new WakeOnLan(macAddress, broadcastAddress, port, bytes);
        }
        /// <summary>
        /// Creates the magic packet based on the provided mac address.
        /// </summary>
        /// <returns></returns>
        private static byte[] GetMagicPacket(string macAddress)
        {
            var formattedMac = Regex.Replace(macAddress, "[ :-]", "");
            if (formattedMac.Length > 12)
            {
                throw new ArgumentException("Invalid mac address. The value must be six hexidecimal bytes, optionally split " +
                    "by space (\" \"), colon (\":\"), or hyphen (\"-\") characters.", nameof(macAddress));
            }
            byte[] macBytes = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                macBytes[i] = Convert.ToByte(formattedMac.Substring(i * 2, 2), 16);
            }
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            for (int i = 0; i < 6; i++)
            {
                bw.Write((byte)0xff);
            }
            for (int i = 0; i < 16; i++)
            {
                bw.Write(macBytes);
            }
            return ms.ToArray();
        }
        #endregion
    }
}