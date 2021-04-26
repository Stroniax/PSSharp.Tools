using System;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails is the command is Write-Output.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(AvoidWriteOutput))]
    public class AvoidWriteOutput : ScriptAnalyzerCommand<CommandAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(CommandAst ast)
        {
            return ast.CommandElements.Count > 0
                && ast.CommandElements[0] is StringConstantExpressionAst sceAst
                && sceAst.Value.Equals("Write-Output", StringComparison.OrdinalIgnoreCase);
        }
        /// <inheritdoc/>
        protected override DiagnosticSeverity Severity => DiagnosticSeverity.Warning;
    }
}
