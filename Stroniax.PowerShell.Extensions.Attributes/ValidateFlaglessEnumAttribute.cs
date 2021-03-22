using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace Stroniax.Powershell
{
    // note that you can't derive from ValidateArgumentsAttribute as you need to catch the value
    // before it has been converted. ArgumentTransformationAttributes are applied before the
    // implicit PowerShell type casting system, which is applied before parameter validation,
    // and are permitted to throw ArgumentException if the argument is invalid.
    public class ValidateFlaglessEnumAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            // PowerShell will generally use object[], but if the parameter value is provided
            // by a property of an object we may have some other enumerable type
            if (inputData is IEnumerable enumerable) 
            {
                // basically any enumeration indicates we weren't passed a single enum value
                // however, as we could have a collection, we'll verify that there's more
                // than one value before throwing.
                var enumerator = enumerable.GetEnumerator();
                try
                {
                    enumerator.MoveNext(); // move to the first value
                    if (enumerator.MoveNext())
                    {
                        throw new PSArgumentException("The value provided must be a single enumeration value (flags and arrays are not supported).");
                    }
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            }
            // if we pass out the object that was passed in, PowerShell will still
            // apply the default type casting to the object - so if we were given a
            // string, we can return that string and PowerShell will convert it to
            // the enum we asked for in our cmdlet.
            return inputData;
        }
    }
}
