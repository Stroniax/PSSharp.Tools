using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public abstract class CompletionBaseAttribute : ArgumentCompleterAttribute
    {
        public CompletionBaseAttribute(ScriptBlock script, IDictionary<string, object?> parameters)
            : base(CreateClosure(script, parameters))
        {

        }
        public CompletionBaseAttribute(Func<IArgumentCompleter> completerConstructor)
            : base(CreateMethodReference(completerConstructor))
        {

        }
        private static ScriptBlock CreateClosure(ScriptBlock script, IDictionary<string, object?> parameters)
        {
            return (ScriptBlock)ScriptBlock.Create("" +
                "$psBuildCompletionParameters.GetEnumerator() | ForEach-Object { Set-Variable -Name $_.Key -Value $_.Value } ; " +
                "$psBuildCompletionScript.GetNewClosure()")
                // pass arguments as named variables to avoid conflict with $args[]
                .InvokeWithContext(null, new List<PSVariable>() { new PSVariable("psBuildCompletionScript", script), new PSVariable("psBuildCompletionParameters", parameters) })[0]
                .BaseObject;
        }
        private static ScriptBlock CreateMethodReference(Func<IArgumentCompleter> completerConstructor)
            //where T: class, IArgumentCompleter
        {
            return (ScriptBlock)ScriptBlock.Create("" +
                "{" +
                "   $argumentCompleter = $psBuildCompletionConstructor.Invoke();" +
                "   $argumentCompleter.CompleteArgument($args[0], $args[1], $args[2], $args[3], $args[4])" +
                "}.GetNewClosure()")
                // pass arguments as named variables to avoid conflict with $args[]
                .InvokeWithContext(null, new List<PSVariable>() { new PSVariable("psBuildCompletionConstructor", completerConstructor) })[0]
                .BaseObject;
        }

        protected static CompletionResult CreateCompletionResult(string value, bool escapeVariables = true)
        {
            if (value.Contains("'") || value.Contains(" ") || (escapeVariables && value.Contains("$")))
            {
                var val = CodeGeneration.EscapeSingleQuotedStringContent(value);
                if (escapeVariables) val = CodeGeneration.EscapeVariableName(val);
                return new CompletionResult("'" + val + "'", value, CompletionResultType.ParameterValue, value);
            }
            else
            {
                return new CompletionResult(value, value, CompletionResultType.ParameterValue, value);
            }
        }
    }
}
