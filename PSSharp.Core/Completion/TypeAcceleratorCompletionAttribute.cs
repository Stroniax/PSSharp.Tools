using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;

namespace PSSharp
{
    internal class TypeAcceleratorCompletionAttribute : ArgumentCompleterAttribute, IArgumentCompleter
    {
        static TypeAcceleratorCompletionAttribute()
        {
            var typeAccelerators = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
            GetTypeAccelerators = typeAccelerators.GetProperty("Get");
        }
        private static PropertyInfo GetTypeAccelerators { get; }

        public TypeAcceleratorCompletionAttribute()
            :base (typeof(TypeAcceleratorCompletionAttribute))
        {

        }

        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            var values = (Dictionary<string, Type>)GetTypeAccelerators.GetValue(null);
            var wc = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);
            foreach (var key in values.Keys)
            {
                if (wc.IsMatch(key))
                {
                    yield return new CompletionResult(key, key, CompletionResultType.ParameterValue, key);
                }
            }
        }
    }
}
