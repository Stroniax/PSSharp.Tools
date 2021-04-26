using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if more than 5 named parameters are provided for a command invocation.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UseParameterSplatting : ScriptAnalyzerRule<CommandAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(CommandAst ast)
        {
            return ast.CommandElements.Where(i => i is CommandParameterAst).Count() > 5;
        }
    }
}
