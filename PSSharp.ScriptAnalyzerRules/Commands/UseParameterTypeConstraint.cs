using PSSharp.ScriptAnalyzerRules.Extensions;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if a parameter is not explicitly constrainted to a type.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UseParameterTypeConstraint))]
    public class UseParameterTypeConstraint : ScriptAnalyzerCommand<ParameterAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(ParameterAst ast)
        {
            foreach (var typeConstraint in ast.FindAll<TypeConstraintAst>(false))
            {
                return true;
            }
            return false;
        }
    }
}
