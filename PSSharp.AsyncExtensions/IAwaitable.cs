namespace PSSharp
{
    /// <summary>
    /// Exposes an awaiter, which can be used to asynchronously wait for an operation to complete.
    /// </summary>
    public interface IAwaitable
    {
        /// <summary>
        /// Returns an awaiter that can be used to wait for the operation to complete.
        /// </summary>
        /// <returns>An awaiter that can be used to wait for the operation to complete.</returns>
        IAwaiter GetAwaiter();
    }
    /// <summary>
    /// Exposes an awaiter, which can be used to asynchronously wait for an operation to complete
    /// and return a result of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The type returned by the operation.</typeparam>
    public interface IAwaitable<out TResult> : IAwaitable
    {
        /// <summary>
        /// Returns an awaiter that can be used to wait for the operation to complete and retrieve the result.
        /// </summary>
        /// <returns>An awaiter that can be used to wait for the operation to complete.</returns>
        new IAwaiter<TResult> GetAwaiter();
    }
}
