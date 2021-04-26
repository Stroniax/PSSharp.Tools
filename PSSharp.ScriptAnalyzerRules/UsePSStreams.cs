using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using PSSharp.ScriptAnalyzerRules.Extensions;
using System;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the function does not use Write-Verbose, Write-Information, or Write-Debug.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UsePSStreams : ScriptAnalyzerRule<FunctionDefinitionAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(FunctionDefinitionAst ast)
        {
            var asts = ast.FindAll<CommandAst>(i =>
                i.CommandElements.Count > 0
                && i.CommandElements[0] is StringConstantExpressionAst sceAst
                && (
                    sceAst.Value.Equals("Write-Verbose", StringComparison.OrdinalIgnoreCase)
                    || sceAst.Value.Equals("Write-Information", StringComparison.OrdinalIgnoreCase)
                    || sceAst.Value.Equals("Write-Debug", StringComparison.OrdinalIgnoreCase)
                ), true);
            foreach (var i in asts)
            {
                return false;
            }
            return true;
        }
    }
}
