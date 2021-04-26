using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if $MyInvocation.BoundParameters is called instead of $PSBoundParameters.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UsePSBoundParameters : ScriptAnalyzerRule<MemberExpressionAst>
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
