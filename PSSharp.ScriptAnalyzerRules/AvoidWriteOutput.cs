using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails is the command is Write-Output.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class AvoidWriteOutput : ScriptAnalyzerRule<CommandAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(CommandAst ast)
        {
            return ast.CommandElements.Count > 0
                && ast.CommandElements[0] is StringConstantExpressionAst sceAst
                && sceAst.Value.Equals("Write-Output", StringComparison.OrdinalIgnoreCase);
        }
        /// <inheritdoc/>
        public override DiagnosticSeverity DiagnosticSeverity => DiagnosticSeverity.Warning;
    }
}
