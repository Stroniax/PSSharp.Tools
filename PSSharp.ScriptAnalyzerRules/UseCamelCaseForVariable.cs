using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using PSSharp.ScriptAnalyzerRules.Extensions;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if a variable within a function begins with a capital letter, unless that variable
    /// is a parameter of the function.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UseCamelCaseForVariable : ScriptAnalyzerRule
    {
        /// <inheritdoc/>
        public override IEnumerable<DiagnosticRecord> AnalyzeScript(Ast ast, string scriptPath)
        {
            if (ast is FunctionDefinitionAst function)
            {
                var parameterNames = new List<string>();
                var parameters = function.FindAll<ParameterAst>(false);
                foreach (var parameter in parameters)
                {
                    parameterNames.Add(parameter.Name.VariablePath.UserPath);
                }
                var violations = function.FindAll<VariableExpressionAst>(i =>
                    !parameterNames.Contains(i.VariablePath.UserPath)
                    && Regex.IsMatch(i.VariablePath.UserPath, "^[A-Z]"),
                    true);
                foreach (var violation in violations)
                {
                    yield return CreateDiagnosticRecord(violation, scriptPath);
                }
            }
        }
        /// <inheritdoc/>
        protected override bool Predicate(Ast ast) => throw new System.NotImplementedException();
    }
}
