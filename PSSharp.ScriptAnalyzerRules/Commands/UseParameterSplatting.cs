using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if more than 7 parameter elements are provided for a command invocation.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UseParameterSplatting))]
    public class UseParameterSplatting : ScriptAnalyzerCommand<CommandAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(CommandAst ast)
        {
            return ast.CommandElements.Count > 8;
        }
    }
}
