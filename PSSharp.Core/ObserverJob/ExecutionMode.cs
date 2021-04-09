namespace PSSharp
{
    /// <summary>
    /// Indicates the order in which child operations should be executed.
    /// </summary>
    public enum ExecutionMode
    {
        /// <summary>
        /// All operations should be executed in parallel, regardless of the state of other operations.
        /// </summary>
        Concurrent = 0,
        /// <summary>
        /// Operations should be executed one after another - no operations should be executed simultaneously.
        /// </summary>
        Consecutive = 1,
        /// <summary>
        /// Operations should be executed one after another - no operations should be executed simultaneously.
        /// If any operation fails, all following operations should be cancelled.
        /// </summary>
        ConsecutiveUntilError = 2
    }
}
