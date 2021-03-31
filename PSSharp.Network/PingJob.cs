using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// A job representing consecutive ping operations until the target responds or
    /// the job is stopped.
    /// </summary>
    public class PingJob : Job
    {
        /// <inheritdoc/>
        public override bool HasMoreData => Output.Count > 0;
        /// <summary>The target of the <see cref="Ping"/>.</summary>
        public override string Location => _location;
        private readonly string _location;
        /// <summary>Indicates the status or error message of the most recent <see cref="PingReply"/>.</summary>
        public override string StatusMessage => _statusMessage;
        private string _statusMessage;
        /// <summary>
        /// The <see cref="Ping"/> represented by this <see cref="PingJob"/>.
        /// </summary>
        private Ping _ping;
        /// <summary>
        /// Stops the <see cref="Ping"/> loop.
        /// </summary>
        public override void StopJob()
        {
            SetJobState(JobState.Stopping);
            _ping.SendAsyncCancel();
        }
        private void StartJob()
        {
            _ping = new Ping();
            _ping.PingCompleted += OnPingCompleted;
            _ping.Disposed += (a, b) =>
            {
                if (JobStateInfo.State == JobState.Running)
                {
                    _statusMessage = $"The job was stopped because the {nameof(Ping)} instance was disposed.";
                    SetJobState(JobState.Stopped);
                }
            };
            SetJobState(JobState.Running);
            OnPingCompleted(null, null);
        }
        private void OnPingCompleted(object? sender, PingCompletedEventArgs? args)
        {
            if (sender is null || args is null)
            {
                goto sendPing;
            }
            else
            {
                // checking args.Cancelled always returns false, even after _ping.SendAsyncCancel()
                if (JobStateInfo.State == JobState.Stopping || args.Cancelled)
                {
                    _statusMessage = $"The job was stopped because the {nameof(Ping)} operation was cancelled.";
                    SetJobState(JobState.Stopped);
                    return;
                }
                else if (args.Error != null)
                {
                    _statusMessage = args.Error.ToString();
                    goto sendPing;
                }
                else if (args.Reply.Status == IPStatus.Success)
                {
                    _statusMessage = args.Reply.Status.ToString();
                    var output = new PingJobOutput(Location, args.Reply.Address);
                    Output.Add(PSObject.AsPSObject(output));
                    SetJobState(JobState.Completed);
                    return;
                }
                else
                {
                    _statusMessage = args.Reply.Status.ToString();
                    goto sendPing;
                }
            }
        sendPing:
            {
                _ping.SendAsync(Location, null);
            }
        }
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            _ping.Dispose();
            base.Dispose(disposing);
        }
        internal PingJob(string? command, string? name, string computerNameOrIp)
            : base(command, name)
        {
            PSJobTypeName = nameof(PingJob);
            _location = computerNameOrIp;
            _statusMessage = "Sending first ping.";
            _ping = null!; // set in StartJob
            StartJob();
        }
        /// <inheritdoc/>
        public override string ToString() => Location;

        /// <summary>
        /// Represents information regarding the response of the most recent ping.
        /// </summary>
        public class PingJobOutput
        {
            /// <summary>
            /// The target of the ping.
            /// </summary>
            public string ComputerName { get; }
            /// <summary>
            /// The address from which the response was received.
            /// </summary>
            public IPAddress? Address { get; }
            internal PingJobOutput(string computerName, IPAddress address)
            {
                ComputerName = computerName;
                Address = address;
            }
        }
    }
}
