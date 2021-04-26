using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the return statement is followed by a value to be returned.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UseReturnForControlFlow : ScriptAnalyzerRule<ReturnStatementAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(ReturnStatementAst ast)
        {
            return ast.Pipeline != null;
        }
    }
}
