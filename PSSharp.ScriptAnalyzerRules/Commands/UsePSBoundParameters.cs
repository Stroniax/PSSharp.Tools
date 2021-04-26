using System;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if $MyInvocation.BoundParameters is called instead of $PSBoundParameters.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UsePSBoundParameters))]
    public class UsePSBoundParameters : ScriptAnalyzerCommand<MemberExpressionAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(MemberExpressionAst ast)
        {
            return ast.Expression is VariableExpressionAst veAst
                        && veAst.VariablePath.UserPath.Equals("MyInvocation", StringComparison.OrdinalIgnoreCase)
                    && ast.Member is StringConstantExpressionAst sceAst
                        && sceAst.Value.Equals("BoundParameters", StringComparison.OrdinalIgnoreCase);
        }
    }
}
