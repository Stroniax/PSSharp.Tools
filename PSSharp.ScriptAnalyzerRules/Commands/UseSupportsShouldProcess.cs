using PSSharp.ScriptAnalyzerRules.Extensions;
using System;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the function calls $PSCmdlet.ShouldProcess or $PSCmdlet.ShouldContinue
    /// without defining <see cref="CmdletCommonMetadataAttribute.SupportsShouldProcess"/>.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UseSupportsShouldProcess))]
    public class UseSupportsShouldProcess : ScriptAnalyzerCommand<FunctionDefinitionAst>
    {
        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            bool supportsShouldProcess = false;
            foreach (var attribute in Ast.Body.ParamBlock.Attributes)
            {
                if (attribute.TypeName.GetReflectionType() == typeof(CmdletBindingAttribute))
                {
                    foreach (var namedArgument in attribute.NamedArguments)
                    {
                        if (namedArgument.ArgumentName.Equals(
                            nameof(CmdletBindingAttribute.SupportsShouldProcess),
                            StringComparison.OrdinalIgnoreCase))
                        {
                            supportsShouldProcess = namedArgument.ExpressionOmitted
                                || namedArgument.Argument is VariableExpressionAst veAst
                                    && veAst.VariablePath.UserPath.Equals("true", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            if (!supportsShouldProcess) return;
            var invocationMemberExpressions = Ast.FindAll((InvokeMemberExpressionAst i) =>
                i.Expression is VariableExpressionAst veAst
                && veAst.VariablePath.UserPath.Equals("PSCmdlet", StringComparison.OrdinalIgnoreCase)
                && i.Member is StringConstantExpressionAst sceAst
                && (sceAst.Value.Equals("ShouldProcess", StringComparison.OrdinalIgnoreCase)
                || sceAst.Value.Equals("ShouldConfirm", StringComparison.OrdinalIgnoreCase)), true);
            foreach (var invocation in invocationMemberExpressions)
            {
                WriteObject(CreateResultObject(invocation));
            }
        }
        /// <inheritdoc/>
        protected override bool Predicate(FunctionDefinitionAst ast) => throw new NotImplementedException();
        /// <inheritdoc/>
        protected override DiagnosticSeverity Severity => DiagnosticSeverity.Error;
    }
}
