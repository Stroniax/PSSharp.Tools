using System;
using System.Management.Automation;

namespace PSSharp
{
    [PSTypeConverter(
        typeof(ConstantDefinition),
        typeof(ConstantDefinition<byte>),
        typeof(ConstantDefinition<short>),
        typeof(ConstantDefinition<ushort>),
        typeof(ConstantDefinition<int>),
        typeof(ConstantDefinition<uint>),
        typeof(ConstantDefinition<string>)
        )]
    public class ConstantDefinitionConverter : PSTypeConverter
    {
        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            // use default PowerShell conversion through constructor
            return false;
        }

        public override bool CanConvertTo(object sourceValue, Type destinationType)
        {
            if (sourceValue is ConstantDefinition def)
            {
                return LanguagePrimitives.TryConvertTo(def.Value, destinationType, out _);
            }
            return false;
        }

        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            throw new PSNotSupportedException();
        }

        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            if (sourceValue is ConstantDefinition def)
            {
                if (LanguagePrimitives.TryConvertTo(def.Value, destinationType, out var result))
                {
                    return result;
                }
                else
                {
                    throw new PSInvalidCastException();
                }
            }
            else
            {
                throw new PSInvalidCastException();
            }
        }
    }
}
