using System;
using System.Management.Automation;
using System.Net;
using System.Text;

namespace PSSharp.Commands
{
    /// <summary>
    /// <para type='synopsis'>Sends a magic packet to a target MAC address to indicate that the computer should turn on.</para>
    /// <para type='description'>Sends a "magic packet" to a destination MAC address over UDP through the defined broadcast 
    /// address (or [IPAddress]::Broadcast by default) to wake a target computer.</para>
    /// </summary>
    [Cmdlet(VerbsCommunications.Send, "WakeOnLan")]
    [Alias("swol")]
    public class SendWakeOnLanCommand : PSCmdlet
    {
        /// <summary>
        /// <para type='description'>The mac address(es) to which the WakeOnLan command will be sent.</para>
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true, Mandatory = true, Position = 0)]
        [CompletionLearner("Send-WakeOnLan", "MacAddress")]
        [LearnedCompletion("Send-WakeOnLan", "MacAddress")]
        public string[] MacAddress { get; set; } = null!;

        /// <summary>
        /// <para type='description'>Determines the address used to broadcast the packet.</para>
        /// </summary>
        [Parameter()]
        [ValidateNotNullOrEmpty]
        [IPv4AddressCompletion]
        public IPAddress BroadcastAddress { get; set; } = IPAddress.Broadcast;

        /// <summary>
        /// <para type='description'>Determines the port to which the packet is sent.</para>
        /// </summary>
        [Parameter]
        [ValidateRange(1, ushort.MaxValue)]
        [NumericCompletion(1, ushort.MaxValue)]
        public ushort Port { get; set; } = WakeOnLan.DefaultPort;

        /// <inheritdoc/>
        protected override void BeginProcessing()
        {
            if (Port < 1)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new ArgumentOutOfRangeException("Port must be greater than 0.", nameof(Port)),
                    "PortOutOfRange",
                    ErrorCategory.InvalidArgument,
                    Port));
            }
        }
        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            foreach (var mac in MacAddress)
            {
                try
                {
                    var result = WakeOnLan.Send(mac, BroadcastAddress, Port);
                    WriteObject(result);
                }
                catch (ArgumentNullException e)
                {
                    var param = e.ParamName == "macAddress" ? nameof(MacAddress) : nameof(BroadcastAddress);
                    var arg = e.ParamName == "macAddress" ? MacAddress : BroadcastAddress as object;
                    WriteError(new ErrorRecord(
                        e,
                        param + "Null",
                        ErrorCategory.InvalidArgument,
                        arg));
                }
                catch (ArgumentException e)
                {
                    WriteError(new ErrorRecord(
                        e,
                        "InvalidMacAddress",
                        ErrorCategory.InvalidArgument,
                        mac));
                }
            }
        }
    }
}
