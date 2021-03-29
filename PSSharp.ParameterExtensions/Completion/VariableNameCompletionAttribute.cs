using PSSharp.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class VariableNameCompletionAttribute : ArgumentCompleterAttribute
    {
        public VariableNameCompletionAttribute(bool completeDefinition = false)
            : base(GetScript(completeDefinition))
        {
        }
        private static ScriptBlock GetScript(bool completeDefinition)
        {
            var scriptText = string.Format(Resources.VariableNameCompletion, $"${completeDefinition}");
            return ScriptBlock.Create(scriptText);
        }
    }
}
