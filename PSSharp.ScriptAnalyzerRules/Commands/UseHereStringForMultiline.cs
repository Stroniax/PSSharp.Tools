using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the <see cref="ExpressionAst"/> is a multi-line stirng that is not
    /// a here-string.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UseHereStringForMultiline))]
    public class UseHereStringForMultiline : ScriptAnalyzerCommand<ExpressionAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(ExpressionAst ast)
        {
            if (ast is StringConstantExpressionAst stringConstant)
            {
                return stringConstant.StringConstantType != StringConstantType.DoubleQuotedHereString
                && stringConstant.StringConstantType != StringConstantType.SingleQuotedHereString
                && stringConstant.Extent.Text.Contains("\n");
            }
            else if (ast is ExpandableStringExpressionAst expandableString)
            {
                return expandableString.StringConstantType != StringConstantType.DoubleQuotedHereString
                && expandableString.StringConstantType != StringConstantType.SingleQuotedHereString
                && expandableString.Extent.Text.Contains("\n");
            }
            return false;
        }
    }
}
