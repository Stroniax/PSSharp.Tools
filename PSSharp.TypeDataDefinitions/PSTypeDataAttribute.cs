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

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PSNotePropertyAttribute : PSTypeDataAttribute
    {
        public PSNotePropertyAttribute(string name, object value)
        {
            PropertyName = name;
            Value = value;
        }
        public string PropertyName { get; }
        public object Value { get; }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "NoteProperty";
            parameters["MemberName"] = PropertyName;
            parameters["Value"] = Value;
            parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
        }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ExternalPSNotePropertyAttribute : PSNotePropertyAttribute
    {
        public ExternalPSNotePropertyAttribute(Type appliesToType, string name, object value)
            : base(name, value)
        {
            AppliesToTypeName = appliesToType.FullName;
        }
        public ExternalPSNotePropertyAttribute(string appliesToTypeName, string name, object value)
            : base(name, value)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public string AppliesToTypeName { get; }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public class PSAliasPropertyAttribute : PSTypeDataAttribute
    {
        public PSAliasPropertyAttribute(string alias)
        {
            Alias = alias;
        }
        public string Alias { get; }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "AliasProperty";
            parameters["MemberName"] = Alias;
            parameters["Value"] = (attributeAppliedTo as MemberInfo)?.Name;
            parameters["TypeName"] = (attributeAppliedTo as MemberInfo)?.DeclaringType;
        }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ExternalPSAliasPropertyAttribute : PSAliasPropertyAttribute
    {
        public ExternalPSAliasPropertyAttribute(Type appliesToType, string referencedPropertyName, string alias)
            : base(alias)
        {
            AppliesToTypeName = appliesToType.FullName;
            ReferencedPropertyName = referencedPropertyName;
        }
        public ExternalPSAliasPropertyAttribute(string appliesToTypeName, string referencedPropertyName, string alias)
            : base(alias)
        {
            AppliesToTypeName = appliesToTypeName;
            ReferencedPropertyName = referencedPropertyName;
        }
        public string AppliesToTypeName { get; }
        public string ReferencedPropertyName { get; }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["Value"] = ReferencedPropertyName;
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PSCodePropertyAttribute : PSTypeDataAttribute
    {
        public PSCodePropertyAttribute(string propertyName, Type referencedGetType, string referencedGetMethodName)
            : this(propertyName, referencedGetType.FullName, referencedGetMethodName, null, null)
        {

        }
        public PSCodePropertyAttribute(string propertyName, string referencedGetTypeFullName, string referencedGetMethodName)
            : this(propertyName, referencedGetTypeFullName, referencedGetMethodName, null, null)
        {

        }
        public PSCodePropertyAttribute(string propertyName, Type? referencedGetType, string? referencedGetMethodName, Type? referencedSetType, string? referencedSetMethodName)
            : this(propertyName, referencedGetType?.FullName, referencedGetMethodName, referencedSetType?.FullName, referencedSetMethodName)
        {

        }
        public PSCodePropertyAttribute(
            string propertyName,
            string? referencedGetTypeFullName,
            string? referencedGetMethodName,
            string? referencedSetTypeFullName,
            string? referencedSetMethodName)
        {
            PropertyName = propertyName;
            ReferencedGetTypeName = referencedGetTypeFullName;
            ReferencedGetMethodName = referencedGetMethodName;
            ReferencedSetTypeName = referencedSetTypeFullName;
            ReferencedSetMethodName = referencedSetMethodName;
        }
        public string PropertyName { get; }
        public string? ReferencedGetTypeName { get; }
        public string? ReferencedGetMethodName { get; }
        public string? ReferencedSetTypeName { get; }
        public string? ReferencedSetMethodName { get; }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            // This method cannot provide all parameters. Some values are set in a PowerShell ScriptProperty
            // that overrides this method.
#warning requires PowerShell implementation
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "CodeProperty";
            parameters["MemberName"] = PropertyName;
            parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
        }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ExternalPSCodePropertyAttribute : PSCodePropertyAttribute
    {
        public string AppliesToTypeName { get; }
        public ExternalPSCodePropertyAttribute(string appliesToTypeName, string propertyName, Type referencedGetType, string referencedGetMethodName)
            : base(propertyName, referencedGetType.FullName, referencedGetMethodName)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public ExternalPSCodePropertyAttribute(string appliesToTypeName, string propertyName, string referencedGetTypeFullName, string referencedGetMethodName)
            : base(propertyName, referencedGetTypeFullName, referencedGetMethodName)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public ExternalPSCodePropertyAttribute(string appliesToTypeName, string propertyName, Type? referencedGetType, string? referencedGetMethodName, Type? referencedSetType, string? referencedSetMethodName)
            : base(propertyName, referencedGetType?.FullName, referencedGetMethodName, referencedSetType?.FullName, referencedSetMethodName)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public ExternalPSCodePropertyAttribute(string appliesToTypeName, string propertyName, string? referencedGetTypeFullName, string? referencedGetMethodName, string? referencedSetTypeFullName, string? referencedSetMethodName)
            : base(propertyName, referencedGetTypeFullName, referencedGetMethodName, referencedSetTypeFullName, referencedSetMethodName)
        {
            AppliesToTypeName = appliesToTypeName;
        }

        public ExternalPSCodePropertyAttribute(Type appliesToType, string propertyName, Type referencedGetType, string referencedGetMethodName)
            : base(propertyName, referencedGetType.FullName, referencedGetMethodName)
        {
            AppliesToTypeName = appliesToType.FullName;
        }
        public ExternalPSCodePropertyAttribute(Type appliesToType, string propertyName, string referencedGetTypeFullName, string referencedGetMethodName)
            : base(propertyName, referencedGetTypeFullName, referencedGetMethodName)
        {
            AppliesToTypeName = appliesToType.FullName;
        }
        public ExternalPSCodePropertyAttribute(Type appliesToType, string propertyName, Type? referencedGetType, string? referencedGetMethodName, Type? referencedSetType, string? referencedSetMethodName)
            : base(propertyName, referencedGetType?.FullName, referencedGetMethodName, referencedSetType?.FullName, referencedSetMethodName)
        {
            AppliesToTypeName = appliesToType.FullName;
        }
        public ExternalPSCodePropertyAttribute(Type appliesToType, string propertyName, string? referencedGetTypeFullName, string? referencedGetMethodName, string? referencedSetTypeFullName, string? referencedSetMethodName)
            : base(propertyName, referencedGetTypeFullName, referencedGetMethodName, referencedSetTypeFullName, referencedSetMethodName)
        {
            AppliesToTypeName = appliesToType.FullName;
        }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PSScriptPropertyAttribute : PSTypeDataAttribute
    {
        public PSScriptPropertyAttribute(string propertyName, string? getScript)
            : this(propertyName, getScript, null)
        {
        }
        public PSScriptPropertyAttribute(string propertyName, string? getScript, string? setScript)
        {
            PropertyName = propertyName;
            GetScript = getScript;
            SetScript = setScript;
        }
        public string PropertyName { get; }
        public string? GetScript { get; }
        public string? SetScript { get; }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            // This method cannot provide all parameters. Some values are set in a PowerShell ScriptProperty
            // that overrides this method.
#warning requires PowerShell implementation
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "ScriptProperty";
            parameters["MemberName"] = PropertyName;
            parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
        }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ExternalPSScriptPropertyAttribute : PSScriptPropertyAttribute
    {
        public string AppliesToTypeName { get; }
        public ExternalPSScriptPropertyAttribute(Type appliesToType, string propertyName, string? getScript)
            : this(appliesToType.FullName, propertyName, getScript, null)
        {
        }
        public ExternalPSScriptPropertyAttribute(Type appliesToType, string propertyName, string? getScript, string? setScript)
            : this(appliesToType.FullName, propertyName, getScript, setScript)
        {
        }
        public ExternalPSScriptPropertyAttribute(string appliesToTypeName, string propertyName, string? getScript)
            : this(appliesToTypeName, propertyName, getScript, null)
        {
        }
        public ExternalPSScriptPropertyAttribute(string appliesToTypeName, string propertyName, string? getScript, string? setScript)
            : base(propertyName, getScript, setScript)
        {
            AppliesToTypeName = appliesToTypeName;
        }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class PSCodePropertyDefinitionAttribute : PSTypeDataAttribute
    {
        public PSCodePropertyDefinitionAttribute(string appliesToTypeName)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public PSCodePropertyDefinitionAttribute(Type appliesToType)
        {
            AppliesToTypeName = appliesToType.Name;
        }
        public string AppliesToTypeName { get; }
        public string? PropertyName { get; set; }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "CodeProperty";
            parameters["MemberName"] = PropertyName;
            parameters["TypeName"] = AppliesToTypeName;
            if (attributeAppliedTo is MethodInfo method)
            {
                if (!method.IsStatic) return;
                var methodParameters = method.GetParameters();
                if (methodParameters.Length == 0) return;
                if (methodParameters[0].ParameterType.FullName != "System.Management.Automation.PSObject") return;
                if (methodParameters.Length == 1)
                {
                    parameters["Value"] = method;
                }
                else if (methodParameters.Length == 2)
                {
                    parameters["SecondValue"] = method;
                }
            }
        }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class PSCodeMethodDefinitionAttribute : PSTypeDataAttribute
    {
        public PSCodeMethodDefinitionAttribute(string appliesToTypeName)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public PSCodeMethodDefinitionAttribute(Type appliesToType)
        {
            AppliesToTypeName = appliesToType.Name;
        }
        public string AppliesToTypeName { get; }
        public string? MethodName { get; set; }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "CodeMethod";
            parameters["MemberName"] = MethodName;
            parameters["TypeName"] = AppliesToTypeName;
            if (attributeAppliedTo is MethodInfo method)
            {
                if (!method.IsStatic) return;
                var methodParameters = method.GetParameters();
                if (methodParameters.Length == 0) return;
                if (methodParameters[0].ParameterType.FullName != "System.Management.Automation.PSObject") return;
                if (methodParameters.Length == 1)
                {
                    parameters["Value"] = method;
                }
                else if (methodParameters.Length == 2)
                {
                    parameters["SecondValue"] = method;
                }
            }
        }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PSCodeMethodFromExtensionMethodAttribute : PSTypeDataAttribute
    {
        public string? MethodName { get; set; }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            // This method cannot provide all parameters. Some values are set in a PowerShell ScriptProperty
            // that overrides this method.
#warning requires PowerShell implementation
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "CodeMethod";
            parameters["MemberName"] = MethodName;
            if (attributeAppliedTo is MethodInfo method)
            {
                parameters["MemberName"] ??= method.Name;
                var methodParameters = method.GetParameters();
                if (methodParameters.Length == 0) return;
                parameters["TypeName"] = methodParameters[0].ParameterType.FullName;
            }
        }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PSCodePropertyFromExtensionMethodAttribute : PSTypeDataAttribute
    {
        public string? PropertyName { get; set; }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            // This method cannot provide all parameters. Some values are set in a PowerShell ScriptProperty
            // that overrides this method.
#warning requires PowerShell implementation
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "CodeProperty";
            parameters["MemberName"] = PropertyName;
            if (attributeAppliedTo is MethodInfo method)
            {
                parameters["MemberName"] ??= method.Name;
                var methodParameters = method.GetParameters();
                if (methodParameters.Length == 0) return;
                parameters["TypeName"] = methodParameters[0].ParameterType.FullName;
            }
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PSScriptMethodAttribute : PSTypeDataAttribute
    {
        public string MethodName { get; }
        public string Script { get; }
        public PSScriptMethodAttribute(string methodName, string script)
        {
            MethodName = methodName;
            Script = script;
        }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            // This method cannot provide all parameters. Some values are set in a PowerShell ScriptProperty
            // that overrides this method.
#warning requires PowerShell implementation
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "ScriptMethod";
            parameters["MemberName"] = MethodName;
            parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ExternalPSScriptMethodAttribute : PSScriptMethodAttribute
    {
        public ExternalPSScriptMethodAttribute(string appliesToTypeName, string methodName, string script)
            : base(methodName, script)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public ExternalPSScriptMethodAttribute(Type appliesToType, string methodName, string script)
            : base(methodName, script)
        {
            AppliesToTypeName = appliesToType.FullName;
        }
        public string AppliesToTypeName { get; }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PSCodeMethodAttribute : PSTypeDataAttribute
    {
        public PSCodeMethodAttribute(string referencedTypeName, string referencedMethodName)
        {
            ReferencedTypeName = referencedTypeName;
            ReferencedMethodName = referencedMethodName;
        }
        public PSCodeMethodAttribute(Type referencedType, string referencedMethodName)
        {
            ReferencedTypeName = referencedType.FullName;
            ReferencedMethodName = referencedMethodName;
        }
        public string ReferencedTypeName { get; }
        public string ReferencedMethodName { get; }
        public string MethodName { get => _methodName ?? ReferencedMethodName; set => _methodName = value; }
        private string? _methodName;

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            // This method cannot provide all parameters. Some values are set in a PowerShell ScriptProperty
            // that overrides this method.
#warning requires PowerShell implementation
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["MemberType"] = "CodeMethod";
            parameters["MemberName"] = MethodName;
            parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ExternalPSCodeMethodAttribute : PSCodeMethodAttribute
    {
        public string AppliesToTypeName { get; }
        public ExternalPSCodeMethodAttribute(string appliesToTypeName, string referencedTypeName, string referencedMethodName)
            : base(referencedTypeName, referencedMethodName) => AppliesToTypeName = appliesToTypeName;
        public ExternalPSCodeMethodAttribute(string appliesToTypeName, Type referencedType, string referencedMethodName)
            : base(referencedType, referencedMethodName) => AppliesToTypeName = appliesToTypeName;
        public ExternalPSCodeMethodAttribute(Type appliesToType, string referencedTypeName, string referencedMethodName)
            : base(referencedTypeName, referencedMethodName) => AppliesToTypeName = appliesToType.FullName;
        public ExternalPSCodeMethodAttribute(Type appliesToType, Type referencedType, string referencedMethodName)
            : base(referencedType, referencedMethodName) => AppliesToTypeName = appliesToType.FullName;

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PSDefaultPropertySetAttribute : PSTypeDataAttribute
    {
        public PSDefaultPropertySetAttribute(params string[] properties)
        {
            _properties = properties;
        }
        public string[] Properties { get => _properties.ToArray(); }
        private string[] _properties;

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["DefaultDisplayPropertySet"] = Properties;
            parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
        }
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ExternalPSDefaultPropertySetAttribute : PSDefaultPropertySetAttribute
    {
        public string AppliesToTypeName { get; }
        public ExternalPSDefaultPropertySetAttribute(string appliesToTypeName, params string[] properties)
            : base(properties)
        {
            AppliesToTypeName = appliesToTypeName;
        }
        public ExternalPSDefaultPropertySetAttribute(Type appliesToType, params string[] properties)
            : base(properties)
        {
            AppliesToTypeName = appliesToType.FullName;
        }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            parameters["TypeName"] = AppliesToTypeName;
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PSTypeConverterAttribute : PSTypeDataAttribute
    {
        public Type? PSTypeConverter { get; }
        public string[]? CanConvertTypeNames { get => _canConvertTypeNames.ToArray(); }
        private readonly string[]? _canConvertTypeNames;

        public PSTypeConverterAttribute(Type typeConverter)
        {
            PSTypeConverter = typeConverter;
        }
        public PSTypeConverterAttribute(params string[] typesToConvert)
        {
            _canConvertTypeNames = typesToConvert;
        }
        public PSTypeConverterAttribute(params Type[] typesToConvert)
        {
            _canConvertTypeNames = typesToConvert.Select(i => i.FullName).ToArray();
        }

        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            if (PSTypeConverter != null)
            {
                parameters["TypeConverter"] = PSTypeConverter;
                parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
            }
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PSTypeAdapterAttribute : PSTypeDataAttribute
    {
        public Type? PSTypeAdapter { get; }
        public string[]? CanAdaptTypeNames { get => _canAdaptTypeNames.ToArray(); }
        private readonly string[]? _canAdaptTypeNames;
        public PSTypeAdapterAttribute(Type typeAdapter)
        {
            PSTypeAdapter = typeAdapter;
        }
        public PSTypeAdapterAttribute(params string[] typesToAdapt)
        {
            _canAdaptTypeNames = typesToAdapt;
        }
        public PSTypeAdapterAttribute(params Type[] typesToAdapt)
        {
            _canAdaptTypeNames = typesToAdapt.Select(i => i.FullName).ToArray();
        }
        public override void SetParameters(Hashtable parameters, ICustomAttributeProvider attributeAppliedTo)
        {
            base.SetParameters(parameters, attributeAppliedTo);
            if (PSTypeAdapter != null)
            {
                parameters["TypeAdapter"] = PSTypeAdapter;
                parameters["TypeName"] = (attributeAppliedTo as Type)?.FullName;
            }
        }
    }

    [PSScriptProperty("John", "'Cena'")]
    [PSNoteProperty("Dwayne", "(The Rock) Johnson")]
    [PSCodeProperty("Andre", "The", "Giant")]
    [PSDefaultPropertySet(nameof(DefaultProperty), "John", "Andre")]
    [PSScriptProperty("One", "'Three'")]
    public class Test
    {
        public string? DefaultProperty { get; set; }
    }
    public static class StaticTests
    {
        [PSCodeMethodFromExtensionMethod]
        public static bool IsNull(this string fromString) => fromString is null;
        [PSCodePropertyFromExtensionMethod]
        public static bool IsNullOrEmpty(this string fromString) => string.IsNullOrEmpty(fromString);
        [PSCodeMethodFromExtensionMethod]
        public static string ToString(this List<string> array, string separator) => string.Join(separator, array);
    }
}
