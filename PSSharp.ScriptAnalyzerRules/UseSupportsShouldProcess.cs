using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using PSSharp.ScriptAnalyzerRules.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the function calls $PSCmdlet.ShouldProcess or $PSCmdlet.ShouldContinue
    /// without defining <see cref="CmdletCommonMetadataAttribute.SupportsShouldProcess"/>.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UseSupportsShouldProcess : ScriptAnalyzerRule<FunctionDefinitionAst>
    {
        /// <inheritdoc/>
        public override IEnumerable<DiagnosticRecord> AnalyzeScript(Ast ast, string? scriptPath)
        {
            if (ast is FunctionDefinitionAst function)
            {
                foreach (var attribute in function.Body.ParamBlock.Attributes)
                {
                    if (attribute.TypeName.GetReflectionType() == typeof(CmdletBindingAttribute))
                    {
                        foreach (var namedArgument in attribute.NamedArguments)
                        {
                            if (namedArgument.ArgumentName.Equals(
                                nameof(CmdletBindingAttribute.SupportsShouldProcess),
                                StringComparison.OrdinalIgnoreCase))
                            {
                                if (namedArgument.ExpressionOmitted
                                    || namedArgument.Argument is VariableExpressionAst veAst
                                        && veAst.VariablePath.UserPath.Equals("true", StringComparison.OrdinalIgnoreCase))
                                {
                                    yield break;
                                }
                            }
                        }
                    }
                }
                var invocationMemberExpressions = function.FindAll((InvokeMemberExpressionAst i) =>
                    i.Expression is VariableExpressionAst veAst
                    && veAst.VariablePath.UserPath.Equals("PSCmdlet", StringComparison.OrdinalIgnoreCase)
                    && i.Member is StringConstantExpressionAst sceAst
                    && (sceAst.Value.Equals("ShouldProcess", StringComparison.OrdinalIgnoreCase)
                    || sceAst.Value.Equals("ShouldConfirm", StringComparison.OrdinalIgnoreCase)), true);
                foreach (var invocation in invocationMemberExpressions)
                {
                    yield return CreateDiagnosticRecord(invocation, scriptPath);
                }
            }
        }
        /// <inheritdoc/>
        protected override bool Predicate(FunctionDefinitionAst ast) => throw new NotImplementedException();
        /// <inheritdoc/>
        public override DiagnosticSeverity DiagnosticSeverity => DiagnosticSeverity.Error;
    }
}
