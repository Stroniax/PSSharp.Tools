using System;
using System.Collections;
using System.Reflection;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PSCodeMethodAttribute : PSTypeDataAttribute
    {
        public PSCodeMethodAttribute(string referencedTypeName, string referencedMethodName)
        {
            ReferencedTypeName = referencedTypeName;
            ReferencedMethodName = referencedMethodName;
        }
        public PSCodeMethodAttribute(Type referencedType, string referencedMethodName)
        {
            ReferencedTypeName = referencedType.FullName;
            ReferencedMethodName = referencedMethodName;
        }
        public string ReferencedTypeName { get; }
        public string ReferencedMethodName { get; }
        public string MethodName { get => _methodName ?? ReferencedMethodName; set => _methodName = value; }
        private string? _methodName;

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            // This method cannot provide all parameters. Some values are set in a PowerShell ScriptProperty
            // that overrides this method.
#warning requires PowerShell implementation
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "CodeMethod";
            parameters["MemberName"] = MethodName;
            parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ExternalPSCodeMethodAttribute : PSCodeMethodAttribute
    {
        public string AppliesToTypeName { get; }
        public ExternalPSCodeMethodAttribute(string appliesToTypeName, string referencedTypeName, string referencedMethodName)
            : base(referencedTypeName, referencedMethodName) => AppliesToTypeName = appliesToTypeName;
        public ExternalPSCodeMethodAttribute(string appliesToTypeName, Type referencedType, string referencedMethodName)
            : base(referencedType, referencedMethodName) => AppliesToTypeName = appliesToTypeName;
        public ExternalPSCodeMethodAttribute(Type appliesToType, string referencedTypeName, string referencedMethodName)
            : base(referencedTypeName, referencedMethodName) => AppliesToTypeName = appliesToType.FullName;
        public ExternalPSCodeMethodAttribute(Type appliesToType, Type referencedType, string referencedMethodName)
            : base(referencedType, referencedMethodName) => AppliesToTypeName = appliesToType.FullName;

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
}
