using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using PSSharp.ScriptAnalyzerRules.Extensions;
using System;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if no parameters accept pipeline input, unless the ParamBlock contains 
    /// an attribute of type PSSharp.NoPipelineInputAttribute.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public class UsePipelineInput : ScriptAnalyzerRule<ParamBlockAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(ParamBlockAst ast)
        {
            foreach (var possibleExclusion in ast.Attributes)
            {
                if (possibleExclusion.TypeName.GetReflectionType().FullName.Equals("PSSharp.NoPipelineInputAttribute", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            var parameters = ast.FindAll<ParameterAst>(false);
            foreach (var parameter in parameters)
            {
                foreach (var attributeBase in parameter.Attributes)
                {
                    if (attributeBase.TypeName.GetReflectionType() == typeof(ParameterAttribute)
                    && attributeBase is AttributeAst attribute)
                    {
                        foreach (var namedArgument in attribute.NamedArguments)
                        {
                            if (namedArgument.ArgumentName.Equals(
                                nameof(ParameterAttribute.ValueFromPipeline),
                                StringComparison.OrdinalIgnoreCase)
                            || namedArgument.ArgumentName.Equals(
                                nameof(ParameterAttribute.ValueFromPipelineByPropertyName),
                                StringComparison.OrdinalIgnoreCase))
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
