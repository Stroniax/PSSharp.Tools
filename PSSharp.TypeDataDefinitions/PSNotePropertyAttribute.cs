using System;
using System.Collections;
using System.Reflection;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PSNotePropertyAttribute : PSTypeDataAttribute
    {
        public PSNotePropertyAttribute(string name, object value)
        {
            PropertyName = name;
            Value = value;
        }
        public string PropertyName { get; }
        public object Value { get; }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "NoteProperty";
            parameters["MemberName"] = PropertyName;
            parameters["Value"] = Value;
            parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
        }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ExternalPSNotePropertyAttribute : PSNotePropertyAttribute
    {
        public ExternalPSNotePropertyAttribute(Type appliesToType, string name, object value)
            : base(name, value)
        {
            AppliesToTypeName = appliesToType.FullName;
        }
        public ExternalPSNotePropertyAttribute(string appliesToTypeName, string name, object value)
            : base(name, value)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public string AppliesToTypeName { get; }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
}
