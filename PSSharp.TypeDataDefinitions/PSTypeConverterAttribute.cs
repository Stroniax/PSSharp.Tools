using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace PSSharp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PSTypeConverterAttribute : PSTypeDataAttribute
    {
        public Type? PSTypeConverter { get; }
        public string[]? CanConvertTypeNames { get => _canConvertTypeNames.ToArray(); }
        private readonly string[]? _canConvertTypeNames;

        public PSTypeConverterAttribute(Type typeConverter)
        {
            PSTypeConverter = typeConverter;
        }
        public PSTypeConverterAttribute(params string[] typesToConvert)
        {
            _canConvertTypeNames = typesToConvert;
        }
        public PSTypeConverterAttribute(params Type[] typesToConvert)
        {
            _canConvertTypeNames = typesToConvert.Select(i => i.FullName).ToArray();
        }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            if (PSTypeConverter != null)
            {
                parameters["TypeConverter"] = PSTypeConverter;
                parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
            }
        }
    }
}
