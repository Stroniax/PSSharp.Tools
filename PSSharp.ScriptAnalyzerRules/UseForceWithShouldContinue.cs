using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using PSSharp.ScriptAnalyzerRules.Extensions;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if $PSCmdlet.ShouldContinue is called and no Force parameter exists.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UseForceWithShouldContinue : ScriptAnalyzerRule<FunctionDefinitionAst>
    {
        /// <inheritdoc/>
        public override IEnumerable<DiagnosticRecord> AnalyzeScript(Ast ast, string scriptPath)
        {
            if (ast is FunctionDefinitionAst function)
            {
                foreach (var parameter in function.Parameters)
                {
                    if (parameter.Name.VariablePath.UserPath.Equals("Force", StringComparison.OrdinalIgnoreCase))
                    {
                        yield break;
                    }
                }
                var violations = function.FindAll<InvokeMemberExpressionAst>(i =>
                {
                    return i.Expression is VariableExpressionAst veAst
                        && veAst.VariablePath.UserPath.Equals("PSCmdlet", StringComparison.OrdinalIgnoreCase)
                        && i.Member is StringConstantExpressionAst sceAst
                        && sceAst.Value.Equals("ShouldContinue", StringComparison.OrdinalIgnoreCase);
                }, true);
                foreach (var violation in violations)
                {
                    yield return CreateDiagnosticRecord(violation, scriptPath);
                }
            }
        }
        /// <inheritdoc/>
        protected override bool Predicate(FunctionDefinitionAst ast) => throw new NotImplementedException();
    }
}
