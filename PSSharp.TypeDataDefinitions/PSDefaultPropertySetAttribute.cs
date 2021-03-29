using System;
using System.Collections;
using System.Reflection;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PSDefaultPropertySetAttribute : PSTypeDataAttribute
    {
        public PSDefaultPropertySetAttribute(params string[] properties)
        {
            _properties = properties;
        }
        public string[] Properties { get => _properties.ToArray(); }
        private string[] _properties;

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["DefaultDisplayPropertySet"] = Properties;
            parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
        }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ExternalPSDefaultPropertySetAttribute : PSDefaultPropertySetAttribute
    {
        public string AppliesToTypeName { get; }
        public ExternalPSDefaultPropertySetAttribute(string appliesToTypeName, params string[] properties)
            : base(properties)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public ExternalPSDefaultPropertySetAttribute(Type appliesToType, params string[] properties)
            : base(properties)
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
