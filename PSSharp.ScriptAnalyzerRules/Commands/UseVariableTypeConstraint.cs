using PSSharp.ScriptAnalyzerRules.Extensions;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails for any variable assignment that is not explicitly typed.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UseVariableTypeConstraint))]
    public class UseVariableTypeConstraint : ScriptAnalyzerCommand<ScriptBlockAst>
    {
        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            var parent = Ast.GetTopParent();
            var assignments = parent.FindAll<AssignmentStatementAst>(true);
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
                    WriteObject(CreateResultObject(assignment.Left));
                }
            }
        }
        /// <inheritdoc/>
        protected override bool Predicate(ScriptBlockAst ast) => throw new NotImplementedException();
    }
}
