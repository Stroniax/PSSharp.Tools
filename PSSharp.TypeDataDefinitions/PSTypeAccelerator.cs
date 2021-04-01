using System;

namespace PSSharp
{
    /// <summary>
    /// Creates a type accelerator to allow referencing a type by an alias.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Delegate | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public class PSTypeAccelerator : PSTypeDataAttribute
    {
        /// <summary>
        /// The alias used to reference this type.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Creates a type accelerator to allow referencing a type by an alias.
        /// </summary>
        /// <param name="name">The alias used to referenc this type.</param>
        public PSTypeAccelerator(string name)
        {
            Name = name;
        }
    }
}
