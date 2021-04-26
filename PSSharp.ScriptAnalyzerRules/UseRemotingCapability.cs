using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the RemotingCapability argument is not named.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UseRemotingCapability : ScriptAnalyzerRule<AttributeAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(AttributeAst ast)
        {
            foreach (var namedArgument in ast.NamedArguments)
            {
                if (namedArgument.ArgumentName.Equals(
                    nameof(CmdletBindingAttribute.RemotingCapability),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
