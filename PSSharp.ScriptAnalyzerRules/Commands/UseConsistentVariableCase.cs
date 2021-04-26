using PSSharp.ScriptAnalyzerRules.Extensions;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails for variables with casing that does not match the case of the variable's
    /// initial definition.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UseConsistentVariableCase))]
    public class UseConsistentVariableCase : ScriptAnalyzerCommand<ScriptBlockAst>
    {
        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            var variables = Ast.GetTopParent().FindAll<VariableExpressionAst>(true);
            var variableNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var variable in variables)
            {
                if (variableNames.TryGetValue(variable.VariablePath.UserPath, out var caseSensitive))
                {
                    if (caseSensitive != variable.VariablePath.UserPath)
                    {
                        WriteObject(CreateResultObject(variable));
                    }
                }
            }
        }
        /// <inheritdoc/>
        protected override bool Predicate(ScriptBlockAst ast) => throw new NotImplementedException();
    }
}
