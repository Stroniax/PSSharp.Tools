using System;
using System.Collections;
using System.Reflection;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class PSCodePropertyDefinitionAttribute : PSTypeDataAttribute
    {
        public PSCodePropertyDefinitionAttribute(string appliesToTypeName)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public PSCodePropertyDefinitionAttribute(Type appliesToType)
        {
            AppliesToTypeName = appliesToType.Name;
        }
        public string AppliesToTypeName { get; }
        public string? PropertyName { get; set; }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "CodeProperty";
            parameters["MemberName"] = PropertyName;
            parameters["TypeName"] = AppliesToTypeName;
            if (attributeAppliedTo is MethodInfo method)
            {
                if (!method.IsStatic) return;
                var methodParameters = method.GetParameters();
                if (methodParameters.Length == 0) return;
                if (methodParameters[0].ParameterType.FullName != "System.Management.Automation.PSObject") return;
                if (methodParameters.Length == 1)
                {
                    parameters["Value"] = method;
                }
                else if (methodParameters.Length == 2)
                {
                    parameters["SecondValue"] = method;
                }
            }
        }
    }
}
