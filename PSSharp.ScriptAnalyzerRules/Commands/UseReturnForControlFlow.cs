using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the return statement is followed by a value to be returned.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UseReturnForControlFlow))]
    public class UseReturnForControlFlow : ScriptAnalyzerCommand<ReturnStatementAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(ReturnStatementAst ast)
        {
            return ast.Pipeline != null;
        }
    }
}
