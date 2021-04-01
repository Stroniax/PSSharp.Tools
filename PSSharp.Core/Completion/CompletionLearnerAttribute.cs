using System.Management.Automation;

namespace PSSharp
{
    /// <summary>
    /// <para type='description'>Apply to PowerShell parameters to learn parameters that can later
    /// be recommended by the <see cref="LearnedCompletionAttribute"/>.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// function Use-LearnedCompletion {
    ///     [CmdletBinding()]
    ///     param (
    ///         [Parameter(Position = 0, Mandatory)]
    ///         [PSSharp.CompletionLearner("Use-LearnedCompletion", "SpecialParameter")]
    ///         [PSSharp.LearnedCompletion("Use-LearnedCompletion", "SpecialParameter")]
    ///         [System.String[]]
    ///         $SpecialParameter
    ///     )
    ///     process {
    ///         "Special Parameter '$SpecialParameter' is learned. Try using this function again and press [Tab] key to see your learned completion results!"
    ///     }
    /// }
    /// </code>
    /// This example demonstrates using the <see cref="CompletionLearnerAttribute"/> and <see cref="LearnedCompletionAttribute"/> for a parameter of a PowerShell function.
    /// </example>
    public sealed class CompletionLearnerAttribute : ValidateArgumentsAttribute
    {
        /// <summary>
        /// Apply to PowerShell parameters to learn parameters that can later
        /// be recommended by the <see cref="LearnedCompletionAttribute"/>.
        /// </summary>
        /// <param name="command">The command that values will be associated with for future completion. 
        /// Generally, use the name of the command this attribute is being applied to.</param>
        /// <param name="parameter">The parameter that the values will be associated with for future completion.
        /// Generally, use the name of the parameter this attribute is being applied to.</param>
        public CompletionLearnerAttribute(string command, string parameter)
        {
            Command = command;
            Parameter = parameter;
        }
        /// <summary>
        /// The command that values will be associated with for future completion.
        /// </summary>
        public string Command { get; }
        /// <summary>
        /// The parameter that the values will be associated with for future completion.
        /// </summary>
        public string Parameter { get; }
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (arguments is object[] manyArgs)
            {
                foreach (var arg in manyArgs)
                {
                    LearnedCompletionData.LearnCompletion(Command, Parameter, arg.ToString());
                }
            }
            else
            {
                LearnedCompletionData.LearnCompletion(Command, Parameter, arguments.ToString());
            }
        }
    }
}
