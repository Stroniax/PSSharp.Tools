using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// A job representing consecutive ping operations until the target responds or the job is stopped.
    /// </summary>
    public sealed class PingJob : AdvancedJobBase
    {
        /// <summary>
        /// Creates a new, unstarted instance of <see cref="PingJob"/>.
        /// </summary>
        /// <param name="command">The PowerShell script command responsible for the creation of the new job.</param>
        /// <param name="name">The friendly name of the new job.</param>
        /// <param name="computerNameOrIp">The ComputerName or IP address to be pinged by this job.</param>
        public PingJob(string? command, string? name, string computerNameOrIp)
            :base(command, name)
        {
            _location = computerNameOrIp ?? throw new ArgumentNullException(nameof(computerNameOrIp));
            _sync = new object();
            _ping = new Ping();
            _status = "Not Started";
            _ping.PingCompleted += OnPingCompleted;
            _ping.Disposed += (a, b) =>
            {
                if (!IsFinished)
                {
                    Error.Add(new ErrorRecord(
                        new ObjectDisposedException(nameof(Ping)),
                        "PingDisposed",
                        ErrorCategory.InvalidOperation,
                        _ping));
                    SetJobState(JobState.Failed);
                }
            };
        }
        /// <summary>
        /// Triggered when the <see cref="Ping"/> asynchronously completes an attempted ping operation,
        /// successful or otherwise.
        /// </summary>
        public event PingCompletedEventHandler? PingCompleted
        {
            add
            {
                _ping.PingCompleted += value;
            }
            remove
            {
                _ping.PingCompleted -= value;
            }
        }
        private void OnPingCompleted(object? sender, PingCompletedEventArgs? e)
        {
            lock (_sync)
            {
                if (IsFinished) return;

                if (State == JobState.Stopping)
                {
                    _status = "The ping operation was stopped.";
                    SetJobState(JobState.Stopped);
                    OnStopJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                    return;
                }
                else if (State == JobState.Suspending)
                {
                    _status = "The ping operation was suspended.";
                    SetJobState(JobState.Suspended);
                    OnSuspendJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                    return;
                }
                else if (e?.Error != null)
                {
                    _status = e.Error?.ToString() ?? _status;
                }
                else if (e?.Reply.Status == IPStatus.Success)
                {
                    var output = new PingJob.PingJobOutput(_location, e.Reply.Address);
                    Output.Add(PSObject.AsPSObject(output));
                    SetJobState(JobState.Completed);
                    return;
                }
                else if (e != null)
                {
                    _status = e.Reply.Status.ToString();
                }

                var initState = State;
                SetJobState(JobState.Running);
                if (initState == JobState.NotStarted)
                {
                    OnStartJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                }
                else if (initState == JobState.Suspended)
                {
                    OnResumeJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                }
                _ping.SendAsync(Location, null);
            }
        }
        
        private readonly object _sync;
        private readonly Ping _ping;
        private readonly string _location;
        private string _status;
        /// <summary>
        /// The most recent ping exception message or reply status.
        /// </summary>
        public override string StatusMessage => _status;
        /// <summary>
        /// The destination address of the ping.
        /// </summary>
        public sealed override string Location => _location;

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            lock(_sync)
            {
                if (State == JobState.Running)
                {
                    Error.Add(new ErrorRecord(
                        new ObjectDisposedException(nameof(PingJob)),
                        "JobDisposed",
                        ErrorCategory.InvalidOperation,
                        this));
                    SetJobState(JobState.Failed);
                }
            }
            _ping.Dispose();
            base.Dispose(disposing);
        }
        /// <summary>Returns the location (ComputerName or IP address) destination of the ping.</summary>
        /// <returns><see cref="PingJob.Location"/></returns>
        public override string ToString() => Location;
        /// <inheritdoc/>
        public override void ResumeJob()
        {
            lock (_sync)
            {
                if (State != JobState.Suspended)
                {
                    throw new InvalidJobStateException(State, "The job cannot be resumed unless the current state is Suspended.");
                }
                OnPingCompleted(null, null);
            }
        }
        /// <inheritdoc/>
        public override void StartJob()
        {
            lock (_sync)
            {
                if (State != JobState.NotStarted)
                {
                    throw new InvalidJobStateException(State, "The job cannot be started unless the current state is NotStarted.");
                }
                OnPingCompleted(null, null);
            }
        }
        /// <inheritdoc/>
        public override void StopJob(bool force, string reason)
        {
            lock(_sync)
            {
                if (!IsFinished)
                {
                    SetJobState(IsHalted ? JobState.Stopped : JobState.Stopping);
                }
            }
        }
        /// <inheritdoc/>
        public override void SuspendJob(bool force, string reason)
        {
            lock(_sync)
            {
                if (IsFinished)
                {
                    throw new InvalidJobStateException(State, "The job cannot be suspended unless the current state is not terminal.");
                }
                if (IsHalted)
                {
                    SetJobState(JobState.Suspended);
                }
                else
                {
                    SetJobState(JobState.Suspending);
                }
            }
        }
        /// <inheritdoc/>
        public override void UnblockJob()
        {
            throw new NotSupportedException();
        }


        /// <summary>Represents information regarding the response of the conclusive ping.</summary>
        public class PingJobOutput
        {
            /// <summary>The target of the ping.</summary>
            public string ComputerName { get; }
            /// <summary>The address from which the response was received.</summary>
            public IPAddress? Address { get; }
            internal PingJobOutput(string computerName, IPAddress address)
            {
                ComputerName = computerName;
                Address = address;
            }
        }
    }
}
