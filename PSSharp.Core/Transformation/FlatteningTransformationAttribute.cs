using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Transformation attribute that takes a group of transformed values and returns only
    /// the inner item if the number of items is one.
    /// </summary>
    public abstract class FlatteningTransformationAttribute : ArgumentTransformationAttribute
    {
        public bool DoNotFlatten { get; set; }
        public override object? Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            var rawResults = TransformMany(inputData, engineIntrinsics);
            object?[] result;

            if (rawResults is object[] objArr)
            {
                result = objArr;
            }
            else
            {
                result = rawResults.ToArray();
            }

            if (!DoNotFlatten && result.Length == 1)
            {
                return result[0];
            }
            else
            {
                return result;
            }
        }
        protected abstract IEnumerable<object?> TransformMany(object inputData, EngineIntrinsics engineIntrinsics);
    }
}
