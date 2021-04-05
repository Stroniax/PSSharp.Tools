using System;
using System.Collections;
using System.Reflection;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public class PSScriptPropertyAttribute : PSTypeDataAttribute
    {
        public PSScriptPropertyAttribute(string propertyName, string? getScript)
            : this(propertyName, getScript, null)
        {
        }
        public PSScriptPropertyAttribute(string propertyName, string? getScript, string? setScript)
        {
            PropertyName = propertyName;
            GetScript = getScript;
            SetScript = setScript;
        }
        public string PropertyName { get; }
        public string? GetScript { get; }
        public string? SetScript { get; }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            // This method cannot provide all parameters. Some values are set in a PowerShell ScriptProperty
            // that overrides this method.
#warning requires PowerShell implementation
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "ScriptProperty";
            parameters["MemberName"] = PropertyName;
            parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
        }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ExternalPSScriptPropertyAttribute : PSScriptPropertyAttribute
    {
        public string AppliesToTypeName { get; }
        public ExternalPSScriptPropertyAttribute(Type appliesToType, string propertyName, string? getScript)
            : this(appliesToType.FullName, propertyName, getScript, null)
        {
        }
        public ExternalPSScriptPropertyAttribute(Type appliesToType, string propertyName, string? getScript, string? setScript)
            : this(appliesToType.FullName, propertyName, getScript, setScript)
        {
        }
        public ExternalPSScriptPropertyAttribute(string appliesToTypeName, string propertyName, string? getScript)
            : this(appliesToTypeName, propertyName, getScript, null)
        {
        }
        public ExternalPSScriptPropertyAttribute(string appliesToTypeName, string propertyName, string? getScript, string? setScript)
            : base(propertyName, getScript, setScript)
        {
            AppliesToTypeName = appliesToTypeName;
        }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
}
