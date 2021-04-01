using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace PSSharp
{
    public abstract class WildcardExpansionAttribute : FlatteningTransformationAttribute
    {
        protected sealed override IEnumerable<object?> TransformMany(object inputData, EngineIntrinsics engineIntrinsics)
        {
            if (!(inputData is IEnumerable))
            {
                inputData = new object[] { inputData };
            }
            IEnumerable array = (inputData as IEnumerable)!;
            List<object?> results = new List<object?>();
            foreach (var item in array)
            {
                if (item is null)
                {
                    results.Add(item);
                }
                else if (item is string itemString)
                {
                    if (AllowTransformNonWildcardStrings || WildcardPattern.ContainsWildcardCharacters(itemString))
                    {
                        var expanded = Expand(itemString, engineIntrinsics);
                        if (!AllowNullOrEmptyTransformation && (expanded is null || expanded.Count() == 0))
                        {
                            results.Add(itemString);
                        }
                        else
                        {
                            results.AddRange(expanded);
                        }
                    }
                }
                else
                {
                    if (AllowTransformNonStrings)
                    {
                        var expanded = Expand(item, engineIntrinsics);
                        if (!AllowNullOrEmptyTransformation && (expanded is null || expanded.Count() == 0))
                        {
                            results.Add(item);
                        }
                        else
                        {
                            results.AddRange(expanded);
                        }
                    }
                    else
                    {
                        results.Add(item);
                    }
                }
            }
            var finalResults = results.ToArray();
            BeforeOutput(ref finalResults, engineIntrinsics);
            return finalResults;
        }
        protected abstract IEnumerable<object> Expand(object inputData, EngineIntrinsics engineIntrinsics);
        protected virtual void BeforeOutput(ref object?[] output, EngineIntrinsics engineIntrinsics)
        {
        }
        protected bool AllowNullOrEmptyTransformation { get; set; }
        protected bool AllowTransformNonStrings { get; set; }
        protected bool AllowTransformNonWildcardStrings { get; set; }
    }
}
