using System;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the parameter has a default value assigned but no <see cref="PSDefaultValueAttribute"/>.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UsePSDefaultValueAttribute))]
    public class UsePSDefaultValueAttribute : ScriptAnalyzerCommand<ParameterAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(ParameterAst ast)
        {
            if (ast.DefaultValue is null)
            {
                return false;
            }
            else
            {
                // confirm that the PSDefaultValue attribute is present
                foreach (var attributeBase in ast.Attributes)
                {
                    if (attributeBase.TypeName.GetReflectionType() == typeof(PSDefaultValueAttribute))
                    {
                        if (attributeBase is AttributeAst attribute)
                        {
                            foreach (var namedArgument in attribute.NamedArguments)
                            {
                                if (namedArgument.ArgumentName?.Equals(nameof(PSDefaultValueAttribute.Value),
                                    StringComparison.OrdinalIgnoreCase) ?? false)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }
        }
    }
}
