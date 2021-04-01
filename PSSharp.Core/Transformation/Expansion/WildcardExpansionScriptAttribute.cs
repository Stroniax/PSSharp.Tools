using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PSSharp
{
    public sealed class WildcardExpansionScriptAttribute : ExpandWildcardsAttribute
    {
        private WildcardExpansionScriptAttribute()
        {
            AllowNullOrEmptyTransformation = false;
            AllowTransformNonStrings = false;
            AllowTransformNonWildcardStrings = false;
        }
        public WildcardExpansionScriptAttribute(string script)
            :this()
        {
            Script = ScriptBlock.Create(script);
        }
        public WildcardExpansionScriptAttribute(ScriptBlock script)
            :this()
        {
            Script = script;
        }
        public ScriptBlock Script { get; }
        protected override IEnumerable<object> Expand(object inputData, EngineIntrinsics engineIntrinsics)
        {
            var variables = new List<PSVariable>()
            {
                new PSVariable(nameof(inputData), inputData),
                new PSVariable(nameof(engineIntrinsics), engineIntrinsics),
                new PSVariable("_", inputData)
            };
            return Script.InvokeWithContext(null, variables, inputData, engineIntrinsics);
        }
    }
}
