using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the string is a double-quoted or not quoted, and does not contain
    /// special characters or expandable expressions.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UseSingleQuotedStringWhenNotInterpolated : ScriptAnalyzerRule<StringConstantExpressionAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(StringConstantExpressionAst ast)
        {
            if (ast.StringConstantType != StringConstantType.SingleQuoted
                && ast.StringConstantType != StringConstantType.SingleQuotedHereString)
            {
                if (ast.Value == ast.Extent.Text)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
