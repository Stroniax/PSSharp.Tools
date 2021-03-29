using System;
using System.Collections;
using System.Reflection;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PSScriptMethodAttribute : PSTypeDataAttribute
    {
        public string MethodName { get; }
        public string Script { get; }
        public PSScriptMethodAttribute(string methodName, string script)
        {
            MethodName = methodName;
            Script = script;
        }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            // This method cannot provide all parameters. Some values are set in a PowerShell ScriptProperty
            // that overrides this method.
#warning requires PowerShell implementation
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "ScriptMethod";
            parameters["MemberName"] = MethodName;
            parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ExternalPSScriptMethodAttribute : PSScriptMethodAttribute
    {
        public ExternalPSScriptMethodAttribute(string appliesToTypeName, string methodName, string script)
            : base(methodName, script)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public ExternalPSScriptMethodAttribute(Type appliesToType, string methodName, string script)
            : base(methodName, script)
        {
            AppliesToTypeName = appliesToType.FullName;
        }
        public string AppliesToTypeName { get; }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
}
