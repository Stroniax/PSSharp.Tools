using System;
using System.Collections;
using System.Reflection;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public class PSAliasPropertyAttribute : PSTypeDataAttribute
    {
        public PSAliasPropertyAttribute(string alias)
        {
            Alias = alias;
        }
        public string Alias { get; }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "AliasProperty";
            parameters["MemberName"] = Alias;
            parameters["Value"] = (attributeAppliedTo as MemberInfo)?.Name;
            parameters["TypeName"] = (attributeAppliedTo as MemberInfo)?.DeclaringType;
        }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ExternalPSAliasPropertyAttribute : PSAliasPropertyAttribute
    {
        public ExternalPSAliasPropertyAttribute(Type appliesToType, string referencedPropertyName, string alias)
            : base(alias)
        {
            AppliesToTypeName = appliesToType.FullName;
            ReferencedPropertyName = referencedPropertyName;
        }
        public ExternalPSAliasPropertyAttribute(string appliesToTypeName, string referencedPropertyName, string alias)
            : base(alias)
        {
            AppliesToTypeName = appliesToTypeName;
            ReferencedPropertyName = referencedPropertyName;
        }
        public string AppliesToTypeName { get; }
        public string ReferencedPropertyName { get; }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["Value"] = ReferencedPropertyName;
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
}
