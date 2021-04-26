using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the first <see cref="CommandAst.CommandElements"/> is not a <see cref="CommandParameterAst"/>.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UsePositionalParameter))]
    public class UsePositionalParameter : ScriptAnalyzerCommand<CommandAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(CommandAst ast)
        {
            if (ast.CommandElements.Count < 2) return false;
            return !(ast.CommandElements[1] is CommandParameterAst);
        }
    }
}
