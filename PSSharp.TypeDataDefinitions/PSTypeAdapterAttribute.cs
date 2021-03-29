using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PSTypeAdapterAttribute : PSTypeDataAttribute
    {
        public Type? PSTypeAdapter { get; }
        public string[]? CanAdaptTypeNames { get => _canAdaptTypeNames.ToArray(); }
        private readonly string[]? _canAdaptTypeNames;
        public PSTypeAdapterAttribute(Type typeAdapter)
        {
            PSTypeAdapter = typeAdapter;
        }
        public PSTypeAdapterAttribute(params string[] typesToAdapt)
        {
            _canAdaptTypeNames = typesToAdapt;
        }
        public PSTypeAdapterAttribute(params Type[] typesToAdapt)
        {
            _canAdaptTypeNames = typesToAdapt.Select(i => i.FullName).ToArray();
        }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            if (PSTypeAdapter != null)
            {
                parameters["TypeAdapter"] = PSTypeAdapter;
                parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
            }
        }
    }
}
