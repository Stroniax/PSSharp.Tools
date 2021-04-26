using PSSharp.ScriptAnalyzerRules.Extensions;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the throw statement exists within a function.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(AvoidThrowStatement))]
    public class AvoidThrowStatement : ScriptAnalyzerCommand<ThrowStatementAst>
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
