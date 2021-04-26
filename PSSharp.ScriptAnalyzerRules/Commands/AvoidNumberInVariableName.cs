using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if a variable's name contains a number.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(AvoidNumberInVariableName))]
    public class AvoidNumberInVariableName : ScriptAnalyzerCommand<VariableExpressionAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(VariableExpressionAst ast)
        {
            return Regex.IsMatch(ast.VariablePath.UserPath, "[0-9]");
        }
    }
}
