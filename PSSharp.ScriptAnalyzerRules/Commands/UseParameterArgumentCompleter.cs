using System;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// Fails if no argument completer attribute is assigned to the parameter and the
    /// parameter name does not end with the text "Path".
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, nameof(UseParameterArgumentCompleter))]
    public class UseParameterArgumentCompleter : ScriptAnalyzerCommand<ParameterAst>
    {
        /// <inheritdoc/>
        protected override bool Predicate(ParameterAst ast)
        {
            if (ast.Name.VariablePath.UserPath.EndsWith("Path", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            foreach (var attriubuteBase in ast.Attributes)
            {
                if (attriubuteBase is AttributeAst attribute)
                {
                    if (typeof(ArgumentCompleterAttribute).IsAssignableFrom(
                        attriubuteBase.TypeName.GetReflectionType()))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
