using System;
using System.Management.Automation;

namespace PSSharp
{
    internal class PSActionObserver<T> : IPSObserver<T>
    {
        private readonly Action<PSActionObserver<T>, T>? _onOutput;
        private readonly Action<PSActionObserver<T>>? _onCompleted;
        private readonly Action<PSActionObserver<T>, ErrorRecord>? _onFailed;
        private readonly Action<PSActionObserver<T>, string>? _onDebug;
        private readonly Action<PSActionObserver<T>, string>? _onWarning;
        private readonly Action<PSActionObserver<T>, string>? _onVerbose;
        private readonly Action<PSActionObserver<T>, ErrorRecord>? _onError;
        private readonly Action<PSActionObserver<T>, InformationRecord>? _onInformation;
        private readonly Action<PSActionObserver<T>, ProgressRecord>? _onProgress;

        public PSActionObserver(Action<PSActionObserver<T>, T>? onOutput,
                                Action<PSActionObserver<T>>? onCompleted,
                                Action<PSActionObserver<T>, ErrorRecord>? onFailed,
                                Action<PSActionObserver<T>, string>? onDebug,
                                Action<PSActionObserver<T>, string>? onWarning,
                                Action<PSActionObserver<T>, string>? onVerbose,
                                Action<PSActionObserver<T>, ErrorRecord>? onError,
                                Action<PSActionObserver<T>, InformationRecord>? onInformation,
                                Action<PSActionObserver<T>, ProgressRecord>? onProgress)
        {
            _onOutput = onOutput;
            _onCompleted = onCompleted;
            _onFailed = onFailed;
            _onDebug = onDebug;
            _onWarning = onWarning;
            _onVerbose = onVerbose;
            _onError = onError;
            _onInformation = onInformation;
            _onProgress = onProgress;
        }

        public void OnCompleted() => _onCompleted?.Invoke(this);
        public void OnFailed(ErrorRecord error) => _onFailed?.Invoke(this, error);
        public void OnDebug(string debug) => _onDebug?.Invoke(this, debug);
        public void OnError(ErrorRecord error) => _onError?.Invoke(this, error);
        public void OnInformation(InformationRecord information) => _onInformation?.Invoke(this, information);
        public void OnOutput(T output) => _onOutput?.Invoke(this, output);
        public void OnProgress(ProgressRecord progress) => _onProgress?.Invoke(this, progress);
        public void OnVerbose(string verbose) => _onVerbose?.Invoke(this, verbose);
        public void OnWarning(string warning) => _onWarning?.Invoke(this, warning);
    }
}
