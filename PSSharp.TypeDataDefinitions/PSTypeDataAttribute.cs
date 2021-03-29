using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    public abstract class PSTypeDataAttribute : Attribute
    {
        protected internal PSTypeDataAttribute()
        {
        }
        public bool Force { get; set; }
        public virtual void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            parameters["Force"] = Force;
        }
        public static Dictionary<PSTypeDataAttribute, ICustomAttributeProvider> GetTypeDataDefinitions(Type type)
            => GetTypeDataDefinitions(type, new Dictionary<PSTypeDataAttribute, ICustomAttributeProvider>());
        private static Dictionary<PSTypeDataAttribute, ICustomAttributeProvider> GetTypeDataDefinitions(Type type, Dictionary<PSTypeDataAttribute, ICustomAttributeProvider> output)
        {
            foreach (var attr in type.GetCustomAttributes<PSTypeDataAttribute>(true))
            {
                output.Add(attr, type);
            }
            foreach (var member in type.GetMembers().Where(m => m.IsDefined(typeof(PSTypeDataAttribute), false)))
            {
                foreach (var attr in member.GetCustomAttributes<PSTypeDataAttribute>(true))
                {
                    output.Add(attr, member);
                }
            }
            foreach (var nestedType in type.GetNestedTypes())
            {
                GetTypeDataDefinitions(nestedType, output);
            }
            return output;
        }
        public static Dictionary<PSTypeDataAttribute, ICustomAttributeProvider> GetTypeDataDefinitions(Assembly assembly)
        {
            var output = new Dictionary<PSTypeDataAttribute, ICustomAttributeProvider>();
            foreach (var attr in assembly.GetCustomAttributes<PSTypeDataAttribute>())
            {
                output.Add(attr, assembly);
            }
            foreach (var type in assembly.GetTypes().Where(t => t.IsPublic))
            {
                GetTypeDataDefinitions(type, output);
            }
            return output;
        }
        public override bool Equals(object obj) => ReferenceEquals(this, obj);

        public override int GetHashCode()
        {
            int hashCode = 229826242;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(TypeId);
            hashCode = hashCode * -1521134295 + Force.GetHashCode();
            return hashCode;
        }
    }
}
