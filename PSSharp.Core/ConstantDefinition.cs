using System;
using System.Linq;
using System.Reflection;

namespace PSSharp
{
    public class ConstantDefinition
    {
        public static ConstantDefinition<T>[] GetValues<T>(bool includeInherited = false)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            if (includeInherited) flags |= BindingFlags.FlattenHierarchy;
            return typeof(T).GetFields(flags)
                .Where(i => !i.IsInitOnly && i.IsLiteral)
                .Select(i => new ConstantDefinition<T>(i))
                .ToArray();
        }
        public static ConstantDefinition[] GetValues(Type type, bool includeInherited = false)
        {
            var ctor = typeof(ConstantDefinition<>)
                .MakeGenericType(type)
                .GetConstructor(new[] { typeof(FieldInfo) });
            var instantiateResult = new Func<FieldInfo, ConstantDefinition>(field => (ConstantDefinition)ctor.Invoke(new[] { field }));

            BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            if (includeInherited) flags |= BindingFlags.FlattenHierarchy;

            return type.GetFields(flags)
                .Where(i => !i.IsInitOnly && i.IsLiteral)
                .Select(i => instantiateResult(i))
                .ToArray();
        }

        public ConstantDefinition(FieldInfo definition)
        {
            if (!definition.IsLiteral || definition.IsInitOnly)
            {
                throw new ArgumentException("The field definition provided is not a constant field.");
            }
            _constField = definition;
        }
        private readonly FieldInfo _constField;
        public FieldInfo GetDefinition() => _constField;
        public Type SourceType { get => _constField.DeclaringType; }
        public string Name { get => _name ??= _constField.Name; }
        private string? _name;
        public object Value { get => _value ??= _constField.GetValue(null); }
        private object? _value;
    }
    public class ConstantDefinition<TSource> : ConstantDefinition
    {
        public ConstantDefinition(FieldInfo definition)
            : base(definition)
        {
            if (!typeof(TSource).IsAssignableFrom(SourceType) && !SourceType.IsAssignableFrom(typeof(TSource)))
            {
                throw new ArgumentException("The declaring type of the constant definition is not " +
                      "valid for the generic type argument provided.", nameof(definition));
            }
        }
        public ConstantDefinition(string constName)
            : this(GetFieldByName(constName))
        {

        }
        private static FieldInfo GetFieldByName(string name)
        {
            var definition = typeof(TSource).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                         && i.IsLiteral
                         && !i.IsInitOnly)
                .FirstOrDefault();
            if (definition is null)
            {
                throw new ArgumentException("The type provided does not define a constant member with the provided name.", nameof(name));
            }
            else return definition;
        }
    }
}
