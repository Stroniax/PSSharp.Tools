namespace PSSharp
{
    /// <summary>
    /// Indicates behavior for a <see cref="TaskJob"/> that is constructed with an action to be invoked instead of with existing tasks.
    /// </summary>
    public enum TaskJobMode
    {
        /// <summary>
        /// All child jobs should be executed simultaneously.
        /// </summary>
        Parallel,
        /// <summary>
        /// All child jobs should be executed only after the preceeding child job has completed.
        /// </summary>
        Consecutive,
        /// <summary>
        /// All child jobs should be executed only after the preceeding child job has completed.
        /// If a child job fails, the following jobs should not be executed.
        /// </summary>
        ConsecutiveStopAtFailure,
    }
}
