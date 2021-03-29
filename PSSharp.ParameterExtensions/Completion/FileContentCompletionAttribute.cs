using PSSharp.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class FileContentCompletionAttribute : ArgumentCompleterAttribute
    {
        public FileContentCompletionAttribute(string filePath)
            : base(GetScript(filePath))
        {
        }
        private static ScriptBlock GetScript(string filePath)
        {
            var scriptText = string.Format(Resources.FileContentArgumentCompleter, filePath?.Replace("\"", "\"\""));
            return ScriptBlock.Create(scriptText);
        }
    }
}
