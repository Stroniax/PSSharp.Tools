using PSSharp.Properties;
using System;
using System.Management.Automation;

namespace PSSharp
{
    /// <summary>
    /// Argument completion for command and parameter names. Completes with parameter name when the parameter
    /// this attribute is completing is named "Parameter" or "ParameterName", and a "Command" or "CommandName" 
    /// parameter has a value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class CommandParameterCompletionAttribute : ArgumentCompleterAttribute
    {
        /// <summary>
        /// Argument completion for command and parameter names. Completes with parameter name when the parameter
        /// this attribute is completing is named "Parameter" or "ParameterName", and a "Command" or "CommandName" 
        /// parameter has a value.
        /// </summary>
        public CommandParameterCompletionAttribute()
            : base(ScriptBlock.Create(Resources.CommandParameterCompletion))
        {
        }
    }
}
