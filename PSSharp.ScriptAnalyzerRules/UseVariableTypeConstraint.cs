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
    /// Fails for any variable assignment that is not explicitly typed.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UseVariableTypeConstraint : ScriptAnalyzerRule<ScriptBlockAst>
    {
        /// <inheritdoc/>
        public override IEnumerable<DiagnosticRecord> AnalyzeScript(Ast ast, string scriptPath)
        {
            if (ast.Parent != null) yield break;

            var assignments = ast.FindAll<AssignmentStatementAst>(true);
            var assignedVariables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var assignment in assignments)
            {
                var variableExpressions = assignment.Left.FindAll<VariableExpressionAst>(false);
                var typeConstraints = assignment.Left.FindAll<TypeConstraintAst>(false);
                bool hasVariableExpression = false;
                bool hasTypeConstraint = false;
                bool isPreviouslyDefined = true;
                foreach (var variable in variableExpressions)
                {
                    hasVariableExpression = true;
                    if (!assignedVariables.ContainsKey(variable.VariablePath.UserPath))
                    {
                        assignedVariables.Add(variable.VariablePath.UserPath, null);
                        isPreviouslyDefined = false;
                    }
                }
                if (isPreviouslyDefined || !hasVariableExpression)
                {
                    continue;
                }
                foreach (var typeConstraint in typeConstraints)
                {
                    hasTypeConstraint = true;
                }
                if (!hasTypeConstraint)
                {
                    yield return CreateDiagnosticRecord(assignment.Left, scriptPath);
                }
            }
        }
        /// <inheritdoc/>
        protected override bool Predicate(ScriptBlockAst ast) => throw new NotImplementedException();
    }
}
