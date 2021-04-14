using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Transforms the name of a constant into the value of an associated constant defined
    /// by <see cref="DefiningType"/>. For example, [ConstantValueTransformationAttribute([int])] 
    /// will transform "MaxValue" into 2147483647 (<see cref="int.MaxValue"/>).
    /// </summary>
    public class ConstantValueTransformationAttribute : FlatteningTransformationAttribute
    {
        /// <inheritdoc cref="ConstantValueTransformationAttribute" path="/summary"/>
        /// <param name="definingType"><inheritdoc cref="DefiningType" path="/summary"/></param>
        public ConstantValueTransformationAttribute(Type definingType)
        {
            DefiningType = definingType;
        }
        /// <summary>
        /// The type that defines the constants to be transformed.
        /// </summary>
        public Type DefiningType { get; }
        /// <summary>
        /// Determines if constants defined in inherited types of the <see cref="DefiningType"/>
        /// should be included in transformation.
        /// </summary>
        [PSDefaultValue(Value = false, Help = "Determines if constants defined in inherited types of the "
            + nameof(DefiningType) + " should be included in transformation.")]
        public bool IncludeInherited;
        /// <summary>
        /// Throws an exception if the object being transformed does not match the name of a constant member of the defining type.
        /// </summary>
        [PSDefaultValue(Value = false, Help = "Throws an exception if the object being transformed does " +
            "not match the name of a constant member of the defining type.")]
        public bool ThrowIfNotMatch { get; set; }
        protected override IEnumerable<object?> TransformMany(object inputData, EngineIntrinsics engineIntrinsics)
        {
            if (inputData is null)
            {
                yield return inputData;
                yield break;
            }
            var inputValues = LanguagePrimitives.ConvertTo<object[]>(inputData);
            var values = ConstantDefinition.GetValues(DefiningType, IncludeInherited);
            foreach (var input in inputValues)
            {
                var value = values.Where(i => i.Name.Equals(input?.ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (value != null)
                {
                    yield return value.Value;
                }
                else if (ThrowIfNotMatch)
                {
                    throw new PSArgumentException($"The type [{DefiningType}] does not define a public constant value named '{input}'.", nameof(inputData));
                }
                else
                {
                    yield return input;
                }
            }
        }
    }
}
