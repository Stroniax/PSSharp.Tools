using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Indicates that no argument completion should be offered. This may be preferred
    /// when ideal argument completion is not possible and a path argument completion
    /// is not desired.
    /// </summary>
    public class NoCompletionAttribute : ArgumentCompleterAttribute, IArgumentCompleter
    {
        /// <summary>
        /// Indicates that no argument completion should be offered.
        /// </summary>
        public NoCompletionAttribute()
            : base(typeof(NoCompletionAttribute))
        {
        }
        /// <summary>
        /// Returns nothing.
        /// </summary>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            yield break;
        }
    }
}
