using System;
using System.Collections;
using System.Reflection;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PSCodePropertyAttribute : PSTypeDataAttribute
    {
        public PSCodePropertyAttribute(string propertyName, Type referencedGetType, string referencedGetMethodName)
            : this(propertyName, referencedGetType.FullName, referencedGetMethodName, null, null)
        {

        }
        public PSCodePropertyAttribute(string propertyName, string referencedGetTypeFullName, string referencedGetMethodName)
            : this(propertyName, referencedGetTypeFullName, referencedGetMethodName, null, null)
        {

        }
        public PSCodePropertyAttribute(string propertyName, Type? referencedGetType, string? referencedGetMethodName, Type? referencedSetType, string? referencedSetMethodName)
            : this(propertyName, referencedGetType?.FullName, referencedGetMethodName, referencedSetType?.FullName, referencedSetMethodName)
        {

        }
        public PSCodePropertyAttribute(
            string propertyName,
            string? referencedGetTypeFullName,
            string? referencedGetMethodName,
            string? referencedSetTypeFullName,
            string? referencedSetMethodName)
        {
            PropertyName = propertyName;
            ReferencedGetTypeName = referencedGetTypeFullName;
            ReferencedGetMethodName = referencedGetMethodName;
            ReferencedSetTypeName = referencedSetTypeFullName;
            ReferencedSetMethodName = referencedSetMethodName;
        }
        public string PropertyName { get; }
        public string? ReferencedGetTypeName { get; }
        public string? ReferencedGetMethodName { get; }
        public string? ReferencedSetTypeName { get; }
        public string? ReferencedSetMethodName { get; }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            // This method cannot provide all parameters. Some values are set in a PowerShell ScriptProperty
            // that overrides this method.
#warning requires PowerShell implementation
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "CodeProperty";
            parameters["MemberName"] = PropertyName;
            parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
        }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ExternalPSCodePropertyAttribute : PSCodePropertyAttribute
    {
        public string AppliesToTypeName { get; }
        public ExternalPSCodePropertyAttribute(string appliesToTypeName, string propertyName, Type referencedGetType, string referencedGetMethodName)
            : base(propertyName, referencedGetType.FullName, referencedGetMethodName)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public ExternalPSCodePropertyAttribute(string appliesToTypeName, string propertyName, string referencedGetTypeFullName, string referencedGetMethodName)
            : base(propertyName, referencedGetTypeFullName, referencedGetMethodName)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public ExternalPSCodePropertyAttribute(string appliesToTypeName, string propertyName, Type? referencedGetType, string? referencedGetMethodName, Type? referencedSetType, string? referencedSetMethodName)
            : base(propertyName, referencedGetType?.FullName, referencedGetMethodName, referencedSetType?.FullName, referencedSetMethodName)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public ExternalPSCodePropertyAttribute(string appliesToTypeName, string propertyName, string? referencedGetTypeFullName, string? referencedGetMethodName, string? referencedSetTypeFullName, string? referencedSetMethodName)
            : base(propertyName, referencedGetTypeFullName, referencedGetMethodName, referencedSetTypeFullName, referencedSetMethodName)
        {
            AppliesToTypeName = appliesToTypeName;
        }

        public ExternalPSCodePropertyAttribute(Type appliesToType, string propertyName, Type referencedGetType, string referencedGetMethodName)
            : base(propertyName, referencedGetType.FullName, referencedGetMethodName)
        {
            AppliesToTypeName = appliesToType.FullName;
        }
        public ExternalPSCodePropertyAttribute(Type appliesToType, string propertyName, string referencedGetTypeFullName, string referencedGetMethodName)
            : base(propertyName, referencedGetTypeFullName, referencedGetMethodName)
        {
            AppliesToTypeName = appliesToType.FullName;
        }
        public ExternalPSCodePropertyAttribute(Type appliesToType, string propertyName, Type? referencedGetType, string? referencedGetMethodName, Type? referencedSetType, string? referencedSetMethodName)
            : base(propertyName, referencedGetType?.FullName, referencedGetMethodName, referencedSetType?.FullName, referencedSetMethodName)
        {
            AppliesToTypeName = appliesToType.FullName;
        }
        public ExternalPSCodePropertyAttribute(Type appliesToType, string propertyName, string? referencedGetTypeFullName, string? referencedGetMethodName, string? referencedSetTypeFullName, string? referencedSetMethodName)
            : base(propertyName, referencedGetTypeFullName, referencedGetMethodName, referencedSetTypeFullName, referencedSetMethodName)
        {
            AppliesToTypeName = appliesToType.FullName;
        }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
}
