using System;
using System.Collections;
using System.Reflection;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PSCodePropertyFromExtensionMethodAttribute : PSTypeDataAttribute
    {
        public string? PropertyName { get; set; }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            // This method cannot provide all parameters. Some values are set in a PowerShell ScriptProperty
            // that overrides this method.
#warning requires PowerShell implementation
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "CodeProperty";
            parameters["MemberName"] = PropertyName;
            if (attributeAppliedTo is MethodInfo method)
            {
                parameters["MemberName"] ??= method.Name;
                var methodParameters = method.GetParameters();
                if (methodParameters.Length == 0) return;
                parameters["TypeName"] = methodParameters[0].ParameterType.FullName;
            }
        }
    }
}
