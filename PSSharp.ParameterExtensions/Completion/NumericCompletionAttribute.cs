using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

namespace PSSharp
{
    public class NumericCompletionAttribute : CompletionBaseAttribute
    {
        public NumericCompletionAttribute()
            : base (() => new Completer(int.MinValue, int.MaxValue, 1))
        {
        }
        public NumericCompletionAttribute(decimal min, decimal max)
            : base(() => new Completer(min, max, 1))
        {
        }
        public NumericCompletionAttribute(decimal min, decimal max, decimal increment)
            : base (() => new Completer(min, max, increment))
        {

        }

        private class Completer : IArgumentCompleter
        {
            private decimal _min;
            private decimal _max;
            private decimal _increment;
            public Completer(decimal min, decimal max, decimal increment)
            {
                _min = min;
                _max = max;
                _increment = increment;
            }
            /// <summary>
            /// Returns argument completion for numbers based on constructor parameters.
            /// </summary>
            public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
            {
                short suggested = 0;
                var wc = new WildcardPattern(wordToComplete + "*");
                decimal start = _min;
                if (decimal.TryParse(wordToComplete, out var dec))
                {
                    start = dec;
                }
                for(decimal i = start; i < _max; i += _increment)
                {
                    if (wc.IsMatch(wordToComplete))
                    {
                        yield return CreateCompletionResult(i.ToString());
                        suggested++;
                    }
                    if (suggested == short.MaxValue) yield break;
                }
            }
        }
    }
}
