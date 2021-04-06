using PSSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;

namespace PSSharp.Commands
{
    public abstract class ObserverPSCmdlet<T> : PSCmdlet
    {
        /// <summary>
        /// Executed during each process iteration to retrieve an <see cref="IObservable{T}"/>
        /// that will be observed by this cmdlet.
        /// </summary>
        /// <returns></returns>
        public abstract IObservable<T>? GetObservationSource();
        private List<CmdletObserver>? _observers;
        private ConcurrentQueue<(bool isErrorRecord, object output)>? _output;

        [Parameter]
        public SwitchParameter AsJob { get; set; }

        protected virtual string? JobCommand { get => MyInvocation.Line; }
        protected virtual string? JobName { get => null; }
        protected sealed override void ProcessRecord()
        {
            base.ProcessRecord();
            var observable = GetObservationSource();
            if (observable is null)
            {
                return;
            }

            if (AsJob)
            {
                var job = new ObserverJob<T>(JobCommand, JobName, observable);
                JobRepository.Add(job);
                WriteObject(job);
            }
            else
            {
                _output ??= new ConcurrentQueue<(bool isErrorRecord, object output)>();
                _observers ??= new List<CmdletObserver>();
                _observers.Add(new CmdletObserver(observable, _output));

                // Drain the queue between each process iteration. This should
                // be relatively quick and will help prevent it from feeling
                // like nothing is happening until EndProcessing is reached.
                while (_output!.TryDequeue(out var output))
                {
                    if (output.isErrorRecord)
                    {
                        WriteError((ErrorRecord)output.output);
                    }
                    else
                    {
                        WriteObject(output.output);
                    }
                }
            }
        }
        protected override void EndProcessing()
        {
            base.EndProcessing();
            if (_observers != null)
            {
                while (_observers.Count > 0)
                {
                    while (_output!.TryDequeue(out var output))
                    {
                        if (output.isErrorRecord)
                        {
                            WriteError((ErrorRecord)output.output);
                        }
                        else
                        {
                            WriteObject(output.output);
                        }
                    }
                    _observers.Where(i => i.IsCompleted).ToList().ForEach(i => _observers.Remove(i));
                }
            }
        }
        protected override void StopProcessing()
        {
            base.StopProcessing();
            _observers?.ForEach(i => i.Cancel());
        }
        private class CmdletObserver : IObserver<T>
        {
            public CmdletObserver(IObservable<T> source, ConcurrentQueue<(bool isErrorRecord, object output)> cmdletOutput)
            {
                _cmdletOutput = cmdletOutput;
                _source = source;
                _cancellation = source.Subscribe(this);
            }
            private readonly ConcurrentQueue<(bool isErrorRecord, object output)> _cmdletOutput;
            private readonly IDisposable _cancellation;
            private readonly IObservable<T> _source;

            public bool IsCompleted { get; private set; }
            public void Cancel()
            {
                _cancellation?.Dispose();
                IsCompleted = true;
            }
            public void OnCompleted()
            {
                IsCompleted = true;
            }

            public void OnError(Exception error)
            {
                _cmdletOutput.Enqueue((true,
                    new ErrorRecord(
                        error,
                        "ObserverOnError",
                        ErrorCategory.NotSpecified,
                        _source)));
                IsCompleted = true;
            }

            public void OnNext(T value)
            {
                _cmdletOutput.Enqueue((false, value!));
            }
        }
    }
}
