using System;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the command is Write-Error.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(AvoidWriteError))]
    public class AvoidWriteError : ScriptAnalyzerCommand<CommandAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(CommandAst ast)
        {
            return ast.CommandElements.Count > 0
                && ast.CommandElements[0] is StringConstantExpressionAst sceAst
                && sceAst.Value.Equals("Write-Error", StringComparison.OrdinalIgnoreCase);
        }
        /// <inheritdoc/>
        protected override DiagnosticSeverity Severity => DiagnosticSeverity.Warning;
    }
}
