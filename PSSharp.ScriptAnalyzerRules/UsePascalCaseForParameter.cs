using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if a parameter begins with a lower case letter.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UsePascalCaseForParameter : ScriptAnalyzerRule<ParameterAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(ParameterAst ast)
        {
            return Regex.IsMatch(ast.Name.VariablePath.UserPath, "^[a-z]");
        }
    }
}
