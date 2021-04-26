using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using PSSharp.ScriptAnalyzerRules.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails for any <see cref="VariableExpressionAst"/> that is not within a 
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class AvoidUsingUndefinedVariable : ScriptAnalyzerRule<ScriptBlockAst>
    {
        /// <inheritdoc/>
        public override IEnumerable<DiagnosticRecord> AnalyzeScript(Ast ast, string fileName)
        {
            if (ast.Parent != null) yield break;
            var allVariables = ast.FindAll<VariableExpressionAst>(true);
            var assignedVariables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var variable in allVariables)
            {
                if (assignedVariables.ContainsKey(variable.VariablePath.UserPath))
                {
                    goto next_variable;
                }
                foreach (var assignment in variable.FindAllParents<AssignmentStatementAst>())
                {
                    assignedVariables.Add(variable.VariablePath.UserPath, null);
                    goto next_variable;
                }

                yield return CreateDiagnosticRecord(variable);
            next_variable:;
            }
        }
        /// <inheritdoc/>
        protected override bool Predicate(ScriptBlockAst ast) => throw new NotImplementedException();
        /// <inheritdoc/>
        public override DiagnosticSeverity DiagnosticSeverity => DiagnosticSeverity.Error;
    }
}
