using System;
using System.Runtime.CompilerServices;

namespace Stroniax.AsyncExtensions
{
    /// <summary>
    /// Contract for an awaiter that waits for an operation to complete.
    /// </summary>
    public interface IAwaiter : INotifyCompletion
    {
        /// <summary>
        /// Indicates whether the operation has completed.
        /// </summary>
        bool IsCompleted { get; }
        /// <summary>
        /// Throws an exception if the operation failed.
        /// </summary>
        /// <exception cref="Exception"/>
        void GetResult();
    }
    /// <summary>
    /// Contract for an awaiter that waits for an operation to complete and returns a value.
    /// </summary>
    public interface IAwaiter<out TResult> : IAwaiter
    {
        /// <summary>
        /// Retrieves the result or throws an exception if the operation failed.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="Exception"/>
        new TResult GetResult();
    }
}
