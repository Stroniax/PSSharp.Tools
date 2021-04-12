using System.Management.Automation;

namespace PSSharp
{
    /// <summary>
    /// Provides a mechanism for receiving push-based notifications.
    /// </summary>
    /// <typeparam name="T">The object that provides notification information.</typeparam>
    public interface IPSObserver<in T>
    {
        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="output">The current output data.</param>
        void OnOutput(T output);
        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        void OnError(ErrorRecord error);
        /// <summary>
        /// Notifies the observer that the provider has encountered an unrecoverable error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        void OnFailed(ErrorRecord error);
        /// <summary>
        /// Provides the observer with a warning message.
        /// </summary>
        /// <param name="warning">The warning message.</param>
        void OnWarning(string warning);
        /// <summary>
        /// Provides the observer with a verbose message.
        /// </summary>
        /// <param name="warning">The verbose message.</param>
        void OnVerbose(string verbose);
        /// <summary>
        /// Provides the observer with a debug message.
        /// </summary>
        /// <param name="warning">The debug message.</param>
        void OnDebug(string debug);
        /// <summary>
        /// Provides the observer with updated progress information.
        /// </summary>
        /// <param name="warning">The progress update.</param>
        void OnProgress(ProgressRecord progress);
        /// <summary>
        /// Provides the observer with informational data.
        /// </summary>
        /// <param name="warning">The informational data.</param>
        void OnInformation(InformationRecord information);
        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        void OnCompleted();
    }
}
