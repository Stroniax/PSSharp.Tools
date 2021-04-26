using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if a function's name contains a number.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class AvoidNumberInFunctionName : ScriptAnalyzerRule<FunctionDefinitionAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(FunctionDefinitionAst ast)
        {
            return Regex.IsMatch(ast.Name, "[0-9]");
        }
    }
}
