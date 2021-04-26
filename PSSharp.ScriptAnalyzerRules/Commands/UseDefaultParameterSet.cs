using System;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the param block does not define 
    /// <see cref="CmdletCommonMetadataAttribute.DefaultParameterSetName"/> if a parameter does define
    /// <see cref="ParameterAttribute.ParameterSetName"/>.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UseDefaultParameterSet))]
    public class UseDefaultParameterSet : ScriptAnalyzerCommand<ParamBlockAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(ParamBlockAst ast)
        {
            bool requiresDefaultParameterSet = false;
            foreach (var parameter in ast.Parameters)
            {
                foreach (var attributeBase in parameter.Attributes)
                {
                    if (attributeBase is AttributeAst attribute
                        && attributeBase.TypeName.GetReflectionType() == typeof(ParameterAttribute))
                    {
                        foreach (var namedArgument in attribute.NamedArguments)
                        {
                            if (namedArgument.ArgumentName.Equals(
                                nameof(ParameterAttribute.ParameterSetName),
                                StringComparison.OrdinalIgnoreCase))
                            {
                                requiresDefaultParameterSet = true;
                                break;
                            }
                        }
                    }
                }
            }
            if (requiresDefaultParameterSet)
            {
                foreach (var attribute in ast.Attributes)
                {
                    foreach (var namedArgument in attribute.NamedArguments)
                    {
                        if (namedArgument.ArgumentName.Equals(
                            nameof(CmdletBindingAttribute.DefaultParameterSetName),
                            StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <inheritdoc/>
        protected override DiagnosticSeverity Severity => DiagnosticSeverity.Error;
    }
}
