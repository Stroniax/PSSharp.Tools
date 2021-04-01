using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace PSSharp
{
    public class ScriptTransformationAttribute : FlatteningTransformationAttribute
    {
        public ScriptBlock Script { get; }
        public ScriptTransformationAttribute(string scriptText)
        {
            Script = ScriptBlock.Create(scriptText);
        }
        public ScriptTransformationAttribute(ScriptBlock script)
        {
            Script = script;
        }
        protected override IEnumerable<object> TransformMany(object inputData, EngineIntrinsics engineIntrinsics)
        {
            var variables = new List<PSVariable>()
            {
                new PSVariable(nameof(inputData), inputData),
                new PSVariable(nameof(engineIntrinsics), engineIntrinsics),
                new PSVariable("_", inputData),
            };
            return Script.InvokeWithContext(null, variables, engineIntrinsics, inputData);
        }
    }
}
