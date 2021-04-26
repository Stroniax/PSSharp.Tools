using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using PSSharp.ScriptAnalyzerRules.Extensions;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the throw statement exists within a function.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class AvoidThrowStatement : ScriptAnalyzerRule<ThrowStatementAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(ThrowStatementAst ast)
        {
            foreach (var _ in ast.FindAllParents<FunctionDefinitionAst>())
            {
                return true;
            }
            return false;
        }
    }
}
