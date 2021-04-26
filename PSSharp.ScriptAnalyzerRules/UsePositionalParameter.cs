using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the first <see cref="CommandAst.CommandElements"/> is not a <see cref="CommandParameterAst"/>.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UsePositionalParameter : ScriptAnalyzerRule<CommandAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(CommandAst ast)
        {
            if (ast.CommandElements.Count < 2) return false;
            return !(ast.CommandElements[1] is CommandParameterAst);
        }
    }
}
