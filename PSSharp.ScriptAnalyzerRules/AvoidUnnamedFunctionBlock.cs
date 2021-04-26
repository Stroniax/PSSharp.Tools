using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using PSSharp.ScriptAnalyzerRules.Extensions;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the <see cref="NamedBlockAst"/> of the <see cref="FunctionDefinitionAst"/> is unnamed.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class AvoidUnnamedFunctionBlock : ScriptAnalyzerRule<FunctionDefinitionAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(FunctionDefinitionAst ast)
        {
            foreach (var namedBlock in ast.FindAll<NamedBlockAst>(false))
            {
                if (namedBlock.Unnamed) return true;
            }
            return false;
        }
    }
}
