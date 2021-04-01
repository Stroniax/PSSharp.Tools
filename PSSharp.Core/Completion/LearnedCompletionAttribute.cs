using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp
{
    /// <summary>
    /// <para type='description'>Apply to PowerShell parameters to use parameters that have formerly
    /// been learned by the <see cref="CompletionLearnerAttribute"/>.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// function Use-LearnedCompletion {
    ///     [CmdletBinding()]
    ///     param (
    ///         [Parameter(Position = 0, Mandatory)]
    ///         [PSSharp.CompletionLearner("Use-LearnedCompletion", "SpecialParameter")]
    ///         [PSSharp.LearningCompletion("Use-LearnedCompletion", "SpecialParameter")]
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
    public sealed class LearnedCompletionAttribute : CompletionBaseAttribute
    {
        /// <summary>
        /// Uses the names of the command and parameter of the executing command to retrieve completion results.
        /// </summary>
        public LearnedCompletionAttribute()
            : base (() => new Completer(null, null))
        {

        }
        /// <summary>
        /// Uses the names of the command and parameter provided to retrieve completion results. This can be used
        /// to share completion between multiple commands that have a similar expected usage value set.
        /// </summary>
        public LearnedCompletionAttribute(string? learningCommand, string? learningParameter)
            : base(() => new Completer(learningCommand, learningParameter))
        {

        }
        private class Completer : IArgumentCompleter
        {
            public Completer(string? learnFromCommand, string? learnFromParameter)
            {
                _learnFromCommand = learnFromCommand;
                _learnFromParameter = learnFromParameter;
            }
            private string? _learnFromCommand;
            private string? _learnFromParameter;
            public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
            {
                var wc = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);
                return LearnedCompletionData.GetLearnedCompletions()
                    .Where(lcd =>lcd.CommandName.Equals(_learnFromCommand ?? commandName, StringComparison.OrdinalIgnoreCase))
                    .Where(lcd => lcd.ParameterName.Equals(_learnFromParameter ?? parameterName, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(lcd => lcd.Completions)
                    .Where(c => wc.IsMatch(c))
                    .Select(c => CreateCompletionResult(c))
                    ;
            }
        }
    }
}
