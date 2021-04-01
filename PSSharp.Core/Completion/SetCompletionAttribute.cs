using PSSharp.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SetCompletionAttribute : CompletionBaseAttribute
    {
        public SetCompletionAttribute(params string[] set)
            : base(() => new Completer(set))
        {
        }
        private class Completer : IArgumentCompleter
        {
            private string[] _set;
            public Completer(string[] set)
            {
                _set = set;
            }
            
            public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, System.Collections.IDictionary fakeBoundParameters)
            {
                var wc = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);
                foreach (var item in _set)
                {
                    if (wc.IsMatch(item))
                    {
                        yield return CreateCompletionResult(item);
                    }
                }
            }
        }
    }
}
