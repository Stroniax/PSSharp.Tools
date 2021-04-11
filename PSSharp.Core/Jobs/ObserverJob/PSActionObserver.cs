using System;
using System.Management.Automation;

namespace PSSharp
{
    internal class PSActionObserver<T> : IPSObserver<T>
    {
        private Action<PSActionObserver<T>, T>? _onOutput;
        private Action<PSActionObserver<T>>? _onCompleted;
        private Action<PSActionObserver<T>, string>? _onDebug;
        private Action<PSActionObserver<T>, string>? _onWarning;
        private Action<PSActionObserver<T>, string>? _onVerbose;
        private Action<PSActionObserver<T>, ErrorRecord, bool>? _onError;
        private Action<PSActionObserver<T>, InformationRecord>? _onInformation;
        private Action<PSActionObserver<T>, ProgressRecord>? _onProgress;

        public void OnCompleted() => _onCompleted?.Invoke(this);
        public void OnDebug(string debug) => _onDebug?.Invoke(this, debug);
        public void OnError(ErrorRecord error, bool isTerminatingError = false) => _onError?.Invoke(this, error, isTerminatingError);
        public void OnInformation(InformationRecord information) => _onInformation?.Invoke(this, information);
        public void OnOutput(T output) => _onOutput?.Invoke(this, output);
        public void OnProgress(ProgressRecord progress) => _onProgress?.Invoke(this, progress);
        public void OnVerbose(string verbose) => _onVerbose?.Invoke(this, verbose);
        public void OnWarning(string warning) => _onWarning?.Invoke(this, warning);
    }
}
