using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;

namespace PSSharp
{
    /// <summary>
    /// Completes the names of constants defined within a given type, particularly for use in conjunction
    /// with <see cref="ConstantDefinition{TSource}"/> parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ConstantValueCompletionAttribute : CompletionBaseAttribute
    {
        /// <inheritdoc cref="ConstantValueCompletionAttribute"/>
        /// <param name="definesConstants">The type that defines the constants to offer as completion.</param>
        /// <param name="includeInherited">Determines if constants defined by parent types of 
        /// <paramref name="definesConstants"/> should be included in the result set.</param>
        public ConstantValueCompletionAttribute(Type definesConstants, bool includeInherited = false)
            : base(() => new Completer(definesConstants, includeInherited))
        {
        }
        private class Completer : IArgumentCompleter
        {
            private readonly Type _type;
            private readonly bool _includeInherited;
            public Completer(Type type, bool includeInherited = false)
            {
                _type = type;
                _includeInherited = includeInherited;
            }
            public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
            {
                var values = ConstantDefinition.GetValues(_type, _includeInherited);
                foreach (var val in values)
                {
                    yield return CreateCompletionResult(val.Name);
                }
            }
        }
    }
}
