using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using PSSharp.ScriptAnalyzerRules.Extensions;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if a parameter is not explicitly constrainted to a type.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UseParameterTypeConstraint : ScriptAnalyzerRule<ParameterAst>
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
