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
    public class SetCompletionAttribute : ArgumentCompleterAttribute
    {
        public SetCompletionAttribute(params string[] set)
            : base(GetScript(set))
        {
        }
        private static ScriptBlock GetScript(string[] set)
        {
            var escapedSet = set.Select(i => "'" + CodeGeneration.EscapeSingleQuotedStringContent(i) + "'");
            var scriptText = string.Format(Resources.SetCompletion, string.Join(", ", escapedSet));
            return ScriptBlock.Create(scriptText);
        }
    }
}
