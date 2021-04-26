using PSSharp.ScriptAnalyzerRules.Extensions;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails for any <see cref="VariableExpressionAst"/> that is not within a 
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(AvoidUsingUndefinedVariable))]
    public class AvoidUsingUndefinedVariable : ScriptAnalyzerCommand<ScriptBlockAst>
    {
        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            var parent = Ast.GetTopParent();
            var allVariables = parent.FindAll<VariableExpressionAst>(true);
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

                WriteObject(CreateResultObject(variable));
            next_variable:;
            }
        }
        /// <inheritdoc/>
        protected override bool Predicate(ScriptBlockAst ast) => throw new NotImplementedException();
        /// <inheritdoc/>
        protected override DiagnosticSeverity Severity => DiagnosticSeverity.Error;
    }
}
