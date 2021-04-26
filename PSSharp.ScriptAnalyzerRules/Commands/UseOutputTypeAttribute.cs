using PSSharp.ScriptAnalyzerRules.Extensions;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if the function does not define the <see cref="OutputTypeAttribute"/>.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UseOutputTypeAttribute))]
    public class UseOutputTypeAttribute : ScriptAnalyzerCommand<FunctionDefinitionAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(FunctionDefinitionAst ast)
        {
            foreach (var paramBlock in ast.FindAll<ParamBlockAst>(false))
            {
                foreach (var attribute in paramBlock.Attributes)
                {
                    if (attribute.TypeName.GetReflectionType() == typeof(OutputTypeAttribute))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
