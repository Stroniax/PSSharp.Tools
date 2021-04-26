using PSSharp.ScriptAnalyzerRules.Extensions;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if a variable within a function begins with a capital letter, unless that variable
    /// is a parameter of the function.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UseCamelCaseForVariable))]
    public class UseCamelCaseForVariable : ScriptAnalyzerCommand
    {
        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            if (Ast is FunctionDefinitionAst function)
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
                    WriteObject(CreateResultObject(violation));
                }
            }
        }
        /// <inheritdoc/>
        protected override bool Predicate(Ast ast) => throw new System.NotImplementedException();
    }
}
