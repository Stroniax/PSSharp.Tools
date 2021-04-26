using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if a variable's name contains a number.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class AvoidNumberInVariableName : ScriptAnalyzerRule<VariableExpressionAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(VariableExpressionAst ast)
        {
            return Regex.IsMatch(ast.VariablePath.UserPath, "[0-9]");
        }
    }
}
