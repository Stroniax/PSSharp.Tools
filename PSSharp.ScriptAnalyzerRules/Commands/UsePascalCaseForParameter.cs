using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if a parameter begins with a lower case letter.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UsePascalCaseForParameter))]
    public class UsePascalCaseForParameter : ScriptAnalyzerCommand<ParameterAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(ParameterAst ast)
        {
            return Regex.IsMatch(ast.Name.VariablePath.UserPath, "^[a-z]");
        }
    }
}
