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
    /// Fails for variables with casing that does not match the case of the variable's
    /// initial definition.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UseConsistentVariableCase : ScriptAnalyzerRule<ScriptBlockAst>
    {
        /// <inheritdoc/>
        public override IEnumerable<DiagnosticRecord> AnalyzeScript(Ast ast, string scriptPath)
        {
            if (ast.Parent != null) yield break;

            var variables = ast.FindAll<VariableExpressionAst>(true);
            var variableNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var variable in variables)
            {
                if (variableNames.TryGetValue(variable.VariablePath.UserPath, out var caseSensitive))
                {
                    if (caseSensitive != variable.VariablePath.UserPath)
                    {
                        yield return CreateDiagnosticRecord(variable, scriptPath);
                    }
                }
            }
        }
        /// <inheritdoc/>
        protected override bool Predicate(ScriptBlockAst ast) => throw new NotImplementedException();
    }
}
