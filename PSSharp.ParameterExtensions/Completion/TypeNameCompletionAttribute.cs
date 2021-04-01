using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp
{
    /// <summary>
    /// Offers the names of types loaded into the session for argument completion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TypeNameCompletionAttribute : ArgumentCompleterAttribute, IArgumentCompleter
    {
        /// <summary>
        /// Offers the names of types loaded into the session for argument completion.
        /// </summary>
        public TypeNameCompletionAttribute() : base(typeof(TypeNameCompletionAttribute))
        {
        }
        /// <summary>
        /// Offers the names of types loaded into the session for argument completion.
        /// </summary>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            var wc = new WildcardPattern(wordToComplete?.Trim("\"'()[]".ToCharArray()) + "*", WildcardOptions.IgnoreCase);
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => t.IsPublic).Select(t => (t.FullName, t.Name)).Where(n => wc.IsMatch(n.Name) || wc.IsMatch(n.FullName));
            foreach (var (FullName, Name) in types)
            {
                string safeOutput = FullName.Contains(" ") ? "'" + CodeGeneration.EscapeSingleQuotedStringContent(FullName) + "'" : FullName;
                yield return new CompletionResult(safeOutput, FullName, CompletionResultType.Type, FullName);
            }
        }
    }
}
