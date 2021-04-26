using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if a function's name contains a number.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(AvoidNumberInFunctionName))]
    public class AvoidNumberInFunctionName : ScriptAnalyzerCommand<FunctionDefinitionAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(FunctionDefinitionAst ast)
        {
            return Regex.IsMatch(ast.Name, "[0-9]");
        }
    }
}
